using System;
using System.Collections.Generic;
using System.IO;
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
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

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
        private readonly IFileHostExportService _fileHostExportService;

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
            IEmailService emailService,
            IFileHostExportService fileHostExportService)
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
            _fileHostExportService = fileHostExportService;
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

        public async Task<bool> VerifyBackupCodeForPasswordResetAsync(string username, string backupCode)
        {
            // SECURITY: Validate input
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(backupCode))
            {
                return false; // Don't leak info about what was wrong
            }

            try
            {
                // Hash the backup code
                var codeHash = HashCode(backupCode.ToUpper());

                // Find admin by username or email (case-insensitive)
                var usernameLower = username.ToLower();
                var admin = (await _adminRepository.FindAsync(a =>
                    ((a.Username.ToLower() == usernameLower || a.Email.ToLower() == usernameLower) && !a.IsDeleted)))
                    .FirstOrDefault();

                if (admin == null || !admin.IsActive || !admin.IsTwoFactorEnabled)
                {
                    // SECURITY: Don't reveal which condition failed
                    await Task.Delay(100); // Prevent timing attacks
                    return false;
                }

                // Find matching backup code
                var code = await _backupCodeRepository.GetByAdminAndHashAsync(admin.Id, codeHash);

                if (code == null || code.IsUsed || code.IsExpired)
                {
                    // AUDIT: Log failed attempt
                    await LogSecurityEventAsync(
                        adminId: admin.Id,
                        eventType: "BackupCodeVerificationFailed_PasswordReset",
                        eventDescription: "Invalid backup code for password reset",
                        username: username,
                        success: false,
                        errorMessage: "Invalid, used, or expired code"
                    );

                    await Task.Delay(100); // Prevent timing attacks
                    return false;
                }

                // AUDIT: Log successful verification (code NOT consumed yet)
                await LogSecurityEventAsync(
                    adminId: admin.Id,
                    eventType: "BackupCodeVerified_PasswordReset",
                    eventDescription: "Backup code verified for password reset (not consumed)",
                    username: username,
                    success: true
                );

                return true; // Code is valid!
            }
            catch
            {
                // SECURITY: Catch all exceptions and return false
                await Task.Delay(100);
                return false;
            }
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
            
            // Normalize format aliases
            if (normalizedFormat == "text") normalizedFormat = "txt";
            if (normalizedFormat == "doc") normalizedFormat = "docx"; // Treat DOC as DOCX
            
            byte[] fileBytes;
            string contentType;
            string fileName;

            // Generate content based on format
            switch (normalizedFormat)
            {
                case "pdf":
                    fileBytes = GeneratePdfContent(request.Codes, admin.Username, exportedAt);
                    contentType = "application/pdf";
                    fileName = $"SYNFLOX_BackupCodes_{admin.Username}_{exportedAt:yyyyMMdd_HHmmss}.pdf";
                    break;

                case "txt":
                    fileBytes = GenerateTextContent(request.Codes, admin.Username, exportedAt);
                    contentType = "text/plain";
                    fileName = $"SYNFLOX_BackupCodes_{admin.Username}_{exportedAt:yyyyMMdd_HHmmss}.txt";
                    break;

                case "docx":
                    fileBytes = GenerateDocxContent(request.Codes, admin.Username, exportedAt);
                    contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                    fileName = $"SYNFLOX_BackupCodes_{admin.Username}_{exportedAt:yyyyMMdd_HHmmss}.docx";
                    break;

                case "json":
                    fileBytes = GenerateJsonContent(request.Codes, admin.Username, exportedAt);
                    contentType = "application/json";
                    fileName = $"SYNFLOX_BackupCodes_{admin.Username}_{exportedAt:yyyyMMdd_HHmmss}.json";
                    break;

                default:
                    throw new BadRequestException($"Unsupported export format: {request.Format}. Supported formats: json, txt, pdf, docx, doc");
            }

            // Save to FileHost and get download URL (auto-deletes after 30 minutes)
            var fileHostResponse = await _fileHostExportService.SaveExportFileAsync(
                fileBytes,
                fileName,
                contentType,
                normalizedFormat
            );

            // AUDIT: Log backup codes export
            await LogSecurityEventAsync(
                adminId: adminId,
                eventType: "BackupCodesExported",
                eventDescription: $"Backup codes exported in {normalizedFormat.ToUpper()} format",
                success: true,
                metadata: $"{{\"format\":\"{normalizedFormat}\",\"codesCount\":{request.Codes.Count},\"fileId\":\"{fileHostResponse.FileId}\"}}"
            );

            return new ExportBackupCodesResponse
            {
                FileContent = fileHostResponse.DownloadUrl, // Changed: Now returns download URL
                ContentType = contentType,
                FileName = fileName,
                UnusedCodesCount = request.Codes.Count,
                Format = normalizedFormat.ToUpper(),
                ExportedAt = exportedAt,
                Message = $"Backup codes exported successfully. Download URL expires in {fileHostResponse.MinutesUntilExpiry} minutes."
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
        /// Generate REAL PDF content using QuestPDF library
        /// Returns actual PDF file bytes
        /// </summary>
        /// <summary>
        /// Generate STUNNING, PROFESSIONAL, CREATIVE PDF with SYNFLOX Branding
        /// Premium design with visual elements and modern layout
        /// </summary>
        private byte[] GeneratePdfContent(List<string> codes, string username, DateTime exportedAt)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.PageColor(Colors.White);
                    
                    // STUNNING GRADIENT HEADER with BRANDING
                    page.Header().Height(140).Column(header =>
                    {
                        // Purple gradient background effect
                        header.Item().Height(140).Layers(layers =>
                        {
                            // Base purple
                            layers.Layer().Background(Colors.Purple.Darken2);
                            // Gradient overlay effect
                            layers.PrimaryLayer().Padding(25).Column(content =>
                            {
                                // Logo/Brand area with icon
                                content.Item().Row(row =>
                                {
                                    row.RelativeItem().Column(brand =>
                                    {
                                        brand.Item().Text("🔒 SYNFLOX")
                                            .FontSize(32).Bold().FontColor(Colors.White);
                                        brand.Item().PaddingTop(3).Text("Central Licensing System")
                                            .FontSize(11).FontColor(Colors.Grey.Lighten3);
                                    });
                                    row.ConstantItem(80).AlignRight().Column(badge =>
                                    {
                                        badge.Item().Background(Colors.Orange.Medium).Padding(8).AlignCenter()
                                            .Text("✓ SECURE").FontSize(9).Bold().FontColor(Colors.White);
                                    });
                                });
                                
                                content.Item().PaddingTop(15).AlignCenter().Column(title =>
                                {
                                    title.Item().Text("TWO-FACTOR AUTHENTICATION")
                                        .FontSize(14).SemiBold().FontColor(Colors.Grey.Lighten4);
                                    title.Item().PaddingTop(5).Text("BACKUP RECOVERY CODES")
                                        .FontSize(28).Bold().FontColor(Colors.White);
                                });
                            });
                        });
                    });

                    // MODERN CONTENT with CREATIVE LAYOUT
                    page.Content().Padding(25).Column(column =>
                    {
                        // ACCOUNT INFO CARD - Modern Card Design
                        column.Item().Border(2).BorderColor(Colors.Purple.Lighten2)
                            .Background(Colors.Purple.Lighten5).Padding(20).Column(infoCard =>
                        {
                            infoCard.Item().Text("ⓘ Account Information").FontSize(12).SemiBold().FontColor(Colors.Purple.Darken2);
                            infoCard.Item().PaddingTop(10).PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Purple.Lighten3);
                            
                            infoCard.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("👤 Account").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    col.Item().PaddingTop(3).Text(username).FontSize(13).Bold().FontColor(Colors.Purple.Darken3);
                                });
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("📅 Generated").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    col.Item().PaddingTop(3).Text(exportedAt.ToString("MMM dd, yyyy HH:mm UTC"))
                                        .FontSize(13).Bold().FontColor(Colors.Purple.Darken3);
                                });
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("🔢 Total Codes").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    col.Item().PaddingTop(3).Text($"{codes.Count} codes")
                                        .FontSize(13).Bold().FontColor(Colors.Purple.Darken3);
                                });
                            });
                        });

                        // CRITICAL SECURITY ALERT - Eye-catching Design
                        column.Item().PaddingTop(20).Border(3).BorderColor(Colors.Red.Darken1)
                            .Background(Colors.Red.Lighten4).Padding(18).Column(alert =>
                        {
                            alert.Item().Row(row =>
                            {
                                row.ConstantItem(40).AlignMiddle().Text("⚠️").FontSize(28);
                                row.RelativeItem().PaddingLeft(10).AlignMiddle().Text("CRITICAL SECURITY INSTRUCTIONS")
                                    .FontSize(16).Bold().FontColor(Colors.Red.Darken3);
                            });
                            
                            alert.Item().PaddingTop(12).PaddingBottom(8).LineHorizontal(2).LineColor(Colors.Red.Lighten2);
                            
                            alert.Item().PaddingTop(8).Column(instructions =>
                            {
                                instructions.Item().PaddingBottom(6).Row(row =>
                                {
                                    row.ConstantItem(25).Text("✔").FontSize(12).FontColor(Colors.Red.Darken2);
                                    row.RelativeItem().Text("Store these codes in a SECURE, ENCRYPTED location")
                                        .FontSize(11).FontColor(Colors.Red.Darken3);
                                });
                                instructions.Item().PaddingBottom(6).Row(row =>
                                {
                                    row.ConstantItem(25).Text("✔").FontSize(12).FontColor(Colors.Red.Darken2);
                                    row.RelativeItem().Text("Each code can ONLY be used ONCE - treat like passwords")
                                        .FontSize(11).FontColor(Colors.Red.Darken3);
                                });
                                instructions.Item().PaddingBottom(6).Row(row =>
                                {
                                    row.ConstantItem(25).Text("✔").FontSize(12).FontColor(Colors.Red.Darken2);
                                    row.RelativeItem().Text("NEVER share codes with anyone - including SYNFLOX staff")
                                        .FontSize(11).FontColor(Colors.Red.Darken3);
                                });
                                instructions.Item().PaddingBottom(6).Row(row =>
                                {
                                    row.ConstantItem(25).Text("✔").FontSize(12).FontColor(Colors.Red.Darken2);
                                    row.RelativeItem().Text("Generate new codes when you have 3 or fewer remaining")
                                        .FontSize(11).FontColor(Colors.Red.Darken3);
                                });
                                instructions.Item().Row(row =>
                                {
                                    row.ConstantItem(25).Text("✔").FontSize(12).FontColor(Colors.Red.Darken2);
                                    row.RelativeItem().Text("Print this document and store offline in a safe place")
                                        .FontSize(11).FontColor(Colors.Red.Darken3);
                                });
                            });
                        });

                        // BACKUP CODES - Premium Table Design
                        column.Item().PaddingTop(25).Column(codesSection =>
                        {
                            codesSection.Item().Row(row =>
                            {
                                row.RelativeItem().Text("🔐 YOUR BACKUP CODES")
                                    .FontSize(18).Bold().FontColor(Colors.Purple.Darken3);
                                row.ConstantItem(100).AlignRight().Background(Colors.Green.Lighten4)
                                    .Padding(6).AlignCenter().Text("✓ ACTIVE").FontSize(9).Bold().FontColor(Colors.Green.Darken2);
                            });
                            
                            codesSection.Item().PaddingTop(3).Text("Use these codes to recover access if you lose your 2FA device")
                                .FontSize(10).Italic().FontColor(Colors.Grey.Darken1);
                        });
                        
                        column.Item().PaddingTop(15).Border(2).BorderColor(Colors.Purple.Medium).Column(tableWrapper =>
                        {
                            tableWrapper.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(60);
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(100);
                                });

                                // PREMIUM HEADER
                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Purple.Darken2).Padding(12)
                                        .Text("#").FontSize(12).Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Purple.Darken2).Padding(12)
                                        .Text("BACKUP CODE").FontSize(12).Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Purple.Darken2).Padding(12).AlignCenter()
                                        .Text("STATUS").FontSize(12).Bold().FontColor(Colors.White);
                                });

                                // CODES with ALTERNATING COLORS and VISUAL APPEAL
                                for (int i = 0; i < codes.Count; i++)
                                {
                                    var bgColor = i % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;
                                    var numberColor = i % 2 == 0 ? Colors.Purple.Medium : Colors.Purple.Darken1;
                                    
                                    table.Cell().Background(bgColor).Border(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(12).AlignCenter().Text((i + 1).ToString())
                                        .FontSize(13).SemiBold().FontColor(numberColor);
                                    
                                    table.Cell().Background(bgColor).Border(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(12).Text(codes[i])
                                        .FontSize(16).FontFamily(QuestPDF.Helpers.Fonts.Courier).Bold()
                                        .FontColor(Colors.Black);
                                    
                                    table.Cell().Background(bgColor).Border(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(12).AlignCenter().Background(Colors.Green.Lighten3)
                                        .Padding(6).AlignCenter().Text("✓ Valid")
                                        .FontSize(9).SemiBold().FontColor(Colors.Green.Darken2);
                                }
                            });
                        });

                        // USAGE INSTRUCTIONS - Helpful Guide
                        column.Item().PaddingTop(20).Background(Colors.Blue.Lighten5).Border(1)
                            .BorderColor(Colors.Blue.Lighten2).Padding(15).Column(usage =>
                        {
                            usage.Item().Text("📝 How to Use These Codes").FontSize(13).SemiBold().FontColor(Colors.Blue.Darken2);
                            usage.Item().PaddingTop(10).Text("1. If you lose access to your 2FA device, use a backup code instead")
                                .FontSize(10).FontColor(Colors.Blue.Darken3);
                            usage.Item().PaddingTop(4).Text("2. Enter ONE code when prompted during login")
                                .FontSize(10).FontColor(Colors.Blue.Darken3);
                            usage.Item().PaddingTop(4).Text("3. Each code is single-use only and cannot be reused")
                                .FontSize(10).FontColor(Colors.Blue.Darken3);
                            usage.Item().PaddingTop(4).Text("4. Generate new codes before running out (3 codes remaining = regenerate)")
                                .FontSize(10).FontColor(Colors.Blue.Darken3);
                        });
                    });

                    // PREMIUM FOOTER with BRANDING
                    page.Footer().Height(60).Column(footer =>
                    {
                        footer.Item().LineHorizontal(2).LineColor(Colors.Purple.Medium);
                        footer.Item().PaddingTop(10).Background(Colors.Grey.Lighten4).Padding(12).Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text("SYNFLOX Central Licensing System")
                                    .FontSize(11).Bold().FontColor(Colors.Purple.Darken2);
                                left.Item().Text("© 2025 SYNFLOX - All Rights Reserved | Confidential Document")
                                    .FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                            row.ConstantItem(120).AlignRight().AlignMiddle().Text($"Page 1 of 1")
                                .FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        /// <summary>
        /// Generate plain text content
        /// Returns raw bytes for direct file writing
        /// </summary>
        private byte[] GenerateTextContent(List<string> codes, string username, DateTime exportedAt)
        {
            var content = new StringBuilder();
            content.AppendLine("SYNFLOX BACKUP CODES");
            content.AppendLine("====================");
            content.AppendLine();
            content.AppendLine($"Account: {username}");
            content.AppendLine($"Generated: {exportedAt:yyyy-MM-dd HH:mm:ss} UTC");
            content.AppendLine($"Total Codes: {codes.Count}");
            content.AppendLine();
            content.AppendLine("IMPORTANT SECURITY NOTES:");
            content.AppendLine("- Store these codes in a secure location");
            content.AppendLine("- Each code can only be used ONCE for 2FA login");
            content.AppendLine("- Generate new codes when running low");
            content.AppendLine("- NEVER share these codes with anyone");
            content.AppendLine("- Keep this file encrypted and backed up securely");
            content.AppendLine();
            content.AppendLine("YOUR BACKUP CODES:");
            content.AppendLine("------------------");
            
            foreach (var code in codes)
            {
                content.AppendLine(code);
            }
            
            content.AppendLine();
            content.AppendLine("============================================");
            content.AppendLine("SYNFLOX Central Licensing System");
            content.AppendLine("© 2025 - All Rights Reserved");
            content.AppendLine("============================================");

            return System.Text.Encoding.UTF8.GetBytes(content.ToString());
        }

        /// <summary>
        /// Generate JSON content
        /// Returns raw bytes for direct file writing
        /// </summary>
        private byte[] GenerateJsonContent(List<string> codes, string username, DateTime exportedAt)
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
                    "Each code can only be used ONCE for 2FA login",
                    "Generate new codes when running low",
                    "NEVER share these codes with anyone",
                    "Keep this file encrypted and backed up securely"
                },
                copyright = "SYNFLOX Central Licensing System © 2025"
            };

            var content = System.Text.Json.JsonSerializer.Serialize(jsonObject, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            return System.Text.Encoding.UTF8.GetBytes(content);
        }

        /// <summary>
        /// Generate STUNNING, PROFESSIONAL, CREATIVE DOCX with SYNFLOX Branding
        /// Premium Word document with enhanced design and visual appeal
        /// </summary>
        private byte[] GenerateDocxContent(List<string> codes, string username, DateTime exportedAt)
        {
            using var stream = new MemoryStream();
            
            // Create Word document
            using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                var body = mainPart.Document.AppendChild(new Body());

                // BRANDED HEADER - Premium Title with Icon
                AddStyledParagraph(body, "🔒 SYNFLOX", true, "48", "7B68EE", "center");
                AddStyledParagraph(body, "Central Licensing System", false, "20", "808080", "center");
                
                // Main Title
                body.AppendChild(new Paragraph()); // Spacing
                AddStyledParagraph(body, "TWO-FACTOR AUTHENTICATION", true, "22", "9370DB", "center");
                AddStyledParagraph(body, "BACKUP RECOVERY CODES", true, "36", "7B68EE", "center");
                
                // Divider
                body.AppendChild(new Paragraph());
                body.AppendChild(new Paragraph());

                // ACCOUNT INFO BOX - Creative Card Design
                AddStyledParagraph(body, "ⓘ Account Information", true, "22", "7B68EE");
                AddStyledParagraph(body, "_______________________________________________________________", false, "18", "D8BFD8");
                body.AppendChild(new Paragraph());
                
                AddStyledParagraph(body, $"👤  Account:  {username}", true, "20", "663399");
                AddStyledParagraph(body, $"📅  Generated:  {exportedAt:MMM dd, yyyy HH:mm UTC}", true, "20", "663399");
                AddStyledParagraph(body, $"🔢  Total Codes:  {codes.Count} recovery codes", true, "20", "663399");
                
                body.AppendChild(new Paragraph());
                body.AppendChild(new Paragraph());

                // CRITICAL SECURITY ALERT - Eye-catching Box
                AddStyledParagraph(body, "⚠️  CRITICAL SECURITY INSTRUCTIONS", true, "28", "DC143C");
                AddStyledParagraph(body, "_______________________________________________________________", false, "18", "FFB6C1");
                body.AppendChild(new Paragraph());
                
                AddStyledParagraph(body, "✔  Store these codes in a SECURE, ENCRYPTED location", false, "20", "8B0000");
                AddStyledParagraph(body, "✔  Each code can ONLY be used ONCE - treat like passwords", false, "20", "8B0000");
                AddStyledParagraph(body, "✔  NEVER share codes with anyone - including SYNFLOX staff", false, "20", "8B0000");
                AddStyledParagraph(body, "✔  Generate new codes when you have 3 or fewer remaining", false, "20", "8B0000");
                AddStyledParagraph(body, "✔  Print this document and store offline in a safe place", false, "20", "8B0000");

                body.AppendChild(new Paragraph());
                body.AppendChild(new Paragraph());

                // CODES SECTION HEADER - Premium Design
                AddStyledParagraph(body, "🔐 YOUR BACKUP CODES", true, "30", "7B68EE");
                AddStyledParagraph(body, "Use these codes to recover access if you lose your 2FA device", false, "18", "808080", "left", true);
                
                body.AppendChild(new Paragraph());

                // PREMIUM TABLE with Enhanced Styling
                var table = body.AppendChild(new Table());
                var tableProps = table.AppendChild(new TableProperties());
                
                // Double border for premium look
                tableProps.AppendChild(new TableBorders(
                    new TopBorder() { Val = BorderValues.Double, Size = 12, Color = "7B68EE" },
                    new BottomBorder() { Val = BorderValues.Double, Size = 12, Color = "7B68EE" },
                    new LeftBorder() { Val = BorderValues.Double, Size = 12, Color = "7B68EE" },
                    new RightBorder() { Val = BorderValues.Double, Size = 12, Color = "7B68EE" },
                    new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 8, Color = "D8BFD8" },
                    new InsideVerticalBorder() { Val = BorderValues.Single, Size = 8, Color = "D8BFD8" }
                ));

                // Premium Table Header with Purple Background
                var headerRow = table.AppendChild(new TableRow());
                AddPremiumTableCell(headerRow, "#", true, "7B68EE", "FFFFFF");
                AddPremiumTableCell(headerRow, "BACKUP CODE", true, "7B68EE", "FFFFFF");
                AddPremiumTableCell(headerRow, "STATUS", true, "7B68EE", "FFFFFF");

                // Code Rows with Alternating Colors
                for (int i = 0; i < codes.Count; i++)
                {
                    var codeRow = table.AppendChild(new TableRow());
                    var bgColor = i % 2 == 0 ? "F5F5F5" : "FFFFFF";
                    
                    AddPremiumTableCell(codeRow, (i + 1).ToString(), false, bgColor, "663399", "20", false);
                    AddPremiumTableCell(codeRow, codes[i], false, bgColor, "000000", "26", true, "Courier New");
                    AddPremiumTableCell(codeRow, "✓ Valid", false, "90EE90", "006400", "16", true);
                }

                body.AppendChild(new Paragraph());
                body.AppendChild(new Paragraph());

                // USAGE INSTRUCTIONS - Helpful Blue Box
                AddStyledParagraph(body, "📝 How to Use These Codes", true, "24", "4169E1");
                AddStyledParagraph(body, "_______________________________________________________________", false, "18", "ADD8E6");
                body.AppendChild(new Paragraph());
                
                AddStyledParagraph(body, "1. If you lose access to your 2FA device, use a backup code instead", false, "18", "000080");
                AddStyledParagraph(body, "2. Enter ONE code when prompted during login", false, "18", "000080");
                AddStyledParagraph(body, "3. Each code is single-use only and cannot be reused", false, "18", "000080");
                AddStyledParagraph(body, "4. Generate new codes before running out (3 remaining = regenerate)", false, "18", "000080");

                body.AppendChild(new Paragraph());
                body.AppendChild(new Paragraph());

                // PREMIUM FOOTER
                AddStyledParagraph(body, "_______________________________________________________________", false, "18", "7B68EE");
                AddStyledParagraph(body, "SYNFLOX Central Licensing System", true, "22", "7B68EE", "center");
                AddStyledParagraph(body, "© 2025 SYNFLOX - All Rights Reserved | Confidential Document", false, "16", "808080", "center");

                mainPart.Document.Save();
            }

            return stream.ToArray();
        }

        private void AddStyledParagraph(Body body, string text, bool bold = false, string fontSize = "20", string color = "000000", string alignment = "left", bool italic = false)
        {
            var para = body.AppendChild(new Paragraph());
            
            // Set alignment
            if (alignment == "center")
            {
                para.AppendChild(new ParagraphProperties(new Justification() { Val = JustificationValues.Center }));
            }
            else if (alignment == "right")
            {
                para.AppendChild(new ParagraphProperties(new Justification() { Val = JustificationValues.Right }));
            }
            
            var run = para.AppendChild(new Run());
            run.AppendChild(new Text(text));
            var props = run.AppendChild(new RunProperties());
            
            if (bold) props.AppendChild(new Bold());
            if (italic) props.AppendChild(new Italic());
            props.AppendChild(new FontSize() { Val = fontSize });
            props.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Color() { Val = color });
        }

        private void AddPremiumTableCell(TableRow row, string text, bool isHeader = false, string bgColor = "FFFFFF", string textColor = "000000", string fontSize = "20", bool bold = false, string fontFamily = "Calibri")
        {
            var cell = row.AppendChild(new TableCell());
            
            // Cell properties with background color
            var cellProps = cell.AppendChild(new TableCellProperties());
            cellProps.AppendChild(new Shading() { Val = ShadingPatternValues.Clear, Fill = bgColor });
            cellProps.AppendChild(new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center });
            
            var para = cell.AppendChild(new Paragraph());
            
            // Center align header cells
            if (isHeader)
            {
                para.AppendChild(new ParagraphProperties(new Justification() { Val = JustificationValues.Center }));
            }
            
            var run = para.AppendChild(new Run());
            run.AppendChild(new Text(text));
            var props = run.AppendChild(new RunProperties());
            
            if (isHeader || bold) props.AppendChild(new Bold());
            props.AppendChild(new FontSize() { Val = fontSize });
            props.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Color() { Val = textColor });
            props.AppendChild(new RunFonts() { Ascii = fontFamily });
        }

        private void AddFormattedParagraph(Body body, string text, bool bold = false, string fontSize = "20")
        {
            var para = body.AppendChild(new Paragraph());
            var run = para.AppendChild(new Run());
            run.AppendChild(new Text(text));
            var props = run.AppendChild(new RunProperties());
            if (bold) props.AppendChild(new Bold());
            props.AppendChild(new FontSize() { Val = fontSize });
        }

        private void AddTableCell(TableRow row, string text, bool isHeader = false, string fontFamily = "Calibri", string fontSize = "20", bool bold = false)
        {
            var cell = row.AppendChild(new TableCell());
            var para = cell.AppendChild(new Paragraph());
            var run = para.AppendChild(new Run());
            run.AppendChild(new Text(text));
            var props = run.AppendChild(new RunProperties());
            
            if (isHeader || bold) props.AppendChild(new Bold());
            props.AppendChild(new FontSize() { Val = fontSize });
            props.AppendChild(new RunFonts() { Ascii = fontFamily });
            
            if (isHeader)
            {
                var cellProps = cell.AppendChild(new TableCellProperties());
                cellProps.AppendChild(new Shading() { Val = ShadingPatternValues.Clear, Fill = "D3D3D3" });
            }
        }
    }
}
