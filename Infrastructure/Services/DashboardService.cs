using Application.DTOs.Dashboard;
using Application.Services;
using Domain.Entities.Authentication;
using Domain.Entities.Licensing;
using Domain.Entities.Subscriptions;
using Domain.Enums;
using Domain.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// Professional Dashboard Service with real data calculations
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IAdminRepository _adminRepository;
    private readonly IBaseRepository<Guid, AdminType> _adminTypeRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IBaseRepository<Guid, SubscriptionPlan> _planRepository;

    public DashboardService(
        ICompanyRepository companyRepository,
        IAdminRepository adminRepository,
        IBaseRepository<Guid, AdminType> adminTypeRepository,
        ISubscriptionRepository subscriptionRepository,
        IBaseRepository<Guid, SubscriptionPlan> planRepository)
    {
        _companyRepository = companyRepository;
        _adminRepository = adminRepository;
        _adminTypeRepository = adminTypeRepository;
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekAgo = now.AddDays(-7);
        var monthAgo = now.AddDays(-30);
        var dayAgo = now.AddDays(-1);

        // Get ALL data sequentially to avoid DbContext threading issues
        var companiesResult = await _companyRepository.GetAllAsync(null, 1, int.MaxValue);
        var companies = companiesResult.Item1.Cast<Company>().ToList();

        var adminsResult = await _adminRepository.GetAllAsync(null, 1, int.MaxValue);
        var admins = adminsResult.Item1.Cast<Admin>().ToList();

        var adminTypesResult = await _adminTypeRepository.GetAllAsync(null, 1, int.MaxValue);
        var adminTypes = adminTypesResult.Item1.Cast<AdminType>().ToList();

        var plansResult = await _planRepository.GetAllAsync(null, 1, int.MaxValue);
        var plans = plansResult.Item1.Cast<SubscriptionPlan>().ToList();

        // Get subscriptions for each company
        var subscriptions = new List<Subscription>();
        foreach (var company in companies)
        {
            var subscription = await _subscriptionRepository.GetActiveByCompanyIdAsync(company.Id);
            if (subscription != null)
            {
                subscriptions.Add(subscription);
            }
        }

        // Calculate company stats (categorized by subscription/license status)
        var companiesWithActiveLicense = 0;
        var companiesWithSuspendedLicense = 0;
        var companiesWithExpiredLicense = 0;

        foreach (var company in companies)
        {
            var subscription = subscriptions.FirstOrDefault(s => s.CompanyId == company.Id);
            var status = CalculateLicenseStatus(subscription);

            if (status == LicenseStatus.Active)
                companiesWithActiveLicense++;
            else if (status == LicenseStatus.Suspended)
                companiesWithSuspendedLicense++;
            else if (status == LicenseStatus.Expired)
                companiesWithExpiredLicense++;
        }

        // Calculate subscription stats
        var activeSubscriptions = subscriptions.Count(s => s.IsActive && !s.IsExpired);
        var trialSubscriptions = subscriptions.Count(s => s.IsTrial);
        var expiredSubscriptions = subscriptions.Count(s => s.IsExpired);
        var suspendedSubscriptions = subscriptions.Count(s => !s.IsActive && !s.IsExpired);

        var expiring7Days = 0;
        var expiring30Days = 0;
        var expiringToday = 0;

        foreach (var sub in subscriptions.Where(s => s.IsActive && !s.IsExpired))
        {
            var daysUntilExpiry = (sub.ExpiryDateUtc - now).Days;

            if (daysUntilExpiry <= 0)
                expiringToday++;
            if (daysUntilExpiry > 0 && daysUntilExpiry <= 7)
                expiring7Days++;
            if (daysUntilExpiry > 7 && daysUntilExpiry <= 30)
                expiring30Days++;
        }

        // Subscriptions by plan
        var subscriptionsByPlan = new Dictionary<string, int>();
        foreach (var plan in plans)
        {
            var count = subscriptions.Count(s => s.PlanId == plan.Id);
            if (count > 0)
            {
                subscriptionsByPlan[plan.Name] = count;
            }
        }

        // Admins by type
        var adminsByType = new Dictionary<string, int>();
        foreach (var adminType in adminTypes)
        {
            var count = admins.Count(a => a.AdminTypeId == adminType.Id);
            if (count > 0)
            {
                adminsByType[adminType.AdminTypeName] = count;
            }
        }

        // Build alert messages
        var alertMessages = new List<string>();
        if (expiringToday > 0)
            alertMessages.Add($"{expiringToday} subscription(s) expiring today");
        if (expiring7Days > 0)
            alertMessages.Add($"{expiring7Days} subscription(s) expiring within 7 days");
        if (companiesWithSuspendedLicense > 0)
            alertMessages.Add($"{companiesWithSuspendedLicense} company(ies) with suspended license");
        if (companiesWithExpiredLicense > 0)
            alertMessages.Add($"{companiesWithExpiredLicense} company(ies) with expired/no license");

        return new DashboardDto
        {
            Overview = new OverviewStatsDto
            {
                TotalCompanies = companies.Count,
                ActiveCompanies = companiesWithActiveLicense,
                TotalSubscriptions = subscriptions.Count,
                ActiveSubscriptions = activeSubscriptions,
                TotalAdmins = admins.Count,
                ActiveAdmins = admins.Count(a => a.IsActive)
            },
            Companies = new CompanyStatsDto
            {
                Total = companies.Count,
                ActiveLicense = companiesWithActiveLicense,
                SuspendedLicense = companiesWithSuspendedLicense,
                ExpiredLicense = companiesWithExpiredLicense,
                CreatedToday = companies.Count(c => c.CreatedTimestamp.Date == today),
                CreatedThisWeek = companies.Count(c => c.CreatedTimestamp >= weekAgo),
                CreatedThisMonth = companies.Count(c => c.CreatedTimestamp >= monthAgo)
            },
            Subscriptions = new SubscriptionStatsDto
            {
                Total = subscriptions.Count,
                Active = activeSubscriptions,
                Trial = trialSubscriptions,
                Expired = expiredSubscriptions,
                Suspended = suspendedSubscriptions,
                ExpiringWithin7Days = expiring7Days,
                ExpiringWithin30Days = expiring30Days,
                CreatedToday = subscriptions.Count(s => s.StartDateUtc.Date == today),
                CreatedThisWeek = subscriptions.Count(s => s.StartDateUtc >= weekAgo),
                CreatedThisMonth = subscriptions.Count(s => s.StartDateUtc >= monthAgo),
                ByPlan = subscriptionsByPlan
            },
            Admins = new AdminStatsDto
            {
                Total = admins.Count,
                Active = admins.Count(a => a.IsActive),
                Inactive = admins.Count(a => !a.IsActive),
                CreatedToday = admins.Count(a => a.CreatedTimestamp.Date == today),
                CreatedThisWeek = admins.Count(a => a.CreatedTimestamp >= weekAgo),
                CreatedThisMonth = admins.Count(a => a.CreatedTimestamp >= monthAgo),
                ByType = adminsByType
            },
            Alerts = new AlertsDto
            {
                SubscriptionsExpiringToday = expiringToday,
                SubscriptionsExpiringThisWeek = expiring7Days,
                CompaniesWithSuspendedLicense = companiesWithSuspendedLicense,
                CompaniesWithExpiredLicense = companiesWithExpiredLicense,
                InactiveAdmins = admins.Count(a => !a.IsActive),
                Messages = alertMessages
            },
            RecentActivity = new RecentActivityDto
            {
                CompaniesLast24Hours = companies.Count(c => c.CreatedTimestamp >= dayAgo),
                SubscriptionsLast24Hours = subscriptions.Count(s => s.StartDateUtc >= dayAgo),
                AdminsLast24Hours = admins.Count(a => a.CreatedTimestamp >= dayAgo)
            },
            TimeSeries = await GetTimeSeriesDataAsync(companies, subscriptions, admins),
            GeneratedAtUtc = DateTime.UtcNow
        };
    }

    public async Task<SystemStatisticsDto> GetSystemStatisticsAsync()
    {
        var now = DateTime.UtcNow;
        var sevenDaysAgo = now.AddDays(-7);
        var thirtyDaysAgo = now.AddDays(-30);

        var companiesResult = await _companyRepository.GetAllAsync(null, 1, int.MaxValue);
        var companies = companiesResult.Item1.Cast<Company>().ToList();

        var adminsResult = await _adminRepository.GetAllAsync(null, 1, int.MaxValue);
        var admins = adminsResult.Item1.Cast<Admin>().ToList();

        var adminTypesCount = await _adminTypeRepository.Count();

        // Calculate license status
        var activeCount = 0;
        var expiredCount = 0;
        var suspendedCount = 0;
        var expiringSoonCount = 0;

        foreach (var company in companies)
        {
            var subscription = await _subscriptionRepository.GetActiveByCompanyIdAsync(company.Id);
            var status = CalculateLicenseStatus(subscription);

            if (status == LicenseStatus.Active)
                activeCount++;
            else if (status == LicenseStatus.Expired)
                expiredCount++;
            else if (status == LicenseStatus.Suspended)
                suspendedCount++;

            if (subscription != null && !subscription.IsExpired && subscription.IsActive)
            {
                var daysUntilExpiry = (subscription.ExpiryDateUtc - now).Days;
                if (daysUntilExpiry > 0 && daysUntilExpiry <= 30)
                    expiringSoonCount++;
            }
        }

        return new SystemStatisticsDto
        {
            TotalCompanies = companies.Count,
            TotalAdmins = admins.Count,
            TotalAdminTypes = adminTypesCount,
            LicenseStatusStats = new LicenseStatusStatsDto
            {
                Active = activeCount,
                Expired = expiredCount,
                Suspended = suspendedCount
            },
            ActiveAdmins = admins.Count(a => a.IsActive),
            InactiveAdmins = admins.Count(a => !a.IsActive),
            CompaniesExpiringSoon = expiringSoonCount,
            RecentlyCreatedCompanies = companies.Count(c => c.CreatedTimestamp >= sevenDaysAgo),
            RecentlyCreatedAdmins = admins.Count(a => a.CreatedTimestamp >= sevenDaysAgo)
        };
    }

    public async Task<CompanyStatsDto> GetCompanyAnalyticsAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekAgo = now.AddDays(-7);
        var monthAgo = now.AddDays(-30);

        var companiesResult = await _companyRepository.GetAllAsync(null, 1, int.MaxValue);
        var companies = companiesResult.Item1.Cast<Company>().ToList();

        var subscriptions = new List<Subscription>();
        foreach (var company in companies)
        {
            var subscription = await _subscriptionRepository.GetActiveByCompanyIdAsync(company.Id);
            if (subscription != null)
            {
                subscriptions.Add(subscription);
            }
        }

        var companiesWithActiveLicense = 0;
        var companiesWithSuspendedLicense = 0;
        var companiesWithExpiredLicense = 0;

        foreach (var company in companies)
        {
            var subscription = subscriptions.FirstOrDefault(s => s.CompanyId == company.Id);
            var status = CalculateLicenseStatus(subscription);

            if (status == LicenseStatus.Active)
                companiesWithActiveLicense++;
            else if (status == LicenseStatus.Suspended)
                companiesWithSuspendedLicense++;
            else if (status == LicenseStatus.Expired)
                companiesWithExpiredLicense++;
        }

        return new CompanyStatsDto
        {
            Total = companies.Count,
            ActiveLicense = companiesWithActiveLicense,
            SuspendedLicense = companiesWithSuspendedLicense,
            ExpiredLicense = companiesWithExpiredLicense,
            CreatedToday = companies.Count(c => c.CreatedTimestamp.Date == today),
            CreatedThisWeek = companies.Count(c => c.CreatedTimestamp >= weekAgo),
            CreatedThisMonth = companies.Count(c => c.CreatedTimestamp >= monthAgo)
        };
    }

    public async Task<SubscriptionStatsDto> GetSubscriptionAnalyticsAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekAgo = now.AddDays(-7);
        var monthAgo = now.AddDays(-30);

        var companiesResult = await _companyRepository.GetAllAsync(null, 1, int.MaxValue);
        var companies = companiesResult.Item1.Cast<Company>().ToList();

        var plansResult = await _planRepository.GetAllAsync(null, 1, int.MaxValue);
        var plans = plansResult.Item1.Cast<SubscriptionPlan>().ToList();

        var subscriptions = new List<Subscription>();
        foreach (var company in companies)
        {
            var subscription = await _subscriptionRepository.GetActiveByCompanyIdAsync(company.Id);
            if (subscription != null)
            {
                subscriptions.Add(subscription);
            }
        }

        var activeSubscriptions = subscriptions.Count(s => s.IsActive && !s.IsExpired);
        var trialSubscriptions = subscriptions.Count(s => s.IsTrial);
        var expiredSubscriptions = subscriptions.Count(s => s.IsExpired);
        var suspendedSubscriptions = subscriptions.Count(s => !s.IsActive && !s.IsExpired);

        var expiring7Days = 0;
        var expiring30Days = 0;

        foreach (var sub in subscriptions.Where(s => s.IsActive && !s.IsExpired))
        {
            var daysUntilExpiry = (sub.ExpiryDateUtc - now).Days;

            if (daysUntilExpiry > 0 && daysUntilExpiry <= 7)
                expiring7Days++;
            if (daysUntilExpiry > 7 && daysUntilExpiry <= 30)
                expiring30Days++;
        }

        var subscriptionsByPlan = new Dictionary<string, int>();
        foreach (var plan in plans)
        {
            var count = subscriptions.Count(s => s.PlanId == plan.Id);
            if (count > 0)
            {
                subscriptionsByPlan[plan.Name] = count;
            }
        }

        return new SubscriptionStatsDto
        {
            Total = subscriptions.Count,
            Active = activeSubscriptions,
            Trial = trialSubscriptions,
            Expired = expiredSubscriptions,
            Suspended = suspendedSubscriptions,
            ExpiringWithin7Days = expiring7Days,
            ExpiringWithin30Days = expiring30Days,
            CreatedToday = subscriptions.Count(s => s.StartDateUtc.Date == today),
            CreatedThisWeek = subscriptions.Count(s => s.StartDateUtc >= weekAgo),
            CreatedThisMonth = subscriptions.Count(s => s.StartDateUtc >= monthAgo),
            ByPlan = subscriptionsByPlan
        };
    }

    public async Task<AdminStatsDto> GetAdminAnalyticsAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekAgo = now.AddDays(-7);
        var monthAgo = now.AddDays(-30);

        var adminsResult = await _adminRepository.GetAllAsync(null, 1, int.MaxValue);
        var admins = adminsResult.Item1.Cast<Admin>().ToList();

        var adminTypesResult = await _adminTypeRepository.GetAllAsync(null, 1, int.MaxValue);
        var adminTypes = adminTypesResult.Item1.Cast<AdminType>().ToList();

        var adminsByType = new Dictionary<string, int>();
        foreach (var adminType in adminTypes)
        {
            var count = admins.Count(a => a.AdminTypeId == adminType.Id);
            if (count > 0)
            {
                adminsByType[adminType.AdminTypeName] = count;
            }
        }

        return new AdminStatsDto
        {
            Total = admins.Count,
            Active = admins.Count(a => a.IsActive),
            Inactive = admins.Count(a => !a.IsActive),
            CreatedToday = admins.Count(a => a.CreatedTimestamp.Date == today),
            CreatedThisWeek = admins.Count(a => a.CreatedTimestamp >= weekAgo),
            CreatedThisMonth = admins.Count(a => a.CreatedTimestamp >= monthAgo),
            ByType = adminsByType
        };
    }

    public async Task<AlertsDto> GetAlertsAsync()
    {
        var now = DateTime.UtcNow;

        var companiesResult = await _companyRepository.GetAllAsync(null, 1, int.MaxValue);
        var companies = companiesResult.Item1.Cast<Company>().ToList();

        var adminsResult = await _adminRepository.GetAllAsync(null, 1, int.MaxValue);
        var admins = adminsResult.Item1.Cast<Admin>().ToList();

        var subscriptions = new List<Subscription>();
        foreach (var company in companies)
        {
            var subscription = await _subscriptionRepository.GetActiveByCompanyIdAsync(company.Id);
            if (subscription != null)
            {
                subscriptions.Add(subscription);
            }
        }

        var companiesWithSuspendedLicense = 0;
        var companiesWithExpiredLicense = 0;

        foreach (var company in companies)
        {
            var subscription = subscriptions.FirstOrDefault(s => s.CompanyId == company.Id);
            var status = CalculateLicenseStatus(subscription);

            if (status == LicenseStatus.Suspended)
                companiesWithSuspendedLicense++;
            else if (status == LicenseStatus.Expired)
                companiesWithExpiredLicense++;
        }

        var expiringToday = 0;
        var expiring7Days = 0;

        foreach (var sub in subscriptions.Where(s => s.IsActive && !s.IsExpired))
        {
            var daysUntilExpiry = (sub.ExpiryDateUtc - now).Days;

            if (daysUntilExpiry <= 0)
                expiringToday++;
            if (daysUntilExpiry > 0 && daysUntilExpiry <= 7)
                expiring7Days++;
        }

        var alertMessages = new List<string>();
        if (expiringToday > 0)
            alertMessages.Add($"{expiringToday} subscription(s) expiring today");
        if (expiring7Days > 0)
            alertMessages.Add($"{expiring7Days} subscription(s) expiring within 7 days");
        if (companiesWithSuspendedLicense > 0)
            alertMessages.Add($"{companiesWithSuspendedLicense} company(ies) with suspended license");
        if (companiesWithExpiredLicense > 0)
            alertMessages.Add($"{companiesWithExpiredLicense} company(ies) with expired/no license");

        return new AlertsDto
        {
            SubscriptionsExpiringToday = expiringToday,
            SubscriptionsExpiringThisWeek = expiring7Days,
            CompaniesWithSuspendedLicense = companiesWithSuspendedLicense,
            CompaniesWithExpiredLicense = companiesWithExpiredLicense,
            InactiveAdmins = admins.Count(a => !a.IsActive),
            Messages = alertMessages
        };
    }

    public async Task<RecentActivityDto> GetRecentActivityAsync()
    {
        var dayAgo = DateTime.UtcNow.AddDays(-1);

        var companiesResult = await _companyRepository.GetAllAsync(null, 1, int.MaxValue);
        var companies = companiesResult.Item1.Cast<Company>().ToList();

        var adminsResult = await _adminRepository.GetAllAsync(null, 1, int.MaxValue);
        var admins = adminsResult.Item1.Cast<Admin>().ToList();

        var subscriptions = new List<Subscription>();
        foreach (var company in companies)
        {
            var subscription = await _subscriptionRepository.GetActiveByCompanyIdAsync(company.Id);
            if (subscription != null)
            {
                subscriptions.Add(subscription);
            }
        }

        return new RecentActivityDto
        {
            CompaniesLast24Hours = companies.Count(c => c.CreatedTimestamp >= dayAgo),
            SubscriptionsLast24Hours = subscriptions.Count(s => s.StartDateUtc >= dayAgo),
            AdminsLast24Hours = admins.Count(a => a.CreatedTimestamp >= dayAgo)
        };
    }

    private async Task<TimeSeriesDto> GetTimeSeriesDataAsync(
        List<Company> companies,
        List<Subscription> subscriptions,
        List<Admin> admins)
    {
        var last30Days = new List<DailyMetricDto>();
        var startDate = DateTime.UtcNow.AddDays(-30).Date;

        for (int i = 0; i < 30; i++)
        {
            var date = startDate.AddDays(i);
            var nextDate = date.AddDays(1);

            // Count entities created on this day
            var companiesCreated = companies.Count(c => c.CreatedTimestamp.Date == date);
            var subscriptionsCreated = subscriptions.Count(s => s.StartDateUtc.Date == date);
            var adminsCreated = admins.Count(a => a.CreatedTimestamp.Date == date);

            // Count active entities as of end of this day
            var companiesActive = companies.Count(c => 
                c.CreatedTimestamp <= nextDate && 
                !c.IsDeleted &&
                subscriptions.Any(s => 
                    s.CompanyId == c.Id && 
                    s.IsActive && 
                    !s.IsExpired &&
                    s.StartDateUtc <= nextDate
                )
            );

            var subscriptionsActive = subscriptions.Count(s => 
                s.StartDateUtc <= nextDate && 
                !s.IsDeleted &&
                s.IsActive && 
                !s.IsExpired
            );

            var adminsActive = admins.Count(a => 
                a.CreatedTimestamp <= nextDate && 
                !a.IsDeleted &&
                a.IsActive
            );

            last30Days.Add(new DailyMetricDto
            {
                Date = date,
                CompaniesCreated = companiesCreated,
                SubscriptionsCreated = subscriptionsCreated,
                AdminsCreated = adminsCreated,
                CompaniesActive = companiesActive,
                SubscriptionsActive = subscriptionsActive,
                AdminsActive = adminsActive
            });
        }

        return new TimeSeriesDto { Last30Days = last30Days };
    }

    private LicenseStatus CalculateLicenseStatus(Subscription? subscription)
    {
        if (subscription == null || subscription.IsExpired)
            return LicenseStatus.Expired;

        if (!subscription.IsActive)
            return LicenseStatus.Suspended;

        var now = DateTime.UtcNow;
        if (subscription.ExpiryDateUtc.AddDays(subscription.Plan?.GracePeriodDays ?? 0) < now)
            return LicenseStatus.Expired;

        return LicenseStatus.Active;
    }
}
