using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace DataService.Cache
{
    public class RedisCacheHandler : DataRetrievalHandlerBase
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
        private readonly IDistributedCache _redis;
        private readonly ILogger<RedisCacheHandler> _logger;

        public RedisCacheHandler(IDistributedCache redis, ILogger<RedisCacheHandler> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        protected override async Task<DataItem?> FetchAsync(string id, CancellationToken ct)
        {
            try
            {
                var cachedData = await _redis.GetStringAsync(id, ct);

                if (string.IsNullOrEmpty(cachedData)) return null;
                
                 _logger.LogInformation($"Redis: Cache HIT for id={id}");

                var item = JsonSerializer.Deserialize<DataItem>(cachedData);
                if (item is null)
                {
                    _logger.LogWarning($"Redis: Deserialization returned null for id={id}");
                    return null;
                }

                return item;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Redis: Cache read failed for id={id}.");
                return null;
            }
        }

        protected override async Task StoreAsync(DataItem item, CancellationToken ct)
        {
            try
            {
                var jsonData = JsonSerializer.Serialize(item);
                var opts = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl };
                await _redis.SetStringAsync(item.Id, jsonData, opts, ct);

                _logger.LogInformation($"Redis: Stored id={item.Id} (TTL={Ttl})");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Redis: Cache write failed for id={item.Id}.");
            }
        }
    }
}
