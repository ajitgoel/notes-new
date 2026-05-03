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

## Interview Questions

1. What is backpressure? Give an example where it matters.
2. Explain the circuit breaker pattern. What are the three states?
3. What's the difference between `publishOn` and `subscribeOn`?
4. When would you use `Schedulers.boundedElastic()` vs `Schedulers.parallel()`?
5. How do you debug a reactive chain? What are checkpoints?
6. What is the bulkhead pattern and how does it protect downstream services?
