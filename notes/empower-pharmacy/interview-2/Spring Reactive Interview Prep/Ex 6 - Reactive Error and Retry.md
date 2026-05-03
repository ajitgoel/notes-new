# Exercise 6: Reactive Error Handling & Retry Patterns

## Scenario

You're building a reactive order service that calls three unreliable downstream APIs. Build robust error handling with retries, circuit breakers, timeouts, and fallbacks.

---

## Task 1: Categorized Error Handling

```java
// TODO: Implement a service that handles different error types differently

public class OrderService {

public Mono<Order> getOrder(String id) {
    return orderApiClient.fetchOrder(id)
        .onErrorResume(WebClientResponseException.class, ex -> {
            return switch (ex.getStatusCode().value()) {
                case 404 -> Mono.empty();
                case 400 -> Mono.error(new ValidationException("Invalid request", ex));
                case 429 -> Mono.error(new RateLimitException("Rate limit hit", ex));
                case 500 -> Mono.error(new ServiceException("Internal server error", ex));
                case 503 -> Mono.error(new ServiceUnavailableException("Service down", ex));
                default -> Mono.error(new GenericException("Unknown API error", ex));
            };
        })
        .retryWhen(Retry.backoff(3, Duration.ofMillis(100))
            .filter(ex -> ex instanceof RateLimitException))
        .retryWhen(Retry.max(3)
            .filter(ex -> ex instanceof ServiceException))
        .retryWhen(Retry.backoff(3, Duration.ofSeconds(1))
            .filter(ex -> ex instanceof ServiceUnavailableException))
        .timeout(Duration.ofSeconds(2))
        .onErrorResume(TimeoutException.class, ex -> cache.get(id))
        .doOnError(ex -> log.error("Unhandled error for order {}", id, ex));
}
}
```

---

## Task 2: Smart Retry Strategy

```java
// TODO: Implement a configurable retry strategy

public Mono<Response> callWithRetry(Mono<Response> apiCall) {
    // Requirements:
    // - Retry up to 3 times
    // - Exponential backoff: 100ms, 200ms, 400ms
    // - Add jitter (±50ms) to prevent thundering herd
    // - Only retry on transient errors (5xx, timeout, IOException)
    // - Do NOT retry on 4xx errors
    // - Log each retry attempt with attempt number and error
    // - If exhausted, throw RetryExhaustedException with original cause
}
```

---

## Task 3: Timeout Cascade

```java
// Problem: You call 3 services. Total budget is 5 seconds.
// If service A takes 3 seconds, services B and C only have 2 seconds left.

public Mono<Dashboard> buildDashboard(String userId) {
    return Mono.zip(
        serviceA.getProfile(userId).timeout(Duration.ofSeconds(2)),
        serviceB.getOrders(userId).timeout(Duration.ofSeconds(3)).onErrorReturn(Collections.emptyList()),
        serviceC.getRecs(userId).timeout(Duration.ofSeconds(1)).onErrorReturn(Collections.emptyList())
    )
    .timeout(Duration.ofSeconds(5))
    .map(tuple -> new Dashboard(tuple.getT1(), tuple.getT2(), tuple.getT3()))
    .onErrorResume(TimeoutException.class, ex -> Mono.error(new GlobalTimeoutException()));
}
```

---

## Task 4: Circuit Breaker Integration

```java
// TODO: Wrap each downstream service with a circuit breaker

@Service
public class ResilientUserClient {

    // Configure circuit breaker:
    // - Window: 10 calls
    // - Failure threshold: 50%
    // - Open duration: 30 seconds
    // - Half-open test calls: 3

private final CircuitBreaker circuitBreaker = CircuitBreaker.of("userClient", 
    CircuitBreakerConfig.custom()
        .slidingWindowSize(10)
        .failureRateThreshold(50)
        .waitDurationInOpenState(Duration.ofSeconds(30))
        .permittedNumberOfCallsInHalfOpenState(3)
        .build());

public Mono<User> getUser(String id) {
    return userClient.call(id)
        .transformDeferred(CircuitBreakerOperator.of(circuitBreaker))
        .onErrorResume(CallNotPermittedException.class, ex -> {
            log.warn("Circuit OPEN, serving from cache for {}", id);
            return cache.get(id).switchIfEmpty(Mono.just(User.stub(id)));
        })
        .doOnNext(u -> log.info("Successfully fetched user {}", id))
        .doOnError(ex -> log.error("Failed to fetch user {}", id, ex));
}
}
```

---

## Task 5: Fallback Chain

```java
// Implement a multi-level fallback:
// 1. Try primary API
// 2. If fails → try secondary API (different region)
// 3. If fails → try local cache
// 4. If cache miss → try stale cache (expired data is better than no data)
// 5. If nothing → return sensible default

public Mono<ProductData> getProductData(String id) {
    return primaryApi.getProduct(id)
        .timeout(Duration.ofSeconds(2))
        .doOnNext(p -> log.info("Served from primary: {}", id))
        .onErrorResume(ex -> {
            log.warn("Primary failed, trying secondary: {}", id);
            return secondaryApi.getProduct(id)
                .timeout(Duration.ofSeconds(3))
                .doOnNext(p -> log.info("Served from secondary: {}", id));
        })
        .onErrorResume(ex -> {
            log.warn("Secondary failed, trying cache: {}", id);
            return cache.get(id)
                .doOnNext(p -> log.info("Served from cache: {}", id));
        })
        .switchIfEmpty(Mono.defer(() -> {
            log.warn("Cache miss, trying stale cache: {}", id);
            return staleCache.get(id)
                .doOnNext(p -> log.info("Served from stale cache: {}", id));
        }))
        .switchIfEmpty(Mono.defer(() -> {
            log.warn("All sources exhausted, using default for {}", id);
            return Mono.just(ProductData.defaultFor(id));
        }));
}
```

