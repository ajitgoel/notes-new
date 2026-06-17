# Mission: Java Reactive Programming

## Why
Ajit is building Empower PMS — a microservices-based pharmacy management system on Spring Boot, Azure Service Bus, and Project Reactor. Non-blocking, reactive code is a first-class architectural requirement: high-throughput prescription processing, event-driven workflows, and resilient external API calls all depend on it. This isn't learning for its own sake; it's the engine that will run the PMS.

## Success looks like
- Confidently write `Mono<T>` and `Flux<T>` pipelines from scratch, without consulting docs
- Replace `@Async` / `CompletableFuture` patterns in Spring Boot services with reactive equivalents
- Chain operators (`map`, `flatMap`, `filter`, `zip`, `merge`, `switchIfEmpty`) correctly
- Handle errors with `onErrorReturn`, `onErrorResume`, and `retry` in production scenarios
- Integrate reactive streams with Spring WebFlux controllers and `R2DBC` or SQL Server
- Wire reactive streams to Azure Service Bus via Spring Cloud Azure
- Write backpressure-aware code that won't OOM under pharmacy peak load

## Constraints
- ~30 years Java experience — skip Java basics entirely; go straight to reactive concepts
- Prefers hands-on, buildable code over documentation-heavy explanations
- Working in Spring Boot + Project Reactor (Reactor Core) — not RxJava

## Out of scope
- RxJava / RxKotlin
- Android reactive patterns
- Reactor Netty internals
- Kotlin coroutines
