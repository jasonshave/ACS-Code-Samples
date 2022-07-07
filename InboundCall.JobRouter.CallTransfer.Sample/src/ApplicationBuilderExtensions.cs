using Azure.Communication.JobRouter;

namespace InboundCall.JobRouter.CallTransfer.Sample;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseEventGridWebHookAutoValidation(this IApplicationBuilder app)
    {
        // experimental
        app.UseMiddleware<EventGridValidationMiddleware>();
        return app;
    }

    public static async Task<IApplicationBuilder> ProvisionJobRouterAsync(this IApplicationBuilder app)
    {
        var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<RouterClient>();

        var routerClient = app.ApplicationServices.GetService<RouterClient>();
        if (routerClient is null) throw new ArgumentNullException(nameof(routerClient));

        // set up distribution policy
        DistributionPolicy distributionPolicy = await routerClient.CreateDistributionPolicyAsync("AlaskaAir_30s_RoundRobin", 30, new RoundRobinMode());
        logger.LogInformation($"Distribution policy {distributionPolicy.Id} created.");

        // set up VIP queue
        JobQueue queue = await routerClient.CreateQueueAsync("Alaska_VIP", distributionPolicy.Id);

        // clean up from previous runs
        var allActiveJobs = routerClient.GetJobsAsync(new GetJobsOptions() { Status = JobStateSelector.Assigned });
        await foreach (var jobPage in allActiveJobs.AsPages(pageSizeHint: 100))
        {
            foreach (var job in jobPage.Values)
            {
                var assignmentId = job.Assignments.First().Value.Id;
                await routerClient.CompleteJobAsync(job.Id, assignmentId);
                await Task.Delay(5000);
                await routerClient.CloseJobAsync(job.Id, assignmentId);
            }
        }

        // register a Worker
        RouterWorker worker = await routerClient.CreateWorkerAsync("Frank", 100, new CreateWorkerOptions()
        {
            AvailableForOffers = true,
            ChannelConfigurations = new Dictionary<string, ChannelConfiguration>()
            {
                { "Voice", new ChannelConfiguration(100) }
            },
            QueueIds = new Dictionary<string, QueueAssignment>()
            {
                {"Alaska_VIP", new QueueAssignment()}
            }
        });
        logger.LogInformation("Worker created");

        return app;
    }
}