using GreenPipes;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using System.Reflection;
using System.Text.Json.Serialization;
using Trade.Common.Identity;
using Trade.Common.MassTransit;
using Trade.Common.MongoDB;
using Trade.Common.Settings;
using Trade.Exchanger.Service.Entities;
using Trade.Exchanger.Service.Exceptions;
using Trade.Exchanger.Service.Settings;
using Trade.Exchanger.Service.SignalR;
using Trade.Exchanger.Service.StateMachines;
using Trade.Identity.Contracts;
using Trade.Inventory.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
const string AllowedOriginSettings = "AllowedOrigin";

builder.Services   
    .AddMongo()
    .AddMongoRepository<CatalogItem>("catalogItems")
    .AddMongoRepository<InventoryItem>("inventoryItems")
    .AddMongoRepository<ApplicationUser>("users") 
    .AddJwtBearerAuthentication();
AddMassTransit(builder);

builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
})
.AddJsonOptions(options => options.JsonSerializerOptions.DefaultIgnoreCondition =
    JsonIgnoreCondition.WhenWritingNull);

builder.Services.AddSingleton<IUserIdProvider, UserIdProvider>()
        .AddSingleton<MessageHub>()
        .AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    var test = builder.Configuration[AllowedOriginSettings];

    app.UseCors(corsBuilder =>
    {
        corsBuilder.WithOrigins(builder.Configuration[AllowedOriginSettings])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MessageHub>("/messageHub");

app.Run();


 void AddMassTransit(WebApplicationBuilder builder)
{
    builder.Services.AddMassTransit(configure =>
    {
        configure.UsingTradeEconomyRabbitMq(retryConfiguration =>
        {
            retryConfiguration.Interval(3, TimeSpan.FromSeconds(5));
            retryConfiguration.Ignore(typeof(UnknownItemException));
        });

        configure.AddConsumers(Assembly.GetEntryAssembly());
        configure.AddSagaStateMachine<PurchaseStateMachine, PurchaseState>(sagaConfigurator =>
        {
            sagaConfigurator.UseInMemoryOutbox();
        })
            .MongoDbRepository(r =>
            {
                var serviceSettings = builder.Configuration.GetSection(nameof(ServiceSettings))
                                                           .Get<ServiceSettings>();
                var mongoDbSettings = builder.Configuration.GetSection(nameof(MongoDbSettings))
                                                           .Get<MongoDbSettings>();

                r.Connection = mongoDbSettings.ConnectionString;
                r.DatabaseName = serviceSettings.ServiceName;
            });
    });

    var queueSettings = builder.Configuration.GetSection(nameof(QueueSettings))
                                                            .Get<QueueSettings>();

    EndpointConvention.Map<GrantItems>(new Uri(queueSettings.GrantItemsQueueAddress));
    EndpointConvention.Map<DebitGil>(new Uri(queueSettings.DebitGilQueueAddress));
    EndpointConvention.Map<SubtractItems>(new Uri(queueSettings.SubtractItemsQueueAddress));

    builder.Services.AddMassTransitHostedService();
    builder.Services.AddGenericRequestClient();
}
