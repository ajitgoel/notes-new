# Reactive Operators & Composition

## Combining Publishers

### zip — combine latest from each, wait for all
```java
// Both Monos must complete. Result combines them.
Mono<Dashboard> dashboard = Mono.zip(
    userService.getUser(userId),
    orderService.getOrders(userId),
    statsService.getStats(userId)
).map(tuple -> new Dashboard(tuple.getT1(), tuple.getT2(), tuple.getT3()));

// With combinator function
Mono<String> greeting = Mono.zip(
    getFirstName(id),
    getLastName(id),
    (first, last) -> "Hello, " + first + " " + last
);
```

### zipWith — combine two publishers
```java
Mono<UserWithOrders> result = userMono
    .zipWith(ordersMono, (user, orders) -> new UserWithOrders(user, orders));
```

### merge — interleave emissions (unordered)
```java
// All sources emit to a single Flux, interleaved
Flux<Notification> all = Flux.merge(
    emailNotifications,   // may emit first
    smsNotifications,     // or this
    pushNotifications     // or this
);
```

### concat — sequential emissions (ordered)
```java
// Second source starts ONLY after first completes
Flux<Product> allProducts = Flux.concat(
    localCache.getProducts(),     // try cache first
    remoteService.getProducts()   // then remote
);
```

### firstWithValue — race, take the fastest
```java
// Returns result from whichever source responds first
Mono<Data> fastest = Mono.firstWithValue(
    primaryService.getData(id),
    fallbackService.getData(id)
);
```

---

## Error Handling Operators

```java
mono
    // Recover with a fallback value
    .onErrorReturn("default value")

    // Recover with another Mono
    .onErrorResume(ex -> {
        if (ex instanceof TimeoutException) {
            return cacheService.getCached(id);
        }
        return Mono.error(ex); // re-throw others
    })

    // Recover from specific exception type
    .onErrorResume(NotFoundException.class, ex -> Mono.empty())

    // Transform error type
    .onErrorMap(IOException.class, ex ->
        new ServiceException("Downstream failed", ex))

    // Retry
    .retry(3)  // retry up to 3 times on any error

    // Retry with backoff
    .retryWhen(Retry.backoff(3, Duration.ofMillis(100))
        .maxBackoff(Duration.ofSeconds(2))
        .filter(ex -> ex instanceof TransientException)
        .onRetryExhaustedThrow((spec, signal) ->
            new ServiceException("Retries exhausted", signal.failure()))
    )

    // Timeout
    .timeout(Duration.ofSeconds(5))
    .timeout(Duration.ofSeconds(5), fallbackMono)
```

---
## Context — Thread-Local Replacement
Reactive chains jump threads. `ThreadLocal` doesn't work. Use `Context`.

```java
// Writing context
Mono<String> result = someOperation()
    .contextWrite(Context.of("traceId", "abc-123", "userId", "user-1"));

// Reading context
Mono<String> withTrace = Mono.deferContextual(ctx -> {
    String traceId = ctx.get("traceId");
    return callService(traceId);
});

// In operators
mono.flatMap(val ->
    Mono.deferContextual(ctx -> {
        log.info("traceId={}", ctx.getOrDefault("traceId", "none"));
        return process(val);
    })
);
```

---

## Testing with StepVerifier

```java
// Verify Mono
StepVerifier.create(userService.findById("1"))
    .expectNextMatches(user -> user.getName().equals("Alice"))
    .verifyComplete();

// Verify Flux
StepVerifier.create(productService.findAll())
    .expectNextCount(5)
    .verifyComplete();

// Verify errors
StepVerifier.create(userService.findById("nonexistent"))
    .expectError(NotFoundException.class)
    .verify();

// Verify with virtual time (for delays, intervals)
StepVerifier.withVirtualTime(() ->
        Flux.interval(Duration.ofHours(1)).take(3))
    .thenAwait(Duration.ofHours(3))
    .expectNextCount(3)
    .verifyComplete();

// Verify context
StepVerifier.create(
        mono.contextWrite(Context.of("key", "value")))
    .expectNext("expected")
    .verifyComplete();
```

---

## Interview Questions & Answers
### 1. When would you use `zip` vs `merge` vs `concat`?
==**`zip`**: Use when you need results from ALL sources before proceeding, and they're independent. Executes in parallel, waits for all to complete, combines them.== Example: fetching user profile + user preferences + user stats to build a dashboard.
==**`merge`**: Use when you want results== from multiple sources interleaved, ==order doesn't matter.== All sources emit to a single Flux concurrently. ==Example: combining notification streams from email, SMS, and push — you want to process them as they arrive.==
==**`concat`**: Use when order matters and sources should be processed sequentially. Second source only starts after first completes.== Example: try local cache first, then hit remote API only if cache misses. Or processing log files in chronological order.

```java
// zip: wait for all, combine
Mono.zip(userMono, ordersMono, recsMono).map(...)

// merge: interleave, fastest first
Flux.merge(emailNotifs, smsNotifs, pushNotifs)

// concat: sequential, ordered
Flux.concat(cacheResults, remoteResults)
```

