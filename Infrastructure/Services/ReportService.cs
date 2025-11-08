using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Reporting;
using Application.Services;
using AutoMapper;
using Domain.Entities.Licensing;
using Domain.Entities.Reporting;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ISubscriptionHistoryRepository _historyRepository;
    private readonly ICompanyUsageLogRepository _usageLogRepository;
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IProjectModuleRepository _projectModuleRepository;
    private readonly IPlanProjectModuleRepository _planProjectModuleRepository;
    private readonly IReportDefinitionRepository _reportDefinitionRepository;
    private readonly ApplicationDBContext _context;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        ICompanyRepository companyRepository,
        ISubscriptionHistoryRepository historyRepository,
        ICompanyUsageLogRepository usageLogRepository,
        ISubscriptionPlanRepository planRepository,
        IProjectModuleRepository projectModuleRepository,
        IPlanProjectModuleRepository planProjectModuleRepository,
        IReportDefinitionRepository reportDefinitionRepository,
        ApplicationDBContext context,
        IMapper mapper,
        ILocalizationService localizer,
        ILogger<ReportService> logger)
    {
        _companyRepository = companyRepository;
        _historyRepository = historyRepository;
        _usageLogRepository = usageLogRepository;
        _planRepository = planRepository;
        _projectModuleRepository = projectModuleRepository;
        _planProjectModuleRepository = planProjectModuleRepository;
        _reportDefinitionRepository = reportDefinitionRepository;
        _context = context;
        _mapper = mapper;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IEnumerable<ReportDefinitionDto>> GetAvailableReportsAsync()
    {
        // Get all active reports from database (pre-built and custom)
        var reports = await _reportDefinitionRepository.GetActiveReportsAsync();
        
        // Map entities to DTOs using AutoMapper (handles ID encryption automatically)
        return _mapper.Map<IEnumerable<ReportDefinitionDto>>(reports);
    }

    public async Task<ReportResultDto> GenerateReportAsync(
        Guid reportId,
        Dictionary<string, object>? parameters = null)
    {
        // Get report definition from database
        var reportDefinition = await _reportDefinitionRepository.GetByIdAsync(reportId, null);
        
        if (reportDefinition == null || reportDefinition.IsDeleted)
        {
            throw new Domain.Exceptions.NotFoundException(_localizer["Report.NotFound"]);
        }

        if (!reportDefinition.IsActive)
        {
            throw new Domain.Exceptions.BadRequestException(_localizer["Report.Inactive"]);
        }

        // Generate report based on report type, passing report name from database
        var result = await GeneratePreBuiltReportAsync(reportDefinition.ReportType, parameters);
        result.ReportName = reportDefinition.Name; // Use name from database instead of hardcoded value
        return result;
    }

    public async Task<ReportResultDto> GeneratePreBuiltReportAsync(
        string reportType,
        Dictionary<string, object>? parameters = null)
    {
        return reportType switch
        {
            "SubscriptionExpiry" => await GenerateSubscriptionExpiryReportAsync(parameters),
            "StatusSummary" => await GenerateStatusSummaryReportAsync(),
            "UsageAnalytics" => await GenerateUsageAnalyticsReportAsync(parameters),
            "TrialConversion" => await GenerateTrialConversionReportAsync(),
            "ModuleUsage" => await GenerateModuleUsageReportAsync(parameters),
            _ => throw new Domain.Exceptions.BadRequestException(_localizer["Report.InvalidType"])
        };
    }

    private async Task<ReportResultDto> GenerateSubscriptionExpiryReportAsync(Dictionary<string, object>? parameters)
    {
        var days = parameters?.ContainsKey("days") == true 
            ? Convert.ToInt32(parameters["days"]) 
            : 30;

        var cutoffDate = DateTime.UtcNow.AddDays(days);
        var (companies, _) = await _companyRepository.GetAllAsync(null, 1, int.MaxValue);
        var companiesList = companies.Cast<Company>().ToList();

        var expiringCompanies = companiesList
            .Where(c => c.ExpiryDate.HasValue 
                   && c.ExpiryDate.Value >= DateTime.UtcNow 
                   && c.ExpiryDate.Value <= cutoffDate
                   && !c.IsDeleted)
            .Select(c => new Dictionary<string, object>
            {
                { "CompanyId", c.Id },
                { "CompanyName", c.Name },
                { "ExpiryDate", c.ExpiryDate!.Value },
                { "DaysUntilExpiry", (c.ExpiryDate.Value - DateTime.UtcNow).Days },
                { "ContactEmail", c.ContactEmail ?? "" },
                { "ContactPhone", c.ContactPhone ?? "" }
            })
            .ToList();

        return new ReportResultDto
        {
            ReportName = string.Empty, // Will be set from ReportDefinition.Name in GenerateReportAsync
            Data = expiringCompanies,
            Summary = new Dictionary<string, object>
            {
                { "TotalExpiring", expiringCompanies.Count },
                { "Days", days }
            }
        };
    }

    private async Task<ReportResultDto> GenerateStatusSummaryReportAsync()
    {
        var (companies, _) = await _companyRepository.GetAllAsync(null, 1, int.MaxValue);
        var companiesList = companies.Cast<Company>().ToList();

        var activeCount = 0;
        var expiredCount = 0;
        var suspendedCount = 0;
        var trialCount = 0;

        foreach (var company in companiesList.Where(c => !c.IsDeleted))
        {
            if (company.IsTrial)
                trialCount++;

            var status = CalculateLicenseStatus(company);
            switch (status)
            {
                case LicenseStatus.Active:
                    activeCount++;
                    break;
                case LicenseStatus.Expired:
                    expiredCount++;
                    break;
                case LicenseStatus.Suspended:
                    suspendedCount++;
                    break;
            }
        }

        return new ReportResultDto
        {
            ReportName = string.Empty, // Will be set from ReportDefinition.Name in GenerateReportAsync
            Data = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { { "Status", "Active" }, { "Count", activeCount } },
                new Dictionary<string, object> { { "Status", "Expired" }, { "Count", expiredCount } },
                new Dictionary<string, object> { { "Status", "Suspended" }, { "Count", suspendedCount } },
                new Dictionary<string, object> { { "Status", "Trial" }, { "Count", trialCount } }
            },
            Summary = new Dictionary<string, object>
            {
                { "TotalCompanies", companiesList.Count(c => !c.IsDeleted) },
                { "Active", activeCount },
                { "Expired", expiredCount },
                { "Suspended", suspendedCount },
                { "Trial", trialCount }
            }
        };
    }

    private async Task<ReportResultDto> GenerateUsageAnalyticsReportAsync(Dictionary<string, object>? parameters)
    {
        var fromDate = parameters?.ContainsKey("fromDate") == true 
            ? Convert.ToDateTime(parameters["fromDate"]) 
            : DateTime.UtcNow.AddDays(-30);
        var toDate = parameters?.ContainsKey("toDate") == true 
            ? Convert.ToDateTime(parameters["toDate"]) 
            : DateTime.UtcNow;

        var logs = await _usageLogRepository.GetAllAsync(fromDate, toDate, 0, 10000);
        var logsList = logs.ToList();

        var usageByCompany = logsList
            .GroupBy(l => l.CompanyId)
            .Select(g => new Dictionary<string, object>
            {
                { "CompanyId", g.Key },
                { "TotalRequests", g.Count() },
                { "AverageResponseTime", g.Average(l => l.ResponseTimeMs) },
                { "LastRequestTime", g.Max(l => l.RequestTimestamp) }
            })
            .ToList();

        return new ReportResultDto
        {
            ReportName = string.Empty, // Will be set from ReportDefinition.Name in GenerateReportAsync
            Data = usageByCompany,
            Summary = new Dictionary<string, object>
            {
                { "TotalRequests", logsList.Count },
                { "UniqueCompanies", usageByCompany.Count },
                { "AverageResponseTime", logsList.Any() ? logsList.Average(l => l.ResponseTimeMs) : 0 },
                { "FromDate", fromDate },
                { "ToDate", toDate }
            }
        };
    }

    private async Task<ReportResultDto> GenerateTrialConversionReportAsync()
    {
        var (companies, _) = await _companyRepository.GetAllAsync(null, 1, int.MaxValue);
        var companiesList = companies.Cast<Company>().ToList();

        var trialCompanies = companiesList
            .Where(c => c.IsTrial && !c.IsDeleted)
            .Select(c => new Dictionary<string, object>
            {
                { "CompanyId", c.Id },
                { "CompanyName", c.Name },
                { "TrialEndDate", c.TrialEndDate?.ToString() ?? "" },
                { "DaysRemaining", c.TrialEndDate.HasValue 
                    ? (c.TrialEndDate.Value - DateTime.UtcNow).Days 
                    : 0 }
            })
            .ToList();

        var convertedCount = companiesList.Count(c => !c.IsTrial && !c.IsDeleted && c.CreatedTimestamp < DateTime.UtcNow.AddDays(-7));

        return new ReportResultDto
        {
            ReportName = string.Empty, // Will be set from ReportDefinition.Name in GenerateReportAsync
            Data = trialCompanies,
            Summary = new Dictionary<string, object>
            {
                { "ActiveTrials", trialCompanies.Count },
                { "ConvertedToPaid", convertedCount },
                { "ConversionRate", companiesList.Any() ? (double)convertedCount / companiesList.Count * 100 : 0 }
            }
        };
    }

    private async Task<ReportResultDto> GenerateModuleUsageReportAsync(Dictionary<string, object>? parameters)
    {
        var (companies, _) = await _companyRepository.GetAllAsync(null, 1, int.MaxValue);
        var companiesList = companies.Cast<Company>().Where(c => !c.IsDeleted && c.SubscriptionPlanId.HasValue).ToList();

        var moduleUsage = new Dictionary<string, int>();

        foreach (var company in companiesList)
        {
            var planModules = await _planProjectModuleRepository.GetByPlanIdAsync(company.SubscriptionPlanId!.Value);
            foreach (var planModule in planModules)
            {
                var module = await _projectModuleRepository.GetByIdAsync(planModule.ProjectModuleId, null);
                if (module != null && !module.IsDeleted)
                {
                    // Get project and module names via navigation or separate queries
                    var project = await _context.Set<Project>().FirstOrDefaultAsync(p => p.Id == module.ProjectId && !p.IsDeleted);
                    var moduleEntity = await _context.Set<Module>().FirstOrDefaultAsync(m => m.Id == module.ModuleId && !m.IsDeleted);
                    var moduleName = $"{(project?.Name ?? "Unknown")}.{(moduleEntity?.Name ?? "Unknown")}";
                    if (!moduleUsage.ContainsKey(moduleName))
                        moduleUsage[moduleName] = 0;
                    moduleUsage[moduleName]++;
                }
            }
        }

        var data = moduleUsage.Select(kvp => new Dictionary<string, object>
        {
            { "Module", kvp.Key },
            { "CompanyCount", kvp.Value }
        }).ToList();

        return new ReportResultDto
        {
            ReportName = string.Empty, // Will be set from ReportDefinition.Name in GenerateReportAsync
            Data = data,
            Summary = new Dictionary<string, object>
            {
                { "TotalModules", moduleUsage.Count },
                { "TotalCompanies", companiesList.Count }
            }
        };
    }

    private LicenseStatus CalculateLicenseStatus(Company company)
    {
        var now = DateTime.UtcNow;

        if (company.IsExpired)
            return LicenseStatus.Expired;

        if (!company.IsActive)
            return LicenseStatus.Suspended;

        if (company.ExpiryDate.HasValue && company.ExpiryDate.Value < now)
            return LicenseStatus.Expired;

        return LicenseStatus.Active;
    }
}

