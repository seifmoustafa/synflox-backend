using System.ComponentModel.DataAnnotations;

namespace WebAPI.Middlewares;

/// <summary>
/// Options for the <see cref="ETagMiddleware"/> controlling cache duration.
/// Values are bound from configuration so no defaults are hard-coded.
/// </summary>
public class ETagOptions
{
    /// <summary>
    /// Maximum time in seconds to keep responses in the in-memory cache.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int MaxAgeSeconds { get; set; }
}
