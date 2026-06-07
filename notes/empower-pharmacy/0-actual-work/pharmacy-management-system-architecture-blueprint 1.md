![[Pasted image 20260606201403.png]]


**PROJECT ARCHITECTURE BLUEPRINT**

**Empower Pharmacy — PMS Core Platform**

Microservices + GraphQL Federation Architecture

Java Spring Boot · Azure Service Bus · SQL Server · Spring for GraphQL

|  |  |
| --- | --- |
| **Version** | 1.1 |
| **Date** | June 2026 |
| **Scope** | PMS Core (Pharmacy, Order, Inventory, Fulfillment, Catalog, Pricing, Account, Compliance) |
| **GraphQL layer** | Spring for GraphQL + Apollo Federation subgraph contracts |
| **Classification** | Internal Architecture Reference |
| **Status** | Active |

# **1. Scope & Goals**

This blueprint focuses exclusively on the Empower Pharmacy Management System (PMS) core platform. External marketplace, CRM, and ERP concerns are referenced only at the integration boundary.

### **What is in scope**

* All 7 PMS microservices: Pharmacy, Inventory, Order, Fulfillment, Catalog, Pricing, Account
* The Compliance Service (503A / 503B / USP 795/797/800)
* GraphQL Federation Gateway — the single API surface for all frontend and BFF consumers
* Azure Service Bus event backbone — topics, subscriptions, saga patterns
* Per-service SQL Server databases and shared Azure infrastructure
* Security: Azure AD B2C, Spring Security, Key Vault, HIPAA/DEA compliance controls

### **What is out of scope**

