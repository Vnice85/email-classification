using EmailClassification.Application.Interfaces;
using EmailClassification.Application.Interfaces.Background;
using EmailClassification.Application.Interfaces.IServices;
using EmailClassification.Infrastructure.Implement;
using EmailClassification.Infrastructure.Persistence;
using EmailClassification.Infrastructure.Service;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Nest;
using System.Text;


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
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddHttpClient();
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
            services.AddCors(options => {
                options.AddPolicy("AllowAll", policy => {
                    policy.WithOrigins("http://localhost:3000")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });


            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddCookie()
                .AddGoogle(options =>
                {
                    options.ClientId = configuration["Authentication:Google:ClientId"]!;
                    options.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;
                    options.CallbackPath = configuration["Authentication:Google:CallbackPath"]!;
                    options.Scope.Add("https://mail.google.com/");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    options.AccessType = "offline"; 
                    options.SaveTokens = true;
                    options.ClaimActions.MapJsonKey("urn:google:picture", "picture"); // get avatar of gmail
                    //options.Events.OnRedirectToAuthorizationEndpoint = context =>
                    //{
                    //    context.Response.Redirect(context.RedirectUri + "&prompt=consent");
                    //    return Task.CompletedTask;
                    //};
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = configuration["Authentication:Jwt:Issuer"],
                        ValidAudience = configuration["Authentication:Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Authentication:Jwt:Key"]!))
                    };
                });

            return services;

        }
    }
}
