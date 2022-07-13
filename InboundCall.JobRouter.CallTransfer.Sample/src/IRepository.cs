namespace InboundCall.JobRouter.CallTransfer.Sample;

public interface IRepository<TData>
{
    Task Save(TData data, string contextId);
    Task<TData?> Get(string contextId);
    Task Remove(string contextId);
}