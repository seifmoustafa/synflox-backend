using Infrastructure;
using Infrastructure.Context;
using WebAPI.Configurations;
using WebAPI.Middlewares;
using Microsoft.AspNetCore.Localization;
using Infrastructure.Settings;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using System.IO;
using System.Globalization;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var logsPath = Path.Combine(builder.Environment.ContentRootPath, "logs");
Directory.CreateDirectory(logsPath);
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.WriteTo.Console()
       .WriteTo.File(
            Path.Combine(logsPath, "log-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: null,
            fileSizeLimitBytes: null));

#region Raise request limits for large uploads
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = null;
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue;
});
#endregion


// Add services to the container.
#region Service registration
// Infrastructure configuration depends on connection strings and JwtSettings in appsettings.json
builder.Services.AddInfrastructureService(builder.Configuration);
builder.Services.AddTransient<CustomClaimsPrincipalMiddleware>();
builder.Services.AddTransient<ExceptionHandlingMiddleware>();
builder.Services.AddTransient<RequestLoggingMiddleware>();
builder.Services.AddTransient<EarlyUnicodeHeaderMiddleware>();
builder.Services.AddTransient<UnicodeHeaderMiddleware>();
builder.Services.AddTransient<CacheHeadersMiddleware>();
builder.Services.AddOptions<CacheHeadersOptions>()
    .Bind(builder.Configuration.GetSection("CacheHeaders"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddTransient<ETagMiddleware>();
builder.Services.AddOptions<ETagOptions>()
    .Bind(builder.Configuration.GetSection("ETag"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
// Register both cache types for ETag middleware (it will use distributed if available)
builder.Services.AddSingleton<Microsoft.Extensions.Caching.Memory.IMemoryCache>(
    sp => new Microsoft.Extensions.Caching.Memory.MemoryCache(
        new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));
builder.Services.AddResponseCaching();
builder.Services.AddControllers();
builder.Services.AddAuthorizationPolicies();
builder.Services.AddAuthenticationRateLimiter();
#endregion

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
#region Swagger & Cors
builder.Services.AddSwaggerGenConfigurationOptions();
builder.Services.AddCorsConfigurationOptions();
#endregion

var app = builder.Build();

var defaultCulture = CultureInfo.CurrentCulture.Name;
var supportedCultures = new[] { "en", "ar" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(defaultCulture)
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

// Give priority to the Accept-Language header so the client can override
// the server's culture. If the header is missing, the system culture is used.
localizationOptions.RequestCultureProviders.Insert(0, new AcceptLanguageHeaderRequestCultureProvider());

// Configure the HTTP request pipeline.
#region Middleware pipeline
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

//Initialize Database With initial data
await DBInitializer.InitializeDatabaseAsync(app.Services);

// Serve uploaded files for all configured schemes
var fileOptions = app.Services.GetRequiredService<IOptionsMonitor<FileSettings>>();
var contentTypeProvider = new FileExtensionContentTypeProvider();
var schemes = new[] { "video", "image", "pdf", "pptx", "any" };
foreach (var scheme in schemes)
{
    var cfg = fileOptions.Get(scheme);
    var absoluteStoragePath = Path.Combine(app.Environment.ContentRootPath, cfg.StoragePath ?? string.Empty);
    Directory.CreateDirectory(absoluteStoragePath);

    app.Use(async (ctx, next) =>
    {
        if (ctx.Request.Path.StartsWithSegments(cfg.RequestPath)
            && ctx.Request.Query.ContainsKey("download"))
        {
            var name = Path.GetFileName(ctx.Request.Path);
            ctx.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{name}\"";
        }
        await next();
    });

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(absoluteStoragePath),
        RequestPath = cfg.RequestPath!,
        ContentTypeProvider = contentTypeProvider
    });
}

app.UseHttpsRedirection();
app.UseRequestLocalization(localizationOptions);
app.UseCors(CorsConfiguration._policyName);
#region Exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();
#endregion
#region Rate limiting
app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/api/authentication"), builder =>
{
    builder.UseRateLimiter();
});
#endregion

app.UseAuthentication();
app.UseMiddleware<CustomClaimsPrincipalMiddleware>();

app.UseResponseCaching();
app.UseMiddleware<ETagMiddleware>();
app.UseMiddleware<CacheHeadersMiddleware>();
app.UseMiddleware<EarlyUnicodeHeaderMiddleware>();
app.UseMiddleware<UnicodeHeaderMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseAuthorization();
#endregion

#region Endpoint mapping
app.MapControllers();
#endregion

app.Run();
