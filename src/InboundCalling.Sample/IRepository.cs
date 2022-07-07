namespace InboundCalling.Sample;

public interface IRepository<T>
{
    Task Save(T data, string contextId);
    Task<T?> Get(string contextId);
    Task Remove(string contextId);
}