using Trade.Common.Settings;
using Trade.Inventory.Service.Entities;
using Trade.Inventory.Service.Clients;
using Polly;
using Polly.Timeout;
using Trade.Common.MongoDB;
using Trade.Common.MassTransit;
using Trade.Common.Identity;
using GreenPipes;
using Trade.Inventory.Service.Exceptions;
using Microsoft.AspNetCore.Builder;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Net.Http;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
const string AllowedOriginSettings = "AllowedOrigin";

builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});

// mongo db options 
var serviceSettings = builder.Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();

builder.Services.AddMongo()
                .AddMongoRepository<InventoryItem>("inventoryItems")
                .AddMongoRepository<CatalogItem>("catalogItems")
                .AddMassTransitWithRabbitMq(retryConfigurator =>
                {
                    retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
                    retryConfigurator.Ignore(typeof(UnknownItemException));
                })
                .AddJwtBearerAuthentication();

AddCatalogClient(builder.Services);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors(cpBuilder =>
    {
        cpBuilder.WithOrigins(builder.Configuration[AllowedOriginSettings])
            .AllowAnyHeader()
            .AllowAnyHeader();
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


void AddCatalogClient(IServiceCollection services)
{
    services.AddHttpClient<CatalogClient>(client =>
    {
        client.BaseAddress = new Uri("https://localhost:5001");
    })
    .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.Or<TimeoutRejectedException>().WaitAndRetryAsync(
        5,
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                        + TimeSpan.FromMilliseconds(new Random().Next(0, 1000)),
        onRetry: (outcome, timespan, retryAttempt) =>
        {
            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetService<ILogger<CatalogClient>>()?
                .LogWarning($"Delaying for {timespan.TotalSeconds} seconds, then making retry {retryAttempt}");
        }
    ))
    .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.Or<TimeoutRejectedException>().CircuitBreakerAsync(
        3,
        TimeSpan.FromSeconds(15),
        onBreak: (outcome, timespan) =>
        {
            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetService<ILogger<CatalogClient>>()?
                .LogWarning($"Opening the circuit for {timespan.TotalSeconds} seconds...");
        },
        onReset: () =>
        {
            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetService<ILogger<CatalogClient>>()?
                .LogWarning($"Closing the circuit...");
        }
    ))
    .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));
}