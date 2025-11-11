using System;

namespace Application.DTOs.Company;

public class UpdateCompanyByIdRequest
{
    public Guid CompanyId { get; set; }
    public UpdateCompanyDto UpdateData { get; set; } = new();
}
