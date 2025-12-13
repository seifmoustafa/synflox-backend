using System;

namespace Application.DTOs.Authentication
{
    /// <summary>
    /// Status of backup codes for an admin
    /// </summary>
    public class BackupCodesStatusDto
    {
        /// <summary>
        /// Number of unused and non-expired backup codes remaining
        /// </summary>
        public int RemainingCodes { get; set; }

        /// <summary>
        /// Total number of backup codes generated
        /// </summary>
        public int TotalCodes { get; set; }

        /// <summary>
        /// Number of expired backup codes
        /// </summary>
        public int ExpiredCodes { get; set; }

        /// <summary>
        /// Earliest expiry date among unused codes (null if no codes)
        /// </summary>
        public DateTime? NextExpiryDate { get; set; }

        /// <summary>
        /// Days until next code expires (null if no codes)
        /// </summary>
        public int? DaysUntilExpiry { get; set; }

        /// <summary>
        /// Whether user has backup codes
        /// </summary>
        public bool HasBackupCodes => TotalCodes > 0;

        /// <summary>
        /// Whether user should be warned about low codes (< 3 remaining)
        /// </summary>
        public bool LowCodesWarning => RemainingCodes > 0 && RemainingCodes < 3;

        /// <summary>
        /// Whether codes are expiring soon (< 30 days until expiry)
        /// </summary>
        public bool ExpiryWarning => DaysUntilExpiry.HasValue && DaysUntilExpiry.Value < 30;

        /// <summary>
        /// Whether user needs to regenerate codes (all codes used/expired or no codes exist)
        /// </summary>
        public bool NeedsRegeneration => RemainingCodes == 0;
    }
}
