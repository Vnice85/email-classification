using EmailClassification.API.Hubs;
using EmailClassification.Application.Interfaces.INotification;
using Microsoft.AspNetCore.SignalR;

namespace EmailClassification.API.Services
{
    public class SignalRNotificationSender : INotificationSender
    {
        private IHubContext<EmailHub> _hubContext;

        public SignalRNotificationSender(IHubContext<EmailHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyNewLabelAsync(string userId, string emailId, string newLabel)
        {
            try
            {
                await _hubContext.Clients
                    .User(userId)
                    .SendAsync("ReceiveNewLabels", new { emailId, newLabel });

                await _hubContext.Clients
                    .Group(userId)
                    .SendAsync("ReceiveNewLabels", new { emailId, newLabel });
            }
            catch
            {
                throw;
            }
        }
    }
}
