using EmailClassification.Application.DTOs;
using EmailClassification.Application.DTOs.Email;
using EmailClassification.Application.DTOs.Guest;
using EmailClassification.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Application.Interfaces.IServices
{
    public interface IEmailSearchService
    {
        Task CreateIndexAsync();
        Task SingleIndexAsync(Email email);
        Task BulkIndexAsync(List<Email> docs);
        Task<bool> DeleteAsync(string id);
        Task<List<EmailHeaderDTO>> SearchAsync(string userId, ElasticFilter filter);
    }
}
