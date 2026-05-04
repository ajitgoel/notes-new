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

## Complete Solutions

### Task 1: Categorized Error Handling

```java
public class OrderService {
// TODO: Implement a service that handles different error types differently
    private final OrderApiClient orderApiClient;
    private final OrderCacheService cache;

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
            .onErrorResume(WebClientResponseException.class, ex -> {
                int status = ex.getStatusCode().value();
                return switch (status) {
                    case 404 -> Mono.empty();
                    case 400 -> Mono.error(new ValidationException(
                        "Invalid request: " + ex.getResponseBodyAsString()));
                    case 429 -> Mono.error(new RateLimitException("Rate limited"));
                    case 500 -> Mono.error(new ServiceException("Server error"));
                    case 503 -> Mono.error(new ServiceUnavailableException(
                        "Service unavailable"));
                    default -> {
                        log.error("Unexpected status {}: {}", status, ex.getMessage());
                        yield Mono.error(new ServiceException("Unexpected error"));
                    }
                };
            })
            .retryWhen(Retry.backoff(2, Duration.ofMillis(200))
                .filter(ex -> ex instanceof ServiceException
                    || ex instanceof RateLimitException
                    || ex instanceof ServiceUnavailableException)
                .doBeforeRetry(signal -> log.warn(
                    "Retry {} for: {}", signal.totalRetries() + 1,
                    signal.failure().getMessage()))
            )
            .timeout(Duration.ofSeconds(3))
            .onErrorResume(TimeoutException.class, ex -> {
                log.warn("Timeout fetching order {}, trying cache", id);
                return cache.getOrder(id);
            });
    }
}
```

### Task 2: Smart Retry Strategy

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

### Task 3: Timeout Cascade

```java
public Mono<Dashboard> buildDashboard(String userId) {
    long startTime = System.currentTimeMillis();
    Duration totalBudget = Duration.ofSeconds(5);

    // Service A: required, max 2s
    Mono<UserProfile> userMono = userClient.getProfile(userId)
        .timeout(Duration.ofSeconds(2));

    return userMono.flatMap(user -> {
        long elapsed = System.currentTimeMillis() - startTime;
        Duration remaining = totalBudget.minusMillis(elapsed);

        // Service B: optional, max 3s but capped by remaining budget
        Duration orderTimeout = Duration.ofMillis(
            Math.min(3000, remaining.toMillis()));
        Mono<List<Order>> ordersMono = orderClient.getOrders(userId)
            .collectList()
            .timeout(orderTimeout)
            .onErrorReturn(List.of());

        // Service C: optional, max 1s but capped by remaining budget
        Duration recTimeout = Duration.ofMillis(
            Math.min(1000, remaining.toMillis()));
        Mono<List<Product>> recsMono = recClient.getForUser(userId)
            .collectList()
            .timeout(recTimeout)
            .onErrorReturn(List.of());

        return Mono.zip(ordersMono, recsMono,
            (orders, recs) -> new Dashboard(user, orders, recs));
    });
}
```

### Task 4: Circuit Breaker Integration

```java
@Service
@Slf4j
public class ResilientUserClient {

    private final WebClient webClient;
    private final UserCacheService cache;
    private final CircuitBreaker circuitBreaker;

    public ResilientUserClient(WebClient.Builder builder,
                                UserCacheService cache) {
        this.webClient = builder.baseUrl("http://user-service").build();
        this.cache = cache;
        this.circuitBreaker = CircuitBreaker.of("userService",
            CircuitBreakerConfig.custom()
                .slidingWindowSize(10)
                .failureRateThreshold(50)
                .waitDurationInOpenState(Duration.ofSeconds(30))
                .permittedNumberOfCallsInHalfOpenState(3)
                .recordExceptions(IOException.class,
                    TimeoutException.class, ServiceException.class)
                .build()
        );

        circuitBreaker.getEventPublisher()
            .onStateTransition(event ->
                log.info("Circuit breaker state: {} → {}",
                    event.getStateTransition().getFromState(),
                    event.getStateTransition().getToState()));
    }

    public Mono<User> getUser(String id) {
        return webClient.get()
            .uri("/users/{id}", id)
            .retrieve()
            .bodyToMono(User.class)
            .timeout(Duration.ofSeconds(3))
            .transformDeferred(CircuitBreakerOperator.of(circuitBreaker))
            .doOnNext(user -> cache.put(id, user))  // update cache on success
            .onErrorResume(CallNotPermittedException.class, ex -> {
                log.warn("Circuit OPEN for user {}, using cache", id);
                return cache.get(id)
                    .switchIfEmpty(Mono.just(User.stub(id)));  // minimal stub
            })
            .onErrorResume(ex -> {
                log.warn("User service failed: {}, using cache", ex.getMessage());
                return cache.get(id)
                    .switchIfEmpty(Mono.error(ex));
            });
    }
}
```

