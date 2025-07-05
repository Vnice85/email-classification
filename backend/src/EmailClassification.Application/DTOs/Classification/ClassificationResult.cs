using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Application.DTOs.Classification
{
    public class ClassificationResult
    {
        //public int Prediction { get; set; }
        public string? Text { get; set; }
        public List<Detail> Classification { get; set; } = new List<Detail>();

        public class Detail
        {
            public string? label { get; set; }
            public double score { get; set; }

        }

        //    [JsonProperty("class")]
        //    public string Label { get; set; } = null!;

        //    [JsonProperty("confidence_level")]
        //    public string ConfidenceLevel { get; set; } = null!;

        //    public Details Details { get; set; } = new Details();
        //}

        //public class Details
        //{
        //    public Flags Flags { get; set; } = new Flags();
        //}

        //public class Flags
        //{
        //    [JsonProperty("urgent_keywords")]
        //    public List<string> UrgentKeywords { get; set; } = new List<string>();

        //    [JsonProperty("suspicious_urls")]
        //    public List<string> SuspiciousUrls { get; set; } = new List<string>();

        //}
    }

    public class ClassificationResponse
    {
        [JsonProperty("results")]
        public List<ClassificationResult>? Results { get; set; }
    }

}
