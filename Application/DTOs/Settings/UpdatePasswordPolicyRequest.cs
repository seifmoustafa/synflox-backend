using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Settings;

public class UpdatePasswordPolicyRequest
{
    [Range(6, 128, ErrorMessage = "Minimum length must be between 6 and 128")]
    public int? MinLength { get; set; }

    public bool? RequireUppercase { get; set; }
    public bool? RequireLowercase { get; set; }
    public bool? RequireNumbers { get; set; }
    public bool? RequireSpecialChars { get; set; }

    [Range(1, 3650, ErrorMessage = "Max age must be between 1 and 3650 days")]
    public int? MaxAgeDays { get; set; }

    [Range(1, 20, ErrorMessage = "Prevent reuse count must be between 1 and 20")]
    public int? PreventReuseCount { get; set; }

    public bool? IsActive { get; set; }
}

