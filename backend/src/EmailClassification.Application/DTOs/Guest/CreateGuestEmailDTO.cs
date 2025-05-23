using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Application.DTOs.Guest
{
    public class CreateGuestEmailDTO
    {
        public string? Subject { get; set; }

        public string? Body { get; set; }
    }
}
