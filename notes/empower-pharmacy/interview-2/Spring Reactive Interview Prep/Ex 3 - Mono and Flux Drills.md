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

## Solutions

<details>
<summary>Drill 1 (click to reveal)</summary>

```java
Mono<String> greet(Mono<String> name) {
    return name.map(n -> "HELLO, " + n.toUpperCase() + "!");
}
```
</details>

<details>
<summary>Drill 8 (click to reveal)</summary>

```java
Mono<Data> resilientCall(String id) {
    return apiClient.getData(id)
        .retryWhen(Retry.backoff(3, Duration.ofMillis(100))
            .filter(ex -> ex instanceof IOException))
        .onErrorResume(ex -> cacheService.getCached(id));
}
```
</details>

<details>
<summary>Drill 13 (click to reveal)</summary>

```java
Flux<Item> fetchAllPages() {
    return fetchPage(null)
        .expand(page -> page.nextCursor() != null
            ? fetchPage(page.nextCursor())
            : Mono.empty())
        .flatMapIterable(PageResult::items);
}
```
</details>
