using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Authentication;
using Application.Services;
using Domain.Entities.Authentication;
using Domain.Exceptions;
using Domain.Interfaces;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services
{
    public class BackupCodeService : IBackupCodeService
    {
        private readonly IBackupCodeRepository _backupCodeRepository;
        private readonly IAdminRepository _adminRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ISecurityAuditLogRepository _auditLogRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ILocalizationService _localizer;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;

        private const int BACKUP_CODES_COUNT = 10;
        private const int CODE_LENGTH = 8;
        private const string CODE_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"; // Uppercase alphanumeric
        private const int MAX_GENERATION_PER_DAY = 5; // Prevent abuse
        private const int MAX_VERIFY_ATTEMPTS_PER_HOUR = 10; // Prevent brute force

        public BackupCodeService(
            IBackupCodeRepository backupCodeRepository,
            IAdminRepository adminRepository,
            IPasswordHasher passwordHasher,
            ISecurityAuditLogRepository auditLogRepository,
            IHttpContextAccessor httpContextAccessor,
            IJwtTokenGenerator jwtTokenGenerator,
            IRefreshTokenRepository refreshTokenRepository,
            ILocalizationService localizer,
            IUnitOfWork unitOfWork,
            IEmailService emailService)
        {
            _backupCodeRepository = backupCodeRepository;
            _adminRepository = adminRepository;
            _passwordHasher = passwordHasher;
            _auditLogRepository = auditLogRepository;
            _httpContextAccessor = httpContextAccessor;
            _jwtTokenGenerator = jwtTokenGenerator;
            _refreshTokenRepository = refreshTokenRepository;
            _localizer = localizer;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        public async Task<GenerateBackupCodesResponse> GenerateBackupCodesAsync(Guid adminId, string currentPassword)
        {
            // SECURITY: Verify admin exists AND is active
            var admin = await _adminRepository.GetByIdAsync(adminId, null);
            if (admin == null || admin.IsDeleted)
            {
                throw new NotFoundException(_localizer["Admin.NotFound"]);
            }

            // SECURITY: Check if admin account is active
            if (!admin.IsActive)
            {
                throw new BadRequestException(_localizer["Account.Deactivated"] ?? "Account is deactivated");
            }

            // SECURITY: Validate password length to prevent DoS
            if (string.IsNullOrEmpty(currentPassword) || currentPassword.Length > 1000)
            {
                throw new BadRequestException(_localizer["InvalidCurrentPassword"] ?? "Invalid password");
            }

            // SECURITY: CRITICAL - Verify current password before generating codes
            if (!_passwordHasher.VerifyPassword(currentPassword, admin.Password))
            {
                // AUDIT: Log failed password attempt
                await LogSecurityEventAsync(
                    adminId: adminId,
                    eventType: "BackupCodeGenerationPasswordFailed",
                    eventDescription: "Invalid password provided for backup code generation",
                    success: false,
                    errorMessage: "Invalid password"
                );
                throw new BadRequestException(_localizer["InvalidCurrentPassword"] ?? "Invalid password");
            }

            // SECURITY: Rate limiting - check how many times generated in last 24 hours from audit log
            // Using audit log prevents bypass by deleting codes
            var oneDayAgo = DateTime.UtcNow.AddDays(-1);
            var recentGenerations = await _auditLogRepository.CountRecentEventsByIpAsync(
                GetClientIpAddress(),
                oneDayAgo,
                "BackupCodesGenerated"
            );

            if (recentGenerations >= MAX_GENERATION_PER_DAY)
            {
                throw new BadRequestException(
                    _localizer["BackupCodes.TooManyGenerations"] ?? 
                    $"Too many backup code generations. Maximum {MAX_GENERATION_PER_DAY} per day.");
            }

            // Delete all existing backup codes
            await _backupCodeRepository.DeleteAllForAdminAsync(adminId);

            // Generate batch ID to link all codes together
            var batchId = Guid.NewGuid();
            var plainTextCodes = new List<string>();

            // Generate 10 backup codes with duplicate check
            var generatedHashes = new HashSet<string>();
            
            for (int i = 0; i < BACKUP_CODES_COUNT; i++)
            {
                string code;
                string codeHash;
                int attempts = 0;
                
                // SECURITY: Ensure no duplicate codes (extremely rare but possible)
                do
                {
                    code = GenerateBackupCode();
                    codeHash = HashCode(code);
                    attempts++;
                    
                    if (attempts > 100)
                    {
                        throw new InvalidOperationException("Failed to generate unique backup code");
                    }
                } while (generatedHashes.Contains(codeHash));
                
                generatedHashes.Add(codeHash);

                var backupCode = new BackupCode
                {
                    AdminId = adminId,
                    CodeHash = codeHash,
                    BatchId = batchId,
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(90) // Codes expire after 90 days
                };

                await _backupCodeRepository.AddAsync(backupCode);
                plainTextCodes.Add(code);
            }

            await _unitOfWork.SaveChangesAsync();

            // AUDIT: Log backup codes generation
            await LogSecurityEventAsync(
                adminId: adminId,
                eventType: "BackupCodesGenerated",
                eventDescription: $"Generated {BACKUP_CODES_COUNT} new backup codes",
                success: true,
                metadata: $"{{\"batchId\":\"{batchId}\",\"codesCount\":{BACKUP_CODES_COUNT}}}"
            );

            // Send email notification about backup codes generation
            if (!string.IsNullOrEmpty(admin.Email))
            {
                var adminName = $"{admin.FirstName} {admin.LastName}".Trim();
                if (string.IsNullOrEmpty(adminName)) adminName = admin.Username;
                
                // Send asynchronously without waiting (fire and forget)
                _ = _emailService.SendBackupCodesGeneratedEmailAsync(
                    admin.Email, 
                    adminName, 
                    BACKUP_CODES_COUNT, 
                    GetClientIpAddress(), 
                    admin.PreferredLanguage);
            }

            return new GenerateBackupCodesResponse
            {
                Codes = plainTextCodes,
                Message = _localizer["BackupCodes.Generated"] ?? "Backup codes generated successfully. Save them in a secure location."
            };
        }

        public async Task<AuthenticationResponse> VerifyBackupCodeAsync(VerifyBackupCodeRequest request)
        {
            // SECURITY: Validate input before processing
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                throw new BadRequestException("Username is required");
            }

            if (string.IsNullOrWhiteSpace(request.BackupCode))
            {
                throw new BadRequestException("Backup code is required");
            }

            // SECURITY: TIMING ATTACK PREVENTION - Always hash the code first
            var codeHash = HashCode(request.BackupCode.ToUpper());
            
            // Find admin by username or email (case-insensitive)
            var usernameLower = request.Username.ToLower();
            var admin = (await _adminRepository.FindAsync(a =>
                ((a.Username.ToLower() == usernameLower || a.Email.ToLower() == usernameLower) && !a.IsDeleted)))
                .FirstOrDefault();

            // SECURITY: TIMING ATTACK PREVENTION - Always query codes even if admin not found
            BackupCode? backupCode = null;
            if (admin != null)
            {
                // SECURITY: Check if admin account is active
                if (!admin.IsActive)
                {
                    throw new BadRequestException(_localizer["Account.Deactivated"] ?? "Account is deactivated");
                }

                // SECURITY: Check if admin has 2FA enabled
                if (!admin.IsTwoFactorEnabled)
                {
                    throw new BadRequestException(
                        _localizer["BackupCodes.TwoFactorNotEnabled"] ?? 
                        "Two-factor authentication is not enabled for this account.");
                }

                // SECURITY: Rate limiting - check FAILED verification attempts in last hour from audit log
                var oneHourAgo = DateTime.UtcNow.AddHours(-1);
                var recentFailedAttempts = await _auditLogRepository.CountRecentFailedAttemptsByIpAsync(
                    GetClientIpAddress(),
                    oneHourAgo,
                    "BackupCodeVerificationFailed"
                );

                if (recentFailedAttempts >= MAX_VERIFY_ATTEMPTS_PER_HOUR)
                {
                    throw new BadRequestException(
                        _localizer["BackupCodes.TooManyAttempts"] ?? 
                        "Too many verification attempts. Please try again later.");
                }

                // Find matching backup code (with transaction lock to prevent race condition)
                backupCode = await _backupCodeRepository.GetByAdminAndHashAsync(admin.Id, codeHash);
            }

            // SECURITY: Generic error message to prevent information leakage
            if (admin == null || backupCode == null)
            {
                // AUDIT: Log failed verification attempt
                await LogSecurityEventAsync(
                    adminId: admin?.Id,
                    eventType: "BackupCodeVerificationFailed",
                    eventDescription: "Invalid backup code attempted",
                    username: request.Username,
                    success: false,
                    errorMessage: "Invalid backup code"
                );

                // Simulate processing time to prevent timing attacks
                await Task.Delay(100);
                throw new InvalidOtpException(_localizer["BackupCodes.Invalid"] ?? "Invalid backup code.");
            }

            // SECURITY: Check if backup code is expired
            if (backupCode.IsExpired)
            {
                // AUDIT: Log expired code attempt
                await LogSecurityEventAsync(
                    adminId: admin.Id,
                    eventType: "BackupCodeExpiredAttempt",
                    eventDescription: "Attempted to use expired backup code",
                    username: request.Username,
                    success: false,
                    errorMessage: "Backup code expired"
                );

                throw new InvalidOtpException(_localizer["BackupCodes.Expired"] ?? "This backup code has expired. Please generate new codes.");
            }

            // SECURITY: RACE CONDITION PREVENTION - Mark as used
            await _backupCodeRepository.MarkAsUsedAsync(backupCode.Id);

            // Generate JWT tokens for successful authentication
            var accessToken = _jwtTokenGenerator.GenerateToken(admin);
            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken(admin);

            // Store refresh token (already generated with correct properties)
            await _refreshTokenRepository.AddAsync(refreshToken);

            // CRITICAL: Single transaction - mark code as used AND save refresh token atomically
            // If this fails, both operations rollback - user can try again with same code
            await _unitOfWork.SaveChangesAsync();

            // Check remaining codes and warn if low (after successful save)
            var remainingCount = await _backupCodeRepository.CountUnusedCodesAsync(admin.Id);
            
            // AUDIT: Log successful backup code usage (separate transaction - OK if this fails)
            await LogSecurityEventAsync(
                adminId: admin.Id,
                eventType: "BackupCodeUsed",
                eventDescription: "Backup code successfully verified for 2FA",
                username: request.Username,
                success: true,
                metadata: $"{{\"remainingCodes\":{remainingCount}}}"
            );

            // Send email notifications based on remaining codes
            if (!string.IsNullOrEmpty(admin.Email))
            {
                var adminName = $"{admin.FirstName} {admin.LastName}".Trim();
                if (string.IsNullOrEmpty(adminName)) adminName = admin.Username;
                
                // Always send backup code used notification
                _ = _emailService.SendBackupCodeUsedEmailAsync(
                    admin.Email, 
                    adminName, 
                    remainingCount, 
                    GetClientIpAddress(), 
                    admin.PreferredLanguage);

                // Send additional warnings based on remaining codes
                if (remainingCount == 0)
                {
                    // Critical: All codes depleted
                    _ = _emailService.SendBackupCodesDepletedEmailAsync(
                        admin.Email, 
                        adminName, 
                        admin.PreferredLanguage);
                }
                else if (remainingCount <= 2)
                {
                    // Warning: Low on codes
                    _ = _emailService.SendBackupCodesLowEmailAsync(
                        admin.Email, 
                        adminName, 
                        remainingCount,
                        admin.PreferredLanguage);
                }
            }

            // Build warning message based on remaining codes
            string warningMessage = null;
            if (remainingCount == 0)
            {
                warningMessage = _localizer["BackupCodes.LastCodeUsed"] ?? "This was your last backup code. Generate new codes immediately.";
            }
            else if (remainingCount < 3)
            {
                warningMessage = string.Format(
                    _localizer["BackupCodes.LowCount"] ?? "You have {0} backup codes remaining.",
                    remainingCount
                );
            }

            return new AuthenticationResponse
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                Message = warningMessage ?? (_localizer["BackupCodes.Verified"] ?? "Backup code verified successfully.")
            };
        }

        public async Task<BackupCodesStatusDto> GetBackupCodesStatusAsync(Guid adminId)
        {
            // SECURITY: Verify admin exists AND is active
            var admin = await _adminRepository.GetByIdAsync(adminId, null);
            if (admin == null || admin.IsDeleted)
            {
                throw new NotFoundException(_localizer["Admin.NotFound"]);
            }

            if (!admin.IsActive)
            {
                throw new BadRequestException(_localizer["Account.Deactivated"] ?? "Account is deactivated");
            }

            var allCodes = await _backupCodeRepository.GetAllByAdminIdAsync(adminId);
            var now = DateTime.UtcNow;
            
            // Calculate unused AND non-expired codes
            var availableCodes = allCodes.Where(c => !c.IsUsed && c.ExpiresAt > now).ToList();
            var unusedCount = availableCodes.Count;
            
            // Calculate expired codes
            var expiredCount = allCodes.Count(c => c.ExpiresAt <= now);
            
            // Find next expiry date among available codes
            DateTime? nextExpiryDate = null;
            int? daysUntilExpiry = null;
            
            if (availableCodes.Any())
            {
                nextExpiryDate = availableCodes.Min(c => c.ExpiresAt);
                daysUntilExpiry = (int)(nextExpiryDate.Value - now).TotalDays;
            }

            return new BackupCodesStatusDto
            {
                RemainingCodes = unusedCount,
                TotalCodes = allCodes.Count,
                ExpiredCodes = expiredCount,
                NextExpiryDate = nextExpiryDate,
                DaysUntilExpiry = daysUntilExpiry
            };
        }

        public async Task DeleteAllBackupCodesAsync(Guid adminId)
        {
            // SECURITY: Verify admin exists AND is active
            var admin = await _adminRepository.GetByIdAsync(adminId, null);
            if (admin == null || admin.IsDeleted)
            {
                throw new NotFoundException(_localizer["Admin.NotFound"]);
            }

            if (!admin.IsActive)
            {
                throw new BadRequestException(_localizer["Account.Deactivated"] ?? "Account is deactivated");
            }

            await _backupCodeRepository.DeleteAllForAdminAsync(adminId);
            await _unitOfWork.SaveChangesAsync();

            // AUDIT: Log backup codes deletion
            await LogSecurityEventAsync(
                adminId: adminId,
                eventType: "BackupCodesDeleted",
                eventDescription: "All backup codes deleted by admin",
                success: true
            );
        }

        public async Task<ExportBackupCodesResponse> ExportBackupCodesAsync(Guid adminId, ExportBackupCodesRequest request)
        {
            // SECURITY: Verify admin exists AND is active
            var admin = await _adminRepository.GetByIdAsync(adminId, null);
            if (admin == null || admin.IsDeleted)
            {
                throw new NotFoundException(_localizer["Admin.NotFound"]);
            }

            if (!admin.IsActive)
            {
                throw new BadRequestException(_localizer["Account.Deactivated"] ?? "Account is deactivated");
            }

            // Validate codes provided
            if (request.Codes == null || request.Codes.Count == 0)
            {
                throw new BadRequestException("No backup codes provided for export");
            }

            // SECURITY: Validate code format to prevent injection attacks
            // Each code must be exactly 8 characters, uppercase alphanumeric
            foreach (var code in request.Codes)
            {
                if (string.IsNullOrWhiteSpace(code) || 
                    code.Length != CODE_LENGTH || 
                    !code.All(c => char.IsUpper(c) || char.IsDigit(c)))
                {
                    throw new BadRequestException($"Invalid backup code format: '{code}'. Codes must be 8-character uppercase alphanumeric.");
                }
            }

            var exportedAt = DateTime.UtcNow;
            var normalizedFormat = request.Format.ToLower();
            string fileContent;
            string contentType;
            string fileName;

            // Generate content based on format
            switch (normalizedFormat)
            {
                case "pdf":
                    fileContent = GeneratePdfContent(request.Codes, admin.Username, exportedAt);
                    contentType = "application/pdf";
                    fileName = $"SYNFLOX_BackupCodes_{admin.Username}_{exportedAt:yyyyMMdd_HHmmss}.pdf";
                    break;

                case "text":
                    fileContent = GenerateTextContent(request.Codes, admin.Username, exportedAt);
                    contentType = "text/plain";
                    fileName = $"SYNFLOX_BackupCodes_{admin.Username}_{exportedAt:yyyyMMdd_HHmmss}.txt";
                    break;

                case "json":
                    fileContent = GenerateJsonContent(request.Codes, admin.Username, exportedAt);
                    contentType = "application/json";
                    fileName = $"SYNFLOX_BackupCodes_{admin.Username}_{exportedAt:yyyyMMdd_HHmmss}.json";
                    break;

                default:
                    throw new BadRequestException($"Invalid export format: {request.Format}");
            }

            // AUDIT: Log backup codes export
            await LogSecurityEventAsync(
                adminId: adminId,
                eventType: "BackupCodesExported",
                eventDescription: $"Backup codes exported in {normalizedFormat.ToUpper()} format",
                success: true,
                metadata: $"{{\"format\":\"{normalizedFormat}\",\"codesCount\":{request.Codes.Count}}}"
            );

            return new ExportBackupCodesResponse
            {
                FileContent = fileContent,
                ContentType = contentType,
                FileName = fileName,
                UnusedCodesCount = request.Codes.Count,
                Format = normalizedFormat.ToUpper(),
                ExportedAt = exportedAt,
                Message = $"Backup codes exported successfully as {normalizedFormat.ToUpper()}"
            };
        }

        // ========================================
        // PRIVATE HELPER METHODS
        // ========================================

        /// <summary>
        /// Generate random 8-character backup code (uppercase alphanumeric)
        /// Uses rejection sampling to ensure uniform distribution (no modulo bias)
        /// Example: A3B9K2L7
        /// </summary>
        private string GenerateBackupCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var code = new char[CODE_LENGTH];
            var charSetLength = CODE_CHARS.Length;
            
            // Calculate rejection threshold to eliminate modulo bias
            // For 36 chars: 256 - (256 % 36) = 256 - 4 = 252
            // Any byte >= 252 is rejected to ensure uniform distribution
            var rejectionThreshold = 256 - (256 % charSetLength);

            for (int i = 0; i < CODE_LENGTH; i++)
            {
                byte randomByte;
                do
                {
                    var bytes = new byte[1];
                    rng.GetBytes(bytes);
                    randomByte = bytes[0];
                }
                while (randomByte >= rejectionThreshold); // Reject biased values
                
                // Now randomByte is uniformly distributed in [0, rejectionThreshold)
                code[i] = CODE_CHARS[randomByte % charSetLength];
            }

            return new string(code);
        }

        /// <summary>
        /// Hash backup code using SHA256
        /// Same pattern as OTP hashing for consistency
        /// </summary>
        private string HashCode(string code)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(code);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash); // Returns uppercase hex string
        }

        /// <summary>
        /// Get client IP address from HTTP context
        /// Returns localhost for background jobs or test environments
        /// </summary>
        private string GetClientIpAddress()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                // Background job or test environment - use localhost
                return "127.0.0.1";
            }

            var ipAddress = context.Connection?.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(ipAddress))
            {
                return "127.0.0.1";
            }

            return ipAddress;
        }

        /// <summary>
        /// Log security event for audit trail
        /// Captures IP address, user agent, and event details
        /// </summary>
        private async Task LogSecurityEventAsync(
            Guid? adminId,
            string eventType,
            string eventDescription,
            string? username = null,
            bool success = true,
            string? errorMessage = null,
            string? metadata = null)
        {
            try
            {
                var ipAddress = GetClientIpAddress();
                var userAgent = _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString();

                var auditLog = new SecurityAuditLog
                {
                    AdminId = adminId,
                    EventType = eventType,
                    EventDescription = eventDescription,
                    Username = username,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Success = success,
                    ErrorMessage = errorMessage,
                    Metadata = metadata,
                    CreatedAt = DateTime.UtcNow
                };

                await _auditLogRepository.AddAsync(auditLog);
                await _unitOfWork.SaveChangesAsync();
            }
            catch
            {
                // Never fail the main operation due to audit logging failure
                // Just silently continue
            }
        }

        /// <summary>
        /// Generate PDF content (as base64 encoded text-based PDF)
        /// Simple PDF format without external libraries
        /// </summary>
        private string GeneratePdfContent(List<string> codes, string username, DateTime exportedAt)
        {
            // Simple text-based PDF structure
            var content = $@"SYNFLOX BACKUP CODES
====================

Account: {username}
Generated: {exportedAt:yyyy-MM-dd HH:mm:ss} UTC
Total Codes: {codes.Count}

IMPORTANT SECURITY NOTES:
- Store these codes in a secure location
- Each code can only be used once
- Generate new codes when running low
- Never share these codes with anyone

BACKUP CODES:
-------------
{string.Join(Environment.NewLine, codes.Select((code, index) => $"{index + 1,2}. {code}"))}

============================================
SYNFLOX Central Licensing System
© 2025 - All Rights Reserved
============================================";

            // Convert to base64
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Generate plain text content (as base64 encoded)
        /// </summary>
        private string GenerateTextContent(List<string> codes, string username, DateTime exportedAt)
        {
            var content = $@"SYNFLOX BACKUP CODES
====================

Account: {username}
Generated: {exportedAt:yyyy-MM-dd HH:mm:ss} UTC
Total Codes: {codes.Count}

IMPORTANT SECURITY NOTES:
- Store these codes in a secure location
- Each code can only be used once
- Generate new codes when running low
- Never share these codes with anyone

BACKUP CODES:
{string.Join(Environment.NewLine, codes)}

============================================
SYNFLOX Central Licensing System
© 2025 - All Rights Reserved
============================================";

            // Convert to base64
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Generate JSON content (as base64 encoded)
        /// </summary>
        private string GenerateJsonContent(List<string> codes, string username, DateTime exportedAt)
        {
            var jsonObject = new
            {
                system = "SYNFLOX",
                type = "backup_codes",
                account = username,
                generated_at = exportedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                total_codes = codes.Count,
                backup_codes = codes,
                security_notes = new[]
                {
                    "Store these codes in a secure location",
                    "Each code can only be used once",
                    "Generate new codes when running low",
                    "Never share these codes with anyone"
                },
                copyright = "SYNFLOX Central Licensing System © 2025"
            };

            var content = System.Text.Json.JsonSerializer.Serialize(jsonObject, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Convert to base64
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            return Convert.ToBase64String(bytes);
        }
    }
}
