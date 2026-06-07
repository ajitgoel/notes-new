### Microservices | Java Spring Boot · Azure Service Bus · SQL Server

**Generated:** June 2026  
**Version:** 1.0  
**Classification:** Internal Architecture Reference

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Architecture Principles](#2-architecture-principles)
3. [Technology Stack](#3-technology-stack)
4. [Domain Model & Bounded Contexts](#4-domain-model--bounded-contexts)
5. [Microservice Catalogue](#5-microservice-catalogue)
6. [API Gateway & Orchestration Layer](#6-api-gateway--orchestration-layer)
7. [Event-Driven Architecture (Azure Service Bus)](#7-event-driven-architecture-azure-service-bus)
8. [Data Architecture](#8-data-architecture)
9. [Cross-Cutting Concerns](#9-cross-cutting-concerns)
10. [Integration Layer](#10-integration-layer)
11. [Security & Compliance (HIPAA · FDA · DEA)](#11-security--compliance)
12. [Deployment Architecture (Azure)](#12-deployment-architecture-azure)
13. [Architectural Decision Records](#13-architectural-decision-records)
14. [Extension & Evolution Guidance](#14-extension--evolution-guidance)

---

## 1. Executive Summary

The Empower Pharmacy Management System (PMS) is a cloud-native, event-driven microservices platform built on Azure, designed to replace fragmented legacy systems with a cohesive, composable architecture. It serves as the domain backbone for pharmacy operations: compounding, dispensing, fulfillment, compliance, and marketplace integration.

### Current Pain Points Addressed

| Pain Point | Architectural Response |
|---|---|
| Fragmented systems (ordering, inventory, pharmacy, finance) | Bounded-context microservices with clear data ownership |
| Limited real-time visibility into operations | Event-driven via Azure Service Bus; real-time dashboards via Data Layer |
| Data inconsistencies across Provider, Product, Order domains | MDM (Master Data Management) as single source of truth |
| Tight coupling slowing change | API-First contracts; Anti-Corruption Layer on integrations |
| Compliance demands (HIPAA, FDA 503A/503B, DEA) | Compliance-by-Design: audit trails, e-Sign, distributed tracing |

---

## 2. Architecture Principles

| # | Principle | Implementation |
|---|---|---|
| P1 | **Event-Driven / Loose Coupling** | Domain events on Azure Service Bus; publishers unaware of consumers |
| P2 | **API-First / Composable Services** | OpenAPI 3.x contracts; Spring Boot REST; versioned endpoints |
| P3 | **System-of-Record vs System-of-Engagement** | Clear data ownership per service; no shared databases |
| P4 | **Compliance by Design** | Audit log emitted on every state change; e-Sign baked into workflow |
| P5 | **MDM-Centric** | Golden records for Provider, Formula, Product, Vendor managed centrally |
| P6 | **Batch & Lot Traceability** | End-to-end lot tracking from raw material → dispensed prescription |
| P7 | **Defense in Depth** | Auth at gateway, RBAC at service, row-level security at database |
| P8 | **Fail-Safe Defaults** | Circuit breakers (Resilience4j); dead-letter queues; idempotent consumers |

---

## 3. Technology Stack

### Core Platform

| Layer | Technology | Purpose |
|---|---|---|
| **Runtime** | Java 21 LTS + Spring Boot 3.x | Microservice chassis |
| **Build** | Maven / Gradle | Build & dependency management |
| **Messaging** | Azure Service Bus (Standard/Premium) | Async event backbone |
| **Database** | Azure SQL Server (Managed Instance) | Transactional store per service |
| **API Gateway** | Azure API Management (APIM) | Routing, throttling, versioning, auth |
| **Service Discovery** | Azure Container Apps internal DNS | Service-to-service resolution |
| **Container Runtime** | Azure Container Apps (ACA) | Serverless container orchestration |
| **CI/CD** | Azure DevOps Pipelines | Build, test, deploy |
| **Secrets** | Azure Key Vault | Credentials, certificates, connection strings |
| **Identity** | Azure AD B2C + Spring Security OAuth2 | AuthN/AuthZ |
| **Observability** | Azure Monitor + Application Insights + OpenTelemetry | Logs, metrics, traces |
| **Caching** | Azure Cache for Redis | Session, pricing, catalog hot-paths |
| **Blob Storage** | Azure Blob Storage | Documents, labels, audit attachments |
| **Reporting** | Power BI Embedded + Azure Synapse | Analytics & BI |
| **AI Platform** | Azure OpenAI + Cognitive Search (RAG) | AI-assisted workflows |

### Spring Boot Service Template (Per Microservice)

```
spring-boot-parent
├── spring-boot-starter-web           → REST endpoints
├── spring-boot-starter-data-jpa      → SQL Server via Hibernate
├── spring-boot-starter-security      → OAuth2 resource server
├── spring-boot-starter-actuator      → Health, metrics
├── spring-cloud-azure-starter-servicebus → Azure Service Bus producer/consumer
├── spring-cloud-azure-starter-keyvault  → Secret injection
├── micrometer-registry-azure-monitor    → Metrics
├── resilience4j-spring-boot3            → Circuit breaker, retry, rate limit
├── springdoc-openapi-starter-webmvc-ui  → OpenAPI docs
└── flyway-core                          → DB migrations
```

---

## 4. Domain Model & Bounded Contexts

Drawn from the Enterprise Architecture Logical View, the platform is divided into **7 bounded contexts**, each owning its data:

```
┌─────────────────────────────────────────────────────────────┐
│                    EMPOWER PHARMACY PMS                      │
│                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │   CATALOG    │  │   PRICING    │  │     PHARMACY     │  │
│  │  (Products,  │  │  (Pricing,   │  │  (Formulas,      │  │
│  │   SKUs, MDM) │  │   Rules,     │  │   Batches,       │  │
│  │              │  │   Contracts) │  │   Dispensing)    │  │
│  └──────────────┘  └──────────────┘  └──────────────────┘  │
│                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │    ORDER     │  │  INVENTORY   │  │   FULFILLMENT    │  │
│  │  (Orders,    │  │  (Stock,     │  │  (Warehouse,     │  │
│  │   Contracts, │  │   Lots,      │  │   Shipping,      │  │
│  │   Compliance)│  │   Batches)   │  │   Carriers)      │  │
│  └──────────────┘  └──────────────┘  └──────────────────┘  │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              ACCOUNT / PROVIDER MANAGEMENT            │   │
│  │         (Providers, Practices, Clinics, Auth)         │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Data Ownership Matrix

| Service                      | Owns                                               | Consumes (read-only via events/API)            |
| ---------------------------- | -------------------------------------------------- | ---------------------------------------------- |
| Catalog Service              | SKUs, Products, Attributes                         | MDM (raw materials, vendor)                    |
| Pricing Service              | Pricing rules, contracts, tiers                    | Catalog (SKU), Account (customer tier)         |
| Pharmacy Management Service  | Formulas, Batches, Lot numbers, Compliance records | Catalog, Inventory, Order                      |
| Order Management Service     | Orders, Order lines, Contract references           | Catalog, Pricing, Inventory, Pharmacy          |
| Inventory Management Service | Stock levels, Lot tracking, Reservations           | Pharmacy (batch creation), MDM (raw materials) |
| Fulfillment Service          | Shipments, Carrier assignments, Labels             | Order, Inventory                               |
| Account Management Service   | Providers, Practices, Clinics, Users               | MDM (golden records), CRM                      |

---

## 5. Microservice Catalogue

### 5.1 Catalog Service

**Responsibility:** System-of-record for all sellable and compoundable products (SKUs), including attributes, packaging, lifecycle status, and channel eligibility.

**Spring Boot Module:** `catalog-service`

**Key Endpoints:**
```
GET  /v1/catalog/products            → paginated product listing
GET  /v1/catalog/products/{sku}      → product detail + attributes
POST /v1/catalog/products            → create product (admin)
PUT  /v1/catalog/products/{sku}      → update product
GET  /v1/catalog/products/{sku}/availability → real-time ATP check
```

**Domain Events Published (Azure Service Bus):**
- `ProductCreated` → Topic: `catalog-events`
- `ProductUpdated` → Topic: `catalog-events`
- `ProductDiscontinued` → Topic: `catalog-events`

**Database (SQL Server):** `db-catalog`

```sql
-- Core tables
Products (ProductId PK, SKU, Name, Type[COMPOUND|OTC|DEVICE], Status, CreatedAt, UpdatedAt)
ProductAttributes (AttributeId PK, ProductId FK, AttributeKey, AttributeValue)
ProductChannels (ProductId FK, ChannelType[MARKETPLACE|DIRECT|B2B], IsActive)
ProductPricing (ProductId FK, PricingServiceRef) -- ref only, no price data owned here
```

**Integrations:** MDM (raw material linkage), ERP Connector (stock ledger reference)

---

### 5.2 Pricing Service

**Responsibility:** Manages all pricing rules, contracts, tier pricing, and real-time price resolution for a given customer + SKU + quantity combination.

**Spring Boot Module:** `pricing-service`

**Key Endpoints:**
```
POST /v1/pricing/resolve             → resolve price for {customerId, sku, qty, channel}
GET  /v1/pricing/contracts/{id}      → get contract details
POST /v1/pricing/contracts           → create pricing contract
GET  /v1/pricing/rules               → list active rules
```

**Pricing Resolution Chain (Strategy Pattern):**
```
1. Contract-specific pricing     (highest priority)
2. Customer-tier pricing
3. Channel-specific pricing
4. Volume/quantity-based pricing
5. Base MSRP                     (fallback)
```

**Domain Events Published:**
- `PricingContractUpdated` → Topic: `pricing-events`
- `PriceResolved` (audit only, no consumer) → Topic: `pricing-audit`

**Database:** `db-pricing`

```sql
PricingContracts (ContractId PK, CustomerId, StartDate, EndDate, Status)
PricingRules (RuleId PK, RuleType, Priority, Conditions NVARCHAR(MAX) JSON, Discount)
PricingTiers (TierId PK, TierName, MinOrderValue, DiscountPct)
ContractLineItems (LineItemId PK, ContractId FK, SKU, UnitPrice, MinQty)
```

**Cache:** Azure Redis — price results cached 15 min, invalidated on `PricingContractUpdated`

---

### 5.3 Pharmacy Management Service

**Responsibility:** The core compounding pharmacy domain — formulas, batch manufacturing, dispensing, pharmacist review, regulatory compliance (USP 795/797/800, DEA, FDA 503A/503B), and lot traceability.

**Spring Boot Module:** `pharmacy-service`

**Key Endpoints:**
```
GET  /v1/pharmacy/prescriptions/{id}        → prescription detail
POST /v1/pharmacy/prescriptions             → intake new prescription
PUT  /v1/pharmacy/prescriptions/{id}/review → pharmacist review action
POST /v1/pharmacy/batches                   → create manufacturing batch
GET  /v1/pharmacy/batches/{batchId}/lot     → full lot traceability chain
POST /v1/pharmacy/batches/{batchId}/dispense → dispense from batch
GET  /v1/pharmacy/compliance/reports        → compliance audit report
POST /v1/pharmacy/formulas                  → create/version a formula
GET  /v1/pharmacy/formulas/{id}/bom         → bill of materials
```

**Internal Workflow (State Machine):**
```
Prescription Intake
  └─► Validation (DEA schedule check, HIPAA patient verify)
       └─► Pharmacist Review
            └─► Formula Selection / BOM Resolution
                 └─► Batch Manufacturing Initiated
                      └─► QC / Compliance Check (USP 797/800)
                           └─► Dispensed
                                └─► Published to Order Service
```

**Domain Events Published:**
- `PrescriptionValidated` → Topic: `pharmacy-events`
- `BatchManufacturingStarted` → Topic: `pharmacy-events`
- `BatchReady` → Topic: `pharmacy-events`
- `PrescriptionDispensed` → Topic: `pharmacy-events`
- `ComplianceCheckFailed` → Topic: `pharmacy-compliance-alerts` (dead-letter + alert)

**Database:** `db-pharmacy`

```sql
Prescriptions (PrescriptionId PK, PatientId, ProviderId, Status, DEASchedule, CreatedAt)
Formulas (FormulaId PK, FormulaCode, Version, USPClass[795|797|800], Status, EffectiveDate)
FormulaBOM (BOMId PK, FormulaId FK, RawMaterialSKU, Quantity, Unit, Lot)
Batches (BatchId PK, FormulaId FK, BatchNumber, ManufacturedQty, LotNumber, ExpiryDate)
BatchLotChain (ChainId PK, BatchId FK, RawMaterialLot, Supplier, ReceivedDate, TestResult)
DispensedItems (DispenseId PK, BatchId FK, PrescriptionId FK, DispensedQty, DispensedBy)
ComplianceRecords (RecordId PK, BatchId FK, CheckType, Result, Auditor, Timestamp)
```

**Compliance Notes:**
- All pharmacist review actions captured with digital signature (Azure AD identity + timestamp)
- DEA Schedule II–V prescriptions require dual-pharmacist e-Sign
- Lot chain immutable after batch close (append-only audit rows)

---

### 5.4 Order Management Service

**Responsibility:** Receives and orchestrates orders from all channels (Marketplace, Provider Portal, Direct API). Manages order lifecycle, contract validation, compliance gating, and coordinates with Inventory and Pharmacy services.

**Spring Boot Module:** `order-service`

**Key Endpoints:**
```
POST /v1/orders                      → place order
GET  /v1/orders/{orderId}            → order detail
PUT  /v1/orders/{orderId}/confirm    → confirm order (post inventory reserve)
PUT  /v1/orders/{orderId}/cancel     → cancel order
GET  /v1/orders/{orderId}/status     → lightweight status check
POST /v1/orders/{orderId}/validate   → compliance pre-check
```

**Order Orchestration Saga (Choreography via Service Bus):**
```
1. OrderPlaced event emitted
2. Inventory Service: RESERVE stock          → InventoryReserved / ReservationFailed
3. Pricing Service:   VALIDATE price lock     → PriceLocked / PriceChanged
4. Pharmacy Service:  VALIDATE prescription  → PrescriptionValidated / ValidationFailed
5. All ACKs received → OrderConfirmed emitted
6. Any NACK          → Compensating events (release reserve, void price lock)
```

**Domain Events Published:**
- `OrderPlaced` → Topic: `order-events`
- `OrderConfirmed` → Topic: `order-events`
- `OrderCancelled` → Topic: `order-events`
- `OrderShipped` → Topic: `order-events` (consumed from Fulfillment)

**Database:** `db-orders`

```sql
Orders (OrderId PK, CustomerId, ChannelType, Status, TotalAmount, PlacedAt, ConfirmedAt)
OrderLineItems (LineId PK, OrderId FK, SKU, Qty, UnitPrice, PrescriptionId)
OrderSagaState (SagaId PK, OrderId FK, Step, Status, LastUpdated)
OrderContracts (OrderId FK, ContractId, ContractPriceRef)
OrderAudit (AuditId PK, OrderId FK, Action, PerformedBy, Timestamp, OldStatus, NewStatus)
```

---

### 5.5 Inventory Management Service

**Responsibility:** Real-time stock levels, lot tracking, available-to-promise (ATP) calculation, reservation management, batch receipts from Pharmacy, and ERP stock ledger synchronisation.

**Spring Boot Module:** `inventory-service`

**Key Endpoints:**
```
GET  /v1/inventory/{sku}/atp              → available-to-promise quantity
POST /v1/inventory/reserve                → reserve stock for an order
DELETE /v1/inventory/reserve/{reserveId} → release reservation
POST /v1/inventory/batches/receive        → receive new batch (from Pharmacy)
GET  /v1/inventory/lots/{lotNumber}       → lot detail + chain
POST /v1/inventory/adjustments            → manual stock adjustment (audited)
GET  /v1/inventory/reorder-alerts        → items below reorder point
```

**Domain Events Published:**
- `StockUpdated` → Topic: `inventory-events`
- `InventoryReserved` → Topic: `inventory-events`
- `ReservationReleased` → Topic: `inventory-events`
- `LowStockAlert` → Topic: `inventory-alerts`

**Domain Events Consumed:**
- `BatchReady` (from Pharmacy) → triggers batch receipt
- `OrderCancelled` (from Order) → releases reservation
- `ShipmentCreated` (from Fulfillment) → deducts committed stock

**Database:** `db-inventory`

```sql
StockLevels (StockId PK, SKU, WarehouseId, OnHandQty, ReservedQty, ATP_Qty, ReorderPoint)
LotRecords (LotId PK, LotNumber, SKU, BatchId, ReceivedQty, ExpiryDate, Status)
Reservations (ReserveId PK, OrderId, SKU, ReservedQty, ExpiresAt, Status)
StockMovements (MovementId PK, SKU, MovementType, Qty, Reference, Timestamp, PerformedBy)
```

---

### 5.6 Fulfillment Service

**Responsibility:** Consumes confirmed orders, orchestrates warehouse pick-pack-ship, batch shipment mapping, carrier API integration, label generation, and dispatch events.

**Spring Boot Module:** `fulfillment-service`

**Key Endpoints:**
```
POST /v1/fulfillment/shipments            → create shipment for confirmed order
GET  /v1/fulfillment/shipments/{id}       → shipment detail
PUT  /v1/fulfillment/shipments/{id}/dispatch → dispatch shipment
GET  /v1/fulfillment/shipments/{id}/label → fetch shipping label (PDF/ZPL)
GET  /v1/fulfillment/shipments/{id}/tracking → carrier tracking status
POST /v1/fulfillment/batch-allocations    → batch-to-shipment allocation
```

**Shipment Lifecycle:**
```
OrderConfirmed → ShipmentCreated → BatchAllocated → Packed → LabelGenerated → Dispatched
```

**External Integration (Shipping Carrier):**
```java
// Carrier API via Anti-Corruption Layer
CarrierApiAdapter
  ├── FedExAdapter implements CarrierPort
  ├── UPSAdapter   implements CarrierPort
  └── USPSAdapter  implements CarrierPort

interface CarrierPort {
    ShipmentLabel generateLabel(ShipmentRequest req);
    TrackingInfo   getTracking(String trackingNumber);
}
```

**Domain Events Published:**
- `ShipmentCreated` → Topic: `fulfillment-events`
- `ShipmentDispatched` → Topic: `fulfillment-events`
- `OrderShipped` → Topic: `fulfillment-events` (consumed by Order, Communication)

**Database:** `db-fulfillment`

```sql
Shipments (ShipmentId PK, OrderId, CarrierId, Status, TrackingNumber, LabelUrl, CreatedAt)
ShipmentItems (ItemId PK, ShipmentId FK, SKU, Qty, LotNumber, BatchId)
BatchShipmentMap (MapId PK, ShipmentId FK, BatchId, AllocatedQty)
CarrierTransactions (TxId PK, ShipmentId FK, CarrierRef, RequestPayload, ResponsePayload, Ts)
```

---

### 5.7 Account Management Service

**Responsibility:** Provider and customer identity — onboarding, CRM sync, token issuance for marketplace access, user account lifecycle.

**Spring Boot Module:** `account-service`

**Key Endpoints:**
```
POST /v1/accounts/providers           → onboard provider (HCP, clinic, hospital)
GET  /v1/accounts/providers/{id}      → provider detail
POST /v1/accounts/tokens              → issue marketplace access token
PUT  /v1/accounts/providers/{id}/status → activate/suspend provider
GET  /v1/accounts/customers/{id}      → customer profile (for Order context)
```

**Domain Events Published:**
- `ProviderOnboarded` → Topic: `account-events`
- `ProviderSuspended` → Topic: `account-events`
- `CustomerAccountCreated` → Topic: `account-events` (triggers welcome email via Communication Service)

**Database:** `db-accounts`

```sql
Providers (ProviderId PK, NPI, Name, Type[HCP|CLINIC|HOSPITAL], Status, CreatedAt)
ProviderAddresses (AddressId PK, ProviderId FK, AddressType, Street, City, State, Zip)
CustomerAccounts (AccountId PK, ProviderId FK, Email, Status, CreatedAt)
AccessTokens (TokenId PK, AccountId FK, Token, Scope, IssuedAt, ExpiresAt, Revoked)
```

---
### 5.8 Communication Service
**Responsibility:** Consumes domain events and dispatches outbound communications — email (welcome, order confirmation, shipping), SMS, and in-app notifications.
**Spring Boot Module:** `communication-service`
**Event Subscriptions:**
| Event | Action |
|---|---|
| `CustomerAccountCreated` | Send welcome email |
| `OrderConfirmed` | Send order confirmation email + SMS |
| `OrderShipped` | Send dispatch notification + tracking link |
| `LowStockAlert` | Send internal ops alert |
| `ComplianceCheckFailed` | Send pharmacist alert + Slack webhook |
**Integrations:** Twilio (SMS), SendGrid/Azure Communication Services (Email), NICE (contact centre webhook)
**Database:** `db-communications` (outbox pattern for at-least-once delivery)
```sql
OutboxMessages (MessageId PK, EventType, Recipient, Channel, Payload, Status, CreatedAt, SentAt)
CommunicationTemplates (TemplateId PK, Name, Channel, Subject, BodyTemplate, Version)
DeliveryLogs (LogId PK, MessageId FK, Provider, ProviderRef, Status, Timestamp)
```

---
### 5.9 Manufacturing Compliance Service (503B)
**Responsibility:** Dedicated service for FDA 503B outsourcing facility compliance — beyond individual-prescription-level batch controls. Handles eBR (electronic Batch Records), deviation management, CAPA, and FDA report generation.
**Spring Boot Module:** `compliance-service`
**Key Endpoints:**
```
POST /v1/compliance/ebr                  → create electronic batch record
GET  /v1/compliance/ebr/{batchId}        → retrieve eBR
POST /v1/compliance/deviations           → log deviation
PUT  /v1/compliance/deviations/{id}/capa → attach corrective action
GET  /v1/compliance/reports/fda-503b     → FDA 503B compliance report
POST /v1/compliance/esign/{recordId}     → e-sign a record
```

**Integrations:** ERP Quality module (audit deviations), FDA eStar gateway (for 503B facilities)

---

## 6. API Gateway & Orchestration Layer

### Azure API Management (APIM) Configuration

```
Marketplace Portal / Provider UI / External API
         │
         ▼
┌────────────────────────────────────────────────────────┐
│              Azure API Management (APIM)               │
│                                                        │
│  Routing          → URL rewrite to ACA service URLs    │
│  Throttling       → Rate limits per subscription key   │
│  Versioning       → /v1/, /v2/ header or path routing  │
│  Auth             → JWT validation (Azure AD B2C)      │
│  Security         → TLS termination, IP allowlisting   │
│  Rate Binding     → Per-consumer quota enforcement     │
│                                                        │
└────────────────────────────────────────────────────────┘
         │
         ▼  Internal VNet
┌─────────────────────────────────────────────────────────────────┐
│                   Azure Container Apps Environment              │
│  catalog-svc  pricing-svc  pharmacy-svc  order-svc  ...        │
└─────────────────────────────────────────────────────────────────┘
```

### Orchestration Layer

Complex multi-service workflows (e.g. "Get Available to Purchase" on the marketplace) are handled by a thin **Orchestration Service** that composes responses from Catalog + Pricing + Inventory:

```java
@Service
public class ProductAvailabilityOrchestrator {
    public ProductAvailabilityResponse resolve(String sku, String customerId) {
        CompletableFuture<CatalogProduct>  catalog   = catalogClient.getProduct(sku);
        CompletableFuture<ResolvedPrice>   price     = pricingClient.resolve(sku, customerId);
        CompletableFuture<AvailableQty>    inventory = inventoryClient.getATP(sku);
        
        return CompletableFuture.allOf(catalog, price, inventory)
            .thenApply(v -> ProductAvailabilityResponse.builder()
                .product(catalog.join())
                .price(price.join())
                .availableQty(inventory.join().getQty())
                .build())
            .join();
    }
}
```

---

## 7. Event-Driven Architecture (Azure Service Bus)

### Topic / Subscription Matrix

| Topic | Published By | Subscribed By | Message Type |
|---|---|---|---|
| `catalog-events` | Catalog Service | Pricing, Inventory, Fulfillment, Order | ProductCreated, ProductUpdated |
| `pricing-events` | Pricing Service | Order Service (price lock), Cache invalidation | PricingContractUpdated |
| `pharmacy-events` | Pharmacy Service | Inventory, Order, Fulfillment, Compliance | BatchReady, PrescriptionDispensed |
| `order-events` | Order Service | Fulfillment, Communication, ERP Connector | OrderConfirmed, OrderCancelled |
| `inventory-events` | Inventory Service | Order Saga, ERP Connector | InventoryReserved, LowStock |
| `fulfillment-events` | Fulfillment Service | Order, Communication | ShipmentDispatched |
| `account-events` | Account Service | Communication, CRM Connector | ProviderOnboarded |
| `pharmacy-compliance-alerts` | Pharmacy, Compliance | Compliance Service, Ops | ComplianceCheckFailed |

### Spring Boot Azure Service Bus Integration

```java
// Producer (any service)
@Service
public class PharmacyEventPublisher {

    @Autowired
    private ServiceBusSenderClient senderClient;

    public void publishBatchReady(BatchReadyEvent event) {
        ServiceBusMessage message = new ServiceBusMessage(toJson(event))
            .setContentType("application/json")
            .setSubject("BatchReady")
            .setMessageId(UUID.randomUUID().toString())  // idempotency key
            .setCorrelationId(event.getTraceId());       // distributed trace
        
        senderClient.sendMessage(message);
    }
}

// Consumer (idempotent)
@Service
@ServiceBusListener(destination = "pharmacy-events", group = "inventory-service")
public class PharmacyEventConsumer {

    @ServiceBusMessageListener
    public void onBatchReady(BatchReadyEvent event, ServiceBusReceivedMessageContext ctx) {
        if (idempotencyStore.alreadyProcessed(ctx.getMessage().getMessageId())) {
            ctx.complete();  // deduplicate
            return;
        }
        inventoryService.receiveBatch(event);
        idempotencyStore.markProcessed(ctx.getMessage().getMessageId());
        ctx.complete();
    }
    
    @ServiceBusErrorHandler
    public void onError(ServiceBusErrorContext ctx) {
        // DLQ after max delivery count; alert ops
        metricsService.incrementDLQCounter(ctx.getEntityPath());
    }
}
```

### Saga Orchestration (Order Confirmation)

```
OrderService                         ServiceBus                    Consumer Services
     │                                   │                               │
     ├──── emit: OrderPlaced ────────────►                               │
     │                                   ├──── InventoryService ◄────────┤
     │                                   │          reserve stock         │
     │                                   ├──── emit: InventoryReserved ──►│
     │                                   ├──── PricingService ◄───────────┤
     │                                   │          lock price            │
     │                                   ├──── emit: PriceLocked ─────────►
     │                                   ├──── PharmacyService ◄──────────┤
     │                                   │          validate Rx           │
     │                                   ├──── emit: RxValidated ─────────►
     │ ◄── all ACKs received ────────────┤                               │
     ├──── emit: OrderConfirmed ─────────►                               │
     │                                                                   │
     │  [Compensating Path: if any step NACK]                            │
     ├──── emit: OrderFailed ────────────►                               │
     │                                   ├──── InventoryService: release reserve
     │                                   └──── PricingService: void price lock
```

---

## 8. Data Architecture

### Per-Service SQL Server Schema (Database-per-Service Pattern)

Each microservice has its own **Azure SQL Managed Instance database**. No cross-service JOINs are permitted. Cross-domain reads use service APIs or read from events.

### Master Data Management (MDM)

The MDM layer provides golden records consumed read-only by all services via a dedicated MDM API:

```
MDM Service (read-only API surface for all services)
├── Practice/Customer: Provider, Patient, Clinic, Hospital
├── Formula: BOM, Instructions, Dosage forms, Strengths
├── Raw Material: Ingredients, Grades, Specs, Units, Suppliers
├── Product (SKU): BOM ref, Attributes, Packaging, Status lifecycle
└── Vendor: Suppliers, Sites, Certifications, DEA registration
```

### Data Layer Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                           DATA LAYER                             │
│                                                                  │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  │   Data Store    │  │   Data Lake     │  │ MDM Management  │  │
│  │  (Transactional)│  │ (Reporting,     │  │  (Azure Purview │  │
│  │  Azure SQL MI   │  │  Analytics, AI) │  │   + Custom MDM) │  │
│  │  per service    │  │  Azure Synapse  │  │                 │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘  │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │           Reporting / Power BI (Embedded)               │    │
│  │  Prescription analytics, Batch KPIs, Compliance metrics │    │
│  └─────────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────────┘
```

### Event Sourcing for Lot Traceability

The Pharmacy and Inventory services use an **append-only event store** pattern for lot chain:

```sql
-- Immutable lot event log (never UPDATE or DELETE)
LotEvents (
  EventId       BIGINT IDENTITY PRIMARY KEY,
  LotNumber     NVARCHAR(50)   NOT NULL,
  EventType     NVARCHAR(50)   NOT NULL,  -- RECEIVED | SAMPLED | RELEASED | DISPENSED | RECALLED
  Payload       NVARCHAR(MAX)  NOT NULL,  -- JSON
  PerformedBy   NVARCHAR(100)  NOT NULL,
  Timestamp     DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
  TraceId       NVARCHAR(64)   NOT NULL   -- OpenTelemetry trace correlation
)
```

---

## 9. Cross-Cutting Concerns

### 9.1 Identity & Access Management

```
External User/System
       │
       ▼
Azure AD B2C (AuthN)
  ├── Provider Portal → Role: PROVIDER
  ├── Internal Portal → Role: PHARMACIST | OPS | ADMIN
  ├── Marketplace API → Role: MARKETPLACE_CLIENT
  └── External Integration → Role: INTEGRATION_SERVICE

       │  JWT access token
       ▼
APIM (validates JWT, extracts roles)
       │  Forwards claims in headers (X-User-Id, X-Roles)
       ▼
Microservice (Spring Security)
  @PreAuthorize("hasRole('PHARMACIST')")
  @PreAuthorize("hasAnyRole('ADMIN', 'OPS')")
```

### 9.2 Observability (Three Pillars)

**Distributed Tracing (OpenTelemetry):**
```java
// Auto-instrumented via Spring Boot + OTEL agent
// All Service Bus messages carry W3C trace context headers
// Traces visible in Azure Application Insights
```

**Structured Logging:**
```java
// MDC-based per-request context
MDC.put("traceId", traceId);
MDC.put("orderId", orderId);
MDC.put("service", "pharmacy-service");
log.info("Batch manufacturing started", kv("batchId", batchId));
// → ships to Azure Monitor Logs (Log Analytics Workspace)
```

**Metrics (Micrometer → Azure Monitor):**
- `orders.placed.count` per channel
- `pharmacy.batches.manufactured.count` per formula
- `inventory.stock.atp` per SKU (gauge)
- `servicebus.dlq.count` per topic (alert threshold: > 0)
- `compliance.checks.failed.count` (alert: PagerDuty)

### 9.3 Resilience Patterns

```java
@Bean
public CircuitBreakerConfig inventoryCircuitBreaker() {
    return CircuitBreakerConfig.custom()
        .failureRateThreshold(50)
        .waitDurationInOpenState(Duration.ofSeconds(30))
        .slidingWindowSize(10)
        .build();
}

// Usage in Order Service calling Inventory
@CircuitBreaker(name = "inventory", fallbackMethod = "fallbackReserve")
@Retry(name = "inventory", maxAttempts = 3)
@TimeLimiter(name = "inventory", timeoutDuration = "2s")
public CompletableFuture<ReservationResult> reserveStock(ReservationRequest req) {
    return inventoryClient.reserve(req);
}
```

### 9.4 Centralized Logging

All services write structured JSON logs to **stdout** (12-factor). Azure Container Apps ships them to **Log Analytics Workspace**. Retention: 90 days hot, 2 years cold (Blob Archive) for compliance.

### 9.5 Key Vault Integration

```yaml
# application.yml per service — no hardcoded secrets
spring:
  cloud:
    azure:
      keyvault:
        secret:
          endpoint: https://empower-pms-kv.vault.azure.net/
          # Secrets injected at startup:
          # db-pharmacy-connection-string
          # servicebus-connection-string
          # twilio-api-key
          # fedex-api-key
```

---

## 10. Integration Layer

### ERP Connector (Azure Integration Services)

Bidirectional sync between PMS microservices and ERP (SAP / Oracle / Dynamics):

```
PMS Events → Azure Service Bus → ERP Connector → ERP API
ERP Events → ERP Webhook → ERP Connector → Service Bus → PMS Services

ERP Connector handles:
├── Batch Production sync (Pharmacy ↔ ERP Manufacturing)
├── Financial Posting (Order confirmed → ERP Finance AR)
├── Procurement sync (Inventory reorder alert → ERP PO creation)
├── Inventory Ledger (stock movements → ERP stock ledger)
├── Invoice generation (fulfilled order → ERP invoice)
└── Validate/Create Official Order (ERP order confirmation loop)
```

### MDM Connectors

```
MDM ← → Salesforce (CRM): Provider/Customer golden records
MDM ← → ERP: Vendor, Raw Material master data
MDM ← → PMS Services: Read-only API access (no direct DB)
```

### Anti-Corruption Layer Pattern

All external system integrations go through ACL adapters:

```java
// External model is never used inside PMS domain
public class ErpOrderAdapter implements ErpOrderPort {

    public PmsOrder fromErpOrder(ErpOrderDto erp) {
        return PmsOrder.builder()
            .orderId(erp.getSapOrderId())           // remap IDs
            .status(mapStatus(erp.getOrderStatus())) // map status enums
            .items(erp.getLineItems().stream()
                .map(this::mapLineItem).toList())
            .build();
    }
}
```

### Payment Gateway Integration

```
Order Service → Payment Gateway Connector → (Stripe / Authorize.Net / internal)
├── Authorize payment on OrderConfirmed
├── Capture payment on ShipmentDispatched
└── Refund on OrderCancelled (post-shipment)
```

---

## 11. Security & Compliance

### HIPAA Controls

| Control | Implementation |
|---|---|
| Data Encryption at Rest | Azure SQL Transparent Data Encryption (TDE); Blob Storage encryption |
| Data Encryption in Transit | TLS 1.3 enforced at APIM and ACA ingress |
| PHI Access Logging | All queries/mutations on `Prescriptions`, `Patients` tables emit audit log |
| Minimum Necessary Access | RBAC per role; field-level masking for non-privileged roles |
| Business Associate Agreements | Azure BAA in place for all relevant services |

### DEA Compliance (Controlled Substances)

```
Prescription Intake → DEA Schedule Classification
  ├── Schedule II: Dual pharmacist e-Sign required (no electronic refills)
  ├── Schedule III-V: Single pharmacist e-Sign; limited refills tracked
  └── All Schedules: Logged to DEA-auditable `ControlledSubstanceLedger` table

ControlledSubstanceLedger (
  RecordId        BIGINT IDENTITY PK,
  PrescriptionId  UNIQUEIDENTIFIER NOT NULL,
  DEASchedule     TINYINT NOT NULL,          -- 2, 3, 4, 5
  PatientId       UNIQUEIDENTIFIER NOT NULL,
  PrescriberDEA   NVARCHAR(20) NOT NULL,
  DispensedQty    DECIMAL(10,3) NOT NULL,
  Pharmacist1Id   NVARCHAR(100) NOT NULL,    -- e-Sign #1
  Pharmacist2Id   NVARCHAR(100),             -- e-Sign #2 (Schedule II)
  Timestamp       DATETIME2 DEFAULT SYSUTCDATETIME(),
  TraceId         NVARCHAR(64)
)
```

### FDA 503A/503B

- **503A** (patient-specific): Each prescription tied to patient + prescriber; no proactive manufacturing
- **503B** (outsourcing facility): Batch manufacturing logs, eBR, annual FDA reports managed by `compliance-service`

### Audit Trail Pattern

Every service implements the `AuditInterceptor` pattern:

```java
@Aspect
@Component
public class AuditInterceptor {
    @AfterReturning(pointcut = "@annotation(Auditable)", returning = "result")
    public void audit(JoinPoint jp, Object result) {
        AuditEntry entry = AuditEntry.builder()
            .service(serviceName)
            .action(jp.getSignature().getName())
            .userId(SecurityContextHolder.getContext()...)
            .timestamp(Instant.now())
            .traceId(MDC.get("traceId"))
            .payload(serialize(jp.getArgs()))
            .result(serialize(result))
            .build();
        auditRepository.save(entry);
        // Also emit to Service Bus audit topic for SIEM ingestion
    }
}
```

---

## 12. Deployment Architecture (Azure)

```
┌─────────────────────────────────────────────────────────────────────┐
│                     AZURE SUBSCRIPTION                              │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────┐      │
│  │               Resource Group: empower-pms-prod            │      │
│  │                                                          │      │
│  │  ┌────────────────────────────────────────────────┐     │      │
│  │  │         Azure Container Apps Environment        │     │      │
│  │  │  (Internal VNet: 10.0.0.0/16)                  │     │      │
│  │  │                                                │     │      │
│  │  │  catalog-svc    pricing-svc    pharmacy-svc    │     │      │
│  │  │  order-svc      inventory-svc  fulfillment-svc │     │      │
│  │  │  account-svc    communication-svc compliance-svc│    │      │
│  │  │  orchestrator   mdm-api        erp-connector   │     │      │
│  │  └────────────────────────────────────────────────┘     │      │
│  │                                                          │      │
│  │  Azure API Management ──► Public ingress                │      │
│  │  Azure Service Bus Premium ──► Event backbone            │      │
│  │  Azure SQL Managed Instance ──► Per-service DBs          │      │
│  │  Azure Cache for Redis ──► Distributed cache             │      │
│  │  Azure Key Vault ──► Secrets                             │      │
│  │  Azure Blob Storage ──► Docs, Labels, Archives           │      │
│  │  Azure Monitor + App Insights ──► Observability          │      │
│  │  Azure Synapse Analytics ──► Data Lake + Reporting        │      │
│  └──────────────────────────────────────────────────────────┘      │
│                                                                     │
│  ┌──────────────────────────────────────────────────┐             │
│  │   Resource Group: empower-pms-nonprod             │             │
│  │   (dev + staging: same topology, smaller SKUs)   │             │
│  └──────────────────────────────────────────────────┘             │
└─────────────────────────────────────────────────────────────────────┘
```

### CI/CD Pipeline (Azure DevOps)

```
Git Push → PR Validation
  ├── Unit tests (JUnit 5 + Mockito)
  ├── Contract tests (Spring Cloud Contract)
  ├── SAST (SonarCloud + OWASP dependency check)
  └── Build Docker image → push to Azure Container Registry

Merge to main → Deploy to Dev (auto)
  └── Integration tests (Testcontainers + Azure SQL emulator)

Manual gate → Deploy to Staging
  └── Smoke tests + performance baseline (k6)

Manual gate → Deploy to Production
  ├── Blue-green deployment (ACA traffic weights)
  ├── Canary: 10% → 50% → 100%
  └── Automated rollback on error rate > 1% (Azure Monitor alert)
```

---

## 13. Architectural Decision Records

### ADR-001: Database-per-Service over Shared Database

**Decision:** Each microservice owns a dedicated Azure SQL database.  
**Rationale:** Enforces bounded-context isolation; allows independent schema evolution; prevents coupling via shared joins. Trade-off: cross-domain queries require API calls or event-driven projections.

### ADR-002: Azure Service Bus over Apache Kafka

**Decision:** Azure Service Bus (Premium tier) as the event backbone.  
**Rationale:** Managed PaaS on Azure; native RBAC integration with Azure AD; sessions support for ordered delivery in Pharmacy workflow; dead-letter queues out-of-the-box; lower operational overhead for the team. Kafka would be preferred if event replay beyond 7 days or >1M msg/sec throughput required.

### ADR-003: Saga Choreography over Orchestration

**Decision:** Order confirmation saga uses choreography (event-driven state machine) rather than a central orchestrator.  
**Rationale:** Avoids a single point of failure; each service owns its compensation logic. Trade-off: harder to visualise end-to-end flow; mitigated by distributed tracing in App Insights.

### ADR-004: Anti-Corruption Layer for All External Systems

**Decision:** All ERP, CRM, Carrier, and Payment integrations go through ACL adapters.  
**Rationale:** Protects PMS domain model from external model pollution. Allows swapping carrier providers without touching domain code.

### ADR-005: Append-Only Lot Traceability Log

**Decision:** `LotEvents` table is append-only; no UPDATEs or DELETEs.  
**Rationale:** HIPAA and FDA require immutable audit trails for dispensing records. Row-level security prevents accidental mutation.

### ADR-006: Azure Container Apps over AKS

**Decision:** ACA for container orchestration rather than AKS.  
**Rationale:** Serverless scale-to-zero reduces cost for non-pharmacy-hours workloads; built-in KEDA autoscaling from Service Bus queue depth; simpler operations for the current team size. Re-evaluate if network policy or custom ingress controllers become required.

---

## 14. Extension & Evolution Guidance

### Adding a New Microservice

1. Clone the `spring-boot-service-template` repository
2. Define your bounded context and data ownership in the Domain Model matrix (Section 4)
3. Register new Service Bus topics in the Topic Matrix (Section 7)
4. Create a new Azure SQL database in the `empower-pms-prod` SQL Managed Instance
5. Add the APIM route in the API Gateway configuration
6. Update MDM ownership registry if the service introduces new master data entities

### Adding a New Carrier

1. Implement `CarrierPort` interface in `fulfillment-service`
2. Register the adapter in Spring `@Configuration`
3. Add carrier credentials to Azure Key Vault
4. No other services change — ACL pattern isolates the integration

### Scaling for 503B Growth

If 503B batch volumes grow to require dedicated infrastructure:

1. Extract `pharmacy-service` into `pharmacy-503a-service` and `pharmacy-503b-service`
2. Each owns its own database and Service Bus subscription filter
3. APIM route by `X-Facility-Type: 503B` header

### Introducing Event Replay / CQRS Projection

If reporting demands outgrow Synapse polling:

1. Enable Service Bus message archiving to Azure Blob (already supported in Premium tier)
2. Introduce a `projection-service` that replays events from Blob to build read models
3. Expose read models via dedicated `/query` endpoints — write endpoints remain on domain services

---

*This blueprint should be reviewed and updated:*  
*— When a new bounded context is introduced*  
*— When the tech stack changes materially*  
*— Quarterly during architecture review*  
*— After any significant compliance audit finding*

---
**© Empower Pharmacy · Internal Use Only · June 2026**
