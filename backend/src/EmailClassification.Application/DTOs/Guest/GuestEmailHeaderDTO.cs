using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Application.DTOs.Guest
{
    public class GuestEmailHeaderDTO
    {
        public string EmailId { get; set; } = null!;
        public string? From { get; set; } = null!;
        public string? To { get; set; } = null!;

        public string? SaveDate { get; set; }   

        public string? Subject { get; set; }

        public string? Snippet { get; set; }

        public string? LabelName { get; set; }
    }
}
