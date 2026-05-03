# Backpressure & Resilience

## What is Backpressure?

When a publisher emits faster than the subscriber can consume, you need a strategy:

```java
// Subscriber requests items at its pace
Flux.range(1, 1_000_000)
    .subscribe(new BaseSubscriber<>() {
        @Override
        protected void hookOnSubscribe(Subscription sub) {
            request(10);  // start with 10
        }

        @Override
        protected void hookOnNext(Integer value) {
            process(value);
            request(1);  // ask for 1 more after processing
        }
    });
```

### Backpressure strategies
```java
// Buffer — keep items in memory (risk: OOM)
flux.onBackpressureBuffer(1000)

// Buffer with overflow strategy
flux.onBackpressureBuffer(1000,
    BufferOverflowStrategy.DROP_OLDEST)

// Drop — discard items that can't be consumed
flux.onBackpressureDrop(dropped -> log.warn("dropped: {}", dropped))

// Latest — keep only the most recent
flux.onBackpressureLatest()

// Error — signal error when overwhelmed
flux.onBackpressureError()
```

---

## Rate Limiting Emissions

```java
// Limit throughput
flux.limitRate(100)  // prefetch 100 at a time

// Sample — emit only the latest item per time window
flux.sample(Duration.ofMillis(500))

// Throttle — first item per window
flux.throttleFirst(Duration.ofSeconds(1))

// Debounce — emit only after a quiet period
flux.delayUntil(val -> Mono.delay(Duration.ofMillis(300)))
```

---

## Circuit Breaker with Resilience4j

```java
// Configuration
@Bean
public CircuitBreakerConfig circuitBreakerConfig() {
    return CircuitBreakerConfig.custom()
        .slidingWindowSize(10)
        .failureRateThreshold(50)           // 50% failures → open
        .waitDurationInOpenState(Duration.ofSeconds(30))
        .permittedNumberOfCallsInHalfOpenState(3)
        .recordExceptions(IOException.class, TimeoutException.class)
        .build();
}

// Usage in reactive chain
@Service
public class UserServiceClient {

    private final CircuitBreaker circuitBreaker;
    private final WebClient webClient;

    public Mono<User> getUser(String id) {
        return webClient.get()
            .uri("/users/{id}", id)
            .retrieve()
            .bodyToMono(User.class)
            .transformDeferred(CircuitBreakerOperator.of(circuitBreaker))
            .timeout(Duration.ofSeconds(3))
            .onErrorResume(CallNotPermittedException.class, ex ->
                getCachedUser(id)  // fallback when circuit is open
            );
    }
}
```

### Circuit breaker states
```
CLOSED (normal) → failure rate exceeds threshold → OPEN (reject calls)
                                                      ↓ wait duration
                                                   HALF_OPEN (test calls)
                                                      ↓ success
                                                   CLOSED
```

---

## Bulkhead Pattern

```java
// Limit concurrent calls to protect a downstream service
Bulkhead bulkhead = Bulkhead.of("userService",
    BulkheadConfig.custom()
        .maxConcurrentCalls(25)
        .maxWaitDuration(Duration.ofMillis(500))
        .build());

Mono<User> result = webClient.get().uri("/users/{id}", id)
    .retrieve()
    .bodyToMono(User.class)
    .transformDeferred(BulkheadOperator.of(bulkhead));
```

---

## Schedulers — Controlling Thread Execution

```java
// publishOn — switches downstream operators to a different scheduler
flux.publishOn(Schedulers.boundedElastic())  // for blocking I/O
    .map(val -> blockingDbCall(val))          // runs on elastic

// subscribeOn — switches the ENTIRE chain's subscription
Mono.fromCallable(() -> blockingLegacyApi())
    .subscribeOn(Schedulers.boundedElastic())

// Available schedulers
Schedulers.parallel()        // CPU-bound (cores × 1 threads)
Schedulers.boundedElastic()  // blocking I/O (max 10 × cores threads)
Schedulers.single()          // single reusable thread
Schedulers.immediate()       // current thread
```

