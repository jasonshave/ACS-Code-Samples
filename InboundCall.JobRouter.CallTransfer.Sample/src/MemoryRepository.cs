namespace InboundCall.JobRouter.CallTransfer.Sample;

public class MemoryRepository<TData> : IRepository<TData>
    where TData : class
{
    private readonly Dictionary<string, TData> _dataStore = new();

    public Task Save(TData data, string id)
    {
        _dataStore.Add(id, data);
        return Task.CompletedTask;
    }

    public Task<TData?> Get(string id)
    {
        return Task.Run(() =>
        {
            _dataStore.TryGetValue(id, out var value);
            return value;
        });
    }

    public Task Remove(string id)
    {
        _dataStore.Remove(id);
        return Task.CompletedTask;
    }
}