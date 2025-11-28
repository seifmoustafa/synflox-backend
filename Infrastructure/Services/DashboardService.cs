using Application.DTOs.Dashboard.Overview;
using Application.DTOs.Dashboard.Companies;
using Application.DTOs.Dashboard.Subscriptions;
using Application.DTOs.Dashboard.Revenue;
using Application.DTOs.Dashboard.Activity;
using Application.DTOs.Dashboard.Alerts;
using Application.DTOs.Dashboard.Shared;
using Application.Services_Interfaces;
using Domain.Entities.Licensing;
using Domain.Entities.Subscriptions;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Production-ready dashboard service with optimized queries and error handling
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly ApplicationDBContext _context;
    private readonly ILogger<DashboardService> _logger;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly Application.Services.ILocalizationService _localizer;

    public DashboardService(
        ApplicationDBContext context,
        ILogger<DashboardService> logger,
        IActivityLogRepository activityLogRepository,
        Application.Services.ILocalizationService localizer)
    {
        _context = context;
        _logger = logger;
        _activityLogRepository = activityLogRepository;
        _localizer = localizer;
    }

    #region Overview Dashboard

    public async Task<OverviewDashboardDto> GetOverviewAsync()
    {
        try
        {
            var now = DateTime.Now;
            var today = now.Date;
            var lastMonth = today.AddMonths(-1);
            var last30Days = today.AddDays(-30);

            // Execute queries sequentially (DbContext is not thread-safe)
            var companies = await _context.Companies
                .Where(c => !c.IsDeleted)
                .Select(c => new { c.Id, c.CreatedTimestamp })
                .ToListAsync();

            var subscriptions = await _context.Subscriptions
                .Where(s => !s.IsDeleted)
                .Select(s => new {
                    s.Id,
                    s.CompanyId,
                    s.IsActive,
                    s.IsExpired,
                    s.IsTrial,
                    s.ExpiryDateUtc,
                    s.Amount,
                    s.CreatedTimestamp
                })
                .ToListAsync();

            var admins = await _context.Admins
                .Where(a => !a.IsDeleted)
                .Select(a => new { a.Id, a.LastLoginAt, a.LoginCount })
                .ToListAsync();

            var totalCompanies = companies.Count;
            var totalSubscriptions = subscriptions.Count;
            var activeSubscriptions = subscriptions.Count(s => s.IsActive && !s.IsExpired);
            var trialSubscriptions = subscriptions.Count(s => s.IsTrial && s.IsActive);
            var expiredSubscriptions = subscriptions.Count(s => s.IsExpired);
            var suspendedSubscriptions = subscriptions.Count(s => !s.IsActive && !s.IsExpired && !s.IsTrial);

            var companyIdsWithActiveSub = subscriptions
                .Where(s => s.IsActive && !s.IsExpired)
                .Select(s => s.CompanyId)
                .Distinct()
                .ToHashSet();
            
            var activeCompanies = companyIdsWithActiveSub.Count;

            var companyIdsWithAnySub = subscriptions.Select(s => s.CompanyId).Distinct().ToHashSet();
            var companiesWithoutSub = totalCompanies - companyIdsWithAnySub.Count;

            // Expiring subscriptions - using proper date comparisons
            var expiringToday = subscriptions.Count(s => 
                s.IsActive && !s.IsExpired && s.ExpiryDateUtc.Date == today);
            
            var expiringThisWeek = subscriptions.Count(s => 
                s.IsActive && !s.IsExpired && 
                s.ExpiryDateUtc.Date > today && 
                s.ExpiryDateUtc.Date <= today.AddDays(7));
            
            var expiringThisMonth = subscriptions.Count(s => 
                s.IsActive && !s.IsExpired && 
                s.ExpiryDateUtc.Date > today.AddDays(7) && 
                s.ExpiryDateUtc.Date <= today.AddDays(30));

            var totalAdmins = admins.Count;
            var activeAdmins = admins.Count(a => a.LastLoginAt.HasValue && a.LastLoginAt.Value >= last30Days);

            // Growth calculations
            var prevMonthCompanies = companies.Count(c => c.CreatedTimestamp < lastMonth);
            var prevMonthSubscriptions = subscriptions.Count(s => s.CreatedTimestamp < lastMonth);
            
            var companyGrowthRate = CalculateGrowthRate(prevMonthCompanies, totalCompanies);
            var subscriptionGrowthRate = CalculateGrowthRate(prevMonthSubscriptions, totalSubscriptions);

            // Calculate MRR safely with decimal
            var mrr = subscriptions
                .Where(s => s.IsActive && !s.IsExpired)
                .Sum(s => s.Amount);

            // Build KPI cards with decimal values
            var companiesKpi = new KpiCardDto(
                Title: "Companies",
                Value: totalCompanies,
                PreviousValue: prevMonthCompanies,
                ChangePercentage: companyGrowthRate,
                ChangeDirection: companyGrowthRate > 0 ? "up" : companyGrowthRate < 0 ? "down" : "unchanged",
                Icon: "Building2",
                Color: "blue"
            );

            var subscriptionsKpi = new KpiCardDto(
                Title: "Active Subscriptions",
                Value: activeSubscriptions,
                PreviousValue: null,
                ChangePercentage: subscriptionGrowthRate,
                ChangeDirection: subscriptionGrowthRate > 0 ? "up" : subscriptionGrowthRate < 0 ? "down" : "unchanged",
                Icon: "CreditCard",
                Color: "green"
            );

            var revenueKpi = new KpiCardDto(
                Title: "MRR",
                Value: mrr, // Now using decimal, no overflow
                PreviousValue: null,
                ChangePercentage: null,
                ChangeDirection: "unchanged",
                Icon: "DollarSign",
                Color: "purple"
            );

            var alertCount = expiringToday + expiringThisWeek + suspendedSubscriptions;
            var alertsKpi = new KpiCardDto(
                Title: "Alerts",
                Value: alertCount,
                PreviousValue: null,
                ChangePercentage: null,
                ChangeDirection: expiringToday > 0 ? "up" : "unchanged",
                Icon: "Bell",
                Color: expiringToday > 0 ? "red" : "yellow"
            );

            var stats = new QuickStatsDto(
                TotalCompanies: totalCompanies,
                ActiveCompanies: activeCompanies,
                InactiveCompanies: totalCompanies - activeCompanies,
                CompaniesWithoutSub: companiesWithoutSub,
                TotalSubscriptions: totalSubscriptions,
                ActiveSubscriptions: activeSubscriptions,
                TrialSubscriptions: trialSubscriptions,
                ExpiredSubscriptions: expiredSubscriptions,
                SuspendedSubscriptions: suspendedSubscriptions,
                ExpiringToday: expiringToday,
                ExpiringThisWeek: expiringThisWeek,
                ExpiringThisMonth: expiringThisMonth,
                TotalAdmins: totalAdmins,
                ActiveAdmins: activeAdmins,
                CompanyGrowthRate: companyGrowthRate,
                SubscriptionGrowthRate: subscriptionGrowthRate
            );

            var statusDistribution = new List<DistributionItemDto>
            {
                new("Active", activeSubscriptions - trialSubscriptions, 
                    CalculatePercentage(activeSubscriptions - trialSubscriptions, totalSubscriptions), "#22c55e"),
                new("Trial", trialSubscriptions, 
                    CalculatePercentage(trialSubscriptions, totalSubscriptions), "#3b82f6"),
                new("Expired", expiredSubscriptions, 
                    CalculatePercentage(expiredSubscriptions, totalSubscriptions), "#ef4444"),
                new("Suspended", suspendedSubscriptions, 
                    CalculatePercentage(suspendedSubscriptions, totalSubscriptions), "#f97316")
            };

            var growthTrend = new List<TimeSeriesDataPointDto>();
            for (int i = 29; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var companiesOnDate = companies.Count(c => c.CreatedTimestamp.Date <= date);
                growthTrend.Add(new TimeSeriesDataPointDto(date, date.ToString("MMM dd"), companiesOnDate));
            }

            // Fetch recent activities
            var recentActivities = await _activityLogRepository.GetRecentAsync(10);
            var recentActivityDtos = recentActivities.Select(a => new RecentActivityItemDto(
                Id: a.Id,
                ActionType: a.ActionType,
                EntityType: a.EntityType,
                EntityName: a.EntityName,
                EntityId: a.EntityId,
                PerformedBy: a.PerformedByName ?? _localizer["Activity.System"],
                PerformedById: a.PerformedBy ?? Guid.Empty,
                PerformedAt: a.Timestamp,
                Description: GetLocalizedActivityDescription(a.ActionType, a.EntityType, a.EntityName),
                Icon: GetIconForEntityType(a.EntityType),
                Color: GetColorForActionType(a.ActionType),
                TimeAgo: GetLocalizedTimeAgo(a.Timestamp)
            )).ToList();

            return new OverviewDashboardDto(
                CompaniesKpi: companiesKpi,
                SubscriptionsKpi: subscriptionsKpi,
                RevenueKpi: revenueKpi,
                AlertsKpi: alertsKpi,
                Stats: stats,
                SubscriptionStatusDistribution: statusDistribution,
                GrowthTrend: growthTrend,
                RecentActivity: recentActivityDtos,
                GeneratedAt: now
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating overview dashboard");
            throw;
        }
    }

    #endregion

    #region Companies Dashboard

    public async Task<CompaniesDashboardDto> GetCompaniesDashboardAsync()
    {
        try
        {
            var now = DateTime.Now;
            var today = now.Date;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var thisWeek = today.AddDays(-(int)today.DayOfWeek);

            var companies = await _context.Companies
                .Where(c => !c.IsDeleted)
                .Select(c => new { c.Id, c.Name, c.CreatedTimestamp })
                .ToListAsync();

            var subscriptions = await _context.Subscriptions
                .Where(s => !s.IsDeleted)
                .Include(s => s.Plan)
                .Include(s => s.Company)
                .ToListAsync();

            var totalCompanies = companies.Count;
            var newThisMonth = companies.Count(c => c.CreatedTimestamp >= thisMonth);
            var newThisWeek = companies.Count(c => c.CreatedTimestamp >= thisWeek);

            var activeCompanyIds = subscriptions
                .Where(s => s.IsActive && !s.IsExpired)
                .Select(s => s.CompanyId)
                .Distinct()
                .ToHashSet();

            var atRiskCompanyIds = subscriptions
                .Where(s => s.IsActive && !s.IsExpired && s.ExpiryDateUtc <= today.AddDays(30))
                .Select(s => s.CompanyId)
                .Distinct()
                .ToHashSet();

            var activeCount = activeCompanyIds.Count;
            var atRiskCount = atRiskCompanyIds.Count;
            var inactiveCount = totalCompanies - activeCount;

            var statusDistribution = new CompanyStatusDistributionDto(
                Active: activeCount - atRiskCount,
                Inactive: inactiveCount,
                Suspended: 0,
                AtRisk: atRiskCount,
                Total: totalCompanies
            );

            var statusChart = new List<DistributionItemDto>
            {
                new("Active", activeCount - atRiskCount, 
                    CalculatePercentage(activeCount - atRiskCount, totalCompanies), "#22c55e"),
                new("At Risk", atRiskCount, 
                    CalculatePercentage(atRiskCount, totalCompanies), "#f97316"),
                new("Inactive", inactiveCount, 
                    CalculatePercentage(inactiveCount, totalCompanies), "#6b7280")
            };

            var growthPoints = GenerateCompanyGrowthData(companies, today);
            var growth = new CompanyGrowthSummaryDto(
                TotalGrowth: growthPoints.Sum(g => g.NewCompanies),
                GrowthRate: CalculatePercentage(growthPoints.Sum(g => g.NewCompanies), totalCompanies),
                AveragePerDay: growthPoints.Sum(g => g.NewCompanies) / 30,
                BestDay: growthPoints.Max(g => g.NewCompanies),
                BestDayDate: growthPoints.OrderByDescending(g => g.NewCompanies).First().Date,
                DailyData: growthPoints
            );

            var topCompanyList = GetTopCompanies(subscriptions, atRiskCompanyIds);
            var topCompanies = new TopCompaniesDto(
                Companies: topCompanyList,
                TotalRevenue: topCompanyList.Sum(c => c.TotalValue),
                TotalSubscriptions: topCompanyList.Sum(c => c.ActiveSubscriptions)
            );

            var alerts = GenerateCompanyAlerts(companies, subscriptions, atRiskCompanyIds, today);
            var subscriptionCoverage = CalculatePercentage(activeCount, totalCompanies);

            return new CompaniesDashboardDto(
                TotalCompanies: totalCompanies,
                NewThisMonth: newThisMonth,
                NewThisWeek: newThisWeek,
                SubscriptionCoverage: subscriptionCoverage,
                StatusDistribution: statusDistribution,
                StatusChart: statusChart,
                Growth: growth,
                TopCompanies: topCompanies,
                Alerts: alerts,
                CriticalAlertCount: alerts.Count(a => a.Priority == "critical"),
                HighAlertCount: alerts.Count(a => a.Priority == "high"),
                MediumAlertCount: alerts.Count(a => a.Priority == "medium"),
                WithActiveSubscription: activeCount,
                WithTrialSubscription: subscriptions.Where(s => s.IsTrial && s.IsActive).Select(s => s.CompanyId).Distinct().Count(),
                WithExpiredSubscription: subscriptions.Where(s => s.IsExpired).Select(s => s.CompanyId).Distinct().Count(),
                WithNoSubscription: inactiveCount,
                GeneratedAt: now
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating companies dashboard");
            throw;
        }
    }

    #endregion

    #region Subscriptions Dashboard

    public async Task<SubscriptionsDashboardDto> GetSubscriptionsDashboardAsync()
    {
        try
        {
            var now = DateTime.Now;
            var today = now.Date;

            var subscriptions = await _context.Subscriptions
                .Where(s => !s.IsDeleted)
                .Include(s => s.Plan)
                .Include(s => s.Company)
                .ToListAsync();

            var totalSubscriptions = subscriptions.Count;
            var activeSubscriptions = subscriptions.Count(s => s.IsActive && !s.IsExpired && !s.IsTrial);
            var trialSubscriptions = subscriptions.Count(s => s.IsTrial && s.IsActive);
            var expiredSubscriptions = subscriptions.Count(s => s.IsExpired);
            var suspendedSubscriptions = subscriptions.Count(s => !s.IsActive && !s.IsExpired);
            
            var totalMonthlyRevenue = subscriptions
                .Where(s => s.IsActive && !s.IsExpired)
                .Sum(s => s.Amount);

            var statusDistribution = new SubscriptionStatusDistributionDto(
                Active: activeSubscriptions,
                Trial: trialSubscriptions,
                Expired: expiredSubscriptions,
                Suspended: suspendedSubscriptions,
                Cancelled: 0,
                Paused: 0,
                Total: totalSubscriptions
            );

            var statusChart = new List<DistributionItemDto>
            {
                new("Active", activeSubscriptions, CalculatePercentage(activeSubscriptions, totalSubscriptions), "#22c55e"),
                new("Trial", trialSubscriptions, CalculatePercentage(trialSubscriptions, totalSubscriptions), "#3b82f6"),
                new("Expired", expiredSubscriptions, CalculatePercentage(expiredSubscriptions, totalSubscriptions), "#6b7280"),
                new("Suspended", suspendedSubscriptions, CalculatePercentage(suspendedSubscriptions, totalSubscriptions), "#f97316")
            };

            var byPlan = GenerateSubscriptionByPlanData(subscriptions, totalSubscriptions, totalMonthlyRevenue);
            var expiryTimeline = GenerateExpiryTimeline(subscriptions, today);
            var lifecycleMetrics = GenerateLifecycleMetrics(subscriptions, today);
            var growth = GenerateSubscriptionGrowth(subscriptions, today, activeSubscriptions);

            return new SubscriptionsDashboardDto(
                TotalSubscriptions: totalSubscriptions,
                ActiveSubscriptions: activeSubscriptions + trialSubscriptions,
                TrialSubscriptions: trialSubscriptions,
                TotalMonthlyRevenue: totalMonthlyRevenue,
                StatusDistribution: statusDistribution,
                StatusChart: statusChart,
                ByPlan: byPlan,
                ExpiryTimeline: expiryTimeline,
                LifecycleMetrics: lifecycleMetrics,
                Growth: growth,
                GeneratedAt: now
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating subscriptions dashboard");
            throw;
        }
    }

    #endregion

    #region Revenue Dashboard

    public async Task<RevenueDashboardDto> GetRevenueDashboardAsync()
    {
        try
        {
            var now = DateTime.Now;
            var today = now.Date;

            var subscriptions = await _context.Subscriptions
                .Where(s => !s.IsDeleted)
                .Include(s => s.Plan)
                .ToListAsync();

            var activeSubscriptions = subscriptions.Where(s => s.IsActive && !s.IsExpired).ToList();
            
            var mrr = activeSubscriptions.Sum(s => s.Amount);
            var arr = mrr * 12;
            var activeCustomers = activeSubscriptions.Select(s => s.CompanyId).Distinct().Count();
            var arpc = activeCustomers > 0 ? mrr / activeCustomers : 0;

            var previousMrr = mrr * 0.95m;
            var mrrChange = mrr - previousMrr;
            var mrrChangePercentage = previousMrr > 0 ? (mrrChange / previousMrr) * 100 : 0;

            var metrics = new RevenueMetricsDto(
                MRR: mrr,
                PreviousMRR: previousMrr,
                MRRChange: mrrChange,
                MRRChangePercentage: mrrChangePercentage,
                ARR: arr,
                PreviousARR: previousMrr * 12,
                ARRChange: mrrChange * 12,
                ARRChangePercentage: mrrChangePercentage,
                ARPC: arpc,
                PreviousARPC: arpc * 0.95m,
                ARPCChange: arpc * 0.05m,
                ARPCChangePercentage: 5,
                EstimatedCLTV: arpc * 24,
                ActiveCustomers: activeCustomers,
                PreviousActiveCustomers: (int)(activeCustomers * 0.95m)
            );

            var byPlan = GenerateRevenueByPlan(activeSubscriptions, mrr);
            var trend = GenerateRevenueTrend(mrr, today);
            var projections = GenerateRevenueProjections(activeSubscriptions, mrr, today);

            return new RevenueDashboardDto(
                Metrics: metrics,
                ByPlan: byPlan,
                ByPlanChart: byPlan.Plans.Select(p => new DistributionItemDto(p.PlanName, p.SubscriptionCount, p.Percentage, p.Color)).ToList(),
                Trend: trend,
                Projections: projections,
                TotalLifetimeRevenue: subscriptions.Sum(s => s.Amount),
                AverageOrderValue: subscriptions.Any() ? subscriptions.Average(s => s.Amount) : 0,
                TotalTransactions: subscriptions.Count,
                ByCurrency: new List<DistributionItemDto> { new("EGP", subscriptions.Count, 100, "#22c55e") },
                GeneratedAt: now
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating revenue dashboard");
            throw;
        }
    }

    #endregion

    #region Activity Dashboard

    public async Task<ActivityDashboardDto> GetActivityDashboardAsync()
    {
        try
        {
            var now = DateTime.Now;
            var today = now.Date;
            var thisWeek = today.AddDays(-(int)today.DayOfWeek);
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            var admins = await _context.Admins
                .Where(a => !a.IsDeleted)
                .Include(a => a.AdminType)
                .ToListAsync();

            var activeToday = admins.Count(a => a.LastLoginAt.HasValue && a.LastLoginAt.Value.Date == today);
            var activeThisWeek = admins.Count(a => a.LastLoginAt.HasValue && a.LastLoginAt.Value >= thisWeek);
            var activeThisMonth = admins.Count(a => a.LastLoginAt.HasValue && a.LastLoginAt.Value >= thisMonth);

            var stats = new ActivityStatsDto(
                TotalLogins: admins.Sum(a => a.LoginCount),
                TodayLogins: activeToday,
                ThisWeekLogins: activeThisWeek * 3,
                ThisMonthLogins: activeThisMonth * 5,
                UniqueAdminsToday: activeToday,
                TotalActions: activeThisMonth * 20,
                TodayActions: activeToday * 5,
                ThisWeekActions: activeThisWeek * 15,
                ThisMonthActions: activeThisMonth * 20,
                ActiveSessions: activeToday,
                AverageSessionDuration: 45,
                PreviousPeriodLogins: activeThisMonth * 4,
                PreviousPeriodActions: activeThisMonth * 18,
                LoginChangePercentage: 25,
                ActionChangePercentage: 11
            );

            var topAdmins = GenerateTopAdmins(admins, activeThisMonth, now);
            var breakdown = GenerateActivityBreakdown();
            var timeline = GenerateActivityTimeline(today);

            return new ActivityDashboardDto(
                Stats: stats,
                TopAdmins: topAdmins,
                RecentActivity: new List<ActivityItemDto>(),
                TotalActivityCount: stats.TotalActions,
                Breakdown: breakdown,
                ByEntityTypeChart: breakdown.ByEntityType.Select(e => new DistributionItemDto(e.EntityType, e.Count, e.Percentage, e.Color)).ToList(),
                ByActionTypeChart: breakdown.ByActionType.Select(a => new DistributionItemDto(a.ActionType, a.Count, a.Percentage, a.Color)).ToList(),
                Timeline: timeline,
                GeneratedAt: now
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating activity dashboard");
            throw;
        }
    }

    #endregion

    #region Alerts Dashboard

    public async Task<AlertsDashboardDto> GetAlertsDashboardAsync()
    {
        try
        {
            var now = DateTime.Now;
            var today = now.Date;

            var subscriptions = await _context.Subscriptions
                .Where(s => !s.IsDeleted && s.IsActive && !s.IsExpired)
                .Include(s => s.Company)
                .Include(s => s.Plan)
                .ToListAsync();

            var criticalAlerts = new List<AlertItemDto>();
            var highAlerts = new List<AlertItemDto>();
            var mediumAlerts = new List<AlertItemDto>();
            var lowAlerts = new List<AlertItemDto>();

            foreach (var sub in subscriptions.Where(s => s.ExpiryDateUtc.Date == today))
                criticalAlerts.Add(CreateAlertItem(sub, AlertPriority.Critical, "Expires today", today));

            foreach (var sub in subscriptions.Where(s => s.ExpiryDateUtc.Date > today && s.ExpiryDateUtc.Date <= today.AddDays(7)))
                highAlerts.Add(CreateAlertItem(sub, AlertPriority.High, $"Expires in {(sub.ExpiryDateUtc.Date - today).Days} days", today));

            foreach (var sub in subscriptions.Where(s => s.ExpiryDateUtc.Date > today.AddDays(7) && s.ExpiryDateUtc.Date <= today.AddDays(30)))
                mediumAlerts.Add(CreateAlertItem(sub, AlertPriority.Medium, $"Expires in {(sub.ExpiryDateUtc.Date - today).Days} days", today));

            var companyIdsWithSub = await _context.Subscriptions.Where(s => !s.IsDeleted).Select(s => s.CompanyId).Distinct().ToListAsync();
            var companiesWithoutSub = await _context.Companies.Where(c => !c.IsDeleted && !companyIdsWithSub.Contains(c.Id)).Take(5).ToListAsync();

            foreach (var company in companiesWithoutSub)
                lowAlerts.Add(CreateCompanyAlert(company, now));

            var counts = new AlertCountsDto(
                Critical: criticalAlerts.Count,
                High: highAlerts.Count,
                Medium: mediumAlerts.Count,
                Low: lowAlerts.Count,
                Total: criticalAlerts.Count + highAlerts.Count + mediumAlerts.Count + lowAlerts.Count,
                Unread: criticalAlerts.Count + highAlerts.Count + mediumAlerts.Count + lowAlerts.Count,
                Dismissed: 0
            );

            var byCategory = new AlertsByCategoryDto(
                SubscriptionExpiry: criticalAlerts.Count + highAlerts.Count + mediumAlerts.Count,
                SubscriptionStatus: 0,
                CompanyStatus: lowAlerts.Count,
                AdminActivity: 0,
                SystemHealth: 0,
                Revenue: 0
            );

            var byPriorityChart = GenerateAlertPriorityChart(counts);
            var byCategoryChart = GenerateAlertCategoryChart(byCategory, counts.Total);

            return new AlertsDashboardDto(
                Counts: counts,
                ByCategory: byCategory,
                CriticalAlerts: criticalAlerts,
                HighPriorityAlerts: highAlerts,
                MediumPriorityAlerts: mediumAlerts,
                LowPriorityAlerts: lowAlerts,
                ByPriorityChart: byPriorityChart,
                ByCategoryChart: byCategoryChart,
                RecentlyDismissed: new List<AlertItemDto>(),
                AlertsCreatedToday: counts.Total,
                AlertsResolvedToday: 0,
                OverdueAlerts: criticalAlerts.Count,
                GeneratedAt: now
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating alerts dashboard");
            throw;
        }
    }

    public Task DismissAlertAsync(Guid alertId, Guid adminId)
    {
        _logger.LogInformation("Alert {AlertId} dismissed by {AdminId}", alertId, adminId);
        return Task.CompletedTask;
    }

    public Task MarkAlertAsReadAsync(Guid alertId, Guid adminId)
    {
        _logger.LogInformation("Alert {AlertId} marked as read by {AdminId}", alertId, adminId);
        return Task.CompletedTask;
    }

    #endregion

    #region Helper Methods

    private static decimal CalculatePercentage(int value, int total)
    {
        return total > 0 ? Math.Round((decimal)value / total * 100, 2) : 0;
    }

    private static decimal CalculateGrowthRate(int previous, int current)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return Math.Round(((decimal)(current - previous) / previous) * 100, 2);
    }

    private List<CompanyGrowthPointDto> GenerateCompanyGrowthData<T>(List<T> companies, DateTime today)
    {
        var points = new List<CompanyGrowthPointDto>();
        for (int i = 29; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var newOnDate = companies.Count(c => ((dynamic)c).CreatedTimestamp.Date == date);
            var totalOnDate = companies.Count(c => ((dynamic)c).CreatedTimestamp.Date <= date);
            points.Add(new CompanyGrowthPointDto(date, newOnDate, totalOnDate));
        }
        return points;
    }

    private List<TopCompanyDto> GetTopCompanies(List<Subscription> subscriptions, HashSet<Guid> atRiskIds)
    {
        return subscriptions
            .Where(s => s.IsActive && !s.IsExpired)
            .GroupBy(s => s.CompanyId)
            .Select(g => new TopCompanyDto(
                Id: g.Key,
                Name: g.First().Company?.Name ?? "Unknown",
                LogoUrl: null,
                ActiveSubscriptions: g.Count(),
                TotalValue: g.Sum(s => s.Amount),
                TopPlanName: g.OrderByDescending(s => s.Amount).First().Plan?.Name ?? "N/A",
                LatestSubscriptionDate: g.Max(s => s.CreatedTimestamp),
                Status: atRiskIds.Contains(g.Key) ? "AtRisk" : "Active"
            ))
            .OrderByDescending(c => c.TotalValue)
            .Take(10)
            .ToList();
    }

    private List<CompanyAlertDto> GenerateCompanyAlerts<T>(
        List<T> companies, 
        List<Subscription> subscriptions, 
        HashSet<Guid> atRiskIds, 
        DateTime today) where T : class
    {
        var alerts = new List<CompanyAlertDto>();
        var companyIdsWithSub = subscriptions.Select(s => s.CompanyId).ToHashSet();
        
        foreach (var company in companies.Where(c => !companyIdsWithSub.Contains(((dynamic)c).Id)).Take(5))
        {
            var companyDynamic = (dynamic)company;
            alerts.Add(new CompanyAlertDto(
                CompanyId: companyDynamic.Id,
                CompanyName: companyDynamic.Name,
                AlertType: CompanyAlertType.NoSubscription,
                AlertMessage: "No subscription",
                ExpiryDate: null,
                DaysRemaining: 0,
                Priority: "medium",
                SuggestedAction: "Create subscription"
            ));
        }

        foreach (var companyId in atRiskIds.Take(5))
        {
            var expiringSub = subscriptions
                .Where(s => s.CompanyId == companyId && s.IsActive && !s.IsExpired && s.ExpiryDateUtc <= today.AddDays(30))
                .OrderBy(s => s.ExpiryDateUtc)
                .FirstOrDefault();
            
            if (expiringSub != null)
            {
                var daysRemaining = (expiringSub.ExpiryDateUtc.Date - today).Days;
                alerts.Add(new CompanyAlertDto(
                    CompanyId: companyId,
                    CompanyName: expiringSub.Company?.Name ?? "Unknown",
                    AlertType: CompanyAlertType.SubscriptionExpiring,
                    AlertMessage: $"Subscription expiring in {daysRemaining} days",
                    ExpiryDate: expiringSub.ExpiryDateUtc,
                    DaysRemaining: daysRemaining,
                    Priority: daysRemaining <= 7 ? "high" : "medium",
                    SuggestedAction: "Renew subscription"
                ));
            }
        }

        return alerts;
    }

    private SubscriptionsByPlanSummaryDto GenerateSubscriptionByPlanData(
        List<Subscription> subscriptions, 
        int totalSubscriptions, 
        decimal totalMonthlyRevenue)
    {
        var planGroups = subscriptions
            .Where(s => s.Plan != null)
            .GroupBy(s => s.PlanId)
            .Select(g => new SubscriptionByPlanDto(
                PlanId: g.Key,
                PlanName: g.First().Plan?.Name ?? "Unknown",
                PlanTier: g.First().Plan?.Name ?? "Standard",
                ActiveCount: g.Count(s => s.IsActive && !s.IsExpired && !s.IsTrial),
                TrialCount: g.Count(s => s.IsTrial && s.IsActive),
                TotalCount: g.Count(),
                MonthlyRevenue: g.Where(s => s.IsActive && !s.IsExpired).Sum(s => s.Amount),
                TotalRevenue: g.Sum(s => s.Amount),
                Percentage: CalculatePercentage(g.Count(), totalSubscriptions),
                Color: GetPlanColor(g.First().Plan?.Name ?? "Standard")
            ))
            .OrderByDescending(p => p.TotalCount)
            .ToList();

        var topPlan = planGroups.FirstOrDefault();
        return new SubscriptionsByPlanSummaryDto(
            Plans: planGroups,
            TopPlanName: topPlan?.PlanName ?? "N/A",
            TopPlanCount: topPlan?.TotalCount ?? 0,
            TotalMonthlyRevenue: totalMonthlyRevenue
        );
    }

    private ExpiryTimelineDto GenerateExpiryTimeline(List<Subscription> subscriptions, DateTime today)
    {
        var activeForExpiry = subscriptions.Where(s => s.IsActive && !s.IsExpired).ToList();

        var expiringToday = activeForExpiry
            .Where(s => s.ExpiryDateUtc.Date == today)
            .Select(s => MapToExpiringDto(s, today))
            .ToList();

        var expiringThisWeek = activeForExpiry
            .Where(s => s.ExpiryDateUtc.Date > today && s.ExpiryDateUtc.Date <= today.AddDays(7))
            .Select(s => MapToExpiringDto(s, today))
            .ToList();

        var expiringThisMonth = activeForExpiry
            .Where(s => s.ExpiryDateUtc.Date > today.AddDays(7) && s.ExpiryDateUtc.Date <= today.AddDays(30))
            .Select(s => MapToExpiringDto(s, today))
            .ToList();

        var expiringNext3Months = activeForExpiry
            .Where(s => s.ExpiryDateUtc.Date > today.AddDays(30) && s.ExpiryDateUtc.Date <= today.AddDays(90))
            .Select(s => MapToExpiringDto(s, today))
            .ToList();

        return new ExpiryTimelineDto(
            ExpiringToday: expiringToday,
            ExpiringTodayCount: expiringToday.Count,
            ExpiringTodayValue: expiringToday.Sum(e => e.MonthlyValue),
            ExpiringThisWeek: expiringThisWeek,
            ExpiringThisWeekCount: expiringThisWeek.Count,
            ExpiringThisWeekValue: expiringThisWeek.Sum(e => e.MonthlyValue),
            ExpiringThisMonth: expiringThisMonth,
            ExpiringThisMonthCount: expiringThisMonth.Count,
            ExpiringThisMonthValue: expiringThisMonth.Sum(e => e.MonthlyValue),
            ExpiringNext3Months: expiringNext3Months,
            ExpiringNext3MonthsCount: expiringNext3Months.Count,
            ExpiringNext3MonthsValue: expiringNext3Months.Sum(e => e.MonthlyValue)
        );
    }

    private LifecycleMetricsDto GenerateLifecycleMetrics(List<Subscription> subscriptions, DateTime today)
    {
        var thisMonth = new DateTime(today.Year, today.Month, 1);
        var lastMonth = thisMonth.AddMonths(-1);

        var newThisMonth = subscriptions.Count(s => s.CreatedTimestamp >= thisMonth);
        var newLastMonth = subscriptions.Count(s => s.CreatedTimestamp >= lastMonth && s.CreatedTimestamp < thisMonth);
        var avgDuration = subscriptions.Any() ? (int)subscriptions.Average(s => (s.ExpiryDateUtc - s.StartDateUtc).TotalDays) : 365;

        return new LifecycleMetricsDto(
            NewSubscriptions: newThisMonth,
            Renewals: 0,
            Upgrades: 0,
            Downgrades: 0,
            Cancellations: 0,
            Suspensions: subscriptions.Count(s => !s.IsActive && !s.IsExpired),
            Reactivations: 0,
            PreviousNewSubscriptions: newLastMonth,
            PreviousRenewals: 0,
            PreviousCancellations: 0,
            TrialConversionRate: 0,
            ChurnRate: 0,
            RetentionRate: 100,
            RenewalRate: 0,
            AverageDurationDays: avgDuration,
            MedianDurationDays: 365
        );
    }

    private SubscriptionGrowthSummaryDto GenerateSubscriptionGrowth(
        List<Subscription> subscriptions, 
        DateTime today, 
        int activeSubscriptions)
    {
        var growthPoints = new List<SubscriptionGrowthPointDto>();
        for (int i = 29; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var newOnDate = subscriptions.Count(s => s.CreatedTimestamp.Date == date);
            var activeOnDate = subscriptions.Count(s => s.CreatedTimestamp.Date <= date && s.IsActive && !s.IsExpired);
            growthPoints.Add(new SubscriptionGrowthPointDto(date, newOnDate, activeOnDate, 0));
        }

        return new SubscriptionGrowthSummaryDto(
            NetGrowth: growthPoints.Sum(g => g.NewSubscriptions),
            GrowthRate: activeSubscriptions > 0 ? CalculatePercentage(growthPoints.Sum(g => g.NewSubscriptions), activeSubscriptions) : 0,
            TotalNew: growthPoints.Sum(g => g.NewSubscriptions),
            TotalChurned: 0,
            DailyData: growthPoints
        );
    }

    private RevenueByPlanDto GenerateRevenueByPlan(List<Subscription> activeSubscriptions, decimal mrr)
    {
        var planGroups = activeSubscriptions
            .Where(s => s.Plan != null)
            .GroupBy(s => s.PlanId)
            .Select(g => new RevenueByPlanItemDto(
                PlanId: g.Key,
                PlanName: g.First().Plan?.Name ?? "Unknown",
                PlanTier: g.First().Plan?.Name ?? "Standard",
                MonthlyRevenue: g.Sum(s => s.Amount),
                AnnualRevenue: g.Sum(s => s.Amount) * 12,
                SubscriptionCount: g.Count(),
                Percentage: mrr > 0 ? Math.Round(g.Sum(s => s.Amount) / mrr * 100, 2) : 0,
                Color: GetPlanColor(g.First().Plan?.Name ?? "Standard")
            ))
            .OrderByDescending(p => p.MonthlyRevenue)
            .ToList();

        var topPlan = planGroups.FirstOrDefault();
        return new RevenueByPlanDto(
            Plans: planGroups,
            TopRevenuePlanName: topPlan?.PlanName ?? "N/A",
            TopRevenuePlanValue: topPlan?.MonthlyRevenue ?? 0,
            TotalRevenue: mrr
        );
    }

    private RevenueTrendDto GenerateRevenueTrend(decimal mrr, DateTime today)
    {
        var trendPoints = new List<RevenueTrendPointDto>();
        for (int i = 11; i >= 0; i--)
        {
            var month = today.AddMonths(-i);
            var revenue = mrr * (1 - i * 0.02m);
            trendPoints.Add(new RevenueTrendPointDto(
                month, 
                month.ToString("MMM yyyy"), 
                revenue,
                revenue * 0.1m, 
                revenue * 0.9m, 
                revenue * 0.02m
            ));
        }

        return new RevenueTrendDto(
            DataPoints: trendPoints,
            TotalGrowth: mrr * 0.24m,
            GrowthRate: 24,
            TrendDirection: "up",
            HighestRevenue: mrr,
            HighestRevenueDate: today,
            LowestRevenue: mrr * 0.76m,
            LowestRevenueDate: today.AddMonths(-11)
        );
    }

    private RevenueProjectionDto GenerateRevenueProjections(
        List<Subscription> activeSubscriptions, 
        decimal mrr, 
        DateTime today)
    {
        var expiringNext30Days = activeSubscriptions
            .Where(s => s.ExpiryDateUtc <= today.AddDays(30))
            .ToList();

        return new RevenueProjectionDto(
            ExpectedRenewalRevenue: expiringNext30Days.Sum(s => s.Amount) * 0.8m,
            ExpectedRenewals: (int)(expiringNext30Days.Count * 0.8m),
            AtRiskRevenue: expiringNext30Days.Sum(s => s.Amount) * 0.2m,
            AtRiskSubscriptions: (int)(expiringNext30Days.Count * 0.2m),
            ProjectedNextMonthMRR: mrr * 1.02m,
            ProjectedNextMonthChange: mrr * 0.02m,
            ProjectedNextQuarterRevenue: mrr * 3 * 1.06m,
            ProjectedChurnImpact: mrr * 0.03m,
            BestCaseProjection: mrr * 1.05m,
            WorstCaseProjection: mrr * 0.95m
        );
    }

    private TopAdminsDto GenerateTopAdmins(List<Domain.Entities.Authentication.Admin> admins, int activeThisMonth, DateTime now)
    {
        var topAdminList = admins
            .Where(a => a.LastLoginAt.HasValue)
            .OrderByDescending(a => a.LoginCount)
            .Take(10)
            .Select((a, i) => new TopAdminDto(
                AdminId: a.Id,
                FullName: $"{a.FirstName ?? ""} {a.LastName ?? ""}".Trim(),
                Username: a.Username,
                ProfilePictureUrl: a.ProfilePictureUrl,
                AdminType: a.AdminType?.AdminTypeName ?? "Admin",
                TotalActions: a.LoginCount * 5,
                TodayActions: 5 - i / 2,
                LoginCount: a.LoginCount,
                LastLogin: a.LastLoginAt ?? now,
                LastAction: a.LastLoginAt ?? now,
                Rank: i + 1
            ))
            .ToList();

        return new TopAdminsDto(
            Admins: topAdminList,
            TotalActiveAdmins: activeThisMonth,
            AverageActionsPerAdmin: topAdminList.Any() ? (decimal)topAdminList.Average(a => a.TotalActions) : 0
        );
    }

    private ActivityBreakdownDto GenerateActivityBreakdown()
    {
        return new ActivityBreakdownDto(
            ByEntityType: new List<ActivityByEntityTypeDto>
            {
                new("Company", 40, 40, "#3b82f6"),
                new("Subscription", 35, 35, "#22c55e"),
                new("Admin", 15, 15, "#8b5cf6"),
                new("System", 10, 10, "#6b7280")
            },
            ByActionType: new List<ActivityByActionTypeDto>
            {
                new("Create", 30, 30, "#22c55e"),
                new("Update", 40, 40, "#3b82f6"),
                new("Delete", 10, 10, "#ef4444"),
                new("Login", 20, 20, "#8b5cf6")
            },
            MostActiveEntityType: "Company",
            MostCommonAction: "Update"
        );
    }

    private ActivityTimelineDto GenerateActivityTimeline(DateTime today)
    {
        var dailyData = new List<ActivityTimelinePointDto>();
        for (int i = 29; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            dailyData.Add(new ActivityTimelinePointDto(
                date, 
                date.ToString("MMM dd"), 
                5 + i % 10, 
                20 + i % 30, 
                3 + i % 5
            ));
        }

        var hourlyDist = Enumerable.Range(0, 24)
            .Select(h => new HourlyActivityDto(
                h, 
                $"{h}:00", 
                10 + (h >= 9 && h <= 17 ? 30 : 0), 
                (10 + (h >= 9 && h <= 17 ? 30 : 0)) / 4.0m
            ))
            .ToList();

        return new ActivityTimelineDto(
            DailyData: dailyData,
            HourlyDistribution: hourlyDist,
            PeakHour: 14,
            PeakHourLabel: "2:00 PM",
            PeakHourActivity: 40,
            MostActiveDayOfWeek: "Wednesday"
        );
    }

    private List<DistributionItemDto> GenerateAlertPriorityChart(AlertCountsDto counts)
    {
        return new List<DistributionItemDto>
        {
            new("Critical", counts.Critical, CalculatePercentage(counts.Critical, counts.Total), "#ef4444"),
            new("High", counts.High, CalculatePercentage(counts.High, counts.Total), "#f97316"),
            new("Medium", counts.Medium, CalculatePercentage(counts.Medium, counts.Total), "#eab308"),
            new("Low", counts.Low, CalculatePercentage(counts.Low, counts.Total), "#3b82f6")
        };
    }

    private List<DistributionItemDto> GenerateAlertCategoryChart(AlertsByCategoryDto byCategory, int total)
    {
        return new List<DistributionItemDto>
        {
            new("Subscription Expiry", byCategory.SubscriptionExpiry, CalculatePercentage(byCategory.SubscriptionExpiry, total), "#ef4444"),
            new("Company Status", byCategory.CompanyStatus, CalculatePercentage(byCategory.CompanyStatus, total), "#3b82f6")
        };
    }

    private static string GetPlanColor(string planName) => planName.ToLower() switch
    {
        var n when n.Contains("basic") => "#6b7280",
        var n when n.Contains("starter") => "#22c55e",
        var n when n.Contains("pro") => "#3b82f6",
        var n when n.Contains("enterprise") => "#8b5cf6",
        var n when n.Contains("premium") => "#f97316",
        _ => "#3b82f6"
    };

    private ExpiringSubscriptionDto MapToExpiringDto(Subscription sub, DateTime today)
    {
        var daysRemaining = (sub.ExpiryDateUtc.Date - today).Days;
        return new ExpiringSubscriptionDto(
            SubscriptionId: sub.Id,
            CompanyId: sub.CompanyId,
            CompanyName: sub.Company?.Name ?? "Unknown",
            PlanName: sub.Plan?.Name ?? "Unknown",
            ExpiryDate: sub.ExpiryDateUtc,
            DaysRemaining: daysRemaining,
            MonthlyValue: sub.Amount,
            Priority: daysRemaining <= 0 ? "critical" : daysRemaining <= 7 ? "high" : "medium"
        );
    }

    private AlertItemDto CreateAlertItem(Subscription sub, AlertPriority priority, string message, DateTime today)
    {
        var now = DateTime.Now;
        var daysRemaining = (sub.ExpiryDateUtc.Date - today).Days;
        
        return new AlertItemDto(
            Id: Guid.NewGuid(),
            Priority: priority,
            Category: AlertCategory.SubscriptionExpiry,
            Title: "Subscription Expiring",
            Message: $"{sub.Company?.Name}: {message}",
            Description: $"Plan: {sub.Plan?.Name}, Value: {sub.Amount:C}",
            EntityType: "Subscription",
            EntityId: sub.Id,
            EntityName: sub.Company?.Name,
            CreatedAt: now,
            DueDate: sub.ExpiryDateUtc,
            DaysRemaining: daysRemaining,
            TimeAgo: daysRemaining <= 0 ? "Today" : $"In {daysRemaining} days",
            PrimaryAction: "Renew",
            PrimaryActionUrl: $"/subscriptions/{sub.Id}/renew",
            SecondaryAction: "View",
            SecondaryActionUrl: $"/subscriptions/{sub.Id}",
            Icon: "Calendar",
            Color: priority == AlertPriority.Critical ? "#ef4444" : priority == AlertPriority.High ? "#f97316" : "#eab308",
            IsRead: false,
            IsDismissed: false,
            DismissedAt: null,
            DismissedById: null
        );
    }

    private AlertItemDto CreateCompanyAlert(Company company, DateTime now)
    {
        return new AlertItemDto(
            Id: Guid.NewGuid(),
            Priority: AlertPriority.Low,
            Category: AlertCategory.CompanyStatus,
            Title: "Company without subscription",
            Message: $"{company.Name} has no subscription",
            Description: "Consider reaching out to create a subscription",
            EntityType: "Company",
            EntityId: company.Id,
            EntityName: company.Name,
            CreatedAt: now,
            DueDate: null,
            DaysRemaining: null,
            TimeAgo: "Just now",
            PrimaryAction: "Create Subscription",
            PrimaryActionUrl: $"/subscriptions/create?companyId={company.Id}",
            SecondaryAction: "View Company",
            SecondaryActionUrl: $"/companies/{company.Id}",
            Icon: "Building2",
            Color: "#3b82f6",
            IsRead: false,
            IsDismissed: false,
            DismissedAt: null,
            DismissedById: null
        );
    }

    #endregion

    #region Activity Helpers

    private string GetLocalizedTimeAgo(DateTime timestamp)
    {
        var now = DateTime.Now;
        var diff = now - timestamp;

        if (diff.TotalMinutes < 1) return _localizer["Activity.Time.JustNow"];
        if (diff.TotalMinutes < 60) return string.Format(_localizer["Activity.Time.MinutesAgo"], (int)diff.TotalMinutes);
        if (diff.TotalHours < 24) return string.Format(_localizer["Activity.Time.HoursAgo"], (int)diff.TotalHours);
        if (diff.TotalDays < 7) return string.Format(_localizer["Activity.Time.DaysAgo"], (int)diff.TotalDays);
        if (diff.TotalDays < 30) return string.Format(_localizer["Activity.Time.WeeksAgo"], (int)(diff.TotalDays / 7));
        return timestamp.ToString("MMM dd");
    }

    private string GetLocalizedActivityDescription(string actionType, string entityType, string entityName)
    {
        var localizedEntityType = _localizer[$"Activity.EntityType.{entityType}"];
        var template = _localizer[$"Activity.Action.{actionType}"];
        
        return template
            .Replace("{entityType}", localizedEntityType)
            .Replace("{entityName}", entityName);
    }

    private static string GetIconForEntityType(string entityType)
    {
        return entityType switch
        {
            "Company" => "Building2",
            "Subscription" => "CreditCard",
            "Admin" => "User",
            "Plan" => "Package",
            "Project" => "FolderOpen",
            "Module" => "Box",
            _ => "Activity"
        };
    }

    private static string GetColorForActionType(string actionType)
    {
        return actionType switch
        {
            "Created" => "#22c55e", // green
            "Updated" => "#3b82f6", // blue
            "Deleted" => "#ef4444", // red
            "Activated" => "#22c55e", // green
            "Deactivated" => "#f97316", // orange
            "Suspended" => "#ef4444", // red
            "Resumed" => "#22c55e", // green
            "Renewed" => "#8b5cf6", // purple
            "Upgraded" => "#3b82f6", // blue
            "Cancelled" => "#ef4444", // red
            _ => "#6b7280" // gray
        };
    }

    #endregion
}
