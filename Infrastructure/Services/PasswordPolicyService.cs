using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Application.DTOs.Settings;
using Application.Services;
using AutoMapper;
using BCrypt.Net;
using Domain.Entities.Authentication;
using Domain.Entities.Settings;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Infrastructure.Services;

public class PasswordPolicyService : IPasswordPolicyService
{
    private readonly IPasswordPolicyRepository _policyRepository;
    private readonly IPasswordHistoryRepository _historyRepository;
    private readonly IAdminRepository _adminRepository;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;

    public PasswordPolicyService(
        IPasswordPolicyRepository policyRepository,
        IPasswordHistoryRepository historyRepository,
        IAdminRepository adminRepository,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork)
    {
        _policyRepository = policyRepository;
        _historyRepository = historyRepository;
        _adminRepository = adminRepository;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
    }

    public async Task<PasswordPolicyDto?> GetActivePolicyAsync()
    {
        var policies = await _policyRepository.FindAsync(p => p.IsActive && !p.IsDeleted);
        var activePolicy = policies.FirstOrDefault();
        
        if (activePolicy == null)
        {
            // Return default policy if none exists
            return new PasswordPolicyDto
            {
                Id = Guid.Empty,
                MinLength = 8,
                RequireUppercase = true,
                RequireLowercase = true,
                RequireNumbers = true,
                RequireSpecialChars = true,
                MaxAgeDays = 90,
                PreventReuseCount = 5,
                IsActive = true
            };
        }

        return _mapper.Map<PasswordPolicyDto>(activePolicy);
    }

    public async Task<PasswordValidationResult> ValidatePasswordAsync(string password, Guid? adminId = null)
    {
        var result = new PasswordValidationResult { IsValid = true };
        var policy = await GetActivePolicyAsync();

        if (policy == null)
        {
            return result; // No policy, allow any password
        }

        // Check minimum length
        if (password.Length < policy.MinLength)
        {
            result.IsValid = false;
            result.Errors.Add(string.Format(_localizer["PasswordPolicy.MinLength"], policy.MinLength));
        }

        // Check uppercase
        if (policy.RequireUppercase && !password.Any(char.IsUpper))
        {
            result.IsValid = false;
            result.Errors.Add(_localizer["PasswordPolicy.RequireUppercase"]);
        }

        // Check lowercase
        if (policy.RequireLowercase && !password.Any(char.IsLower))
        {
            result.IsValid = false;
            result.Errors.Add(_localizer["PasswordPolicy.RequireLowercase"]);
        }

        // Check numbers
        if (policy.RequireNumbers && !password.Any(char.IsDigit))
        {
            result.IsValid = false;
            result.Errors.Add(_localizer["PasswordPolicy.RequireNumbers"]);
        }

        // Check special characters
        if (policy.RequireSpecialChars)
        {
            var specialCharPattern = new Regex(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]");
            if (!specialCharPattern.IsMatch(password))
            {
                result.IsValid = false;
                result.Errors.Add(_localizer["PasswordPolicy.RequireSpecialChars"]);
            }
        }

        // Check password reuse if adminId is provided
        if (adminId.HasValue && policy.PreventReuseCount.HasValue)
        {
            var admin = await _adminRepository.GetByIdAsync(adminId.Value, null);
            if (admin != null)
            {
                var recentPasswords = await _historyRepository.FindAsync(ph => 
                    ph.AdminId == adminId.Value && !ph.IsDeleted);
                
                var recentHashes = recentPasswords
                    .OrderByDescending(ph => ph.SetAt)
                    .Take(policy.PreventReuseCount.Value)
                    .Select(ph => ph.PasswordHash)
                    .ToList();

                // Hash the new password to check against history
                var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                
                // Check if any recent password matches (BCrypt.Verify would be needed, but we store hashes)
                // For now, we'll check if the hash matches exactly (BCrypt hashes are unique each time)
                // Better approach: verify against each hash in history
                foreach (var oldHash in recentHashes)
                {
                    try
                    {
                        if (BCrypt.Net.BCrypt.Verify(password, oldHash))
                        {
                            result.IsValid = false;
                            result.Errors.Add(string.Format(_localizer["PasswordPolicy.PreventReuse"], policy.PreventReuseCount.Value));
                            break;
                        }
                    }
                    catch
                    {
                        // If verification fails, continue
                    }
                }
            }
        }

        return result;
    }

    public async Task<PasswordPolicyDto> UpdatePolicyAsync(UpdatePasswordPolicyRequest request)
    {
        var policies = await _policyRepository.FindAsync(p => !p.IsDeleted);
        var existingPolicy = policies.FirstOrDefault(p => p.IsActive);

        PasswordPolicy policy;
        if (existingPolicy != null)
        {
            // Update existing active policy
            policy = existingPolicy;
            _mapper.Map(request, policy);
        }
        else
        {
            // Create new policy
            policy = _mapper.Map<PasswordPolicy>(request);
            policy.IsActive = true;
            await _policyRepository.AddAsync(policy);
        }

        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<PasswordPolicyDto>(policy);
    }

    public async Task<bool> IsPasswordExpiredAsync(Guid adminId)
    {
        var policy = await GetActivePolicyAsync();
        if (policy == null || !policy.MaxAgeDays.HasValue)
        {
            return false; // No expiration policy
        }

        var admin = await _adminRepository.GetByIdAsync(adminId, null);
        if (admin == null)
        {
            return false;
        }

        // Check password history for last change date
        var passwordHistory = await _historyRepository.FindAsync(ph => 
            ph.AdminId == adminId && !ph.IsDeleted);
        
        var lastChange = passwordHistory
            .OrderByDescending(ph => ph.SetAt)
            .FirstOrDefault();

        if (lastChange == null)
        {
            // No password history, check CreatedTimestamp as fallback
            var daysSinceCreation = (DateTime.UtcNow - admin.CreatedTimestamp).TotalDays;
            return daysSinceCreation > policy.MaxAgeDays.Value;
        }

        var daysSinceChange = (DateTime.UtcNow - lastChange.SetAt).TotalDays;
        return daysSinceChange > policy.MaxAgeDays.Value;
    }

    public async Task RecordPasswordChangeAsync(Guid adminId, string passwordHash)
    {
        var historyEntry = new PasswordHistory
        {
            AdminId = adminId,
            PasswordHash = passwordHash,
            SetAt = DateTime.UtcNow
        };

        await _historyRepository.AddAsync(historyEntry);
        await _unitOfWork.SaveChangesAsync();

        // Clean up old password history (keep only last N entries based on policy)
        var policy = await GetActivePolicyAsync();
        if (policy?.PreventReuseCount.HasValue == true)
        {
            var allHistory = await _historyRepository.FindAsync(ph => 
                ph.AdminId == adminId && !ph.IsDeleted);
            
            var toKeep = allHistory
                .OrderByDescending(ph => ph.SetAt)
                .Take(policy.PreventReuseCount.Value + 1) // Keep one extra for safety
                .Select(ph => ph.Id)
                .ToList();

            var toDelete = allHistory
                .Where(ph => !toKeep.Contains(ph.Id))
                .ToList();

            foreach (var entry in toDelete)
            {
                await _historyRepository.DeleteAsync(entry.Id);
            }
        }
    }
}

