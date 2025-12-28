using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Domain.Constants;

namespace Infrastructure.Authentication
{
    public class CustomClaimsPrincipal : ClaimsPrincipal
    {
        public CustomClaimsPrincipal(ClaimsPrincipal principal)
            : base(principal) { }

        #region Admin (SYNFLOX) Claims

        public Guid UserId
        {
            get
            {
                return Guid.TryParse(FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                    ? id
                    : Guid.Empty;
            }
        }
        public string Username => FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        public string? FirstName => FindFirst(JwtClaimTypes.FirstName)?.Value;
        public string? MiddleName => FindFirst(JwtClaimTypes.MiddleName)?.Value;
        public string? LastName => FindFirst(JwtClaimTypes.LastName)?.Value;
        public DateTime? BirthDate =>
            DateTime.TryParse(FindFirst(JwtClaimTypes.BirthDate)?.Value, out var d) ? d : null;
        public string? Email => FindFirst(JwtClaimTypes.Email)?.Value;
        public string? PhoneNumber => FindFirst(JwtClaimTypes.PhoneNumber)?.Value;
        public string? NationalId => FindFirst(JwtClaimTypes.NationalId)?.Value;
        public string? Gender => FindFirst(JwtClaimTypes.Gender)?.Value;
        public string? ImagePath => FindFirst(JwtClaimTypes.ImagePath)?.Value;
        public string? Country => FindFirst(JwtClaimTypes.Country)?.Value;
        public string? Government => FindFirst(JwtClaimTypes.Government)?.Value;
        public string? City => FindFirst(JwtClaimTypes.City)?.Value;
        public string? AdminTypeName => FindFirst(JwtClaimTypes.AdminTypeName)?.Value;

        public bool? IsEmailVerified
        {
            get
            {
                var claim = FindFirst(JwtClaimTypes.IsEmailVerified)?.Value;
                return claim != null ? bool.Parse(claim) : (bool?)null;
            }
        }

        public bool? IsPhoneVerified
        {
            get
            {
                var claim = FindFirst(JwtClaimTypes.IsPhoneVerified)?.Value;
                return claim != null ? bool.Parse(claim) : (bool?)null;
            }
        }

        public bool? IsVerified
        {
            get
            {
                var isVerifiedClaim = FindFirst(JwtClaimTypes.IsVerified)?.Value;
                return isVerifiedClaim != null ? bool.Parse(isVerifiedClaim) : (bool?)null;
            }
        }

        public bool? IsActive
        {
            get
            {
                var claim = FindFirst(JwtClaimTypes.IsActive)?.Value;
                return claim != null ? bool.Parse(claim) : (bool?)null;
            }
        }

        #endregion

        #region CompanyAdmin (Client Portal) Claims

        public Guid CompanyAdminId
        {
            get
            {
                return Guid.TryParse(FindFirst(JwtClaimTypes.CompanyAdminId)?.Value, out var id)
                    ? id
                    : Guid.Empty;
            }
        }

        public Guid CompanyId
        {
            get
            {
                return Guid.TryParse(FindFirst(JwtClaimTypes.CompanyId)?.Value, out var id)
                    ? id
                    : Guid.Empty;
            }
        }

        public string? DisplayName => FindFirst(JwtClaimTypes.DisplayName)?.Value;

        /// <summary>
        /// Returns true if this is a CompanyAdmin token (Client Portal).
        /// Determined by role claim = "CompanyAdmin" or presence of CompanyAdminId claim.
        /// </summary>
        public bool IsCompanyAdmin
        {
            get
            {
                var role = FindFirst(ClaimTypes.Role)?.Value;
                return role == "CompanyAdmin" || CompanyAdminId != Guid.Empty;
            }
        }

        #endregion
    }
}
