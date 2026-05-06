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
    return validateOrder(request)
        .flatMap(validOrder -> checkInventory(validOrder)
            .flatMap(inv -> processPayment(inv)
                .flatMap(payment -> createFulfillment(payment)
                    .flatMap(fulfillment -> sendNotification(fulfillment)
                        .thenReturn(new OrderResult(validOrder, inv, payment, fulfillment))
                    )
                )
            )
        );
}
```

---

## Task 2: Parallel Fan-Out with Sequential Dependency

```java
// After validation, check inventory AND check fraud score in parallel.
// Both must pass before proceeding to payment.

public Mono<OrderResult> processOrderV2(OrderRequest request) {
    return validateOrder(request)
        .flatMap(order -> Mono.zip(checkInventory(order), checkFraud(order))
            .flatMap(tuple -> {
                if (tuple.getT2().score() > 0.8) {
                    return Mono.error(new FraudException("High fraud score"));
                }
                return processPayment(order, tuple.getT1());
            })
        )
        .flatMap(payment -> {
            Mono<Void> sideEffects = Mono.when(
                createFulfillment(payment),
                updateAnalytics(payment),
                sendNotification(payment).onErrorResume(ex -> Mono.empty()) // non-critical
            );
            return sideEffects.thenReturn(new OrderResult(payment));
        });
}
```

---

## Task 3: Saga Pattern with Compensating Actions

```java
// If payment succeeds but fulfillment fails, you must REFUND the payment.
// Implement compensating transactions.

public Mono<OrderResult> processWithSaga(OrderRequest request) {
    return reserveInventory(request)
        .flatMap(reservation -> chargePayment(request, reservation)
            .flatMap(payment -> createFulfillment(payment)
                .onErrorResume(ex -> refundPayment(payment)
                    .then(releaseInventory(reservation))
                    .then(Mono.error(ex))
                )
            )
            .onErrorResume(PaymentException.class, ex -> releaseInventory(reservation)
                .then(Mono.error(ex))
            )
        )
        .flatMap(fulfillment -> sendNotification(fulfillment)
            .onErrorResume(ex -> {
                log.warn("Notification failed", ex);
                return Mono.empty();
            })
            .thenReturn(new OrderResult(fulfillment))
        );
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
    return orders
        .buffer(100)
        .flatMap(batch -> Flux.fromIterable(batch)
            .flatMap(item -> processOrder(item)
                .map(res -> 1)
                .onErrorResume(ex -> {
                    log.error("Item failed", ex);
                    return Mono.just(0);
                })
            )
            .reduce(0, Integer::sum)
            .map(successCount -> new BatchStats(successCount, 100 - successCount)), 
        5) // Max 5 batches concurrently
        .reduce(new BatchResult(), BatchResult::accumulate);
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
    return updates.groupBy(PriceUpdate::getSymbol)
        .flatMap(groupedFlux -> groupedFlux
            .distinctUntilChanged(PriceUpdate::getPrice)
            .window(Duration.ofSeconds(5))
            .flatMap(window -> window.collectList()
                .filter(list -> !list.isEmpty())
                .flatMapMany(list -> {
                    double avg = list.stream().mapToDouble(PriceUpdate::getPrice).average().orElse(0.0);
                    PriceUpdate latest = list.get(list.size() - 1);
                    if (Math.abs(latest.getPrice() - avg) / avg > 0.05) {
                        return Mono.just(new PriceAlert(groupedFlux.key(), latest.getPrice(), avg));
                    }
                    return Mono.empty();
                })
            )
            .sample(Duration.ofSeconds(10))
        );
}
```

---

## Solution

```java
<details>
<summary>Task 1: Sequential Pipeline (click to reveal)</summary>
public Mono<OrderResult> processOrder(OrderRequest request) {
    return validateOrder(request)
        .flatMap(validOrder -> checkInventory(validOrder)
            .flatMap(inv -> processPayment(inv)
                .flatMap(payment -> createFulfillment(payment)
                    .flatMap(fulfillment -> sendNotification(fulfillment)
                        .thenReturn(new OrderResult(validOrder, inv, payment, fulfillment))
                    )
                )
            )
        );
}
</details>
```

```java
<details>
<summary>Task 2: Parallel fan-out (click to reveal)</summary>
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
</details>
```

```java
<details>
<summary>Task 3: Saga pattern (click to reveal)</summary>
public Mono<OrderResult> processWithSaga(OrderRequest request) {
    return reserveInventory(request)
        .flatMap(reservation ->
            chargePayment(request, reservation)
                .flatMap(payment ->
                    createFulfillment(payment)
                        .onErrorResume(ex ->
                            refundPayment(payment)
                                .then(releaseInventory(reservation))
                                .then(Mono.error(ex))
                        )
                )
                .onErrorResume(PaymentException.class, ex ->
                    releaseInventory(reservation)
                        .then(Mono.error(ex))
                )
        )
        .flatMap(fulfillment ->
            sendNotification(fulfillment)
                .onErrorResume(ex -> {
                    log.warn("Notification failed", ex);
                    return Mono.empty();
                })
                .thenReturn(new OrderResult(fulfillment))
        );
}
</details>
```

```java
<details>
<summary>Task 4: Batch Processing (click to reveal)</summary>
public Mono<BatchResult> processBatch(Flux<OrderRequest> orders) {
    return orders
        .buffer(100)
        .flatMap(batch -> Flux.fromIterable(batch)
            .flatMap(item -> processOrder(item)
                .map(res -> 1)
                .onErrorResume(ex -> {
                    log.error("Item failed", ex);
                    return Mono.just(0);
                })
            )
            .reduce(0, Integer::sum)
            .map(successCount -> new BatchStats(successCount, 100 - successCount)), 
        5)
        .reduce(new BatchResult(), BatchResult::accumulate);
}
</details>
```

```java
<details>
<summary>Task 5: Event Stream Processing (click to reveal)</summary>
public Flux<PriceAlert> monitorPrices(Flux<PriceUpdate> updates) {
    return updates.groupBy(PriceUpdate::getSymbol)
        .flatMap(groupedFlux -> groupedFlux
            .distinctUntilChanged(PriceUpdate::getPrice)
            .window(Duration.ofSeconds(5))
            .flatMap(window -> window.collectList()
                .filter(list -> !list.isEmpty())
                .flatMapMany(list -> {
                    double avg = list.stream().mapToDouble(PriceUpdate::getPrice).average().orElse(0.0);
                    PriceUpdate latest = list.get(list.size() - 1);
                    if (Math.abs(latest.getPrice() - avg) / avg > 0.05) {
                        return Mono.just(new PriceAlert(groupedFlux.key(), latest.getPrice(), avg));
                    }
                    return Mono.empty();
                })
            )
            .sample(Duration.ofSeconds(10))
        );
}
</details>
```