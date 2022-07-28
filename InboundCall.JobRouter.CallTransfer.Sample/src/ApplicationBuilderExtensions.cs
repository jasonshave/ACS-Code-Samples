using Azure.Communication.JobRouter;
using Azure.Communication.JobRouter.Models;

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

        var routerAdminClient = app.ApplicationServices.GetService<RouterAdministrationClient>();
        if (routerAdminClient is null) throw new ArgumentNullException(nameof(routerAdminClient));

        // set up distribution policy
        var distributionPolicy = await routerAdminClient.CreateDistributionPolicyAsync(
            new CreateDistributionPolicyOptions("AlaskaAir_30s_RoundRobin", TimeSpan.FromSeconds(30), new RoundRobinMode()));
        logger.LogInformation($"Distribution policy {distributionPolicy.Value.Id} created.");

        // set up VIP queue
        var queue = await routerAdminClient.CreateQueueAsync(new CreateQueueOptions("Alaska_VIP", distributionPolicy.Value.Id));

        // clean up from previous runs
        var allActiveJobs = routerClient.GetJobsAsync(new GetJobsOptions() { Status = JobStateSelector.Assigned });
        await foreach (var jobPage in allActiveJobs.AsPages(pageSizeHint: 100))
        {
            foreach (var job in jobPage.Values)
            {
                var assignmentId = job.RouterJob.Assignments.First().Value.Id;
                await routerClient.CompleteJobAsync(new CompleteJobOptions(job.RouterJob.Id, assignmentId));
                await Task.Delay(8000);
                await routerClient.CloseJobAsync(new CloseJobOptions(job.RouterJob.Id, assignmentId));
            }
        }

        // register a Worker
        RouterWorker worker = await routerClient.CreateWorkerAsync(new CreateWorkerOptions("Frank", 100)
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