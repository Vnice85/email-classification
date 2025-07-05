using EmailClassification.Application.DTOs.Classification;
using EmailClassification.Application.Interfaces.IServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EmailClassification.Infrastructure.Implement
{
    public class ClassificationService : IClassificationService
    {
        private readonly ILogger<ClassificationService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string path;
        public ClassificationService(ILogger<ClassificationService> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
            path = _configuration["ClassificationApi:Endpoint"];
        }

        public async Task<string?> IdentifyLabel(string emailContent)
        {
            try
            {
                ////var endpoint = _configuration["ClassificationApi:Endpoint"];

                ////var emlBytes = EmailConverter.ConvertToEml(emailContent);

                //using var content = new MultipartFormDataContent();
                //var byteContent = new ByteArrayContent(emlBytes);
                //byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("message/rfc822");

                //content.Add(byteContent, "file", "email.eml");

                //var endpoint = _configuration["ClassificationApi:Endpoint"];
                string endpoint = path + "/classify";
                var payload = new { text = emailContent };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var response = await _httpClient.PostAsync(endpoint, content, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Classification failed. Status code: {StatusCode}, Body: {Body}", response.StatusCode, errorContent);
                    return null;
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                return jsonString;
            }
            catch (Exception ex)
            {
                _logger.LogError("Have problem when classify: " + ex);
                return null;
            }
        }

        public async Task<List<ClassificationResult>?> IdentifyLabelBatch(List<string> emailContents)
        {
            try
            {
                string endpoint = path + "/classify-batch";
                var content = new StringContent(JsonConvert.SerializeObject(emailContents), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Classification failed. Status code: {StatusCode}, Body: {Body}", response.StatusCode, errorContent);
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ClassificationResponse>(responseBody);

                return result?.Results;
            }
            catch (Exception ex)
            {
                _logger.LogError("Have problem when classify: " + ex);
                return null;
            }

        }
    }
}
