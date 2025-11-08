using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Licensing;
using Domain.Entities.Common;

namespace Application.Services;

/// <summary>
/// Service interface for managing company entities (CRUD operations).
/// </summary>
public interface ICompanyService
{
    /// <summary>
    /// Creates a new company.
    /// </summary>
    Task<CompanyDto> CreateCompanyAsync(CreateCompanyDto dto);

    /// <summary>
    /// Gets all companies with pagination.
    /// </summary>
    Task<(IEnumerable<CompanyDto> Companies, PaginationMetadata Meta)> GetAllCompaniesAsync(
        int page = 1, 
        int pageSize = 10, 
        string? search = null);

    /// <summary>
    /// Gets a company by ID.
    /// </summary>
    Task<CompanyDto?> GetCompanyByIdAsync(Guid id);

    /// <summary>
    /// Updates a company.
    /// </summary>
    Task<CompanyDto?> UpdateCompanyAsync(Guid id, UpdateCompanyDto dto);

    /// <summary>
    /// Deletes a company (soft delete).
    /// </summary>
    Task<bool> DeleteCompanyAsync(Guid id);

    /// <summary>
    /// Performs bulk delete on multiple companies.
    /// </summary>
    Task<BulkOperationResponse> BulkDeleteAsync(List<Guid> companyIds);

    /// <summary>
    /// Performs bulk update on multiple companies.
    /// </summary>
    Task<BulkOperationResponse> BulkUpdateAsync(List<Guid> companyIds, UpdateCompanyDto updateDto);
}

