using System.Threading.Tasks;

namespace Application.Services
{
    /// <summary>
    /// Service for IP geolocation lookup
    /// </summary>
    public interface IIpGeolocationService
    {
        /// <summary>
        /// Get geographic location from IP address
        /// Returns country and city if available
        /// </summary>
        Task<(string Country, string City)> GetLocationAsync(string ipAddress);
    }
}
