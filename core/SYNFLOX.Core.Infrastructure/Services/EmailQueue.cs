using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Infrastructure.Services;

/// <summary>
/// Simple background queue for sending emails without blocking requests.
/// </summary>
public interface IEmailQueue
{
    ValueTask EnqueueAsync(string to, string subject, string body);
}

public class ChannelEmailQueue : BackgroundService, IEmailQueue
{
    private readonly Channel<(string To, string Subject, string Body)> _channel = Channel.CreateUnbounded<(string,string,string)>();
    private readonly IServiceScopeFactory _scopeFactory;

    public ChannelEmailQueue(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public ValueTask EnqueueAsync(string to, string subject, string body)
        => _channel.Writer.WriteAsync((to, subject, body));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var (to, subject, body) in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                await sender.SendEmailAsync(to, subject, body);
            }
            catch (Exception ex)
            {
                using var scope2 = _scopeFactory.CreateScope();
                var logger = scope2.ServiceProvider.GetRequiredService<ILogger<ChannelEmailQueue>>();
                logger.LogError(ex, "Email send failed for {To} - {Subject}", to, subject);
            }
        }
    }
}
