using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities.Common;
using Domain.Enums;


namespace Domain.Entities.Authentication
{
    public class User : AuditEntity<Guid>
    {
        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        public string? MiddleName { get; set; }

        public DateTime? BirthDate { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? NationalId { get; set; }

        public Gender? Gender { get; set; }

        public string? ImagePath { get; set; }

        public string? Country { get; set; }

        public string? Government { get; set; }

        public string? City { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [StringLength(100)]
        public required string Username { get; set; }

        [Required]
        [StringLength(100)]
        public required string Password { get; set; }

        public AuthProvider Providers { get; set; } = AuthProvider.None;

        /// <summary>
        /// Indicates whether the user's email has been verified.
        /// </summary>
        public bool IsEmailVerified { get; set; } = false;

        /// <summary>
        /// Indicates whether the user's phone number has been verified.
        /// </summary>
        public bool IsPhoneVerified { get; set; } = false;

        /// <summary>
        /// Legacy flag kept for backwards compatibility. Represents account verification.
        /// </summary>
        public bool IsVerified { get; set; } = false;

        public DateTime? LastLogin { get; set; }
    }
}
