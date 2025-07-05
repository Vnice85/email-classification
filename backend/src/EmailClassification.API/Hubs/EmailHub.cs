using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace EmailClassification.API.Hubs
{
    public class EmailHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> _userConnections = new();

        public async Task RegisterUser(string userId)
        {
            _userConnections[Context.ConnectionId] = userId;
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (_userConnections.TryRemove(Context.ConnectionId, out var userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
