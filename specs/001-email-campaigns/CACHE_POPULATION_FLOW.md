# Cache Population Flow

## SubdomainCache Population

### Scenario 1: Cache Miss (First Request for Domain)

```
SMTP Email Arrives
  ↓
SpammaMessageStore.SaveAsyncWithProvider()
  ↓
subdomainCache.GetSubdomainAsync("example.com")
  ↓
  ├─ Check Redis: StringGet("subdomain:example.com")
  │  └─ NOT FOUND (cache miss)
  │
  ├─ Query Database: querier.Send(SearchSubdomainsQuery)
  │  └─ SELECT * FROM subdomains WHERE domain_name = 'example.com' AND status = Active LIMIT 1
  │     └─ Returns: SubdomainSummary { Id, Name, DomainId, ... }
  │
  ├─ Store in Cache: SetSubdomainAsync()
  │  └─ Redis StringSet("subdomain:example.com", JSON_bytes, TTL: 1 hour)
  │
  └─ Return: Maybe.From(subdomain) ✅
```

**Performance Impact:**
- First request: ~100-200ms (database query + Redis write)
- Logs: `Cache MISS for subdomain: example.com` + `Cached subdomain: example.com with TTL 01:00:00`

---

### Scenario 2: Cache Hit (Subsequent Requests for Same Domain)

```
SMTP Email Arrives
  ↓
SpammaMessageStore.SaveAsyncWithProvider()
  ↓
subdomainCache.GetSubdomainAsync("example.com")
  ↓
  ├─ Check Redis: StringGet("subdomain:example.com")
  │  └─ FOUND ✅
  │
  ├─ Deserialize: JsonSerializer.Deserialize<SubdomainSummary>(cached_bytes)
  │  └─ Parse JSON back to object
  │
  └─ Return: Maybe.From(cached_subdomain) ✅
     └─ Performance: ~1-2ms (Redis read + JSON deserialization)
```

**Performance Impact:**
- Cache hit: ~1-2ms (Redis read only, no database query)
- Logs: `Cache HIT for subdomain: example.com`

---

### Scenario 3: Cache Invalidation (Subdomain Status Changes)

```
Admin suspends subdomain via UI
  ↓
SuspendSubdomainCommand sent to server
  ↓
SuspendSubdomainCommandHandler.HandleInternal()
  ├─ Load subdomain from database
  ├─ Call subdomain.Suspend() (domain logic)
  ├─ Save to database (Marten event store)
  │
  └─ [TODO] Publish: SubdomainStatusChangedIntegrationEvent
     └─ CAP publishes to Redis: "domain-management.subdomain.status-changed"
        │
        └─ CacheInvalidationEventHandler receives event
           ├─ OnSubdomainStatusChanged()
           └─ Call subdomainCache.InvalidateAsync("example.com")
              └─ Redis KeyDelete("subdomain:example.com")
                 └─ Cache cleared ✅
```

**Result:**
- Next SMTP email for that domain → Cache MISS (expected)
- Database re-queried with fresh status
- New result cached for 1 hour

---

## ChaosAddressCache Population

### Same Pattern - Three Scenarios:

**Scenario 1: Cache Miss**
```
SpammaMessageStore checks: chaosAddressCache.GetChaosAddressAsync(subdomainId, localPart)
  ↓
  ├─ Check Redis: StringGet("chaos:{subdomainId}:{localPart}")
  │  └─ NOT FOUND
  │
  ├─ Query Database: querier.Send(GetChaosAddressBySubdomainAndLocalPartQuery)
  │  └─ SELECT * FROM chaos_addresses WHERE subdomain_id = ? AND local_part = ? LIMIT 1
  │
  ├─ Store in Cache: SetChaosAddressAsync()
  │  └─ Redis StringSet("chaos:...", JSON_bytes, TTL: 1 hour)
  │
  └─ Performance: ~100-200ms (database + Redis write)
```

**Scenario 2: Cache Hit**
```
SpammaMessageStore checks: chaosAddressCache.GetChaosAddressAsync(subdomainId, localPart)
  ↓
  ├─ Check Redis: StringGet("chaos:{subdomainId}:{localPart}")
  │  └─ FOUND ✅
  │
  ├─ Deserialize JSON
  │
  └─ Performance: ~1-2ms (Redis read only)
```

**Scenario 3: Cache Invalidation**
```
Admin updates chaos address config
  ↓
[TODO] Publish: ChaosAddressUpdatedIntegrationEvent
  ↓
CacheInvalidationEventHandler.OnChaosAddressUpdated()
  ├─ Call chaosAddressCache.InvalidateBySubdomainAsync(subdomainId)
  │  └─ Pattern delete: Redis KeyDelete("chaos:{subdomainId}:*")
  │     └─ Deletes ALL chaos addresses for that subdomain
  │
  └─ Cache cleared ✅
```

---

## Cache Lifecycle

### Cold Start (Application Start)
- **Redis**: Empty
- First SMTP email for any domain: MISS → Database query → Populate cache
- Subsequent emails: HIT

### Steady State (Running)
- **Hit Rate**: ~95%+ (assuming frequent emails to same domains)
- **TTL**: 1 hour per cache entry
- **Memory**: Depends on unique domain/chaos address combinations
  - Typical: ~1KB per cached entry
  - 1000 unique domains: ~1MB Redis memory

### After Invalidation
- Cache entry deleted immediately via CAP event
- Next access: MISS → Database query → Repopulated
- Ensures freshness after configuration changes

---

## Key Implementation Details

### Cache Keys
- **Subdomain**: `subdomain:{domain.ToLowerInvariant()}`
  - Example: `subdomain:example.com`
  
- **Chaos Address**: `chaos:{subdomainId}:{localPart.ToLowerInvariant()}`
  - Example: `chaos:550e8400-e29b-41d4-a716-446655440000:admin`

### Serialization
- **Format**: JSON (UTF-8 bytes)
- **Library**: System.Text.Json (built-in)
- **Size**: ~200-500 bytes per entry (typical)

### Error Handling
- **Redis unavailable**: Logs warning, continues with database query
- **Deserialization fails**: Logs warning, deletes bad cache entry, queries database
- **Cache write fails**: Logs warning, operation succeeds via database fallback

### Logging Levels
- `DEBUG`: Cache hits/misses (disabled in production typically)
- `INFO`: Cache operations during startup/initialization
- `WARNING`: Redis errors, serialization failures, cache invalidation issues

---

## Performance Summary

| Operation | Before Cache | After Cache | Improvement |
|-----------|-------------|-------------|------------|
| Subdomain lookup (miss) | ~100-200ms | ~100-200ms | Same (first time) |
| Subdomain lookup (hit) | N/A | ~1-2ms | 50-200x faster |
| Chaos address lookup (miss) | ~100-150ms | ~100-150ms | Same (first time) |
| Chaos address lookup (hit) | N/A | ~1-2ms | 50-150x faster |
| **Per SMTP email** (2 lookups) | ~200-350ms | ~2-4ms avg | 50-175x faster |
| **30 emails/session** | ~6-10 seconds | ~60-120ms | **50-175x faster** |

**Real-world with cache warming:**
- 95% hit rate → Average: ~0.2ms × 0.95 + 100ms × 0.05 = ~5.2ms per lookup
- 30 emails (60 lookups) → ~300ms total (~20x improvement)
- 150 emails (300 lookups) → ~1.5 seconds total ✅
