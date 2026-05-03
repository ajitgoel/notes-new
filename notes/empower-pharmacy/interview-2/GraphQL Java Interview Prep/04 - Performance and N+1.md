# Performance & the N+1 Problem

## The N+1 Problem in GraphQL

```graphql
query {
  users(first: 50) {
    name
    orders { id total }   # ← 1 query for users + 50 queries for orders
  }
}
```

Without batching, the `orders` resolver fires once **per user**. 50 users = 50 separate calls to the order service. This is the N+1 problem.

---

## DataLoader: The Solution

DataLoader collects individual loads within a single execution tick, then dispatches them as one batch call.

### Step 1: Register a BatchLoader
```java
// Spring for GraphQL
@Configuration
public class DataLoaderConfig {

    @Bean
    public BatchLoaderRegistry batchLoaderRegistry(
            OrderService orderService) {
        return registry -> registry
            .forTypePair(String.class, List.class)
            .registerMappedBatchLoader((userIds, env) ->
                Mono.fromCallable(() ->
                    orderService.findOrdersByUserIds(userIds)
                    // returns Map<String, List<Order>>
                )
            );
    }
}
```

### Step 2: Use in resolver
```java
@SchemaMapping(typeName = "User")
public CompletableFuture<List<Order>> orders(
        User user, DataLoader<String, List<Order>> ordersLoader) {
    return ordersLoader.load(user.getId());
}
```

### Or use @BatchMapping (Spring shortcut)
```java
@BatchMapping
public Map<User, List<Order>> orders(List<User> users) {
    List<String> ids = users.stream()
        .map(User::getId).toList();
    Map<String, List<Order>> ordersByUserId =
        orderService.findOrdersByUserIds(ids);
    return users.stream().collect(Collectors.toMap(
        u -> u,
        u -> ordersByUserId.getOrDefault(u.getId(), List.of())
    ));
}
```

**Result**: 50 users → 1 query for users + 1 batched query for all orders = 2 total.

---

## DataLoader in DGS

```java
@DgsDataLoader(name = "ordersLoader")
public class OrdersDataLoader implements
        MappedBatchLoader<String, List<Order>> {

    @Override
    public CompletionStage<Map<String, List<Order>>> load(
            Set<String> userIds) {
        return CompletableFuture.supplyAsync(() ->
            orderService.findOrdersByUserIds(userIds)
        );
    }
}

// In fetcher
@DgsData(parentType = "User", field = "orders")
public CompletableFuture<List<Order>> orders(
        DgsDataFetchingEnvironment dfe) {
    DataLoader<String, List<Order>> loader =
        dfe.getDataLoader("ordersLoader");
    User user = dfe.getSource();
    return loader.load(user.getId());
}
```

---

## Query Complexity Analysis

Prevent expensive queries from overwhelming your orchestration layer:

```java
GraphQL graphQL = GraphQL.newGraphQL(schema)
    .instrumentation(new MaxQueryComplexityInstrumentation(200))
    .instrumentation(new MaxQueryDepthInstrumentation(10))
    .build();
```

### Custom complexity calculation
```graphql
type Query {
  users(first: Int): [User!]! @complexity(value: 5, multipliers: ["first"])
}
```

---

## Caching Strategies

### Request-scoped cache (DataLoader)
- DataLoader automatically deduplicates within a single request
- `loader.load("123")` called 3 times → 1 actual fetch

### Application-level cache
```java
@SchemaMapping(typeName = "Query")
@Cacheable(value = "products", key = "#id")
public Product product(@Argument String id) {
    return productService.findById(id);
}
```

### HTTP-level: persisted queries
```java
// Client sends hash instead of full query
POST /graphql
{ "extensions": { "persistedQuery": { "sha256Hash": "abc123..." } } }
```
Reduces payload size and enables CDN caching for GET-based persisted queries.

---

## Async & Reactive Execution

```java
// Return CompletableFuture — engine executes in parallel
@QueryMapping
public CompletableFuture<User> user(@Argument String id) {
    return CompletableFuture.supplyAsync(
        () -> userService.findById(id),
        executor  // custom thread pool
    );
}

// Or Mono with WebFlux
@QueryMapping
public Mono<User> user(@Argument String id) {
    return webClient.get()
        .uri("/users/{id}", id)
        .retrieve()
        .bodyToMono(User.class);
}
```

---

## Interview Questions

1. Explain the N+1 problem in GraphQL. How does DataLoader solve it?
2. What's the difference between `BatchLoader` and `MappedBatchLoader`?
3. How would you prevent a malicious query from overwhelming your server?
4. When would you use application-level caching vs DataLoader's request-scoped cache?
5. How does `@BatchMapping` work under the hood in Spring for GraphQL?
