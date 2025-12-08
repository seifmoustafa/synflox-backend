using System;

namespace Application.DTOs.ClientAdmin;

/// <summary>
/// Request to generate a new admin token for a company.
/// </summary>
public class GenerateAdminTokenRequest
{
    /// <summary>
    /// Company ID to generate token for.
    /// </summary>
    public Guid CompanyId { get; set; }
    
    /// <summary>
    /// Friendly name for the token.
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Number of days until token expires. Default 365.
    /// </summary>
    public int ExpiryDays { get; set; } = 365;
    
    /// <summary>
    /// Offline Permissions
    /// </summary>
    public bool CanBindDevices { get; set; } = true;
    public bool CanUnbindDevices { get; set; } = true;
    public bool CanViewDevices { get; set; } = true;
    public bool CanApproveReplacements { get; set; } = true;
    
    /// <summary>
    /// Online Permissions
    /// </summary>
    public bool CanViewOnlineTokens { get; set; } = true;
    public bool CanManageOnlineTokens { get; set; } = true;
    public bool CanViewOnlineDevices { get; set; } = true;
    public bool CanUnbindOnlineDevices { get; set; } = true;
    
    /// <summary>
    /// Daily API call limit (0 = unlimited).
    /// </summary>
    public int DailyApiLimit { get; set; } = 0;
    
    /// <summary>
    /// Notes for internal tracking.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Response from token generation.
/// </summary>
public class GenerateAdminTokenResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    
    /// <summary>
    /// The actual JWT token. Only returned once during generation!
    /// </summary>
    public string? Token { get; set; }
    
    /// <summary>
    /// Token details (without the actual token).
    /// </summary>
    public AdminTokenDto? TokenInfo { get; set; }
}

/// <summary>
/// Admin token information (without the actual token).
/// </summary>
public class AdminTokenDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime IssuedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public int DaysUntilExpiry { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public bool IsExpired { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public int UsageCount { get; set; }
    public string? LastUsedFromIp { get; set; }
    
    // Offline Permissions
    public bool CanBindDevices { get; set; }
    public bool CanUnbindDevices { get; set; }
    public bool CanViewDevices { get; set; }
    public bool CanApproveReplacements { get; set; }
    
    // Online Permissions
    public bool CanViewOnlineTokens { get; set; }
    public bool CanManageOnlineTokens { get; set; }
    public bool CanViewOnlineDevices { get; set; }
    public bool CanUnbindOnlineDevices { get; set; }
    
    public int DailyApiLimit { get; set; }
    public int TodayApiCalls { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Context for authenticated client admin requests.
/// </summary>
public class ClientAdminContext
{
    public Guid TokenId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    
    // Offline Permissions
    public bool CanBindDevices { get; set; }
    public bool CanUnbindDevices { get; set; }
    public bool CanViewDevices { get; set; }
    public bool CanApproveReplacements { get; set; }
    
    // Online Permissions
    public bool CanViewOnlineTokens { get; set; }
    public bool CanManageOnlineTokens { get; set; }
    public bool CanViewOnlineDevices { get; set; }
    public bool CanUnbindOnlineDevices { get; set; }
}
