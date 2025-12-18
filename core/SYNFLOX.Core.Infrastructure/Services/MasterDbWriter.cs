using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Services;
using Domain.Entities.Licensing;
using Domain.Entities.OnlineAccess;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Writes to Master DB for client-api operations.
/// This ensures client writes are visible to admin immediately.
/// Falls back to no-op if MasterDbContext is not registered (admin-api).
/// </summary>
public class MasterDbWriter : IMasterDbWriter
{
    private readonly MasterDbContext? _masterContext;
    private readonly ILogger<MasterDbWriter> _logger;

    public MasterDbWriter(
        IServiceProvider serviceProvider,
        ILogger<MasterDbWriter> logger)
    {
        _logger = logger;
        // Try to resolve MasterDbContext - will be null if not registered (admin-api)
        _masterContext = serviceProvider.GetService<MasterDbContext>();
        
        if (_masterContext != null)
        {
            _logger.LogInformation("MasterDbWriter initialized with Master DB connection");
        }
        else
        {
            _logger.LogDebug("MasterDbWriter initialized without Master DB connection (this is normal for admin-api)");
        }
    }

    public bool IsAvailable => _masterContext != null;

    public async Task<LicenseActivation> WriteActivationAsync(LicenseActivation activation, CancellationToken cancellationToken = default)
    {
        if (_masterContext == null)
        {
            _logger.LogDebug("MasterDbContext not available, skipping Master DB write for activation");
            return activation;
        }

        try
        {
            // Use raw SQL to bypass EF tracking issues between contexts
            var sql = @"
                IF NOT EXISTS (SELECT 1 FROM LicenseActivations WHERE Id = @p0)
                BEGIN
                    INSERT INTO LicenseActivations 
                    (Id, SubscriptionId, CompanyId, MachineHash, DeviceName, OperatingSystem, 
                     MacAddress, MotherboardSerial, CpuId, DiskSerial, 
                     ActivatedAtUtc, LastSeenAtUtc, IsActive, IsDeleted, ValidationCount,
                     HardwareChangeCount, CreatedTimestamp)
                    VALUES 
                    (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14, @p15, @p16)
                END";

            var rowsAffected = await _masterContext.Database.ExecuteSqlRawAsync(sql,
                activation.Id,
                activation.SubscriptionId,
                activation.CompanyId,
                activation.MachineHash ?? "",
                activation.DeviceName ?? "Unknown",
                activation.OperatingSystem ?? "",
                activation.MacAddress ?? "",
                activation.MotherboardSerial ?? "",
                activation.CpuId ?? "",
                activation.DiskSerial ?? "",
                activation.ActivatedAtUtc,
                activation.LastSeenAtUtc,
                activation.IsActive ? 1 : 0,
                activation.IsDeleted ? 1 : 0,
                activation.ValidationCount,
                activation.HardwareChangeCount,
                DateTime.UtcNow);

            if (rowsAffected > 0)
            {
                _logger.LogInformation("Wrote license activation {ActivationId} to Master DB via SQL", activation.Id);
            }
            else
            {
                _logger.LogDebug("Activation {ActivationId} already exists in Master DB", activation.Id);
            }
            
            return activation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write activation to Master DB");
            // Don't throw - silently fail to not break the main operation
            return activation;
        }
    }

    public async Task UpdateActivationAsync(LicenseActivation activation, CancellationToken cancellationToken = default)
    {
        if (_masterContext == null)
        {
            _logger.LogDebug("MasterDbContext not available, skipping Master DB update for activation");
            return;
        }

        try
        {
            _masterContext.LicenseActivations.Update(activation);
            await _masterContext.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Updated license activation {ActivationId} in Master DB", activation.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update activation in Master DB");
            throw;
        }
    }

    public async Task DeactivateDeviceAsync(Guid activationId, string reason, CancellationToken cancellationToken = default)
    {
        if (_masterContext == null)
        {
            _logger.LogDebug("MasterDbContext not available, skipping Master DB deactivation");
            return;
        }

        try
        {
            var activation = await _masterContext.LicenseActivations
                .FirstOrDefaultAsync(a => a.Id == activationId, cancellationToken);
            
            if (activation != null)
            {
                activation.IsActive = false;
                activation.DeactivatedAtUtc = DateTime.UtcNow;
                activation.DeactivationReason = reason;
                await _masterContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Deactivated device {ActivationId} in Master DB", activationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate device in Master DB");
            throw;
        }
    }

    public async Task<DeviceReplacementRequest> WriteReplacementRequestAsync(DeviceReplacementRequest request, CancellationToken cancellationToken = default)
    {
        if (_masterContext == null)
        {
            _logger.LogDebug("MasterDbContext not available, skipping Master DB write for replacement request");
            return request;
        }

        try
        {
            _masterContext.DeviceReplacementRequests.Add(request);
            await _masterContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Wrote replacement request {RequestId} to Master DB", request.Id);
            return request;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write replacement request to Master DB");
            throw;
        }
    }

    public async Task UpdateReplacementRequestAsync(DeviceReplacementRequest request, CancellationToken cancellationToken = default)
    {
        if (_masterContext == null)
        {
            _logger.LogDebug("MasterDbContext not available, skipping Master DB update for replacement request");
            return;
        }

        try
        {
            _masterContext.DeviceReplacementRequests.Update(request);
            await _masterContext.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Updated replacement request {RequestId} in Master DB", request.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update replacement request in Master DB");
            throw;
        }
    }

    public async Task<OnlineClientToken> WriteOnlineTokenAsync(OnlineClientToken token, CancellationToken cancellationToken = default)
    {
        if (_masterContext == null)
        {
            _logger.LogDebug("MasterDbContext not available, skipping Master DB write for online token");
            return token;
        }

        try
        {
            _masterContext.OnlineClientTokens.Add(token);
            await _masterContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Wrote online token {TokenId} to Master DB", token.Id);
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write online token to Master DB");
            throw;
        }
    }

    public async Task UpdateOnlineTokenAsync(OnlineClientToken token, CancellationToken cancellationToken = default)
    {
        if (_masterContext == null)
        {
            _logger.LogDebug("MasterDbContext not available, skipping Master DB update for online token");
            return;
        }

        try
        {
            _masterContext.OnlineClientTokens.Update(token);
            await _masterContext.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Updated online token {TokenId} in Master DB", token.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update online token in Master DB");
            throw;
        }
    }

    public async Task<OnlineDeviceBinding> WriteOnlineDeviceAsync(OnlineDeviceBinding device, CancellationToken cancellationToken = default)
    {
        if (_masterContext == null)
        {
            _logger.LogDebug("MasterDbContext not available, skipping Master DB write for online device");
            return device;
        }

        try
        {
            _masterContext.OnlineDeviceBindings.Add(device);
            await _masterContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Wrote online device binding {DeviceId} to Master DB", device.Id);
            return device;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write online device binding to Master DB");
            throw;
        }
    }

    public async Task UpdateOnlineDeviceAsync(OnlineDeviceBinding device, CancellationToken cancellationToken = default)
    {
        if (_masterContext == null)
        {
            _logger.LogDebug("MasterDbContext not available, skipping Master DB update for online device");
            return;
        }

        try
        {
            _masterContext.OnlineDeviceBindings.Update(device);
            await _masterContext.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Updated online device binding {DeviceId} in Master DB", device.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update online device binding in Master DB");
            throw;
        }
    }
}
