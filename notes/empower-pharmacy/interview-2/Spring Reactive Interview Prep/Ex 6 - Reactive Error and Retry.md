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
            // TODO: Handle these cases:
            // - 404 → return Mono.empty() (not an error)
            // - 400 → wrap in ValidationException (don't retry)
            // - 429 → wrap in RateLimitException (retry with backoff)
            // - 500 → wrap in ServiceException (retry immediately)
            // - 503 → wrap in ServiceUnavailableException (retry with longer backoff)
            // - Timeout → retry 2 times, then fallback to cache
            // - Unknown → log and return generic error
            ;
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
    // TODO: Implement cascading timeouts
    // Total budget: 5 seconds
    // Service A (user profile): max 2s, required
    // Service B (orders): max 3s, optional (empty list fallback)
    // Service C (recommendations): max 1s, optional (empty fallback)
    //
    // Track elapsed time and reduce downstream timeouts accordingly
    // Hint: use Mono.zip with individual timeouts
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

    public Mono<User> getUser(String id) {
        // Normal flow: call API with circuit breaker
        // When circuit is OPEN: return cached user
        // When cache miss during OPEN: return minimal User stub
        // Log circuit state transitions
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
    // TODO: Chain all fallback levels
    // Each level should have its own timeout
    // Log which level served the response
}
```

---

## Task 6: Test Error Scenarios

```java
// TODO: Write StepVerifier tests for each scenario

@Test
void retriesOnServerError_thenSucceeds() {
    // Mock: fail twice with 500, succeed on 3rd
    // Verify: result is returned, 3 total calls made
}

@Test
void doesNotRetryOn400() {
    // Mock: fail with 400
    // Verify: error immediately, only 1 call made
}

@Test
void circuitBreaker_opensAfterThreshold() {
    // Mock: fail 6 out of 10 calls
    // Verify: circuit opens, subsequent calls get fallback
}

@Test
void timeoutFallsBackToCache() {
    // Mock: primary hangs for 10 seconds
    // Verify: timeout at 2s, cache value returned
}

@Test
void fallbackChain_usesStaleCache() {
    // Mock: primary, secondary, and cache all fail
    // Verify: stale cache value returned
}
```

---

## Solution

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
<summary>Task 5: Fallback chain (click to reveal)</summary>

```java
public Mono<ProductData> getProductData(String id) {
    return primaryApi.getProduct(id)
        .timeout(Duration.ofSeconds(2))
        .doOnNext(p -> log.info("Served from primary"))
        .onErrorResume(ex -> {
            log.warn("Primary failed: {}", ex.getMessage());
            return secondaryApi.getProduct(id)
                .timeout(Duration.ofSeconds(3))
                .doOnNext(p -> log.info("Served from secondary"));
        })
        .onErrorResume(ex -> {
            log.warn("Secondary failed: {}", ex.getMessage());
            return cache.get(id)
                .doOnNext(p -> log.info("Served from cache"));
        })
        .switchIfEmpty(Mono.defer(() -> {
            log.warn("Cache miss, trying stale cache");
            return staleCache.get(id)
                .doOnNext(p -> log.info("Served from stale cache"));
        }))
        .switchIfEmpty(Mono.defer(() -> {
            log.warn("All sources exhausted, using default");
            return Mono.just(ProductData.defaultFor(id));
        }));
}
```
</details>
