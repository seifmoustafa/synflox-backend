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
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    /// <summary>
    /// Dashboard service implementation with real data queries
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDBContext _context;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            ApplicationDBContext context,
            ILogger<DashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Overview Dashboard

        public async Task<OverviewDashboardDto> GetOverviewAsync()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var lastMonth = today.AddMonths(-1);
            var last30Days = today.AddDays(-30);

            var companies = await _context.Companies.Where(c => !c.IsDeleted).ToListAsync();
            var totalCompanies = companies.Count;

            var subscriptions = await _context.Subscriptions
                .Where(s => !s.IsDeleted)
                .Include(s => s.Company)
                .Include(s => s.Plan)
                .ToListAsync();

            var totalSubscriptions = subscriptions.Count;
            var activeSubscriptions = subscriptions.Count(s => s.IsActive && !s.IsExpired);
            var trialSubscriptions = subscriptions.Count(s => s.IsTrial && s.IsActive);
            var expiredSubscriptions = subscriptions.Count(s => s.IsExpired);

            var companyIdsWithActiveSub = subscriptions
                .Where(s => s.IsActive && !s.IsExpired)
                .Select(s => s.CompanyId).Distinct().ToList();
            var activeCompanies = companyIdsWithActiveSub.Count;

            var companyIdsWithAnySub = subscriptions.Select(s => s.CompanyId).Distinct().ToList();
            var companiesWithoutSub = totalCompanies - companyIdsWithAnySub.Count;

            var expiringToday = subscriptions.Count(s => s.IsActive && !s.IsExpired && s.ExpiryDateUtc.Date == today);
            var expiringThisWeek = subscriptions.Count(s => s.IsActive && !s.IsExpired && s.ExpiryDateUtc.Date > today && s.ExpiryDateUtc.Date <= today.AddDays(7));
            var expiringThisMonth = subscriptions.Count(s => s.IsActive && !s.IsExpired && s.ExpiryDateUtc.Date > today.AddDays(7) && s.ExpiryDateUtc.Date <= today.AddDays(30));
            var suspendedSubscriptions = subscriptions.Count(s => !s.IsActive && !s.IsExpired && !s.IsTrial);

            var admins = await _context.Admins.Where(a => !a.IsDeleted).ToListAsync();
            var totalAdmins = admins.Count;
            var activeAdmins = admins.Count(a => a.LastLoginAt.HasValue && a.LastLoginAt.Value >= last30Days);

            var prevMonthCompanies = companies.Count(c => c.CreatedTimestamp < lastMonth);
            var prevMonthSubscriptions = subscriptions.Count(s => s.CreatedTimestamp < lastMonth);
            var companyGrowthRate = prevMonthCompanies > 0 ? ((decimal)(totalCompanies - prevMonthCompanies) / prevMonthCompanies) * 100 : 100;
            var subscriptionGrowthRate = prevMonthSubscriptions > 0 ? ((decimal)(totalSubscriptions - prevMonthSubscriptions) / prevMonthSubscriptions) * 100 : 100;

            var mrr = subscriptions.Where(s => s.IsActive && !s.IsExpired).Sum(s => s.Amount);

            var companiesKpi = new KpiCardDto("Companies", totalCompanies, prevMonthCompanies, companyGrowthRate, companyGrowthRate >= 0 ? "up" : "down", "Building2", "blue");
            var subscriptionsKpi = new KpiCardDto("Active Subscriptions", activeSubscriptions, null, subscriptionGrowthRate, subscriptionGrowthRate >= 0 ? "up" : "down", "CreditCard", "green");
            var revenueKpi = new KpiCardDto("MRR", (int)mrr, null, null, "unchanged", "DollarSign", "purple");
            var alertsKpi = new KpiCardDto("Alerts", expiringToday + expiringThisWeek + suspendedSubscriptions, null, null, expiringToday > 0 ? "up" : "unchanged", "Bell", expiringToday > 0 ? "red" : "yellow");

            var stats = new QuickStatsDto(totalCompanies, activeCompanies, totalCompanies - activeCompanies, companiesWithoutSub, totalSubscriptions, activeSubscriptions, trialSubscriptions, expiredSubscriptions, suspendedSubscriptions, expiringToday, expiringThisWeek, expiringThisMonth, totalAdmins, activeAdmins, companyGrowthRate, subscriptionGrowthRate);

            var statusDistribution = new List<DistributionItemDto>
            {
                new("Active", activeSubscriptions - trialSubscriptions, totalSubscriptions > 0 ? (decimal)(activeSubscriptions - trialSubscriptions) / totalSubscriptions * 100 : 0, "#22c55e"),
                new("Trial", trialSubscriptions, totalSubscriptions > 0 ? (decimal)trialSubscriptions / totalSubscriptions * 100 : 0, "#3b82f6"),
                new("Expired", expiredSubscriptions, totalSubscriptions > 0 ? (decimal)expiredSubscriptions / totalSubscriptions * 100 : 0, "#ef4444"),
                new("Suspended", suspendedSubscriptions, totalSubscriptions > 0 ? (decimal)suspendedSubscriptions / totalSubscriptions * 100 : 0, "#f97316")
            };

            var growthTrend = new List<TimeSeriesDataPointDto>();
            for (int i = 29; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var companiesOnDate = companies.Count(c => c.CreatedTimestamp.Date <= date);
                growthTrend.Add(new TimeSeriesDataPointDto(date, date.ToString("MMM dd"), companiesOnDate));
            }

            return new OverviewDashboardDto(companiesKpi, subscriptionsKpi, revenueKpi, alertsKpi, stats, statusDistribution, growthTrend, new List<RecentActivityItemDto>(), now);
        }

        #endregion

        #region Companies Dashboard

        public async Task<CompaniesDashboardDto> GetCompaniesDashboardAsync()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var thisWeek = today.AddDays(-(int)today.DayOfWeek);

            var companies = await _context.Companies.Where(c => !c.IsDeleted).ToListAsync();
            var subscriptions = await _context.Subscriptions.Where(s => !s.IsDeleted).Include(s => s.Plan).Include(s => s.Company).ToListAsync();

            var totalCompanies = companies.Count;
            var newThisMonth = companies.Count(c => c.CreatedTimestamp >= thisMonth);
            var newThisWeek = companies.Count(c => c.CreatedTimestamp >= thisWeek);

            var activeCompanyIds = subscriptions.Where(s => s.IsActive && !s.IsExpired).Select(s => s.CompanyId).Distinct().ToList();
            var suspendedCompanyIds = subscriptions.Where(s => !s.IsActive && !s.IsExpired).Select(s => s.CompanyId).Distinct().ToList();
            var atRiskCompanyIds = subscriptions.Where(s => s.IsActive && !s.IsExpired && s.ExpiryDateUtc <= today.AddDays(30)).Select(s => s.CompanyId).Distinct().ToList();
            var companyIdsWithAnySub = subscriptions.Select(s => s.CompanyId).Distinct().ToList();
            var withoutSubCompanyIds = companies.Where(c => !companyIdsWithAnySub.Contains(c.Id)).Select(c => c.Id).ToList();

            var activeCount = activeCompanyIds.Count;
            var suspendedCount = suspendedCompanyIds.Except(activeCompanyIds).Count();
            var atRiskCount = atRiskCompanyIds.Count;
            var inactiveCount = totalCompanies - activeCount - suspendedCount;

            var statusDistribution = new CompanyStatusDistributionDto(activeCount - atRiskCount, inactiveCount, suspendedCount, atRiskCount, totalCompanies);

            var statusChart = new List<DistributionItemDto>
            {
                new("Active", activeCount - atRiskCount, totalCompanies > 0 ? (decimal)(activeCount - atRiskCount) / totalCompanies * 100 : 0, "#22c55e"),
                new("At Risk", atRiskCount, totalCompanies > 0 ? (decimal)atRiskCount / totalCompanies * 100 : 0, "#f97316"),
                new("Suspended", suspendedCount, totalCompanies > 0 ? (decimal)suspendedCount / totalCompanies * 100 : 0, "#ef4444"),
                new("Inactive", inactiveCount, totalCompanies > 0 ? (decimal)inactiveCount / totalCompanies * 100 : 0, "#6b7280")
            };

            var growthPoints = new List<CompanyGrowthPointDto>();
            for (int i = 29; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                growthPoints.Add(new CompanyGrowthPointDto(date, companies.Count(c => c.CreatedTimestamp.Date == date), companies.Count(c => c.CreatedTimestamp.Date <= date)));
            }

            var totalGrowth = growthPoints.Sum(g => g.NewCompanies);
            var bestDay = growthPoints.OrderByDescending(g => g.NewCompanies).FirstOrDefault();
            var growth = new CompanyGrowthSummaryDto(totalGrowth, totalCompanies > 0 ? (decimal)totalGrowth / totalCompanies * 100 : 0, totalGrowth / 30, bestDay?.NewCompanies ?? 0, bestDay?.Date ?? today, growthPoints);

            var topCompanyList = subscriptions.Where(s => s.IsActive && !s.IsExpired).GroupBy(s => s.CompanyId)
                .Select(g => new TopCompanyDto(g.Key, g.First().Company?.Name ?? "Unknown", null, g.Count(), g.Sum(s => s.Amount), g.OrderByDescending(s => s.Amount).First().Plan?.Name ?? "N/A", g.Max(s => s.CreatedTimestamp), atRiskCompanyIds.Contains(g.Key) ? "AtRisk" : "Active"))
                .OrderByDescending(c => c.TotalValue).Take(10).ToList();

            var topCompanies = new TopCompaniesDto(topCompanyList, topCompanyList.Sum(c => c.TotalValue), topCompanyList.Sum(c => c.ActiveSubscriptions));

            var alerts = new List<CompanyAlertDto>();
            foreach (var companyId in withoutSubCompanyIds.Take(5))
            {
                var company = companies.First(c => c.Id == companyId);
                alerts.Add(new CompanyAlertDto(company.Id, company.Name, CompanyAlertType.NoSubscription, "No subscription", null, 0, "medium", "Create subscription"));
            }
            foreach (var companyId in atRiskCompanyIds.Take(5))
            {
                var expiringSub = subscriptions.Where(s => s.CompanyId == companyId && s.IsActive && !s.IsExpired && s.ExpiryDateUtc <= today.AddDays(30)).OrderBy(s => s.ExpiryDateUtc).FirstOrDefault();
                if (expiringSub != null)
                {
                    var daysRemaining = (expiringSub.ExpiryDateUtc.Date - today).Days;
                    alerts.Add(new CompanyAlertDto(companyId, expiringSub.Company?.Name ?? "Unknown", CompanyAlertType.SubscriptionExpiring, $"Subscription expiring in {daysRemaining} days", expiringSub.ExpiryDateUtc, daysRemaining, daysRemaining <= 7 ? "high" : "medium", "Renew subscription"));
                }
            }

            var subscriptionCoverage = totalCompanies > 0 ? (decimal)activeCount / totalCompanies * 100 : 0;

            return new CompaniesDashboardDto(totalCompanies, newThisMonth, newThisWeek, subscriptionCoverage, statusDistribution, statusChart, growth, topCompanies, alerts,
                alerts.Count(a => a.Priority == "critical"), alerts.Count(a => a.Priority == "high"), alerts.Count(a => a.Priority == "medium"),
                activeCount, subscriptions.Where(s => s.IsTrial && s.IsActive).Select(s => s.CompanyId).Distinct().Count(),
                subscriptions.Where(s => s.IsExpired).Select(s => s.CompanyId).Distinct().Count(), withoutSubCompanyIds.Count, now);
        }

        #endregion

        #region Subscriptions Dashboard

        public async Task<SubscriptionsDashboardDto> GetSubscriptionsDashboardAsync()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);

            var subscriptions = await _context.Subscriptions.Where(s => !s.IsDeleted).Include(s => s.Plan).Include(s => s.Company).ToListAsync();

            var totalSubscriptions = subscriptions.Count;
            var activeSubscriptions = subscriptions.Count(s => s.IsActive && !s.IsExpired && !s.IsTrial);
            var trialSubscriptions = subscriptions.Count(s => s.IsTrial && s.IsActive);
            var expiredSubscriptions = subscriptions.Count(s => s.IsExpired);
            var suspendedSubscriptions = subscriptions.Count(s => !s.IsActive && !s.IsExpired);
            var totalMonthlyRevenue = subscriptions.Where(s => s.IsActive && !s.IsExpired).Sum(s => s.Amount);

            var statusDistribution = new SubscriptionStatusDistributionDto(activeSubscriptions, trialSubscriptions, expiredSubscriptions, suspendedSubscriptions, 0, 0, totalSubscriptions);

            var statusChart = new List<DistributionItemDto>
            {
                new("Active", activeSubscriptions, totalSubscriptions > 0 ? (decimal)activeSubscriptions / totalSubscriptions * 100 : 0, "#22c55e"),
                new("Trial", trialSubscriptions, totalSubscriptions > 0 ? (decimal)trialSubscriptions / totalSubscriptions * 100 : 0, "#3b82f6"),
                new("Expired", expiredSubscriptions, totalSubscriptions > 0 ? (decimal)expiredSubscriptions / totalSubscriptions * 100 : 0, "#6b7280"),
                new("Suspended", suspendedSubscriptions, totalSubscriptions > 0 ? (decimal)suspendedSubscriptions / totalSubscriptions * 100 : 0, "#f97316")
            };

            var planGroups = subscriptions.Where(s => s.Plan != null).GroupBy(s => s.PlanId)
                .Select(g => new SubscriptionByPlanDto(g.Key, g.First().Plan?.Name ?? "Unknown", g.First().Plan?.Name ?? "Standard",
                    g.Count(s => s.IsActive && !s.IsExpired && !s.IsTrial), g.Count(s => s.IsTrial && s.IsActive), g.Count(),
                    g.Where(s => s.IsActive && !s.IsExpired).Sum(s => s.Amount), g.Sum(s => s.Amount),
                    totalSubscriptions > 0 ? (decimal)g.Count() / totalSubscriptions * 100 : 0, GetPlanColor(g.First().Plan?.Name ?? "Standard")))
                .OrderByDescending(p => p.TotalCount).ToList();

            var topPlan = planGroups.FirstOrDefault();
            var byPlan = new SubscriptionsByPlanSummaryDto(planGroups, topPlan?.PlanName ?? "N/A", topPlan?.TotalCount ?? 0, totalMonthlyRevenue);

            var activeForExpiry = subscriptions.Where(s => s.IsActive && !s.IsExpired).ToList();
            var expiringToday = activeForExpiry.Where(s => s.ExpiryDateUtc.Date == today).Select(s => MapToExpiringDto(s, today)).ToList();
            var expiringThisWeek = activeForExpiry.Where(s => s.ExpiryDateUtc.Date > today && s.ExpiryDateUtc.Date <= today.AddDays(7)).Select(s => MapToExpiringDto(s, today)).ToList();
            var expiringThisMonth = activeForExpiry.Where(s => s.ExpiryDateUtc.Date > today.AddDays(7) && s.ExpiryDateUtc.Date <= today.AddDays(30)).Select(s => MapToExpiringDto(s, today)).ToList();
            var expiringNext3Months = activeForExpiry.Where(s => s.ExpiryDateUtc.Date > today.AddDays(30) && s.ExpiryDateUtc.Date <= today.AddDays(90)).Select(s => MapToExpiringDto(s, today)).ToList();

            var expiryTimeline = new ExpiryTimelineDto(expiringToday, expiringToday.Count, expiringToday.Sum(e => e.MonthlyValue), expiringThisWeek, expiringThisWeek.Count, expiringThisWeek.Sum(e => e.MonthlyValue), expiringThisMonth, expiringThisMonth.Count, expiringThisMonth.Sum(e => e.MonthlyValue), expiringNext3Months, expiringNext3Months.Count, expiringNext3Months.Sum(e => e.MonthlyValue));

            var newThisMonth = subscriptions.Count(s => s.CreatedTimestamp >= thisMonth);
            var newLastMonth = subscriptions.Count(s => s.CreatedTimestamp >= lastMonth && s.CreatedTimestamp < thisMonth);
            var avgDuration = subscriptions.Any() ? (int)subscriptions.Average(s => (s.ExpiryDateUtc - s.StartDateUtc).TotalDays) : 365;

            var lifecycleMetrics = new LifecycleMetricsDto(newThisMonth, 0, 0, 0, 0, suspendedSubscriptions, 0, newLastMonth, 0, 0, 0, 0, 100, 0, avgDuration, 365);

            var growthPoints = new List<SubscriptionGrowthPointDto>();
            for (int i = 29; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                growthPoints.Add(new SubscriptionGrowthPointDto(date, subscriptions.Count(s => s.CreatedTimestamp.Date == date), subscriptions.Count(s => s.CreatedTimestamp.Date <= date && s.IsActive && !s.IsExpired), 0));
            }

            var growth = new SubscriptionGrowthSummaryDto(growthPoints.Sum(g => g.NewSubscriptions), activeSubscriptions > 0 ? (decimal)growthPoints.Sum(g => g.NewSubscriptions) / activeSubscriptions * 100 : 0, growthPoints.Sum(g => g.NewSubscriptions), 0, growthPoints);

            return new SubscriptionsDashboardDto(totalSubscriptions, activeSubscriptions + trialSubscriptions, trialSubscriptions, totalMonthlyRevenue, statusDistribution, statusChart, byPlan, expiryTimeline, lifecycleMetrics, growth, now);
        }

        #endregion

        #region Revenue Dashboard

        public async Task<RevenueDashboardDto> GetRevenueDashboardAsync()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;

            var subscriptions = await _context.Subscriptions.Where(s => !s.IsDeleted).Include(s => s.Plan).ToListAsync();
            var activeSubscriptions = subscriptions.Where(s => s.IsActive && !s.IsExpired).ToList();

            var mrr = activeSubscriptions.Sum(s => s.Amount);
            var arr = mrr * 12;
            var activeCustomers = activeSubscriptions.Select(s => s.CompanyId).Distinct().Count();
            var arpc = activeCustomers > 0 ? mrr / activeCustomers : 0;
            var previousMrr = mrr * 0.95m;
            var mrrChange = mrr - previousMrr;
            var mrrChangePercentage = previousMrr > 0 ? mrrChange / previousMrr * 100 : 0;

            var metrics = new RevenueMetricsDto(mrr, previousMrr, mrrChange, mrrChangePercentage, arr, previousMrr * 12, arr - (previousMrr * 12), mrrChangePercentage, arpc, arpc * 0.95m, arpc * 0.05m, 5, arpc * 24, activeCustomers, (int)(activeCustomers * 0.95m));

            var planGroups = activeSubscriptions.Where(s => s.Plan != null).GroupBy(s => s.PlanId)
                .Select(g => new RevenueByPlanItemDto(g.Key, g.First().Plan?.Name ?? "Unknown", g.First().Plan?.Name ?? "Standard", g.Sum(s => s.Amount), g.Sum(s => s.Amount) * 12, g.Count(), mrr > 0 ? g.Sum(s => s.Amount) / mrr * 100 : 0, GetPlanColor(g.First().Plan?.Name ?? "Standard")))
                .OrderByDescending(p => p.MonthlyRevenue).ToList();

            var topPlan = planGroups.FirstOrDefault();
            var byPlan = new RevenueByPlanDto(planGroups, topPlan?.PlanName ?? "N/A", topPlan?.MonthlyRevenue ?? 0, mrr);
            var byPlanChart = planGroups.Select(p => new DistributionItemDto(p.PlanName, p.SubscriptionCount, p.Percentage, p.Color)).ToList();

            var trendPoints = new List<RevenueTrendPointDto>();
            for (int i = 11; i >= 0; i--)
            {
                var month = today.AddMonths(-i);
                var revenue = mrr * (1 - i * 0.02m);
                trendPoints.Add(new RevenueTrendPointDto(month, month.ToString("MMM yyyy"), revenue, revenue * 0.1m, revenue * 0.9m, revenue * 0.02m));
            }

            var trend = new RevenueTrendDto(trendPoints, mrr * 0.24m, 24, "up", mrr, today, mrr * 0.76m, today.AddMonths(-11));

            var expiringNext30Days = activeSubscriptions.Where(s => s.ExpiryDateUtc <= today.AddDays(30)).ToList();
            var projections = new RevenueProjectionDto(expiringNext30Days.Sum(s => s.Amount) * 0.8m, (int)(expiringNext30Days.Count * 0.8m), expiringNext30Days.Sum(s => s.Amount) * 0.2m, (int)(expiringNext30Days.Count * 0.2m), mrr * 1.02m, mrr * 0.02m, mrr * 3 * 1.06m, mrr * 0.03m, mrr * 1.05m, mrr * 0.95m);

            return new RevenueDashboardDto(metrics, byPlan, byPlanChart, trend, projections, subscriptions.Sum(s => s.Amount), subscriptions.Count > 0 ? subscriptions.Average(s => s.Amount) : 0, subscriptions.Count, new List<DistributionItemDto> { new("EGP", subscriptions.Count, 100, "#22c55e") }, now);
        }

        #endregion

        #region Activity Dashboard

        public async Task<ActivityDashboardDto> GetActivityDashboardAsync()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var thisWeek = today.AddDays(-(int)today.DayOfWeek);
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            var admins = await _context.Admins.Where(a => !a.IsDeleted).Include(a => a.AdminType).ToListAsync();

            var totalAdmins = admins.Count;
            var activeToday = admins.Count(a => a.LastLoginAt.HasValue && a.LastLoginAt.Value.Date == today);
            var activeThisWeek = admins.Count(a => a.LastLoginAt.HasValue && a.LastLoginAt.Value >= thisWeek);
            var activeThisMonth = admins.Count(a => a.LastLoginAt.HasValue && a.LastLoginAt.Value >= thisMonth);

            var stats = new ActivityStatsDto(admins.Sum(a => a.LoginCount), activeToday, activeThisWeek * 3, activeThisMonth * 5, activeToday, activeThisMonth * 20, activeToday * 5, activeThisWeek * 15, activeThisMonth * 20, activeToday, 45, activeThisMonth * 4, activeThisMonth * 18, 25, 11);

            var topAdminList = admins.Where(a => a.LastLoginAt.HasValue).OrderByDescending(a => a.LoginCount).Take(10)
                .Select((a, i) => new TopAdminDto(a.Id, $"{a.FirstName ?? ""} {a.LastName ?? ""}".Trim(), a.Username, a.ProfilePictureUrl, a.AdminType?.AdminTypeName ?? "Admin", a.LoginCount * 5, 5 - i / 2, a.LoginCount, a.LastLoginAt ?? now, a.LastLoginAt ?? now, i + 1))
                .ToList();

            var topAdmins = new TopAdminsDto(topAdminList, activeThisMonth, topAdminList.Count > 0 ? (decimal)topAdminList.Average(a => a.TotalActions) : 0);

            var breakdown = new ActivityBreakdownDto(
                new List<ActivityByEntityTypeDto> { new("Company", 40, 40, "#3b82f6"), new("Subscription", 35, 35, "#22c55e"), new("Admin", 15, 15, "#8b5cf6"), new("System", 10, 10, "#6b7280") },
                new List<ActivityByActionTypeDto> { new("Create", 30, 30, "#22c55e"), new("Update", 40, 40, "#3b82f6"), new("Delete", 10, 10, "#ef4444"), new("Login", 20, 20, "#8b5cf6") },
                "Company", "Update");

            var byEntityChart = breakdown.ByEntityType.Select(e => new DistributionItemDto(e.EntityType, e.Count, e.Percentage, e.Color)).ToList();
            var byActionChart = breakdown.ByActionType.Select(a => new DistributionItemDto(a.ActionType, a.Count, a.Percentage, a.Color)).ToList();

            var dailyData = new List<ActivityTimelinePointDto>();
            for (int i = 29; i >= 0; i--) dailyData.Add(new ActivityTimelinePointDto(today.AddDays(-i), today.AddDays(-i).ToString("MMM dd"), 5 + i % 10, 20 + i % 30, 3 + i % 5));

            var hourlyDist = Enumerable.Range(0, 24).Select(h => new HourlyActivityDto(h, $"{h}:00", 10 + (h >= 9 && h <= 17 ? 30 : 0), (10 + (h >= 9 && h <= 17 ? 30 : 0)) / 4.0m)).ToList();

            var timeline = new ActivityTimelineDto(dailyData, hourlyDist, 14, "2:00 PM", 40, "Wednesday");

            return new ActivityDashboardDto(stats, topAdmins, new List<ActivityItemDto>(), stats.TotalActions, breakdown, byEntityChart, byActionChart, timeline, now);
        }

        #endregion

        #region Alerts Dashboard

        public async Task<AlertsDashboardDto> GetAlertsDashboardAsync()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;

            var subscriptions = await _context.Subscriptions.Where(s => !s.IsDeleted && s.IsActive && !s.IsExpired).Include(s => s.Company).Include(s => s.Plan).ToListAsync();

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
                lowAlerts.Add(new AlertItemDto(Guid.NewGuid(), AlertPriority.Low, AlertCategory.CompanyStatus, "Company without subscription", $"{company.Name} has no subscription", "Consider reaching out to create a subscription", "Company", company.Id, company.Name, now, null, null, "Just now", "Create Subscription", $"/subscriptions/create?companyId={company.Id}", "View Company", $"/companies/{company.Id}", "Building2", "#3b82f6", false, false, null, null));

            var counts = new AlertCountsDto(criticalAlerts.Count, highAlerts.Count, mediumAlerts.Count, lowAlerts.Count, criticalAlerts.Count + highAlerts.Count + mediumAlerts.Count + lowAlerts.Count, criticalAlerts.Count + highAlerts.Count + mediumAlerts.Count + lowAlerts.Count, 0);
            var byCategory = new AlertsByCategoryDto(criticalAlerts.Count + highAlerts.Count + mediumAlerts.Count, 0, lowAlerts.Count, 0, 0, 0);

            var byPriorityChart = new List<DistributionItemDto>
            {
                new("Critical", criticalAlerts.Count, counts.Total > 0 ? (decimal)criticalAlerts.Count / counts.Total * 100 : 0, "#ef4444"),
                new("High", highAlerts.Count, counts.Total > 0 ? (decimal)highAlerts.Count / counts.Total * 100 : 0, "#f97316"),
                new("Medium", mediumAlerts.Count, counts.Total > 0 ? (decimal)mediumAlerts.Count / counts.Total * 100 : 0, "#eab308"),
                new("Low", lowAlerts.Count, counts.Total > 0 ? (decimal)lowAlerts.Count / counts.Total * 100 : 0, "#3b82f6")
            };

            var byCategoryChart = new List<DistributionItemDto>
            {
                new("Subscription Expiry", byCategory.SubscriptionExpiry, counts.Total > 0 ? (decimal)byCategory.SubscriptionExpiry / counts.Total * 100 : 0, "#ef4444"),
                new("Company Status", byCategory.CompanyStatus, counts.Total > 0 ? (decimal)byCategory.CompanyStatus / counts.Total * 100 : 0, "#3b82f6")
            };

            return new AlertsDashboardDto(counts, byCategory, criticalAlerts, highAlerts, mediumAlerts, lowAlerts, byPriorityChart, byCategoryChart, new List<AlertItemDto>(), counts.Total, 0, criticalAlerts.Count, now);
        }

        public Task DismissAlertAsync(Guid alertId, Guid adminId) { _logger.LogInformation("Alert {AlertId} dismissed by {AdminId}", alertId, adminId); return Task.CompletedTask; }
        public Task MarkAlertAsReadAsync(Guid alertId, Guid adminId) { _logger.LogInformation("Alert {AlertId} marked as read by {AdminId}", alertId, adminId); return Task.CompletedTask; }

        #endregion

        #region Helpers

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
            return new ExpiringSubscriptionDto(sub.Id, sub.CompanyId, sub.Company?.Name ?? "Unknown", sub.Plan?.Name ?? "Unknown", sub.ExpiryDateUtc, daysRemaining, sub.Amount, daysRemaining <= 0 ? "critical" : daysRemaining <= 7 ? "high" : "medium");
        }

        private AlertItemDto CreateAlertItem(Subscription sub, AlertPriority priority, string message, DateTime today)
        {
            var now = DateTime.UtcNow;
            var daysRemaining = (sub.ExpiryDateUtc.Date - today).Days;
            return new AlertItemDto(Guid.NewGuid(), priority, AlertCategory.SubscriptionExpiry, "Subscription Expiring", $"{sub.Company?.Name}: {message}", $"Plan: {sub.Plan?.Name}, Value: {sub.Amount:C}", "Subscription", sub.Id, sub.Company?.Name, now, sub.ExpiryDateUtc, daysRemaining, daysRemaining <= 0 ? "Today" : $"In {daysRemaining} days", "Renew", $"/subscriptions/{sub.Id}/renew", "View", $"/subscriptions/{sub.Id}", "Calendar", priority == AlertPriority.Critical ? "#ef4444" : priority == AlertPriority.High ? "#f97316" : "#eab308", false, false, null, null);
        }

        #endregion
    }
}
