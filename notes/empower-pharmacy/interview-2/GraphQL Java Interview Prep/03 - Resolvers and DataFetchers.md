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

## Interview Questions

1. What is a DataFetcher and how does the engine decide which one to call?
2. How does `@BatchMapping` differ from `@SchemaMapping`?
3. Show two ways to parallelize calls to independent services in a resolver.
4. How would you use `getSelectionSet()` to avoid unnecessary downstream calls?
5. What's the difference between `getSource()` and `getArgument()`?
