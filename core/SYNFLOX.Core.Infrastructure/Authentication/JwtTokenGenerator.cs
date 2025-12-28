using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Services;
using Domain.Constants;
using Domain.Entities.Authentication;
using Domain.Entities.Licensing;
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

        #region Admin (SYNFLOX Admin) Token Generation

        public RefreshToken GenerateRefreshToken(Admin admin) =>
            GenerateRefreshTokenInternal(adminId: admin.Id);

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
                new(JwtClaimTypes.PhoneNumber, admin.PhoneNumber ?? string.Empty),
            };

            return GenerateTokenInternal(claims);
        }

        #endregion

        #region CompanyAdmin (Client Portal) Token Generation

        public string GenerateCompanyAdminToken(CompanyAdmin companyAdmin)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, companyAdmin.Id.ToString()),
                new(ClaimTypes.Name, companyAdmin.Username),
                new(ClaimTypes.Role, "CompanyAdmin"),
                new(JwtClaimTypes.CompanyAdminId, companyAdmin.Id.ToString()),
                new(JwtClaimTypes.CompanyId, companyAdmin.CompanyId.ToString()),
                new(JwtClaimTypes.DisplayName, companyAdmin.DisplayName ?? string.Empty),
                new(JwtClaimTypes.Email, companyAdmin.Email ?? string.Empty),
                new(JwtClaimTypes.PhoneNumber, companyAdmin.Phone ?? string.Empty),
            };

            return GenerateTokenInternal(claims);
        }

        public RefreshToken GenerateCompanyAdminRefreshToken(CompanyAdmin companyAdmin) =>
            GenerateRefreshTokenInternal(companyAdminId: companyAdmin.Id);

        #endregion

        #region Private Methods

        private RefreshToken GenerateRefreshTokenInternal(
            Guid? adminId = null,
            Guid? companyAdminId = null
        )
        {
            var randomNumber = new byte[32];
            RandomNumberGenerator.Fill(randomNumber);

            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomNumber),
                Expires = DateTime.UtcNow.AddMinutes(_options.RefreshTokenExpiration),
                AdminId = adminId,
                CompanyAdminId = companyAdminId,
            };
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
                Subject = new ClaimsIdentity(claims),
            };

            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(securityToken);
        }

        #endregion
    }
}
