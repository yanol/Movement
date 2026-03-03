namespace DataService.Cache
{
    public class DatabaseHandler : DataRetrievalHandlerBase
    {
        private readonly IDataRepository _repository;
        private readonly ILogger<DatabaseHandler> _logger;

        public DatabaseHandler(IDataRepository repository, ILogger<DatabaseHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        protected override async Task<DataItem?> FetchAsync(string id, CancellationToken ct)
        {
            var item = await _repository.GetByIdAsync(id, ct);

            if (item != null)
                _logger.LogInformation("DB: Found id={Id}", id);

            else
                _logger.LogInformation("DB: Not found id={Id}", id);

            return item;
        }

      
        protected override Task StoreAsync(DataItem item, CancellationToken ct)
            => Task.CompletedTask;
    }
}