### 2. How do you implement fallback logic in a reactive chain? Compare `onErrorReturn` vs `onErrorResume`.

==**`onErrorReturn(value)`**: Returns a static fallback value on any error.== Simple but inflexible — you can't inspect the error or produce the fallback reactively.
```java
mono.onErrorReturn("default") // Any error → "default"
mono.onErrorReturn(IOException.class, "io-default") // Only IOException
```

==**`onErrorResume(fn)`**:== Returns a fallback Publisher based on the error. More powerful — ==you can inspect the error,== call another service==, or re-throw selectively.==
```java
mono.onErrorResume(ex -> {
    if (ex instanceof TimeoutException) {
        return cacheService.getCached(id); // Fallback to cache
    }
    if (ex instanceof NotFoundException) {
        return Mono.empty(); // Swallow gracefully
    }
    return Mono.error(ex); // Re-throw everything else
})
```

==**Rule**: Use `onErrorReturn` for simple static defaults. Use `onErrorResume` when the fallback depends on the error type or requires another async call.==
Also useful: `onErrorMap(ex -> new WrappedException(ex))` to transform the error type without recovering, and `timeout(duration, fallbackMono)` which combines timeout + fallback in one operator.

### 3. Why can't you use ThreadLocal in reactive code? What replaces it?

In Spring MVC, each request runs on a single thread from start to finish, so `ThreadLocal` (used for SecurityContext, MDC logging, transaction state) works. In reactive code, a single request's pipeline hops across multiple threads — `map()` might run on thread A, `flatMap()` on thread B (from a different scheduler), and `doOnNext()` on thread C. ThreadLocal values set on thread A aren't visible on thread B.

**Reactor Context** replaces ThreadLocal:

```java
// Write context (downstream to upstream — called near the end of the chain)
mono.contextWrite(Context.of("traceId", "abc-123"))

// Read context (inside operators)
Mono.deferContextual(ctx -> {
    String traceId = ctx.get("traceId");
    return callService(traceId);
})
```

Context flows **upstream** (from subscriber to publisher), which is the opposite of data flow. This means `contextWrite()` placed at the end of a chain is visible to all upstream operators. It's immutable and thread-safe — each operator sees a consistent snapshot.

For MDC logging, use Micrometer's context propagation or Reactor's `Hooks.enableAutomaticContextPropagation()` to bridge between Context and ThreadLocal.

### 4. How does `retryWhen` with backoff work? How do you avoid retrying non-transient errors?

`retryWhen` accepts a `Retry` spec that controls retry behavior:

```java
mono.retryWhen(Retry.backoff(3, Duration.ofMillis(100))
    .maxBackoff(Duration.ofSeconds(2))
    .jitter(0.5)
    .filter(ex -> ex instanceof IOException || ex instanceof TimeoutException)
    .doBeforeRetry(signal -> log.warn("Retry #{}", signal.totalRetries() + 1))
    .onRetryExhaustedThrow((spec, signal) ->
        new ServiceException("Retries exhausted", signal.failure()))
)
```

- **`backoff(3, 100ms)`**: Up to 3 retries with exponential backoff: 100ms → 200ms → 400ms
- **`maxBackoff`**: Caps the delay so it doesn't grow unbounded
- **`jitter(0.5)`**: Randomizes delay ±50% to prevent thundering herd (all retries hitting the server simultaneously)
- **`filter`**: Only retry matching errors. 4xx client errors, `IllegalArgumentException`, validation errors — these won't be fixed by retrying, so exclude them
- **`onRetryExhaustedThrow`**: Custom exception when all retries fail (default wraps in `RetryExhaustedException`)

Without `filter`, ALL errors trigger retries — including permanent failures like 404 or authentication errors. Always filter to transient errors only.

### 5. Explain `StepVerifier` and virtual time — when do you need it?

`StepVerifier` is Reactor's testing tool that subscribes to a Publisher and asserts on the emitted signals step by step:

```java
StepVerifier.create(flux)
    .expectNext("a")
    .expectNext("b")
    .expectNextMatches(s -> s.startsWith("c"))
    .expectComplete()
    .verify(); // Blocks until complete or timeout
```

**Virtual time** solves testing delays and intervals. If you test `Flux.interval(Duration.ofHours(1)).take(24)`, real time would take 24 hours. Virtual time simulates the clock:

```java
StepVerifier.withVirtualTime(() -> Flux.interval(Duration.ofHours(1)).take(3))
    .thenAwait(Duration.ofHours(3)) // "Fast forward" 3 hours
    .expectNextCount(3)
    .verifyComplete();
```

`withVirtualTime` replaces the scheduler with a virtual clock. `thenAwait()` advances the clock without actually waiting. This makes time-dependent tests fast and deterministic.

**Use virtual time for**: `Flux.interval()`, `Mono.delay()`, `delayElements()`, `timeout()`, or any operator that depends on wall-clock time.
