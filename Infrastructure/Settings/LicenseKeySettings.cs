using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Settings
{
    /// <summary>
    /// Settings for license key generation and encryption.
    /// </summary>
    public class LicenseKeySettings
    {
        /// <summary>
        /// AES encryption key (32 bytes for AES-256, base64 encoded).
        /// </summary>
        [Required]
        [StringLength(44)] // Base64 encoded 32 bytes = 44 characters
        public string EncryptionKey { get; set; } = string.Empty;

        /// <summary>
        /// AES initialization vector (16 bytes for AES, base64 encoded).
        /// </summary>
        [Required]
        [StringLength(24)] // Base64 encoded 16 bytes = 24 characters
        public string IV { get; set; } = string.Empty;

        /// <summary>
        /// HMAC signing key for license key signature (32 bytes, base64 encoded).
        /// </summary>
        [Required]
        [StringLength(44)] // Base64 encoded 32 bytes = 44 characters
        public string SigningKey { get; set; } = string.Empty;

        /// <summary>
        /// License key version for future compatibility.
        /// </summary>
        public int Version { get; set; } = 1;
    }
}

