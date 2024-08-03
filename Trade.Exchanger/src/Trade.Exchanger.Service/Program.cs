using MassTransit;
using Trade.Common.Identity;
using Trade.Common.MassTransit;
using Trade.Common.MongoDB;
using Trade.Common.Settings;
using Trade.Exchanger.Service.StateMachines;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddMongo()
    .AddJwtBearerAuthentication();
AddMassTransit(builder);

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


static void AddMassTransit(WebApplicationBuilder builder)
{
    builder.Services.AddMassTransit(configure =>
    {
        configure.UsingTradeEconomyRabbitMq();
        configure.AddSagaStateMachine<PurchaseStateMachine, PurchaseState>()
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

    builder.Services.AddMassTransitHostedService();
}
