using System.ComponentModel.DataAnnotations;

namespace WebAPI.Middlewares;

/// <summary>
/// Options for <see cref="CacheHeadersMiddleware"/>. Values are bound from configuration.
/// </summary>
public class CacheHeadersOptions
{
    [Range(0, int.MaxValue)]
    public int MaxAgeSeconds { get; set; }
    public string[]? VaryByHeaders { get; set; }
}
