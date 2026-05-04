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
```java hl:11,4-5,9
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
```java hl:1,9
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
## Interview Questions & Answers
### => 1. Explain the N+1 problem in GraphQL. How does DataLoader solve it?
When a query requests `users { orders }`, the engine resolves `users` first (1 query), then calls the `orders` resolver once per user (N queries). With 50 users, that's 51 database calls.
==DataLoader== solves this by ==**batching and deduplication**.== ==Instead of executing each `orders(userId)` call immediately, DataLoader queues them. At the end of each execution level, it dispatches all queued keys as a single batch call:== `orderService.findByUserIds(Set.of("1", "2", ... "50"))`. Result: 2 total calls instead of 51.
==DataLoader also deduplicates within a request. If `loader.load("123")` is called three times (because user 123 appears in multiple places), it makes only one actual call and returns the same result to all callers.==
### 2. What's the difference between `BatchLoader` and `MappedBatchLoader`?
**`BatchLoader<K, V>`**: Receives `List<K>` keys, returns `List<V>` values. The values list MUST be the same size and order as the keys list — position `i` in the result corresponds to key `i`. This is strict and error-prone if a key has no result (you must insert null at that position).
**`MappedBatchLoader<K, V>`**: Receives `Set<K>` keys, returns `Map<K, V>`. Much more natural — you return a map of key→value, and DataLoader handles missing keys (returns null). This is the preferred approach because you don't have to worry about ordering, and it maps naturally to most service APIs that return `Map<String, List<Order>>`.
### => 3. How would you prevent a malicious query from overwhelming your server?
Three complementary strategies:
**Query depth limiting**: `MaxQueryDepthInstrumentation(10)` — ==rejects deeply nested queries== like `{ user { orders { items { product { reviews { author { orders { ... } } } } } } } }`. ==Prevents recursive traversal attacks.==
**Query complexity analysis**: `MaxQueryComplexityInstrumentation(200)` — ==assigns a cost to each field and rejects queries exceeding the budget.== List fields can use multipliers: `users(first: 100)` with complexity 5 = 500 total. This catches wide queries that depth alone wouldn't catch.
**Persisted queries**: Only allow pre-registered query hashes. The client sends `{ "extensions": { "persistedQuery": { "sha256Hash": "abc..." } } }` instead of an arbitrary query string. This completely prevents query injection and also enables CDN caching.
Additionally: disable introspection in production, rate-limit by API key, and set request timeouts.
### => 4. When would you use application-level caching vs DataLoader's request-scoped cache?
==**DataLoader (request-scoped)**: Automatically deduplicates within a single GraphQL request.== If the same user ID is needed in two different parts of the query, DataLoader makes one call. This is free, automatic, and always safe — no stale data concerns because it lives and dies with the request.
==**Application-level cache (Spring `@Cacheable`, Redis, etc.)**: Caches across requests.== Use it for data that changes infrequently (product catalog, configuration, reference data) where the latency savings justify the staleness risk. Requires cache invalidation strategy (TTL, event-based eviction).
**Rule**: ==DataLoader for everything (it's free). Application cache only for slow, stable data where you've thought through invalidation.==
### 5. How does `@BatchMapping` work under the hood in Spring for GraphQL?
When Spring sees `@BatchMapping` on a method, it:
1. **Registers a DataLoader** in the `BatchLoaderRegistry` for that field, keyed by the parent type + field name
2. **Replaces the field's DataFetcher** with one that calls `dataLoader.load(parentObject)` instead of invoking the method directly
3. **At dispatch time**, the DataLoader collects all parent objects that were queued during that execution level, passes them as a `List<Parent>` to your `@BatchMapping` method, and receives the `Map<Parent, Result>` back
4. **Distributes results** — each parent object gets its corresponding value from the map
The key insight: your method is called exactly once per execution level, regardless of how many parent objects exist. Spring handles the plumbing of collecting parents, calling your method, and distributing results — you just write a method that takes a list and returns a map.
