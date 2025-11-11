using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Licensing;

public class ExtendCompanyRequest
{
    public Guid CompanyId { get; set; }
    
    [Required(ErrorMessage = "New expiry date is required")]
    public DateTime NewExpiryDate { get; set; }
}

