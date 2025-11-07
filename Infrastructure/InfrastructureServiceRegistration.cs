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
        services.AddOptions<EmailSettings>()
            .Bind(configuration.GetSection("EmailSettings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<VerificationOptions>()
            .Bind(configuration.GetSection("Verification"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<SmsSettings>()
            .Bind(configuration.GetSection("SmsSettings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<EncryptionSettings>()
            .Bind(configuration.GetSection("Encryption"))
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
        services.AddAutoMapper(typeof(Application.Mapping.UserMappingProfile).Assembly);
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IIdEncryptionService, IdEncryptionService>();
        services.AddScoped<SmtpEmailSender>();
        services.AddScoped<DevEmailSender>();
        services.AddScoped<DevSmsSender>();
        services.AddHttpClient<TwilioSmsSender>();
        services.AddScoped<TwilioSmsSender>();
        services.AddSingleton<ChannelEmailQueue>();
        services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<ChannelEmailQueue>());
        services.AddHostedService(sp => sp.GetRequiredService<ChannelEmailQueue>());
        services.AddScoped<IEmailSender>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<EmailSettings>>().Value;
            return settings.Mode.Equals("Prod", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<SmtpEmailSender>()
                : sp.GetRequiredService<DevEmailSender>();
        });
        services.AddScoped<ISmsSender>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<SmsSettings>>().Value;
            if (!settings.Mode.Equals("Prod", StringComparison.OrdinalIgnoreCase))
            {
                return sp.GetRequiredService<DevSmsSender>();
            }

            return sp.GetRequiredService<TwilioSmsSender>();
        });
        // Configure distributed cache (Redis) if connection string provided, otherwise use memory cache
        var cacheConnectionString = configuration.GetConnectionString("RedisConnection");
        if (!string.IsNullOrEmpty(cacheConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cacheConnectionString;
                var instanceName = configuration["CacheSettings:InstanceName"] ?? "TemplateApp";
                options.InstanceName = instanceName;
            });
        }
        else
        {
            // Fallback to in-memory cache if Redis not configured
            services.AddMemoryCache();
        }
        
        // Remove path-based localization so embedded resources from the
        // Infrastructure assembly are found correctly
        services.AddLocalization();
        services.AddScoped<ILocalizationService, LocalizationService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IAdminTypeService, AdminTypeService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<Application.Services_Interfaces.ISearchService, SearchService>();
        
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
        #endregion

        #region Repositories Registration
        // No need to register other Repository  as long as they dont have any other custom method that are not covered by BaseRepository
        services.AddScoped(typeof(IBaseRepository<,>), typeof(BaseRepository<,>));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<IAdminTypeRepository, AdminTypeRepository>();

        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        #endregion




        return services;
    }
}

