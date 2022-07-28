using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;

namespace InboundCall.JobRouter.CallTransfer.Sample;

// TODO: Seem to be a bug with this middleware as it always returns HTTP/400
public class EventGridValidationMiddleware
{
    private readonly RequestDelegate _next;

    public EventGridValidationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext httpContext)
    {
        BinaryData events = await BinaryData.FromStreamAsync(httpContext.Request.Body);
        EventGridEvent[] eventGridEvents = EventGridEvent.ParseMany(events);

        foreach (EventGridEvent eventGridEvent in eventGridEvents)
        {
            // Handle system events
            if (eventGridEvent.TryGetSystemEventData(out object eventData))
            {
                // Handle the subscription validation event
                if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
                {
                    var responseData = new SubscriptionValidationResponse()
                    {
                        ValidationResponse = subscriptionValidationEventData.ValidationCode
                    };
                    await httpContext.Response.WriteAsJsonAsync(responseData);
                }
            }

            await _next(httpContext);
        }
    }
}