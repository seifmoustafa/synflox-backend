using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Admin;
using Application.DTOs.Authentication;
using Application.Services;
using AutoMapper;
using Domain.Entities.Authentication;
using Domain.Entities.Common;
using Domain.Exceptions;
using Domain.Interfaces;
using Infrastructure.Authentication;
using OtpNet;

namespace Infrastructure.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IRefreshTokenRepository _refreshTokenRepo;
        private readonly IBaseRepository<Guid, AdminType> _adminTypeRepository;
        private readonly IMapper _mapper;
        private readonly ILocalizationService _localizer;
        private readonly IUnitOfWork _unitOfWork;

        public AuthenticationService(
            IAdminRepository adminRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            IRefreshTokenRepository refreshTokenRepo,
            IBaseRepository<Guid, AdminType> adminTypeRepository,
            IMapper mapper,
            ILocalizationService localizer,
            IUnitOfWork unitOfWork)
        {
            _adminRepository = adminRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _refreshTokenRepo = refreshTokenRepo;
            _adminTypeRepository = adminTypeRepository;
            _mapper = mapper;
            _localizer = localizer;
            _unitOfWork = unitOfWork;
        }

        public async Task<AdminDto> RegisterAdminAsync(CreateAdminDto request)
        {
            var admin = _mapper.Map<Admin>(request);
            admin.Password = _passwordHasher.HashPassword(request.Password);

            if (admin.AdminTypeId == Guid.Empty)
            {
                var adminType = (await _adminTypeRepository.FindAsync(u => u.AdminTypeName == "Admin")).FirstOrDefault();
                if (adminType != null)
                {
                    admin.AdminTypeId = adminType.Id;
                }
            }

            await _adminRepository.AddAsync(admin);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<AdminDto>(admin);
        }

        public async Task<AuthenticationResponse> AdminAuthenticationAsync(string username, string password)
        {
            var admin = await _adminRepository.GetByUserNameAsync(username);
            if (admin == null)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["InvalidAdminCredentials"]
                };
            }

            // Security: Check if admin is active and not deleted
            if (!admin.IsActive || admin.IsDeleted)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["Account.Deactivated"]
                };
            }

            bool checkPassword = _passwordHasher.VerifyPassword(password, admin.Password);
            if (!checkPassword)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["InvalidAdminCredentials"]
                };
            }

            // Check if 2FA is enabled
            if (admin.IsTwoFactorEnabled)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    Requires2FA = true,
                    Message = _localizer["2FA.Required"],
                    ErrorMessage = _localizer["2FA.Required"]
                };
            }

            admin.AdminType = await _adminTypeRepository.GetByIdAsync(admin.AdminTypeId, null)
                ?? throw new NotFoundException(_localizer["AdminTypeNotFound"]);

            // Update login tracking
            admin.LastLoginAt = DateTime.UtcNow;
            admin.LoginCount++;
            await _adminRepository.UpdateAsync(admin);
            await _unitOfWork.SaveChangesAsync();

            return await GenerateTokensAsync(
                admin,
                _jwtTokenGenerator.GenerateToken,
                _jwtTokenGenerator.GenerateRefreshToken);
        }

        public async Task<AuthenticationResponse> Verify2FAAsync(string username, string password, string twoFactorCode)
        {
            var admin = await _adminRepository.GetByUserNameAsync(username);
            if (admin == null)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["InvalidAdminCredentials"]
                };
            }

            // Security: Check if admin is active and not deleted
            if (!admin.IsActive || admin.IsDeleted)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["Account.Deactivated"]
                };
            }

            // Verify password
            bool checkPassword = _passwordHasher.VerifyPassword(password, admin.Password);
            if (!checkPassword)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["InvalidAdminCredentials"]
                };
            }

            // Check if 2FA is enabled
            if (!admin.IsTwoFactorEnabled || string.IsNullOrEmpty(admin.TwoFactorSecret))
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["2FA.NotEnabled"]
                };
            }

            // Verify TOTP code first (before checking reuse)
            var secretBytes = Base32Encoding.ToBytes(admin.TwoFactorSecret);
            var totp = new Totp(secretBytes);
            var isValid = totp.VerifyTotp(twoFactorCode, out _, new VerificationWindow(2, 2));

            if (!isValid)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["2FA.InvalidCode"]
                };
            }

            // ⭐ SECURITY: Prevent SAME code reuse within TOTP window (60 seconds)
            // Hash the code for defense in depth (even if DB compromised, codes can't be derived)
            var hashedCode = HashTwoFactorCode(twoFactorCode);
            
            // Inline cleanup: Clear expired tracking data (older than 60 seconds)
            if (admin.LastTwoFactorCodeUsedAt.HasValue)
            {
                var timeSinceLastUse = DateTime.UtcNow - admin.LastTwoFactorCodeUsedAt.Value;
                
                if (timeSinceLastUse.TotalSeconds >= 60)
                {
                    // Expired: Clear old data (no longer needed)
                    admin.LastTwoFactorCodeUsed = null;
                    admin.LastTwoFactorCodeUsedAt = null;
                }
                else if (!string.IsNullOrEmpty(admin.LastTwoFactorCodeUsed))
                {
                    // Within 60s: Check if SAME code was used (compare hashes)
                    if (admin.LastTwoFactorCodeUsed == hashedCode)
                    {
                        return new AuthenticationResponse
                        {
                            Success = false,
                            ErrorMessage = _localizer["2FA.CodeAlreadyUsed"]
                        };
                    }
                }
            }

            // Track code usage to prevent reuse (store HASHED code + timestamp)
            admin.LastTwoFactorCodeUsed = hashedCode; // SHA256 hash, not plain text
            admin.LastTwoFactorCodeUsedAt = DateTime.UtcNow;

            admin.AdminType = await _adminTypeRepository.GetByIdAsync(admin.AdminTypeId, null)
                ?? throw new NotFoundException(_localizer["AdminTypeNotFound"]);

            // Update login tracking
            admin.LastLoginAt = DateTime.UtcNow;
            admin.LoginCount++;
            await _adminRepository.UpdateAsync(admin);
            await _unitOfWork.SaveChangesAsync();

            return await GenerateTokensAsync(
                admin,
                _jwtTokenGenerator.GenerateToken,
                _jwtTokenGenerator.GenerateRefreshToken);
        }

        public async Task<AuthenticationResponse> RegenerateAccessToken(RefreshTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["InvalidRefreshToken"]
                };
            }

            // Find refresh token in database
            var refreshedToken = (await _refreshTokenRepo.FindAsync(rt => 
                rt.Token == request.RefreshToken && 
                rt.IsActive && 
                !rt.IsDeleted && 
                !rt.IsRevoked)).FirstOrDefault();

            if (refreshedToken == null || refreshedToken.IsExpired)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["InvalidOrExpiredRefreshToken"]
                };
            }

            // Get admin associated with this refresh token
            if (refreshedToken.AdminId == null)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["InvalidRefreshToken"]
                };
            }

            var admin = await _adminRepository.GetByIdAsync(refreshedToken.AdminId.Value, null);
            if (admin == null)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["UserNotFound"]
                };
            }

            admin.AdminType = await _adminTypeRepository.GetByIdAsync(admin.AdminTypeId, null)
                ?? throw new NotFoundException(_localizer["AdminTypeNotFound"]);

            // Revoke old refresh token (rotation - old token is replaced with new one)
            refreshedToken.IsRevoked = true;
            refreshedToken.RevokedAt = DateTime.UtcNow;
            refreshedToken.RevokedReason = "TokenRotation";
            await _refreshTokenRepo.UpdateAsync(refreshedToken);

            // Generate new tokens
            var token = _jwtTokenGenerator.GenerateToken(admin);
            var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken(admin);
            await _refreshTokenRepo.AddAsync(newRefreshToken);
            await _unitOfWork.SaveChangesAsync();

            return new AuthenticationResponse
            {
                AccessToken = token,
                Success = true,
                RefreshToken = newRefreshToken.Token
            };
        }

        public async Task Logout(Guid adminId)
        {
            var refreshedToken = await _refreshTokenRepo.GetByAdminId(adminId);
            if (refreshedToken != null)
            {
                refreshedToken.IsRevoked = true;
                refreshedToken.RevokedAt = DateTime.UtcNow;
                refreshedToken.RevokedReason = "Logout";
                await _refreshTokenRepo.UpdateAsync(refreshedToken);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task ChangeAdminPasswordAsync(ChangePasswordByIdRequest request)
        {
            // Use AutoMapper to decrypt the ID
            var decryptedId = _mapper.Map<Guid>(request);
            
            var admin = await _adminRepository.GetByIdAsync(decryptedId, null);
            if (admin == null)
            {
                throw new NotFoundException(_localizer["UserNotFound"]);
            }

            var adminType = (await _adminTypeRepository.FindAsync(u => u.AdminTypeName == "Admin")).FirstOrDefault();
            if (adminType == null || admin.AdminTypeId != adminType.Id)
            {
                throw new BadRequestException(_localizer["TargetNotAdmin"]);
            }

            admin.Password = _passwordHasher.HashPassword(request.NewPassword);
            await _adminRepository.UpdateAsync(admin);
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task<AuthenticationResponse> GenerateTokensAsync(
            Admin admin,
            Func<Admin, string> tokenGenerator,
            Func<Admin, RefreshToken> refreshTokenGenerator)
        {
            var token = tokenGenerator(admin);
            var refreshToken = refreshTokenGenerator(admin);
            await _refreshTokenRepo.AddAsync(refreshToken);
            await _unitOfWork.SaveChangesAsync();
            return new AuthenticationResponse
            {
                AccessToken = token,
                Success = true,
                RefreshToken = refreshToken.Token
            };
        }

        public async Task<Check2FAStatusResponse> Check2FAStatusAsync(string email)
        {
            // Find admin by email
            var admin = (await _adminRepository.FindAsync(a => a.Email == email && !a.IsDeleted))
                .FirstOrDefault();

            // Don't leak user existence for security
            // Always return EmailExists = true, but set Has2FA based on actual status
            if (admin == null)
            {
                return new Check2FAStatusResponse
                {
                    Has2FA = false,
                    EmailExists = true // Don't reveal user doesn't exist
                };
            }

            return new Check2FAStatusResponse
            {
                Has2FA = admin.IsTwoFactorEnabled,
                EmailExists = true
            };
        }

        /// <summary>
        /// Hash 2FA code using SHA256 for secure storage
        /// Defense in depth: Even if DB is compromised, codes can't be derived
        /// </summary>
        private static string HashTwoFactorCode(string code)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(code));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
