# Resolvers & DataFetchers in Java

## The Core Abstraction: DataFetcher

Every field in a GraphQL schema is backed by a `DataFetcher<T>`. The engine calls it when a client requests that field.

```java
// graphql-java low-level
DataFetcher<User> userFetcher = environment -> {
    String id = environment.getArgument("id");
    return userService.findById(id);
};
```

### DataFetchingEnvironment — what you have access to
- `getArgument(name)` — query arguments
- `getSource()` — parent object (for nested fields)
- `getContext()` — request-scoped context (auth, headers)
- `getDataLoader(name)` — batch loader for N+1 prevention
- `getSelectionSet()` — which sub-fields the client selected

---

## Spring for GraphQL — Annotation-Based

```java
@Controller
public class UserController {

    @QueryMapping
    public User user(@Argument String id) {
        return userService.findById(id);
    }

    @SchemaMapping(typeName = "User")
    public List<Order> orders(User user) {
        // 'user' is the parent object (source)
        return orderService.findByUserId(user.getId());
    }

    @MutationMapping
    public PlaceOrderPayload placeOrder(@Argument PlaceOrderInput input) {
        return orderService.placeOrder(input);
    }

    @BatchMapping
    public Map<User, List<Order>> orders(List<User> users) {
        // Batched — called once for all Users in the query
        return orderService.findOrdersByUsers(users);
    }
}
```

### Key annotations
| Annotation | Purpose |
|-----------|---------|
| `@QueryMapping` | Resolves a field on `Query` type |
| `@MutationMapping` | Resolves a field on `Mutation` type |
| `@SchemaMapping` | Resolves a field on any type |
| `@BatchMapping` | Batch-resolves a field (DataLoader under the hood) |
| `@Argument` | Binds a query argument to a method parameter |

---

## Netflix DGS — Annotation-Based

```java
@DgsComponent
public class UserDataFetcher {

    @DgsQuery
    public User user(@InputArgument String id) {
        return userService.findById(id);
    }

    @DgsData(parentType = "User", field = "orders")
    public List<Order> orders(DgsDataFetchingEnvironment dfe) {
        User user = dfe.getSource();
        return orderService.findByUserId(user.getId());
    }

    @DgsData(parentType = "User", field = "orders")
    public CompletableFuture<List<Order>> ordersWithLoader(
            DgsDataFetchingEnvironment dfe) {
        User user = dfe.getSource();
        DataLoader<String, List<Order>> loader =
            dfe.getDataLoader("ordersLoader");
        return loader.load(user.getId());
    }
}
```

---

## Orchestration Patterns in Resolvers

### Pattern 1: Sequential calls (one depends on another)
```java
@QueryMapping
public OrderSummary orderSummary(@Argument String orderId) {
    Order order = orderService.getOrder(orderId);
    User user = userService.getUser(order.getUserId());
    ShippingStatus status = shippingService.track(order.getTrackingId());
    return new OrderSummary(order, user, status);
}
```

### Pattern 2: Parallel calls (independent data sources)
```java
@QueryMapping
public CompletableFuture<Dashboard> dashboard(@Argument String userId) {
    CompletableFuture<User> userF = CompletableFuture
        .supplyAsync(() -> userService.getUser(userId));
    CompletableFuture<List<Order>> ordersF = CompletableFuture
        .supplyAsync(() -> orderService.getRecentOrders(userId));
    CompletableFuture<Recommendations> recsF = CompletableFuture
        .supplyAsync(() -> recService.getForUser(userId));

    return CompletableFuture.allOf(userF, ordersF, recsF)
        .thenApply(v -> new Dashboard(
            userF.join(), ordersF.join(), recsF.join()
        ));
}
```

### Pattern 3: Reactive (WebFlux)
```java
@QueryMapping
public Mono<Dashboard> dashboard(@Argument String userId) {
    return Mono.zip(
        userClient.getUser(userId),
        orderClient.getRecentOrders(userId),
        recClient.getForUser(userId)
    ).map(tuple -> new Dashboard(
        tuple.getT1(), tuple.getT2(), tuple.getT3()
    ));
}
```

---

## Selection Set Optimization

Only call downstream services for fields the client actually requested:

