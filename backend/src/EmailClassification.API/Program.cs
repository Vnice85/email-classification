
using EmailClassification.API.Hubs;
using EmailClassification.API.Services;
using EmailClassification.Application;
using EmailClassification.Application.Interfaces.INotification;
using EmailClassification.Application.Interfaces.IServices;
using EmailClassification.Infrastructure;
using EmailClassification.Infrastructure.Middlewares;
using Hangfire;
using Microsoft.OpenApi.Models;

namespace EmailClassification.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddRouting(options =>
            {
                options.LowercaseUrls = true;
            });
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddApplication(builder.Configuration);
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddServer(new OpenApiServer
                {
                    Url = "https://localhost:44366",
                    Description = "Local Dev"
                });
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "EmailClassification API", Version = "v1" });
                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Nhập JWT token (không cần prefix Bearer)",
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                };
                options.AddSecurityDefinition("Bearer", securityScheme);
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                        {
                            securityScheme,
                            Array.Empty<string>()
                        }
                });
            });
            builder.Services.AddSignalR();
            builder.Services.AddScoped<INotificationSender, SignalRNotificationSender>();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            using (var scope = app.Services.CreateScope())
            {
                var elasticSearchService = scope.ServiceProvider.GetRequiredService<IEmailSearchService>();
                await elasticSearchService.CreateIndexAsync();
            }

            app.UseCors("AllowAll");
            app.UseHttpsRedirection();
            app.UseMiddleware<GuestIdMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapHub<EmailHub>("/emailhub");
            app.UseHangfireDashboard();


            app.MapControllers();
            await app.RunAsync();
        }
    }
}
