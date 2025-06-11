using EmailClassification.Application.Interfaces.Background;
using Hangfire;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Infrastructure.Service
{
    public class BackgroundJobInitializer : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            RecurringJob.AddOrUpdate<IBackgroundService>("sync-emails", s => s.SyncAllUsersEmails(), "*/2 * * * *");
            RecurringJob.AddOrUpdate<IBackgroundService>("classify-emails", s => s.ClassifyAllUsersEmails(), "*/2 * * * *");
            RecurringJob.AddOrUpdate<IBackgroundService>("delete-guest", s => s.DeleteGuestAsync(), "* * */3 * *");
            return Task.CompletedTask;

        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
