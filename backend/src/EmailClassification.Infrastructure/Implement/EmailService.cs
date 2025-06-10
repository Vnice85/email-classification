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


namespace EmailClassification.Infrastructure.Implement;

public class EmailService : IEmailService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly HttpClient _httpClient;
    private readonly IClassificationService _classificationService;
    private readonly IEmailSearchService _emailSearchService;
    private readonly IConfiguration _configuration;

    public EmailService(IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        HttpClient httpClient,
        IClassificationService classificationService,
        IEmailSearchService emailSearchService,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _httpClient = httpClient;
        _classificationService = classificationService;
        _emailSearchService = emailSearchService;
        _configuration = configuration;
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
        catch
        {
            throw new Exception("Sync email failed");
        }

        BackgroundJob.Enqueue<IEmailService>(e => e.ClassifyAllEmails(userId));
        return (int)messageResponse.StatusCode;
    }

    public Task<List<EmailHeaderDTO>> GetAllEmailsAsync(Filter filter)
    {
        throw new NotImplementedException();
    }


    public Task<EmailDTO?> GetEmailByIdAsync(string emailId)
    {
        throw new NotImplementedException();
    }


    public Task<EmailDTO> SaveDraftEmailAsync(SendEmailDTO email)
    {
        throw new NotImplementedException();
    }

    public Task<EmailDTO?> UpdateDraftEmailByIdAsync(string id, SendEmailDTO email)
    {
        throw new NotImplementedException();
    }

    public Task<int> DeleteEmailAsync(string emailId)
    {
        throw new NotImplementedException();
    }


    public async Task<int> SyncEmailsFromGmail(string userId, string directionName, bool syncAllEmails)
    {
        var token = await _unitOfWork.Token.GetItemWhere(
            t => t.UserId == userId && t.Provider.ToUpper() == "GOOGLE");
        if (token == null || string.IsNullOrWhiteSpace(token.AccessToken) || token.ExpiresAt < DateTime.UtcNow)
            return 0;
        List<string> responseEmailsId = syncAllEmails
            ? await GetLatestEmailId(directionName, token.AccessToken)
            : await GetNewEmailsIdFromHistory(userId, directionName, token.AccessToken);
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
                await ProcessEmailAsync(emailId, token.AccessToken, userId, directionName, newEmails);
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


    public async Task<List<string>> GetNewEmailsIdFromHistory(string userId, string directionName,
        string accessToken)
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
              _configuration["Authentication:Google:EndpointApi"] + "/history?startHistoryId={latestItemInDb}" +
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
                            string id = msgAdded["message"]?["id"]?.ToString();
                            var labels = msgAdded["message"]?["labelIds"]?.ToObject<List<string>>() ?? new();

                            if (!string.IsNullOrEmpty(id) && labels.Contains(upperDirection))
                            {
                                listEmailId.Add(id);
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
            new Uri(_configuration["Authentication:Google:EndpointApi"] + "/messages?labelIds={directionName}");
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
                  _configuration["Authentication:Google:EndpointApi"] + "/messages/{emailId}");
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
                //Body = GZip.CompressStringToBase64(emailInfo.Body!),
                Body = emailInfo.Body!,
                PlainText = emailInfo.PlainText,
                Snippet = emailInfo.Snippet,
                ReceivedDate = emailInfo.ReceivedDate,
                SentDate = emailInfo.SentDate,
                DirectionId = (int)Enum.Parse(typeof(DirectionStatus), directionName.ToUpper()),
                UserId = userEmail,
                HistoryId = emailInfo.HistoryId,
                //LabelId = labelItem.LabelId
            });
        }
        catch
        {
            // log, i will add later
        }
    }


    public async Task ClassifyAllEmails(string userId)
    {
        throw new NotImplementedException();
    }


    public string GetUserEmail()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            throw new Exception("User not found");
        var email = user.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
            throw new Exception("Email claim not found");
        return email;
    }
    public async Task<string> GetAccessTokenAsync(string email)
    {
        var token = await _unitOfWork.Token.GetItemWhere(t => t.UserId.Trim() == email.Trim() && t.Provider.Trim() == "GOOGLE");
        if (token == null)
            throw new Exception("Access token not found");
        return token.AccessToken ?? throw new Exception("Access token is null");
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
            emailInfo.PlainText = HtmlHelper.StripHtmlTags(emailInfo.Body == null ? "" : emailInfo.Body);
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
                    if (partObj?["mimeType"]?.ToString() == "text/html" && !string.IsNullOrEmpty(partData))
                    {
                        emailInfo.Body = DEMail.DecodeBase64(partData);
                        emailInfo.PlainText = HtmlHelper.StripHtmlTags(emailInfo.Body == null ? "" : emailInfo.Body);
                        break;
                    }
                }
            }
        }
        return emailInfo;
    }


}