using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Domain.Entities.OnlineAccess;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services;

/// <summary>
/// Service for generating and validating Online Client JWT tokens.
/// These are THIN tokens - only identity, NO entitlements embedded.
/// </summary>
public interface IOnlineJwtService
{
    /// <summary>
    /// Generate a new JWT token for an online client.
    /// </summary>
    string GenerateToken(OnlineClientToken tokenEntity);
    
    /// <summary>
    /// Validate a JWT token and extract claims.
    /// </summary>
    OnlineTokenClaims? ValidateToken(string token);
    
    /// <summary>
    /// Compute SHA-256 hash of a token for storage.
    /// </summary>
    string ComputeTokenHash(string token);
}

/// <summary>
/// Claims extracted from an online client token.
/// </summary>
public class OnlineTokenClaims
{
    public Guid TokenId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid SubscriptionId { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class OnlineJwtService : IOnlineJwtService
{
    private readonly OnlineTokenSettings _settings;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public OnlineJwtService(IOptions<OnlineTokenSettings> settings)
    {
        _settings = settings.Value;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public string GenerateToken(OnlineClientToken tokenEntity)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // THIN TOKEN: Only identity claims, NO entitlements
        var claims = new[]
        {
            new Claim("token_id", tokenEntity.Id.ToString()),
            new Claim("company_id", tokenEntity.CompanyId.ToString()),
            new Claim("subscription_id", tokenEntity.SubscriptionId.ToString()),
            new Claim("token_name", tokenEntity.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: tokenEntity.ExpiresAtUtc,
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }

    public OnlineTokenClaims? ValidateToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _settings.Issuer,
                ValidAudience = _settings.Audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            if (validatedToken is not JwtSecurityToken jwtToken)
                return null;

            return new OnlineTokenClaims
            {
                TokenId = Guid.Parse(principal.FindFirst("token_id")?.Value ?? Guid.Empty.ToString()),
                CompanyId = Guid.Parse(principal.FindFirst("company_id")?.Value ?? Guid.Empty.ToString()),
                SubscriptionId = Guid.Parse(principal.FindFirst("subscription_id")?.Value ?? Guid.Empty.ToString()),
                IssuedAt = jwtToken.ValidFrom,
                ExpiresAt = jwtToken.ValidTo
            };
        }
        catch
        {
            return null;
        }
    }

    public string ComputeTokenHash(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
