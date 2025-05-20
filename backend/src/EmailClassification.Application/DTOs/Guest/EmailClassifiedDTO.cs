using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Application.DTOs.Guest
{
    public class EmailClassifiedDTO
    {
        public string GuestId { get; set; } = null!;
        public string EmailId { get; set; } = null!;
        public string? SaveDate { get; set; }   

        public string? Subject { get; set; }

        public string? Body { get; set; }

        public string? LabelName { get; set; }
    }
}
