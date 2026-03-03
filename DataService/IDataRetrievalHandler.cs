namespace DataService
{
    public interface IDataRetrievalHandler
    {
      
        IDataRetrievalHandler? SetNext(IDataRetrievalHandler next);

        Task<DataItem?> HandleAsync(string id, CancellationToken ct = default);
    }
}
