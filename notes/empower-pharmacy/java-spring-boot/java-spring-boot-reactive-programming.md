**Core Concepts** 
**1. Publisher / Subscriber Pattern** 
A **Publisher** emits data. A **Subscriber** consumes it. Data flows only when someone subscribes (lazy evaluation).
**2. The Two Core Types (Project Reactor)** 

| Type      | What it represents                         |
| --------- | ------------------------------------------ |
| `Mono<T>` | 0 or 1 element (like `Optional` but async) |
| `Flux<T>` | 0 to N elements (like `Stream` but async)  |
**Getting Started** 
**Maven dependency:**
```xml
<dependency>
    <groupId>io.projectreactor</groupId>
    <artifactId>reactor-core</artifactId>
    <version>3.7.0</version>
</dependency>
```

**Mono — Single Value** 
```java
// Create a Mono
Mono<String> mono = Mono.just("Hello");
// Nothing happens until you subscribe
mono.subscribe(value -> System.out.println(value)); // prints "Hello"
// Mono that's empty
Mono<String> empty = Mono.empty();
// Mono from a slow computation
Mono<String> deferred = Mono.fromSupplier(() -> {
    // simulate DB call
    return fetchFromDatabase();
});
```
**Flux — Multiple Values** 
```java
// From fixed values
Flux<String> flux = Flux.just("A", "B", "C");
flux.subscribe(System.out::println); // prints A, B, C
// From a range
Flux<Integer> numbers = Flux.range(1, 5); // 1, 2, 3, 4, 5
// From a collection
Flux<String> fromList = Flux.fromIterable(List.of("X", "Y", "Z"));
// Infinite stream — emits every second
Flux<Long> ticks = Flux.interval(Duration.ofSeconds(1));
```
**Operators — Transforming Data** 
**map — transform each element** 
```java
Flux.just("apple", "banana", "cherry")
    .map(String::toUpperCase)
    .subscribe(System.out::println);
// APPLE, BANANA, CHERRY
```
**filter — keep matching elements** 
```java
Flux.range(1, 10)
    .filter(n -> n % 2 == 0)
    .subscribe(System.out::println);
// 2, 4, 6, 8, 10
```
**flatMap — async transformation (returns a Publisher)** 
```java
Flux.just(1, 2, 3)
    .flatMap(id -> fetchUserFromDb(id)) // each call returns Mono<User>
    .subscribe(user -> System.out.println(user.getName()));
```

The key difference: ==`map` is synchronous (value → value), `flatMap` is asynchronous (value → Publisher).==

**zip — combine multiple streams** 
```java
Mono<String> name = Mono.just("Alice");
Mono<Integer> age = Mono.just(30);
Mono.zip(name, age)
    .map(tuple -> tuple.getT1() + " is " + tuple.getT2())
    .subscribe(System.out::println);
// "Alice is 30"
```

**Error Handling** 
```java
Flux.just(1, 2, 0, 4)
    .map(n -> 10 / n) // will throw ArithmeticException on 0
    .onErrorReturn(-1) // fallback value on any error
    .subscribe(System.out::println);
// 10, 5, -1
// Or recover with a different publisher
Flux.just(1, 2, 0)
    .map(n -> 10 / n)
    .onErrorResume(e -> Flux.just(99, 100))
    .subscribe(System.out::println);
// 10, 5, 99, 100
```

**Backpressure — Controlling Flow** 
==When a publisher is faster than the subscriber, backpressure lets the subscriber control the rate:==
```java
Flux.range(1, 1000)
    .onBackpressureDrop(dropped -> System.out.println("Dropped: " + dropped))
    .subscribe(new BaseSubscriber<>() {
        @Override
        protected void hookOnSubscribe(Subscription sub) {
            request(5); // only ask for 5 items initially
        }
        @Override
        protected void hookOnNext(Integer value) {
            System.out.println("Got: " + value);
            request(1); // ask for 1 more after processing
        }
    });
```

