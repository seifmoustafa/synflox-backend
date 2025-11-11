using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Company;
using Application.DTOs.Licensing;
using Domain.Entities.Common;

namespace Application.Services;

/// <summary>
/// Service interface for managing company licensing and subscriptions.
/// This service handles only licensing operations (activate, suspend, extend, status, license keys).
/// For company CRUD operations, use ICompanyService.
/// </summary>
public interface ILicensingService
{
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

