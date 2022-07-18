# Configuration

1. Obtain your ACS connection string.
2. Set your `ConnectionString` in your [.NET User Secrets store](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-6.0&tabs=windows), `appsettings.json`, or anywhere your `IConfiguration` provider can look for the `QueueClientSettings`. For example:

    ```json
    {
        "ACS" : {
            "ConnectionString": "[your_connection_string]",
        }
    }
    ```

3. Configure two Event Grid subscriptions, one to handle the IncomingCall event being sent to `/api/incomingCall` and one for Job Router events sent to `/api/jobRouter`. Locate the [Program.cs](/InboundCall.JobRouter.CallTransfer.Sample/src/Program.cs) for these routes as the sample uses .NET 6 minimal API's.

## Staying up to date with NuGet packages

Since this repository is a work in progress, consider updating the [EventHandler.CallingServer](https://www.nuget.org/packages/JasonShave.Azure.Communication.Service.EventHandler.CallingServer/) and [EventHandler.JobRouter](https://www.nuget.org/packages/JasonShave.Azure.Communication.Service.EventHandler.JobRouter/) NuGet packages frequently. Remember to check the "Include Prerelease" checkbox when searching for an update.

>NOTE: There is a [setup 'script'](/InboundCall.JobRouter.CallTransfer.Sample/src/ApplicationBuilderExtensions.cs) as part of `ApplicationBuilderExtensions.cs` called `app.ProvisionJobRouterAsync().Wait();` which creates a distribution policy, queue, and registers a worker for this flow. It will also clean out any previously assigned jobs from a previous run.