using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Authentication
{
    /// <summary>
    /// Request to export backup codes in specified format
    /// Accepts codes from frontend (received during generation)
    /// </summary>
    public class ExportBackupCodesRequest
    {
        /// <summary>
        /// List of backup codes to export (plain text)
        /// These are the codes returned during generation
        /// SECURITY: MaxLength prevents DOS attacks, each code must be 8-char alphanumeric
        /// </summary>
        [Required(ErrorMessage = "Codes are required")]
        [MinLength(1, ErrorMessage = "At least one code is required")]
        [MaxLength(20, ErrorMessage = "Maximum 20 codes allowed")]
        public required List<string> Codes { get; set; }

        /// <summary>
        /// Export format: "pdf", "text", or "json"
        /// </summary>
        [Required(ErrorMessage = "Format is required")]
        [RegularExpression("^(pdf|text|json)$", ErrorMessage = "Format must be 'pdf', 'text', or 'json'")]
        public required string Format { get; set; }
    }
}
