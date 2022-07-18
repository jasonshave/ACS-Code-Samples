namespace InboundCall.JobRouter.CallTransfer.Sample;

public interface IRepository<TData>
{
    Task Save(TData data, string id);
    Task<TData?> Get(string id);
    Task Remove(string id);
}