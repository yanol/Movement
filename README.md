DataService

A .NET 10 Web API providing a layered data retrieval service with multi-level caching.

---

1. Executive Summary

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

2. System Architecture
   
The solution follows a Clean Architecture approach, separating concerns into API, Service, Repository, and Infrastructure layers.

2.1 The Retrieval Chain (Chain of Responsibility)
Data retrieval is orchestrated through a sequential pipeline:

L1 - Redis (Distributed): The first point of contact. If data exists here, it is returned immediately.

L2 - SDCS (In-Memory): If L1 misses, the Self-Designed Caching System is checked. On a hit, data is returned and "saved back" to L1.

L3 - Database (Persistent): The final source of truth. On a hit, data is returned and propagated to both L2 and L1.

2.2 Design Patterns
Chain of Responsibility: Manages the sequential flow of retrieval handlers.

Repository Pattern: Abstracts database operations, allowing for easy switching between PostgreSQL, MongoDB, or SQLite.

Dependency Injection: All components interact via interfaces to ensure testability and loose coupling.

3. SDCS: Self-Designed Caching SystemThe SDCS is a custom in-process cache implementation optimized for memory management without external dependencies.
   
  3.1 Technical Constraints:
  
  Storage: Volatile RAM (in-process).
  Capacity: Strictly enforced range of 3 to 100 entries.
  Eviction Policy: LRU (Least Recently Used). When the capacity is reached, the entry with the oldest "Last Used" timestamp is removed to make space for new data.
  Thread Safety: Implemented via lock guards to ensure safe concurrent access.
  
  3.2 Internal Data StructureThe SDCS utilizes two primary primitive collections to achieve O(1) and O(n) performance:
  
  CacheEntity[]: A fixed-size array storing the actual objects and their metadata.
  Dictionary<string, int>: A lookup table mapping data IDs to their specific array index.
  _lruIndex: An integer pointer that identifies which slot is next for eviction.

4. API Endpoints
The service exposes a RESTful interface for data management.

4.1 Save Data
Endpoint: POST /data

Logic: Saves data to the Database only. Does not populate caches on write to ensure cache "freshness" follows the "on-demand" pattern.

Response: 201 Created with the unique ID.

4.2 Retrieve Data
Endpoint: GET /data/{id}

Logic: Executes the retrieval chain (Redis → SDCS → Database).

Response: 200 OK with the data object or 404 Not Found if missing from all layers.

5. Deployment and Setup
5.1 Infrastructure with Docker
The solution includes a docker-compose.yml that provides a complete environment:

Database: PostgreSQL 16 or MongoDB.

Cache: Redis 7-Alpine.

GUIs: pgAdmin (SQL), Mongo-Express (NoSQL), and Redis-Commander (Cache).

5.2 Launch Instructions
Initialize Containers: docker-compose up -d.

Run API: dotnet run within the /DataService directory.

Access Documentation: Open http://localhost:5260/swagger to view and test the interactive API documentation.

6. Testing and Quality Assurance
The solution includes a dedicated test suite (DataService.Tests) covering:

Boundary Testing: Capacity limits (2, 3, 100, 101).

Logic Verification: LRU eviction order and access promotion.

Stress Testing: Thread safety under 500 concurrent operations.
