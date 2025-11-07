using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;

namespace WebAPI.Middlewares;

/// <summary>
/// Custom culture provider that supports multiple ways to specify language (2025 best practices):
/// 1. Accept-Language header (standard HTTP, RFC 7231) - MOST RESTFUL ⭐
/// 2. Custom header X-Language (explicit control) - RECOMMENDED for frontend apps
/// 3. Query parameter: ?lang=ar (fallback for compatibility)
/// 
/// Priority order follows RESTful API best practices where headers are preferred over query parameters.
/// </summary>
public class CustomRequestCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        if (httpContext == null)
        {
            throw new ArgumentNullException(nameof(httpContext));
        }

        // Priority 1: Accept-Language header (standard HTTP, RFC 7231, most RESTful)
        // This is the standard HTTP header for content negotiation
        if (httpContext.Request.Headers.TryGetValue("Accept-Language", out var acceptLang))
        {
            var culture = ExtractCultureFromAcceptLanguage(acceptLang.ToString());
            if (culture != null)
            {
                return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture, culture));
            }
        }

        // Priority 2: Custom header X-Language (explicit control, clean, recommended for SPAs)
        // Frontend can explicitly set this header for user-selected language
        if (httpContext.Request.Headers.TryGetValue("X-Language", out var headerLang))
        {
            var culture = NormalizeCulture(headerLang.ToString());
            if (culture != null)
            {
                return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture, culture));
            }
        }

        // Priority 3: Query parameter (fallback for compatibility, not ideal for POST/PUT/DELETE)
        // Supported but not recommended as primary method
        var queryLang = httpContext.Request.Query["lang"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(queryLang))
        {
            var culture = NormalizeCulture(queryLang);
            if (culture != null)
            {
                return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture, culture));
            }
        }

        // Default: Return null to use default culture (English)
        return Task.FromResult<ProviderCultureResult?>(null);
    }

    /// <summary>
    /// Normalizes culture code to "en" or "ar"
    /// </summary>
    private static string? NormalizeCulture(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return null;

        culture = culture.Trim().ToLowerInvariant();

        // Support various formats
        if (culture.StartsWith("ar", StringComparison.OrdinalIgnoreCase) || 
            culture.StartsWith("ar-", StringComparison.OrdinalIgnoreCase))
        {
            return "ar";
        }

        if (culture.StartsWith("en", StringComparison.OrdinalIgnoreCase) || 
            culture.StartsWith("en-", StringComparison.OrdinalIgnoreCase))
        {
            return "en";
        }

        return null;
    }

    /// <summary>
    /// Extracts culture from Accept-Language header (e.g., "ar-SA,ar;q=0.9,en-US;q=0.8,en;q=0.7")
    /// </summary>
    private static string? ExtractCultureFromAcceptLanguage(string acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage))
            return null;

        // Split by comma and process each language preference
        var languages = acceptLanguage.Split(',')
            .Select(lang => lang.Split(';')[0].Trim())
            .ToList();

        foreach (var lang in languages)
        {
            var culture = NormalizeCulture(lang);
            if (culture != null)
            {
                return culture;
            }
        }

        return null;
    }
}

