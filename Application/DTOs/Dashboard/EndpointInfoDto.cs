namespace Application.DTOs.Dashboard;

/// <summary>
/// Represents information about an API endpoint
/// </summary>
public class EndpointInfoDto
{
    /// <summary>
    /// The HTTP method (GET, POST, PUT, DELETE, etc.)
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// The full route path of the endpoint
    /// </summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>
    /// The controller name
    /// </summary>
    public string Controller { get; set; } = string.Empty;

    /// <summary>
    /// The action method name
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Authorization policy required (if any)
    /// </summary>
    public string? AuthorizationPolicy { get; set; }

    /// <summary>
    /// Whether the endpoint allows anonymous access
    /// </summary>
    public bool AllowAnonymous { get; set; }

    /// <summary>
    /// XML documentation summary (if available)
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Parameter information
    /// </summary>
    public List<ParameterInfoDto> Parameters { get; set; } = new();
}

