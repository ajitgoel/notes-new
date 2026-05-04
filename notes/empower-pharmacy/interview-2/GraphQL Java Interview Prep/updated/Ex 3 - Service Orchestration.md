# Exercise 3: Multi-Service Orchestration

## Scenario

You're building a dashboard endpoint that aggregates data from four independent services:

```
GraphQL Server
  ├── User Service (REST) — profile info
  ├── Order Service (REST) — recent orders
  ├── Notification Service (gRPC) — unread count
  └── Recommendation Service (REST) — suggested products
```

---

## Task 1: Define the Schema

```graphql
# TODO: Design the schema for a dashboard query
# Requirements:
# - Dashboard type with: user, recentOrders, unreadNotifications (count),
#   recommendations
# - Each field should be independently resolvable
# - Use appropriate nullability (what if recommendations service is down?)
```

---

## Task 2: Implement Parallel Resolution

```java
@Controller
public class DashboardController {

    // TODO: Implement dashboard query
    // Requirements:
    // - User and orders are REQUIRED (fail if unavailable)
    // - Notifications and recommendations are OPTIONAL (degrade gracefully)
    // - All four calls should execute in parallel
    // - Use CompletableFuture.allOf or Mono.zip

    @QueryMapping
    public CompletableFuture<Dashboard> dashboard(@Argument String userId) {
        // Your implementation here
    }
}
```

---

## Task 3: Implement Graceful Degradation

When an optional service fails, return a default instead of failing the whole query:

```java
// TODO: Implement a helper that wraps a CompletableFuture
// with fallback behavior:
// - Log the error
// - Return a default value
// - Add an error to the GraphQL response (partial error)

private <T> CompletableFuture<T> withFallback(
    CompletableFuture<T> future,
    T fallbackValue,
    String serviceName,
    DataFetchingEnvironment env
) {
    // Your implementation here
}
```

---

## Task 4: Implement WebClient Calls

```java
// TODO: Implement the WebClient-based service calls
// Use Spring WebFlux WebClient for REST services

@Service
public class UserServiceClient {

    private final WebClient webClient;

    public Mono<User> getUser(String userId) {
        // GET /api/users/{userId}
    }
}

@Service
public class OrderServiceClient {

    public Mono<List<Order>> getRecentOrders(String userId, int limit) {
        // GET /api/orders?userId={userId}&limit={limit}
    }
}
```

---

## Acceptance Criteria

- [ ] All four service calls execute in parallel (not sequentially)
- [ ] If recommendations service returns 500, dashboard still returns with other data
- [ ] If user service returns 404, the entire query fails with a clear error
- [ ] Response time ≈ slowest single service call (not sum of all)

---

## Complete Solution

### Schema

```graphql
type Query {
  dashboard(userId: ID!): Dashboard!
}

type Dashboard {
  user: User!
  recentOrders: [Order!]!
  unreadNotifications: Int!
  recommendations: [Product!]     # nullable — service may be down
}
```

### Domain Records

```java
public record Dashboard(
    User user,
    List<Order> recentOrders,
    int unreadNotifications,
    List<Product> recommendations
) {}
```

### WebClient Service Clients

```java
@Service
public class UserServiceClient {
    private final WebClient webClient;

    public UserServiceClient(WebClient.Builder builder) {
        this.webClient = builder.baseUrl("http://user-service:8081").build();
    }

    public Mono<User> getUser(String userId) {
        return webClient.get()
            .uri("/api/users/{userId}", userId)
            .retrieve()
            .onStatus(status -> status.value() == 404, response ->
                Mono.error(new ResourceNotFoundException("User", userId)))
            .bodyToMono(User.class)
            .timeout(Duration.ofSeconds(3));
    }
}

@Service
public class OrderServiceClient {
    private final WebClient webClient;

    public OrderServiceClient(WebClient.Builder builder) {
        this.webClient = builder.baseUrl("http://order-service:8082").build();
    }

    public Mono<List<Order>> getRecentOrders(String userId, int limit) {
        return webClient.get()
            .uri(uriBuilder -> uriBuilder
                .path("/api/orders")
                .queryParam("userId", userId)
                .queryParam("limit", limit)
                .build())
            .retrieve()
            .bodyToFlux(Order.class)
            .collectList()
            .timeout(Duration.ofSeconds(3));
    }
}
```

### Controller — Reactive (Mono.zip)

```java
@Controller
public class DashboardController {

    private final UserServiceClient userClient;
    private final OrderServiceClient orderClient;
    private final NotificationServiceClient notifClient;
    private final RecommendationServiceClient recClient;

    @QueryMapping
    public Mono<Dashboard> dashboard(@Argument String userId) {
        // All four calls start concurrently via Mono.zip
        Mono<User> user = userClient.getUser(userId);
            // Required — errors propagate and fail the whole query

        Mono<List<Order>> orders = orderClient.getRecentOrders(userId, 10);
            // Required — errors propagate

        Mono<Integer> notifCount = notifClient.getUnreadCount(userId)
            .onErrorReturn(0);
            // Optional — fallback to 0 on failure

        Mono<List<Product>> recs = recClient.getForUser(userId)
            .onErrorReturn(List.of());
            // Optional — fallback to empty list on failure

        return Mono.zip(user, orders, notifCount, recs)
            .map(tuple -> new Dashboard(
                tuple.getT1(),   // user
                tuple.getT2(),   // orders
                tuple.getT3(),   // notifCount
                tuple.getT4()    // recommendations
            ));
    }
}
```

### Controller — CompletableFuture Alternative

```java
@QueryMapping
public CompletableFuture<Dashboard> dashboard(@Argument String userId) {
    var userF = CompletableFuture.supplyAsync(() -> userClient.getUser(userId));
    var ordersF = CompletableFuture.supplyAsync(() -> orderClient.getOrders(userId, 10));
    var notifF = CompletableFuture.supplyAsync(() -> notifClient.getUnreadCount(userId))
        .exceptionally(ex -> { log.warn("Notif failed", ex); return 0; });
    var recsF = CompletableFuture.supplyAsync(() -> recClient.getForUser(userId))
        .exceptionally(ex -> { log.warn("Recs failed", ex); return List.of(); });

    return CompletableFuture.allOf(userF, ordersF, notifF, recsF)
        .thenApply(v -> new Dashboard(
            userF.join(), ordersF.join(), notifF.join(), recsF.join()
        ));
}
```

### Graceful Degradation Helper

```java
private <T> CompletableFuture<T> withFallback(
        CompletableFuture<T> future,
        T fallbackValue,
        String serviceName,
        DataFetchingEnvironment env) {
    return future.exceptionally(ex -> {
        log.warn("{} failed: {}", serviceName, ex.getMessage());
        // Optionally add a partial error to the GraphQL response
        env.getGraphQlContext().compute("warnings", (key, existing) -> {
            List<String> warnings = existing != null
                ? new ArrayList<>((List<String>) existing) : new ArrayList<>();
            warnings.add(serviceName + " unavailable");
            return warnings;
        });
        return fallbackValue;
    });
}
```
