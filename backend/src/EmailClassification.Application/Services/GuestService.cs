using EmailClassification.Application.DTOs;
using EmailClassification.Application.DTOs.Classification;
using EmailClassification.Application.DTOs.Guest;
using EmailClassification.Application.Helpers;
using EmailClassification.Application.Interfaces;
using EmailClassification.Application.Interfaces.IServices;
using EmailClassification.Domain.Enum;
using EmailClassification.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Index.HPRtree;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace EmailClassification.Application.Services
{
    public class GuestService : IGuestService
    {
        private readonly IClassificationService _classificationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGuestContext _context;
        private readonly IEmailSearchService _emailSearchService;
        private string? guestId => _context.GuestId;

        public GuestService(IClassificationService classificationService,
                            IUnitOfWork unitOfWork,
                            IGuestContext context,
                            IEmailSearchService emailSearchService)
        {
            _classificationService = classificationService;
            _context = context;
            _unitOfWork = unitOfWork;
            _emailSearchService = emailSearchService;
        }

        public async Task<string> GenerateGuestIdAsync()
        {
            var id = "guest-" + Guid.NewGuid().ToString();
            var guest = new AppUser
            {
                UserId = id,
                UserName = id.Substring(0, 10),
                CreatedAt = DateTime.UtcNow,
                IsTemp = true
            };
            await _unitOfWork.AppUser.AddAsync(guest);
            await _unitOfWork.SaveAsync();
            return guest.UserId;
        }

        public async Task<GuestEmailHeaderDTO> CreateGuestEmailAsync(CreateGuestEmailDTO email)
        {
            if (guestId == null)
                throw new Exception("Required guest id in header");
            var emailContent = new EmailContent
            {
                From = email.From ?? " ",
                To = email.To ?? " ",
                Subject = email.Subject ?? " ",
                Body = email.Body ?? " ",
                Date = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7))
            };
            var jsonString = await _classificationService.IdentifyLabel(emailContent);
            var label = "UNDEFINE";
            if(jsonString != null)
            {
                var classificationResult = JsonConvert.DeserializeObject<ClassificationResult>(jsonString);
                if (classificationResult != null)
                    label = classificationResult?.Probability >= 0.5 ? "SPAM" : "NORMAL";
            }
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
                var saveItem = new Email
                {
                    UserId = guestId,
                    EmailId = Guid.NewGuid().ToString(),
                    SentDate = DateTime.UtcNow,
                    Subject = email.Subject,
                    Body = email.Body,
                    FromAddress = email.From,
                    ToAddress = email.To,
                    Snippet = email.Body?.Length > 255 ? email.Body.Substring(0, 255) : email.Body,
                    DirectionId = (int)DirectionStatus.DRAFT,
                    LabelId = labelItem.LabelId,
                    PredictionResult = jsonString
                };
                await _unitOfWork.Email.AddAsync(saveItem);
                await _unitOfWork.SaveAsync();
                //await _unitOfWork.BulkInsertAsync(new List<Email> { sendItem });
                await _unitOfWork.CommitTransactionAsync();

                // Fix tracking problem
                var doc = new Email
                {
                    EmailId = saveItem.EmailId,
                    UserId = guestId,
                    SentDate = saveItem.SentDate,
                    FromAddress = saveItem.FromAddress,
                    ToAddress = saveItem.ToAddress,
                    Subject = saveItem.Subject,
                    Body = saveItem.Body,
                    PlainText = saveItem.Body,
                    Snippet = saveItem.Snippet,
                    DirectionId = (int)DirectionStatus.DRAFT,
                    LabelId = labelItem.LabelId
                };
            
                await _emailSearchService.SingleIndexAsync(doc);
                Console.WriteLine("Indexing email: " + saveItem.EmailId);
                return new GuestEmailHeaderDTO
                {
                    EmailId = saveItem.EmailId,
                    SaveDate = DateTimeHelper.FormatToVietnamTime(saveItem.SentDate),
                    From = saveItem.FromAddress,
                    To = saveItem.ToAddress,
                    Subject = saveItem.Subject,
                    Snippet = saveItem.Snippet,
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
            var deleteItem = await _unitOfWork.Email.GetItemWhere(d => d.UserId == guestId && d.EmailId == emailId);
            if (deleteItem == null)
                return false;
            _unitOfWork.Email.Remove(deleteItem);
            return await _unitOfWork.SaveAsync();
        }

        public async Task<GuestEmailHeaderDTO?> EditGuestEmailById(string id, CreateGuestEmailDTO email)
        {
            if (email.Body == string.Empty)
                email.Body = " ";
            var emailContent = new EmailContent
            {
                From = email.From ?? " ",
                To = email.To ?? " ",
                Subject = email.Subject ?? " ",
                Body = email.Body ?? "",
                Date = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7))
            };
            var jsonString = await _classificationService.IdentifyLabel(emailContent);
            var label = "UNDEFINE";
            if(jsonString != null)
            {
                var classificationResult = JsonConvert.DeserializeObject<ClassificationResult>(jsonString);
                if (classificationResult != null)
                    label = classificationResult?.Probability >= 0.5 ? "SPAM" : "NORMAL";

            }
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
                item.FromAddress = email.From;
                item.ToAddress = email.To;
                item.Subject = email.Subject;
                item.Body = email.Body;
                item.Snippet = email.Body?.Length > 255 ? email.Body.Substring(0, 255) : email.Body;
                item.LabelId = labelItem.LabelId;
                _unitOfWork.Email.Update(item);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
                var doc = new Email
                {
                    EmailId = item.EmailId,
                    UserId = guestId,
                    SentDate = item.SentDate,
                    Subject = item.Subject,
                    FromAddress = item.FromAddress,
                    ToAddress = item.ToAddress,
                    Body = item.Body,
                    PlainText = item.Body,
                    Snippet = item.Snippet,
                    DirectionId = (int)DirectionStatus.DRAFT,
                    LabelId = labelItem.LabelId,
                    PredictionResult = jsonString
                };
                await _emailSearchService.SingleIndexAsync(doc);
                return new GuestEmailHeaderDTO
                {
                    EmailId = item.EmailId,
                    SaveDate = DateTimeHelper.FormatToVietnamTime(item.SentDate),
                    From = item.FromAddress,
                    To = item.ToAddress,
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

        public async Task<GuestEmailDetailDTO?> GetGuestEmailByIdAsync(string id)
        {
            var item = await _unitOfWork.Email.AsQueryable(i => i.UserId == guestId && i.EmailId == id)
                                                .Include(i => i.Label).FirstOrDefaultAsync();
            if (item == null)
                return null;
            var jsonString = item.PredictionResult;
            if (jsonString == null)
                jsonString = "{}"; 
            return new GuestEmailDetailDTO
            {
                EmailId = item.EmailId,
                From = item.FromAddress,
                To = item.ToAddress,
                SaveDate = DateTimeHelper.FormatToVietnamTime(item.SentDate),
                Subject = item.Subject,
                Body = item.Body,
                LabelName = item.Label!.LabelName,
                Details = JsonConvert.DeserializeObject<ClassificationResult>(jsonString) ?? new ClassificationResult()
            };
        }

        public async Task<List<GuestEmailHeaderDTO>> GetGuestEmailsAsync(GuestFilter filter)
        {
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
            var guestEmails = emails.Select(email => new GuestEmailHeaderDTO
            {
                EmailId = email.EmailId,
                Subject = email.Subject,
                From = email.FromAddress,
                To = email.ToAddress,
                Snippet = email.Snippet,
                LabelName = email.Label!.LabelName,
                SaveDate = DateTimeHelper.FormatToVietnamTime(email.SentDate),
            }).OrderByDescending(d => d.SaveDate).ToList();

            return guestEmails;
        }

        public async Task<List<GuestEmailSearchHeaderDTO>> SearchGuestEmailAsync(ElasticFilter filter)
        {
            var ls = await _emailSearchService.SearchAsync(guestId!, filter);
            var labelName = _unitOfWork.EmailLabel.AsQueryable().ToList();
            var labelNameDict = new Dictionary<int, string>();
            foreach(var item in labelName)
            {
                labelNameDict.Add(item.LabelId, item.LabelName!);
            }
            var guestEmails = ls.Select(email => new GuestEmailSearchHeaderDTO
            {
                EmailId = email.EmailId,
                From = email.FromAddress,
                To = email.ToAddress,
                Subject = email.Subject,
                Snippet = email.Snippet,
                SaveDate = email.SentDate,
            }).OrderByDescending(d => d.SaveDate).ToList();
            return guestEmails;
        }
    }
}
