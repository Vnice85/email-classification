using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Application.DTOs.Guest
{
    public class EmailDetailDTO
    {
        public string EmailId { get; set; } = null!;

        public string? SaveDate { get; set; }

        public string? Subject { get; set; }

        public string? Body { get; set; }

        public string? LabelName { get; set; }
    }
}
