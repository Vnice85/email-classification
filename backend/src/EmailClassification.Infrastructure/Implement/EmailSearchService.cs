using EmailClassification.Application.DTOs;
using EmailClassification.Application.DTOs.Email;
using EmailClassification.Application.DTOs.Guest;
using EmailClassification.Application.Helpers;
using EmailClassification.Application.Interfaces.IServices;
using EmailClassification.Domain.Enum;
using EmailClassification.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Infrastructure.Implement
{
    public class EmailSearchService : IEmailSearchService
    {
        private readonly IElasticClient _client;
        private readonly string index;
        private readonly ILogger<EmailSearchService> _logger;

        public EmailSearchService(IElasticClient client, 
                                  IConfiguration configuration,
                                  ILogger<EmailSearchService> logger)
        {
            _client = client;
            this.index = configuration["Elastic:Index"]!;
            _logger = logger;
        }

        public async Task SingleIndexAsync(Email email)
        {
            var indexResponse = await _client.IndexDocumentAsync(email);
            if (!indexResponse.IsValid)
            {
                _logger.LogError("Failed to index document: " + indexResponse.DebugInformation);
              throw new Exception("Failed to index document: " + indexResponse.DebugInformation);
            }
        }

        public async Task CreateIndexAsync()
        {
            var indexExistsResponse = await _client.Indices.ExistsAsync(index);
            if (indexExistsResponse.Exists)
                return;
            // lowercase, asciifolding, stop for better vietnamese search
            // can use plugin instead, but i won't, because i'm a chill guy:>>>>
            var createIndexResponse = await _client.Indices.CreateAsync("emails", c => c
                .Settings(s => s
                    .Analysis(a => a
                        .TokenFilters(tf => tf
                            .Stop("vietnamese_stop", stop => stop
                                .StopWords("_vietnamese_")
                            )
                        )
                        .Analyzers(an => an
                            .Custom("vietnamese_analyzer", ca => ca
                                .Tokenizer("standard")
                                .Filters("lowercase", "asciifolding", "vietnamese_stop")
                            )
                        )
                    )
                )
                .Map(m => m
                    .Properties(p => p
                        .Text(t => t.Name("subject").Analyzer("vietnamese_analyzer"))
                        .Text(t => t.Name("plainText").Analyzer("vietnamese_analyzer"))
                    )
                ));
            if (!createIndexResponse.IsValid)
            {
                _logger.LogError("Failed to create index: " + createIndexResponse.DebugInformation);
                throw new Exception("Failed to create index: " + createIndexResponse.DebugInformation);

            }
        }

        public async Task BulkIndexAsync(List<Email> docs)
        {
            var response = await _client.BulkAsync(b => b.Index("emails").IndexMany(docs));
            if (response.Errors)
            {
                var failedItems = response.ItemsWithErrors
                    .Select(e => $"ID: {e.Id}, Error: {e.Error?.Reason}")
                    .ToList();

                _logger.LogError("Bulk indexing failed:\n" + string.Join("\n", failedItems));
                throw new Exception("Bulk indexing failed:\n" + string.Join("\n", failedItems));

            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var response = await _client.DeleteAsync<Email>(id, d => d.Index(index));
            return response.IsValid && response.Result == Result.Deleted;
        }

        public async Task<List<EmailSearchHeaderDTO>> SearchAsync(string userId, ElasticFilter filter)
        {
            var response = await _client.SearchAsync<Email>(s => s
                .Index(index)
                .From((filter.PageIndex - 1) * filter.PageSize)
                .Size(filter.PageSize)
                .Query(q => q
                    .Bool(b => b
                        .Must(mu => mu
                           .MultiMatch(mm => mm
                                .Fields(f => f
                                    .Field("subject")
                                    .Field("plainText")
                                )
                                .Query(filter.KeyWord)
                                .Fuzziness(Fuzziness.EditDistance(1))
                                .Operator(Operator.And)
                           )
                        )
                        .Filter(f => f
                            .Term(t => t
                                .Field("userId.keyword")
                                .Value(userId)
                            )
                        )
                    )
                )
            );
            return response.Documents.Select(email => new EmailSearchHeaderDTO
            {
                EmailId = email.EmailId,
                FromAddress = email.FromAddress,
                ToAddress = email.ToAddress,
                Snippet = email.Snippet,
                ReceivedDate = DateTimeHelper.FormatToVietnamTime(email.ReceivedDate),
                SentDate = DateTimeHelper.FormatToVietnamTime(email.SentDate),
                Subject = email.Subject,
                DirectionName = ((DirectionStatus)email.DirectionId).ToString()
            }).OrderByDescending(e=>e.EmailId).ToList();
        }

        public Task DeleteByUserIdAsync(string userId)
        {
           var response = _client.DeleteByQueryAsync<Email>(d => d
                .Index(index)
                .Query(q => q
                    .Term(t => t
                        .Field("userId.keyword")
                        .Value(userId)
                    )
                )
            );
            if (!response.Result.IsValid)
            {
                _logger.LogError("Failed to delete by userId: " + response.Result.DebugInformation);
                throw new Exception("Failed to delete by userId: " + response.Result.DebugInformation);
            }
            return response;
        }

        //public async Task<bool> UpdateAsync(Email email)
        //{
        //    var response = await _client.UpdateAsync<Email, object>(email.EmailId, u => u
        //        .Index("emails")
        //        .Doc(email)
        //    );

        //    return response.IsValidResponse && response.Result == Result.Updated;
        //}
    }
}
