using System.Threading.Tasks;
using Application.DTOs.Settings;

namespace Application.Services;

/// <summary>
/// Service interface for managing password policies.
/// </summary>
public interface IPasswordPolicyService
{
    /// <summary>
    /// Gets the active password policy.
    /// </summary>
    Task<PasswordPolicyDto?> GetActivePolicyAsync();

    /// <summary>
    /// Validates a password against the active policy.
    /// </summary>
    Task<PasswordValidationResult> ValidatePasswordAsync(string password, Guid? adminId = null);

    /// <summary>
    /// Updates the password policy. Only one policy can be active at a time.
    /// </summary>
    Task<PasswordPolicyDto> UpdatePolicyAsync(UpdatePasswordPolicyRequest request);

    /// <summary>
    /// Checks if a password has expired for a given admin.
    /// </summary>
    Task<bool> IsPasswordExpiredAsync(Guid adminId);

    /// <summary>
    /// Records a password change in the password history.
    /// </summary>
    Task RecordPasswordChangeAsync(Guid adminId, string passwordHash);
}

