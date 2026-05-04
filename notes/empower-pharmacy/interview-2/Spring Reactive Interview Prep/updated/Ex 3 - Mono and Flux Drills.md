# Exercise 3: Mono & Flux Operator Drills

Run each exercise with StepVerifier to confirm correctness.

---

## Drill 1: Transform
```java
// Given a Mono<String> of a user's name,
// return their name in uppercase with a greeting prefix.
// "alice" → "HELLO, ALICE!"
Mono<String> greet(Mono<String> name) {
    // TODO
}
```

## Drill 2: FlatMap
```java
// Given a user ID, look up the user, then look up their department.
// userService.findById(id) → Mono<User>
// deptService.findById(user.deptId) → Mono<Department>
Mono<Department> getUserDepartment(String userId) {
    // TODO
}
```

## Drill 3: Zip
```java
// Fetch a user's profile AND their preferences in parallel.
// Combine into UserWithPrefs record.
Mono<UserWithPrefs> getUserWithPrefs(String id) {
    // TODO: use Mono.zip
}
```

## Drill 4: Default and Switch
```java
// Look up a config value. If empty, try a fallback source.
// If both empty, return a hardcoded default.
Mono<String> getConfig(String key) {
    // TODO: primaryConfig.get(key)
    //   .switchIfEmpty(fallbackConfig.get(key))
    //   .defaultIfEmpty("default-value")
}
```

## Drill 5: Filter and CollectList
```java
// Given a Flux<Order>, return a Mono<List<Order>> of only
// orders over $100 placed in the last 7 days.
Mono<List<Order>> getRecentBigOrders(Flux<Order> orders) {
    // TODO
}
```

## Drill 6: GroupBy and Reduce
```java
// Given a Flux<Transaction>, group by category and sum amounts.
// Return Flux<CategoryTotal> where CategoryTotal(String category, BigDecimal total)
Flux<CategoryTotal> summarize(Flux<Transaction> txns) {
    // TODO: groupBy → flatMap with reduce
}
```

## Drill 7: Concat vs Merge
```java
// Scenario A: Try local cache, then remote if cache misses
Flux<Product> cacheFirst(Flux<Product> cache, Flux<Product> remote) {
    // TODO: concat — sequential order matters
}

// Scenario B: Combine results from 3 notification sources, order doesn't matter
Flux<Notification> allNotifications(Flux<Notification> email,
        Flux<Notification> sms, Flux<Notification> push) {
    // TODO: merge — interleaved is fine
}
```

## Drill 8: Error Recovery
```java
// Call an unreliable API. If it fails with IOException, retry 3 times.
// If it fails with AuthException, don't retry — return Mono.error.
// If retries exhausted, return a cached result.
Mono<Data> resilientCall(String id) {
    // TODO
}
```

## Drill 9: Timeout with Fallback
```java
// Call a slow service with a 2-second timeout.
// On timeout, return data from a fast cache instead.
Mono<Product> getProduct(String id) {
    // TODO
}
```

## Drill 10: ConcatMap vs FlatMap
```java
// Process a Flux<String> of file paths.
// Each must be uploaded sequentially (order matters, no parallelism).
Flux<UploadResult> uploadFiles(Flux<String> paths) {
    // TODO: use concatMap (not flatMap!)
}
```

## Drill 11: Buffer and Batch Process
```java
// Given a Flux<Event> stream, batch into groups of 50
// and save each batch to the database.
Mono<Void> batchSave(Flux<Event> events) {
    // TODO: buffer(50) → flatMap(batch -> repo.saveAll(batch)) → then()
}
```

## Drill 12: First With Value
```java
// Race two data centers. Return whichever responds first.
Mono<Response> raceDCs(Mono<Response> dc1, Mono<Response> dc2) {
    // TODO
}
```

## Drill 13: Expand (recursive/tree traversal)
```java
// Given a paginated API that returns PageResult(items, nextCursor),
// fetch all pages until nextCursor is null.
Flux<Item> fetchAllPages() {
    // TODO: use expand()
}
```

## Drill 14: Context Propagation
```java
// Pass a correlationId through a reactive chain without method params.
Mono<Response> processRequest(Request request) {
    // TODO: write correlationId to Context, read it in downstream operators
}
```

## Drill 15: StepVerifier Assertions
```java
// Write StepVerifier tests for the following:
// a) Verify Mono emits exactly "hello" then completes
// b) Verify Flux emits 1, 2, 3 then completes
// c) Verify Mono errors with IllegalArgumentException
// d) Verify Flux with Duration.ofSeconds(1) interval emits 3 items
//    (hint: use withVirtualTime)
```

---

## All Solutions

### Drill 1: Transform
```java
Mono<String> greet(Mono<String> name) {
    return name.map(n -> "HELLO, " + n.toUpperCase() + "!");
}
```

### Drill 2: FlatMap
```java
Mono<Department> getUserDepartment(String userId) {
    return userService.findById(userId)
        .flatMap(user -> deptService.findById(user.getDeptId()));
}
```

