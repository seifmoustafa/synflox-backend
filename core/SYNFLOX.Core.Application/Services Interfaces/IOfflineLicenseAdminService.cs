using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.ClientAdmin;

namespace Application.Services;

/// <summary>
/// Service for managing offline license admin tokens and device binding.
/// Used by client administrators to manage their company's device activations.
/// </summary>
public interface IOfflineLicenseAdminService
{
    #region Token Management (SYNFLOX Admin Only)
    
    /// <summary>
    /// Generate a new admin token for a company.
    /// Only SYNFLOX admins can create these tokens.
    /// </summary>
    Task<GenerateAdminTokenResponse> GenerateAdminTokenAsync(GenerateAdminTokenRequest request);
    
    /// <summary>
    /// Revoke an admin token.
    /// </summary>
    Task<bool> RevokeAdminTokenAsync(Guid tokenId, string reason);
    
    /// <summary>
    /// Get all admin tokens for a company.
    /// </summary>
    Task<List<AdminTokenDto>> GetTokensByCompanyAsync(Guid companyId, bool includeRevoked = false);
    
    /// <summary>
    /// Get token details by ID.
    /// </summary>
    Task<AdminTokenDto?> GetTokenByIdAsync(Guid tokenId);
    
    #endregion
    
    #region Device Management (Client Admin via Token)
    
    /// <summary>
    /// Validate client admin token and get company info.
    /// Returns null if token is invalid.
    /// </summary>
    Task<ClientAdminContext?> ValidateAdminTokenAsync(string jwtToken);
    
    /// <summary>
    /// Bind a device to a subscription.
    /// Called by client admin using their token.
    /// </summary>
    Task<DeviceBindingResponse> BindDeviceAsync(ClientAdminContext context, BindDeviceRequest request);
    
    /// <summary>
    /// Bind multiple devices to a subscription.
    /// </summary>
    Task<BulkDeviceBindingResponse> BindDevicesBulkAsync(ClientAdminContext context, BulkBindDeviceRequest request);
    
    /// <summary>
    /// Unbind a device from a subscription.
    /// </summary>
    Task<bool> UnbindDeviceAsync(ClientAdminContext context, UnbindDeviceRequest request);
    
    /// <summary>
    /// Get all bound devices for a subscription.
    /// </summary>
    Task<List<BoundDeviceDto>> GetBoundDevicesAsync(ClientAdminContext context, Guid subscriptionId);
    
    /// <summary>
    /// Get device binding summary for all company subscriptions.
    /// </summary>
    Task<CompanyDeviceSummary> GetCompanyDeviceSummaryAsync(ClientAdminContext context);
    
    #endregion
    
    #region Device Replacement (Client Admin via Token)
    
    /// <summary>
    /// Get pending device replacement requests.
    /// </summary>
    Task<List<DeviceReplacementRequestDto>> GetPendingReplacementRequestsAsync(ClientAdminContext context);
    
    /// <summary>
    /// Approve a device replacement request.
    /// </summary>
    Task<DeviceBindingResponse> ApproveReplacementRequestAsync(ClientAdminContext context, Guid requestId);
    
    /// <summary>
    /// Reject a device replacement request.
    /// </summary>
    Task<bool> RejectReplacementRequestAsync(ClientAdminContext context, Guid requestId, string reason);
    
    /// <summary>
    /// Create a device replacement request when max devices reached.
    /// </summary>
    Task<CreateReplacementResponse> CreateReplacementRequestAsync(ClientAdminContext context, CreateReplacementRequest request);
    
    #endregion
}
