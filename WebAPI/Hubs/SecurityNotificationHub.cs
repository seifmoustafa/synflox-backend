using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace WebAPI.Hubs
{
    /// <summary>
    /// SignalR hub for real-time security notifications
    /// Broadcasts security events to connected clients
    /// </summary>
    [Authorize]
    public class SecurityNotificationHub : Hub
    {
        /// <summary>
        /// Called when client connects to hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("sub")?.Value 
                      ?? Context.User?.FindFirst("userId")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Add user to their personal notification group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when client disconnects from hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst("sub")?.Value 
                      ?? Context.User?.FindFirst("userId")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
