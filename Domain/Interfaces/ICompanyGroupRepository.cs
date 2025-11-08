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
    /// Gets companies in a group.
    /// </summary>
    Task<IEnumerable<Company>> GetCompaniesInGroupAsync(Guid groupId);

    /// <summary>
    /// Gets groups for a company.
    /// </summary>
    Task<IEnumerable<CompanyGroup>> GetGroupsForCompanyAsync(Guid companyId);
}



