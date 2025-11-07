using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Application.Services;
using Application.DTOs.Authentication;
using Domain.Entities.Authentication;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Configurations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    /// <summary>
    /// Manages OTP generation and validation.
    /// </summary>
    public class OtpService : IOtpService
    {
        private readonly IOtpRepository _repository;
        private readonly IEmailQueue _emailQueue;
        private readonly ISmsSender _smsSender;
        private readonly IMemoryCache _cache;
        private readonly VerificationOptions _options;
        private readonly ILocalizationService _localizer;
        private readonly ILogger<OtpService> _logger;
        private readonly IUnitOfWork _unitOfWork;

        private class ThrottleEntry
        {
            public int Count { get; set; }
            public DateTime WindowEnd { get; set; }
        }

        public OtpService(IOtpRepository repository, IEmailQueue emailQueue,
            IMemoryCache cache, IOptions<VerificationOptions> options,
            ILocalizationService localizer, ISmsSender smsSender,
            ILogger<OtpService> logger, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _emailQueue = emailQueue;
            _cache = cache;
            _options = options.Value;
            _localizer = localizer;
            _smsSender = smsSender;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<OtpSendResult> SendOtpAsync(User user, OtpPurpose purpose)
        {
            string key = $"otp:{purpose}:{user.Id}";
            if (!_cache.TryGetValue(key, out ThrottleEntry throttle) || throttle.WindowEnd < DateTime.UtcNow)
            {
                throttle = new ThrottleEntry
                {
                    Count = 0,
                    WindowEnd = DateTime.UtcNow.AddSeconds(_options.ResendWindowSeconds)
                };
            }

            if (throttle.Count >= _options.MaxResendsPerWindow)
            {
                int retry = (int)(throttle.WindowEnd - DateTime.UtcNow).TotalSeconds;
                return new OtpSendResult
                {
                    Sent = false,
                    RetryAfterSeconds = retry,
                    Message = string.Format(_localizer["OtpThrottled"], retry)
                };
            }

            throttle.Count++;
            _cache.Set(key, throttle, throttle.WindowEnd);

            byte[] buffer = RandomNumberGenerator.GetBytes(4);
            int numeric = BitConverter.ToInt32(buffer, 0) & 0x7FFFFFFF;
            string code = purpose == OtpPurpose.PhoneVerification
                ? (numeric % 10_000).ToString("D4")
                : (numeric % 1_000_000).ToString("D6");

            var entry = new OtpCode
            {
                UserId = user.Id,
                Code = code,
                Purpose = purpose,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_options.CodeExpiryMinutes)
            };
            await _repository.AddAsync(entry);
            await _unitOfWork.SaveChangesAsync();
            if (!string.IsNullOrEmpty(user.Email))
            {
                await _emailQueue.EnqueueAsync(
                    user.Email,
                    _localizer["OtpSubject"],
                    string.Format(_localizer["OtpBody"], code));
            }
            if (purpose == OtpPurpose.PhoneVerification)
            {
                if (string.IsNullOrEmpty(user.PhoneNumber))
                {
                    return new OtpSendResult
                    {
                        Sent = false,
                        Message = _localizer["PhoneNumberMissing"]
                    };
                }
                try
                {
                    await _smsSender.SendSmsAsync(user.PhoneNumber!,
                        string.Format(_localizer["OtpBody"], code));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SMS sending failed");
                    return new OtpSendResult
                    {
                        Sent = false,
                        Message = _localizer["SmsSendFailed"]
                    };
                }
            }

            return new OtpSendResult { Sent = true };
        }

        public async Task<bool> ValidateOtpAsync(User user, string code, OtpPurpose purpose)
        {
            var entry = await _repository.GetValidOtpAsync(user.Id, code, purpose);
            if (entry == null) return false;
            entry.IsUsed = true;
            await _repository.UpdateAsync(entry);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
