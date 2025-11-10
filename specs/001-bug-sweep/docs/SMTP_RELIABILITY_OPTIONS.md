# SMTP Email Ingestion Reliability Options

## Problem Statement
Current implementation uses in-memory `System.Threading.Channels` which **loses queued jobs on crash/restart**.

**Risk**: If 10,000 emails are queued and server crashes after processing 2,000, the remaining 8,000 are permanently lost even though SMTP returned success.

## Solution Options

### Option 1: **Synchronous Write + Async Processing** (Recommended for Most Cases)
**Strategy**: Write email file immediately in SMTP path, queue only the metadata command for async processing.

**Pros**:
- Email file is durable immediately (no data loss)
- Still improves performance (file write is faster than file write + DB commit)
- Simple implementation
- No external dependencies

**Cons**:
- Still blocks SMTP on file I/O (~5-20ms)
- Less throughput improvement than full async (but still 5-10x better)

**Implementation**:
```csharp
// SpammaMessageStore.cs
var messageId = Guid.NewGuid();

// Write file SYNCHRONOUSLY (durable immediately)
var saveFileResult = await messageStoreProvider.StoreMessageContentAsync(messageId, message, cancellationToken);
if (!saveFileResult.IsSuccess)
{
    return SmtpResponse.TransactionFailed;
}

// Queue ONLY the command execution (non-blocking)
var ingestionJob = new EmailMetadataIngestionJob(
    messageId,
    foundSubdomain.ParentDomainId,
    foundSubdomain.SubdomainId,
    message.Subject,
    message.Date.DateTime,
    addresses,
    chaosAddressId);

await ingestionChannel.Writer.WriteAsync(ingestionJob, cancellationToken);
return SmtpResponse.Ok;
```

**Recovery**: On startup, scan file system for `.eml` files without corresponding DB records, replay `ReceivedEmailCommand`.

---

### Option 2: **Persistent Queue (Redis/RabbitMQ)** (Best for High Reliability)
**Strategy**: Replace in-memory channel with durable message queue.

**Pros**:
- Zero data loss (queue survives crashes)
- Can scale horizontally (multiple processors)
- Built-in retry/dead-letter queue support
- Industry-standard pattern

**Cons**:
- External dependency (Redis/RabbitMQ required)
- More complex infrastructure
- Network latency added to SMTP path (~1-5ms)

**Implementation** (Redis Streams example):
```csharp
// Use StackExchange.Redis or MassTransit
public class RedisEmailIngestionQueue : IEmailIngestionQueue
{
    private readonly IConnectionMultiplexer _redis;
    
    public async Task EnqueueAsync(EmailIngestionJob job, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var serialized = JsonSerializer.Serialize(job);
        
        // Durable queue - survives crashes
        await db.StreamAddAsync("email-ingestion", "job", serialized);
    }
}
```

**Recommended for**: Production systems with high email volume or strict SLA requirements.

---

### Option 3: **Database-Backed Queue** (Postgres/Marten)
**Strategy**: Use Postgres as the queue (you already have Marten).

**Pros**:
- No new infrastructure (reuse existing Postgres)
- ACID guarantees
- Can query queue state
- Transactional (email + queue entry in one commit)

**Cons**:
- Slower than Redis (~10-50ms per job)
- Database load increases
- Requires polling or LISTEN/NOTIFY

**Implementation** (Marten Events):
```csharp
// Store email as an "EmailReceived" event immediately
// Background processor polls for unprocessed events
public class EmailIngestionQueueRepository
{
    public async Task EnqueueAsync(EmailIngestionJob job)
    {
        // Store as event - durable immediately
        await _documentStore.Events.Append(
            Guid.NewGuid(), 
            new EmailReceivedEvent(job.MessageId, job.Message, ...));
        await _documentStore.SaveChangesAsync();
    }
}
```

---

### Option 4: **Bounded Channel + Backpressure** (Trade Throughput for Safety)
**Strategy**: Use bounded channel that blocks SMTP when queue is full.

**Pros**:
- Prevents unbounded memory growth
- Forces senders to slow down under load
- No external dependencies
- Simple

**Cons**:
- **Still loses queued data on crash** (just limits how much)
- SMTP can block/timeout if queue fills
- Doesn't solve core reliability issue

**Implementation**:
```csharp
// Module.cs
services.AddSingleton(Channel.CreateBounded<EmailIngestionJob>(
    new BoundedChannelOptions(5000) // Max 5000 pending
    {
        FullMode = BoundedChannelFullMode.Wait // Block SMTP when full
    }));
```

**Not recommended as sole solution** - still has data loss risk.

---

## Recommendation Matrix

| Your Priority | Recommended Solution | Expected Throughput | Reliability |
|--------------|---------------------|---------------------|-------------|
| **Quick fix, minimal changes** | Option 1: Sync write + async cmd | 5-10x improvement | High (file-based recovery) |
| **High reliability, scale** | Option 2: Redis queue | 10-30x improvement | Very High (zero loss) |
| **Simplicity, existing stack** | Option 3: Postgres queue | 3-5x improvement | Very High (ACID) |
| **Memory safety only** | Option 4: Bounded channel | 10-50x improvement | Low (still loses data) |

## My Recommendation: **Option 1** (Sync Write + Async Command)

**Why**: Best balance of reliability and performance without new infrastructure.

**Migration path**:
1. Start with Option 1 (file write sync, command async)
2. Monitor performance under real load
3. If throughput insufficient, upgrade to Option 2 (Redis)

**Justification**:
- Email file is most important (contains actual message)
- DB metadata can be reconstructed from file if needed
- File write is fast (5-20ms), much faster than file + DB commit (50-200ms)
- Still get 5-10x throughput improvement
- Zero data loss on crash

Would you like me to implement Option 1 (sync file write, async command)?
