namespace DataService.Cache
{
    public abstract class DataRetrievalHandlerBase : IDataRetrievalHandler
    {
        private IDataRetrievalHandler? _nextChain;

        public async Task<DataItem?> HandleAsync(string id, CancellationToken ct = default)
        {
            var item = await FetchAsync(id, ct);

            if (item != null) return item;
            
            if (_nextChain == null) return null;
                
            item = await _nextChain.HandleAsync(id, ct);

            if (item != null)
                await StoreAsync(item, ct);

            return item;
        }

        public IDataRetrievalHandler? SetNext(IDataRetrievalHandler next)
        {
            _nextChain = next;
            return next;
        }

        protected abstract Task<DataItem?> FetchAsync(string id, CancellationToken ct);

        protected abstract Task StoreAsync(DataItem item, CancellationToken ct);
    }
}
