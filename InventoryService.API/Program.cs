using InventoryService.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using Serilog;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
    config
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName()
        .Enrich.WithProperty("ServiceName", "InventoryService")
        .WriteTo.Console()
        .WriteTo.Seq("http://seq:80");
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<InventoryDbContext>()
    .AddRabbitMQ(sp =>
    {
        var factory = new ConnectionFactory
        {
            HostName = "rabbitmq"
        };

        return factory.CreateConnectionAsync();
    });

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("InventoryDb")));


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<OrderCreatedConsumer>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "OnlineOrder.Auth",
            ValidAudience = "OnlineOrder.Clients",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("VerySecretKeyForOnlineOrderSystem"))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");

app.Run();

