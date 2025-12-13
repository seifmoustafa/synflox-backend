using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Services;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {

        private readonly IHttpContextAccessor _httpContextAccessor;
        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }



        private CustomClaimsPrincipal Principal => new CustomClaimsPrincipal(_httpContextAccessor.HttpContext?.User ?? new System.Security.Claims.ClaimsPrincipal());

        public Guid UserId => Principal.UserId;
        public string Username => Principal.Username;
        public string? FirstName => Principal.FirstName;
        public string? MiddleName => Principal.MiddleName;
        public string? LastName => Principal.LastName;
        public DateTime? BirthDate => Principal.BirthDate;
        public string? Email => Principal.Email;
        public string? PhoneNumber => Principal.PhoneNumber;
        public string? NationalId => Principal.NationalId;
        public string? Gender => Principal.Gender;
        public string? ImagePath => Principal.ImagePath;
        public string? Country => Principal.Country;
        public string? Government => Principal.Government;
        public string? City => Principal.City;
        public string? AdminTypeName => Principal.AdminTypeName;
        public bool? IsEmailVerified => Principal.IsEmailVerified;
        public bool? IsPhoneVerified => Principal.IsPhoneVerified;
        public bool? IsVerified => Principal.IsVerified;
        public bool? IsActive => Principal.IsActive;
    }
}
