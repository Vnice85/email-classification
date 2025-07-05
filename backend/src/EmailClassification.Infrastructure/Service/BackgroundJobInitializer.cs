using EmailClassification.Application.Interfaces.Background;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Hosting;

namespace EmailClassification.Infrastructure.Service
{
    public class BackgroundJobInitializer : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            var connection = JobStorage.Current.GetConnection();
            var existingJobs = connection.GetRecurringJobs();

            if (!existingJobs.Any(j => j.Id == "sync-emails"))
            {
                RecurringJob.AddOrUpdate<IBackgroundService>("sync-emails", s => s.SyncAllUsersEmails(), "* */1 * * *");
            }
            //if (!existingJobs.Any(j => j.Id == "classify-emails"))
            //{
            //    RecurringJob.AddOrUpdate<IBackgroundService>("classify-emails", s => s.ClassifyAllUsersEmails(), "* */1 * * *");
            //}
            if (!existingJobs.Any(j => j.Id == "delete-guest"))
            {
                RecurringJob.AddOrUpdate<IBackgroundService>("delete-guest", s => s.DeleteGuestAsync(), "* * */3 * *");
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
