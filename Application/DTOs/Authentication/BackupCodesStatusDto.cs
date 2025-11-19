namespace Application.DTOs.Authentication
{
    /// <summary>
    /// Status of backup codes for an admin
    /// </summary>
    public class BackupCodesStatusDto
    {
        /// <summary>
        /// Number of unused backup codes remaining
        /// </summary>
        public int RemainingCodes { get; set; }

        /// <summary>
        /// Total number of backup codes generated
        /// </summary>
        public int TotalCodes { get; set; }

        /// <summary>
        /// Whether user has backup codes
        /// </summary>
        public bool HasBackupCodes => TotalCodes > 0;

        /// <summary>
        /// Whether user should be warned about low codes (< 3 remaining)
        /// </summary>
        public bool LowCodesWarning => RemainingCodes > 0 && RemainingCodes < 3;

        /// <summary>
        /// Whether user needs to regenerate codes (all codes used or no codes exist)
        /// </summary>
        public bool NeedsRegeneration => RemainingCodes == 0;
    }
}
