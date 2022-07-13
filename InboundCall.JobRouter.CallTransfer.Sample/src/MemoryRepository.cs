namespace InboundCall.JobRouter.CallTransfer.Sample;

public class MemoryRepository<TData> : IRepository<TData>
{
    private readonly Dictionary<string, TData> _store = new();

    public Task Save(TData data, string contextId)
    {
        _store.Add(contextId, data);
        return Task.CompletedTask;
    }

    public Task<TData?> Get(string contextId)
    {
        return Task.Run(() =>
        {
            _store.TryGetValue(contextId, out var value);
            return value;
        });
    }

    public Task Remove(string contextId)
    {
        _store.Remove(contextId);
        return Task.CompletedTask;
    }
}