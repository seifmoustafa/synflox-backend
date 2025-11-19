namespace Application.Services
{
    /// <summary>
    /// Service for parsing User-Agent strings
    /// </summary>
    public interface IUserAgentParserService
    {
        /// <summary>
        /// Parse user agent string into device type and browser name
        /// </summary>
        (string DeviceType, string Browser) Parse(string userAgent);
    }
}
