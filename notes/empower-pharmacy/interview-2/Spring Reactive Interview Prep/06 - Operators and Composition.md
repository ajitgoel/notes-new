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

## Interview Questions

1. When would you use `zip` vs `merge` vs `concat`?
2. How do you implement fallback logic in a reactive chain? Compare `onErrorReturn` vs `onErrorResume`.
3. Why can't you use ThreadLocal in reactive code? What replaces it?
4. How does `retryWhen` with backoff work? How do you avoid retrying non-transient errors?
5. Explain `StepVerifier` and virtual time — when do you need it?
