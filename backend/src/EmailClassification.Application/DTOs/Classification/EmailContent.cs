namespace EmailClassification.Application.DTOs.Classification
{
    public class EmailContent
    {
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public string Subject { get; set; } = "";
        public DateTimeOffset Date { get; set; } 
        public string ContentType { get; set; } = "text/html";
        public string Body { get; set; } = "";
    }

}
