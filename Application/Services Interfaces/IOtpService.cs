using System;
using System.Threading.Tasks;
using Domain.Entities.Authentication;
using Domain.Enums;
using Application.DTOs.Authentication;

namespace Application.Services
{
    public interface IOtpService
    {
        Task<OtpSendResult> SendOtpAsync(User user, OtpPurpose purpose);
        Task<bool> ValidateOtpAsync(User user, string code, OtpPurpose purpose);
    }
}
