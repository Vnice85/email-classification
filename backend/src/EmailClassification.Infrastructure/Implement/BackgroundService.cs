using Elasticsearch.Net;
using EmailClassification.Application.Interfaces;
using EmailClassification.Application.Interfaces.Background;
using EmailClassification.Application.Interfaces.IServices;
using EmailClassification.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Infrastructure.Implement
{
    public class BackgroundService : IBackgroundService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BackgroundService> _logger;
        private readonly IEmailSearchService _emailSearchService;

        public BackgroundService(IUnitOfWork unitOfWork, 
                                ILogger<BackgroundService> logger,
                                IEmailSearchService emailSearchService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _emailSearchService = emailSearchService;
        }

        public async Task DeleteGuestAsync()
        {
            // this service's used with background service to delete all data of guest after 3 days
            var guests = await _unitOfWork.AppUser
                                .AsQueryable(u => u.IsTemp && u.CreatedAt < DateTime.UtcNow.AddDays(-3))
                                .ToListAsync();
            if (guests == null || !guests.Any())
            {
                return;
            }
            var guestIds = guests.Select(u => u.UserId).ToHashSet();
            var listEmailsHaveToDelete = await _unitOfWork.Email
                .AsQueryable(e => guestIds.Contains(e.UserId!))
                .ToListAsync();

            await _unitOfWork.BeginTransactionASync();
            try
            {

                if (listEmailsHaveToDelete.Any())
                    _unitOfWork.Email.RemoveRange(listEmailsHaveToDelete);
                _unitOfWork.AppUser.RemoveRange(guests);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
                try
                {
                    var deleteTasks = guests.Select(u => _emailSearchService.DeleteByUserIdAsync(u.UserId));
                    await Task.WhenAll(deleteTasks);
                }
                catch (Exception esEx)
                {
                    _logger.LogError(esEx, "Error when deleting guest emails in Elasticsearch");
                }


            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error when delete guest");
            }

        }

        public async Task SyncAllUsersEmails()
        {
            var userIds = await _unitOfWork.AppUser.AsQueryable(u=> u.IsTemp == false).Select(u => u.UserId).ToListAsync();
            if (!userIds.Any())
            {
                return;
            }
            foreach (var id in userIds)
            {
                try
                {
                    BackgroundJob.Enqueue<IEmailService>(s => s.SyncEmailsFromGmail(id, "INBOX", false));
                    BackgroundJob.Enqueue<IEmailService>(s => s.SyncEmailsFromGmail(id, "SENT", false));
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Error when enqueue sync emails for user {UserId}", id);
                }

            }
        }
        
        public async Task ClassifyAllUsersEmails()
        {
            var userIds = await _unitOfWork.AppUser.AsQueryable().Select(u => u.UserId).ToListAsync();
            if (!userIds.Any())
            {
                return;
            }
            foreach (var id in userIds)
            {
                BackgroundJob.Enqueue<IEmailService>(s => s.ClassifyAllEmails(id));
            }
        }

    }
}
