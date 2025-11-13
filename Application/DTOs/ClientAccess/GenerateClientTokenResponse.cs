using System;
using System.Collections.Generic;

namespace Application.DTOs.ClientAccess;

/// <summary>
/// Response containing the generated client access token
/// </summary>
public class GenerateClientTokenResponse
{
    /// <summary>
    /// The generated JWT token (only returned once)
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Token metadata
    /// </summary>
    public Guid TokenId { get; set; }
    public DateTime IssuedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public List<string> AllowedEndpoints { get; set; } = new();
    public string TokenVersion { get; set; } = string.Empty;

    /// <summary>
    /// Company and subscription information
    /// </summary>
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid SubscriptionId { get; set; }
    public string PlanName { get; set; } = string.Empty;

    /// <summary>
    /// Usage instructions
    /// </summary>
    public string Usage { get; set; } = "Include this token in the Authorization header as 'Bearer {token}' when making API calls.";
    
    /// <summary>
    /// Security warning
    /// </summary>
    public string SecurityWarning { get; set; } = "This token will only be shown once. Store it securely and never share it.";
}
