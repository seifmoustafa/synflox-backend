using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Admin;

/// <summary>
/// Request DTO for deleting all admins - requires explicit confirmation
/// </summary>
public class DeleteAllAdminsRequest
{
    /// <summary>
    /// Must be exactly "DELETE_ALL_ADMINS" to confirm deletion
    /// </summary>
    [Required]
    [RegularExpression("^DELETE_ALL_ADMINS$", ErrorMessage = "Confirmation text must be exactly 'DELETE_ALL_ADMINS'")]
    public required string ConfirmationText { get; set; }
}
