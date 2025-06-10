
namespace EmailClassification.Application.DTOs.Email
{
    public class EmailDTO
    {
        public string EmailId { get; set; } = null!;

        public string? DirectionName { get; set; }

        public string? LabelName { get; set; }

        public string? FromAddress { get; set; }

        public string? ToAddress { get; set; }

        public string? ReceivedDate { get; set; }

        public string? SentDate { get; set; }

        public string? Snippet { get; set; }

        public string? Subject { get; set; }

        public string? Body { get; set; }


    }
}
