using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Licensing;

namespace Application.Services;

/// <summary>
/// Service interface for managing company custom fields.
/// </summary>
public interface ICompanyCustomFieldService
{
    /// <summary>
    /// Creates a custom field for a company.
    /// </summary>
    Task<CompanyCustomFieldDto> CreateFieldAsync(CreateCompanyCustomFieldDto request);

    /// <summary>
    /// Gets all custom fields for a company.
    /// </summary>
    Task<IEnumerable<CompanyCustomFieldDto>> GetFieldsByCompanyIdAsync(Guid companyId);

    /// <summary>
    /// Gets a custom field by ID.
    /// </summary>
    Task<CompanyCustomFieldDto?> GetFieldByIdAsync(Guid fieldId);

    /// <summary>
    /// Updates a custom field.
    /// </summary>
    Task<CompanyCustomFieldDto> UpdateFieldAsync(Guid fieldId, UpdateCompanyCustomFieldDto request);

    /// <summary>
    /// Deletes a custom field.
    /// </summary>
    Task DeleteFieldAsync(Guid fieldId);
}



