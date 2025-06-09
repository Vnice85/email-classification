using Elasticsearch.Net;
using EmailClassification.Application.Interfaces;
using EmailClassification.Application.Interfaces.Background;
using EmailClassification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Infrastructure.Implement
{
    public class BackgroundService : IBackgroundService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BackgroundService> _logger;

        public BackgroundService(IUnitOfWork unitOfWork, ILogger<BackgroundService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
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

            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error when delete guest");
            }

        }

    }
}