### Drill 3: Zip
```java
Mono<UserWithPrefs> getUserWithPrefs(String id) {
    return Mono.zip(
        userService.getProfile(id),
        userService.getPreferences(id),
        (profile, prefs) -> new UserWithPrefs(profile, prefs)
    );
}
```

### Drill 4: Default and Switch
```java
Mono<String> getConfig(String key) {
    return primaryConfig.get(key)
        .switchIfEmpty(fallbackConfig.get(key))
        .defaultIfEmpty("default-value");
}
```

### Drill 5: Filter and CollectList
```java
Mono<List<Order>> getRecentBigOrders(Flux<Order> orders) {
    LocalDate cutoff = LocalDate.now().minusDays(7);
    return orders
        .filter(o -> o.getTotal().compareTo(BigDecimal.valueOf(100)) > 0)
        .filter(o -> o.getDate().isAfter(cutoff))
        .collectList();
}
```

### Drill 6: GroupBy and Reduce
```java
Flux<CategoryTotal> summarize(Flux<Transaction> txns) {
    return txns.groupBy(Transaction::getCategory)
        .flatMap(group -> group
            .map(Transaction::getAmount)
            .reduce(BigDecimal.ZERO, BigDecimal::add)
            .map(total -> new CategoryTotal(group.key(), total))
        );
}
```

### Drill 7: Concat vs Merge
```java
Flux<Product> cacheFirst(Flux<Product> cache, Flux<Product> remote) {
    return Flux.concat(cache, remote);
}

Flux<Notification> allNotifications(Flux<Notification> email,
        Flux<Notification> sms, Flux<Notification> push) {
    return Flux.merge(email, sms, push);
}
```

### Drill 8: Error Recovery
```java
Mono<Data> resilientCall(String id) {
    return apiClient.getData(id)
        .retryWhen(Retry.backoff(3, Duration.ofMillis(100))
            .filter(ex -> ex instanceof IOException))
        .onErrorResume(AuthException.class, ex -> Mono.error(ex))
        .onErrorResume(ex -> cacheService.getCached(id));
}
```

### Drill 9: Timeout with Fallback
```java
Mono<Product> getProduct(String id) {
    return slowService.getProduct(id)
        .timeout(Duration.ofSeconds(2))
        .onErrorResume(TimeoutException.class,
            ex -> cacheService.getCached(id));
}
```

### Drill 10: ConcatMap vs FlatMap
```java
Flux<UploadResult> uploadFiles(Flux<String> paths) {
    return paths.concatMap(path -> uploadService.upload(path));
    // concatMap: sequential — waits for each upload before starting next
    // flatMap would upload in parallel with no ordering guarantee
}
```

### Drill 11: Buffer and Batch Process
```java
Mono<Void> batchSave(Flux<Event> events) {
    return events
        .buffer(50)
        .flatMap(batch -> eventRepo.saveAll(batch).then(), 5) // max 5 concurrent
        .then();
}
```

### Drill 12: First With Value
```java
Mono<Response> raceDCs(Mono<Response> dc1, Mono<Response> dc2) {
    return Mono.firstWithValue(dc1, dc2);
}
```

### Drill 13: Expand (recursive/tree traversal)
```java
Flux<Item> fetchAllPages() {
    return fetchPage(null)
        .expand(page -> page.nextCursor() != null
            ? fetchPage(page.nextCursor())
            : Mono.empty())
        .flatMapIterable(PageResult::items);
}
```

### Drill 14: Context Propagation
```java
Mono<Response> processRequest(Request request) {
    return Mono.deferContextual(ctx -> {
            String correlationId = ctx.get("correlationId");
            log.info("Processing with correlationId={}", correlationId);
            return serviceA.call(request)
                .flatMap(result -> Mono.deferContextual(innerCtx -> {
                    log.info("Calling serviceB, correlationId={}",
                        innerCtx.get("correlationId"));
                    return serviceB.call(result);
                }));
        })
        .contextWrite(Context.of("correlationId", request.getCorrelationId()));
}
```

### Drill 15: StepVerifier Assertions
```java
// a) Verify Mono emits exactly "hello" then completes
StepVerifier.create(Mono.just("hello"))
    .expectNext("hello")
    .verifyComplete();

// b) Verify Flux emits 1, 2, 3 then completes
StepVerifier.create(Flux.just(1, 2, 3))
    .expectNext(1, 2, 3)
    .verifyComplete();

// c) Verify Mono errors with IllegalArgumentException
StepVerifier.create(Mono.error(new IllegalArgumentException("bad")))
    .expectError(IllegalArgumentException.class)
    .verify();

// d) Verify Flux with Duration.ofSeconds(1) interval emits 3 items
StepVerifier.withVirtualTime(() ->
        Flux.interval(Duration.ofSeconds(1)).take(3))
    .thenAwait(Duration.ofSeconds(3))
    .expectNextCount(3)
    .verifyComplete();
```
