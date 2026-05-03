# Azure Service Bus Interview Prep

## Core Concepts

### What is Azure Service Bus?
- Fully managed enterprise message broker with queues and publish-subscribe topics
- Supports AMQP 1.0 and HTTP/REST protocols
- Guarantees **at-least-once** delivery; supports **exactly-once** processing via sessions
- Competes with RabbitMQ, Amazon SQS/SNS, but adds enterprise features like transactions, sessions, and dead-lettering out of the box

### Tiers
| Feature | Basic | Standard | Premium |
|---------|-------|----------|---------|
| Queues | ✅ | ✅ | ✅ |
| Topics/Subscriptions | ❌ | ✅ | ✅ |
| Message size | 256 KB | 256 KB | 100 MB |
| Transactions | ❌ | ✅ | ✅ |
| Dedicated resources | ❌ | ❌ | ✅ (MU-based) |
| VNet integration | ❌ | ❌ | ✅ |
| RBAC + Private Endpoints | ❌ | Limited | ✅ |

---

## Queues vs. Topics

### Queues (Point-to-Point)
- One sender → one receiver (competing consumers pattern)
- Each message consumed by exactly one receiver
- Supports sessions for ordered, stateful processing
- Dead-letter sub-queue for poison messages

### Topics & Subscriptions (Pub/Sub)
- One sender → many subscribers
- Each subscription gets its own copy of the message
- **Subscription filters**:
  - **SQL filter**: `StoreId = 'Store1' AND Amount > 100`
  - **Correlation filter**: Match on properties like `ContentType`, `Subject`, custom properties (faster than SQL)
  - **Boolean/True filter**: Subscription receives everything
- Each subscription has its own dead-letter sub-queue

---

## Message Lifecycle

```
Sender → Queue/Topic → [Lock] → Receiver → Complete/Abandon/DeadLetter
```

### Receive Modes
- **ReceiveAndDelete**: Message removed on read. Fast but no safety net.
- **PeekLock** (default): Message locked for receiver. Must `Complete()`, `Abandon()`, or `DeadLetter()`.
  - Lock duration configurable (default 30s, max 5 min)
  - If lock expires, message reappears for other receivers

### Dead-Letter Queue (DLQ)
- Sub-queue at `{queue}/$deadletterqueue`
- Messages land here when:
  - Max delivery count exceeded
  - Receiver explicitly dead-letters
  - TTL expires (if `EnableDeadLetteringOnMessageExpiration = true`)
  - Filter evaluation fails on a topic subscription
- DLQ messages must be explicitly read and handled — they don't auto-purge

---

## Advanced Features

### Sessions (Ordered Processing)
- Set `RequiresSession = true` on queue/subscription
- Messages with same `SessionId` processed in FIFO order by one receiver
- Receiver accepts a session → gets exclusive lock on all messages in that session
- Session state: small blob stored on the session for correlation/checkpointing

### Scheduled Messages
- `ScheduledEnqueueTimeUtc` — message invisible until that time
- Use case: delayed notifications, retry after interval

### Message Deferral
- Receiver defers a message → it stays in queue but only retrievable by sequence number
- Use case: saga orchestration — "I'll process this later when another step completes"

### Transactions
- `TransactionScope` or `ServiceBusTransactionGroup` groups operations:
  - Send + Complete atomically
  - Send to multiple queues/topics in one transaction (same namespace)
- Prevents partial failures in multi-step message processing

### Auto-Forwarding
- Chain queues/subscriptions: messages arriving at A auto-forward to B
- Use case: fan-out, routing, aggregation patterns

### Duplicate Detection
- Enable on queue/topic with `RequiresDuplicateDetection = true`
- Dedup window (default 10 min, max 7 days)
- Uses `MessageId` to detect duplicates

---

## Security

### Authentication
- **Shared Access Signatures (SAS)**: Connection strings with send/listen/manage rights
- **Azure AD + RBAC** (preferred):
  - `Azure Service Bus Data Sender`
  - `Azure Service Bus Data Receiver`
  - `Azure Service Bus Data Owner`
- **Managed Identity**: App's managed identity gets RBAC role → no connection strings

### Network Security
- **Private Endpoints**: Service Bus accessible only via VNet
- **IP Firewall Rules**: Restrict access to known IPs
- **Service Endpoints**: Older approach, private endpoint preferred

---

## Patterns & Best Practices

### Competing Consumers
- Multiple receivers on one queue
- Service Bus handles lock-based distribution
- Scale receivers independently (works well with KEDA on AKS)

### Saga / Choreography
- Services communicate via topics
- Each service subscribes to relevant events, publishes its own
- Use sessions + deferral for ordering guarantees

### Claim Check
- Large payload → store in Blob Storage, send reference in message
- Avoids message size limits and reduces throughput costs

### Retry & Resilience
- SDK has built-in retry (exponential backoff)
- Set `MaxDeliveryCount` (default 10) — after that, message goes to DLQ
- Monitor DLQ depth as an alert trigger

### Idempotency
- At-least-once delivery means receivers MUST be idempotent
- Strategies: dedupe by `MessageId`, upsert operations, idempotency keys in DB

---

## Service Bus vs. Other Azure Messaging

| Criteria | Service Bus | Event Hub | Event Grid | Storage Queue |
|----------|------------|-----------|------------|---------------|
| Pattern | Enterprise messaging | Event streaming | Reactive events | Simple queue |
| Ordering | Sessions (FIFO) | Per partition | No | No |
| Throughput | Moderate | Very high (millions/s) | High | Low-moderate |
| Message size | 256 KB–100 MB | 1 MB | 1 MB | 64 KB |
| Dead-lettering | ✅ Built-in | ❌ | ✅ | ❌ |
| When to use | Decoupled services, workflows, transactions | Telemetry, log streaming, analytics | Webhooks, automation triggers | Cheap async tasks |

---

## Common Interview Questions

### Q: When would you choose Service Bus over Event Hub?
Service Bus for command-style messages where each message must be processed exactly once, with features like dead-lettering, sessions, and transactions. Event Hub for high-throughput event streaming where consumers independently read a log.

### Q: How do you guarantee ordered processing?
Enable sessions. All messages with the same `SessionId` are delivered in FIFO order to a single consumer that holds the session lock.

### Q: What happens when a message fails repeatedly?
After `MaxDeliveryCount` attempts, the message moves to the dead-letter queue with a `DeadLetterReason`. You need a separate process to read DLQ messages, alert, and either fix-and-resubmit or archive.

### Q: How would you scale Service Bus consumers on AKS?
Use KEDA with the Azure Service Bus scaler. ScaledObject watches queue depth or subscription message count and scales the deployment (including to zero).

### Q: Explain the Claim Check pattern.
When a message payload exceeds size limits or is expensive to transfer, store the payload in Blob Storage and send a lightweight message containing the blob URI. The consumer retrieves the full payload from storage.

---

## Tags
#azure #servicebus #messaging #interview #distributed-systems