* Marketplace portal backend (separate bounded context)
* ERP / Salesforce CRM internals (referenced at ACL boundary only)
* MDM platform internals (consumed as read-only API)
* Patient-facing mobile app (consumes this platform's GraphQL API)

# **2. Architecture Overview**

The PMS platform uses a layered architecture with five distinct tiers. The GraphQL Federation Gateway is the key addition — it absorbs all orchestration logic that would otherwise leak into the frontend, while each microservice remains focused on its domain.

|  |  |
| --- | --- |
| **Tier** | **Description** |
| **Tier 1 — Clients** | Provider portal, internal ops UI, mobile app, external integration API. All clients speak only to the GraphQL gateway — no direct service calls. |
| **Tier 2 — APIM** | Azure API Management: JWT validation (Azure AD B2C), TLS termination, rate limiting, versioning, IP allowlisting. Forwards validated requests to the GraphQL gateway. |
| **Tier 3 — GraphQL Federation Gateway** | Spring for GraphQL running Apollo Federation. Composes subgraph schemas from all services, plans queries, routes mutations, and serves real-time subscriptions via WebSocket. No business logic here — only orchestration. |
| **Tier 4 — Domain Microservices** | Seven bounded-context Spring Boot services. Each exposes a GraphQL subgraph (SDL + resolvers) and publishes/consumes domain events on Azure Service Bus. No service-to-service HTTP calls — all cross-domain reads go through the gateway or events. |
| **Tier 5 — Data & Events** | Per-service Azure SQL databases (no shared DB), Azure Service Bus Premium for events, Azure Redis for caching, Azure Blob for documents and labels. |

# **3. GraphQL Federation Gateway**

The GraphQL Federation Gateway is the single entry point for all frontend queries, mutations, and subscriptions. It replaces the REST orchestration layer entirely — every frontend view is driven by a composed GraphQL query rather than a sequence of REST calls.

## **3.1 Technology Choice**

|  |  |
| --- | --- |
| **Concern** | **Decision** |
| **Runtime** | Spring for GraphQL (spring-graphql 1.3.x) — native Spring Boot integration, no separate Node process |
| **Federation standard** | Apollo Federation v2 subgraph contracts — each microservice owns its SDL schema and resolvers |
| **Schema composition** | Apollo Router (Rust binary, sidecar) OR Spring Cloud Gateway with custom schema registry — evaluated at implementation |
| **Subscriptions** | WebSocket transport (graphql-ws protocol); Azure Service Bus events → GraphQL subscriptions via Reactor (Project Reactor + Spring WebFlux) |
| **DataLoader** | org.dataloader:java-dataloader — automatic per-request batching to prevent N+1 queries across subgraphs |
| **Introspection** | Disabled in production; schema registry serves SDL for developer tooling |
| **Persisted queries** | Apollo Persisted Queries (APQ) — clients send query hash, gateway resolves from registry; reduces payload size and enables whitelisting |

## **3.2 What the Gateway Does (and Does Not Do)**

### **Does**

* Compose subgraph SDL schemas into a unified supergraph schema at startup
* Plan query execution across subgraphs (fetch Plan → parallel subgraph calls)
* Batch entity lookups (DataLoader — e.g. resolve Order.product for 50 orders in 1 call to Catalog)
* Authenticate: validate JWT, inject user context (userId, roles, tenantId) into GraphQL context
* Route mutations to the correct owning subgraph
* Push real-time events to subscribed clients via WebSocket (prescription status, batch progress, shipment tracking)
* Rate-limit per-operation complexity using graphql-java cost analysis
* Log structured query traces (operation name, variables, latency, errors) to App Insights

### **Does not**

* Contain business logic (no if/else on domain objects, no transformation of data)
* Own a database
* Publish or consume Service Bus events (that is the microservices' job)
* Call services directly via HTTP REST (all downstream calls use the subgraph protocol)

## **3.3 Subgraph Contract per Service**

Each microservice exposes its own GraphQL subgraph. The gateway federates them into a unified schema. The key pattern is entity extension — the Order subgraph can extend the Product type owned by the Catalog subgraph, adding order-specific fields without Catalog knowing about Order.

|  |  |  |
| --- | --- | --- |
| **Service** | **SDL sample (abbreviated)** | **Owns** |
| **Catalog subgraph** | type Product @key(fields: "sku") type Query { product(sku: ID!): Product products(filter: ProductFilter): [Product] } | SKU, name, attributes, channel eligibility, ATP check |
| **Pricing subgraph** | type ResolvedPrice @key(fields: "id") extend type Product @key(fields: "sku") { price(customerId: ID!): ResolvedPrice } | Price resolution (contract → tier → base) |
| **Pharmacy subgraph** | type Prescription @key(fields: "id") type Formula @key(fields: "id") type Batch @key(fields: "batchId") | Rx intake, formula, batch, dispense, lot chain |
| **Order subgraph** | type Order @key(fields: "orderId") type Mutation { placeOrder(...): Order cancelOrder(orderId:ID!): Order } | Order lifecycle, saga state, compliance gate |
| **Inventory subgraph** | extend type Product @key(fields: "sku") { stockLevel: StockLevel atp: Int } | Stock, ATP, reservations, lot records |
| **Fulfillment subgraph** | extend type Order @key(fields: "orderId") { shipment: Shipment } type Subscription { shipmentTracking(orderId:ID!): TrackingEvent } | Shipment, label, carrier, tracking subscription |
| **Account subgraph** | type Provider @key(fields: "providerId") type CustomerAccount @key(fields: "accountId") | Provider profiles, practices, access tokens |
| **Compliance subgraph** | extend type Batch @key(fields: "batchId") { eBR: ElectronicBatchRecord compliance: ComplianceStatus } | eBR, deviation, CAPA, e-Sign, FDA 503A/503B |

## **3.4 Example: Prescription Detail View**

A frontend page showing full prescription detail — patient, Rx, batch, lot chain, compliance status, and shipment — previously required 6+ REST calls. With GraphQL federation, it is a single query:

query PrescriptionDetail($rxId: ID!, $customerId: ID!) {

prescription(id: $rxId) { # → Pharmacy subgraph

id status deaSchedule

formula { # → Pharmacy subgraph

name uspClass version

}

batch { # → Pharmacy subgraph

batchId lotNumber manufacturedQty

compliance { # → Compliance subgraph (entity extension)

eBRStatus lastAuditedAt

deviations { severity resolvedAt }

}

stockLevel { # → Inventory subgraph (entity extension)

onHand reserved

}

}

order { # → Order subgraph (entity extension)

orderId status placedAt

product { # → Catalog subgraph (entity extension)

sku name

price(customerId: $customerId) { # → Pricing subgraph

resolvedPrice contractRef

}

}

shipment { # → Fulfillment subgraph

trackingNumber carrier status

}

}

}

}

The gateway plans this as parallel subgraph fetches where possible (batch + compliance + stockLevel in parallel; order + shipment in parallel after batch resolves), with DataLoader coalescing multiple batch lookups into a single resolver call.

## **3.5 Mutation Routing**

Mutations always route to the single owning subgraph. The gateway never fans a mutation across multiple services — side effects are handled by the service emitting a domain event, which other services consume asynchronously.

|  |  |
| --- | --- |
| **Mutation** | **Owning subgraph & side effect** |
| **placeOrder(input: OrderInput!)** | Order subgraph → emits OrderPlaced event → saga begins on Service Bus |
| **intakePrescription(input: RxInput!)** | Pharmacy subgraph → validates, persists, emits PrescriptionIntaked |
| **startBatch(formulaId: ID!, qty: Int!)** | Pharmacy subgraph → emits BatchManufacturingStarted |
| **dispenseFromBatch(batchId: ID!, rxId: ID!)** | Pharmacy subgraph → updates lot, emits PrescriptionDispensed |
| **createShipment(orderId: ID!)** | Fulfillment subgraph → reserves carrier slot, emits ShipmentCreated |
| **signElectronically(recordId: ID!, signatureType: SignatureType!)** | Compliance subgraph → dual-sign gate for Schedule II |
| **cancelOrder(orderId: ID!)** | Order subgraph → emits OrderCancelled → compensating events on bus |

## **3.6 Subscriptions (Real-Time)**

The gateway bridges Azure Service Bus topics to GraphQL subscriptions via Project Reactor. When a client subscribes to shipmentTracking or batchProgress, the gateway subscribes to the relevant Service Bus topic and pushes events over WebSocket as they arrive.

|  |  |
| --- | --- |
| **Subscription** | **Service Bus bridge** |
| **subscription { shipmentTracking(orderId) }** | Consumes fulfillment-events topic → filters by orderId → pushes TrackingEvent |
| **subscription { batchProgress(batchId) }** | Consumes pharmacy-events topic → filters by batchId → pushes BatchStatusUpdate |
| **subscription { prescriptionStatus(rxId) }** | Consumes pharmacy-events → filters by rxId → pushes PrescriptionStatusUpdate |
| **subscription { complianceAlert }** | Consumes pharmacy-compliance-alerts → pushes to ops subscribers only (role gate) |

# **4. PMS Microservice Catalogue**

Each service is a Spring Boot application deployed as an Azure Container App. It owns a dedicated SQL database, publishes/consumes Service Bus events, and exposes a GraphQL subgraph consumed by the federation gateway.

## **4.1 Pharmacy Management Service**

|  |  |
| --- | --- |
| **Property** | **Detail** |
| **Module** | pharmacy-service |
| **GraphQL subgraph** | Prescription, Formula, Batch, BatchLotChain, DispensedItem |
| **Owns (SQL)** | db-pharmacy: Prescriptions, Formulas, FormulaBOM, Batches, BatchLotChain, DispensedItems, ComplianceRecords |
| **Publishes** | PrescriptionIntaked, PrescriptionValidated, BatchManufacturingStarted, BatchReady, PrescriptionDispensed, ComplianceCheckFailed |
| **Consumes** | InventoryReserved (saga ACK), OrderCancelled (release batch) |
| **Key constraint** | DEA Schedule II: dual pharmacist e-Sign mandatory before dispatch; enforced in PrescriptionStateMachine |
| **Lot traceability** | LotEvents table append-only; no UPDATE or DELETE; HIPAA-compliant immutable audit trail |
| **USP compliance** | 795 (non-sterile), 797 (sterile env monitoring), 800 (hazardous drug flag + ventilation log) |

## **4.2 Order Management Service**

|  |  |
| --- | --- |
| **Property** | **Detail** |
| **Module** | order-service |
| **GraphQL subgraph** | Order, OrderLineItem, OrderSagaState (internal only — not exposed to frontend) |
| **Owns (SQL)** | db-orders: Orders, OrderLineItems, OrderSagaState, OrderAudit |
| **Publishes** | OrderPlaced, OrderConfirmed, OrderCancelled |
| **Consumes** | InventoryReserved, PriceLocked, PrescriptionValidated (saga ACKs); ReservationFailed (compensate) |
| **Saga pattern** | Choreography via Service Bus. OrderSagaState table tracks saga progress. Compensating events on any NACK. |
| **Compliance gate** | Order.validateCompliance() must pass before OrderConfirmed emitted — calls Pharmacy subgraph resolver |

## **4.3 Inventory Management Service**

|  |  |
| --- | --- |
| **Property** | **Detail** |
| **Module** | inventory-service |
| **GraphQL subgraph** | StockLevel, LotRecord, Reservation — exposed as extensions on Product and Batch |
| **Owns (SQL)** | db-inventory: StockLevels, LotRecords, Reservations, StockMovements |
| **Publishes** | InventoryReserved, ReservationReleased, LowStockAlert, StockUpdated |
| **Consumes** | BatchReady (receive new batch), OrderCancelled (release reserve), ShipmentDispatched (deduct committed) |
| **ATP calculation** | ATP = OnHand - Reserved - InTransit; computed at query time, cached 30s in Redis |
| **Lot tracking** | Full chain: raw material lot → batch → dispensed item; queryable via getLotChain(lotNumber) resolver |

## **4.4 Fulfillment Service**

|  |  |
| --- | --- |
| **Property** | **Detail** |
| **Module** | fulfillment-service |
| **GraphQL subgraph** | Shipment, ShipmentItem, TrackingEvent — extended on Order |
| **Owns (SQL)** | db-fulfillment: Shipments, ShipmentItems, BatchShipmentMap, CarrierTransactions |
| **Publishes** | ShipmentCreated, ShipmentDispatched |
| **Consumes** | OrderConfirmed (trigger shipment creation), StockUpdated (batch-to-shipment allocation) |
| **Carrier integration** | Anti-Corruption Layer: CarrierPort interface → FedExAdapter, UPSAdapter, USPSAdapter |
| **Label generation** | ZPL (thermal) or PDF labels stored in Azure Blob; URL returned via Shipment.labelUrl resolver |
| **Tracking subscription** | Fulfillment service pushes carrier polling results to fulfillment-events; gateway bridges to GraphQL subscription |

## **4.5 Catalog Service**

|  |  |
| --- | --- |
| **Property** | **Detail** |
| **Module** | catalog-service |
| **GraphQL subgraph** | Product @key(sku) — the root entity extended by Pricing and Inventory subgraphs |
| **Owns (SQL)** | db-catalog: Products, ProductAttributes, ProductChannels |
| **Publishes** | ProductCreated, ProductUpdated, ProductDiscontinued |
| **Consumes** | Nothing — Catalog is a pure publisher in the event topology |
| **Cache** | Product detail cached in Redis (TTL 10 min); invalidated on ProductUpdated event |

## **4.6 Pricing Service**

|  |  |
| --- | --- |
| **Property** | **Detail** |
| **Module** | pricing-service |
| **GraphQL subgraph** | ResolvedPrice, PricingContract — extended on Product (price field) |
| **Owns (SQL)** | db-pricing: PricingContracts, PricingRules, PricingTiers, ContractLineItems |
| **Publishes** | PricingContractUpdated, PriceLocked |
| **Consumes** | ProductDiscontinued (mark contract lines inactive) |
| **Resolution chain** | Contract-specific → customer tier → channel-specific → volume → base MSRP (Strategy pattern, priority ordered) |
| **Cache** | Resolved price cached in Redis (TTL 15 min); key: price:{customerId}:{sku}:{channel}; invalidated on PricingContractUpdated |

## **4.7 Account Management Service**

|  |  |
| --- | --- |
| **Property** | **Detail** |
| **Module** | account-service |
| **GraphQL subgraph** | Provider @key(providerId), CustomerAccount @key(accountId) |
| **Owns (SQL)** | db-accounts: Providers, ProviderAddresses, CustomerAccounts, AccessTokens |
| **Publishes** | ProviderOnboarded, CustomerAccountCreated |
| **Consumes** | Nothing — downstream services consume account events |
| **Auth integration** | Issues marketplace access tokens backed by Azure AD B2C identity; token stored in AccessTokens table with scope and expiry |

## **4.8 Compliance Service**

|  |  |
| --- | --- |
| **Property** | **Detail** |
| **Module** | compliance-service |
| **GraphQL subgraph** | ElectronicBatchRecord, Deviation, CAPA, ComplianceStatus — extended on Batch |
| **Owns (SQL)** | db-compliance: ElectronicBatchRecords, Deviations, CAPA, ESignatures, AuditEvents |
| **Publishes** | ComplianceCheckPassed, ComplianceCheckFailed, DeviationLogged, CapaResolved |
| **Consumes** | BatchManufacturingStarted (auto-create eBR), BatchReady (trigger QC check) |
| **e-Sign** | Azure AD identity + PKCS#12 cert from Key Vault; signature hash stored with record; non-repudiable; dual-sign enforced for Schedule II |
| **FDA 503B** | Annual report generation endpoint; eBR linked to batch; environmental monitoring records mandatory |

# **5. Event-Driven Architecture — Azure Service Bus**

All inter-service communication in the PMS platform flows through Azure Service Bus topics. No microservice calls another microservice's HTTP API directly — the only cross-service HTTP traffic is the GraphQL federation protocol between the gateway and each service's subgraph endpoint.

## **5.1 Topic / Subscription Matrix**

|  |  |  |  |
| --- | --- | --- | --- |
| **Topic** | **Publisher** | **Subscribers** | **Key events** |
| **catalog-events** | Catalog | Pricing, Inventory | ProductCreated, ProductUpdated, ProductDiscontinued |
| **pricing-events** | Pricing | Order (price lock), Redis invalidation | PricingContractUpdated, PriceLocked |
| **pharmacy-events** | Pharmacy | Inventory, Order, Compliance, GW subscriptions | BatchReady, PrescriptionDispensed, BatchManufacturingStarted |
| **order-events** | Order | Fulfillment, ERP connector | OrderConfirmed, OrderCancelled |
| **inventory-events** | Inventory | Order saga, ERP connector | InventoryReserved, LowStockAlert, ReservationReleased |
| **fulfillment-events** | Fulfillment | Order, GW subscriptions | ShipmentCreated, ShipmentDispatched |
| **account-events** | Account | Downstream services | ProviderOnboarded, CustomerAccountCreated |
| **pharmacy-compliance-alerts** | Pharmacy + Compliance | Compliance, Ops (PagerDuty), GW subscriptions | ComplianceCheckFailed, DeviationLogged |

## **5.2 Order Saga (Choreography)**

The order confirmation saga uses choreography — no central orchestrator. Each participating service listens to the relevant topic, processes its step, and emits an ACK or NACK event. The Order Service tracks saga state in its OrderSagaState table.

|  |  |
| --- | --- |
| **Saga step** | **Action** |
| **Step 1** | Order Service emits OrderPlaced → order-events topic |
| **Step 2** | Inventory Service receives OrderPlaced, reserves stock → emits InventoryReserved or ReservationFailed |
| **Step 3** | Pricing Service receives OrderPlaced, locks price → emits PriceLocked or PriceChanged |
| **Step 4** | Pharmacy Service receives OrderPlaced, validates Rx → emits PrescriptionValidated or ValidationFailed |
| **Step 5a — all ACKs** | Order Service receives all 3 ACKs → emits OrderConfirmed |
| **Step 5b — any NACK** | Order Service receives any NACK → emits OrderFailed → compensating: release reserve, void price lock |
| **Timeout** | OrderSagaState expires after 30 seconds with no full ACK; treated as NACK; Azure Monitor alert fires |

## **5.3 Consumer Patterns**

|  |  |
| --- | --- |
| **Pattern** | **Implementation** |
| **Idempotent consumers** | Every consumer checks Redis idempotency store before processing. Key: {topic}:{messageId}. Prevents duplicate processing on redelivery. |
| **Dead-Letter Queue (DLQ)** | Messages failing after 10 delivery attempts → DLQ. Azure Monitor alert fires when DLQ depth > 0. On-call engineer investigates. |
| **Sessions (ordered delivery)** | Azure Service Bus Sessions used for prescription workflow — guarantees per-prescription message ordering within a partition. |
| **At-least-once delivery** | All consumers idempotent by design. Exactly-once is not assumed — deduplication handles the rest. |
| **Outbox pattern (Communication)** | Communication Service uses transactional outbox: event written to db-communications.OutboxMessages in same DB transaction as domain change; background worker publishes to Service Bus. |

# **6. Spring for GraphQL — Implementation Guide**

## **6.1 Service Template Structure**

Each microservice follows the same module structure. The GraphQL subgraph is a first-class concern alongside REST (used only for health checks and internal admin endpoints).

src/main/

├── java/com/empower/{service}/

│ ├── graphql/

│ │ ├── {Entity}Controller.java // @Controller @SchemaMapping @QueryMapping

│ │ ├── {Entity}DataLoader.java // BatchLoaderRegistry

│ │ └── {Entity}Subscription.java // @SubscriptionMapping (gateway only)

│ ├── domain/

│ │ ├── {Entity}.java // JPA entity

│ │ ├── {Entity}Service.java // domain logic

│ │ └── {Entity}Repository.java // Spring Data JPA

│ ├── events/

│ │ ├── {Event}Publisher.java // ServiceBusSenderClient

│ │ └── {Event}Consumer.java // @ServiceBusMessageListener

│ └── config/

│ ├── SecurityConfig.java // OAuth2 resource server

│ └── ServiceBusConfig.java

└── resources/

├── graphql/{service}.graphqls // SDL schema file

└── application.yml

## **6.2 Key Dependencies (pom.xml)**

<!-- GraphQL -->

<dependency>

<groupId>org.springframework.boot</groupId>

<artifactId>spring-boot-starter-graphql</artifactId>

</dependency>

<!-- WebSocket transport for subscriptions (gateway only) -->

<dependency>

<groupId>org.springframework.boot</groupId>

<artifactId>spring-boot-starter-websocket</artifactId>

</dependency>

<!-- DataLoader for N+1 prevention -->

<dependency>

<groupId>org.dataloader</groupId>

<artifactId>java-dataloader</artifactId>

<version>3.3.0</version>

</dependency>

<!-- Azure Service Bus -->

<dependency>

<groupId>com.azure.spring</groupId>

<artifactId>spring-cloud-azure-starter-servicebus</artifactId>

</dependency>

<!-- Azure Key Vault -->

<dependency>

<groupId>com.azure.spring</groupId>

<artifactId>spring-cloud-azure-starter-keyvault-secrets</artifactId>

</dependency>

<!-- Resilience4j -->

<dependency>

<groupId>io.github.resilience4j</groupId>

<artifactId>resilience4j-spring-boot3</artifactId>

</dependency>

## **6.3 Subgraph Controller Pattern**

@Controller

public class PrescriptionController {

@QueryMapping

public Prescription prescription(@Argument String id, DataFetchingEnvironment env) {

String userId = env.getGraphQlContext().get("userId"); // from APIM JWT

return prescriptionService.findById(id, userId);

}

@SchemaMapping(typeName = "Batch", field = "lotChain")

public List<LotEvent> lotChain(Batch batch) {

return lotEventRepository.findByLotNumber(batch.getLotNumber());

}

@MutationMapping

public Prescription intakePrescription(@Argument RxInput input) {

return prescriptionService.intake(input); // emits PrescriptionIntaked event

}

@BatchMapping // DataLoader — resolves N formulaIds in 1 DB query

public Map<String, Formula> formula(List<Prescription> prescriptions) {

List<String> ids = prescriptions.stream().map(Prescription::getFormulaId).toList();

return formulaRepository.findAllById(ids).stream()

.collect(Collectors.toMap(Formula::getId, f -> f));

}

}

## **6.4 Subscription Bridge (Gateway ↔ Service Bus)**

@Controller

public class ShipmentSubscriptionController {

@Autowired ServiceBusProcessorClient fulfillmentEventsClient;

@SubscriptionMapping

public Flux<TrackingEvent> shipmentTracking(@Argument String orderId) {

return Flux.<TrackingEvent>create(sink -> {

fulfillmentEventsClient.start();

// register sink — messages from Service Bus → emit to Flux

eventBridge.subscribe(orderId, sink::next, sink::error, sink::complete);

})

.filter(e -> e.getOrderId().equals(orderId))

.doFinally(signal -> eventBridge.unsubscribe(orderId));

}

}

# **7. Data Architecture**

## **7.1 Database-per-Service**

Each service owns exactly one Azure SQL database. No cross-database JOINs. Cross-domain data resolved via GraphQL federation (entity extensions) or event-driven projections.

|  |  |  |
| --- | --- | --- |
| **Database** | **Core tables** | **Owner** |
| **db-pharmacy** | Prescriptions, Formulas, FormulaBOM, Batches, BatchLotChain, DispensedItems, ComplianceRecords, LotEvents (append-only) | Pharmacy service |
| **db-orders** | Orders, OrderLineItems, OrderSagaState, OrderAudit | Order service |
| **db-inventory** | StockLevels, LotRecords, Reservations, StockMovements | Inventory service |
| **db-fulfillment** | Shipments, ShipmentItems, BatchShipmentMap, CarrierTransactions | Fulfillment service |
| **db-catalog** | Products, ProductAttributes, ProductChannels | Catalog service |
| **db-pricing** | PricingContracts, PricingRules, PricingTiers, ContractLineItems | Pricing service |
| **db-accounts** | Providers, ProviderAddresses, CustomerAccounts, AccessTokens | Account service |
| **db-compliance** | ElectronicBatchRecords, Deviations, CAPA, ESignatures, ComplianceAuditEvents | Compliance service |

## **7.2 Append-Only Lot Event Log**

The LotEvents table in db-pharmacy is immutable. No UPDATE or DELETE statements are permitted (enforced by row-level security in Azure SQL). This provides the FDA-required immutable dispensing audit trail.

|  |  |
| --- | --- |
| **Column** | **Definition** |
| **EventId** | BIGINT IDENTITY PK — sequential |
| **LotNumber** | NVARCHAR(50) — links raw material, batch, and dispensed item |
| **EventType** | NVARCHAR(50) — RECEIVED | SAMPLED | RELEASED | DISPENSED | RECALLED |
| **Payload** | NVARCHAR(MAX) — JSON; event-specific data |
| **PerformedBy** | NVARCHAR(100) — Azure AD object ID |
| **Timestamp** | DATETIME2 DEFAULT SYSUTCDATETIME() — UTC immutable |
| **TraceId** | NVARCHAR(64) — OpenTelemetry trace context ID |

## **7.3 Caching Strategy**

|  |  |  |
| --- | --- | --- |
| **Data** | **Cache policy** | **Invalidation** |
| **Product detail (Catalog)** | Redis, TTL 10 min | Invalidated by ProductUpdated event |
| **Resolved price (Pricing)** | Redis, TTL 15 min; key: price:{customerId}:{sku}:{channel} | Invalidated by PricingContractUpdated event |
| **ATP quantity (Inventory)** | Redis, TTL 30 sec | Short TTL — stock changes frequently; never cache stale ATP > 30s |
| **Provider profile (Account)** | Redis, TTL 60 min | Invalidated on ProviderUpdated event |
| **GraphQL persisted queries** | Redis, no expiry | Query hash → SDL string; updated on schema deploy |

# **8. Security & Compliance**

## **8.1 Authentication & Authorisation Flow**

All traffic enters through APIM which validates JWTs against Azure AD B2C. APIM injects X-User-Id and X-Roles headers before forwarding to the GraphQL gateway. The gateway propagates these as GraphQL context, making them available in every resolver.

|  |  |
| --- | --- |
| **Component** | **Auth responsibility** |
| **APIM** | Validates JWT signature and expiry against Azure AD B2C JWKS endpoint; rejects with 401 on failure |
| **GraphQL Gateway** | Extracts X-User-Id and X-Roles from APIM headers; builds GraphQL execution context; role checked per operation using @PreAuthorize on mutation methods |
| **Microservice subgraphs** | Trust the gateway (mTLS between gateway and subgraph endpoints); validate role claims from context for sensitive resolvers |
| **Roles** | PHARMACIST · OPS · ADMIN · PROVIDER · INTEGRATION\_SERVICE |
| **Schedule II gate** | @PreAuthorize("hasRole('PHARMACIST') and @dualSignService.hasSecondSignature(prescriptionId)") on dispenseFromBatch mutation |

## **8.2 HIPAA Controls**

|  |  |
| --- | --- |
| **Control** | **Implementation** |
| **Encryption at rest** | Azure SQL TDE (customer-managed keys for PHI databases in Key Vault); Blob Storage encryption |
| **Encryption in transit** | TLS 1.3 at APIM and ACA ingress; mTLS between gateway and subgraph services; Service Bus AMQP over TLS |
| **PHI access logging** | All resolvers accessing Prescriptions/Patients emit structured audit event via @Auditable AOP aspect |
| **Field masking** | Patient DOB masked for LOGISTICS role via GraphQL field-level @Auth directive |
| **Data retention** | Audit logs: 90 days hot (Log Analytics), 2 years cold (Blob Archive); LotEvents: 7 years (FDA requirement) |

## **8.3 DEA Compliance**

|  |  |
| --- | --- |
| **Requirement** | **Implementation** |
| **Schedule II dual e-Sign** | Two distinct PHARMACIST-role principals must call signElectronically mutation before dispenseFromBatch is permitted |
| **ControlledSubstanceLedger** | Append-only table: PrescriptionId, DEASchedule, PatientId, PrescriberDEA, DispensedQty, Pharmacist1Id, Pharmacist2Id, Timestamp |
| **Prescriber validation** | NPI checked against CMS NPI Registry at prescription intake; DEA number validated against DEA database connector |
| **No electronic refills** | Schedule II prescriptions hard-blocked from refill mutation in Pharmacy subgraph resolver |

# **9. Infrastructure & Deployment**

## **9.1 Azure Resources**

|  |  |
| --- | --- |
| **Resource** | **Configuration** |
| **Azure Container Apps (ACA)** | All 9 services (8 domain + 1 gateway) deployed as ACA apps; internal VNet; KEDA auto-scale from Service Bus queue depth |
| **Azure API Management** | Premium tier (VNet injection); WAF; custom domain api.empowerpharmacy.com; APIM policy for JWT validation |
| **Azure Service Bus** | Premium tier; 16 messaging units; geo-disaster recovery to paired region; sessions enabled for pharmacy topic |
| **Azure SQL Managed Instance** | Business Critical tier; zone-redundant; geo-replication; read replicas for Catalog and Inventory (high-read services) |
| **Azure Cache for Redis** | Enterprise tier; zone-redundant; TLS only; 6 GB for pricing, catalog, ATP, persisted queries |
| **Azure Key Vault** | One KV per environment; RBAC access model; soft-delete + purge protection; certificate storage for e-Sign |
| **Azure Blob Storage** | LRS for shipping labels/documents; GRS for compliance archives; immutable storage policy for LotEvents archive |
| **Azure Monitor + App Insights** | Distributed tracing (OpenTelemetry); structured logs (Log Analytics); dashboards for prescription throughput, batch KPIs, DLQ depth |
| **Azure DevOps Pipelines** | PR validation → Dev (auto) → Staging (manual gate) → Production (blue-green canary via ACA traffic weights) |

## **9.2 Scaling Rules**

|  |  |
| --- | --- |
| **Service** | **Scaling policy** |
| **GraphQL Gateway** | Min 2, max 20 replicas; KEDA: HTTP request rate; P95 latency target < 200ms |
| **Pharmacy Service** | Min 2, max 10; KEDA: pharmacy-events queue depth > 5 → scale out |
| **Order Service** | Min 2, max 10; KEDA: order-events queue depth |
| **Inventory Service** | Min 2, max 10; read replica SQL on GET resolvers; Redis ATP cache reduces SQL load |
| **Fulfillment Service** | Min 1, max 8; KEDA: fulfillment-events depth |
| **Catalog Service** | Min 1, max 6; scale-to-zero outside business hours; Redis absorbs most reads |
| **Compliance Service** | Min 1, max 4; scale-to-zero overnight; wake latency < 5s for ACA cold start |

# **10. Architectural Decision Records**

### **ADR-001: GraphQL Federation over REST BFF**

|  |  |
| --- | --- |
| **Decision** | Spring for GraphQL with Apollo Federation subgraph contracts as the single API surface for all frontend consumers. |
| **Rationale** | Eliminates N+1 REST call patterns in the frontend; enables flexible client-driven queries without new backend endpoints; entity extensions allow cross-domain views with no coupling between domain services; subscriptions via WebSocket replace polling for real-time views. |
| **Trade-offs** | Federation schema composition adds startup complexity; Apollo Router (Rust sidecar) or custom composition required; schema breaking changes need coordinated deploys across subgraphs. |

### **ADR-002: No direct inter-service HTTP calls**

|  |  |
| --- | --- |
| **Decision** | Microservices never call each other's REST APIs. Cross-domain data resolved via GraphQL federation (read) or Service Bus events (async side effects). |
| **Rationale** | Prevents temporal coupling; eliminates cascading failures; GraphQL gateway owns all orchestration; services remain independently deployable. |
| **Trade-offs** | Some read paths require multiple subgraph resolver hops; mitigated by DataLoader batching and Redis caching. |

### **ADR-003: Azure Service Bus over Apache Kafka (PMS scope)**

|  |  |
| --- | --- |
| **Decision** | Azure Service Bus Premium tier as the PMS event backbone. |
| **Rationale** | Managed PaaS; native Azure AD RBAC; Sessions for ordered prescription workflow; DLQ out-of-the-box; lower operational burden. Kafka would be re-evaluated only if event replay beyond 7 days or >1M msg/sec throughput becomes required. |
| **Trade-offs** | No built-in event replay (mitigation: archive to Blob Storage); 7-day max retention vs Kafka's indefinite log. |

### **ADR-004: Append-only LotEvents for lot traceability**

|  |  |
| --- | --- |
| **Decision** | LotEvents table is insert-only; row-level security blocks UPDATE and DELETE in Azure SQL. |
| **Rationale** | FDA and HIPAA require immutable dispensing records; event sourcing pattern provides complete audit trail from raw material to patient. |
| **Trade-offs** | Storage grows indefinitely; mitigated by archiving rows older than 7 years to Azure Blob cold storage. |

### **ADR-005: Database-per-Service with no shared SQL**

|  |  |
| --- | --- |
| **Decision** | Each of the 8 PMS microservices owns a dedicated Azure SQL database; no cross-database joins permitted in application code. |
| **Rationale** | Bounded-context isolation; independent schema evolution; prevents tight coupling via shared data structures. |
| **Trade-offs** | Cross-domain data requires API calls or event projections; GraphQL federation mitigates most read-side pain. |

### **ADR-006: mTLS between Gateway and Subgraph services**

|  |  |
| --- | --- |
| **Decision** | GraphQL gateway uses mutual TLS to call each subgraph endpoint over the internal VNet. |
| **Rationale** | Ensures only the gateway can call subgraph endpoints; prevents direct bypass of APIM auth; certificates managed in Key Vault. |
| **Trade-offs** | Certificate rotation requires coordination; mitigated by Azure Key Vault auto-rotation and ACA secret reload. |