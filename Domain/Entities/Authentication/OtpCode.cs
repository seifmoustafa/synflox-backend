using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;
using Domain.Enums;

namespace Domain.Entities.Authentication
{
    /// <summary>
    /// Stores verification and password reset codes for a user.
    /// </summary>
    public class OtpCode : AuditEntity<int>
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty;

        public OtpPurpose Purpose { get; set; }

        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; } = false;

        public virtual User User { get; set; } = null!;
    }
}
