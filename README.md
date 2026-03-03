DataService

A **.NET 8 Web API** that provides a layered data-retrieval service using three
levels of storage: **Redis**, a **Self-Designed Caching System (SDCS)**, and
**SQLite** as the persistent database, following the **Chain of Responsibility**
design pattern.

## Architecture Overview
```
POST /data
  └── DataService.SaveAsync()
        └── DataRepository.SaveAsync()  ← writes to SQLite only

GET /data/{id}
  └── Chain of Responsibility
        ├── RedisCacheHandler  (L1) ──► HIT → return immediately
        │        ↓ MISS
        ├── SdcsCacheHandler   (L2) ──► HIT → store in Redis → return
        │        ↓ MISS
        └── DatabaseHandler    (L3) ──► HIT → store in SDCS + Redis → return
                 ↓ MISS
             404 Not Found
```

> **Key rule:** On `POST`, data is saved to the **database only**.
> Cache layers are populated **lazily** on the first `GET`.

---

## Storage Layers

### Redis — L1 Cache

| Property | Value |
|---|---|
| Type | Distributed in-memory cache |
| TTL | **5 minutes** per entry |
| Library | `StackExchange.Redis` |
| Role | Fastest layer — checked first on every GET |
| On miss | Falls through to SDCS |
| On hit | Returns immediately |

If Redis is unavailable the handler catches the exception and falls through
gracefully — Redis failure never breaks a request.

---

### SDCS — L2 Cache

| Property | Value |
|---|---|
| Type | In-process in-memory cache (RAM) |
| Eviction | **LRU** — Least Recently Used |
| Capacity | Configurable: `3 ≤ capacity ≤ 100` |
| TTL | None — capacity-based eviction only |
| Role | Second layer — checked when Redis misses |
| On miss | Falls through to SQLite |
| On hit | Stores result in Redis, then returns |

Built from scratch using only primitive collections (`CacheEntry[]` array +
`ConcurrentDictionary`). See [SDCS Algorithm](#sdcs-algorithm).

---

### SQLite — L3 Database

| Property | Value |
|---|---|
| Type | Relational database (file-based) |
| Library | `Microsoft.EntityFrameworkCore.Sqlite` |
| Role | Source of truth — checked last |
| On miss | Returns 404 Not Found |
| On hit | Stores in SDCS and Redis, then returns |

`data.db` is created automatically on startup via `EnsureCreated()`.
No migrations needed.

## SDCS Algorithm

### Data structures

_entries[]   Fixed CacheEntry array (size = capacity)
_indexMap{}  Dictionary<string, int>  key → array index  O(1) lookup
_lruIndex    int — direct pointer to current LRU slot
_lock        object — guards multi-step atomic operations
```

### CacheEntry
```csharp
string Key           // cache key
T      Value         // any reference or value type
long   LastUsedTime  // DateTime.UtcNow.Ticks — updated on every touch

### TryGet

TryGet("k1")
  _indexMap lookup → MISS  → return false
  _indexMap lookup → HIT at index 2
    entry.LastUsedTime = now
    if index == _lruIndex → FindLruIndex()   // pointer must move
    return value

### Set

Set("k4", value)
  Key already in _indexMap?
    YES → update value + LastUsedTime
          if was lruIndex → FindLruIndex()
          return

  _count < _capacity?
    YES → GetEmptySlot() → write → _count++

  Cache FULL
    targetIdx = _lruIndex           // evict this slot
    _indexMap.TryRemove(old key)
    write new entry into targetIdx
    FindLruIndex() → _lruIndex updated

### FindLruIndex

Scans all **occupied** slots, returns index of the entry with the
smallest `LastUsedTime` — that is the new eviction candidate.

### Complexity table

| Operation | Cost | When |
|---|---|---|
| TryGet — no pointer move | O(1) | Entry is not LRU |
| TryGet — pointer move | O(n) | Entry IS the current LRU |
| Set — update existing | O(1)/O(n) | O(n) if entry was LRU |
| Set — insert, not full | O(n) | Scan for empty slot |
| Set — eviction | O(n) | FindLruIndex after evict |
| Maximum n | **100** | Config ceiling |

---

## Design Patterns

| Pattern | Where used |
|---|---|
| **Chain of Responsibility** | `RedisCacheHandler → SdcsCacheHandler → DatabaseHandler` |
| **Repository** | `IDataRepository / DataRepository` isolates EF Core |
| **Dependency Injection** | All abstractions wired in `Program.cs` |

Chain assembly in `Program.cs`:
```csharp
redis.SetNext(sdcs).SetNext(db);
return redis; // head of chain

## How to Run

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

### Step 1 — Start Redis

`docker-compose.yml` (Redis only, API runs locally):
```yaml
services:
  redis:
    image: redis:7-alpine
    container_name: redis_cache
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
```bash
docker-compose up -d

> `Sdcs:Capacity` must be **3–100**. App throws at startup if violated.

### Step 3 — Run the API
```bash
cd DataService
dotnet run

`data.db` is created automatically in the project folder on first run.

### Step 4 — Open Swagger
```
http://localhost:5260/swagger

## Working with Swagger
```
http://localhost:5260/swagger
```

### POST /data — Save data

Request body:
```json
{ "content": "Hello World" }
```

Response `201 Created`:
```json
{ "id": "a3f8c1d2e4b64f8a9c1d2e3f4b5a6c7d" }
```

### GET /data/{id} — Retrieve data

Response `200 OK`:
```json
{
  "id":        "a3f8c1d2e4b64f8a9c1d2e3f4b5a6c7d",
  "content":   "Hello World",
  "createdAt": "2024-01-15T12:00:00Z"
}

## Running Tests
```bash
cd DataService.Tests
dotnet test

| Category | Tests |
|---|---|
| Capacity validation | Below min (2), above max (101), boundaries 3 and 100 |
| TryGet | Miss → false, hit → true + correct value |
| Set | New key, update existing, fill to capacity |
| LRU eviction | Oldest evicted, access promotes, update promotes, sequential |
| Edge cases | Re-add evicted key, value types, object types |
| Bug exposure | FindLruIndex occupied vs empty slot condition |
| Thread safety | 500 concurrent reads/writes, 100 concurrent sets |
