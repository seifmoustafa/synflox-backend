using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Licensing;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for CompanyGroup entities.
/// </summary>
public interface ICompanyGroupRepository : IBaseRepository<Guid, CompanyGroup>
{
    /// <summary>
    /// Gets companies in a group with pagination.
    /// </summary>
    Task<(IEnumerable<Company> Companies, int TotalCount)> GetCompaniesInGroupAsync(
        Guid groupId,
        int page = 1,
        int pageSize = 10);

    /// <summary>
    /// Gets all companies in a group (without pagination, for bulk operations).
    /// </summary>
    Task<IEnumerable<Company>> GetAllCompaniesInGroupAsync(Guid groupId);

    /// <summary>
    /// Gets groups for a company.
    /// </summary>
    Task<IEnumerable<CompanyGroup>> GetGroupsForCompanyAsync(Guid companyId);

    /// <summary>
    /// Gets the count of companies in a group.
    /// </summary>
    Task<int> GetCompanyCountInGroupAsync(Guid groupId);

    /// <summary>
    /// Gets the count of companies for multiple groups (batch query for efficiency).
    /// </summary>
    Task<Dictionary<Guid, int>> GetCompanyCountsForGroupsAsync(IEnumerable<Guid> groupIds);
}