Other backpressure strategies: `onBackpressureBuffer()`, `onBackpressureLatest()`.
**Real-World Example: REST API with Spring WebFlux** 
```java
@RestController
public class UserController {
    private final UserRepository repo; // ReactiveMongoRepository
    @GetMapping("/users")
    public Flux<User> getAllUsers() {
        return repo.findAll();
    }
    @GetMapping("/users/{id}")
    public Mono<ResponseEntity<User>> getUser(@PathVariable String id) {
        return repo.findById(id)
            .map(ResponseEntity::ok)
            .defaultIfEmpty(ResponseEntity.notFound().build());
    }
    @PostMapping("/users")
    public Mono<User> createUser(@RequestBody User user) {
        return repo.save(user);
    }
}
```

**Combining Multiple Async Calls** 
```java hl:6,8,9,13
// Run two calls in parallel and combine results
public Mono<Dashboard> getDashboard(String userId) {
    Mono<User> user = userService.findById(userId);
    Mono<List<Order>> orders = orderService.findByUserId(userId);
    Mono<List<Notification>> notifs = notifService.findByUserId(userId);
    return Mono.zip(user, orders, notifs)
        .map(tuple -> new Dashboard(
            tuple.getT1(),
            tuple.getT2(),
            tuple.getT3()
        ));
}
// All three calls run concurrently — no thread blocking
```

**Key Mental Model** 

| Imperative (blocking)              | Reactive (non-blocking)                 |
| ---------------------------------- | --------------------------------------- |
| `User u = repo.findById(id)`       | `Mono<User> u = repo.findById(id)`      |
| `List<User> list = repo.findAll()` | `Flux<User> flux = repo.findAll()`      |
| `try/catch`                        | `.onErrorReturn()` / `.onErrorResume()` |
| `for` loop                         | `.map()` / `.flatMap()` / `.filter()`   |
| Thread.sleep / wait                | `.delayElements()` / `.timeout()`       |
==The data doesn’t exist yet when you build the pipeline — it flows through when someone subscribes.== Think of it as assembling plumbing before turning on the water.

----------
  
==`flatMap` **— Concurrent, Unordered**== 
==Subscribes to all inner publishers **eagerly and concurrently**. Results arrive in whatever order they complete — **order is NOT preserved**.==
```java hl:2,4
Flux.just("A", "B", "C")
    .flatMap(letter -> fetchFromApi(letter))  // all 3 fire at once
    .subscribe(System.out::println);
// Could print: B, A, C (order depends on which resolves first)
```
**Use when:** You want maximum throughput and don’t care about order (e.g., independent API calls, parallel DB lookups).

==`concatMap` **— Sequential, Ordered**== 
==Subscribes to each inner publisher **one at a time**, waiting for the previous one to complete before starting the next. **Order is always preserved.**==
```java
Flux.just("A", "B", "C")
    .concatMap(letter -> fetchFromApi(letter))  // A finishes, then B, then C
    .subscribe(System.out::println);
// Always prints: A, B, C
```
**Use when:** Order matters or operations must be sequential (e.g., dependent calls, write-then-read, rate-limited APIs).

==`flatMapMany` **— flatMap for Mono → Flux**== 
==Only exists on `Mono`. It’s used when a `Mono` operation returns a `Flux` (multiple items). Regular `flatMap` on a Mono can only return a `Mono`. `flatMapMany` lets you return a `Flux` instead.==
```java
Mono.just("sci-fi")
    .flatMapMany(genre -> bookRepo.findByGenre(genre))  // returns Flux<Book>
    .subscribe(System.out::println);
// Prints all sci-fi books
```

Without `flatMapMany`, you’d be stuck:
```java
// flatMap on Mono must return Mono — this won't compile:
Mono.just("sci-fi")
    .flatMap(genre -> bookRepo.findByGenre(genre))  // ✗ Flux, not Mono
```

**Side-by-Side** 
||`flatMap`|`concatMap`|`flatMapMany`|
|---|---|---|---|
|**Available on**|Mono + Flux|Mono + Flux|Mono only|
|**Concurrency**|All at once|One at a time|N/A (single Mono source)|
|**Order preserved**|No|Yes|Yes|
|**Speed**|Fastest|Slowest|N/A|
|**Returns**|Same type (Flux→Flux)|Same type (Flux→Flux)|Mono → Flux|

