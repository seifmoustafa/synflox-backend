using System.Collections.Generic;

namespace Application.DTOs.Authentication
{
    /// <summary>
    /// Response after generating backup codes
    /// Codes are shown ONLY once and should be saved by user
    /// </summary>
    public class GenerateBackupCodesResponse
    {
        /// <summary>
        /// List of plain text backup codes (10 codes)
        /// NEVER returned after initial generation
        /// </summary>
        public List<string> Codes { get; set; } = new();

        /// <summary>
        /// Success message
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
