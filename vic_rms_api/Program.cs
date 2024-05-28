
using Microsoft.EntityFrameworkCore;
using vic_rms_api.Models;
using vic_rms_api.Context;
using vic_rms_api.Services;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

// Đăng ký IHttpClientFactory
builder.Services.AddHttpClient();
// Add services to the container.
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<RatesService>();
builder.Services.AddScoped<HotelsService>();
builder.Services.AddScoped<IRates_GridService, Rates_GridService>();
builder.Services.AddHostedService<TimedHostedService>();
//builder.Services.AddHostedService<ScheduledTaskService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Build Configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                     .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                     .AddEnvironmentVariables();

// Use Configuration to get connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//

// Add services to the container.
//builder.Services.AddDbContext<vicweb_2022DbContext>(options =>
//options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
builder.Services.AddDbContext<vicweb_2022DbContext>(options =>
        options.UseMySql(connectionString,
            new MySqlServerVersion(new Version(8, 0, 21)),
            builder => builder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(120), null)));

// Cấu hình HttpClient với thời gian chờ tăng lên
builder.Services.AddHttpClient("MyHttpClient", client =>
{
    client.Timeout = TimeSpan.FromMinutes(3); // Tăng thời gian chờ lên 5 phút
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowAnyOrigin",
                      policy =>
                      {
                          policy.AllowAnyOrigin()
                               .AllowAnyHeader()
                               .AllowAnyMethod();
                      });
});

// ============================= Cấu hình Serilog=============================
//Log.Logger = new LoggerConfiguration()
//    .ReadFrom.Configuration(builder.Configuration)
//    .Enrich.FromLogContext()
//    .CreateLogger();

//builder.Host.UseSerilog();

//// Cấu hình logging
//builder.Logging.ClearProviders();
//builder.Logging.AddConsole();
//===================
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
