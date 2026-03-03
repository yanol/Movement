namespace DataService.Cache
{
    public class CacheEntity<T>
    {
        public string Key { get; set; } = string.Empty;
        public T Value { get; set; } = default!;
        public long LastUsedTime{ get; set; } = 0; 
    }
}
