# Exercise 4: Reactive Pipeline Composition

## Scenario

You're building an order processing pipeline. Each order goes through: validation → inventory check → payment → fulfillment → notification. Some steps are parallel, some sequential.

---

## Task 1: Sequential Pipeline

```java
// TODO: Build a pipeline where each step depends on the previous.
// If any step fails, the pipeline should short-circuit.
// Each step returns Mono<StepResult>

public Mono<OrderResult> processOrder(OrderRequest request) {
    // 1. validateOrder(request) → Mono<ValidatedOrder>
    // 2. checkInventory(validatedOrder) → Mono<InventoryReservation>
    // 3. processPayment(reservation) → Mono<PaymentConfirmation>
    // 4. createFulfillment(payment) → Mono<FulfillmentOrder>
    // 5. sendNotification(fulfillment) → Mono<Void>
    // Return OrderResult combining all step outputs
}
```

---

## Task 2: Parallel Fan-Out with Sequential Dependency

```java
// After validation, check inventory AND check fraud score in parallel.
// Both must pass before proceeding to payment.

public Mono<OrderResult> processOrderV2(OrderRequest request) {
    // TODO:
    // 1. validateOrder(request) → Mono<ValidatedOrder>
    // 2. PARALLEL:
    //    a. checkInventory(order) → Mono<InventoryResult>
    //    b. checkFraud(order) → Mono<FraudResult>
    //    (use Mono.zip)
    // 3. If fraud score > 0.8, reject
    // 4. processPayment(...) → Mono<PaymentResult>
    // 5. PARALLEL:
    //    a. createFulfillment(...)
    //    b. updateAnalytics(...)
    //    c. sendNotification(...)
    //    (use Mono.when for fire-and-forget)
}
```

---

## Task 3: Saga Pattern with Compensating Actions

```java
// If payment succeeds but fulfillment fails, you must REFUND the payment.
// Implement compensating transactions.

public Mono<OrderResult> processWithSaga(OrderRequest request) {
    // TODO:
    // 1. Reserve inventory
    // 2. Charge payment
    // 3. Create fulfillment
    //    - If fails: refund payment, then release inventory
    // 4. Send notification
    //    - If fails: log warning but don't rollback (non-critical)

    // Hint: use onErrorResume to trigger compensating actions
    // then re-throw the original error
}
```

---

## Task 4: Batch Processing Pipeline

```java
// Process a CSV of 10,000 orders. Requirements:
// - Process in batches of 100
// - Max 5 batches processing concurrently
// - Track progress: emit a ProgressEvent every 100 items
// - If one item in a batch fails, skip it (don't fail the batch)
// - Collect final stats: success count, failure count, processing time

public Mono<BatchResult> processBatch(Flux<OrderRequest> orders) {
    // TODO:
    // 1. buffer(100)
    // 2. flatMap(batch -> processBatchItems(batch), concurrency = 5)
    // 3. Each item: processOrder(item)
    //      .onErrorResume(ex -> log + return Failure)
    // 4. Reduce into BatchResult
}
```

---

## Task 5: Event Stream Processing

```java
// Process a real-time stream of price updates.
// Requirements:
// - Deduplicate by symbol (ignore if same price as last)
// - Calculate 5-second moving average per symbol
// - Emit alert if price changes > 5% from moving average
// - Rate limit alerts to max 1 per symbol per 10 seconds

public Flux<PriceAlert> monitorPrices(Flux<PriceUpdate> updates) {
    // TODO:
    // 1. groupBy(symbol)
    // 2. For each group:
    //    a. distinctUntilChanged(PriceUpdate::price)
    //    b. window(Duration.ofSeconds(5))
    //    c. Calculate average per window
    //    d. Compare latest price to average
    //    e. sample(Duration.ofSeconds(10)) for rate limiting
}
```

---

## Complete Solutions

### Task 1: Sequential Pipeline

```java
public Mono<OrderResult> processOrder(OrderRequest request) {
    return validateOrder(request)
        .flatMap(validated -> checkInventory(validated)
            .map(reservation -> new OrderContext(validated, reservation)))
        .flatMap(ctx -> processPayment(ctx.reservation())
            .map(payment -> ctx.withPayment(payment)))
        .flatMap(ctx -> createFulfillment(ctx.payment())
            .map(fulfillment -> ctx.withFulfillment(fulfillment)))
        .flatMap(ctx -> sendNotification(ctx.fulfillment())
            .thenReturn(new OrderResult(
                ctx.order(), ctx.reservation(),
                ctx.payment(), ctx.fulfillment()
            )));
}

// Helper record to carry state through the pipeline
record OrderContext(
    ValidatedOrder order,
    InventoryReservation reservation,
    PaymentConfirmation payment,
    FulfillmentOrder fulfillment
) {
    OrderContext(ValidatedOrder order, InventoryReservation reservation) {
        this(order, reservation, null, null);
    }
    OrderContext withPayment(PaymentConfirmation p) {
        return new OrderContext(order, reservation, p, null);
    }
    OrderContext withFulfillment(FulfillmentOrder f) {
        return new OrderContext(order, reservation, payment, f);
    }
}
```

