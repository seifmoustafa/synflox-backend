using Domain.Entities.Reporting;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for ReportDefinition entity operations.
/// </summary>
public class ReportDefinitionRepository : BaseRepository<Guid, ReportDefinition>, IReportDefinitionRepository
{
    public ReportDefinitionRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ReportDefinition>> GetActiveReportsAsync()
    {
        return await _context.ReportDefinitions
            .Where(r => r.IsActive && !r.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<ReportDefinition>> GetPreBuiltReportsAsync()
    {
        return await _context.ReportDefinitions
            .Where(r => r.IsPreBuilt && !r.IsDeleted)
            .ToListAsync();
    }

    public async Task<ReportDefinition?> GetByReportTypeAsync(string reportType)
    {
        return await _context.ReportDefinitions
            .FirstOrDefaultAsync(r => r.ReportType == reportType && !r.IsDeleted);
    }
}

