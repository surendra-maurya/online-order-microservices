using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProductService.Application.Interfaces;
using ProductService.Application.Services;
using ProductService.Application.Validators;
using ProductService.Infrastructure.Data;
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
        .Enrich.WithProperty("ServiceName", "ProductService")
        .WriteTo.Console()
        .WriteTo.Seq("http://seq:80");
});

//builder.Services.AddHealthChecks()
//    .AddSqlServer(
//        builder.Configuration.GetConnectionString("ProductDb"));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ProductDbContext>();


builder.Services.AddControllers();

// Modern FluentValidation registration
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();
builder.Services.AddFluentValidationAutoValidation();

// Database
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ProductDb")));

// Application Services
builder.Services.AddScoped<IProductService, ProductService.Application.Services.ProductService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Global Exception Handling
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { Message = "Unexpected error occurred" });
    });
});

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