**When to Pick What** 
```java
// Independent lookups, order doesn't matter → flatMap
userIds.flatMap(id -> userService.findById(id))
// Must process in sequence → concatMap  
steps.concatMap(step -> executeStep(step))
// Mono produces multiple results → flatMapMany
Mono.just(userId)
    .flatMapMany(id -> orderRepo.findByUserId(id))
```

One more nuance: `flatMap` accepts an optional **concurrency** parameter to limit parallelism, which makes it a middle ground:
```java
// At most 4 concurrent calls — still unordered
ids.flatMap(id -> callApi(id), 4)
```
If you want concurrency **and** order, use `flatMapSequential` — it fires all inner publishers concurrently but re-orders the output to match the input sequence.

---------
Intermediate Spring Boot Reactive Programming 

**01 — Schedulers & Threading** 
Reactor is single-threaded by default. `Schedulers` let you control which thread pool runs each stage of your pipeline. Two key operators: `subscribeOn` (where the source runs) and `publishOn` (where downstream operators run).  
**Scheduler Types:**

| Scheduler                     | Thread Pool                                | Use For                         |
| ----------------------------- | ------------------------------------------ | ------------------------------- |
| `boundedElastic()`            | Grows up to 10× cores, idle threads expire | Blocking I/O (JDBC, file reads) |
| `parallel()`                  | Fixed, equal to CPU cores                  | CPU-intensive computation       |
| `single()`                    | Single reusable thread                     | Serial, low-latency tasks       |
| `immediate()`                 | Caller’s thread                            | Testing, no context switch      |
| **subscribeOn vs publishOn:** |                                            |                                 |

```java
Flux.range(1, 5)
    .map(i -> {
        // runs on boundedElastic thread
        return blockingDbCall(i);
    })
    .subscribeOn(Schedulers.boundedElastic())  // affects the SOURCE
    .publishOn(Schedulers.parallel())          // affects DOWNSTREAM
    .map(data -> {
        // runs on parallel thread — CPU work here
        return heavyComputation(data);
    })
    .subscribe();
```
**Key distinction:** `subscribeOn` can appear anywhere in the chain and affects where the subscription starts (the source). `publishOn` switches the thread for everything _after_ it. You can use multiple `publishOn` calls but only the first `subscribeOn` matters.  
**Wrapping Blocking Code Safely:**  
```java
@Service
public class LegacyService {
    private final JdbcTemplate jdbc; // blocking!
    public Mono<User> findUser(String id) {
        return Mono.fromCallable(() ->
                jdbc.queryForObject(
                    "SELECT * FROM users WHERE id = ?",
                    userRowMapper, id))
            .subscribeOn(Schedulers.boundedElastic());
        // wraps blocking call on a safe thread pool
    }
}
```

**02 — Hot vs Cold Publishers** 
**Cold Publisher:** Data is generated _per subscriber_. Each subscriber gets the full sequence from the start. Like a DVD — each viewer watches from the beginning.
```java
// Each subscriber triggers a new DB query
Flux<Book> books = repo.findAll();
books.subscribe(b -> ...); // query 1
books.subscribe(b -> ...); // query 2
```

**Hot Publisher:** Data flows _regardless of subscribers_. Late subscribers miss earlier items. Like live TV — you see whatever’s on now.
```java
// Shared stream — all see the same events
Sinks.Many<String> sink =
    Sinks.many().multicast().onBackpressureBuffer();
sink.asFlux().subscribe(s -> ...);
sink.tryEmitNext("event1");
```

