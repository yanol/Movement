using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace DataService.Cache
{
    public class SdcsCache<T> : ISdcsCache<T>
    {
        private readonly CacheEntity<T>[] _entries;
        private readonly Dictionary<string, int> _indexMap;

        private readonly int _capacity;
        private int _count;
        private int _lruIndex; 

        private readonly object _lock = new();

        public SdcsCache(int capacity)
        {
            if (capacity < 3 || capacity > 100)
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    "Capacity must be between 3 and 100.");

            _capacity = capacity;
            _entries = new CacheEntity<T>[_capacity];
            _indexMap = new Dictionary<string, int>(_capacity);

            _lruIndex = 0;
            _count = 0;

            for (int i = 0; i < _capacity; i++)
                _entries[i] = new CacheEntity<T>();
        }

        public void Set(string key, T value)
        {
            lock (_lock)
            {
                // ── Check for existing key ─────────────────────────────────
                if (_indexMap.TryGetValue(key, out int existingIdx))
                {
                    var existing = _entries[existingIdx];
                    existing.Value = value;
                    existing.LastUsedTime = DateTime.UtcNow.Ticks;

                    if (existingIdx == _lruIndex)
                        _lruIndex = FindLruIndex();

                    return;
                }

                // ── Find empty slot ───────────────-──────────────────────
                int targetIdx;

                if (_count < _capacity)
                {
                    targetIdx = GetEmptySlot();
                    _count++;
                }
                else
                {
                    targetIdx = _lruIndex;
                    _indexMap.Remove(_entries[targetIdx].Key, out _);
                }

                var entry = _entries[targetIdx];
                entry.Key = key;
                entry.Value = value;
                entry.LastUsedTime = DateTime.UtcNow.Ticks;

                _indexMap[key] = targetIdx;

                _lruIndex = FindLruIndex();
            }
        }

        public bool TryGet(string key, out T? value)
        {
            lock (_lock)
            {
                if (!_indexMap.TryGetValue(key, out int index))
                {
                    value = default;
                    return false;
                }

                var entry = _entries[index];
                entry.LastUsedTime = DateTime.UtcNow.Ticks;

                if (index == _lruIndex)
                    _lruIndex = FindLruIndex();

                value = entry.Value;
                return true;
            }
        }


        private int FindLruIndex()
        {
            int minIndexd = -1;
            long minTimestamp = long.MaxValue;

            for (int i = 0; i < _capacity; i++)
            {
                if (!string.IsNullOrWhiteSpace(_entries[i].Key) && _entries[i].LastUsedTime < minTimestamp)
                {
                    minTimestamp = _entries[i].LastUsedTime;
                    minIndexd = i;
                }
            }

            return minIndexd == -1 ? 0 : minIndexd;
        }

        private int GetEmptySlot()
        {
            for (int i = 0; i < _capacity; i++)
                if (string.IsNullOrWhiteSpace(_entries[i].Key))
                    return i;
            return 0;
        }
    }
}
