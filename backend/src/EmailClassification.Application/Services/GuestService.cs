using EmailClassification.Application.DTOs.Guest;
using EmailClassification.Application.Interfaces;
using EmailClassification.Application.Interfaces.IServices;
using EmailClassification.Domain.Enum;
using EmailClassification.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Index.HPRtree;

namespace EmailClassification.Application.Services
{
    public class GuestService : IGuestService
    {
        private readonly IClassificationService _classificationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public GuestService(IClassificationService classificationService,
                            IUnitOfWork unitOfWork,
                            IHttpContextAccessor httpContextAccessor)
        {
            _classificationService = classificationService;
            _httpContextAccessor = httpContextAccessor;
            _unitOfWork = unitOfWork;

        }
        
        public string GetGuestIdAsync()
        {
            var guestId = _httpContextAccessor.HttpContext.Request.Headers["GuestId"].ToString();
            return guestId;
        }

        public async Task<string> GenerateGuestIdAsync()
        {
            var id = "guest-" + Guid.NewGuid().ToString();
            var guest = new AppUser
            {
                UserId = id,
                UserName = id.Substring(0, 10),
            };
            {
            }
            ;
            await _unitOfWork.AppUser.AddAsync(guest);
            await _unitOfWork.SaveAsync();
            return guest.UserId;
        }

        public async Task<EmailDTO> AddGuestEmailAsync(GuestEmailDTO email)
        {
            string guestId = GetGuestIdAsync();
            if (guestId == null)
                throw new Exception("Required guest id in header");
            if (email.Body == string.Empty)
                email.Body = " ";
            var label = await _classificationService.IdentifyLabel(email.Body!);
            await _unitOfWork.BeginTransactionASync();
            try
            {
                var labelItem = await _unitOfWork.EmailLabel.GetItemWhere(ls => ls.LabelName == label);
                if (labelItem == null)
                {
                    labelItem = new EmailLabel
                    {
                        LabelName = label,
                    };
                    await _unitOfWork.EmailLabel.AddAsync(labelItem);
                    await _unitOfWork.SaveAsync();
                }
                var sendItem = new Email
                {
                    UserId = guestId,
                    EmailId = Guid.NewGuid().ToString(),
                    SentDate = DateTime.UtcNow,
                    Subject = email.Subject,
                    Body = email.Body,
                    Snippet = email.Body?.Length > 255 ? email.Body.Substring(0, 255) : email.Body,
                    DirectionId = (int)DirectionStatus.DRAFT,
                    LabelId = labelItem.LabelId
                };
                await _unitOfWork.Email.AddAsync(sendItem);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
                return new EmailDTO
                {
                    EmailId = sendItem.EmailId,
                    SaveDate = sendItem.SentDate?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    Subject = sendItem.Subject,
                    Snippet = sendItem.Snippet,
                    LabelName = labelItem.LabelName
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> DeleteGuestEmailAsync(string emailId)
        {
            string guestId = GetGuestIdAsync();
            var deleteItem = await _unitOfWork.Email.GetItemWhere(d => d.UserId == guestId && d.EmailId == emailId);
            if (deleteItem == null)
                return false;
            _unitOfWork.Email.Delete(deleteItem);
            return await _unitOfWork.SaveAsync();
        }

        public async Task<EmailDTO?> EditGuestEmailById(string id, GuestEmailDTO email)
        {
            string guestId = GetGuestIdAsync();
            if (email.Body == string.Empty)
                email.Body = " ";
            var label = await _classificationService.IdentifyLabel(email.Body!);
            await _unitOfWork.BeginTransactionASync();
            try
            {
                var labelItem = await _unitOfWork.EmailLabel.GetItemWhere(ls => ls.LabelName == label);
                if (labelItem == null)
                {
                    labelItem = new EmailLabel
                    {
                        LabelName = label,
                    };
                    await _unitOfWork.EmailLabel.AddAsync(labelItem);
                    await _unitOfWork.SaveAsync();
                }
                var item = await _unitOfWork.Email.GetItemWhere(ls => ls.UserId == guestId && ls.EmailId == id);
                if (item == null)
                    return null;
                item.SentDate = DateTime.UtcNow;
                item.Subject = email.Subject;
                item.Body = email.Body;
                item.Snippet = email.Body?.Length > 255 ? email.Body.Substring(0, 255) : email.Body;
                item.LabelId = labelItem.LabelId;
                _unitOfWork.Email.Update(item);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
                return new EmailDTO
                {
                    EmailId = item.EmailId,
                    SaveDate = item.SentDate?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    Subject = item.Subject,
                    Snippet = item.Snippet,
                    LabelName = labelItem.LabelName
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<EmailDetailDTO?> GetGuestEmailByIdAsync(string id)
        {
            string guestId = GetGuestIdAsync();
            var item = await _unitOfWork.Email.AsQueryable(i => i.UserId == guestId && i.EmailId == id)
                                                .Include(i => i.Label).FirstOrDefaultAsync();
            if (item == null)
                return null;
            return new EmailDetailDTO
            {
                EmailId = item.EmailId,
                SaveDate = item.SentDate?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                Subject = item.Subject,
                Body = item.Body,
                LabelName = item.Label!.LabelName
            };
        }

        public async Task<List<EmailDTO>> GetGuestEmailsAsync(GuestFilter filter)
        {
            string guestId = GetGuestIdAsync();
            var label = await _unitOfWork.EmailLabel.GetItemWhere(l => l.LabelName == filter.LabelName);
            var query = _unitOfWork.Email.AsQueryable(ls => ls.UserId == guestId && ls.DirectionId == (int)DirectionStatus.DRAFT);

            if (label != null)
            {
                query = query.Where(ls => ls.Label!.LabelName == label.LabelName);
            }
            var emails = await query
                .OrderByDescending(ls => ls.SentDate)
                .Skip((filter.PageIndex - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Include(ls => ls.Label)
                .ToListAsync();
            var guestEmails = emails.Select(email => new EmailDTO
            {
                EmailId = email.EmailId,
                Subject = email.Subject,
                Snippet = email.Snippet,
                LabelName = email.Label!.LabelName,
                SaveDate = email.SentDate?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
            }).OrderByDescending(d => d.SaveDate).ToList();

            return guestEmails;
        }

    }
}
