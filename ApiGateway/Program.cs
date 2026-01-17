using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using Serilog.Context;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
    config
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName()
        .Enrich.WithProperty("ServiceName", "ApiGateService")
        .WriteTo.Console()
        .WriteTo.Seq("http://seq:80");
});


builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("ocelot.json", false, true);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("JwtBearer", options =>
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

builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.Use(async (context, next) =>
{
    var correlationId = Guid.NewGuid().ToString();
    context.Items["CorrelationId"] = correlationId;

    LogContext.PushProperty("CorrelationId", correlationId);
    context.Response.Headers.Add("X-Correlation-Id", correlationId);

    await next();
});


app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

await app.UseOcelot();

app.Run();
