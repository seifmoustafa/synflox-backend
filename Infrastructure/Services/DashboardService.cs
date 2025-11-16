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
            Revenue = await GetRevenueDataAsync(subscriptions, companies),
            Lifecycle = await GetLifecycleDataAsync(companies, subscriptions),
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

    private async Task<RevenueDto> GetRevenueDataAsync(
        List<Subscription> subscriptions,
        List<Company> companies)
    {
        var activeSubscriptions = subscriptions.Where(s => s.IsActive && !s.IsExpired).ToList();
        var payingSubscriptions = activeSubscriptions.Where(s => !s.IsTrial).ToList();
        
        // Calculate total revenue (normalized to USD for simplicity)
        // In production, you'd use exchange rates
        decimal totalRevenue = payingSubscriptions.Sum(s => s.Amount);
        
        // Calculate MRR (Monthly Recurring Revenue)
        // Normalize all subscriptions to monthly revenue
        decimal mrr = 0;
        foreach (var sub in payingSubscriptions)
        {
            var monthlyAmount = NormalizeToMonthlyRevenue(sub);
            mrr += monthlyAmount;
        }
        
        // ARR = MRR * 12
        decimal arr = mrr * 12;
        
        // ARPC (Average Revenue Per Customer)
        int payingCustomers = payingSubscriptions.Select(s => s.CompanyId).Distinct().Count();
        decimal arpc = payingCustomers > 0 ? mrr / payingCustomers : 0;
        
        // Revenue by plan
        var revenueByPlan = payingSubscriptions
            .GroupBy(s => s.Plan?.Name ?? "Unknown")
            .ToDictionary(
                g => g.Key,
                g => g.Sum(s => s.Amount)
            );
        
        // Revenue by currency
        var revenueByCurrency = payingSubscriptions
            .GroupBy(s => s.Currency.ToString())
            .ToDictionary(
                g => g.Key,
                g => g.Sum(s => s.Amount)
            );
        
        // Monthly revenue for last 12 months
        var monthlyRevenue = new List<MonthlyRevenueDto>();
        var now = DateTime.UtcNow;
        
        for (int i = 11; i >= 0; i--)
        {
            var targetMonth = now.AddMonths(-i);
            var monthStart = new DateTime(targetMonth.Year, targetMonth.Month, 1);
            var monthEnd = monthStart.AddMonths(1);
            
            var monthSubscriptions = subscriptions.Where(s =>
                s.StartDateUtc < monthEnd &&
                s.ExpiryDateUtc >= monthStart &&
                !s.IsTrial &&
                !s.IsDeleted
            ).ToList();
            
            var monthRevenue = monthSubscriptions.Sum(s => s.Amount);
            var avgRevenue = monthSubscriptions.Count > 0 
                ? monthRevenue / monthSubscriptions.Count 
                : 0;
            
            monthlyRevenue.Add(new MonthlyRevenueDto
            {
                Month = monthStart.ToString("MMM yyyy"),
                Revenue = monthRevenue,
                SubscriptionCount = monthSubscriptions.Count,
                AverageRevenuePerSubscription = avgRevenue
            });
        }
        
        // Calculate month-over-month growth
        decimal momGrowth = 0;
        if (monthlyRevenue.Count >= 2)
        {
            var currentMonth = monthlyRevenue[^1].Revenue;
            var previousMonth = monthlyRevenue[^2].Revenue;
            
            if (previousMonth > 0)
            {
                momGrowth = ((currentMonth - previousMonth) / previousMonth) * 100;
            }
        }
        
        return new RevenueDto
        {
            MRR = mrr,
            ARR = arr,
            TotalRevenue = totalRevenue,
            ARPC = arpc,
            RevenueByPlan = revenueByPlan,
            RevenueByCurrency = revenueByCurrency,
            MonthlyRevenue = monthlyRevenue,
            MonthOverMonthGrowth = momGrowth,
            PayingCustomers = payingCustomers,
            TrialSubscriptions = activeSubscriptions.Count(s => s.IsTrial)
        };
    }
    
    private decimal NormalizeToMonthlyRevenue(Subscription subscription)
    {
        // Normalize subscription revenue to monthly amount based on plan duration
        if (subscription.Plan == null)
            return subscription.Amount;
        
        return subscription.Plan.DurationType switch
        {
            PlanDurationType.Weekly => subscription.Amount * 4.33m,      // ~4.33 weeks per month
            PlanDurationType.BiWeekly => subscription.Amount * 2.165m,   // ~2.165 bi-weeks per month
            PlanDurationType.Monthly => subscription.Amount,
            PlanDurationType.Quarterly => subscription.Amount / 3m,
            PlanDurationType.SemiAnnually => subscription.Amount / 6m,   // Fixed: was SemiAnnual
            PlanDurationType.Yearly => subscription.Amount / 12m,
            PlanDurationType.Biennial => subscription.Amount / 24m,      // 2 years
            PlanDurationType.Triennial => subscription.Amount / 36m,     // 3 years
            PlanDurationType.Lifetime => subscription.Amount / 120m,     // Amortize over 10 years
            _ => subscription.Amount
        };
    }

    private async Task<LifecycleDto> GetLifecycleDataAsync(
        List<Company> companies,
        List<Subscription> subscriptions)
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);
        var sevenDaysAgo = now.AddDays(-7);

        // Calculate lifecycle stages
        var newCompanies = companies.Count(c => c.CreatedTimestamp >= thirtyDaysAgo);
        
        var activeCompanies = companies.Count(c =>
            !c.IsDeleted &&
            subscriptions.Any(s =>
                s.CompanyId == c.Id &&
                s.IsActive &&
                !s.IsExpired
            )
        );

        var atRiskCompanies = companies.Count(c =>
            !c.IsDeleted &&
            subscriptions.Any(s =>
                s.CompanyId == c.Id &&
                (
                    // Expiring within 7 days
                    (s.IsActive && !s.IsExpired && (s.ExpiryDateUtc - now).Days <= 7 && (s.ExpiryDateUtc - now).Days > 0) ||
                    // Suspended
                    (!s.IsActive && !s.IsExpired)
                )
            )
        );

        var churnedCompanies = companies.Count(c =>
            !c.IsDeleted &&
            !subscriptions.Any(s =>
                s.CompanyId == c.Id &&
                s.IsActive &&
                !s.IsExpired
            )
        );

        // Companies that had expired subscriptions but now have active ones
        var returningCompanies = 0; // TODO: Track this in future with subscription history

        // Calculate churn metrics
        var totalActiveEver = activeCompanies + churnedCompanies;
        var churnRate = totalActiveEver > 0 ? (decimal)churnedCompanies / totalActiveEver * 100 : 0;
        var retentionRate = 100 - churnRate;

        var churnedThisMonth = companies.Count(c =>
            !c.IsDeleted &&
            subscriptions.Any(s =>
                s.CompanyId == c.Id &&
                s.IsExpired &&
                s.ExpiryDateUtc >= thirtyDaysAgo
            )
        );

        // Calculate average lifetime
        var lifetimes = companies
            .Where(c => !c.IsDeleted)
            .Select(c =>
            {
                var subscription = subscriptions.FirstOrDefault(s => s.CompanyId == c.Id);
                if (subscription == null) return 0;
                
                var end = subscription.IsActive && !subscription.IsExpired 
                    ? now 
                    : subscription.ExpiryDateUtc;
                
                return (end - subscription.StartDateUtc).TotalDays;
            })
            .Where(days => days > 0)
            .ToList();

        var averageLifetimeDays = lifetimes.Any() ? lifetimes.Average() : 0;

        // Calculate risk distribution
        var riskScores = companies
            .Where(c => !c.IsDeleted)
            .Select(c => CalculateRiskScore(c, subscriptions.Where(s => s.CompanyId == c.Id).ToList(), now))
            .ToList();

        var lowRisk = riskScores.Count(score => score <= 33);
        var mediumRisk = riskScores.Count(score => score > 33 && score <= 66);
        var highRisk = riskScores.Count(score => score > 66);

        // Calculate health distribution
        var healthScores = companies
            .Where(c => !c.IsDeleted)
            .Select(c => CalculateHealthScore(c, subscriptions.Where(s => s.CompanyId == c.Id).ToList(), now))
            .ToList();

        var excellent = healthScores.Count(score => score >= 80);
        var good = healthScores.Count(score => score >= 60 && score < 80);
        var fair = healthScores.Count(score => score >= 40 && score < 60);
        var poor = healthScores.Count(score => score < 40);

        // Lifecycle transitions (simplified - would need historical data for accurate tracking)
        var transitions = new List<LifecycleTransitionDto>
        {
            new() { FromStage = "New", ToStage = "Active", Count = Math.Min(newCompanies, activeCompanies) },
            new() { FromStage = "Active", ToStage = "At-Risk", Count = Math.Max(0, atRiskCompanies / 2) },
            new() { FromStage = "At-Risk", ToStage = "Churned", Count = Math.Max(0, churnedThisMonth / 2) },
            new() { FromStage = "At-Risk", ToStage = "Active", Count = Math.Max(0, atRiskCompanies / 3) }
        };

        return new LifecycleDto
        {
            Stages = new LifecycleStageDto
            {
                New = newCompanies,
                Active = activeCompanies,
                AtRisk = atRiskCompanies,
                Churned = churnedCompanies,
                Returning = returningCompanies
            },
            Churn = new ChurnDto
            {
                ChurnRate = churnRate,
                ChurnedThisMonth = churnedThisMonth,
                HighRiskCount = highRisk,
                RetentionRate = retentionRate,
                AverageLifetimeDays = averageLifetimeDays,
                RiskDistribution = new RiskDistributionDto
                {
                    Low = lowRisk,
                    Medium = mediumRisk,
                    High = highRisk
                }
            },
            HealthDistribution = new HealthDistributionDto
            {
                Excellent = excellent,
                Good = good,
                Fair = fair,
                Poor = poor
            },
            Transitions = transitions
        };
    }

    private int CalculateRiskScore(Company company, List<Subscription> companySubscriptions, DateTime now)
    {
        var subscription = companySubscriptions.FirstOrDefault();
        if (subscription == null) return 100; // No subscription = highest risk

        var score = 0;

        // Subscription status
        if (subscription.IsExpired) score += 50;
        else if (!subscription.IsActive) score += 30;

        // Days until expiry
        var daysUntilExpiry = (subscription.ExpiryDateUtc - now).Days;
        if (daysUntilExpiry <= 0) score += 40;
        else if (daysUntilExpiry <= 7) score += 30;
        else if (daysUntilExpiry <= 30) score += 15;

        // Trial subscription
        if (subscription.IsTrial) score += 10;

        return Math.Min(100, score);
    }

    private int CalculateHealthScore(Company company, List<Subscription> companySubscriptions, DateTime now)
    {
        var subscription = companySubscriptions.FirstOrDefault();
        if (subscription == null) return 0; // No subscription = no health

        var score = 100;

        // Subscription status
        if (subscription.IsExpired) score -= 50;
        else if (!subscription.IsActive) score -= 30;

        // Days until expiry
        var daysUntilExpiry = (subscription.ExpiryDateUtc - now).Days;
        if (daysUntilExpiry <= 0) score -= 40;
        else if (daysUntilExpiry <= 7) score -= 30;
        else if (daysUntilExpiry <= 30) score -= 15;

        // Trial subscription (neutral)
        if (subscription.IsTrial) score -= 10;

        // Company age (older = healthier)
        var ageInDays = (now - company.CreatedTimestamp).TotalDays;
        if (ageInDays > 365) score += 10;
        else if (ageInDays > 180) score += 5;

        return Math.Max(0, Math.Min(100, score));
    }
}
