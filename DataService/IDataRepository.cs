namespace DataService
{
    public interface IDataRepository
    {
        Task<DataItem?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<string> SaveAsync(DataItem item, CancellationToken ct = default);
    }
}