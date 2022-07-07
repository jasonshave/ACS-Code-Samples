using Azure.Communication.CallingServer;
using Azure.Communication.JobRouter;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using InboundCalling.Sample;
using JasonShave.Azure.Communication.Service.EventHandler;
using JasonShave.Azure.Communication.Service.EventHandler.CallingServer;
using JasonShave.Azure.Communication.Service.EventHandler.JobRouter;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEventHandlerServices(option => option.PropertyNameCaseInsensitive = true)
    .AddCallingServerEventHandling()
    .AddJobRouterEventHandling();

builder.Services.AddSingleton(new CallingServerClient(
    new Uri(builder.Configuration["CallingServerClientSettings:PmaEndpoint"]), 
    builder.Configuration["CallingServerClientSettings:ConnectionString"]));

builder.Services.AddSingleton(new RouterClient(builder.Configuration["RouterClient:ConnectionString"]));

builder.Services.AddSingleton<IRepository<CallConnection>, MemoryRepository<CallConnection>>();
builder.Services.AddSingleton<IRepository<RouterJob>, MemoryRepository<RouterJob>>();

builder.Services.AddHostedService<MidCallEventHandler>();
builder.Services.AddHostedService<PreCallEventHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.ProvisionJobRouterAsync().Wait();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/api/incomingCall", (
    [FromBody] EventGridEvent[] eventGridEvents,
    [FromServices] IEventPublisher<CallingServer> publisher,
    [FromServices] CallingServerClient callingServerClient) => 
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        publisher.Publish(eventGridEvent.Data, eventGridEvent.EventType, Guid.NewGuid().ToString());
    }

    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

app.MapPost("/api/jobRouter", (
    [FromBody] EventGridEvent[] eventGridEvents,
    [FromServices] IEventPublisher<JobRouter> publisher) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        publisher.Publish(eventGridEvent.Data, eventGridEvent.EventType);
    }

    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

app.MapPost("/api/calls/{contextId}", (
    [FromBody] CloudEvent[] cloudEvent,
    [FromRoute] string contextId,
    [FromServices] IEventPublisher<CallingServer> publisher) =>
{
    foreach (var @event in cloudEvent)
    {
        publisher.Publish(@event.Data, @event.Type, contextId);
    }

    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

app.Run();
