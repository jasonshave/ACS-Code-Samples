# ACS Code Samples

This repo contains a collection of code samples for Azure Communication Services to help developers construct business applications using primitives such as ACS Calling and Job Router.

>NOTE: These samples are currently a work in progress...

## InboundCall.JobRouter.CallTransfer.Sample

This [sample](/InboundCall.JobRouter.CallTransfer.Sample/) orchestrates the following:

1. An Event Grid `IncomingCall` event is received by an HTTP endpoint then published/dispatched and sent to the `PreCallEventHandler` where the `CallingServer` SDK answers it while providing a callback URI.
2. An event is raised by ACS and sent to the callback URI above. The HTTP endpoint publishes the event which is dispatched to the `MidCallEventHandler` where it subscribes and invokes a method to create a Job in Job Router.
3. Job Router raises a Job Offer event through Event Grid which is sent to the HTTP endpoint. It is published and dispatched to the same `MidCallEventHandler` where a Worker accepts the offer.
4. ACS Job Router sends an Offer Accepted event to the HTTP endpoint using Event Grid where it's published/dispatched to the same `MidCallEventHandler`. At this point we know the Worker has been assigned to the Job so we can transfer the call to the Worker using the CallingServer SDK.

>NOTE: There is a [setup 'script'](/InboundCall.JobRouter.CallTransfer.Sample/src/ApplicationBuilderExtensions.cs) as part of `ApplicationBuilderExtensions.cs` called `app.ProvisionJobRouterAsync().Wait();` which creates a distribution policy, queue, and registers a worker for this flow. It will also clean out any previously assigned jobs from a previous run.
