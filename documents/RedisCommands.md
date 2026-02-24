# Redis & StoreApi — Run Commands

## Option A — Docker Compose (recommended)

Everything (SQL Server + Redis + API) in one command.

```bash
# Build and start all containers
docker compose up --build -d

# Check all containers are running and healthy
docker compose ps

# Watch live API logs
docker logs storeapi-app -f

# Stop everything
docker compose down

# Stop and delete all data volumes (fresh start)
docker compose down -v
```

API → http://localhost:5124  
Swagger → http://localhost:5124/swagger

---

## Option B — Local dev (dotnet run)

Start Redis separately, then run the API locally.

```bash
# Start only Redis (if not already running)
docker run -d -p 6379:6379 --name storeapi-redis redis:alpine

# Run the API
dotnet run
```

API → https://localhost:5148 (see Properties/launchSettings.json for exact port)

---

## Redis Inspection Commands

```bash
# See all cached keys
docker exec storeapi-redis redis-cli KEYS '*'

# See only app cache keys
docker exec storeapi-redis redis-cli KEYS 'StoreApi:*'

# See only rate limit keys
docker exec storeapi-redis redis-cli KEYS 'ratelimit:*'

# Check TTL (seconds remaining) for a key
docker exec storeapi-redis redis-cli TTL 'StoreApi:products:all'

# Read the raw cached JSON for a key
docker exec storeapi-redis redis-cli GET 'StoreApi:products:all'

# Watch every Redis command in real time (debug)
docker exec storeapi-redis redis-cli MONITOR

# Delete all keys (manual cache flush)
docker exec storeapi-redis redis-cli FLUSHALL

# Open interactive Redis CLI
docker exec -it storeapi-redis redis-cli
```

---

## Cache Key Reference

| Key pattern | What it caches | TTL |
|---|---|---|
| `StoreApi:products:all` | Full product list | 60s |
| `StoreApi:products:paged:{page}:{size}` | Paginated product page | 60s |
| `StoreApi:products:id:{id}` | Single product | 60s |
| `StoreApi:products:category:{id}` | Products by category | 60s |
| `StoreApi:products:search:{term}` | Search results | 60s |
| `StoreApi:categories:all` | Full category list | 300s |
| `StoreApi:categories:id:{id}` | Single category | 300s |
| `ratelimit:{ip}` | Request counter per IP | 60s (1 min window) |

> Cache TTLs are configured in `appsettings.json` under `"Cache"`.  
> All `products:*` keys are invalidated automatically on any POST/PUT/DELETE to `/api/products`.  
> All `categories:*` **and** `products:*` keys are invalidated on any POST/PUT/DELETE to `/api/categories` (because `ProductResponseDto` embeds `CategoryName`).
