using Application.Services;
using Domain.Interfaces;
using Infrastructure.Authentication;
using Infrastructure.Resources;
using System.Globalization;
using System.Resources;
using Infrastructure.Context;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Configurations;
using Infrastructure.Settings;
using Infrastructure.Background;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Hosting;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure;

public static class InfrastructureServiceRegistration
{

    /// <summary>
    /// Registers persistence and authentication services.
    /// <para>Requires <c>SqlServerConnection</c> or <c>OracleConnection</c> connection string and
    /// a <c>JwtSettings</c> section in <c>appsettings.json</c>.</para>
    /// </summary>
    public static IServiceCollection AddInfrastructureService(this IServiceCollection services,
        IConfiguration configuration)
    {

        #region Database and Authentication configuration
        var resourceManager = new ResourceManager(typeof(SharedResource));
        var culture = CultureInfo.CurrentCulture;
        var jwtoptions = configuration.GetSection("JwtSettings").Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                resourceManager.GetString("JwtSettingsMissing", culture)
                ?? "JwtSettings section is missing");

        services.Configure<JwtOptions>(configuration.GetSection("JwtSettings"));
        services.AddOptions<EncryptionSettings>()
            .Bind(configuration.GetSection("Encryption"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<LicenseKeySettings>()
            .Bind(configuration.GetSection("LicenseKeySettings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Bind named schemes for file uploads/downloads (using lowercase for consistency)
        services.Configure<FileSettings>("image", configuration.GetSection("ImageSettings"));
        services.Configure<FileSettings>("pdf", configuration.GetSection("PdfSettings"));
        services.Configure<FileSettings>("pptx", configuration.GetSection("PptxSettings"));
        services.Configure<FileSettings>("video", configuration.GetSection("VideoSettings"));
        services.Configure<FileSettings>("any", configuration.GetSection("AllFileSettings"));

        services.AddDbContext<ApplicationDBContext>(options =>
        {
            var sqlServer = configuration.GetConnectionString("SqlServerConnection");
            if (!string.IsNullOrEmpty(sqlServer))
            {
                options.UseSqlServer(sqlServer);
            }
            else
            {
                var oracle = configuration.GetConnectionString("OracleConnection");
                if (string.IsNullOrEmpty(oracle))
                {
                    throw new InvalidOperationException(
                        resourceManager.GetString("DbConnectionMissing", culture)
                        ?? "Database connection string is missing");
                }
                options.UseOracle(oracle);
            }
        });

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtoptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtoptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtoptions.SecretKey))
            };
        });
        #endregion


        #region Services
        // register AutoMapper using profiles defined in the Application layer
        services.AddAutoMapper(typeof(Application.Mapping.AdminMappingProfile).Assembly);
        services.AddAutoMapper(typeof(Application.Mapping.LicensingMappingProfile).Assembly);
        services.AddAutoMapper(typeof(Application.Mapping.NotificationMappingProfile).Assembly);
        services.AddAutoMapper(typeof(Application.Mapping.ApiKeyMappingProfile).Assembly);
        services.AddAutoMapper(typeof(Application.Mapping.LoginAttemptMappingProfile).Assembly);
        services.AddAutoMapper(typeof(Application.Mapping.WebhookMappingProfile).Assembly);
        services.AddAutoMapper(typeof(Application.Mapping.SubscriptionPlanMappingProfile).Assembly);
        services.AddAutoMapper(typeof(Application.Mapping.PasswordPolicyMappingProfile).Assembly);
        services.AddAutoMapper(typeof(Application.Mapping.MenuItemsMappingProfile).Assembly);
        services.AddAutoMapper(typeof(Application.Mapping.ErrorLogMappingProfile).Assembly);
        services.AddAutoMapper(typeof(Application.Mapping.TenantMappingProfile).Assembly);
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IIdEncryptionService, IdEncryptionService>();
        
        // Register HttpClientFactory for webhook service
        services.AddHttpClient();
        
        // Configure distributed cache (Redis) if connection string provided and Redis is available
        // Otherwise, fallback to in-memory distributed cache
        var cacheConnectionString = configuration.GetConnectionString("RedisConnection");
        var useRedis = configuration.GetValue<bool>("CacheSettings:UseRedis", false);
        
        if (!string.IsNullOrEmpty(cacheConnectionString) && useRedis)
        {
            try
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = cacheConnectionString;
                    var instanceName = configuration["CacheSettings:InstanceName"] ?? "SYNFLOX";
                    options.InstanceName = instanceName;
                    // Increase timeout to prevent quick failures
                    options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
                    {
                        ConnectTimeout = 2000, // 2 seconds
                        SyncTimeout = 2000,
                        AbortOnConnectFail = false, // Don't fail if Redis is unavailable
                    };
                });
            }
            catch
            {
                // If Redis configuration fails, fallback to in-memory distributed cache
                services.AddDistributedMemoryCache();
                services.AddMemoryCache();
            }
        }
        else
        {
            // Use in-memory distributed cache (default for development)
            // This provides IDistributedCache implementation using memory
            services.AddDistributedMemoryCache();
            // Also register IMemoryCache for other services that might need it
            services.AddMemoryCache();
        }
        
        // Remove path-based localization so embedded resources from the
        // Infrastructure assembly are found correctly
        services.AddLocalization();
        services.AddScoped<ILocalizationService, LocalizationService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IAdminTypeService, AdminTypeService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<ILicensingService, LicensingService>();
        services.AddScoped<ISubscriptionHistoryService, SubscriptionHistoryService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<ILoginAttemptService, LoginAttemptService>();
        services.AddScoped<IWebhookService, WebhookService>();
        services.AddScoped<ICompanyUsageAnalyticsService, CompanyUsageAnalyticsService>();
        services.AddScoped<IMetricsService, MetricsService>();
        services.AddScoped<IErrorLogService, ErrorLogService>();
        services.AddScoped<ICompanyGroupService, CompanyGroupService>();
        services.AddScoped<ICompanyCustomFieldService, CompanyCustomFieldService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
        services.AddScoped<IPasswordPolicyService, PasswordPolicyService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IModuleService, ModuleService>();
        services.AddScoped<IProjectModuleService, ProjectModuleService>();
        
        // Register email services (validation removed - email services are optional)
        services.AddOptions<Infrastructure.Configurations.EmailSettings>()
            .Bind(configuration.GetSection("EmailSettings"));
        
        services.AddScoped<IEmailSender>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<Infrastructure.Configurations.EmailSettings>>().Value;
            return settings.Mode.Equals("Prod", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<Infrastructure.Services.SmtpEmailSender>()
                : sp.GetRequiredService<Infrastructure.Services.DevEmailSender>();
        });
        services.AddScoped<Infrastructure.Services.SmtpEmailSender>();
        services.AddScoped<Infrastructure.Services.DevEmailSender>();
        services.AddSingleton<Infrastructure.Services.ChannelEmailQueue>();
        services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<Infrastructure.Services.ChannelEmailQueue>());
        services.AddHostedService(sp => sp.GetRequiredService<Infrastructure.Services.ChannelEmailQueue>());
        
        services.AddScoped<IMenuItemsService, MenuItemsService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<Application.Services_Interfaces.ISearchService, SearchService>();
        services.AddScoped<IDashboardService, DashboardService>();
        
        // Download service with shared dictionary
        var downloadsDictionary = new System.Collections.Concurrent.ConcurrentDictionary<string, DownloadSession>();
        services.AddSingleton(downloadsDictionary);
        services.AddScoped<IDownloadService>(sp => 
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var env = sp.GetRequiredService<IWebHostEnvironment>();
            var logger = sp.GetRequiredService<ILogger<DownloadService>>();
            var configs = sp.GetRequiredService<IOptionsMonitor<FileSettings>>();
            return new DownloadService(config, env, downloadsDictionary, logger, configs);
        });
        
        // Upload service with shared dictionary for cleanup worker
        var uploadsDictionary = new System.Collections.Concurrent.ConcurrentDictionary<string, UploadInfo>();
        services.AddSingleton(uploadsDictionary);
        services.AddSingleton<IUploadService>(sp => 
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var env = sp.GetRequiredService<IWebHostEnvironment>();
            var logger = sp.GetRequiredService<ILogger<UploadService>>();
            var configs = sp.GetRequiredService<IOptionsMonitor<FileSettings>>();
            return new UploadService(config, env, uploadsDictionary, logger, configs);
        });
        
        services.AddHostedService<UploadCleanupWorker>();
        
        // Register ExpiryCheckSettings
        services.AddOptions<ExpiryCheckSettings>()
            .Bind(configuration.GetSection("ExpiryCheckSettings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        // Register SubscriptionExpiryWorker
        services.AddHostedService<SubscriptionExpiryWorker>();
        
        // Register NotificationSettings
        services.AddOptions<NotificationSettings>()
            .Bind(configuration.GetSection("NotificationSettings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        // Register ExpiryNotificationWorker
        services.AddHostedService<ExpiryNotificationWorker>();
        
        // Register LoginSecuritySettings
        services.AddOptions<LoginSecuritySettings>()
            .Bind(configuration.GetSection("LoginSecuritySettings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        #endregion

        #region Repositories Registration
        // No need to register other Repository  as long as they dont have any other custom method that are not covered by BaseRepository
        services.AddScoped(typeof(IBaseRepository<,>), typeof(BaseRepository<,>));

        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<IAdminTypeRepository, AdminTypeRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ISubscriptionHistoryRepository, SubscriptionHistoryRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<ILoginAttemptRepository, LoginAttemptRepository>();
        services.AddScoped<IWebhookRepository, WebhookRepository>();
        services.AddScoped<IWebhookDeliveryRepository, WebhookDeliveryRepository>();
        services.AddScoped<ICompanyUsageLogRepository, CompanyUsageLogRepository>();
        services.AddScoped<ISystemMetricRepository, SystemMetricRepository>();
        services.AddScoped<IErrorLogRepository, ErrorLogRepository>();
        services.AddScoped<ICompanyGroupRepository, CompanyGroupRepository>();
        services.AddScoped<ICompanyGroupMemberRepository, CompanyGroupMemberRepository>();
        services.AddScoped<ICompanyCustomFieldRepository, CompanyCustomFieldRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IModuleRepository, ModuleRepository>();
        services.AddScoped<IProjectModuleRepository, ProjectModuleRepository>();
        services.AddScoped<IPlanProjectModuleRepository, PlanProjectModuleRepository>();
        services.AddScoped<IPasswordPolicyRepository, PasswordPolicyRepository>();
        services.AddScoped<IPasswordHistoryRepository, PasswordHistoryRepository>();
        services.AddScoped<IMenuItemsRepository, MenuItemsRepository>();
        services.AddScoped<IReportDefinitionRepository, ReportDefinitionRepository>();

        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        #endregion




        return services;
    }
}