**Converting Cold to Hot:**
```java
// share() — multicasts to multiple subscribers, replays nothing
Flux<Long> ticker = Flux.interval(Duration.ofSeconds(1))
    .share();  // now hot — late subscribers miss past ticks
// cache() — replays ALL past items to new subscribers
Flux<Config> config = loadConfig()
    .cache(Duration.ofMinutes(5));  // hot + replay with TTL
// replay().refCount() — replay last N to new subscribers
Flux<Price> prices = priceStream()
    .replay(3)      // buffer last 3
    .refCount(1);   // auto-connect with 1 subscriber
```

**Sinks — Programmatic Hot Publishers:**
```java
@Component
public class EventBus {
    private final Sinks.Many<OrderEvent> sink =
        Sinks.many().multicast().onBackpressureBuffer();
    public void publish(OrderEvent event) {
        sink.tryEmitNext(event);
    }
    public Flux<OrderEvent> stream() {
        return sink.asFlux();
    }
}
// Controller: push events as SSE
@GetMapping(value = "/events",
    produces = MediaType.TEXT_EVENT_STREAM_VALUE)
public Flux<OrderEvent> events() {
    return eventBus.stream();
}
```

**03 — Reactor Context** 
==In reactive code there is no ThreadLocal. Reactor Context is the replacement — it flows with the subscription signal (bottom to top), carrying metadata like user IDs, trace IDs, or locale.==
```java
// Writing context: attach a correlation ID
public Mono<Book> findBook(String id, String traceId) {
    return repo.findById(id)
        .contextWrite(Context.of("traceId", traceId));
}
// Reading context: use it in a downstream operator
public Mono<Book> findBookWithLogging(String id) {
    return Mono.deferContextual(ctx -> {
        String traceId = ctx.getOrDefault("traceId", "none");
        log.info("[{}] Looking up book {}", traceId, id);
        return repo.findById(id);
    });
}
```

**WebFilter — Attach Context to Every Request:**
```java hl:2,12,13
@Component
public class TraceFilter implements WebFilter {
    @Override
    public Mono<Void> filter(ServerWebExchange exchange,
                             WebFilterChain chain) {
        String traceId = exchange.getRequest()
            .getHeaders()
            .getFirst("X-Trace-Id");
        if (traceId == null) {
            traceId = UUID.randomUUID().toString();
        }
        return chain.filter(exchange)
            .contextWrite(Context.of("traceId", traceId));
    }
}
```

**04 — Retry & Timeout Strategies** 
```java hl:8,9
public Mono<ExternalData> fetchExternal(String url) {
    return webClient.get()
        .uri(url)
        .retrieve()
        .bodyToMono(ExternalData.class)
        // 1. Timeout after 3 seconds
        .timeout(Duration.ofSeconds(3))
        // 2. Retry with exponential backoff
        .retryWhen(Retry.backoff(3, Duration.ofMillis(500))
            .maxBackoff(Duration.ofSeconds(5))
            .filter(ex -> ex instanceof WebClientResponseException.
                              ServiceUnavailable
                         || ex instanceof TimeoutException)
            .onRetryExhaustedThrow((spec, signal) ->
                new ServiceUnavailableException(
                    "Service down after retries")))
        // 3. Fallback if all retries fail
        .onErrorResume(ServiceUnavailableException.class,
            ex -> Mono.just(ExternalData.fallback()));
}
```
**Retry Strategies Compared:**

| Method                    | Behavior                            | When to Use                             |
| ------------------------- | ----------------------------------- | --------------------------------------- |
| `retry(n)`                | Retry immediately, up to n times    | Transient local errors                  |
| `Retry.fixedDelay(n, d)`  | Wait fixed duration between retries | Rate-limited APIs                       |
| ==`Retry.backoff(n, d)`== | ==Exponential backoff with jitter== | ==External services (most common)==     |
| `Retry.indefinitely()`    | Never stop retrying                 | Critical connections (use with caution) |
**05 — Parallel Flux** 
Process items across multiple CPU cores. ==`parallel()` splits the Flux into rails, `runOn()` assigns a scheduler, and `sequential()` merges them back.==
```java hl:3,4,7
public Flux<ProcessedImage> processImages(List<Image> images) {
    return Flux.fromIterable(images)
        .parallel(4)                        // split into 4 rails
        .runOn(Schedulers.parallel())       // assign CPU threads
        .map(this::resize)                  // CPU work in parallel
        .map(this::compress)                // still parallel
        .sequential()                       // merge back to Flux
        .flatMap(img -> repo.save(img));    // save reactively
}
// Alternative: flatMap with concurrency control
public Flux<Result> callApiConcurrently(List<String> ids) {
    return Flux.fromIterable(ids)
        .flatMap(id -> callApi(id), 8);  // max 8 concurrent
}
```
**When to use which:** ==Use `parallel()` for CPU-bound work (image processing, hashing). Use `flatMap(fn, concurrency)` for I/O-bound concurrent calls (HTTP, DB).== The concurrency parameter in flatMap is often the better choice for real applications.

