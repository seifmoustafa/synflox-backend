namespace Application.DTOs.Dashboard;

/// <summary>
/// Represents information about an endpoint parameter
/// </summary>
public class ParameterInfoDto
{
    /// <summary>
    /// Parameter name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parameter type name
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Where the parameter comes from (Route, Query, Body, etc.)
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Whether the parameter is optional
    /// </summary>
    public bool IsOptional { get; set; }
}

