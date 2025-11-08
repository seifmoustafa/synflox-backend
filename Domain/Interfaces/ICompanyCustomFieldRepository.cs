using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Licensing;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for CompanyCustomField entities.
/// </summary>
public interface ICompanyCustomFieldRepository : IBaseRepository<Guid, CompanyCustomField>
{
    /// <summary>
    /// Gets all custom fields for a company.
    /// </summary>
    Task<IEnumerable<CompanyCustomField>> GetByCompanyIdAsync(Guid companyId);

    /// <summary>
    /// Gets a custom field by company ID and field name.
    /// </summary>
    Task<CompanyCustomField?> GetByCompanyIdAndFieldNameAsync(Guid companyId, string fieldName);
}



