using EmailClassification.Application.DTOs.Classification;
using EmailClassification.Application.Interfaces.IServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace EmailClassification.Infrastructure.Implement
{
    public class ClassificationService : IClassificationService
    {
        private readonly ILogger<ClassificationService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ClassificationService(ILogger<ClassificationService> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
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

                var endpoint = _configuration["ClassificationApi:Endpoint"];
                var payload = new { text = emailContent };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Classification failed. Status code: {StatusCode}, Body: {Body}", response.StatusCode, errorContent);
                    throw new Exception($"Classification failed. Status code: {response.StatusCode}");
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                //var jsonString = @"
                //                {
                //                    ""text"": ""Hi, how are you today?"",
                //                    ""classification"": [
                //                        {
                //                            ""label"": ""Safe Email"",
                //                            ""score"": 0.9925717711448669
                //                        }
                //                    ]
                //                }";


                return jsonString;
            }
            catch (Exception ex)
            {
                _logger.LogError("Have problem when classify: " + ex);
                return null;
                throw;
            }
        }

    }
}
