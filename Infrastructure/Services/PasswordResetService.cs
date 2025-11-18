using System;
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

namespace Infrastructure.Services
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IPasswordResetTokenRepository _resetTokenRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILocalizationService _localizer;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;

        private const int OTP_LENGTH = 6;
        private const int OTP_EXPIRY_MINUTES = 15;
        private const int MAX_REQUESTS_PER_HOUR = 1000;  // DEV MODE: Very high limit for testing
        private const int MAX_OTP_ATTEMPTS = 100;  // DEV MODE: Very high limit for testing

        public PasswordResetService(
            IAdminRepository adminRepository,
            IPasswordResetTokenRepository resetTokenRepository,
            IPasswordHasher passwordHasher,
            ILocalizationService localizer,
            IUnitOfWork unitOfWork,
            IEmailService emailService)
        {
            _adminRepository = adminRepository;
            _resetTokenRepository = resetTokenRepository;
            _passwordHasher = passwordHasher;
            _localizer = localizer;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        public async Task<string> SendPasswordResetOtpAsync(ForgotPasswordRequest request, string? ipAddress = null)
        {
            // Find admin by email
            var admin = (await _adminRepository.FindAsync(a => a.Email == request.Email && !a.IsDeleted))
                .FirstOrDefault();

            if (admin == null)
            {
                // Security: Don't reveal if email exists or not
                // Return success message even if email doesn't exist
                return _localizer["Password.OtpSent"];
            }

            // Check if admin account is active
            if (!admin.IsActive)
            {
                throw new BadRequestException(_localizer["Account.Deactivated"]);
            }

            // Rate limiting: Check recent requests
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var recentRequests = await _resetTokenRepository.CountRecentRequestsAsync(admin.Id, oneHourAgo);

            if (recentRequests >= MAX_REQUESTS_PER_HOUR)
            {
                throw new RateLimitExceededException(
                    _localizer["Password.TooManyRequests"],
                    retryAfterMinutes: 60
                );
            }

            // Generate 6-digit OTP
            var otp = GenerateOtp();

            // Hash the OTP for storage
            var otpHash = HashOtp(otp);

            // Invalidate any existing tokens for this admin
            await _resetTokenRepository.InvalidateAllTokensForAdminAsync(admin.Id);

            // Create new reset token
            var resetToken = new PasswordResetToken
            {
                AdminId = admin.Id,
                TokenHash = otpHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES),
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            await _resetTokenRepository.AddAsync(resetToken);
            await _unitOfWork.SaveChangesAsync();

            // Generate magic link token for one-click reset
            var encryptionKey = DeriveEncryptionKey();
            var magicToken = GenerateMagicLinkToken(resetToken.Id, admin.Email, otp, resetToken.ExpiresAt, encryptionKey);

            // Send password reset OTP email with both manual OTP and magic link
            await _emailService.SendPasswordResetOtpEmailAsync(
                admin.Email, 
                admin.Username, 
                otp, 
                magicToken,
                OTP_EXPIRY_MINUTES, 
                ipAddress ?? "Unknown");

            return _localizer["Password.OtpSent"];
        }

        public async Task<string> VerifyResetOtpAsync(VerifyResetOtpRequest request)
        {
            // Find admin by email
            var admin = (await _adminRepository.FindAsync(a => a.Email == request.Email && !a.IsDeleted))
                .FirstOrDefault();

            if (admin == null)
            {
                throw new InvalidOtpException(_localizer["Password.InvalidOtp"]);
            }

            // Get active token
            var token = await _resetTokenRepository.GetActiveTokenByAdminIdAsync(admin.Id);

            if (token == null)
            {
                throw new InvalidOtpException(_localizer["Password.InvalidOtp"]);
            }

            // Check if token is expired
            if (token.IsExpired)
            {
                throw new InvalidOtpException(_localizer["Password.OtpExpired"]);
            }

            // Check max attempts
            if (token.FailedAttempts >= MAX_OTP_ATTEMPTS)
            {
                token.IsUsed = true;
                token.UsedAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();
                throw new TooManyAttemptsException(_localizer["Password.TooManyAttempts"]);
            }

            // Verify OTP hash
            var otpHash = HashOtp(request.OtpCode);

            if (token.TokenHash != otpHash)
            {
                // Increment failed attempts
                token.FailedAttempts++;
                await _unitOfWork.SaveChangesAsync();
                throw new InvalidOtpException(_localizer["Password.InvalidOtp"]);
            }

            // OTP is valid - don't mark as used yet (will be used in ResetPassword)
            return _localizer["Password.OtpVerified"];
        }

        public async Task<string> ResetPasswordAsync(ResetPasswordRequest request)
        {
            // Find admin by email
            var admin = (await _adminRepository.FindAsync(a => a.Email == request.Email && !a.IsDeleted))
                .FirstOrDefault();

            if (admin == null)
            {
                throw new NotFoundException(_localizer["UserNotFound"]);
            }

            // Verify OTP again (security: ensure OTP is still valid)
            var token = await _resetTokenRepository.GetActiveTokenByAdminIdAsync(admin.Id);

            if (token == null || token.IsExpired)
            {
                throw new InvalidOtpException(_localizer["Password.InvalidOtp"]);
            }

            // Verify OTP hash
            var otpHash = HashOtp(request.OtpCode);

            if (token.TokenHash != otpHash)
            {
                throw new InvalidOtpException(_localizer["Password.InvalidOtp"]);
            }

            // Hash new password
            var hashedPassword = _passwordHasher.HashPassword(request.NewPassword);

            // Update password
            admin.Password = hashedPassword;
            admin.LastPasswordChangeAt = DateTime.UtcNow;

            // Mark token as used
            token.IsUsed = true;
            token.UsedAt = DateTime.UtcNow;

            // Save changes
            await _adminRepository.UpdateAsync(admin);
            await _unitOfWork.SaveChangesAsync();

            // Send password reset success confirmation email
            await _emailService.SendPasswordResetSuccessEmailAsync(
                admin.Email, 
                admin.Username, 
                token.IpAddress ?? "Unknown");
            
            // TODO: Invalidate all refresh tokens for this admin (force re-login for security)
            
            return _localizer["Password.ResetSuccess"];
        }

        /// <summary>
        /// Generate a cryptographically secure 6-digit OTP
        /// </summary>
        private static string GenerateOtp()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var number = BitConverter.ToUInt32(bytes, 0) % 1000000;
            return number.ToString("D6");
        }

        /// <summary>
        /// Hash OTP using SHA256 for secure storage
        /// </summary>
        private static string HashOtp(string otp)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(otp));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        /// <summary>
        /// Generate magic link token for one-click password reset
        /// Token format: {TokenId}|{Email}|{ExpiresAt}|{OTP} encrypted with AES
        /// </summary>
        private static string GenerateMagicLinkToken(Guid tokenId, string email, string otp, DateTime expiresAt, byte[] encryptionKey)
        {
            var tokenData = $"{tokenId}|{email}|{expiresAt:O}|{otp}";
            
            using var aes = Aes.Create();
            aes.Key = encryptionKey;
            aes.GenerateIV();
            
            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            var plainBytes = Encoding.UTF8.GetBytes(tokenData);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            
            // Combine IV + encrypted data for transmission
            var combined = new byte[aes.IV.Length + encryptedBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
            Buffer.BlockCopy(encryptedBytes, 0, combined, aes.IV.Length, encryptedBytes.Length);
            
            return Convert.ToBase64String(combined).Replace('+', '-').Replace('/', '_').Replace("=", "");
        }

        /// <summary>
        /// Decrypt and parse magic link token
        /// </summary>
        private static (Guid tokenId, string email, DateTime expiresAt, string otp) DecryptMagicLinkToken(string token, byte[] encryptionKey)
        {
            // Restore Base64 URL-safe characters
            token = token.Replace('-', '+').Replace('_', '/');
            var padding = (4 - token.Length % 4) % 4;
            token += new string('=', padding);
            
            var combined = Convert.FromBase64String(token);
            
            using var aes = Aes.Create();
            aes.Key = encryptionKey;
            
            // Extract IV (first 16 bytes)
            var iv = new byte[16];
            Buffer.BlockCopy(combined, 0, iv, 0, 16);
            aes.IV = iv;
            
            // Extract encrypted data
            var encryptedBytes = new byte[combined.Length - 16];
            Buffer.BlockCopy(combined, 16, encryptedBytes, 0, encryptedBytes.Length);
            
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            var tokenData = Encoding.UTF8.GetString(decryptedBytes);
            
            // Parse token data
            var parts = tokenData.Split('|');
            return (
                Guid.Parse(parts[0]),
                parts[1],
                DateTime.Parse(parts[2], null, System.Globalization.DateTimeStyles.RoundtripKind),
                parts[3]
            );
        }

        public async Task<MagicLinkValidationResponse> ValidateMagicLinkAsync(ValidateMagicLinkRequest request)
        {
            try
            {
                // Get encryption key from configuration (same as ID encryption)
                var encryptionKey = DeriveEncryptionKey();
                
                // Decrypt and parse token
                var (tokenId, email, expiresAt, otp) = DecryptMagicLinkToken(request.Token, encryptionKey);
                
                // Check if token expired
                if (DateTime.UtcNow > expiresAt)
                {
                    throw new InvalidOtpException(_localizer["Password.OtpExpired"]);
                }
                
                // Find admin by email
                var admin = (await _adminRepository.FindAsync(a => a.Email == email && !a.IsDeleted))
                    .FirstOrDefault();
                
                if (admin == null)
                {
                    throw new NotFoundException(_localizer["UserNotFound"]);
                }
                
                // Get token from database
                var token = await _resetTokenRepository.GetByIdAsync(tokenId, null);
                
                if (token == null || token.IsUsed || token.IsExpired)
                {
                    throw new InvalidOtpException(_localizer["Password.InvalidOtp"]);
                }
                
                // Verify token belongs to this admin
                if (token.AdminId != admin.Id)
                {
                    throw new InvalidOtpException(_localizer["Password.InvalidOtp"]);
                }
                
                // Return validation response with OTP for auto-fill
                var remainingMinutes = (int)(expiresAt - DateTime.UtcNow).TotalMinutes;
                
                return new MagicLinkValidationResponse
                {
                    Email = email,
                    OtpCode = otp,
                    IsValid = true,
                    ExpiryMinutes = remainingMinutes
                };
            }
            catch (Exception ex) when (ex is not InvalidOtpException && ex is not NotFoundException)
            {
                // Invalid token format or decryption failed
                throw new InvalidOtpException(_localizer["Password.InvalidOtp"]);
            }
        }

        /// <summary>
        /// Derive encryption key from configuration (should match ID encryption settings)
        /// </summary>
        private byte[] DeriveEncryptionKey()
        {
            // Use a consistent key - in production, get from IOptions<EncryptionSettings>
            var keyString = "SYNFLOX_MAGIC_LINK_ENCRYPTION_KEY_32BYTES";
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
        }
    }
}