**Rule**: Never block on `Schedulers.parallel()`. Wrap blocking calls with `Mono.fromCallable(...).subscribeOn(Schedulers.boundedElastic())`.

---

## Debugging Reactive Chains

```java
// Enable debug mode (expensive — development only)
Hooks.onOperatorDebug();

// Checkpoint — adds a label to the assembly trace
flux.checkpoint("after-user-lookup")

// Log operator — prints signals
mono.log("user-service")
    // Prints: onSubscribe, request, onNext, onComplete/onError

// Metrics (Micrometer integration)
mono.name("user.lookup")
    .tag("service", "user-api")
    .metrics()
```

---

## Interview Questions & Answers

### 1. What is backpressure? Give an example where it matters.

Backpressure is a mechanism where the consumer tells the producer how fast to emit data. Without it, a fast producer overwhelms a slow consumer, causing memory exhaustion or dropped data.

**Example**: A database query returns 1 million rows as a `Flux<Row>`. Each row needs to be transformed and written to a file. The database can emit rows at 100K/sec, but file I/O processes at 10K/sec. Without backpressure, 90K rows/sec accumulate in memory → `OutOfMemoryError`.

With backpressure, the file writer requests items as it can process them:
```java
databaseFlux
    .onBackpressureBuffer(1000) // Buffer up to 1000, then drop oldest
    .flatMap(row -> writeToFile(row), 10) // Max 10 concurrent writes
```

In the Reactive Streams spec, this is implemented via `Subscription.request(n)` — the subscriber tells the publisher exactly how many items it can handle. Operators like `flatMap`, `buffer`, and `limitRate` manage this automatically.

### 2. Explain the circuit breaker pattern. What are the three states?

A circuit breaker wraps calls to an unreliable service. Instead of repeatedly calling a failing service (wasting resources and increasing latency), it "trips" and fails fast.

**Three states**:

1. **CLOSED** (normal): All calls pass through. The circuit breaker monitors failures. When the failure rate exceeds a threshold (e.g., 50% of the last 10 calls), it transitions to OPEN.

2. **OPEN** (tripped): All calls are immediately rejected with `CallNotPermittedException` — no actual call is made. This protects the failing service from being overwhelmed and gives it time to recover. After a configured wait duration (e.g., 30 seconds), transitions to HALF_OPEN.

3. **HALF_OPEN** (testing): A limited number of test calls (e.g., 3) are allowed through. If they succeed, the circuit transitions back to CLOSED. If they fail, it returns to OPEN.

```java
CircuitBreakerConfig.custom()
    .slidingWindowSize(10)           // Evaluate last 10 calls
    .failureRateThreshold(50)        // 50% failures → OPEN
    .waitDurationInOpenState(Duration.ofSeconds(30))  // Wait before testing
    .permittedNumberOfCallsInHalfOpenState(3)         // 3 test calls
```

In reactive code, wrap with `transformDeferred(CircuitBreakerOperator.of(cb))` and handle `CallNotPermittedException` with `onErrorResume` to return cached data.

### 3. What's the difference between `publishOn` and `subscribeOn`?

**`publishOn(scheduler)`**: Switches the execution of ALL downstream operators to the specified scheduler. The operators ABOVE `publishOn` run on the original thread; operators BELOW run on the new scheduler.

```java
flux
    .map(x -> transform(x))           // Runs on original thread
    .publishOn(Schedulers.parallel())
    .map(x -> cpuIntensive(x))        // Runs on parallel scheduler
    .publishOn(Schedulers.boundedElastic())
    .map(x -> blockingIO(x))          // Runs on elastic scheduler
```

**`subscribeOn(scheduler)`**: Switches the execution of the ENTIRE subscription (from source to the point of `subscribeOn`) to the specified scheduler. Placement doesn't matter — it always affects the source.

