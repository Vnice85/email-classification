using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Application.DTOs.Guest
{
    public class GuestFilter
    {
        [Range(1, int.MaxValue)]
        public int PageIndex { get; set; } = 1;
        [Range(1, 100)]
        public int PageSize { get; set; } = 20;
        public string? LabelName { get; set; }
    }
}
