namespace DataService
{
    public interface ISdcsCache<T>
    {
        bool TryGet(string key, out T? value);
        void Set(string key, T value);
    }
}