```java
@QueryMapping
public User user(@Argument String id,
                 DataFetchingEnvironment env) {
    boolean needsOrders = env.getSelectionSet()
        .contains("orders");
    User user = userService.findById(id);
    if (needsOrders) {
        user.setOrders(orderService.findByUserId(id));
    }
    return user;
}
```

---

## Interview Questions & Answers

### 1. What is a DataFetcher and how does the engine decide which one to call?

A `DataFetcher<T>` is a function that resolves a single field in the schema. Every field has one. During execution, the engine walks the query AST depth-first. For each field node, it looks up the `DataFetcher` registered for that type+field combination in the `RuntimeWiring`. If none is registered, it falls back to `PropertyDataFetcher`, which calls a getter matching the field name on the parent object (e.g., `getEmail()` for the `email` field).

The engine passes a `DataFetchingEnvironment` containing: the parent object (`getSource()`), arguments (`getArgument()`), the GraphQL context (auth, headers), the DataLoader registry, and the selection set (which sub-fields were requested). The DataFetcher uses this to fetch and return the data.

### 2. How does `@BatchMapping` differ from `@SchemaMapping`?

`@SchemaMapping` resolves a single field for a single parent object. If you query 50 users, the `orders` SchemaMapping is called 50 times — once per user. This is the N+1 problem.

`@BatchMapping` resolves a field for ALL parent objects at once. The method receives `List<User>` and returns `Map<User, List<Order>>`. Spring collects all the parents during execution, calls your method once, and distributes the results. Under the hood, it registers a `DataLoader` that batches the calls.

```java
// SchemaMapping: called N times
@SchemaMapping(typeName = "User")
public List<Order> orders(User user) { ... }  // 50 calls for 50 users

// BatchMapping: called once
@BatchMapping
public Map<User, List<Order>> orders(List<User> users) { ... }  // 1 call
```

### 3. Show two ways to parallelize calls to independent services in a resolver.

**CompletableFuture** (blocking services, runs on thread pool):
```java
@QueryMapping
public CompletableFuture<Dashboard> dashboard(@Argument String userId) {
    var userF = CompletableFuture.supplyAsync(() -> userService.getUser(userId));
    var ordersF = CompletableFuture.supplyAsync(() -> orderService.getOrders(userId));
    var recsF = CompletableFuture.supplyAsync(() -> recService.getRecs(userId));
    return CompletableFuture.allOf(userF, ordersF, recsF)
        .thenApply(v -> new Dashboard(userF.join(), ordersF.join(), recsF.join()));
}
```

**Mono.zip** (reactive services, non-blocking):
```java
@QueryMapping
public Mono<Dashboard> dashboard(@Argument String userId) {
    return Mono.zip(
        userClient.getUser(userId),
        orderClient.getOrders(userId),
        recClient.getRecs(userId)
    ).map(t -> new Dashboard(t.getT1(), t.getT2(), t.getT3()));
}
```

Both approaches run all three calls concurrently. The total latency equals the slowest service, not the sum. `Mono.zip` is preferred in WebFlux because it doesn't block threads.

### 4. How would you use `getSelectionSet()` to avoid unnecessary downstream calls?

`getSelectionSet()` tells you which sub-fields the client actually requested. If a client queries `{ user(id: "1") { name } }` — they didn't ask for orders. You can skip the order service call entirely:

```java
@QueryMapping
public User user(@Argument String id, DataFetchingEnvironment env) {
    User user = userService.findById(id);
    if (env.getSelectionSet().contains("orders")) {
        user.setOrders(orderService.findByUserId(id));
    }
    if (env.getSelectionSet().contains("recommendations")) {
        user.setRecommendations(recService.getForUser(id));
    }
    return user;
}
```

This is especially valuable in orchestration where downstream calls are expensive. You can also use it to optimize database queries — if the client didn't select `address`, you don't need to JOIN the addresses table.

### 5. What's the difference between `getSource()` and `getArgument()`?

`getSource()` returns the parent object in the graph. When resolving `User.orders`, the source is the `User` instance that was already resolved. It's how you access the parent's data to fetch child fields.

`getArgument()` returns the arguments passed in the query. For `user(id: "123")`, `getArgument("id")` returns `"123"`. Arguments come from the client's query, while source comes from the parent resolver.

In practice: top-level Query fields typically use `getArgument()` (the client passes the ID), while nested fields use `getSource()` (the parent provides context like `user.getId()` to look up orders).