**06 — Window & Buffer** 
==Group elements for batch processing. `buffer` collects into Lists,== `window` collects into sub-Fluxes (lazy).
```java hl:3,8
// buffer: collect into fixed-size Lists
Flux.range(1, 100)
    .buffer(10)  // List<Integer> of size 10
    .flatMap(batch -> saveBatch(batch))
    .subscribe();
// buffer by time: flush every 2 seconds
eventFlux
    .bufferTimeout(50, Duration.ofSeconds(2))
    // emits every 2s OR when 50 items accumulate
    .flatMap(batch -> bulkInsert(batch))
    .subscribe();
// window: like buffer but emits Flux<Flux<T>> (lazy)
Flux.range(1, 100)
    .window(10)  // Flux<Flux<Integer>>
    .flatMap(window -> window.collectList()
        .flatMap(this::processChunk))
    .subscribe();
// groupBy: split by key
orderFlux
    .groupBy(Order::getRegion)  // Flux<GroupedFlux<String, Order>>
    .flatMap(group -> group
        .count()
        .map(count -> group.key() + ": " + count))
    .subscribe(System.out::println);
    // "US: 42", "EU: 28", "APAC: 15"
```

| Operator      | Output Type              | Memory                   | Best For                 |
| ------------- | ------------------------ | ------------------------ | ------------------------ |
| `buffer(n)`   | `Flux<List<T>>`          | Holds n items in memory  | Batch inserts, API calls |
| `window(n)`   | `Flux<Flux<T>>`          | Lazy, low memory         | Processing large streams |
| `groupBy(fn)` | `Flux<GroupedFlux<K,T>>` | Holds one buffer per key | Categorization, routing  |
**07 — Caching Reactive Streams** 
```java hl:4
// Simple cache: replays results to all subscribers for 5 min
private final Mono<Config> configCache =
    loadConfigFromDb()
        .cache(Duration.ofMinutes(5));
public Mono<Config> getConfig() {
    return configCache; // all callers share same cached result
}
```

**CacheMono with Caffeine (External Cache):**
```java
@Service
public class CachedBookService {
    private final BookRepository repo;
    private final Cache<String, Book> cache =
        Caffeine.newBuilder()
            .maximumSize(1000)
            .expireAfterWrite(Duration.ofMinutes(10))
            .build();
    public Mono<Book> findById(String id) {
        Book cached = cache.getIfPresent(id);
        if (cached != null) {
            return Mono.just(cached);
        }
        return repo.findById(id)
            .doOnNext(book -> cache.put(id, book));
    }
    // Invalidate on write
    public Mono<Book> update(Book book) {
        return repo.save(book)
            .doOnNext(saved -> cache.invalidate(saved.getId()));
    }
}
```

**Gotcha:** `.cache()` on a Mono that errors will cache the error too. Use the overloaded version with separate TTLs for values and errors, or wrap with `onErrorResume` before caching.

