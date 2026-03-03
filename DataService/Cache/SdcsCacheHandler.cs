using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace DataService.Cache
{
    public class SdcsCacheHandler : DataRetrievalHandlerBase
    {
        private readonly ISdcsCache<DataItem> _sdcs;
        private readonly ILogger<SdcsCacheHandler> _logger;

        public SdcsCacheHandler( ISdcsCache<DataItem> sdcs, ILogger<SdcsCacheHandler> logger)
        {
            _sdcs = sdcs;
            _logger = logger;
        }

        protected override Task<DataItem?> FetchAsync(string id, CancellationToken ct)
        {
            if (_sdcs.TryGet(id, out var item))
            {
                _logger.LogInformation("SDCS: Cache HIT for id={Id}", id);
                return Task.FromResult<DataItem?>(item);
            }

            _logger.LogInformation("SDCS: Cache MISS for id={Id}", id);
            return Task.FromResult<DataItem?>(null);
        }

        protected override Task StoreAsync(DataItem item, CancellationToken ct)
        {
            _sdcs.Set(item.Id, item);
            _logger.LogInformation("SDCS: Stored id={Id}", item.Id);

            return Task.CompletedTask;
        }
    }
}
