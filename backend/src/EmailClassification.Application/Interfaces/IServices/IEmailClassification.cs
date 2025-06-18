using EmailClassification.Application.DTOs.Classification;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Application.Interfaces.IServices
{
    public interface IClassificationService
    {
        Task<string?> IdentifyLabel(string emailContent);
    }
}
