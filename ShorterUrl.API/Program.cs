using ShorterUrl.Core;
using ShorterUrl.Core.Datasource;
using Microsoft.EntityFrameworkCore;
internal class Program
{
    public IConfiguration Configuration { get; }
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        var services = builder.Services;

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        services.AddDbContext<UrlShortenerContext>(options =>
                   options.UseCosmos(builder.Configuration["CosmosDb:AccountEndpoint"],
                       builder.Configuration["CosmosDb:AccountKey"],
                       builder.Configuration["CosmosDb:DatabaseName"]));

        services.AddSingleton<StorageService>(provider =>
        {
            var context = provider.GetRequiredService<UrlShortenerContext>();
            var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
            var sqlConnectionString = builder.Configuration["SqlServer:ConnectionString"];
            return new StorageService(context, redisConnectionString, sqlConnectionString);
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}