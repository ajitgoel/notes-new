# Java Reactive Programming — Resources

## Knowledge

- [Project Reactor Reference Guide](https://projectreactor.io/docs/core/release/reference/)
  The canonical, authoritative reference for Reactor Core. Use for: operator semantics, schedulers, context, backpressure. Trust this above all else.

- [Spring WebFlux Docs](https://docs.spring.io/spring-framework/docs/current/reference/html/web-reactive.html)
  Official Spring docs for WebFlux, functional endpoints, and router functions. Use for: integrating Reactor with Spring MVC-style controllers.

- [Reactor 3 Reference — "Which operator do I need?"](https://projectreactor.io/docs/core/release/reference/#which-operator)
  Decision tree for operator selection. Use for: when you know the effect you want but not the operator name.

- [Spring Cloud Azure — Service Bus](https://microsoft.github.io/spring-cloud-azure/current/reference/html/index.html#spring-messaging-azure-service-bus)
  Microsoft's official Spring integration for Azure Service Bus. Use for: binding reactive streams to Service Bus topics/queues.

- [Reactor Marble Diagrams](https://rxmarbles.com/)
  Visual interactive diagrams of stream operators. Use for: intuition-building on `merge`, `zip`, `combineLatest`, etc.

- [R2DBC Spec & Spring Data R2DBC](https://r2dbc.io/)
  Reactive relational DB connectivity. Use for: replacing JDBC with non-blocking SQL access.

- [Reactive Streams Specification (JVM)](https://github.com/reactive-streams/reactive-streams-jvm)
  The low-level spec that underpins Reactor, RxJava, and Java 9 Flow. Use for: understanding backpressure contracts.

## Wisdom (Communities)

- [r/java](https://reddit.com/r/java) — for general Java ecosystem discussion; occasional reactive threads
- [Spring Community Forum](https://community.spring.io/) — official Spring Q&A; good for WebFlux edge cases
- [Stack Overflow — `project-reactor` tag](https://stackoverflow.com/questions/tagged/project-reactor) — highest signal for operator-level questions
