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

## Mono< T > — 0 or 1 item

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

## Flux< T > — 0 to N items

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

## Interview Questions

1. What's the difference between Mono and Flux? When do you use each?
2. What does "nothing happens until you subscribe" mean? Why is it important?
3. Explain `flatMap` vs `concatMap` vs `flatMapSequential`.
4. What's the difference between `Mono.just()` and `Mono.defer()`?
5. Explain hot vs cold publishers with an example.
6. When is it acceptable to call `.block()`?
