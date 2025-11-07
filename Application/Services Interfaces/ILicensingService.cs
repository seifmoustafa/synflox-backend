using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Licensing;
using Domain.Entities.Common;

namespace Application.Services;

/// <summary>
/// Service interface for managing company licensing and subscriptions.
/// </summary>
public interface ILicensingService
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
    /// Activates a company subscription with the specified expiry date.
    /// </summary>
    Task<CompanyDto> ActivateCompanyAsync(Guid id, DateTime expiryDate);

    /// <summary>
    /// Suspends a company subscription.
    /// </summary>
    Task<CompanyDto> SuspendCompanyAsync(Guid id);

    /// <summary>
    /// Resumes a suspended company subscription.
    /// </summary>
    Task<CompanyDto> ResumeCompanyAsync(Guid id);

    /// <summary>
    /// Extends the subscription expiry date for a company.
    /// </summary>
    Task<CompanyDto> ExtendCompanyAsync(Guid id, DateTime newExpiryDate);

    /// <summary>
    /// Checks the current status of a company subscription.
    /// </summary>
    Task<CompanyStatusResponse> CheckCompanyStatusAsync(Guid id);

    /// <summary>
    /// Generates a license key for a company (for offline systems).
    /// </summary>
    Task<string> GenerateLicenseKeyAsync(Guid companyId);

    /// <summary>
    /// Regenerates a license key for a company.
    /// </summary>
    Task<string> RegenerateLicenseKeyAsync(Guid companyId);

    /// <summary>
    /// Validates a license key and returns the validation result.
    /// Used by offline systems to check their license status.
    /// </summary>
    Task<LicenseKeyValidationResponse> ValidateLicenseKeyAsync(string licenseKey);
}

