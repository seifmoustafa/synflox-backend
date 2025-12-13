using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Admin;

public class ChangePasswordByIdRequest
{
    public Guid AdminId { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])[A-Za-z\d@$!%*?&#]+$", 
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character (@$!%*?&#)")]
    public string NewPassword { get; set; } = string.Empty;
}
