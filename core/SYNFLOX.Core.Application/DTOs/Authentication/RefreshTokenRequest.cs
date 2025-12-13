using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Authentication
{
    /// <summary>
    /// Request model for refreshing access token using refresh token.
    /// </summary>
    public class RefreshTokenRequest
    {
        /// <summary>
        /// The refresh token received during login or previous refresh.
        /// </summary>
        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; }
    }
}