**08 — R2DBC (Reactive SQL)** 
==R2DBC brings reactive drivers to relational databases (PostgreSQL, MySQL, H2). It replaces JDBC with non-blocking I/O.==  
**Dependencies:**
```xml
<dependency>
    <groupId>org.springframework.boot</groupId>
    <artifactId>spring-boot-starter-data-r2dbc</artifactId>
</dependency>
<dependency>
    <groupId>org.postgresql</groupId>
    <artifactId>r2dbc-postgresql</artifactId>
</dependency>
```
**application.yml:**
```yaml
spring:
  r2dbc:
    url: r2dbc:postgresql://localhost:5432/bookstore
    username: demo
    password: demo
```
**Repository:**
```java hl:2
public interface OrderRepository
        extends ReactiveCrudRepository<Order, Long> {
    // Derived queries — same as JPA but returns Flux/Mono
    Flux<Order> findByCustomerId(String customerId);
    Flux<Order> findByStatusOrderByCreatedAtDesc(String status);
    // Custom query
    @Query("SELECT * FROM orders WHERE total > :min " +
           "AND created_at > :since")
    Flux<Order> findLargeRecentOrders(
        Double min, LocalDateTime since);
    // Aggregation
    @Query("SELECT region, SUM(total) as total " +
           "FROM orders GROUP BY region")
    Flux<RegionTotal> totalsByRegion();
}
```

**Reactive Transactions:**
```java hl:5
@Service
public class OrderService {
    private final OrderRepository orderRepo;
    private final InventoryRepository invRepo;
    @Transactional  // reactive transaction!
    public Mono<Order> placeOrder(Order order) {
        return invRepo.findById(order.getProductId())
            .flatMap(inv -> {
                if (inv.getQty() < order.getQty()) {
                    return Mono.error(
                        new InsufficientStockException());
                }
                inv.setQty(inv.getQty() - order.getQty());
                return invRepo.save(inv);
            })
            .then(orderRepo.save(order));
        // if any step fails, entire transaction rolls back
    }
}
```
==**R2DBC vs Reactive Mongo:** R2DBC is for SQL databases (Postgres, MySQL). The repository pattern is nearly identical — `ReactiveCrudRepository` instead of `ReactiveMongoRepository`. The main difference: R2DBC supports `@Transactional` for ACID transactions, while Mongo transactions require `ReactiveMongoTransactionManager`.==

**09 — Lifecycle Hooks** 
Side-effect operators that don’t modify the stream but let you observe signals — essential for logging, metrics, and debugging.
```java
public Flux<Book> findAllInstrumented() {
    return repo.findAll()
        .doOnSubscribe(sub ->
            log.info("Query started"))
        .doOnNext(book ->
            metrics.increment("books.fetched"))
        .doOnError(err ->
            log.error("Query failed: {}", err.getMessage()))
        .doOnComplete(() ->
            log.info("Query completed"))
        .doFinally(signal ->
            log.info("Finished with signal: {}", signal))
        .log("BookQuery", Level.FINE);
}
```

| Hook            | Fires When                    | Common Use                       |
| --------------- | ----------------------------- | -------------------------------- |
| `doOnSubscribe` | Someone subscribes            | Start timers, log entry          |
| `doOnNext`      | Each element emitted          | Metrics, audit logging           |
| `doOnError`     | Error signal                  | Error logging, alerts            |
| `doOnComplete`  | Stream completes normally     | Success logging                  |
| `doOnCancel`    | Subscriber cancels            | Resource cleanup                 |
| `doFinally`     | Any terminal signal           | Cleanup (like try-finally)       |
| `doOnEach`      | Every signal including onNext | Tracing, generic instrumentation |

**10 — Advanced Testing Patterns** 
**Virtual Time — Testing Delays Without Waiting:**
```java
@Test
void testDelayedFlux_withVirtualTime() {
    // This emits every 1 hour — we don't want to wait 3 hours
    StepVerifier.withVirtualTime(() ->
        Flux.interval(Duration.ofHours(1)).take(3)
    )
    .expectSubscription()
    .thenAwait(Duration.ofHours(3)) // fast-forward 3 hours instantly
    .expectNext(0L, 1L, 2L)
    .verifyComplete();
}
@Test
void testTimeout() {
    StepVerifier.withVirtualTime(() ->
        Mono.delay(Duration.ofSeconds(10))
            .timeout(Duration.ofSeconds(5))
    )
    .expectSubscription()
    .thenAwait(Duration.ofSeconds(5))
    .expectError(TimeoutException.class)
    .verify();
}
```

