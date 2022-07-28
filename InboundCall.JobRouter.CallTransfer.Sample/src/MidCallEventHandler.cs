using Azure.Communication;
using Azure.Communication.CallingServer;
using Azure.Communication.JobRouter;
using JasonShave.Azure.Communication.Service.CallingServer.Sdk.Contracts.V2022_11_1_preview.Events;
using JasonShave.Azure.Communication.Service.EventHandler.CallingServer;
using JasonShave.Azure.Communication.Service.EventHandler.JobRouter;
using JasonShave.Azure.Communication.Service.JobRouter.Sdk.Contracts.V2021_10_20_preview.Events;

namespace InboundCall.JobRouter.CallTransfer.Sample;

public class MidCallEventHandler : BackgroundService
{
    private readonly ICallingServerEventSubscriber _callingServerEventSubscriber;
    private readonly IJobRouterEventSubscriber _jobRouterEventSubscriber;
    private readonly IRepository<CallConnection> _activeCallsRepository;
    private readonly IRepository<RouterJob> _activeJobsRepository;
    private readonly CallingServerClient _callingServerClient;
    private readonly RouterClient _routerClient;
    private readonly ILogger<MidCallEventHandler> _logger;

    private readonly string _transferMri;
    private readonly string _addParticipantMri;

    public MidCallEventHandler(
        IConfiguration configuration,
        ICallingServerEventSubscriber callingServerEventSubscriber,
        IJobRouterEventSubscriber jobRouterEventSubscriber,
        IRepository<CallConnection> activeCallsRepository,
        IRepository<RouterJob> activeJobsRepository,
        CallingServerClient callingServerClient,
        RouterClient routerClient,

        ILogger<MidCallEventHandler> logger)
    {
        _callingServerEventSubscriber = callingServerEventSubscriber;
        _jobRouterEventSubscriber = jobRouterEventSubscriber;
        _activeCallsRepository = activeCallsRepository;
        _activeJobsRepository = activeJobsRepository;
        _callingServerClient = callingServerClient;
        _routerClient = routerClient;
        _logger = logger;

        _transferMri = configuration["ACS:TransferToMri"];
        _addParticipantMri = configuration["ACS:AddParticipantMri"];
    }

    public override void Dispose()
    {
        // unsubscribe to events
        _callingServerEventSubscriber.OnCallConnected -= HandleCallConnected;
        _callingServerEventSubscriber.OnCallDisconnected -= HandleCallDisconnected;

        _jobRouterEventSubscriber.OnJobCompleted -= HandleJobCompleted;
        _jobRouterEventSubscriber.OnJobClosed -= HandleJobClosed;
        _jobRouterEventSubscriber.OnWorkerOfferIssued -= HandleOfferIssued;
        _jobRouterEventSubscriber.OnWorkerOfferAccepted -= HandleOfferAccepted;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // subscribe to events
        _callingServerEventSubscriber.OnCallConnected += HandleCallConnected;
        _callingServerEventSubscriber.OnCallDisconnected += HandleCallDisconnected;
        
        _jobRouterEventSubscriber.OnJobCompleted += HandleJobCompleted;
        _jobRouterEventSubscriber.OnJobClosed += HandleJobClosed;
        _jobRouterEventSubscriber.OnWorkerOfferIssued += HandleOfferIssued;
        _jobRouterEventSubscriber.OnWorkerOfferAccepted += HandleOfferAccepted;

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async ValueTask HandleJobClosed(RouterJobClosed routerJobClosed, string? contextId)
    {
        _logger.LogInformation($"Job closed: {routerJobClosed.JobId}");
        await _activeJobsRepository.Remove(routerJobClosed.JobId);
    }

    private async ValueTask HandleJobCompleted(RouterJobCompleted routerJobCompleted, string? contextId)
    {
        await _routerClient.CloseJobAsync(routerJobCompleted.JobId, routerJobCompleted.AssignmentId);
        _logger.LogInformation($"Job completed: {routerJobCompleted.JobId}");
    }

    private async ValueTask HandleCallConnected(CallConnected callConnected, string? contextId)
    {
        var callConnection = _callingServerClient.GetCallConnection(callConnected.CallConnectionId);

        await callConnection
            .GetCallMedia()
            .PlayToAllAsync(new FileSource(new Uri("https://acstestapp1.azurewebsites.net/audio/bot-hold-music-2.wav")));

        RouterJob routerJob = await _routerClient.CreateJobAsync(contextId, "Voice", "Alaska_VIP");
        await _activeJobsRepository.Save(routerJob, contextId);

        _logger.LogInformation($"Call state: Connected | Call connection ID: {callConnected.CallConnectionId} | Context ID: {contextId}");
    }

    private async ValueTask HandleCallDisconnected(CallDisconnected callDisconnected, string? contextId)
    {
        await _activeCallsRepository.Remove(contextId);

        // complete the job since the call has been disconnected
        var existingJob = await _activeJobsRepository.Get(contextId);
        if (existingJob is not null)
        {
            // job exists in local repo; complete it
            await _routerClient.CompleteJobAsync(existingJob.Id, existingJob.Assignments.Keys.FirstOrDefault());
        }

        _logger.LogInformation($"Call state: Disconnected | Call connection ID: {callDisconnected.CallConnectionId} | Context ID: {contextId}");
    }
    
    private async ValueTask HandleOfferIssued(RouterWorkerOfferIssued offerIssued, string? contextId)
    {
        //check if call is still active
        var callConnection = await _activeCallsRepository.Get(offerIssued.JobId);

        if (callConnection is not null)
        {
            // Frank accepts the job
            await _routerClient.AcceptJobOfferAsync("Frank", offerIssued.OfferId);
            _logger.LogInformation($"Accepting job {offerIssued.JobId}");
        }
        else
        {
            await _routerClient.CancelJobAsync(offerIssued.JobId);
            _logger.LogInformation($"Cancelled stale call {offerIssued.JobId}");
        }
    }

    private async ValueTask HandleOfferAccepted(RouterWorkerOfferAccepted offerAccepted, string? contextId)
    {
        // Transfer active call to Frank
        var callConnection = await _activeCallsRepository.Get(offerAccepted.JobId);
        if (callConnection is not null)
        {
            await callConnection.AddParticipantsAsync(new List<CommunicationIdentifier>
            {
                new CommunicationUserIdentifier(_addParticipantMri)
            });
        }
    }
}