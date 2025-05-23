
using EmailClassification.Application;
using EmailClassification.Application.Interfaces.IServices;
using EmailClassification.Infrastructure;
using EmailClassification.Infrastructure.Middlewares;

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
            builder.Services.AddSwaggerGen();
            builder.Services.AddRouting(options =>
            {
                options.LowercaseUrls = true;
            });
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddApplication(builder.Configuration);
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
            app.UseHttpsRedirection();
            app.UseMiddleware<GuestIdMiddleware>();
            app.UseAuthorization();


            app.MapControllers();
            await app.RunAsync();
        }
    }
}