---

## Task 6: Test Error Scenarios

```java
// TODO: Write StepVerifier tests for each scenario

@Test
void retriesOnServerError_thenSucceeds() {
    AtomicInteger attempts = new AtomicInteger();
    Mono<String> producer = Mono.defer(() -> {
        if (attempts.incrementAndGet() < 3) return Mono.error(new RuntimeException("500"));
        return Mono.just("OK");
    });

    StepVerifier.create(producer.retry(3))
        .expectNext("OK")
        .verifyComplete();
}

@Test
void doesNotRetryOn400() {
    AtomicInteger attempts = new AtomicInteger();
    Mono<String> producer = Mono.defer(() -> {
        attempts.incrementAndGet();
        return Mono.error(new WebClientResponseException(400, "Bad Request", null, null, null));
    });

    StepVerifier.create(callWithRetry(producer.cast(Response.class)))
        .expectError(RetryExhaustedException.class)
        .verify();
    
    assertEquals(1, attempts.get());
}

@Test
void timeoutFallsBackToCache() {
    Mono<String> producer = Mono.delay(Duration.ofSeconds(10)).thenReturn("Late");
    Mono<String> cache = Mono.just("Cached");

    StepVerifier.create(producer.timeout(Duration.ofSeconds(1)).onErrorResume(TimeoutException.class, e -> cache))
        .expectNext("Cached")
        .verifyComplete();
}
```

---

## Solution

<details>
<summary>Task 1: Categorized errors (click to reveal)</summary>

```java
public Mono<Order> getOrder(String id) {
    return orderApiClient.fetchOrder(id)
        .onErrorResume(WebClientResponseException.class, ex -> {
            return switch (ex.getStatusCode().value()) {
                case 404 -> Mono.empty();
                case 400 -> Mono.error(new ValidationException("Invalid request", ex));
                case 429 -> Mono.error(new RateLimitException("Rate limit hit", ex));
                case 500 -> Mono.error(new ServiceException("Internal server error", ex));
                case 503 -> Mono.error(new ServiceUnavailableException("Service down", ex));
                default -> Mono.error(new GenericException("Unknown API error", ex));
            };
        })
        .retryWhen(Retry.backoff(3, Duration.ofMillis(100))
            .filter(ex -> ex instanceof RateLimitException))
        .retryWhen(Retry.max(3)
            .filter(ex -> ex instanceof ServiceException))
        .retryWhen(Retry.backoff(3, Duration.ofSeconds(1))
            .filter(ex -> ex instanceof ServiceUnavailableException))
        .timeout(Duration.ofSeconds(2))
        .onErrorResume(TimeoutException.class, ex -> cache.get(id));
}
```
</details>

<details>
<summary>Task 2: Smart retry (click to reveal)</summary>

```java
public Mono<Response> callWithRetry(Mono<Response> apiCall) {
    return apiCall.retryWhen(Retry.backoff(3, Duration.ofMillis(100))
        .maxBackoff(Duration.ofMillis(500))
        .jitter(0.5)
        .filter(ex ->
            ex instanceof IOException ||
            ex instanceof TimeoutException ||
            (ex instanceof WebClientResponseException wce &&
                wce.getStatusCode().is5xxServerError())
        )
        .doBeforeRetry(signal ->
            log.warn("Retry attempt {} for error: {}",
                signal.totalRetries() + 1,
                signal.failure().getMessage())
        )
        .onRetryExhaustedThrow((spec, signal) ->
            new RetryExhaustedException(
                "All retries exhausted", signal.failure())
        )
    );
}
```
</details>

<details>
<summary>Task 3: Timeout Cascade (click to reveal)</summary>

```java
public Mono<Dashboard> buildDashboard(String userId) {
    return Mono.zip(
        serviceA.getProfile(userId).timeout(Duration.ofSeconds(2)),
        serviceB.getOrders(userId).timeout(Duration.ofSeconds(3)).onErrorReturn(Collections.emptyList()),
        serviceC.getRecs(userId).timeout(Duration.ofSeconds(1)).onErrorReturn(Collections.emptyList())
    )
    .timeout(Duration.ofSeconds(5))
    .map(tuple -> new Dashboard(tuple.getT1(), tuple.getT2(), tuple.getT3()));
}
```
</details>

<details>
<summary>Task 4: Circuit Breaker (click to reveal)</summary>

```java
public Mono<User> getUser(String id) {
    return userClient.call(id)
        .transformDeferred(CircuitBreakerOperator.of(circuitBreaker))
        .onErrorResume(CallNotPermittedException.class, ex -> 
            cache.get(id).switchIfEmpty(Mono.just(User.stub(id))));
}
```
</details>

<details>
<summary>Task 5: Fallback chain (click to reveal)</summary>

```java
public Mono<ProductData> getProductData(String id) {
    return primaryApi.getProduct(id)
        .timeout(Duration.ofSeconds(2))
        .doOnNext(p -> log.info("Served from primary"))
        .onErrorResume(ex -> {
            log.warn("Primary failed, trying secondary");
            return secondaryApi.getProduct(id)
                .timeout(Duration.ofSeconds(3));
        })
        .onErrorResume(ex -> {
            log.warn("Secondary failed, trying cache");
            return cache.get(id);
        })
        .switchIfEmpty(Mono.defer(() -> {
            log.warn("Cache miss, trying stale cache");
            return staleCache.get(id);
        }))
        .switchIfEmpty(Mono.defer(() -> {
            log.warn("All sources exhausted, using default");
            return Mono.just(ProductData.defaultFor(id));
        }));
}
```
</details>