### Task 2: Parallel Fan-Out with Sequential Dependency

```java
public Mono<OrderResult> processOrderV2(OrderRequest request) {
    return validateOrder(request)
        .flatMap(order -> Mono.zip(
            checkInventory(order),
            checkFraud(order)
        ).flatMap(tuple -> {
            InventoryResult inv = tuple.getT1();
            FraudResult fraud = tuple.getT2();

            if (fraud.score() > 0.8) {
                return Mono.error(new FraudRejectedException(fraud));
            }

            return processPayment(order, inv);
        }))
        .flatMap(payment -> {
            Mono<Void> sideEffects = Mono.when(
                createFulfillment(payment),
                updateAnalytics(payment),
                sendNotification(payment)
                    .onErrorResume(ex -> {
                        log.warn("Notification failed", ex);
                        return Mono.empty();
                    })
            );
            return sideEffects.thenReturn(new OrderResult(payment));
        });
}
```

### Task 3: Saga Pattern with Compensating Actions

```java
public Mono<OrderResult> processWithSaga(OrderRequest request) {
    return reserveInventory(request)
        .flatMap(reservation ->
            chargePayment(request, reservation)
                .flatMap(payment ->
                    createFulfillment(payment)
                        .onErrorResume(ex ->
                            // Compensate: refund then release inventory
                            refundPayment(payment)
                                .then(releaseInventory(reservation))
                                .then(Mono.error(ex))
                        )
                )
                .onErrorResume(PaymentException.class, ex ->
                    // Compensate: release inventory (payment never charged)
                    releaseInventory(reservation)
                        .then(Mono.error(ex))
                )
        )
        .flatMap(fulfillment ->
            sendNotification(fulfillment)
                .onErrorResume(ex -> {
                    // Non-critical: log but don't rollback
                    log.warn("Notification failed", ex);
                    return Mono.empty();
                })
                .thenReturn(new OrderResult(fulfillment))
        );
}
```

### Task 4: Batch Processing Pipeline

```java
public Mono<BatchResult> processBatch(Flux<OrderRequest> orders) {
    long startTime = System.currentTimeMillis();

    return orders
        .buffer(100)  // Group into batches of 100
        .flatMap(batch ->
            Flux.fromIterable(batch)
                .flatMap(order ->
                    processOrder(order)
                        .map(result -> ItemResult.success(result))
                        .onErrorResume(ex -> {
                            log.warn("Item failed: {}", ex.getMessage());
                            return Mono.just(ItemResult.failure(ex));
                        })
                )
                .collectList(),
            5  // Max 5 batches concurrently
        )
        .flatMapIterable(list -> list)  // Flatten List<ItemResult> into stream
        .reduce(new BatchStats(), (stats, item) -> {
            if (item.isSuccess()) stats.incrementSuccess();
            else stats.incrementFailure();
            return stats;
        })
        .map(stats -> new BatchResult(
            stats.successCount(),
            stats.failureCount(),
            System.currentTimeMillis() - startTime
        ));
}

record ItemResult(boolean success, OrderResult result, Throwable error) {
    static ItemResult success(OrderResult r) { return new ItemResult(true, r, null); }
    static ItemResult failure(Throwable e) { return new ItemResult(false, null, e); }
    boolean isSuccess() { return success; }
}
```

### Task 5: Event Stream Processing

```java
public Flux<PriceAlert> monitorPrices(Flux<PriceUpdate> updates) {
    return updates
        .groupBy(PriceUpdate::symbol)
        .flatMap(group -> {
            String symbol = group.key();

            return group
                // Deduplicate: ignore if same price as last
                .distinctUntilChanged(PriceUpdate::price)
                // Window into 5-second intervals
                .window(Duration.ofSeconds(5))
                .flatMap(window ->
                    window.collectList().flatMap(prices -> {
                        if (prices.isEmpty()) return Mono.empty();

                        double avg = prices.stream()
                            .mapToDouble(PriceUpdate::price)
                            .average().orElse(0);
                        PriceUpdate latest = prices.get(prices.size() - 1);
                        double changePercent =
                            Math.abs(latest.price() - avg) / avg * 100;

                        if (changePercent > 5.0) {
                            return Mono.just(new PriceAlert(
                                symbol, latest.price(), avg, changePercent
                            ));
                        }
                        return Mono.empty();
                    })
                )
                // Rate limit: max 1 alert per 10 seconds per symbol
                .sample(Duration.ofSeconds(10));
        });
}
```
