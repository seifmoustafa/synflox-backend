using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Licensing;

/// <summary>
/// Request DTO for starting a trial period.
/// </summary>
public class StartTrialRequest
{
    [Required(ErrorMessage = "Trial days is required")]
    [Range(1, 365, ErrorMessage = "Trial days must be between 1 and 365")]
    public int TrialDays { get; set; }
}

