# DataService

A .NET 8 Web API providing a layered data retrieval service with multi-level caching.

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                  DataController                     │
│            POST /data  │  GET /data/{id}            │
└──────────────┬──────────────────┬───────────────────┘
               │                  │
          SaveAsync            GetAsync
               │                  │
┌──────────────▼──────────────────▼───────────────────┐
│                   DataService                        │
└──────────────┬──────────────────┬───────────────────┘
          (POST)│          (GET)   │ Chain of Responsibility
                │       ┌──────────▼──────────────┐
                │       │   RedisCacheHandler  L1  │ HIT → return
                │       └──────────┬──────────────┘
                │       ┌──────────▼──────────────┐
                │       │    SdcsCacheHandler  L2  │ HIT → store in L1 → return
                │       └──────────┬──────────────┘
                │       ┌──────────▼──────────────┐
                │       │    DatabaseHandler    L3  │ HIT → store in L2+L1 → return
                │       └─────────────────────────┘
                │
       ┌────────▼────────────────┐
       │    DataRepository       │
       │  	SQLite   
       └─────────────────────────┘


### Design Patterns

- **Chain of Responsibility** — data retrieval flows through Redis → SDCS → Database sequentially. Each layer populates faster layers on a cache miss.
- **Repository Pattern** — `IDataRepository` abstracts the database. Swapping databases requires changing only two files.
- **Dependency Injection** — all components depend on interfaces, never concrete classes.

---

## Endpoints

### `POST /data`
Save a new item. Writes to **database only**.

**Request body**
```json
{ "content": "your data here" }
```

**Response** `201 Created`
```json
{ "id": "a3f8c1d2e4b64f8a..." }
```

### `GET /data/{id}`
Retrieve an item following the chain: **Redis → SDCS → Database**.

**Response** `200 OK`
```json
{
  "id":        "a3f8c1d2e4b64f8a...",
  "content":   "your data here",
  "createdAt": "2024-01-15T12:00:00Z"
}
```

**Response** `404 Not Found` — if not found in any layer.
