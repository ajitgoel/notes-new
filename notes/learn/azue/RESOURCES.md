# Azure Service Bus Resources

## Knowledge

- [Azure Service Bus Overview — Microsoft Learn](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview)
  Official overview of Service Bus concepts, features, and architecture. Use for: understanding queues, topics, namespaces, and advanced features.

- [Queues, Topics, and Subscriptions — Microsoft Learn](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-queues-topics-subscriptions)
  Deep dive into messaging entities, filters, and actions. Use for: choosing between queue vs. topic patterns, subscription filtering.

- [Compare Azure Messaging Services — Microsoft Learn](https://learn.microsoft.com/en-us/azure/service-bus-messaging/compare-messaging-services)
  Decision framework for Service Bus vs. Event Hubs vs. Event Grid. Use for: justifying architectural choices.

- [Premium Messaging Tier — Microsoft Learn](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-premium-messaging)
  Premium vs. Standard differences, messaging units, large message support. Use for: tier selection, capacity planning.

- [Message Transfers, Locks, and Settlement — Microsoft Learn](https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement)
  The lifecycle of a message: send settlement, receive modes (PeekLock, ReceiveAndDelete), Complete/Abandon/DeadLetter. Use for: reliability design, understanding exactly-once semantics.

- [Best Practices for Performance — Microsoft Learn](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-performance-improvements)
  Throughput optimization, prefetch, concurrent operations, protocol selection. Use for: performance tuning, capacity planning.

- [Azure Service Bus SDK for Java — Microsoft Learn](https://learn.microsoft.com/en-us/java/api/overview/azure/service-bus?view=azure-java-stable)
  Modern Java SDK overview with Maven coordinates. Use for: adding `azure-messaging-servicebus` dependency, finding samples.

- [Service Bus Java SDK API Reference (azure-messaging-servicebus)](https://azuresdkdocs.z19.web.core.windows.net/java/azure-messaging-servicebus/latest/index.html)
  Full Javadoc for the reactive SDK. Use for: ServiceBusSenderAsyncClient, ServiceBusReceiverAsyncClient, ServiceBusProcessorClient API details.

- [Spring Cloud Azure Service Bus JMS Starter — Microsoft Learn](https://learn.microsoft.com/en-us/azure/developer/java/spring-framework/configure-spring-boot-starter-java-app-with-azure-service-bus)
  Spring Boot + JMS integration with Service Bus. Use for: JmsTemplate/@JmsListener approach (blocking). Note: for reactive, prefer the native SDK.

- [Service Bus Java Samples — GitHub](https://github.com/Azure/azure-sdk-for-java/tree/main/sdk/servicebus/azure-messaging-servicebus/src/samples)
  Official Java SDK samples covering async/reactive send, receive, processor, sessions, and administration.

- [Spring Cloud Azure Version Mapping](https://github.com/Azure/azure-sdk-for-java/wiki/Spring-Versions-Mapping)
  Which version of Spring Cloud Azure to use with your Spring Boot version. Use for: dependency alignment.

- [Premium Messaging: How Fast Is It? — Microsoft Tech Community](https://techcommunity.microsoft.com/t5/Service-Bus-blog/Premium-Messaging-How-fast-is-it/ba-p/370722)
  Real benchmark results: ~4 MB/s per Messaging Unit. Use for: capacity calculations.

## Wisdom (Communities)

- [Microsoft Q&A — Azure Service Bus tag](https://learn.microsoft.com/answers/tags/73/azure-service-bus/)
  Official Microsoft Q&A, actively answered by the Service Bus product team. Use for: troubleshooting specific issues, getting authoritative answers.

- [Stack Overflow — azure-service-bus tag](https://stackoverflow.com/questions/tagged/azure-service-bus)
  Broad community with practical solutions. Use for: implementation questions, edge cases, patterns.

- [Azure Feedback — Service Bus](https://feedback.azure.com/d365community/forum/7c0a897d-2125-ec11-b6e6-000d3a4f0f84)
  Official feature request and bug report channel. Use for: understanding roadmap, voting on features.

- [NServiceBus](https://docs.particular.net/transports/azure-service-bus/) and [MassTransit](https://masstransit.io/documentation/transports/azure-service-bus)
  Higher-level .NET abstractions over Service Bus. Use for: production-grade patterns like sagas, retries, and routing — if you outgrow the raw SDK (Java equivalents would be Spring Integration or Apache Camel with AMQP).
