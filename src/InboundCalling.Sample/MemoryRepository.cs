namespace InboundCalling.Sample;

public class MemoryRepository<TPrimitive> : IRepository<TPrimitive>
{
    private readonly Dictionary<string, TPrimitive> _store = new ();

    public Task Save(TPrimitive data, string contextId)
    {
        _store.Add(contextId, data);
        return Task.CompletedTask;
    }

    public Task<TPrimitive?> Get(string contextId)
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