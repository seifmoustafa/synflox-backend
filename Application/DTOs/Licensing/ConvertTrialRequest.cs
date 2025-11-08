using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Licensing;

/// <summary>
/// Request DTO for converting a trial to an active subscription.
/// </summary>
public class ConvertTrialRequest
{
    [Required(ErrorMessage = "Expiry date is required")]
    public DateTime ExpiryDate { get; set; }
}

