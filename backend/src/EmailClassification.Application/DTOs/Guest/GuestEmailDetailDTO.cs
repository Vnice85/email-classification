using EmailClassification.Application.DTOs.Classification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Application.DTOs.Guest
{
    public class GuestEmailDetailDTO
    {
        public string EmailId { get; set; } = null!;
        public string? From { get; set; }
        public string? To { get; set; }

        public string? SaveDate { get; set; }

        public string? Subject { get; set; }

        public string? Body { get; set; }

        public string? LabelName { get; set; }
        public ClassificationResult Details { get; set; } = null!;
    }
}
