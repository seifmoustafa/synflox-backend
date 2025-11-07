using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Application.Services;
using Domain.Entities.Authentication;
using Domain.Constants;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Authentication
{
    /// <summary>
    /// Generates JWT access tokens and refresh tokens using settings bound from configuration.
    /// Ensure <c>JwtSettings</c> section is populated with Issuer, Audience, SecretKey,
    /// token <c>Lifetime</c> in minutes and <c>RefreshTokenExpiration</c>.
    /// </summary>
    public class JwtTokenGenerator : IJwtTokenGenerator
    {

        private readonly JwtOptions _options;

        public JwtTokenGenerator(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }

        public RefreshToken GenerateRefreshToken(User user) =>
            GenerateRefreshTokenInternal(userId: user.Id);

        public RefreshToken GenerateRefreshToken(Admin admin) =>
            GenerateRefreshTokenInternal(adminId: admin.Id);

        private RefreshToken GenerateRefreshTokenInternal(Guid? userId = null, Guid? adminId = null)
        {
            var randomNumber = new byte[32];
            RandomNumberGenerator.Fill(randomNumber);

            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomNumber),
                Expires = DateTime.UtcNow.AddMinutes(_options.RefreshTokenExpiration),
                UserId = userId,
                AdminId = adminId
            };
        }

        public string GenerateToken(User user)
        {

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, "User"),
                new(JwtClaimTypes.AdminTypeName, "User"),
                new(JwtClaimTypes.FirstName, user.FirstName ?? string.Empty),
                new(JwtClaimTypes.MiddleName, user.MiddleName ?? string.Empty),
                new(JwtClaimTypes.LastName, user.LastName ?? string.Empty),
                new(JwtClaimTypes.BirthDate, user.BirthDate.HasValue ? user.BirthDate.Value.ToString("O") : string.Empty),
                new(JwtClaimTypes.Email, user.Email ?? string.Empty),
                new(JwtClaimTypes.PhoneNumber, user.PhoneNumber ?? string.Empty),
                new(JwtClaimTypes.NationalId, user.NationalId ?? string.Empty),
                new(JwtClaimTypes.Gender, user.Gender.HasValue ? ((int)user.Gender.Value).ToString() : string.Empty),
                new(JwtClaimTypes.ImagePath, user.ImagePath ?? string.Empty),
                new(JwtClaimTypes.Country, user.Country ?? string.Empty),
                new(JwtClaimTypes.Government, user.Government ?? string.Empty),
                new(JwtClaimTypes.City, user.City ?? string.Empty),
                new(JwtClaimTypes.IsEmailVerified, user.IsEmailVerified.ToString()),
                new(JwtClaimTypes.IsPhoneVerified, user.IsPhoneVerified.ToString()),
                new(JwtClaimTypes.IsActive, user.IsActive.ToString())
            };

            return GenerateTokenInternal(claims);
        }

        public string GenerateToken(Admin admin)
        {
            var typeName = admin.AdminType?.AdminTypeName ?? string.Empty;
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                new(ClaimTypes.Name, admin.Username),
                new(ClaimTypes.Role, typeName),
                new(JwtClaimTypes.AdminTypeName, typeName),
                new(JwtClaimTypes.FirstName, admin.FirstName ?? string.Empty),
                new(JwtClaimTypes.LastName, admin.LastName ?? string.Empty),
                new(JwtClaimTypes.PhoneNumber, admin.PhoneNumber ?? string.Empty)
            };

            return GenerateTokenInternal(claims);
        }

        private string GenerateTokenInternal(IEnumerable<Claim> claims)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                Expires = DateTime.UtcNow.AddMinutes(_options.Lifetime),
                SigningCredentials = credentials,
                Subject = new ClaimsIdentity(claims)
            };

            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(securityToken);
        }
    }

}
