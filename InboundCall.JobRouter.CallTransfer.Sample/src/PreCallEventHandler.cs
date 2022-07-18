using Azure.Communication.CallingServer;
using JasonShave.Azure.Communication.Service.CallingServer.Sdk.Contracts.V2022_11_1_preview.Events;
using JasonShave.Azure.Communication.Service.EventHandler.CallingServer;

namespace InboundCall.JobRouter.CallTransfer.Sample;

public class PreCallEventHandler : BackgroundService
{
    private readonly IRepository<CallConnection> _activeCallsRepository;
    private readonly CallingServerClient _client;
    private readonly ICallingServerEventSubscriber _eventSubscriber;
    private readonly ILogger<PreCallEventHandler> _logger;

    private readonly string _callbackUri;

    public PreCallEventHandler(
        IRepository<CallConnection> activeCallsRepository,
        CallingServerClient client,
        ICallingServerEventSubscriber eventSubscriber,
        ILogger<PreCallEventHandler> logger,
        IConfiguration configuration)
    {
        _activeCallsRepository = activeCallsRepository;
        _client = client;
        _eventSubscriber = eventSubscriber;
        _logger = logger;

        _callbackUri = configuration["CallbackUri"];
    }

    public override void Dispose() => _eventSubscriber.OnIncomingCall -= HandleIncomingCall;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _eventSubscriber.OnIncomingCall += HandleIncomingCall;

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async ValueTask HandleIncomingCall(IncomingCall @event, string? contextId)
    {
        try
        {
            // don't re-answer same call (redirect/forward scenarios)
            var existingCall = await _activeCallsRepository.Get(@event.CorrelationId);
            if (existingCall is null)
            {
                var callsEndpoint = new Uri(_callbackUri + @event.CorrelationId);
                CallConnection callConnection = await _client.AnswerCallAsync(@event.IncomingCallContext, callsEndpoint);
                await _activeCallsRepository.Save(callConnection, @event.CorrelationId);

                _logger.LogInformation($"Answered call from: {@event.From.RawId} to: {@event.To.RawId}");
            }

            // ignore due to re-answer issue
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message, e);
        }
    }
}