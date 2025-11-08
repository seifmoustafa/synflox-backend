using System;
using System.Threading.Tasks;
using Domain.Entities.Licensing;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for CompanyGroupMember entities.
/// </summary>
public interface ICompanyGroupMemberRepository : IBaseRepository<Guid, CompanyGroupMember>
{
    /// <summary>
    /// Checks if a company is in a group.
    /// </summary>
    Task<bool> IsCompanyInGroupAsync(Guid companyId, Guid groupId);

    /// <summary>
    /// Removes a company from a group.
    /// </summary>
    Task RemoveCompanyFromGroupAsync(Guid companyId, Guid groupId);

    /// <summary>
    /// Removes all companies from a group.
    /// </summary>
    Task RemoveAllCompaniesFromGroupAsync(Guid groupId);
}



