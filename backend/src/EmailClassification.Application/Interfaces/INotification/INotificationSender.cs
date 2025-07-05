using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Application.Interfaces.INotification
{
    public interface INotificationSender
    {
        Task NotifyNewLabelAsync(string userId, string emailId, string newLabel);
    }
}
