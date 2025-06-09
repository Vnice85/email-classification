using EmailClassification.Application.Interfaces.IServices;
using Microsoft.Extensions.Logging;

namespace EmailClassification.Infrastructure.Implement
{
    public class ClassificationService : IClassificationService
    {
        private readonly ILogger<ClassificationService> _logger;

        public ClassificationService(ILogger<ClassificationService> logger)
        {
            _logger = logger;
        }
        public async Task<string> IdentifyLabel(string emailContent)
        {
           
            try
            {
                // fake result
                await Task.Delay(100);
                var label = new[] { "SPAM", "UNDEFINE", "NORMAL", "PHISING" };
                var random = new Random();
                var value = random.Next(0, label.Count());
                return label[value].ToUpper();
            }
            catch (Exception ex)
            {
                _logger.LogError("Have problem when classify: " + ex);
                throw;

            }
        }
    }
}
