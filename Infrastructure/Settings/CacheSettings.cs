using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Settings
{
    public class CacheSettings
    {
        [Required]
        public string ConnectionString { get; set; } = string.Empty;
        
        public string? InstanceName { get; set; }
        
        public int DefaultSlidingExpirationMinutes { get; set; } = 30;
        
        public int DefaultAbsoluteExpirationMinutes { get; set; } = 60;
    }
}