### Task 5: Fallback Chain

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

### Task 6: Test Error Scenarios

```java
@ExtendWith(MockitoExtension.class)
class ResilientOrderServiceTest {

    @Mock OrderApiClient apiClient;
    @Mock OrderCacheService cache;

    @Test
    void retriesOnServerError_thenSucceeds() {
        AtomicInteger callCount = new AtomicInteger();
        Order expected = new Order("123", "Test");

        Mono<Order> apiCall = Mono.defer(() -> {
            int count = callCount.incrementAndGet();
            if (count <= 2) return Mono.error(
                WebClientResponseException.create(500, "Error", null, null, null));
            return Mono.just(expected);
        });

        when(apiClient.fetchOrder("123")).thenReturn(apiCall);

        StepVerifier.create(orderService.getOrder("123"))
            .expectNext(expected)
            .verifyComplete();

        assertThat(callCount.get()).isEqualTo(3);
    }

    @Test
    void doesNotRetryOn400() {
        AtomicInteger callCount = new AtomicInteger();

        when(apiClient.fetchOrder("123")).thenReturn(Mono.defer(() -> {
            callCount.incrementAndGet();
            return Mono.error(WebClientResponseException.create(
                400, "Bad Request", null, null, null));
        }));

        StepVerifier.create(orderService.getOrder("123"))
            .expectError(ValidationException.class)
            .verify();

        assertThat(callCount.get()).isEqualTo(1);  // No retries
    }

    @Test
    void timeoutFallsBackToCache() {
        Order cached = new Order("123", "Cached");

        when(apiClient.fetchOrder("123"))
            .thenReturn(Mono.never());  // Hangs forever → timeout

        when(cache.getOrder("123"))
            .thenReturn(Mono.just(cached));

        StepVerifier.create(orderService.getOrder("123"))
            .expectNext(cached)
            .verifyComplete();
    }

    @Test
    void fallbackChain_usesStaleCache() {
        ProductData stale = new ProductData("123", "Stale Data");

        when(primaryApi.getProduct("123"))
            .thenReturn(Mono.error(new ServiceException("primary down")));
        when(secondaryApi.getProduct("123"))
            .thenReturn(Mono.error(new ServiceException("secondary down")));
        when(cache.get("123")).thenReturn(Mono.empty());
        when(staleCache.get("123")).thenReturn(Mono.just(stale));

        StepVerifier.create(fallbackService.getProductData("123"))
            .expectNext(stale)
            .verifyComplete();
    }

    @Test
    void circuitBreaker_opensAfterThreshold() {
        CircuitBreaker cb = CircuitBreaker.of("test",
            CircuitBreakerConfig.custom()
                .slidingWindowSize(10)
                .failureRateThreshold(50)
                .minimumNumberOfCalls(5)
                .build());

        AtomicInteger callCount = new AtomicInteger();

        // Make 6 failing calls to trip the circuit (60% failure > 50% threshold)
        for (int i = 0; i < 6; i++) {
            try {
                Mono.error(new IOException("fail"))
                    .transformDeferred(CircuitBreakerOperator.of(cb))
                    .block();
            } catch (Exception ignored) {}
        }

        assertThat(cb.getState()).isEqualTo(CircuitBreaker.State.OPEN);

        // Next call should be rejected immediately
        StepVerifier.create(
            Mono.just("value")
                .transformDeferred(CircuitBreakerOperator.of(cb))
        )
            .expectError(CallNotPermittedException.class)
            .verify();
    }
}
```
