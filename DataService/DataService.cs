namespace DataService
{
    public class DataService : IDataService
    {
        private readonly IDataRetrievalHandler _chain;
        private readonly IDataRepository _repository;

        public DataService(IDataRetrievalHandler chain, IDataRepository repository)
        {
            _chain = chain;
            _repository = repository;
        }

        public Task<DataItem?> GetAsync(string id, CancellationToken ct = default)
            => _chain.HandleAsync(id, ct);

        public async Task<string> SaveAsync(string payload, CancellationToken ct = default)
        {
            var item = new DataItem
            {
                Id = Guid.NewGuid().ToString(),
                Info = payload,
                CreatedAt = DateTime.UtcNow
            };

            return await _repository.SaveAsync(item, ct);
        }
    }
}
