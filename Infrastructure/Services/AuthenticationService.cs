using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Application.DTOs.Authentication;
using Application.Services;
using AutoMapper;
using Application.DTOs.User;
using Application.DTOs.Admin;
using Domain.Entities.Authentication;
using Domain.Enums;
using Domain.Interfaces;
using Domain.Exceptions;

namespace Infrastructure.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAdminRepository _adminRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IRefreshTokenRepository _refreshTokenRepo;
        private readonly IBaseRepository<Guid, AdminType> _userTypeRepository;
        private readonly IMapper _mapper;
        private readonly IOtpService _otpService;
        private readonly ILocalizationService _localizer;
        private readonly IFileService _fileService;
        private readonly IIdEncryptionService _idEncryption;
        private readonly IUnitOfWork _unitOfWork;
        public AuthenticationService(IUserRepository userRepository, IAdminRepository adminRepository, IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator, IRefreshTokenRepository refreshTokenRepo,
            IBaseRepository<Guid, AdminType> userTypeRepository, IMapper mapper, IOtpService otpService,
            ILocalizationService localizer, IFileService fileService, IIdEncryptionService idEncryption, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _adminRepository = adminRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
            _refreshTokenRepo = refreshTokenRepo;
            _userTypeRepository = userTypeRepository;
            _mapper = mapper;
            _otpService = otpService;
            _localizer = localizer;
            _fileService = fileService;
            _idEncryption = idEncryption;
            _unitOfWork = unitOfWork;
        }


        public async Task<UserDto> RegisterAsync(RegistrationRequest request)
        {
            var user = _mapper.Map<User>(request);

            var existingUsernameUser = await _userRepository.GetByUserNameAsync(user.Username);
            if (existingUsernameUser != null)
            {
                throw new BadRequestException(_localizer["UsernameTaken"]);
            }

            if (!string.IsNullOrEmpty(user.Email))
            {
                var existingEmailUser = await _userRepository.GetByEmailAsync(user.Email);
                if (existingEmailUser != null)
                {
                    throw new BadRequestException(_localizer["EmailTaken"]);
                }
            }

            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                var existingPhoneUser = await _userRepository.GetByPhoneNumberAsync(user.PhoneNumber);
                if (existingPhoneUser != null)
                {
                    throw new BadRequestException(_localizer["PhoneTaken"]);
                }
            }

            string? imagePath = null;
            if (!string.IsNullOrEmpty(request.ImageUrl))
            {
                imagePath = await _fileService.RenameFileAsync(request.ImageUrl, "Image", user.Username);
                user.ImagePath = imagePath;
            }

            user.Password = _passwordHasher.HashPassword(request.Password);
            user.Providers = AuthProvider.Credentials;
            user.IsEmailVerified = false;
            user.IsPhoneVerified = false;
            user.IsVerified = false;

            try
            {
                await _userRepository.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();
            }
            catch
            {
                if (!string.IsNullOrEmpty(imagePath))
                {
                    await _fileService.DeleteFileAsync(imagePath, "Image");
                }
                throw;
            }

            await _otpService.SendOtpAsync(user, OtpPurpose.EmailVerification);
            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                await _otpService.SendOtpAsync(user, OtpPurpose.PhoneVerification);
            }

            return _mapper.Map<UserDto>(user);
        }

        public async Task<AdminDto> RegisterAdminAsync(CreateAdminDto request)
        {
            if (request.AdminTypeId.HasValue)
                request.AdminTypeId = _idEncryption.Decrypt(request.AdminTypeId.Value);

            var admin = _mapper.Map<Admin>(request);
            admin.Password = _passwordHasher.HashPassword(request.Password);

            if (admin.AdminTypeId == Guid.Empty)
            {
                var adminType = (await _userTypeRepository.FindAsync(u => u.AdminTypeName == "Admin")).FirstOrDefault();
                if (adminType != null)
                {
                    admin.AdminTypeId = adminType.Id;
                }
            }

            await _adminRepository.AddAsync(admin);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<AdminDto>(admin);
        }

        public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateProfileRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId, null);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFound"]);
            }

            string? oldImage = user.ImagePath;
            string? newImage = null;
            if (!string.IsNullOrEmpty(request.ImageUrl))
            {
                newImage = await _fileService.RenameFileAsync(request.ImageUrl, "Image", user.Username);
                user.ImagePath = newImage;
            }

            // map updatable fields while ignoring nulls
            _mapper.Map(request, user);

            try
            {
                await _userRepository.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();
                if (!string.IsNullOrEmpty(oldImage) && oldImage != user.ImagePath)
                {
                    await _fileService.DeleteFileAsync(oldImage, "Image");
                }
            }
            catch
            {
                if (!string.IsNullOrEmpty(newImage))
                {
                    await _fileService.DeleteFileAsync(newImage, "Image");
                }
                throw;
            }

            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> ChangeEmailAsync(Guid userId, string email)
        {
            var user = await _userRepository.GetByIdAsync(userId, null);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFound"]);
            }

            if (user.Email != email)
            {
                var existing = await _userRepository.GetByEmailAsync(email);
                if (existing != null && existing.Id != userId)
                {
                    throw new BadRequestException(_localizer["EmailTaken"]);
                }
                user.Email = email;
                user.IsEmailVerified = false;
                user.IsVerified = user.IsPhoneVerified || user.IsEmailVerified;
                await _userRepository.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();
                await _otpService.SendOtpAsync(user, OtpPurpose.EmailVerification);
            }

            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> ChangePhoneAsync(Guid userId, string phoneNumber)
        {
            var user = await _userRepository.GetByIdAsync(userId, null);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFound"]);
            }

            if (user.PhoneNumber != phoneNumber)
            {
                var existing = await _userRepository.GetByPhoneNumberAsync(phoneNumber);
                if (existing != null && existing.Id != userId)
                {
                    throw new BadRequestException(_localizer["PhoneTaken"]);
                }
                user.PhoneNumber = phoneNumber;
                user.IsPhoneVerified = false;
                user.IsVerified = user.IsPhoneVerified || user.IsEmailVerified;
                await _userRepository.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();
                await _otpService.SendOtpAsync(user, OtpPurpose.PhoneVerification);
            }

            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> ChangeUsernameAsync(Guid userId, string username)
        {
            var user = await _userRepository.GetByIdAsync(userId, null);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFound"]);
            }

            if (user.Username != username)
            {
                var existing = await _userRepository.GetByUserNameAsync(username);
                if (existing != null && existing.Id != userId)
                {
                    throw new BadRequestException(_localizer["UsernameTaken"]);
                }
                if (!string.IsNullOrEmpty(user.ImagePath))
                {
                    user.ImagePath = await _fileService.RenameFileAsync(user.ImagePath, "Image", username);
                }

                user.Username = username;
                await _userRepository.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();
            }

            return _mapper.Map<UserDto>(user);
        }

        public async Task DeleteUserImageAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId, null);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFound"]);
            }

            if (!string.IsNullOrEmpty(user.ImagePath))
            {
                var path = user.ImagePath;
                user.ImagePath = null;
                await _userRepository.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();
                await _fileService.DeleteFileAsync(path, "Image");
            }
        }

        public async Task ChangeAdminPasswordAsync(Guid adminId, string newPassword)
        {
            var admin = await _adminRepository.GetByIdAsync(adminId, null);
            if (admin == null)
            {
                throw new NotFoundException(_localizer["UserNotFound"]);
            }

            var adminType = (await _userTypeRepository.FindAsync(u => u.AdminTypeName == "Admin")).FirstOrDefault();
            if (adminType == null || admin.AdminTypeId != adminType.Id)
            {
                throw new BadRequestException(_localizer["TargetNotAdmin"]);
            }

            admin.Password = _passwordHasher.HashPassword(newPassword);
            await _adminRepository.UpdateAsync(admin);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var user = await _userRepository.GetByIdAsync(userId, null);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFound"]);
            }

            if (!_passwordHasher.VerifyPassword(currentPassword, user.Password))
            {
                throw new BadRequestException(_localizer["InvalidCurrentPassword"]);
            }

            user.Password = _passwordHasher.HashPassword(newPassword);
            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<AuthenticationResponse> ExternalLoginAsync(string provider, ExternalAuthRequest request)
        {
            AuthProvider providerFlag = provider.ToLower() switch
            {
                "google" => AuthProvider.Google,
                "facebook" => AuthProvider.Facebook,
                "apple" => AuthProvider.Apple,
                _ => AuthProvider.None
            };

            if (providerFlag == AuthProvider.None)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["ExternalProviderUnsupported"]
                };
            }

            string? email = providerFlag switch
            {
                AuthProvider.Google => await VerifyGoogleTokenAsync(request.Token),
                AuthProvider.Facebook => await VerifyFacebookTokenAsync(request.Token),
                AuthProvider.Apple => await VerifyAppleTokenAsync(request.Token),
                _ => null
            };

            if (string.IsNullOrEmpty(email))
            {
                return new AuthenticationResponse { Success = false, ErrorMessage = _localizer["ExternalLoginInvalid"] };
            }

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                user = new User
                {
                    Username = email,
                    Password = _passwordHasher.HashPassword(Guid.NewGuid().ToString()),
                    Email = email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Providers = providerFlag,
                    IsEmailVerified = true,
                    IsPhoneVerified = false,
                    IsVerified = true,
                    LastLogin = DateTime.UtcNow
                };
                await _userRepository.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                if (!user.IsActive)
                {
                    return new AuthenticationResponse
                    {
                        Success = false,
                        ErrorMessage = _localizer["AccountDeactivated"]
                    };
                }
                if (providerFlag != AuthProvider.None && !user.Providers.HasFlag(providerFlag))
                {
                    user.Providers |= providerFlag;
                }
                if (!user.IsEmailVerified && providerFlag != AuthProvider.None)
                {
                    user.IsEmailVerified = true;
                }
                user.LastLogin = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();
            }

            return await GenerateTokensAsync(
                user,
                _jwtTokenGenerator.GenerateToken,
                _jwtTokenGenerator.GenerateRefreshToken);
        }

        public async Task<AuthenticationResponse> AuthenticationAsync(string credential, string password)
        {
            // determine if the credential is email, phone or username
            User? user = null;
            if (credential.Contains("@"))
            {
                user = await _userRepository.GetByEmailAsync(credential);
            }
            if (user == null)
            {
                user = await _userRepository.GetByPhoneNumberAsync(credential);
            }
            if (user == null)
            {
                user = await _userRepository.GetByUserNameAsync(credential);
            }
            if (user == null)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["InvalidCredentials"]
                };
            }
            // hash password and compare
            bool checkPassword = _passwordHasher.VerifyPassword(password, user.Password);
            if (!checkPassword)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["InvalidCredentials"]
                };
            }

            if (!user.IsActive)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["AccountDeactivated"]
                };
            }

            if (!user.IsVerified)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["UserNotVerified"]
                };
            }

            if (!user.Providers.HasFlag(AuthProvider.Credentials))
            {
                user.Providers |= AuthProvider.Credentials;
            }

            user.LastLogin = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return await GenerateTokensAsync(
                user,
                _jwtTokenGenerator.GenerateToken,
                _jwtTokenGenerator.GenerateRefreshToken);

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

            bool checkPassword = _passwordHasher.VerifyPassword(password, admin.Password);
            if (!checkPassword)
            {
                return new AuthenticationResponse
                {
                    Success = false,
                    ErrorMessage = _localizer["InvalidAdminCredentials"]
                };
            }

            admin.AdminType = await _userTypeRepository.GetByIdAsync(admin.AdminTypeId, null)
                ?? throw new NotFoundException(_localizer["AdminTypeNotFound"]);
            return await GenerateTokensAsync(
                admin,
                _jwtTokenGenerator.GenerateToken,
                _jwtTokenGenerator.GenerateRefreshToken);
        }

        // Regenerating a new AccessToken when hitting the refresh-Token EndPoint 
        public async Task<AuthenticationResponse> RegenerateAccessToken(Guid userId)
        {
            var refreshedToken = await _refreshTokenRepo.GetByUserId(userId);

            if (refreshedToken != null && refreshedToken.IsActive && !refreshedToken.IsExpired)
            {
                var user = await _userRepository.GetByIdAsync(userId, null);
                if (user == null)
                {
                    return new AuthenticationResponse
                    {
                        Success = false,
                        ErrorMessage = _localizer["UserNotFound"]
                    };
                }

                refreshedToken.IsActive = false;
                await _refreshTokenRepo.UpdateAsync(refreshedToken);

                var token = _jwtTokenGenerator.GenerateToken(user);
                var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken(user);
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

        public async Task Logout(Guid userId)
        {
            var refreshedToken = await _refreshTokenRepo.GetByUserId(userId);
            if (refreshedToken != null)
            {
                refreshedToken.IsActive = false;
                await _refreshTokenRepo.UpdateAsync(refreshedToken);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        private async Task<AuthenticationResponse> GenerateTokensAsync<T>(
            T account,
            Func<T, string> tokenGenerator,
            Func<T, RefreshToken> refreshTokenGenerator)
        {
            var token = tokenGenerator(account);
            var refreshToken = refreshTokenGenerator(account);
            await _refreshTokenRepo.AddAsync(refreshToken);
            await _unitOfWork.SaveChangesAsync();
            return new AuthenticationResponse
            {
                AccessToken = token,
                Success = true,
                RefreshToken = refreshToken.Token
            };
        }

        public async Task<OtpSendResult> SendPasswordResetCodeAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return new OtpSendResult { Sent = false };
            return await _otpService.SendOtpAsync(user, OtpPurpose.PasswordReset);
        }

        public async Task<OtpSendResult> ResendVerificationCodeAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFound"]);
            }

            if (user.IsEmailVerified)
            {
                throw new BadRequestException(_localizer["UserAlreadyVerified"]);
            }

            return await _otpService.SendOtpAsync(user, OtpPurpose.EmailVerification);
        }

        public async Task ResetPasswordWithCodeAsync(string email, string code, string newPassword)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFound"]);
            }

            bool valid = await _otpService.ValidateOtpAsync(user, code, OtpPurpose.PasswordReset);
            if (!valid)
            {
                throw new BadRequestException(_localizer["InvalidOtp"]);
            }

            user.Password = _passwordHasher.HashPassword(newPassword);
            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task VerifyEmailAsync(string email, string code)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFound"]);
            }

            bool valid = await _otpService.ValidateOtpAsync(user, code, OtpPurpose.EmailVerification);
            if (!valid)
            {
                throw new BadRequestException(_localizer["InvalidOtp"]);
            }

            user.IsEmailVerified = true;
            user.IsVerified = user.IsPhoneVerified || user.IsEmailVerified;
            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<OtpSendResult> SendPhoneVerificationCodeAsync(string phoneNumber)
        {
            var user = await _userRepository.GetByPhoneNumberAsync(phoneNumber);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFound"]);
            }

            return await _otpService.SendOtpAsync(user, OtpPurpose.PhoneVerification);
        }

        public async Task<OtpSendResult> ResendPhoneVerificationCodeAsync(string phoneNumber)
        {
            var user = await _userRepository.GetByPhoneNumberAsync(phoneNumber);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFound"]);
            }

            if (user.IsPhoneVerified)
            {
                throw new BadRequestException(_localizer["UserAlreadyVerified"]);
            }

            return await _otpService.SendOtpAsync(user, OtpPurpose.PhoneVerification);
        }

        public async Task VerifyPhoneAsync(string phoneNumber, string code)
        {
            var user = await _userRepository.GetByPhoneNumberAsync(phoneNumber);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFound"]);
            }

            bool valid = await _otpService.ValidateOtpAsync(user, code, OtpPurpose.PhoneVerification);
            if (!valid)
            {
                throw new BadRequestException(_localizer["InvalidOtp"]);
            }

            user.IsPhoneVerified = true;
            user.IsVerified = user.IsPhoneVerified || user.IsEmailVerified;
            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task<string?> VerifyGoogleTokenAsync(string token)
        {
            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={token}");
                if (!response.IsSuccessStatusCode) return null;
                using var stream = await response.Content.ReadAsStreamAsync();
                var payload = await JsonSerializer.DeserializeAsync<JsonElement>(stream);
                if (payload.TryGetProperty("email", out var emailElem) &&
                    payload.TryGetProperty("email_verified", out var verifiedElem) &&
                    verifiedElem.GetString() == "true")
                {
                    if (payload.TryGetProperty("exp", out var expElem))
                    {
                        var exp = expElem.GetInt64();
                        var expDate = DateTimeOffset.FromUnixTimeSeconds(exp);
                        if (expDate < DateTimeOffset.UtcNow)
                        {
                            return null;
                        }
                    }
                    return emailElem.GetString();
                }
            }
            catch { }
            return null;
        }

        private async Task<string?> VerifyFacebookTokenAsync(string token)
        {
            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"https://graph.facebook.com/me?fields=email&access_token={token}");
                if (!response.IsSuccessStatusCode) return null;
                using var stream = await response.Content.ReadAsStreamAsync();
                var payload = await JsonSerializer.DeserializeAsync<JsonElement>(stream);
                if (payload.TryGetProperty("email", out var emailElem))
                {
                    return emailElem.GetString();
                }
            }
            catch { }
            return null;
        }

        private async Task<string?> VerifyAppleTokenAsync(string token)
        {
            try
            {
                using var httpClient = new HttpClient();
                var keysJson = await httpClient.GetStringAsync("https://appleid.apple.com/auth/keys");
                var keys = new JsonWebKeySet(keysJson);
                var handler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "https://appleid.apple.com",
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    IssuerSigningKeys = keys.Keys,
                    ClockSkew = TimeSpan.Zero
                };
                handler.ValidateToken(token, validationParameters, out SecurityToken validated);
                var jwt = (JwtSecurityToken)validated;
                if (jwt.Payload.TryGetValue("email", out var emailObj))
                {
                    return emailObj?.ToString();
                }
            }
            catch { }
            return null;
        }




    }
}
