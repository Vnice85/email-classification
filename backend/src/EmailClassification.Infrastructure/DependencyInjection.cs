using EmailClassification.Application.Interfaces;
using EmailClassification.Application.Interfaces.Background;
using EmailClassification.Application.Interfaces.IServices;
using EmailClassification.Infrastructure.Implement;
using EmailClassification.Infrastructure.Persistence;
using EmailClassification.Infrastructure.Service;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;


namespace EmailClassification.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<EmaildbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
            services.AddScoped<IClassificationService, ClassificationService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IGuestContext, GuestContext>();
            services.AddScoped<IEmailSearchService, EmailSearchService>();
            services.AddScoped<IBackgroundService, BackgroundService>();

            // register for elasticsearch
            services.AddSingleton<IElasticClient>(sp =>
            {
                var Url = configuration["Elastic:Url"]!;
                var Index = configuration["Elastic:Index"];
                //var Username = configuration["Elastic:Username"];
                //var Password = configuration["Elastic:Password"];
                var settings = new ConnectionSettings(new Uri(Url))
                .DefaultIndex(Index)
                /*.BasicAuthentication(Username, Password)*/;
                var client = new ElasticClient(settings);
                return client;
            });

            // register for hangfire 
            services.AddHangfire(config =>
            {
                config.UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection"));
                });
            });
            services.AddHangfireServer();
            services.AddHostedService<BackgroundJobInitializer>();


            return services;

        }
    }
}
