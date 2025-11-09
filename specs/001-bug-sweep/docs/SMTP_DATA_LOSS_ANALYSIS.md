# Current Implementation: Data Loss Risk Analysis

## Current Flow (In-Memory Channel)

```
SMTP Connection                          In-Memory Channel                    Background Processor
     │                                         │                                      │
     │  1. Email arrives                       │                                      │
     ├──────────────────────►                  │                                      │
     │  2. Parse MIME                          │                                      │
     │  3. Cache lookups                       │                                      │
     │  4. Create job                          │                                      │
     ├─────────────────────►│                  │                                      │
     │                      │ Queue Job        │                                      │
     │◄─────────────────────┤                  │                                      │
     │  5. Return OK        │                  │                                      │
     │                      │                  ├─────────────────────────────────────►│
     │                      │                  │  6. Dequeue job                      │
     │                      │                  │                                      │
     │                      │                  │                       7. Write file  │
     │                      │                  │                       8. Execute cmd │
     │                      │                  │                       9. Queue jobs  │
     │                      │                  │◄─────────────────────────────────────┤
     │                      │                  │  10. Done             
     
❌ CRASH HERE ─────────────►│ ← Jobs in queue lost forever!
                            │
                            └── 0 jobs recovered (memory cleared)
```

## Failure Scenario Timeline

```
T+0ms    : SMTP receives 10,000 emails
T+100ms  : All 10,000 queued to in-memory channel, SMTP returns OK to all senders
T+500ms  : Processor handled 2,000 emails (8,000 still in channel buffer)
T+501ms  : ⚡ SERVER CRASH (power outage, OOM, kernel panic, etc.)
T+502ms  : 💥 8,000 jobs in memory buffer evaporate
T+10s    : Server restarts
T+11s    : EmailIngestionProcessor starts with EMPTY channel
Result   : 8,000 emails permanently lost (but senders think they were delivered)
```

## Memory Usage Risk

### Unbounded Channel Growth
```
Scenario: Burst traffic - 50,000 emails/minute for 5 minutes

Assumptions:
- Average email size: 100KB (with attachments)
- Processing rate: 5,000 emails/minute (slower than ingestion)
- Channel queue grows by: 45,000 emails/minute

Memory impact:
Minute 1: 45,000 emails × 100KB = 4.5 GB
Minute 2: 90,000 emails × 100KB = 9.0 GB
Minute 3: 135,000 emails × 100KB = 13.5 GB
Minute 4: 180,000 emails × 100KB = 18.0 GB
Minute 5: ⚡ OutOfMemoryException → Crash → ALL 225,000 emails lost
```

## Recommended Fix: Hybrid Approach

```
SMTP Connection                          File System                          Database/EventStore
     │                                         │                                      │
     │  1. Email arrives                       │                                      │
     ├──────────────────────►                  │                                      │
     │  2. Parse MIME                          │                                      │
     │  3. Cache lookups                       │                                      │
     │  4. Write file (SYNC) ─────────────────►│                                      │
     │                                          │ Durable on disk ✅                   │
     │◄─────────────────────────────────────────┤                                      │
     │  5. Return OK (file safe)               │                                      │
     │                                          │                                      │
     │  Background processor scans orphaned files on startup:                          │
     │  - File exists: message_123.eml         │                                      │
     │  - DB query: SELECT * WHERE id=123      │                                      │
     │  - Not found → Replay ReceivedEmailCommand ───────────────────────────────────►│
     │                                          │                                      │
     
✅ CRASH HERE ──────────────────────────────►│ ← Files persist, can be recovered!
                                              │
                                              └── Recovery: Scan .eml files, replay missing commands
```

## Recovery Strategy

### File-Based Recovery (After crash)

```csharp
// Startup recovery service
public class EmailRecoveryService : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var messageFiles = Directory.GetFiles(messageStorePath, "*.eml");
        
        foreach (var filePath in messageFiles)
        {
            var messageId = ExtractMessageIdFromFileName(filePath);
            
            // Check if metadata exists in DB
            var exists = await _emailRepository.ExistsAsync(messageId);
            
            if (!exists)
            {
                // Orphaned file - replay command
                var message = await MimeMessage.LoadAsync(filePath);
                await ReplayReceivedEmailCommand(messageId, message);
                
                _logger.LogWarning("Recovered orphaned email {MessageId}", messageId);
            }
        }
    }
}
```

## Key Insights

1. **Current risk level: HIGH**
   - Any crash = data loss
   - SMTP said "OK" but email never persisted
   - No recovery mechanism

2. **File write is cheap**
   - ~5-20ms to write email to disk
   - Much cheaper than full DB transaction (~50-200ms)
   - Provides durability guarantee

3. **Command execution can wait**
   - Metadata in DB is less critical
   - Can be reconstructed from email file
   - Safe to defer to background processor

4. **Recovery is simple**
   - Scan for .eml files on startup
   - Query DB for each message ID
   - If missing → replay command
   - Idempotent (safe to replay)

## Action Required

Choose your risk tolerance:

| Risk Level | Accept Data Loss? | Action |
|------------|------------------|---------|
| **Critical systems** | ❌ Never acceptable | Implement Option 1 (sync file write) or Option 2 (Redis) |
| **Development/Testing** | ✅ Acceptable | Keep current (document risk) |
| **Medium priority** | ⚠️ Rare acceptable | Add bounded channel + monitoring alerts |

The current implementation is **NOT production-ready** for systems that cannot tolerate data loss.
