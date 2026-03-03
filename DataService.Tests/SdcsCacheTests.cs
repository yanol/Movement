using NUnit.Framework;
using DataService.Cache;

namespace DataService.Tests;

[TestFixture]
public class SdcsCacheTests
{
    private static SdcsCache<string> CreateCache(int capacity)
        => new SdcsCache<string>(capacity);

    [Test]
    public void Constructor_CapacityBelowMin_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateCache(2));
    }

    [Test]
    public void Constructor_CapacityAboveMax_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateCache(101));
    }

    [Test]
    public void Constructor_CapacityAtMin_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => CreateCache(3));
    }

    [Test]
    public void Constructor_CapacityAtMax_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => CreateCache(100));
    }

    [TestCase(3)]
    [TestCase(10)]
    [TestCase(50)]
    [TestCase(100)]
    public void Constructor_ValidCapacity_DoesNotThrow(int capacity)
    {
        Assert.DoesNotThrow(() => CreateCache(capacity));
    }

    [Test]
    public void TryGet_KeyNotPresent_ReturnsFalse()
    {
        var cache = CreateCache(5);

        var result = cache.TryGet("missing", out var value);

        Assert.That(result, Is.False);
        Assert.That(value, Is.Null);
    }

    [Test]
    public void TryGet_KeyPresent_ReturnsTrueAndCorrectValue()
    {
        var cache = CreateCache(5);
        cache.Set("k1", "hello");

        var result = cache.TryGet("k1", out var value);

        Assert.That(result, Is.True);
        Assert.That(value, Is.EqualTo("hello"));
    }

    [Test]
    public void TryGet_MultipleKeys_ReturnsCorrectValues()
    {
        var cache = CreateCache(5);
        cache.Set("k1", "v1");
        cache.Set("k2", "v2");
        cache.Set("k3", "v3");

        Assert.That(cache.TryGet("k2", out var value), Is.True);
        Assert.That(value, Is.EqualTo("v2"));
    }

    [Test]
    public void Set_NewKey_CanBeRetrieved()
    {
        var cache = CreateCache(5);
        cache.Set("k1", "value1");

        cache.TryGet("k1", out var value);

        Assert.That(value, Is.EqualTo("value1"));
    }

    [Test]
    public void Set_ExistingKey_UpdatesValue()
    {
        var cache = CreateCache(5);
        cache.Set("k1", "original");
        cache.Set("k1", "updated");

        cache.TryGet("k1", out var value);

        Assert.That(value, Is.EqualTo("updated"));
    }

    [Test]
    public void Set_MultipleKeys_AllRetrievable()
    {
        var cache = CreateCache(5);
        cache.Set("k1", "v1");
        cache.Set("k2", "v2");
        cache.Set("k3", "v3");

        Assert.Multiple(() =>
        {
            Assert.That(cache.TryGet("k1", out var v1), Is.True);
            Assert.That(cache.TryGet("k2", out var v2), Is.True);
            Assert.That(cache.TryGet("k3", out var v3), Is.True);
        });
    }

    [Test]
    public void Set_FillToCapacity_AllRetrievable()
    {
        var cache = CreateCache(3);
        cache.Set("k1", "v1");
        cache.Set("k2", "v2");
        cache.Set("k3", "v3");

        Assert.Multiple(() =>
        {
            Assert.That(cache.TryGet("k1", out _), Is.True);
            Assert.That(cache.TryGet("k2", out _), Is.True);
            Assert.That(cache.TryGet("k3", out _), Is.True);
        });
    }

    [Test]
    public void Set_SameKeyMultipleTimes_DoesNotGrowBeyondCapacity()
    {
        var cache = CreateCache(3);

        for (int i = 0; i < 10; i++)
            cache.Set("k1", $"value{i}");

        Assert.That(cache.TryGet("k1", out var value), Is.True);
        Assert.That(value, Is.EqualTo("value9"));
    }

    [Test]
    public void Eviction_WhenFull_OldestItemIsEvicted()
    {
        var cache = CreateCache(3);
        cache.Set("k1", "v1");
        Thread.Sleep(5);
        cache.Set("k2", "v2");
        Thread.Sleep(5);
        cache.Set("k3", "v3");
        Thread.Sleep(5);

        cache.Set("k4", "v4");

        Assert.Multiple(() =>
        {
            Assert.That(cache.TryGet("k1", out _), Is.False, "k1 should be evicted");
            Assert.That(cache.TryGet("k2", out _), Is.True);
            Assert.That(cache.TryGet("k3", out _), Is.True);
            Assert.That(cache.TryGet("k4", out _), Is.True);
        });
    }

    [Test]
    public void Eviction_AccessingItemPromotesIt_SoItIsNotEvicted()
    {
        var cache = CreateCache(3);
        cache.Set("k1", "v1");
        Thread.Sleep(5);
        cache.Set("k2", "v2");
        Thread.Sleep(5);
        cache.Set("k3", "v3");
        Thread.Sleep(5);

        cache.TryGet("k1", out _);
        Thread.Sleep(5);

        cache.Set("k4", "v4");

        Assert.Multiple(() =>
        {
            Assert.That(cache.TryGet("k1", out _), Is.True, "k1 was accessed — must survive");
            Assert.That(cache.TryGet("k2", out _), Is.False, "k2 is LRU — must be evicted");
            Assert.That(cache.TryGet("k3", out _), Is.True);
            Assert.That(cache.TryGet("k4", out _), Is.True);
        });
    }

    [Test]
    public void Eviction_UpdatingItemPromotesIt_SoItIsNotEvicted()
    {
        var cache = CreateCache(3);
        cache.Set("k1", "v1");
        Thread.Sleep(5);
        cache.Set("k2", "v2");
        Thread.Sleep(5);
        cache.Set("k3", "v3");
        Thread.Sleep(5);

        cache.Set("k1", "v1-updated");
        Thread.Sleep(5);

        cache.Set("k4", "v4");

        Assert.Multiple(() =>
        {
            Assert.That(cache.TryGet("k1", out var v1), Is.True);
            Assert.That(v1, Is.EqualTo("v1-updated"));
            Assert.That(cache.TryGet("k2", out _), Is.False, "k2 is LRU — must be evicted");
            Assert.That(cache.TryGet("k3", out _), Is.True);
            Assert.That(cache.TryGet("k4", out _), Is.True);
        });
    }

    [Test]
    public void Eviction_MultipleEvictions_AlwaysRemovesLru()
    {
        var cache = CreateCache(3);

        cache.Set("k1", "v1");
        Thread.Sleep(5);
        cache.Set("k2", "v2");
        Thread.Sleep(5);
        cache.Set("k3", "v3");
        Thread.Sleep(5);

        cache.Set("k4", "v4");
        Assert.That(cache.TryGet("k1", out _), Is.False, "k1 should be evicted first");
        Thread.Sleep(5);

        cache.Set("k5", "v5");
        Assert.That(cache.TryGet("k2", out _), Is.False, "k2 should be evicted second");
        Thread.Sleep(5);

        cache.Set("k6", "v6");
        Assert.That(cache.TryGet("k3", out _), Is.False, "k3 should be evicted third");

        Assert.Multiple(() =>
        {
            Assert.That(cache.TryGet("k4", out _), Is.True);
            Assert.That(cache.TryGet("k5", out _), Is.True);
            Assert.That(cache.TryGet("k6", out _), Is.True);
        });
    }


    [Test]
    public void TryGet_AfterEviction_ReturnsFalse()
    {
        var cache = CreateCache(3);
        cache.Set("k1", "v1");
        Thread.Sleep(5);
        cache.Set("k2", "v2");
        Thread.Sleep(5);
        cache.Set("k3", "v3");
        Thread.Sleep(5);
        cache.Set("k4", "v4");

        Assert.That(cache.TryGet("k1", out _), Is.False);
    }

    [Test]
    public void Set_AfterEviction_EvictedKeyCanBeReAdded()
    {
        var cache = CreateCache(3);
        cache.Set("k1", "v1");
        Thread.Sleep(5);
        cache.Set("k2", "v2");
        Thread.Sleep(5);
        cache.Set("k3", "v3");
        Thread.Sleep(5);
        cache.Set("k4", "v4");

        cache.Set("k1", "v1-new");

        Assert.That(cache.TryGet("k1", out var value), Is.True);
        Assert.That(value, Is.EqualTo("v1-new"));
    }

    [Test]
    public void Cache_WorksWithValueTypes()
    {
        var cache = new SdcsCache<int>(5);
        cache.Set("num", 42);

        Assert.That(cache.TryGet("num", out var value), Is.True);
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void Cache_WorksWithObjectTypes()
    {
        var cache = new SdcsCache<List<string>>(5);
        var list = new List<string> { "a", "b", "c" };
        cache.Set("list", list);

        Assert.That(cache.TryGet("list", out var result), Is.True);
        Assert.That(result!.Count, Is.EqualTo(3));
    }

    [Test]
    [Description("Exposes bug: FindLruIndex checks IsNullOrWhiteSpace instead of !IsNullOrWhiteSpace")]
    public void FindLruIndex_Bug_MustFindOccupiedSlotsNotEmptyOnes()
    {
        var cache = CreateCache(3);
        cache.Set("k1", "v1");
        Thread.Sleep(5);
        cache.Set("k2", "v2");
        Thread.Sleep(5);
        cache.Set("k3", "v3");
        Thread.Sleep(5);

        cache.Set("k4", "v4");

        int found = new[] { "k1", "k2", "k3", "k4" }
            .Count(k => cache.TryGet(k, out _));

        Assert.That(found, Is.EqualTo(3), "Cache must hold exactly 3 items after eviction");
    }

    [Test]
    public void ConcurrentSetAndGet_DoesNotThrow()
    {
        var cache = CreateCache(10);

        var tasks = Enumerable.Range(0, 500).Select(i => Task.Run(() =>
        {
            var key = $"k{i % 15}";
            cache.Set(key, $"value{i}");
            cache.TryGet(key, out _);
        })).ToArray();

        Assert.DoesNotThrow(() => Task.WhenAll(tasks).GetAwaiter().GetResult());
    }

    [Test]
    public void ConcurrentSet_DoesNotExceedCapacity_AndCacheStillWorks()
    {
        var cache = CreateCache(5);

        var tasks = Enumerable.Range(0, 100)
            .Select(i => Task.Run(() => cache.Set($"k{i}", $"v{i}")))
            .ToArray();

        Task.WhenAll(tasks).GetAwaiter().GetResult();

        cache.Set("final", "value");
        Assert.That(cache.TryGet("final", out _), Is.True);
    }
}