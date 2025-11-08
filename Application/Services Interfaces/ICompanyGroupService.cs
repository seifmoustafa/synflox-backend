using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Licensing;
using Domain.Entities.Common;

namespace Application.Services;

/// <summary>
/// Service interface for managing company groups.
/// </summary>
public interface ICompanyGroupService
{
    /// <summary>
    /// Creates a new company group.
    /// </summary>
    Task<CompanyGroupDto> CreateGroupAsync(CreateCompanyGroupDto request);

    /// <summary>
    /// Gets all company groups.
    /// </summary>
    Task<(IEnumerable<CompanyGroupDto> Groups, PaginationMetadata Meta)> GetAllGroupsAsync(
        int page = 1,
        int pageSize = 10);

    /// <summary>
    /// Gets a company group by ID.
    /// </summary>
    Task<CompanyGroupDto?> GetGroupByIdAsync(Guid groupId);

    /// <summary>
    /// Updates a company group.
    /// </summary>
    Task<CompanyGroupDto> UpdateGroupAsync(Guid groupId, UpdateCompanyGroupDto request);

    /// <summary>
    /// Deletes a company group.
    /// </summary>
    Task DeleteGroupAsync(Guid groupId);

    /// <summary>
    /// Adds companies to a group.
    /// </summary>
    Task AddCompaniesToGroupAsync(Guid groupId, IEnumerable<Guid> companyIds);

    /// <summary>
    /// Removes companies from a group.
    /// </summary>
    Task RemoveCompaniesFromGroupAsync(Guid groupId, IEnumerable<Guid> companyIds);

    /// <summary>
    /// Gets companies in a group.
    /// </summary>
    Task<(IEnumerable<CompanyDto> Companies, PaginationMetadata Meta)> GetCompaniesInGroupAsync(
        Guid groupId,
        int page = 1,
        int pageSize = 10);

    /// <summary>
    /// Gets groups for a company.
    /// </summary>
    Task<IEnumerable<CompanyGroupDto>> GetGroupsForCompanyAsync(Guid companyId);

    /// <summary>
    /// Performs bulk operations on companies in a group.
    /// </summary>
    Task BulkActivateByGroupAsync(Guid groupId);
    Task BulkSuspendByGroupAsync(Guid groupId);
    Task BulkResumeByGroupAsync(Guid groupId);
    Task BulkExtendByGroupAsync(Guid groupId, DateTime expiryDate);
}



