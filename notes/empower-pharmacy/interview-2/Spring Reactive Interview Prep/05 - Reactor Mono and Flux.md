# Reactor: Mono & Flux Foundations

## Reactive Streams Spec

Four interfaces from `org.reactivestreams`:

```java
Publisher<T>    // produces items
Subscriber<T>  // consumes items
Subscription   // connects publisher → subscriber, handles backpressure
Processor<T,R> // both publisher and subscriber
```

**Key rule**: nothing happens until you subscribe. Reactive streams are lazy.

---

## Mono< T> — 0 or 1 item

```java
// Creation
Mono<String> empty = Mono.empty();
Mono<String> just = Mono.just("hello");
Mono<String> deferred = Mono.fromCallable(() -> expensiveCall());
Mono<String> fromFuture = Mono.fromFuture(completableFuture);
Mono<User> fromOptional = Mono.justOrEmpty(optionalUser);
Mono<String> error = Mono.error(new RuntimeException("boom"));

// Deferred — lazily evaluated each subscription
Mono<Instant> now = Mono.defer(() -> Mono.just(Instant.now()));

// Transformation
mono.map(s -> s.toUpperCase())              // sync transform
    .flatMap(s -> callApi(s))               // async transform → Mono
    .filter(s -> s.length() > 3)            // filter (may become empty)
    .defaultIfEmpty("fallback")             // provide default
    .switchIfEmpty(Mono.defer(() -> alt())) // switch to another Mono
    .cache()                                // cache the result
    .delayElement(Duration.ofMillis(100))   // delay emission

// Side effects (don't transform the signal)
mono.doOnSubscribe(sub -> log.info("subscribed"))
    .doOnNext(val -> log.info("got: {}", val))
    .doOnError(err -> log.error("failed", err))
    .doOnSuccess(val -> log.info("completed with: {}", val))
    .doFinally(signal -> cleanup())
```

---

## Flux< T> — 0 to N items

```java
// Creation
Flux<Integer> range = Flux.range(1, 10);
Flux<String> fromList = Flux.fromIterable(List.of("a", "b", "c"));
Flux<Long> interval = Flux.interval(Duration.ofSeconds(1));
Flux<String> concat = Flux.concat(flux1, flux2);  // sequential
Flux<String> merge = Flux.merge(flux1, flux2);     // interleaved

// Transformation
flux.map(i -> i * 2)
    .flatMap(i -> fetchItem(i))         // async, interleaved order
    .concatMap(i -> fetchItem(i))       // async, preserves order
    .flatMapSequential(i -> fetchItem(i)) // async, ordered but concurrent
    .filter(i -> i > 5)
    .distinct()
    .take(10)                           // first 10 items
    .skip(5)                            // skip first 5
    .collectList()                      // Flux<T> → Mono<List<T>>
    .reduce(0, Integer::sum)            // Flux<T> → Mono<T>

// Buffering & windowing
flux.buffer(5)                          // Flux<T> → Flux<List<T>>
    .window(Duration.ofSeconds(1))      // Flux<T> → Flux<Flux<T>>
    .groupBy(item -> item.getCategory()) // Flux<T> → Flux<GroupedFlux<K,T>>
```

---

## Mono vs Flux: When to Use

| Use Case | Type |
|----------|------|
| Single API call result | Mono |
| Database findById | Mono |
| Database findAll / query | Flux |
| Stream of events | Flux |
| Void operation | Mono<Void> |
| Aggregate multiple results | Flux → collectList() → Mono<List> |

---

## Subscribing (terminal operations)

```java
// In production, the framework subscribes for you.
// For testing or standalone use:
mono.subscribe();                              // fire and forget
mono.subscribe(val -> process(val));           // with consumer
mono.subscribe(val -> process(val), err -> handleError(err));
mono.subscribe(val -> process(val), err -> handleError(err), () -> onComplete());

// Blocking (ONLY in tests or imperative code — never in reactive chain)
String result = mono.block();
String result = mono.block(Duration.ofSeconds(5));
List<String> all = flux.collectList().block();
```

---

## Hot vs Cold Publishers

```java
// COLD — replays from start for each subscriber
Mono<String> cold = Mono.fromCallable(() -> fetchData());
// Each subscriber gets its own call to fetchData()

// HOT — shares emissions across subscribers
Sinks.Many<String> sink = Sinks.many().multicast().onBackpressureBuffer();
Flux<String> hot = sink.asFlux();
sink.tryEmitNext("event1"); // all subscribers receive this

// Convert cold to hot
Flux<String> shared = coldFlux.share(); // multicast, auto-connect on first sub
Flux<String> replayed = coldFlux.cache(3); // replay last 3 to new subs
```

---

## Interview Questions & Answers

### => 1. What's the difference between Mono and Flux? When do you use each?
**Mono< T>**: Emits 0 or 1 item, then completes. Think of it as a reactive `Optional` or `CompletableFuture`. ==Use for:== single value lookups (`findById`), single API calls, void operations (`Mono<Void>`), ==any operation that produces at most one result.==
**Flux< T>**: Emits 0 to N items, then completes. Think of it as a reactive `List` or `Stream`. ==Use for:== collection queries (`findAll`), event streams, paginated results, SSE endpoints, ==any operation that produces multiple items over time.==
**Conversion**: `flux.collectList()` converts `Flux<T>` → `Mono<List<T>>`. `Mono.flux()` converts `Mono<T>` → `Flux<T>` (single-element Flux). `flux.next()` takes the first element as a `Mono<T>`.

