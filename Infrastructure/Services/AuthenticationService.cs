using System;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Authentication;
using Application.DTOs.Admin;
using Application.Services;
using AutoMapper;
using Domain.Entities.Authentication;
using Domain.Interfaces;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;

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
        private readonly IIdEncryptionService _idEncryption;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoginAttemptService _loginAttemptService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPasswordPolicyService _passwordPolicyService;

        public AuthenticationService(
            IAdminRepository adminRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator,
            IRefreshTokenRepository refreshTokenRepo,
            IBaseRepository<Guid, AdminType> adminTypeRepository,
            IMapper mapper,
            ILocalizationService localizer,
            IIdEncryptionService idEncryption,
            IUnitOfWork unitOfWork,
            ILoginAttemptService loginAttemptService,
            IHttpContextAccessor httpContextAccessor,
            IPasswordPolicyService passwordPolicyService)
        {
            _adminRepository = adminRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _refreshTokenRepo = refreshTokenRepo;
            _adminTypeRepository = adminTypeRepository;
            _mapper = mapper;
            _localizer = localizer;
            _idEncryption = idEncryption;
            _unitOfWork = unitOfWork;
            _loginAttemptService = loginAttemptService;
            _httpContextAccessor = httpContextAccessor;
            _passwordPolicyService = passwordPolicyService;
        }

        public async Task<AdminDto> RegisterAdminAsync(CreateAdminDto request)
        {
            // Validate password against policy
            var passwordValidation = await _passwordPolicyService.ValidatePasswordAsync(request.Password);
            if (!passwordValidation.IsValid)
            {
                throw new BadRequestException(string.Join(", ", passwordValidation.Errors));
            }

            var admin = _mapper.Map<Admin>(request);
            var passwordHash = _passwordHasher.HashPassword(request.Password);
            admin.Password = passwordHash;

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

            // Record password in history
            await _passwordPolicyService.RecordPasswordChangeAsync(admin.Id, passwordHash);

            return _mapper.Map<AdminDto>(admin);
        }

        public async Task<AuthenticationResponse> AdminAuthenticationAsync(string username, string password)
        {
            // Get IP address from request
            var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

            // Check if account is locked
            var isLocked = await _loginAttemptService.IsAccountLockedAsync(username, ipAddress);
            if (isLocked)
            {
                await _loginAttemptService.RecordAttemptAsync(
                    username,
                    false,
                    ipAddress,
                    _localizer["AccountLocked"],
                    null);

                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["AccountLocked"]
                };
            }

            var admin = await _adminRepository.GetByUserNameAsync(username);
            if (admin == null)
            {
                await _loginAttemptService.RecordAttemptAsync(
                    username,
                    false,
                    ipAddress,
                    _localizer["InvalidAdminCredentials"],
                    null);

                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["InvalidAdminCredentials"]
                };
            }

            // Check if password has expired
            var isPasswordExpired = await _passwordPolicyService.IsPasswordExpiredAsync(admin.Id);
            if (isPasswordExpired)
            {
                await _loginAttemptService.RecordAttemptAsync(
                    username,
                    false,
                    ipAddress,
                    _localizer["PasswordExpired"],
                    null);

                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["PasswordExpired"]
                };
            }

            bool checkPassword = _passwordHasher.VerifyPassword(password, admin.Password);
            if (!checkPassword)
            {
                await _loginAttemptService.RecordAttemptAsync(
                    username,
                    false,
                    ipAddress,
                    _localizer["InvalidAdminCredentials"],
                    null);

                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["InvalidAdminCredentials"]
                };
            }

            // Record successful login
            await _loginAttemptService.RecordAttemptAsync(
                username,
                true,
                ipAddress,
                null,
                admin.Id);

            admin.AdminType = await _adminTypeRepository.GetByIdAsync(admin.AdminTypeId, null)
                ?? throw new NotFoundException(_localizer["AdminTypeNotFound"]);

            return await GenerateTokensAsync(
                admin,
                _jwtTokenGenerator.GenerateToken,
                _jwtTokenGenerator.GenerateRefreshToken);
        }

        public async Task<AuthenticationResponse> RegenerateAccessToken(Guid adminId)
        {
            var refreshedToken = await _refreshTokenRepo.GetByAdminId(adminId);

            if (refreshedToken != null && refreshedToken.IsActive && !refreshedToken.IsExpired)
            {
                var admin = await _adminRepository.GetByIdAsync(adminId, null);
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

                refreshedToken.IsActive = false;
                await _refreshTokenRepo.UpdateAsync(refreshedToken);

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

            return new AuthenticationResponse
            {
                Success = false,
                ErrorMessage = _localizer["Unauthorized"]
            };
        }

        public async Task Logout(Guid adminId)
        {
            var refreshedToken = await _refreshTokenRepo.GetByAdminId(adminId);
            if (refreshedToken != null)
            {
                refreshedToken.IsActive = false;
                await _refreshTokenRepo.UpdateAsync(refreshedToken);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task ChangeAdminPasswordAsync(Guid adminId, string newPassword)
        {
            var admin = await _adminRepository.GetByIdAsync(adminId, null);
            if (admin == null)
            {
                throw new NotFoundException(_localizer["UserNotFound"]);
            }

            var adminType = (await _adminTypeRepository.FindAsync(u => u.AdminTypeName == "Admin")).FirstOrDefault();
            if (adminType == null || admin.AdminTypeId != adminType.Id)
            {
                throw new BadRequestException(_localizer["TargetNotAdmin"]);
            }

            // Validate password against policy
            var passwordValidation = await _passwordPolicyService.ValidatePasswordAsync(newPassword, adminId);
            if (!passwordValidation.IsValid)
            {
                throw new BadRequestException(string.Join(", ", passwordValidation.Errors));
            }

            // Check if password has expired
            var isExpired = await _passwordPolicyService.IsPasswordExpiredAsync(adminId);
            if (isExpired)
            {
                // Password expired, but allow change (this is the change password flow)
            }

            var passwordHash = _passwordHasher.HashPassword(newPassword);
            admin.Password = passwordHash;
            await _adminRepository.UpdateAsync(admin);
            await _unitOfWork.SaveChangesAsync();

            // Record password in history
            await _passwordPolicyService.RecordPasswordChangeAsync(adminId, passwordHash);
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
    }
}
