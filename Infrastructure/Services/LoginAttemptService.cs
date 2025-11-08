using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Authentication;
using Application.Services;
using AutoMapper;
using Domain.Entities.Authentication;
using Domain.Entities.Common;
using Domain.Interfaces;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class LoginAttemptService : ILoginAttemptService
{
    private readonly ILoginAttemptRepository _repository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LoginSecuritySettings _settings;

    public LoginAttemptService(
        ILoginAttemptRepository repository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IOptions<LoginSecuritySettings> settings)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
    }

    public async Task RecordAttemptAsync(
        string username,
        bool success,
        string? ipAddress = null,
        string? failureReason = null,
        Guid? adminId = null)
    {
        var attempt = new LoginAttempt
        {
            Id = Guid.NewGuid(),
            Username = username,
            IpAddress = ipAddress,
            Success = success,
            FailureReason = failureReason,
            AttemptedAt = DateTime.UtcNow,
            AdminId = adminId,
            IsActive = true,
            IsDeleted = false
        };

        await _repository.AddAsync(attempt);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<LoginAttemptDto>> GetRecentFailedAttemptsAsync(
        string username,
        int minutes = 30)
    {
        var since = DateTime.UtcNow.AddMinutes(-minutes);
        var attempts = await _repository.GetRecentFailedAttemptsAsync(username, since);
        return _mapper.Map<IEnumerable<LoginAttemptDto>>(attempts);
    }

    public async Task<bool> IsAccountLockedAsync(string username, string? ipAddress = null)
    {
        var since = DateTime.UtcNow.AddMinutes(-_settings.FailedAttemptWindowMinutes);
        
        // Check by username
        var failedCount = await _repository.CountFailedAttemptsAsync(username, since);
        if (failedCount >= _settings.MaxFailedAttempts)
        {
            // Check if lockout period has passed
            var recentAttempts = await _repository.GetRecentFailedAttemptsAsync(username, since, 1);
            var lastAttempt = recentAttempts.FirstOrDefault();
            if (lastAttempt != null)
            {
                var lockoutEnd = lastAttempt.AttemptedAt.AddMinutes(_settings.LockoutDurationMinutes);
                if (DateTime.UtcNow < lockoutEnd)
                {
                    return true; // Still locked
                }
            }
        }

        // Also check by IP if provided
        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            var failedCountByIp = await _repository.CountFailedAttemptsByIpAsync(ipAddress, since);
            if (failedCountByIp >= _settings.MaxFailedAttempts)
            {
                var recentAttemptsByIp = await _repository.GetRecentFailedAttemptsByIpAsync(ipAddress, since, 1);
                var lastAttemptByIp = recentAttemptsByIp.FirstOrDefault();
                if (lastAttemptByIp != null)
                {
                    var lockoutEnd = lastAttemptByIp.AttemptedAt.AddMinutes(_settings.LockoutDurationMinutes);
                    if (DateTime.UtcNow < lockoutEnd)
                    {
                        return true; // IP is locked
                    }
                }
            }
        }

        return false;
    }

    public async Task<(IEnumerable<LoginAttemptDto> Attempts, PaginationMetadata Meta)> GetAllAttemptsAsync(
        string? username = null,
        bool? success = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 10)
    {
        // Use base repository GetAllAsync with filtering
        // For now, get all and filter in memory (can be optimized later)
        var (entities, meta) = await _repository.GetAllAsync(null, page, pageSize, null, default);
        var filtered = entities.AsQueryable();

        if (!string.IsNullOrWhiteSpace(username))
        {
            filtered = filtered.Where(a => a.Username.Contains(username, StringComparison.OrdinalIgnoreCase));
        }

        if (success.HasValue)
        {
            filtered = filtered.Where(a => a.Success == success.Value);
        }

        if (fromDate.HasValue)
        {
            filtered = filtered.Where(a => a.AttemptedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            filtered = filtered.Where(a => a.AttemptedAt <= toDate.Value);
        }

        var attempts = filtered.OrderByDescending(a => a.AttemptedAt).ToList();
        var dtos = _mapper.Map<IEnumerable<LoginAttemptDto>>(attempts);

        return (dtos, meta);
    }
}

