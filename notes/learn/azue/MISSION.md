# Mission: Azure Service Bus

## Why
Building a system that requires reliable asynchronous messaging in Azure. Need to make informed architectural decisions about whether Service Bus fits the use case and how to use it effectively — not just understand it abstractly.

## Success looks like
- Confidently choose between Service Bus queues vs. topics for a given scenario
- Select the right tier (Standard vs. Premium) with clear rationale
- Implement producers and consumers using the modern Azure SDK
- Design for reliability: understand locks, settlement, dead-lettering, and retry strategies
- Compare Service Bus to other options (Event Hubs, Storage Queues) and justify the choice

## Constraints
- Experienced with messaging concepts (RabbitMQ, Kafka, SQS) — no need to teach fundamentals
- Prefers mixed learning: concept first, then hands-on practice
- Working with Java ecosystem: Spring Boot + Reactive (WebFlux/Reactor)

## Out of scope
- Event Hubs (except for comparison purposes)
- Event Grid (except for comparison purposes)
- Azure Functions bindings (can be a later topic if needed)
- JMS/Java-specific Service Bus features (unless needed later)
