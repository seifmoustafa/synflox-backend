using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    /// <summary>
    /// IP Geolocation service using ip-api.com (free, no key required)
    /// Falls back to "Unknown" if service is unavailable
    /// </summary>
    public class IpGeolocationService : IIpGeolocationService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<IpGeolocationService> _logger;
        private const string API_URL = "http://ip-api.com/json/{0}?fields=country,city,status,message";
        private const int CACHE_MINUTES = 60;

        public IpGeolocationService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            ILogger<IpGeolocationService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _cache = cache;
            _logger = logger;
        }

        public async Task<(string Country, string City)> GetLocationAsync(string ipAddress)
        {
            // Handle local/private IPs
            if (string.IsNullOrWhiteSpace(ipAddress) || 
                IsPrivateIp(ipAddress) || 
                ipAddress == "::1" || 
                ipAddress == "127.0.0.1")
            {
                return ("Local", "Local");
            }

            // Check cache first
            var cacheKey = $"geo_{ipAddress}";
            if (_cache.TryGetValue<(string, string)>(cacheKey, out var cachedLocation))
            {
                return cachedLocation;
            }

            try
            {
                // Call free geolocation API
                var url = string.Format(API_URL, ipAddress);
                var response = await _httpClient.GetStringAsync(url);
                var data = JsonSerializer.Deserialize<GeoLocationResponse>(response);

                if (data?.Status == "success")
                {
                    var result = (data.Country ?? "Unknown", data.City ?? "Unknown");
                    
                    // Cache for 1 hour
                    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CACHE_MINUTES));
                    
                    return result;
                }
                else
                {
                    _logger.LogWarning("IP geolocation failed for {IpAddress}: {Message}", 
                        ipAddress, data?.Message);
                    return ("Unknown", "Unknown");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting geolocation for IP {IpAddress}", ipAddress);
                return ("Unknown", "Unknown");
            }
        }

        private bool IsPrivateIp(string ipAddress)
        {
            if (!System.Net.IPAddress.TryParse(ipAddress, out var ip))
                return false;

            var bytes = ip.GetAddressBytes();
            
            // Check for private IP ranges
            return bytes[0] == 10 ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168);
        }

        private class GeoLocationResponse
        {
            public string? Status { get; set; }
            public string? Country { get; set; }
            public string? City { get; set; }
            public string? Message { get; set; }
        }
    }
}
