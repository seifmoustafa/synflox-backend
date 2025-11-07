using System.Threading.Tasks;
using Application.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class DevSmsSender : ISmsSender
    {
        private readonly ILogger<DevSmsSender> _logger;
        public DevSmsSender(ILogger<DevSmsSender> logger)
        {
            _logger = logger;
        }

        public Task SendSmsAsync(string number, string message)
        {
            _logger.LogInformation("SMS to {Number}: {Message}", number, message);
            return Task.CompletedTask;
        }
    }
}