**TestPublisher — Simulate Edge Cases:**
```java
@Test
void testServiceHandlesSlowPublisher() {
    TestPublisher<Book> publisher = TestPublisher.create();
    StepVerifier.create(
        publisher.flux()
            .map(Book::getTitle)
    )
    .then(() -> publisher.next(book1))
    .expectNext("Dune")
    .then(() -> publisher.next(book2))
    .expectNext("1984")
    .then(() -> publisher.complete())
    .verifyComplete();
}
@Test
void testServiceHandlesError() {
    TestPublisher<Book> publisher = TestPublisher.create();
    StepVerifier.create(
        publisher.flux().onErrorReturn(fallbackBook)
    )
    .then(() -> publisher.error(
        new RuntimeException("DB down")))
    .expectNext(fallbackBook)
    .verifyComplete();
}
```

**PublisherProbe — Verify a Branch Was Taken:**
```java
@Test
void testFallbackWasUsed() {
    PublisherProbe<Book> fallbackProbe =
        PublisherProbe.of(Mono.just(defaultBook));
    Mono<Book> result = repo.findById("missing")
        .switchIfEmpty(fallbackProbe.mono());
    StepVerifier.create(result)
        .expectNext(defaultBook)
        .verifyComplete();
    // Assert the fallback branch was actually subscribed
    fallbackProbe.assertWasSubscribed();
    fallbackProbe.assertWasRequested();
}
```

  

**WebTestClient — Integration Testing:**

```java
@SpringBootTest(webEnvironment = WebEnvironment.RANDOM_PORT)
class BookControllerIT {
    @Autowired
    WebTestClient client;
    @Test
    void testCreateAndFetchBook() {
        Book book = new Book("Dune", "Herbert",
                             15.0, "sci-fi", 10);
        // Create
        client.post().uri("/api/books")
            .bodyValue(book)
            .exchange()
            .expectStatus().isCreated()
            .expectBody(Book.class)
            .value(created -> {
                assertNotNull(created.getId());
                assertEquals("Dune", created.getTitle());
            });
        // Fetch all
        client.get().uri("/api/books")
            .exchange()
            .expectStatus().isOk()
            .expectBodyList(Book.class)
            .hasSize(1);
    }
    @Test
    void testSSEStream() {
        client.get().uri("/api/books/stream")
            .accept(MediaType.TEXT_EVENT_STREAM)
            .exchange()
            .expectStatus().isOk()
            .returnResult(Book.class)
            .getResponseBody()
            .as(StepVerifier::create)
            .expectNextCount(3)
            .thenCancel()
            .verify();
    }
}
```

  

**Quick Reference** 

|Concept|Key Operators|Use When|
|---|---|---|
|Schedulers|`subscribeOn`, `publishOn`|Wrapping blocking code, CPU-intensive work|
|Hot vs Cold|`share()`, `cache()`, `Sinks`|Multiple subscribers, event broadcasting|
|Context|`contextWrite`, `deferContextual`|Replacing ThreadLocal (trace IDs, auth)|
|Retry|`retryWhen(Retry.backoff())`|Resilient external service calls|
|Parallel|`parallel(n).runOn()`|CPU-bound batch processing|
|Window/Buffer|`buffer(n)`, `window(n)`, `groupBy`|Batch inserts, stream chunking|
|Caching|`cache(ttl)`, Caffeine|Expensive lookups, config loading|
|R2DBC|`ReactiveCrudRepository`, `@Transactional`|Reactive SQL (Postgres, MySQL)|
|Hooks|`doOnNext`, `doFinally`, `log()`|Logging, metrics, debugging|
|Testing|`StepVerifier`, `withVirtualTime`, `TestPublisher`|Asserting on reactive pipelines|
NextJs Fundamentals and medium level concepts with examples. please create them inline in the chat here so i can copy them into obsidian, also remove empty line breaks between text.