using EmailClassification.Application.DTOs.Classification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Application.DTOs.Email
{
    public class EmailDetailDTO
    {
        public string EmailId { get; set; } = null!;

        public string? DirectionName { get; set; }


        public string? FromAddress { get; set; }

        public string? ToAddress { get; set; }

        public string? ReceivedDate { get; set; }

        public string? SentDate { get; set; }

        public string? Subject { get; set; }

        public string? Body { get; set; }
        public string? LabelName { get; set; }
        public ClassificationResult Details { get; set; } = null!;
    }
}
