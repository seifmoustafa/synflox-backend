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
using Application.Services_Interfaces;
using Infrastructure.BackgroundJobs;
using Application.Mapping;

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
        services.AddOptions<ClientTokenSettings>()
            .Bind(configuration.GetSection("ClientTokenSettings"))
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
        })
        .AddJwtBearer("ClientToken", options =>
        {
            var clientTokenSettings = configuration.GetSection("ClientTokenSettings").Get<ClientTokenSettings>();
            if (clientTokenSettings != null)
            {
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = clientTokenSettings.ValidateIssuer,
                    ValidIssuer = clientTokenSettings.Issuer,
                    ValidateAudience = clientTokenSettings.ValidateAudience,
                    ValidAudience = clientTokenSettings.Audience,
                    ValidateIssuerSigningKey = clientTokenSettings.ValidateIssuerSigningKey,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(clientTokenSettings.SigningKey)),
                    ValidateLifetime = clientTokenSettings.ValidateLifetime,
                    ClockSkew = TimeSpan.FromMinutes(5),
                    RequireExpirationTime = true,
                    RequireSignedTokens = true
                };
            }
        });
        #endregion


        #region Services
        // register AutoMapper using profiles defined in the Application layer
        services.AddAutoMapper(typeof(AdminMappingProfile).Assembly);
        services.AddAutoMapper(typeof(MenuItemsMappingProfile).Assembly);
        services.AddAutoMapper(typeof(SubscriptionMappingProfile).Assembly);
        services.AddAutoMapper(typeof(CompanyMappingProfile).Assembly);
        services.AddAutoMapper(typeof(ClientTokenMappingProfile).Assembly);
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IBackupCodeService, BackupCodeService>();
        services.AddScoped<ISecurityAnalyticsService, SecurityAnalyticsService>();
        // SecurityNotificationService registered in WebAPI layer to avoid circular dependency with Hub
        services.AddScoped<IAdvancedSecurityAnalyticsService, AdvancedSecurityAnalyticsService>();
        services.AddScoped<ISecurityReportService, SecurityReportService>();
        services.AddScoped<IIdEncryptionService, IdEncryptionService>();
        
        // HttpClient for IP Geolocation API calls
        services.AddHttpClient();
        
        // IP Geolocation & User-Agent Parser (for real analytics)
        services.AddSingleton<IIpGeolocationService, IpGeolocationService>();
        services.AddSingleton<IUserAgentParserService, UserAgentParserService>();
        
        // FileHost Export Service (30-minute auto-cleanup)
        services.AddScoped<IFileHostExportService, FileHostExportService>();
        
        // Background job for cleaning expired export files
        services.AddHostedService<Infrastructure.BackgroundJobs.ExportFilesCleanupJob>();
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
        
        // Admin services - Split for Single Responsibility Principle
        services.AddScoped<IAdminService, AdminCrudService>();         // CRUD operations (SuperAdmin only)
        services.AddScoped<IAdminProfileService, AdminProfileService>(); // Profile operations (current user)
        
        services.AddScoped<IAdminTypeService, AdminTypeService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<ILicenseService, LicenseService>();
        services.AddScoped<IMenuItemsService, MenuItemsService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IDashboardService, DashboardService>();
        
        // Subscription Engine Services (Phase 1)
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IModuleService, ModuleService>();
        services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IEmailService, EmailService>();
        
        // Client Token Services (Phase 2)
        services.AddScoped<IClientTokenService, ClientTokenService>();
        services.AddScoped<IClientApiService, ClientApiService>();
        services.AddSingleton<ClientJwtService>(); // Singleton because JWT validation is stateless
        
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
        
        // Subscription Engine Background Jobs (Phase 1)
        services.AddHostedService<SubscriptionStatusBackgroundJob>();
        services.AddHostedService<OutboxProcessorBackgroundJob>();
        #endregion

        #region Repositories Registration
        // No need to register other Repository  as long as they dont have any other custom method that are not covered by BaseRepository
        services.AddScoped(typeof(IBaseRepository<,>), typeof(BaseRepository<,>));

        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<IAdminTypeRepository, AdminTypeRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IMenuItemsRepository, MenuItemsRepository>();

        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IBackupCodeRepository, BackupCodeRepository>();
        services.AddScoped<ISecurityAuditLogRepository, SecurityAuditLogRepository>();
        
        // Subscription Engine Repositories (Phase 1)
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IModuleRepository, ModuleRepository>();
        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IOutboxEventRepository, OutboxEventRepository>();
        
        // Client Token Repositories (Phase 2)
        services.AddScoped<IClientAccessTokenRepository, ClientAccessTokenRepository>();
        services.AddScoped<IClientTokenUsageLogRepository, ClientTokenUsageLogRepository>();
        
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        #endregion




        return services;
    }
}

