using System.Text.Json;
using Azure.Communication.CallingServer;
using Azure.Communication.JobRouter;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using InboundCall.JobRouter.CallTransfer.Sample;
using JasonShave.Azure.Communication.Service.EventHandler;
using JasonShave.Azure.Communication.Service.EventHandler.CallingServer;
using JasonShave.Azure.Communication.Service.EventHandler.JobRouter;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEventHandlerServices(option => option.PropertyNameCaseInsensitive = true)
    .AddCallingServerEventHandling()
    .AddJobRouterEventHandling();

builder.Services.AddSingleton(new CallingServerClient(builder.Configuration["ACS:ConnectionString"]));
builder.Services.AddSingleton(new RouterClient(builder.Configuration["ACS:ConnectionString"]));

builder.Services.AddSingleton<IRepository<CallConnection>, MemoryRepository<CallConnection>>();
builder.Services.AddSingleton<IRepository<RouterJob>, MemoryRepository<RouterJob>>();

builder.Services.AddHostedService<MidCallEventHandler>();
builder.Services.AddHostedService<PreCallEventHandler>();
builder.Services.AddHostedService<TelemetryHandler>();

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
    [FromServices] IEventPublisher<Calling> publisher,
    [FromServices] CallingServerClient callingServerClient) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        publisher.Publish(eventGridEvent.Data.ToString(), eventGridEvent.EventType);
    }

    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

app.MapPost("/api/jobRouter", (
    [FromBody] EventGridEvent[] eventGridEvents,
    [FromServices] IEventPublisher<Router> publisher) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        publisher.Publish(eventGridEvent.Data.ToString(), eventGridEvent.EventType);
    }

    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

app.MapPost("/api/calls/{contextId}", async (
    HttpRequest request,
    [FromRoute] string contextId,
    [FromServices] IEventPublisher<Calling> publisher) =>
{
    // get body from HttpRequest
    string requestBody = await new StreamReader(request.Body).ReadToEndAsync();

    // deserialize into an array of CloudEvent types
    CloudEvent[] cloudEvents = JsonSerializer.Deserialize<CloudEvent[]>(requestBody);

    foreach (var @event in cloudEvents)
    {
        publisher.Publish(@event.Data.ToString(), @event.Type, contextId);
    }

    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

app.Run();
