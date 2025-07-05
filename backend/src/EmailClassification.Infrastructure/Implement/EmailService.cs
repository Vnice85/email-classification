using EmailClassification.Application.Interfaces.IServices;
using EmailClassification.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Text;
using EmailClassification.Application.DTOs.Email;
using EmailClassification.Domain.Enum;
using Newtonsoft.Json.Linq;
using System.Security.Claims;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using EmailClassification.Application.Helpers;
using System.Collections.Concurrent;
using EFCore.BulkExtensions;
using Newtonsoft.Json;
using Hangfire;
using EmailClassification.Infrastructure.Persistence;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EmailClassification.Application.DTOs.Classification;
using NetTopologySuite.Index.HPRtree;
using Microsoft.AspNetCore.SignalR;
using EmailClassification.Application.Interfaces.INotification;
using static EmailClassification.Application.DTOs.Classification.ClassificationResult;


namespace EmailClassification.Infrastructure.Implement;

public class EmailService : IEmailService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly HttpClient _httpClient;
    private readonly IClassificationService _classificationService;
    private readonly IEmailSearchService _emailSearchService;
    private readonly IConfiguration _configuration;
    private readonly INotificationSender _notificationSender;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        HttpClient httpClient,
        IClassificationService classificationService,
        IEmailSearchService emailSearchService,
        IConfiguration configuration,
        ILogger<EmailService> logger,
        INotificationSender notificationSender)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _httpClient = httpClient;
        _classificationService = classificationService;
        _emailSearchService = emailSearchService;
        _configuration = configuration;
        _notificationSender = notificationSender;
        _logger = logger;
    }


    public async Task<int> SendEmailAsync(SendEmailDTO email)
    {
        var userId = GetUserEmail();
        var accessToken = await GetAccessTokenAsync(userId);
        var body = $"From: {userId}\r\n" +
                   $"To: {email.ToAddress}\r\n" +
                   $"Subject: {email.Subject}\r\n\r\n" +
                   $"{email.Body}";
        var bodyBase64 = DEMail.EncodeBase64(body);
        var payload = new
        {
            raw = bodyBase64
        };

        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

        using var messageRequest = new HttpRequestMessage(HttpMethod.Post,
            _configuration["Authentication:Google:EndpointApi"] + "/messages/send");
        messageRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        messageRequest.Content = content;
        var messageResponse = await _httpClient.SendAsync(messageRequest);
        try
        {
            await SyncEmailsFromGmail(userId, "SENT", false);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to sync sent email for user {UserId} : {ex}", userId, ex);
            throw new Exception("Sync email failed");
        }

        BackgroundJob.Enqueue<IEmailService>(e => e.ClassifyAllEmailsByBatch(userId));
        return (int)messageResponse.StatusCode;
    }

    public async Task<List<EmailHeaderDTO>> GetAllEmailsAsync(Filter filter)
    {
        var userId = GetUserEmail();
        var query = _unitOfWork.Email.AsQueryable(ls => ls.UserId == userId);
        if (filter.LabelName != null)
        {
            var label = await _unitOfWork.EmailLabel.GetItemWhere(l => l.LabelName == filter.LabelName.ToUpper());
            if (label != null)
            {
                query = query.Where(ls => ls.Label!.LabelName == label.LabelName);
            }
        }

        if (!string.IsNullOrEmpty(filter.DirectionName) &&
            Enum.TryParse<DirectionStatus>(filter.DirectionName, true, out var direction))
        {
            query = query.Where(ls => ls.DirectionId == (int)direction);
        }

        var emails = await query
            .OrderByDescending(ls => ls.SentDate)
            .Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Include(ls => ls.Label)
            .AsNoTracking()
            .ToListAsync();
        if (!emails.Any())
        {
            try
            {
                await SyncEmailsFromGmail(userId, "INBOX", true);
                await SyncEmailsFromGmail(userId, "SENT", true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to sync emails for user {UserId} : {ex}", userId, ex);
                throw new Exception(ex.Message);
            }
            finally
            {
                emails = await query
                    .OrderByDescending(ls => ls.SentDate)
                    .Skip((filter.PageIndex - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Include(ls => ls.Label)
                    .AsNoTracking()
                    .ToListAsync();
            }

            BackgroundJob.Enqueue<IEmailService>(e => e.ClassifyAllEmailsByBatch(userId));
        }

        var emailHeaderDtos = emails.Select(email => new EmailHeaderDTO
        {
            EmailId = email.EmailId,
            FromAddress = email.FromAddress,
            ToAddress = email.ToAddress,
            Snippet = email.Snippet,
            ReceivedDate = DateTimeHelper.FormatToVietnamTime(email.ReceivedDate),
            SentDate = DateTimeHelper.FormatToVietnamTime(email.SentDate),
            Subject = email.Subject,
            DirectionName = ((DirectionStatus)email.DirectionId).ToString(),
            LabelName = email.Label?.LabelName
        }).ToList();
        return emailHeaderDtos;
    }


    public async Task<EmailDetailDTO?> GetEmailByIdAsync(string emailId)
    {
        var userId = GetUserEmail();
        var item = await _unitOfWork.Email.AsQueryable(i => i.EmailId == emailId && i.UserId == userId)
            .Include(i => i.Label).FirstOrDefaultAsync();
        if (item == null)
            return null;
        return new EmailDetailDTO
        {
            EmailId = item.EmailId,
            FromAddress = item.FromAddress,
            ToAddress = item.ToAddress,
            ReceivedDate = DateTimeHelper.FormatToVietnamTime(item.ReceivedDate),
            SentDate = DateTimeHelper.FormatToVietnamTime(item.SentDate),
            Subject = item.Subject,
            Body = item.Body!,
            DirectionName = ((DirectionStatus)item.DirectionId).ToString(),
            LabelName = item.Label?.LabelName ?? "UNDEFINE",
            Details = string.IsNullOrEmpty(item.PredictionResult)
                ? new ClassificationResult()
                : JsonConvert.DeserializeObject<ClassificationResult>(item.PredictionResult) ?? new ClassificationResult()
        };
    }


    public async Task<EmailDTO> SaveDraftEmailAsync(SendEmailDTO email)
    {
        var userId = GetUserEmail();
        if (email.Body == string.Empty)
            email.Body = " ";

        //var emailContent = new EmailContent
        //{
        //    From = userId,
        //    To = email.ToAddress ?? "",
        //    Subject = email.Subject ?? "",
        //    Body = email.Body ?? "",
        //    Date = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7))
        //};
        var label = "UNDEFINE";
        var jsonString = await _classificationService.IdentifyLabel(email.Body ?? " ");
        if (jsonString != null)
        {
            var classificationResult = JsonConvert.DeserializeObject<ClassificationResult>(jsonString);
            if (classificationResult != null)
                label = classificationResult?.Classification[0].label == "Phishing Email" ? "SPAM/PHISHING" : "NORMAL";
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

            var sendItem = new Email
            {
                UserId = userId,
                EmailId = Guid.NewGuid().ToString(),
                SentDate = DateTime.UtcNow,
                FromAddress = userId,
                Snippet = email.Subject,
                ToAddress = email.ToAddress,
                Subject = email.Subject,
                Body = email.Body!,
                DirectionId = (int)DirectionStatus.DRAFT,
                LabelId = labelItem.LabelId,
                PredictionResult = jsonString
            };
            await _unitOfWork.Email.AddAsync(sendItem);
            await _unitOfWork.SaveAsync();
            await _unitOfWork.CommitTransactionAsync();
            return new EmailDTO
            {
                EmailId = sendItem.EmailId,
                FromAddress = sendItem.FromAddress,
                ToAddress = sendItem.ToAddress,
                ReceivedDate = DateTimeHelper.FormatToVietnamTime(sendItem.ReceivedDate),
                SentDate = DateTimeHelper.FormatToVietnamTime(sendItem.SentDate),
                Subject = sendItem.Subject,
                Snippet = sendItem.Snippet,
                DirectionName = ((DirectionStatus)sendItem.DirectionId).ToString(),
                LabelName = labelItem.LabelName
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError("Error saving draft email for user {UserId} : {ex}", userId, ex);
            throw;
        }
    }

    public async Task<EmailDTO?> UpdateDraftEmailByIdAsync(string id, SendEmailDTO email)
    {
        var userId = GetUserEmail();

        if (email.Body == string.Empty)
            email.Body = " ";
        var emailContent = new EmailContent
        {
            From = userId,
            To = email.ToAddress ?? "",
            Subject = email.Subject ?? "",
            Body = email.Body ?? "",
            Date = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7))
        };
        var jsonString = await _classificationService.IdentifyLabel(email.Body ?? " ");
        var label = "UNDEFINE";
        if (jsonString != null)
        {
            var classificationResult = JsonConvert.DeserializeObject<ClassificationResult>(jsonString);
            if (classificationResult != null)
                label = classificationResult?.Classification[0].label == "Phishing Email" ? "SPAM/PHISHING" : "NORMAL";
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

            var item = await _unitOfWork.Email.GetItemWhere(ls => ls.UserId == userId && ls.EmailId == id);
            if (item == null)
                return null;
            item.SentDate = DateTime.UtcNow;
            item.Subject = email.Subject;
            item.Snippet = email.Subject;
            item.ToAddress = email.ToAddress;
            item.Body = email.Body!;
            item.LabelId = labelItem.LabelId;
            item.PredictionResult = jsonString;
            _unitOfWork.Email.Update(item);
            await _unitOfWork.SaveAsync();
            await _unitOfWork.CommitTransactionAsync();
            return new EmailDTO
            {
                EmailId = item.EmailId,
                FromAddress = item.FromAddress,
                ToAddress = item.ToAddress,
                Snippet = item.Snippet,
                ReceivedDate = DateTimeHelper.FormatToVietnamTime(item.ReceivedDate),
                SentDate = DateTimeHelper.FormatToVietnamTime(item.SentDate),
                Subject = item.Subject,
                DirectionName = ((DirectionStatus)item.DirectionId).ToString(),
                LabelName = item.Label?.LabelName
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError("Error updating draft email for user {UserId} : {ex}", userId, ex);
            throw;
        }
    }

    public async Task<int> DeleteEmailAsync(string emailId)
    {
        var userId = GetUserEmail();
        var accessToken = await GetAccessTokenAsync(userId);
        var email = await _unitOfWork.Email.GetItemWhere(e => e.EmailId == emailId && e.UserId == userId);
        if (email == null)
            return 404;
        await _unitOfWork.BeginTransactionASync();
        try
        {
            if (email.DirectionId == (int)DirectionStatus.DRAFT)
            {
                _unitOfWork.Email.Remove(email);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
                return 204;
            }

            using var messageRequest = new HttpRequestMessage(HttpMethod.Delete,
              _configuration["Authentication:Google:EndpointApi"] + $"/messages/{emailId}");
            messageRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var messageResponse = await _httpClient.SendAsync(messageRequest);
            if (messageResponse.IsSuccessStatusCode)
            {
                _unitOfWork.Email.Remove(email);
                await _unitOfWork.SaveAsync();
                await _emailSearchService.DeleteAsync(emailId);
                await _unitOfWork.CommitTransactionAsync();
                return 204;
            }

            return (int)messageResponse.StatusCode;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError("Error deleting email {EmailId} for user {UserId} : {ex}", emailId, userId, ex);
            return 500;
        }
    }


    public async Task<int> SyncEmailsFromGmail(string userId, string directionName, bool syncAllEmails)
    {
        var token = await _unitOfWork.Token.GetItemWhere(
            t => t.UserId == userId && t.Provider.ToUpper() == "GOOGLE");
        if (token == null || string.IsNullOrWhiteSpace(token.AccessToken) || token.ExpiresAt < DateTime.UtcNow)
            return 0;
        // Decrypt accesstoken before using it
        // NOTE: mustn't use token.AccessToken directly, because it is encrypted in database
        // and have background job using it
        // so if you use like this : token.accessToken = AesHelper.Decrypt(token.AccessToken, _configuration["Aes:Key"] ?? "");
        // boooom, it will cause error in background job =)))))
        var accessToken = await GetAccessTokenAsync(userId);
        List<string> responseEmailsId = syncAllEmails
            ? await GetLatestEmailId(directionName, accessToken)
            : await GetNewEmailsIdFromHistory(userId, directionName, accessToken);
        if (!responseEmailsId.Any())
        {
            return 0;
        }
        var existEmails = await _unitOfWork.Email
            .AsQueryable(e => responseEmailsId
                .Contains(e.EmailId))
            .Select(e => e.EmailId)
            .AsNoTracking()
            .ToListAsync();
        var hashSetEmail = new HashSet<string>(existEmails);
        var listId = responseEmailsId.Where(e => !hashSetEmail.Contains(e)).ToList();
        ConcurrentBag<Email> newEmails = new();
        if (!listId.Any())
        {
            return 0;
        }

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 20,
        };
        await Parallel.ForEachAsync(listId.Where(id => !string.IsNullOrWhiteSpace(id)), options,
            async (emailId, _) =>
            {
                await ProcessEmailAsync(emailId, accessToken, userId, directionName, newEmails);
            });
        await _unitOfWork.BulkInsertAsync(newEmails.ToList());
        await _emailSearchService.BulkIndexAsync(newEmails.ToList());

        return newEmails.Count();
    }


    public async Task<bool> ExistHistoryId(string userId)
    {
        var historyId = await _unitOfWork.Email.AsQueryable(e => e.UserId == userId)
            .OrderByDescending(e => e.HistoryId)
            .Select(e => e.HistoryId)
            .FirstOrDefaultAsync();
        return historyId != null;
    }


    public async Task<List<string>> GetNewEmailsIdFromHistory(string userId, string directionName, string accessToken)
    {
        var latestItemInDb = await _unitOfWork.Email.AsQueryable(l => l.UserId == userId)
            .OrderByDescending(l => Convert.ToInt64(l.HistoryId))
            .Select(l => l.HistoryId)
            .FirstOrDefaultAsync();

        if (latestItemInDb == null)
        {
            return new List<string>();
        }

        var listEmailId = new ConcurrentBag<string>();
        string? nextPageToken = null;
        string upperDirection = directionName.ToUpper();
        do
        {
            using var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = new Uri(
              _configuration["Authentication:Google:EndpointApi"] + $"/history?startHistoryId={latestItemInDb}" +
                $"&maxResults=500&historyTypes=messageAdded" +
                $"{(string.IsNullOrEmpty(nextPageToken) ? "" : $"&pageToken={nextPageToken}")}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var root = JObject.Parse(responseBody);
            var historyArray = root["history"] as JArray;

            if (historyArray != null)
            {
                Parallel.ForEach(historyArray, new ParallelOptions { MaxDegreeOfParallelism = 20 }, (item, _) =>
                {
                    var messagesAdded = item["messagesAdded"] as JArray;
                    if (messagesAdded != null)
                    {
                        foreach (var msgAdded in messagesAdded)
                        {
                            string? id = msgAdded["message"]?["id"]?.ToString();
                            if (!string.IsNullOrEmpty(id))
                            {
                                var labels = msgAdded["message"]?["labelIds"]?.ToObject<List<string>>() ?? new();
                                if (labels.Contains(upperDirection))
                                {
                                    listEmailId.Add(id);
                                }
                            }
                        }
                    }
                });
            }

            nextPageToken = root["nextPageToken"]?.ToString();
        } while (!string.IsNullOrEmpty(nextPageToken));

        return listEmailId.ToList();
    }


    public async Task<List<string>> GetLatestEmailId(string directionName, string accessToken)
    {
        var listEmailId = new List<string>();
        using var request = new HttpRequestMessage();
        request.Method = HttpMethod.Get;
        request.RequestUri =
            new Uri(_configuration["Authentication:Google:EndpointApi"] + $"/messages?labelIds={directionName}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var root = JObject.Parse(responseBody);
        var jsonArray = root["messages"] as JArray;
        if (jsonArray != null)
            listEmailId.AddRange(jsonArray.Select(l => (string)l["id"]!).ToList());
        return listEmailId;
    }


    private async Task ProcessEmailAsync(string emailId,
        string accessToken,
        string userEmail,
        string directionName,
        ConcurrentBag<Email> newEmails)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                  _configuration["Authentication:Google:EndpointApi"] + $"/messages/{emailId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return;

            var body = await response.Content.ReadAsStringAsync();
            var jsonObject = JObject.Parse(body);
            var emailInfo = GetEmailInfo(jsonObject);

            if (emailInfo == null) return;
            newEmails.Add(new Email
            {
                EmailId = emailId,
                FromAddress = emailInfo.FromAddress,
                ToAddress = emailInfo.ToAddress,
                Subject = emailInfo.Subject,
                Body = emailInfo.Body!,
                PlainText = emailInfo.PlainText,
                Snippet = emailInfo.Snippet,
                ReceivedDate = emailInfo.ReceivedDate,
                SentDate = emailInfo.SentDate,
                DirectionId = (int)Enum.Parse(typeof(DirectionStatus), directionName.ToUpper()),
                UserId = userEmail,
                HistoryId = emailInfo.HistoryId,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email {EmailId} for user {UserEmail}", emailId, userEmail);
            throw;
        }
    }

    [DisableConcurrentExecution(timeoutInSeconds: 3600)]
    public async Task ClassifyAllEmails(string userId)
    {
        var semaphore = new SemaphoreSlim(1, 1);
        var emails = new List<Email>();
        do
        {
            emails.Clear();
            var labels = await _unitOfWork.EmailLabel.AsQueryable().AsNoTracking().ToListAsync();
            var undifineId = labels.FirstOrDefault(l => l.LabelName == "UNDEFINE")?.LabelId;
            var query = _unitOfWork.Email.AsQueryable(e => (e.LabelId == null || e.LabelId == undifineId) && e.UserId == userId).OrderByDescending(e => e.HistoryId).Take(100)
                    .Select(e => new Email
                    {
                        EmailId = e.EmailId,
                        Body = e.Body,
                        Subject = e.Subject,
                        FromAddress = e.FromAddress,
                        ToAddress = e.ToAddress,
                        ReceivedDate = e.ReceivedDate,
                        SentDate = e.SentDate,
                        // 
                        DirectionId = e.DirectionId,
                        HistoryId = e.HistoryId,
                        Snippet = e.Snippet,
                        PlainText = e.PlainText,
                        UserId = e.UserId
                    });
            emails.AddRange(await query.ToListAsync());
            //var tasks = emails.Select(async item =>
            //{
            //    //var emailContent = new EmailContent
            //    //{
            //    //    From = userId,
            //    //    To = item.ToAddress ?? "",
            //    //    Subject = item.Subject ?? "",
            //    //    Body = item.Body ?? "",
            //    //    Date = new DateTimeOffset(
            //    //        DateTime.SpecifyKind(item.SentDate ?? item.ReceivedDate ?? DateTime.UtcNow, DateTimeKind.Unspecified), 
            //    //        TimeSpan.FromHours(7)
            //    //        )
            //    //};
            //    var jsonString = await _classificationService.IdentifyLabel(item.PlainText ?? " ");
            //    var label = "UNDEFINE";
            //    if (jsonString != null)
            //    {
            //        var classificationResult = JsonConvert.DeserializeObject<ClassificationResult>(jsonString);
            //        if (classificationResult != null)
            //            label = classificationResult?.Classification[0].label == "Phishing Email" ? "SPAM/PHISHING" : "NORMAL";
            //    }
            //    var labelItem = labels.FirstOrDefault(l => l.LabelName == label);
            //    if (labelItem == null)
            //    {
            //        await semaphore.WaitAsync();
            //        try
            //        {
            //            labelItem = labels.FirstOrDefault(l => l.LabelName == label);
            //            if (labelItem == null)
            //            {
            //                labelItem = new EmailLabel { LabelName = label };
            //                await _unitOfWork.EmailLabel.AddAsync(labelItem);
            //                await _unitOfWork.SaveAsync();
            //                labels.Add(labelItem);
            //            }
            //        }
            //        finally
            //        {
            //            semaphore.Release();
            //        }
            //    }

            //    item.LabelId = labelItem.LabelId;
            //    item.PredictionResult = jsonString;
            //});
            //await Task.WhenAll(tasks);
            foreach (var item in emails)
            {
                var jsonString = await _classificationService.IdentifyLabel(item.PlainText ?? "N/A");
                var label = "UNDEFINE";

                if (jsonString != null)
                {
                    var classificationResult = JsonConvert.DeserializeObject<ClassificationResult>(jsonString);
                    if (classificationResult != null)
                        label = classificationResult.Classification[0].label == "Phishing Email" ? "SPAM/PHISHING" : "NORMAL";
                }

                var labelItem = labels.FirstOrDefault(l => l.LabelName == label);

                if (labelItem == null)
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        labelItem = labels.FirstOrDefault(l => l.LabelName == label);
                        if (labelItem == null)
                        {
                            labelItem = new EmailLabel { LabelName = label };
                            await _unitOfWork.EmailLabel.AddAsync(labelItem);
                            await _unitOfWork.SaveAsync();
                            labels.Add(labelItem);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }
                item.LabelId = labelItem.LabelId;
                item.PredictionResult = jsonString;
                try
                {
                    _unitOfWork.Email.Update(item);
                    await _unitOfWork.SaveAsync();
                    await _notificationSender.NotifyNewLabelAsync(
                    userId,
                        item.EmailId,
                        labels.First(l => l.LabelId == item.LabelId).LabelName);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Have problem when update email with id={EmailId}: {Message}", item.EmailId, ex.Message);
                }
            }

            //try
            //{
            //    await _unitOfWork.BulkUpdateAsync(emails, new BulkConfig
            //    {
            //        PropertiesToInclude = new List<string> { nameof(Email.EmailId), nameof(Email.LabelId), nameof(Email.PredictionResult) },
            //    });
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError("Error updating labels for user {UserId}: {Message}", userId, ex);
            //    throw;
            //}
        } while (emails.Any());
    }

    [DisableConcurrentExecution(timeoutInSeconds: 3600)]
    public async Task ClassifyAllEmailsByBatch(string userId)
    {
        var semaphore = new SemaphoreSlim(1, 1);
        var emails = new List<Email>();

        do
        {
            emails.Clear();
            var labels = await _unitOfWork.EmailLabel.AsQueryable().AsNoTracking().ToListAsync();
            var undefineId = labels.FirstOrDefault(l => l.LabelName == "UNDEFINE")?.LabelId;

            var query = _unitOfWork.Email.AsQueryable(e =>
                (e.LabelId == null || e.LabelId == undefineId) && e.UserId == userId)
                .OrderByDescending(e => e.HistoryId)
                .Take(5)
                .Select(e => new Email
                {
                    EmailId = e.EmailId,
                    Body = e.Body,
                    Subject = e.Subject,
                    FromAddress = e.FromAddress,
                    ToAddress = e.ToAddress,
                    ReceivedDate = e.ReceivedDate,
                    SentDate = e.SentDate,
                    DirectionId = e.DirectionId,
                    HistoryId = e.HistoryId,
                    Snippet = e.Snippet,
                    PlainText = e.PlainText,
                    UserId = e.UserId
                });

            emails.AddRange(await query.ToListAsync());

            if (!emails.Any()) break;

            var texts = emails.Select(e => e.PlainText ?? "N/A").ToList();
            var batchResult = await _classificationService.IdentifyLabelBatch(texts);

            if (batchResult == null || !batchResult?.Any() == true || batchResult.Count != emails.Count)
            {
                _logger.LogWarning("Batch classify result mismatch with emails count.");
                return;
            }

            // Tạo transaction để batch update
            await _unitOfWork.BeginTransactionASync();

            try
            {
                for (int i = 0; i < emails.Count; i++)
                {
                    var email = emails[i];
                    var result = batchResult[i];

                    var label = "UNDEFINE";
                    if (result.Classification?.Any() == true)
                    {
                        var labelRaw = result.Classification[0].label;
                        label = labelRaw == "Phishing Email" ? "SPAM/PHISHING" : "NORMAL";
                    }

                    var labelItem = labels.FirstOrDefault(l => l.LabelName == label);

                    if (labelItem == null)
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            labelItem = new EmailLabel { LabelName = label };
                            await _unitOfWork.EmailLabel.AddAsync(labelItem);
                            await _unitOfWork.SaveAsync();
                            labels.Add(labelItem);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }

                    email.LabelId = labelItem.LabelId;
                    email.PredictionResult = JsonConvert.SerializeObject(result);
                    _unitOfWork.Email.Update(email);
                }

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                foreach (var email in emails)
                {
                    await _notificationSender.NotifyNewLabelAsync(
                        userId,
                        email.EmailId,
                        labels.First(l => l.LabelId == email.LabelId).LabelName);
                }
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError("Error in batch classification: {Message}", ex.Message);
                throw;
            }

        } while (emails.Any());
    }



    public string GetUserEmail()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
        {
            _logger.LogError("User not found in HTTP context");
            throw new Exception("User not found");
        }
        var email = user.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogError("Email claim not found");
            throw new Exception("Email claim not found");
        }
        return email;
    }
    public async Task<string> GetAccessTokenAsync(string email)
    {

        var token = await _unitOfWork.Token.GetItemWhere(t => t.UserId.Trim() == email.Trim() && t.Provider.Trim() == "GOOGLE");
        if (token == null || string.IsNullOrWhiteSpace(token.AccessToken) || token.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogError("Access token not found for user {Email}", email);
            throw new Exception("Access token not found");
        }
        var decryptAccessToken = AesHelper.Decrypt(token.AccessToken ?? "", _configuration["Aes:Key"] ?? "");
        return decryptAccessToken ?? throw new Exception("Access token is null");
    }


    private EmailInfo? GetEmailInfo(JObject message)
    {
        var payload = message["payload"] as JObject;
        if (payload == null) return null;

        var emailInfo = new EmailInfo();

        if (message["historyId"] != null)
        {
            emailInfo.HistoryId = message["historyId"]?.ToString();
        }

        if (message["snippet"] != null)
        {
            var snippet = message["snippet"]?.ToString();
            emailInfo.Snippet = snippet?.Length > 255 ? snippet.Substring(0, 255) : snippet;
        }
        if (message["internalDate"] != null && long.TryParse(message["internalDate"]?.ToString(), out long timestamp))
        {
            emailInfo.SentDate = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
        }

        var headers = payload["headers"] as JArray;
        if (headers != null)
        {
            foreach (var header in headers)
            {
                var headerObj = header as JObject;
                var name = headerObj?["name"]?.ToString();
                var value = headerObj?["value"]?.ToString();

                switch (name)
                {
                    case "From":
                        if (value != null)
                        {
                            var regFrom = Regex.Match(value, @"<(.+?)>");
                            if (regFrom.Success)
                            {
                                emailInfo.FromAddress = regFrom.Groups[1].Value;
                            }
                            else
                            {
                                emailInfo.FromAddress = !string.IsNullOrEmpty(value)
                                    ? (value.Length > 255 ? value.Substring(0, 255) : value)
                                    : null;
                            }
                        }

                        break;
                    case "To":
                        if (value != null)
                        {
                            var regTo = Regex.Match(value, @"<(.+?)>");
                            if (regTo.Success)
                            {
                                emailInfo.ToAddress = regTo.Groups[1].Value;
                            }
                            else
                            {
                                emailInfo.ToAddress = !string.IsNullOrEmpty(value)
                                    ? (value.Length > 255 ? value.Substring(0, 255) : value)
                                    : null;

                            }
                        }

                        break;
                    case "Subject":
                        emailInfo.Subject = !string.IsNullOrEmpty(value)
                                            ? (value.Length > 255 ? value.Substring(0, 255) : value)
                                            : null;

                        break;
                    case "Date":
                        if (DateTime.TryParse(value, out DateTime receivedDate))
                        {
                            emailInfo.ReceivedDate = DateTimeHelper.NormalizeDateTime(receivedDate);
                        }
                        break;
                    case "Received":
                        if (value != null)
                        {
                            var datePart = value.Split(';').LastOrDefault()?.Trim();
                            if (DateTime.TryParse(datePart, out DateTime receivedDateFromHeader))
                            {
                                emailInfo.ReceivedDate = DateTimeHelper.NormalizeDateTime(receivedDateFromHeader);
                            }
                        }
                        break;
                }
            }
        }
        if (payload["mimeType"]?.ToString() == "text/html")
        {
            var bodyData = payload["body"]?["data"]?.ToString();
            emailInfo.Body = !string.IsNullOrEmpty(bodyData)
                             ? DEMail.DecodeBase64(bodyData)
                             : null;
            emailInfo.PlainText = HtmlHelper.StripHtmlTags(emailInfo.Body ?? "");
        }
        else if (payload["mimeType"]?.ToString() == "text/plain")
        {
            var bodyData = payload["body"]?["data"]?.ToString();
            emailInfo.PlainText = !string.IsNullOrEmpty(bodyData)
                                 ? DEMail.DecodeBase64(bodyData)
                                 : null;
            emailInfo.Body = emailInfo.PlainText;
        }
        else
        {
            var parts = payload["parts"] as JArray;
            if (parts != null)
            {
                foreach (var part in parts)
                {
                    var partObj = part as JObject;
                    var partData = partObj?["body"]?["data"]?.ToString();
                    var mimeType = partObj?["mimeType"]?.ToString();

                    if (mimeType == "text/html" && !string.IsNullOrEmpty(partData))
                    {
                        emailInfo.Body = DEMail.DecodeBase64(partData);
                        emailInfo.PlainText = HtmlHelper.StripHtmlTags(emailInfo.Body ?? "");
                        break;
                    }
                    else if (mimeType == "text/plain" && !string.IsNullOrEmpty(partData) && string.IsNullOrEmpty(emailInfo.PlainText))
                    {
                        emailInfo.PlainText = DEMail.DecodeBase64(partData);
                        if (string.IsNullOrEmpty(emailInfo.Body))
                        {
                            emailInfo.Body = emailInfo.PlainText;
                        }
                    }
                }
            }
        }
        return emailInfo;
    }


}