### => 2. What does "nothing happens until you subscribe" mean? Why is it important?
==Reactive streams are **lazy**. Building a chain like== `mono.map(...).flatMap(...).filter(...)` ==only assembles a pipeline== — no code executes, no HTTP calls are made, no database queries run. ==Execution starts only when someone calls `.subscribe()` (or the framework does it for you, like WebFlux subscribing to the returned Mono/Flux).==
This matters because:
- **No side effects during assembly**: `Mono.fromCallable(() -> sendEmail())` doesn't send the email when the Mono is created — only when subscribed. This is different from `CompletableFuture.supplyAsync()` which starts immediately.
- **Resubscription**: You can subscribe multiple times to a cold publisher. Each subscription re-executes the pipeline. `Mono.defer()` ensures the inner supplier is called fresh each time.
- **Framework control**: WebFlux subscribes to the returned Mono/Flux after applying backpressure, error handling, and context propagation. If execution started eagerly, the framework couldn't manage it.

### => 3. Explain `flatMap` vs `concatMap` vs `flatMapSequential`.
All three transform each element into a new Publisher and merge the results, but differ in ordering and concurrency:
**==flatMap**: Subscribes to all inner publishers== eagerly, interleaves results as they arrive. Fast but unordered. ==Use when order doesn't matter== and you want maximum concurrency. Concurrency is unbounded by default (configurable with `flatMap(fn, concurrency)`).
==**concatMap**: Subscribes to inner publishers== sequentially — ==waits for each publisher to complete before subscribing to the next. Preserves order== but no concurrency. ==Use when order matters== AND operations have side effects that must be sequential (e.g., writing files in order).
==**flatMapSequential**: Subscribes to inner publishers== eagerly (like `flatMap`) for concurrency, but ==queues results to emit in the original order.== Best of both worlds: concurrent execution with ordered output. Use when you want parallelism but the ==downstream consumer expects ordered results.==
```java
// flatMap: [3, 1, 2] (order depends on completion speed)
// concatMap: [1, 2, 3] (always ordered, sequential execution)
// flatMapSequential: [1, 2, 3] (always ordered, concurrent execution)
```

### => 4. What's the difference between `Mono.just()` and `Mono.defer()`?
==**Mono.just(value)**: Captures the value at assembly time.== The value is computed once, when the line executes. Every subscriber gets the same value.
```java hl:3
Mono<Instant> now = Mono.just(Instant.now()); // Instant captured NOW
// 5 seconds later...
now.subscribe(System.out::println); // Prints the ORIGINAL instant, not "now"
```
==**Mono.defer(() -> Mono.just(value))**: Defers the supplier to subscription time.== The lambda runs fresh for each subscriber.
```java hl:3
Mono<Instant> now = Mono.defer(() -> Mono.just(Instant.now()));
// 5 seconds later...
now.subscribe(System.out::println); // Prints the CURRENT instant
```
**When `defer` matters**:
- Computing values that should be fresh per subscription (timestamps, random values)
- Wrapping conditional logic: `Mono.defer(() -> condition ? monoA : monoB)` evaluates the condition at subscription time
- Preventing premature evaluation of expensive operations
- Inside `switchIfEmpty` — `switchIfEmpty(Mono.defer(() -> fallback()))` ensures the fallback isn't computed unless needed

### 5. Explain hot vs cold publishers with an example.

**Cold publisher**: Like a Netflix show — each viewer starts from episode 1. Every subscriber triggers a fresh execution of the data source. Most Reactor publishers are cold.

```java
Mono<User> user = Mono.fromCallable(() -> db.findUser(id));
user.subscribe(); // Executes db.findUser()
user.subscribe(); // Executes db.findUser() AGAIN
```

**Hot publisher**: Like a live broadcast — subscribers join mid-stream and only see new events. Data is produced regardless of subscribers.

```java
Sinks.Many<String> sink = Sinks.many().multicast().onBackpressureBuffer();
Flux<String> hot = sink.asFlux();

hot.subscribe(s -> System.out.println("Sub1: " + s));
sink.tryEmitNext("A"); // Sub1 sees "A"

hot.subscribe(s -> System.out.println("Sub2: " + s));
sink.tryEmitNext("B"); // Both see "B". Sub2 missed "A".
```
**Converting cold to hot**: `flux.share()` multicasts to all subscribers (late subscribers miss past events). `flux.cache()` replays past events to late subscribers. `flux.share()` is commonly used for event buses and SSE endpoints.

### => 6. When is it acceptable to call `.block()`?
`.block()` suspends the calling thread until the Mono/Flux completes. It's acceptable ONLY in:
1. ==**Tests**: `StepVerifier` is preferred, but `.block()` works for simple assertions==
2. **Main methods / CLI apps**: Outside a reactive runtime, you need to block to get results
3. **Imperative integration points**: When reactive code must call a blocking library (but wrap it with `subscribeOn(Schedulers.boundedElastic())` first)
4. **Spring MVC controllers**: MVC is thread-per-request anyway, so blocking a WebClient call is acceptable (though using the async callback is better)
==**Never block** inside a reactive chain,== on an event loop thread, or in WebFlux handlers. ==Blocking a Netty event loop thread deadlocks the server== — a single blocked thread can prevent ALL requests from being processed. If you must integrate blocking code in a reactive chain, use `Mono.fromCallable(() -> blockingCall()).subscribeOn(Schedulers.boundedElastic())`.
