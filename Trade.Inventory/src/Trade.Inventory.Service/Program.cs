using Trade.Common.Settings;
using Trade.Inventory.Service.Entities;
using Trade.Inventory.Service.Clients;
using Polly;
using Polly.Timeout;
using Trade.Common.MongoDB;
using Trade.Common.MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});

// mongo db options 
var serviceSettings = builder.Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();

builder.Services.AddMongo()
                .AddMongoRepository<InventoryItem>("inventoryItems")
                .AddMongoRepository<CatalogItem>("catalogItems")
                .AddMassTransitWithRabbitMq();

AddCatalogClient(builder);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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


static void AddCatalogClient(WebApplicationBuilder builder)
{
    builder.Services.AddHttpClient<CatalogClient>(client =>
    {
        client.BaseAddress = new Uri("https://localhost:5001");
    })
    .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.Or<TimeoutRejectedException>().WaitAndRetryAsync(
        5,
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                        + TimeSpan.FromMilliseconds(new Random().Next(0, 1000)),
        onRetry: (outcome, timespan, retryAttempt) =>
        {
            var serviceProvider = builder.Services.BuildServiceProvider();
            serviceProvider.GetService<ILogger<CatalogClient>>()?
                .LogWarning($"Delaying for {timespan.TotalSeconds} seconds, then making retry {retryAttempt}");
        }
    ))
    .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.Or<TimeoutRejectedException>().CircuitBreakerAsync(
        3,
        TimeSpan.FromSeconds(15),
        onBreak: (outcome, timespan) =>
        {
            var serviceProvider = builder.Services.BuildServiceProvider();
            serviceProvider.GetService<ILogger<CatalogClient>>()?
                .LogWarning($"Opening the circuit for {timespan.TotalSeconds} seconds...");
        },
        onReset: () =>
        {
            var serviceProvider = builder.Services.BuildServiceProvider();
            serviceProvider.GetService<ILogger<CatalogClient>>()?
                .LogWarning($"Closing the circuit...");
        }
    ))
    .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));
}