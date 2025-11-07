using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Licensing;

public class ActivateCompanyRequest
{
    [Required(ErrorMessage = "Expiry date is required")]
    public DateTime ExpiryDate { get; set; }
}

