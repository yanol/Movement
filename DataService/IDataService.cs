namespace DataService
{
    public interface IDataService
    {
        Task<DataItem?> GetAsync(string id, CancellationToken ct = default);
        Task<string> SaveAsync(string payload, CancellationToken ct = default);
    }
}
