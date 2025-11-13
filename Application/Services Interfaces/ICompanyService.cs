using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Company;
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
    Task<CompanyDto?> GetCompanyByIdAsync(GetCompanyByIdRequest request);

    /// <summary>
    /// Updates a company.
    /// </summary>
    Task<CompanyDto?> UpdateCompanyAsync(UpdateCompanyByIdRequest request);

    /// <summary>
    /// Deletes a company (soft delete).
    /// </summary>
    Task<bool> DeleteCompanyAsync(DeleteCompanyRequest request);

    /// <summary>
    /// Activates a company.
    /// </summary>
    Task<bool> ActivateCompanyAsync(CompanyActionRequest request);

    /// <summary>
    /// Deactivates a company.
    /// </summary>
    Task<bool> DeactivateCompanyAsync(CompanyActionRequest request);

    /// <summary>
    /// Bulk delete multiple companies.
    /// </summary>
    Task<BulkOperationResult> BulkDeleteCompaniesAsync(BulkCompanyActionRequest request);

    /// <summary>
    /// Bulk activate multiple companies.
    /// </summary>
    Task<BulkOperationResult> BulkActivateCompaniesAsync(BulkCompanyActionRequest request);

    /// <summary>
    /// Bulk deactivate multiple companies.
    /// </summary>
    Task<BulkOperationResult> BulkDeactivateCompaniesAsync(BulkCompanyActionRequest request);
}

