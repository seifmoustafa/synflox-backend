using System;
using Application.Services;
using UAParser;

namespace Infrastructure.Services
{
    /// <summary>
    /// User-Agent parser service using UAParser library
    /// Extracts device type and browser information from UA strings
    /// </summary>
    public class UserAgentParserService : IUserAgentParserService
    {
        private readonly Parser _parser;

        public UserAgentParserService()
        {
            _parser = Parser.GetDefault();
        }

        public (string DeviceType, string Browser) Parse(string userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                return ("Unknown", "Unknown");
            }

            try
            {
                var clientInfo = _parser.Parse(userAgent);

                // Determine device type
                var deviceType = DetermineDeviceType(clientInfo);

                // Get browser name
                var browser = clientInfo.UA.Family ?? "Unknown Browser";
                if (!string.IsNullOrEmpty(clientInfo.UA.Major))
                {
                    browser += $" {clientInfo.UA.Major}";
                }

                return (deviceType, browser);
            }
            catch (Exception)
            {
                return ("Unknown", "Unknown");
            }
        }

        private string DetermineDeviceType(ClientInfo clientInfo)
        {
            var device = clientInfo.Device.Family?.ToLower() ?? "";
            var os = clientInfo.OS.Family?.ToLower() ?? "";

            // Check for mobile devices
            if (device.Contains("iphone") || device.Contains("ipad") || 
                device.Contains("ipod") || os.Contains("ios"))
            {
                return device.Contains("ipad") ? "Tablet" : "Mobile";
            }

            if (device.Contains("android"))
            {
                return device.Contains("tablet") ? "Tablet" : "Mobile";
            }

            if (os.Contains("android"))
            {
                return "Mobile";
            }

            // Check for tablets
            if (device.Contains("tablet") || os.Contains("tablet"))
            {
                return "Tablet";
            }

            // Check for mobile indicators
            if (device.Contains("mobile") || os.Contains("mobile") ||
                device.Contains("phone") || os.Contains("windows phone"))
            {
                return "Mobile";
            }

            // Default to desktop
            return "Desktop";
        }
    }
}
