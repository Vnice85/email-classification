using EmailClassification.Application.Interfaces.IServices;

namespace EmailClassification.Infrastructure.Implement
{
    public class ClassificationService : IClassificationService
    {

        public async Task<string> IdentifyLabel(string emailContent)
        {
           
            // fake result
            await Task.Delay(100); 
            var label = new[] { "SPAM", "UNDEFINE", "NORMAL", "PHISING"};
            var random = new Random();
            var value = random.Next(0, label.Count());
            return label[value].ToUpper();
        }
    }
}
