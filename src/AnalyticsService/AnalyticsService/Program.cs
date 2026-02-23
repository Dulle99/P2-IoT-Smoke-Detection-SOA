using Grpc.Net.Client;
using AnalyticsService.Protos;
var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddHostedService<AnalyticsService.Services.MqttSubscriberService>();
builder.Services.Configure<AnalyticsService.Services.InfluxOptions>(builder.Configuration.GetSection("Influx"));
builder.Services.AddSingleton<AnalyticsService.Services.IEventWriter, AnalyticsService.Services.InfluxEventWriter>();
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
builder.Services.AddSingleton(sp =>
{
    var channel = GrpcChannel.ForAddress("http://notification-service:3000");
   return new NotificationService.NotificationServiceClient(channel);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
