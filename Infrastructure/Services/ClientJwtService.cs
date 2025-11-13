using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Domain.Entities.ClientAccess;
using Domain.Entities.Subscriptions;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services;

/// <summary>
/// JWT service specifically for client access tokens
/// Separate from admin JWT to ensure security isolation
/// </summary>
public class ClientJwtService
{
    private readonly ClientTokenSettings _settings;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly SigningCredentials _signingCredentials;

    public ClientJwtService(IOptions<ClientTokenSettings> settings)
    {
        _settings = settings.Value;
        _tokenHandler = new JwtSecurityTokenHandler();
        
        var key = Encoding.UTF8.GetBytes(_settings.SigningKey);
        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key), 
            SecurityAlgorithms.HmacSha256Signature);
    }

    /// <summary>
    /// Generates a JWT token for a client subscription
    /// </summary>
    public string GenerateToken(ClientAccessToken clientToken, Subscription subscription)
    {
        var claims = new List<Claim>
        {
            // Standard JWT claims
            new(JwtRegisteredClaimNames.Jti, clientToken.Id.ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Exp, ((DateTimeOffset)clientToken.ExpiresAtUtc).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            
            // Custom client claims
            new("client_token_id", clientToken.Id.ToString()),
            new("company_id", clientToken.CompanyId.ToString()),
            new("company_name", subscription.Company.Name),
            new("subscription_id", clientToken.SubscriptionId.ToString()),
            new("plan_id", subscription.PlanId.ToString()),
            new("plan_name", subscription.Plan.Name),
            new("token_version", clientToken.TokenVersion),
            new("is_trial", subscription.IsTrial.ToString().ToLower()),
            
            // Permissions and endpoints
            new("allowed_endpoints", clientToken.AllowedEndpoints ?? JsonSerializer.Serialize(_settings.DefaultAllowedEndpoints)),
            
            // Security claims
            new("issued_at_utc", clientToken.IssuedAtUtc.ToString("O")),
            new("expires_at_utc", clientToken.ExpiresAtUtc.ToString("O")),
            new("rate_limit_per_hour", _settings.RateLimitPerHour.ToString()),
            
            // Subscription status
            new("subscription_active", subscription.IsActive.ToString().ToLower()),
            new("subscription_expired", subscription.IsExpired.ToString().ToLower()),
            new("subscription_expiry", subscription.ExpiryDateUtc.ToString("O"))
        };

        // Add plan features if available
        if (subscription.Plan.CustomFeatures?.Any() == true)
        {
            claims.Add(new Claim("plan_features", JsonSerializer.Serialize(subscription.Plan.CustomFeatures)));
        }

        // Add plan modules if available
        if (subscription.Plan.PlanModules?.Any() == true)
        {
            var modules = subscription.Plan.PlanModules.Select(pm => pm.Module.Name).ToList();
            claims.Add(new Claim("plan_modules", JsonSerializer.Serialize(modules)));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = clientToken.ExpiresAtUtc,
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            SigningCredentials = _signingCredentials,
            NotBefore = DateTime.UtcNow,
            IssuedAt = DateTime.UtcNow
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Validates a JWT token and extracts claims
    /// </summary>
    public (bool IsValid, ClaimsPrincipal? Principal, string? Error) ValidateToken(string token)
    {
        try
        {
            var key = Encoding.UTF8.GetBytes(_settings.SigningKey);
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = _settings.ValidateIssuerSigningKey,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = _settings.ValidateIssuer,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = _settings.ValidateAudience,
                ValidAudience = _settings.Audience,
                ValidateLifetime = _settings.ValidateLifetime,
                ClockSkew = TimeSpan.FromMinutes(5), // Allow 5 minutes clock skew
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            // Additional validation for JWT token type
            // Additional validation for JWT token type
            if (validatedToken is not JwtSecurityToken jwtToken || 
                !jwtToken.Header.Alg.Equals("HS256", StringComparison.InvariantCultureIgnoreCase))
            {
                return (false, null, "Invalid token algorithm");
            }

            return (true, principal, null);
        }
        catch (SecurityTokenExpiredException)
        {
            return (false, null, "Token has expired");
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return (false, null, "Invalid token signature");
        }
        catch (SecurityTokenValidationException ex)
        {
            return (false, null, $"Token validation failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, null, $"Token validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Extracts client token ID from JWT claims
    /// </summary>
    public Guid? GetClientTokenId(ClaimsPrincipal principal)
    {
        var tokenIdClaim = principal.FindFirst("client_token_id")?.Value;
        return Guid.TryParse(tokenIdClaim, out var tokenId) ? tokenId : null;
    }

    /// <summary>
    /// Extracts company ID from JWT claims
    /// </summary>
    public Guid? GetCompanyId(ClaimsPrincipal principal)
    {
        var companyIdClaim = principal.FindFirst("company_id")?.Value;
        return Guid.TryParse(companyIdClaim, out var companyId) ? companyId : null;
    }

    /// <summary>
    /// Extracts subscription ID from JWT claims
    /// </summary>
    public Guid? GetSubscriptionId(ClaimsPrincipal principal)
    {
        var subscriptionIdClaim = principal.FindFirst("subscription_id")?.Value;
        return Guid.TryParse(subscriptionIdClaim, out var subscriptionId) ? subscriptionId : null;
    }

    /// <summary>
    /// Checks if token has access to a specific endpoint
    /// </summary>
    public bool HasEndpointAccess(ClaimsPrincipal principal, string endpoint)
    {
        var allowedEndpointsClaim = principal.FindFirst("allowed_endpoints")?.Value;
        if (string.IsNullOrEmpty(allowedEndpointsClaim))
            return false;

        try
        {
            var allowedEndpoints = JsonSerializer.Deserialize<List<string>>(allowedEndpointsClaim);
            return allowedEndpoints?.Any(ae => endpoint.StartsWith(ae, StringComparison.OrdinalIgnoreCase)) == true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates a secure token hash for database storage
    /// </summary>
    public string GenerateTokenHash(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Gets token expiry from JWT without full validation (for quick checks)
    /// </summary>
    public DateTime? GetTokenExpiry(string token)
    {
        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets all claims from a JWT token as a dictionary
    /// </summary>
    public Dictionary<string, string> GetTokenClaims(ClaimsPrincipal principal)
    {
        return principal.Claims.ToDictionary(c => c.Type, c => c.Value);
    }
}