```java
Mono.fromCallable(() -> blockingDbCall())  // Runs on elastic scheduler
    .subscribeOn(Schedulers.boundedElastic())
    .map(x -> transform(x))                // Also runs on elastic
```

**Key difference**: `publishOn` affects downstream, `subscribeOn` affects upstream (the source). You can have multiple `publishOn` calls in a chain (each switches downstream), but only the first `subscribeOn` takes effect.

### 4. When would you use `Schedulers.boundedElastic()` vs `Schedulers.parallel()`?

**`Schedulers.parallel()`**: Fixed pool of `CPU cores` threads. Designed for CPU-bound work (computation, transformation, serialization). **Never block on this scheduler** — blocking one of 8 threads means 12.5% capacity loss.

**`Schedulers.boundedElastic()`**: Elastic pool of up to `10 × CPU cores` threads, with a queue for excess tasks. Designed for blocking I/O operations (JDBC calls, file I/O, legacy synchronous APIs). Threads are cached and reused, created on demand, and destroyed after 60 seconds of idle time.

```java
// CPU work: use parallel
flux.publishOn(Schedulers.parallel())
    .map(data -> encrypt(data))

// Blocking I/O: use boundedElastic
Mono.fromCallable(() -> jdbcTemplate.query(...))
    .subscribeOn(Schedulers.boundedElastic())
```

**Rule**: If the operation blocks a thread (JDBC, file I/O, `Thread.sleep`, synchronous HTTP), use `boundedElastic`. If it's pure computation, use `parallel`. If it's already non-blocking (WebClient, R2DBC), you don't need to switch schedulers — the event loop handles it.

### 5. How do you debug a reactive chain? What are checkpoints?

Reactive stack traces are notoriously unhelpful — they show the framework internals, not your code. Several tools help:

**`Hooks.onOperatorDebug()`**: Enables assembly-time stack traces for ALL operators. Shows where each operator was created (your code), not just where it executed. Expensive — 10-20% performance overhead. Development only.

**`checkpoint("label")`**: Lightweight alternative. Adds a named marker to the assembly trace. When an error occurs, the checkpoint appears in the stack trace:

```java
userService.findById(id)
    .checkpoint("after-user-lookup")
    .flatMap(user -> orderService.findByUserId(user.getId()))
    .checkpoint("after-order-lookup")
```

If `orderService` fails, the stack trace shows `"after-order-lookup"` — you know exactly where in the chain the error occurred.

**`.log("name")`**: Prints all signals (subscribe, request, next, complete, error) to the logger. Useful for understanding what's happening in a specific part of the chain.

**Micrometer metrics**: `.name("operation").metrics()` emits timing and error metrics. Production-safe, unlike debug hooks.

### 6. What is the bulkhead pattern and how does it protect downstream services?

The bulkhead pattern limits concurrent access to a downstream service, preventing one slow/failing service from consuming all resources and cascading failures to others.

Named after ship bulkheads — compartments that prevent a hull breach from sinking the entire ship. If the user service is slow, a bulkhead ensures it can only consume 25 concurrent connections, leaving resources available for order service, product service, etc.

```java
Bulkhead bulkhead = Bulkhead.of("userService",
    BulkheadConfig.custom()
        .maxConcurrentCalls(25)        // Max 25 calls at once
        .maxWaitDuration(Duration.ofMillis(500))  // Wait 500ms for a slot, then reject
        .build());
```

Without a bulkhead: if the user service hangs, all 200 threads (or all event loop capacity) pile up waiting for it, and no other service can be called. With a bulkhead: at most 25 calls wait for user service; the remaining capacity serves other requests normally.

Bulkheads work alongside circuit breakers: the bulkhead limits concurrency (prevents resource exhaustion), while the circuit breaker detects failure patterns (prevents repeated calls to a broken service). Use both together for resilient service orchestration.
