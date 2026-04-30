## Core concepts, .NET integration, and interview-ready answers

---
## 1. What Is Kafka (in 30 seconds)
==Kafka is a **distributed event streaming platform**.== It acts as a durable, high-throughput message bus between services.
==Think of it as a **commit log**: producers append events, consumers read them at their own pace, and the events stick around== (they're not deleted after reading).
```
┌──────────┐         ┌─────────────┐         ┌──────────┐
│ Producer │ ──────► │    KAFKA    │ ──────► │ Consumer │
│ (Order   │  write  │   BROKER    │  read   │ (Billing │
│  Service) │         │             │         │  Service) │
└──────────┘         │  Topic:     │         └──────────┘
                     │  "orders"   │
┌──────────┐         │             │         ┌──────────┐
│ Producer │ ──────► │  Partition 0│ ──────► │ Consumer │
│ (Rx      │         │  Partition 1│         │ (Notifi- │
│  Service) │         │  Partition 2│         │  cation) │
└──────────┘         └─────────────┘         └──────────┘
```

==**Key difference from a traditional queue (like RabbitMQ):**==
- ==Queue: message is **deleted** after one consumer reads it==
- ==Kafka: message is **retained** — multiple consumers can read independently==
---
## 2. Core Concepts
### Topics
==A **topic** is a named stream of events. Think of it as a table in a database, but append-only.==
```
Topic: "prescription-events"
├── Event: { type: "created", rxId: 1001, patient: "Alice", ts: "..." }
├── Event: { type: "filled",  rxId: 1001, pharmacist: "Dr. Patel", ts: "..." }
├── Event: { type: "shipped", rxId: 1001, tracking: "1Z999...", ts: "..." }
└── Event: { type: "created", rxId: 1002, patient: "Bob", ts: "..." }
```
Topics have a **retention period** (default 7 days). Events aren't deleted when consumed — they expire based on time or size.
### Partitions
==A topic is split into **partitions**== — Kafka's unit of parallelism.
```
Topic: "orders" (3 partitions)

Partition 0: [msg0] [msg3] [msg6] [msg9]  ...
Partition 1: [msg1] [msg4] [msg7] [msg10] ...
Partition 2: [msg2] [msg5] [msg8] [msg11] ...
```

**Why partitions matter:**
- Each partition is an **ordered, immutable sequence** of events
- ==Order is guaranteed **within a partition**, NOT across partitions==
- ==More partitions = more parallelism = higher throughput==
- ==A message's **key** determines which partition it goes to==
```
// Messages with the same key always go to the same partition
Key: "patient-42" → hash("patient-42") % 3 = Partition 1
Key: "patient-42" → always Partition 1 (ordering guaranteed for this patient)
Key: "patient-99" → hash("patient-99") % 3 = Partition 0
```

**Interview insight**
=="How do you guarantee message ordering in Kafka?" → "Ordering is guaranteed within a partition. I use a consistent key (like PatientId) so all events for the same patient go to the same partition and arrive in order."==
### Offsets
==Each message in a partition gets a sequential **offset** — its position number.==
```
Partition 0: [offset 0] [offset 1] [offset 2] [offset 3]
                                        ▲
                                   Consumer A is here
                                   (committed offset = 2)
```

Consumers track their position via offsets. If a consumer crashes and restarts, it picks up from its **last committed offset**.
### Consumer Groups
==A **consumer group** is a team of consumers that **share the work** of reading a topic.==
```
Topic: "orders" (3 partitions)
Consumer Group: "billing-service"

  Consumer A → reads Partition 0
  Consumer B → reads Partition 1
  Consumer C → reads Partition 2

Each partition is assigned to exactly ONE consumer in the group.
If Consumer B dies, its partition is reassigned to A or C (rebalancing).
```
**Key rules:**
- ==Each partition is read by **at most one consumer** in a group==
- ==If you have more consumers than partitions, the extras sit idle==
- ==Different consumer groups read **independently** (each tracks its own offsets)==
```
Consumer Group "billing"      → reads "orders" at its own pace
Consumer Group "notifications" → reads "orders" independently
Consumer Group "analytics"     → reads "orders" independently

All three groups see ALL messages — Kafka doesn't delete after reading.
```
### Brokers and the Cluster
A Kafka **cluster** is a set of **brokers** (servers). Each broker stores a subset of partitions.
```
Cluster (3 brokers)

Broker 0: orders-P0 (leader), orders-P1 (replica)
Broker 1: orders-P1 (leader), orders-P2 (replica)
Broker 2: orders-P2 (leader), orders-P0 (replica)
```
- Each partition has one **leader** (handles reads/writes) and N **replicas** (backups)
- If a broker dies, a replica is promoted to leader — no data loss
- **Replication factor** = how many copies of each partition (typically 3 in production)
---
## 3. Producers
### How producing works
``` hl:2-6
Producer sends message
    → Serialized (object → bytes)
    → Partitioner determines partition (based on key hash or round-robin)
    → Message written to partition leader broker
    → Leader replicates to followers
    → Ack sent back to producer
```
### Acknowledgment modes (acks)

| Setting    | Meaning                        | Trade-off                                              |
| ---------- | ------------------------------ | ------------------------------------------------------ |
| `acks=0`   | Don't wait for any ack         | Fastest, but messages can be lost                      |
| `acks=1`   | Wait for leader to write       | Balanced — lost only if leader dies before replication |
| `acks=all` | Wait for ALL replicas to write | Slowest, but **no data loss** — use for critical data  |

**For Empower (healthcare)**
==Always use `acks=all` for anything involving prescriptions or patient data. Data loss in healthcare = compliance violation.==
### Producer in C# (.NET)

```csharp
using Confluent.Kafka;
public class OrderEventProducer
{
    private readonly IProducer _producer;
    public OrderEventProducer(IConfiguration config)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"],
            Acks = Acks.All,                    // no data loss
            EnableIdempotence = true,            // prevent duplicates
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 1000
        };
        _producer = new ProducerBuilder(producerConfig)
            .Build();
    }
    public async Task PublishOrderCreatedAsync(Order order)
    {
        var message = new Message
        {
            Key = order.PatientId.ToString(),   // same patient → same partition
            Value = JsonSerializer.Serialize(new
            {
                EventType = "OrderCreated",
                OrderId = order.Id,
                PatientId = order.PatientId,
                Total = order.Total,
                Timestamp = DateTime.UtcNow
            })
        };
        var result = await _producer.ProduceAsync("order-events", message);
        // result.Offset = the offset assigned to this message
        // result.Partition = which partition it went to
    }
    public void Dispose() => _producer?.Dispose();
}
```

---
## 4. Consumers

### How consuming works

``` hl:1-6
Consumer polls broker for new messages
    → Gets a batch of messages
    → Deserializes (bytes → object)
    → Processes each message
    → Commits offset ("I've processed up to here")
    → Polls again
```
### Consumer in C# (.NET)

```csharp hl:38-39
using Confluent.Kafka;
public class OrderEventConsumer : BackgroundService
{
    private readonly IConsumer _consumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    public OrderEventConsumer(IConfiguration config,
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"],
            GroupId = "billing-service",// consumer group
            AutoOffsetReset = AutoOffsetReset.Earliest, // start from beginning if no offset
            EnableAutoCommit = false// manual commit for reliability
        };
        _consumer = new ConsumerBuilder(consumerConfig)
            .Build();
    }
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _consumer.Subscribe("order-events");
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(ct); // blocks until message arrives
                var eventData = JsonSerializer.Deserialize(
                    result.Message.Value);
                // Process in a new DI scope (for scoped services like DbContext)
                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider
                    .GetRequiredService();
                await handler.HandleAsync(eventData);
                // Commit AFTER successful processing
                _consumer.Commit(result);
                _logger.LogInformation(
                    "Processed {EventType} for order {OrderId}, offset {Offset}",
                    eventData.EventType, eventData.OrderId,
                    result.Offset);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error");
            }
        }
    }
    public override void Dispose()
    {
        _consumer?.Close(); // graceful leave from consumer group
        _consumer?.Dispose();
        base.Dispose();
    }
}
```
### Auto-commit vs Manual commit

| Mode                             | How                                       | Risk                                                                                             |
| -------------------------------- | ----------------------------------------- | ------------------------------------------------------------------------------------------------ |
| Auto-commit                      | Offsets committed on a timer (default 5s) | If consumer crashes after commit but before processing → **message lost**                        |
| Manual commit (after processing) | You call `Commit()` after handling        | If consumer crashes after processing but before commit → **message reprocessed** (at-least-once) |
|                                  |                                           |                                                                                                  |
**Best practice**
==Use **manual commit after processing** for at-least-once delivery. Design your handlers to be **idempotent** — processing the same message twice should produce the same result.==

---
## 5. At-Least-Once vs Exactly-Once vs At-Most-Once

| Guarantee | How | Trade-off |
|---|---|---|
| **At-most-once** | Commit offset BEFORE processing | Fast, but messages can be lost |
| **At-least-once** | Commit offset AFTER processing | Safe, but duplicates possible — **most common choice** |
| **Exactly-once** | Kafka transactions + idempotent producer | Highest guarantee, more complexity and overhead |

### Making handlers idempotent (for at-least-once)

```csharp
public class BillingEventHandler : IOrderEventHandler
{
    private readonly BillingDbContext _db;
    public async Task HandleAsync(OrderEvent evt)
    {
        // Idempotency check: have we already processed this event?
        var exists = await _db.ProcessedEvents
            .AnyAsync(e => e.EventId == evt.EventId);
        if (exists)
            return; // already handled — skip
        // Process the event
        await _db.Invoices.AddAsync(new Invoice
        {
            OrderId = evt.OrderId,
            Amount = evt.Total,
            CreatedAt = DateTime.UtcNow
        });
        // Record that we've processed this event
        await _db.ProcessedEvents.AddAsync(new ProcessedEvent
        {
            EventId = evt.EventId,
            ProcessedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(); // single transaction
    }
}
```

---

## 6. Common Patterns

### Event Sourcing

Instead of storing current state, store the **sequence of events** that led to it.

```
Topic: "patient-42-events"

Offset 0: { type: "PatientCreated", name: "Alice", state: "TX" }
Offset 1: { type: "AddressUpdated", state: "CA" }
Offset 2: { type: "PrescriptionAdded", rxId: 1001 }
Offset 3: { type: "PrescriptionFilled", rxId: 1001 }

Current state = replay all events from the beginning
```

### CQRS (Command Query Responsibility Segregation)

```
Commands (writes) → Kafka topic → Consumer updates Read DB
Queries (reads)   → Read DB (optimized for queries)

Write side: "OrderPlaced" event → Kafka
Read side:  Consumer builds a denormalized view in a read-optimized store
```

### Dead Letter Queue (DLQ)

When a message fails processing repeatedly, move it to a separate topic instead of blocking the entire pipeline.

```csharp
catch (Exception ex)
{
    retryCount++;
    if (retryCount >= 3)
    {
        // Send to dead letter topic for manual investigation
        await _dlqProducer.ProduceAsync("order-events-dlq", new Message
        {
            Key = result.Message.Key,
            Value = result.Message.Value,
            Headers = new Headers
            {
                { "error", Encoding.UTF8.GetBytes(ex.Message) },
                { "original-topic", Encoding.UTF8.GetBytes("order-events") },
                { "retry-count", Encoding.UTF8.GetBytes(retryCount.ToString()) }
            }
        });

        _consumer.Commit(result); // move past the poison message
    }
}
```
### Saga / Choreography Pattern
==Multiple services coordinate through events without a central orchestrator.==
```
1. OrderService publishes  → "OrderCreated"
2. InventoryService hears  → reserves stock → publishes "InventoryReserved"
3. BillingService hears    → charges payment → publishes "PaymentProcessed"
4. FulfillmentService hears → ships order → publishes "OrderShipped"
5. NotificationService hears → emails patient

If BillingService fails → publishes "PaymentFailed"
   → InventoryService hears → releases reserved stock (compensating action)
```

---

## 7. Kafka vs RabbitMQ vs Azure Service Bus

| Feature | Kafka | RabbitMQ | Azure Service Bus |
|---|---|---|---|
| **Model** | Distributed log | Message queue | Message queue + topics |
| **Retention** | Keeps messages (configurable) | Deletes after consumption | Deletes after consumption |
| **Throughput** | Millions/sec | Thousands/sec | Thousands/sec |
| **Ordering** | Per partition | Per queue | Per session |
| **Replay** | Yes — consumers can rewind | No | Limited (dead letter) |
| **Best for** | Event streaming, high volume, replay | Task queues, RPC, routing | Azure-native, enterprise messaging |
| **Complexity** | High (cluster management) | Medium | Low (managed service) |

> [!tip] Interview answer
> "I'd choose Kafka when we need high throughput, event replay, or multiple independent consumers reading the same stream. For simple task queues or request-reply patterns, RabbitMQ or Service Bus is simpler."

---

## 8. Key Configuration to Know

### Producer

```
acks=all                    # Wait for all replicas
enable.idempotence=true     # Prevent duplicate messages on retry
max.in.flight.requests=5    # Max concurrent requests (with idempotence)
compression.type=snappy     # Compress for throughput
linger.ms=5                 # Batch messages for 5ms before sending
batch.size=32768            # Max batch size in bytes
```

### Consumer

```
group.id=billing-service         # Consumer group name
auto.offset.reset=earliest       # Where to start if no committed offset
enable.auto.commit=false         # Manual commit for reliability
max.poll.records=500             # Max messages per poll
session.timeout.ms=30000         # How long before consumer is considered dead
heartbeat.interval.ms=10000      # How often consumer sends heartbeat
```

### Topic

```
partitions=12                    # Number of partitions (can't decrease later!)
replication.factor=3             # Number of copies across brokers
retention.ms=604800000           # 7 days retention
min.insync.replicas=2            # At least 2 replicas must ack (with acks=all)
```

> [!warning] Partition count
> You **cannot decrease** the number of partitions after creation. Start with enough for your expected throughput. Rule of thumb: target throughput / throughput per partition. Over-partitioning wastes resources; under-partitioning limits parallelism.

---

## 9. Quick-Fire Interview Q&A

### "How does Kafka guarantee ordering?"
Ordering is guaranteed **within a single partition only**. Use a consistent message key (e.g., PatientId) to ensure all events for the same entity go to the same partition.
### "What happens when a consumer crashes?"
The consumer group detects the loss via missed heartbeats. A **rebalance** occurs — the dead consumer's partitions are reassigned to surviving consumers. They resume from the last committed offset.
### "How do you handle duplicate messages?"
Use **at-least-once delivery** with **idempotent consumers**. Track processed event IDs in a database and skip duplicates. Alternatively, use Kafka's exactly-once semantics with transactional producers/consumers (more complex).
### "What's a consumer lag?"
The difference between the latest offset in a partition and the consumer's committed offset. High lag means the consumer can't keep up. Monitor it — if lag grows continuously, you need more consumers or faster processing.
### "How do you scale consumers?"
Add more consumers to the consumer group (up to the number of partitions). If you have 12 partitions and 6 consumers, each reads 2 partitions. Add 6 more and each reads 1. Add a 13th and it sits idle.
### "What's compaction?"
A retention policy where Kafka keeps only the **latest value per key**. Instead of expiring messages by time, it deduplicates by key. Used for maintaining current state (e.g., latest config per service, latest address per patient).
### "Kafka Connect vs writing your own producer/consumer?"
Kafka Connect is a framework for **no-code/low-code** data pipelines. Use connectors (JDBC, S3, Elasticsearch) to move data in/out of Kafka without writing code. Write custom producers/consumers when you need business logic during processing.

---
## 10. DI Registration in ASP.NET Core

```csharp
// Program.cs
builder.Services.AddSingleton>(sp =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = builder.Configuration["Kafka:BootstrapServers"],
        Acks = Acks.All,
        EnableIdempotence = true
    };
    return new ProducerBuilder(config).Build();
});
// Register consumer as a hosted service (runs in background)
builder.Services.AddHostedService();
// Register event handlers
builder.Services.AddScoped();
```

> [!tip] Why Singleton for Producer?
> Kafka producers are thread-safe and maintain internal connection pools and batching. Creating a new producer per request wastes resources and kills performance. One producer instance per application is the standard pattern.

----
## Mindmap 1: The Parts

```
KAFKA
├── Producer = the app that SENDS messages
│   └── Sets a key on each message (e.g., PatientId)
│   └── are thread-safe & maintain internal connection pools & batching. Create one producer instance per application.
│
├── Broker = a server that STORES messages,acknowledgement modes are Don't wait for any ack, Wait for leader to write, Wait for ALL replicas to write(most safe)
│   ├── Leader = the one copy that handles traffic
│   └── Follower = backup copies on other servers
│
├── Topic = a named category (e.g., "prescription-events")
│   └── Partition = a lane within the topic
│       └── Offset = each message's position number (0, 1, 2, 3...)
│       └── Ordering is guaranteed within a single partition only. Use a consistent message key (e.g., PatientId) to ensure all events for the same entity go to the same partition.

│
└── Consumer = the app that READS messages, they track their position in partition via offsets
    └── Consumer Group = a team that splits the reading work
    └── if consumer crashes, consumer group detects loss via missed heartbeats and dead consumer's partitions are reassigned to surviving consumers. They resume from the last committed offset
    └── if consumer is unable to keep up with the messages in partition (based on difference between latest offset in partition and the consumer's committed offset) then we will need more consumers or faster processing
    └── we can scale consumers by adding more consumers to the consumer group (up to number of partitions)
    └── call manual commit after processing, design handlers such that processing same message twice produces same result
```

  ## Mindmap 2: The Flow

```
A message's journey:

1. Producer creates a message
       ↓
2. Key gets hashed to pick a partition
   (same patient → always same partition → events stay in order)
       ↓
3. Message sent to that partition's LEADER broker
       ↓
4. Leader copies it to FOLLOWER brokers
       ↓
5. Leader tells producer "got it" (acknowledgment)
   • acks=0  → don't wait     → fast, can lose data
   • acks=1  → leader confirms → fast, rarely loses data
   • acks=all → ALL confirm    → slow, never loses data ✓
       ↓
6. Consumers read it independently
   (Billing, Notifications, Analytics — each at their own pace)
```

  ## Mindmap 3: When Things Break

```
What fails?          What happens?
─────────────        ──────────────────────────────
Leader dies        → A follower gets promoted to leader
                     No data loss if acks=all

Consumer crashes   → Its partitions get reassigned to others
                     They resume from the last saved offset
                     Some messages may get re-delivered
                     → Make your code IDEMPOTENT (check before acting)

Message keeps      → After 3 retries, send it to a
  failing            Dead Letter Queue and move on
```