## I. Architecture & System Design (10–12 min)

### Q: "Design a system that handles prescription order processing at scale — from intake through fulfillment — with traceability at every step."

I'd break this into five bounded contexts: **Order Intake**, **Prescription Validation**, **Inventory**, **Manufacturing/Compounding**, and **Fulfillment/Shipping**. Each runs as an independent Spring Boot Reactive microservice on AKS.

The frontend is a Next.js application — pharmacy staff use it for order management dashboards, patient lookup, and status tracking. It talks to a **GraphQL gateway** that acts as the orchestration layer, aggregating data from multiple backend services into a single query. This keeps the frontend decoupled from backend service topology.

For the flow: a prescription comes in via the Order Intake service, which validates the payload and publishes an `OrderReceived` event to **Azure Service Bus**. The Prescription Validation service subscribes to this topic, checks drug interactions, verifies provider credentials, and publishes `OrderValidated` or `OrderRejected`. The Inventory service reserves stock, the Compounding service queues the manufacturing work order, and the Fulfillment service handles shipping label generation and carrier integration.

For traceability — this is the critical part in a regulated pharmacy — I'd use an **event sourcing** pattern. Every state change is an immutable event stored in an append-only event store. This gives us a complete audit trail: who changed what, when, and why. We get HIPAA-compliant audit logging as a natural byproduct of the architecture, not as an afterthought. Each event includes a correlation ID that threads through all services, so we can reconstruct the full lifecycle of any prescription.

Observability: OpenTelemetry for distributed tracing across all services, structured logging shipped to Azure Monitor, and Prometheus metrics scraped by Grafana dashboards on AKS. We'd set up alerts for SLA breaches — for example, if a prescription sits in validation longer than 5 minutes.

---

### Q: "How do you decide service boundaries? Walk me through a real example."

I align service boundaries with **business domain boundaries** using Domain-Driven Design. The question I ask is: "If this capability were owned by a different team tomorrow, what data and logic would they need to operate independently?" That's your service boundary.

**Concrete example:** I worked on a system that started as a monolith handling both customer orders and warehouse operations. The pain point was that deploying a change to the order flow required retesting warehouse logic — and warehouse had a completely different release cadence.

We ran an event-storming workshop with product and engineering. Two bounded contexts emerged clearly: **Order Management** (customer-facing, needed fast iteration) and **Warehouse Operations** (operational, needed stability and reliability). The shared concept was "order" — but an order means different things in each context. In Order Management, it's a customer's intent to purchase. In Warehouse, it's a pick list with bin locations.

We split them into two services with their own databases (database-per-service pattern). They communicated via events: Order Management published `OrderConfirmed`, Warehouse subscribed and created its own internal representation. This gave each team deployment independence. Order Management shipped 3x a week; Warehouse shipped biweekly.

The anti-pattern I watch for: splitting by technical layer (a "user service," a "notification service") instead of by business capability. That creates chatty services that can't do anything useful alone.

---

### Q: "How do you handle distributed transactions across services?"

I avoid distributed transactions wherever possible — two-phase commit doesn't scale and creates tight coupling. Instead, I use the **Saga pattern** with eventual consistency.

For a pharmacy order flow, I'd use an **orchestration-based saga**. Here's why: the business process has a clear sequence (validate → reserve inventory → compound → ship), and if any step fails, we need specific compensating actions. An orchestrator service owns this workflow — it sends commands to each participant and handles failures.

If the Compounding step fails (say, an ingredient is out of stock), the orchestrator triggers compensating actions: release the inventory reservation, notify the prescriber, and update the order status to "on hold." Each step is idempotent — if a message is delivered twice, the service detects the duplicate via a unique saga ID and skips it.

I'd implement this with **Azure Service Bus sessions** for ordered message processing within a saga, and dead-letter queues for messages that fail after retries. The orchestrator persists its state so it can recover from crashes.

The alternative — choreography — works for simpler flows where services just react to events. But for a regulated pharmacy workflow with 5+ steps and complex failure modes, orchestration gives better visibility into where a process is stuck and makes it easier to add compensating logic.

---

### Q: "How do you manage data ownership when multiple services need the same data?"

Each service owns its data — no shared databases. When another service needs that data, I use one of three patterns depending on the use case:

- **Event-carried state transfer:** The owning service publishes events with the relevant data payload. Consuming services maintain their own read-optimized local copies. Example: the Patient service publishes `PatientUpdated` events; the Order service keeps a local projection of patient name, allergies, and insurance info that it needs for order validation. Eventual consistency is acceptable here — patient data doesn't change every second.
- **API query at runtime:** When the consuming service needs data that's too large to replicate or changes too frequently, it queries the owning service's API directly. I add caching (Redis) and circuit breakers to handle failures gracefully.
- **GraphQL aggregation:** For the frontend, the GraphQL layer resolves data from multiple services in a single query. This is where DataLoader batching prevents N+1 calls. The frontend doesn't need to know which service owns what — the schema hides that complexity.

The key discipline: one service is always the **source of truth** for any given entity. If two services seem to co-own data, it's a sign the boundaries are wrong, and I'd revisit the domain model.

---

## II. Core Stack Deep Dive (15–18 min)

### Q: "Why Spring WebFlux over traditional Spring MVC? When would you not use reactive?"

Spring MVC uses a **thread-per-request** model — each incoming request occupies a thread for its entire lifecycle, including time spent waiting for database queries, external API calls, or message broker responses. Under high concurrency, you exhaust the thread pool and start queuing requests.

WebFlux uses an **event-loop** model with non-blocking I/O. A small number of threads (typically one per CPU core) handle many concurrent requests by releasing the thread while waiting for I/O. For a pharmacy system processing thousands of concurrent order status checks, prescription validations, and inventory queries — most of which are I/O-bound — this means dramatically higher throughput on the same hardware.

When I would **not** use reactive:

- **CPU-bound workloads** — if your service is doing heavy computation (e.g., batch report generation), reactive doesn't help because you're not waiting on I/O.
- **Simple CRUD with low concurrency** — if a service handles 50 requests/second and talks to one database, MVC is simpler and the team is more productive. The reactive programming model has a steeper learning curve and harder debugging.
- **Legacy dependencies** — if most of your downstream calls are to blocking JDBC drivers or SOAP services, you're wrapping everything in `Schedulers.boundedElastic()` anyway, which negates the benefit.

For Empower's architecture, reactive makes sense for the orchestration and data-access-heavy services. For a simple internal admin tool, I'd use MVC.

---

### Q: "Explain backpressure in Project Reactor."

Backpressure is the mechanism by which a **consumer tells a producer to slow down** when it can't keep up. Without it, a fast producer overwhelms a slow consumer, leading to out-of-memory errors or dropped data.

In Project Reactor, this follows the Reactive Streams specification. When a subscriber subscribes to a `Flux`, it calls `request(n)` to say "give me n items." The publisher only emits that many items. Once the subscriber processes them, it requests more. This pull-based model prevents unbounded buffering.

Practical example in a pharmacy context: suppose we have a `Flux` streaming prescription orders from Azure Service Bus. The downstream validation service processes each order by checking a drug interaction database (which takes ~50ms). If orders arrive faster than 50ms each, backpressure kicks in — the Flux buffers a bounded number and signals the Service Bus consumer to stop pulling messages until the validator catches up.

Operators that affect backpressure behavior:

- `onBackpressureBuffer(maxSize)` — buffers up to maxSize, then errors or drops
- `onBackpressureDrop()` — drops items the consumer can't handle (useful for real-time metrics where latest value matters more than every value)
- `limitRate(n)` — prefetches n items at a time from upstream, useful for controlling batch sizes
---
### Q: "How do you handle blocking calls inside a reactive pipeline?"
Never run blocking code on the event-loop threads — that blocks all other requests sharing that thread. The solution is to **offload blocking calls to a dedicated thread pool**.
In Project Reactor, you use `Schedulers.boundedElastic()`, which is designed specifically for wrapping blocking I/O:
```java
Mono.fromCallable(() -> legacyJdbcRepository.findById(id))
    .subscribeOn(Schedulers.boundedElastic())
    .flatMap(result -> processReactively(result));
```

The `fromCallable` wraps the blocking call, and `subscribeOn(boundedElastic)` ensures it runs on a separate, bounded thread pool (capped at 10x CPU cores by default). The rest of the pipeline stays on the event-loop threads.

For database access specifically, I'd push to adopt **R2DBC** instead of JDBC — it's a truly non-blocking database driver for PostgreSQL, MySQL, and SQL Server. That eliminates the need for the boundedElastic workaround for DB calls. For legacy SOAP or REST APIs that block, the boundedElastic approach is the right pragmatic choice.

The key mistake to avoid: using `block()` anywhere in a reactive pipeline. That defeats the entire purpose and will cause Reactor to throw an `IllegalStateException` if you're on a non-blocking thread.

---

### Q: "Walk me through a reactive prescription processing pipeline."

Here's the flow as a reactive chain:

1. **Receive:** A message arrives from Azure Service Bus. I use the `azure-messaging-servicebus` reactive receiver, which gives us a `Flux<ServiceBusReceivedMessage>`.

2. **Deserialize & Validate:** `flatMap` to parse JSON into a `PrescriptionOrder` DTO, then run schema validation. Invalid payloads get routed to a dead-letter topic via `onErrorResume`.

3. **Drug Interaction Check:** `flatMap` into a non-blocking WebClient call to the Drug Interaction service. This returns `Mono<InteractionResult>`. If there's a critical interaction, we short-circuit with an `OrderRejected` event.

4. **Inventory Check:** `flatMap` into a WebClient call to the Inventory service. Uses `Mono.zip` to check availability of all ingredients in parallel, then combine results.

5. **Publish Event:** If all checks pass, publish `OrderValidated` to Service Bus via the reactive sender. The acknowledgment from Service Bus completes the `Mono`.

6. **Error handling:** `retryWhen(Retry.backoff(3, Duration.ofSeconds(1)))` for transient failures (network blips). `onErrorResume` for permanent failures — log, publish to a dead-letter topic, and move on. Each step logs with a correlation ID for traceability.

The entire pipeline is non-blocking. No thread sits idle waiting for the drug interaction database or the inventory API. A handful of Netty event-loop threads handle thousands of concurrent prescriptions.

---

### Q: "How do you test reactive code?"

**StepVerifier** is the core tool. It lets you subscribe to a Mono or Flux and assert emissions step by step:

```java
StepVerifier.create(prescriptionService.validate(order))
    .expectNext(ValidationResult.APPROVED)
    .verifyComplete();
```

For testing error scenarios: `.expectError(DrugInteractionException.class)`. For testing backpressure: `.thenRequest(5).expectNextCount(5)`.

For time-dependent operations (timeouts, retries), StepVerifier has a **virtual time** mode: `StepVerifier.withVirtualTime(() -> service.retryableCall())` lets you fast-forward through retry delays without actually waiting.

Integration tests: I use **Testcontainers** for a real Service Bus emulator and a real database, with `WebTestClient` for end-to-end reactive endpoint testing.

Debugging is harder than imperative code because stack traces are less meaningful — you see Reactor internals, not your business logic. Two things help: the `Hooks.onOperatorDebug()` flag (dev only — it's expensive) which adds assembly-time stack traces, and the `.checkpoint("description")` operator sprinkled at key points in the chain to create named reference points in error traces.

---
### Q: "Why GraphQL over REST? What new problems does it introduce?"
==**Why GraphQL here:** In a microservices architecture with a React frontend, the frontend often needs data from 3–4 services for a single view.== A pharmacy dashboard showing order details needs data from Order, Patient, Inventory, and Shipping services. ==With REST, that's either 4 round trips from the client or a BFF that aggregates them — and you build a new BFF endpoint for every new view.== ==GraphQL lets the frontend declare exactly what it needs in a single query, and the gateway resolves it from the right services.==

**Problems it introduces:**

- **N+1 queries:** If a query fetches 50 orders, each with a patient, the naive implementation makes 50 separate calls to the Patient service. Solution: DataLoader batches those into a single call that fetches all 50 patients at once.
- **Query complexity attacks:** A malicious or poorly-written query can request deeply nested data and cripple the backend. I'd implement query depth limiting, complexity scoring (assign a cost to each field, reject queries above a threshold), and persisted queries in production — only pre-approved query hashes can execute.
- **Caching is harder:** REST responses are easily cached by URL. GraphQL POST requests with varying query bodies defeat HTTP caching. Solutions: response-level caching at the GraphQL server, and data-level caching (Redis) within each resolver.
- **Monitoring complexity:** Every request hits the same `/graphql` endpoint, so you can't use URL-based metrics. You need custom instrumentation that tags metrics by operation name.

---

### Q: "How do you prevent N+1 queries with DataLoader?"

DataLoader batches and caches individual lookups into batch operations.

**Without DataLoader:** resolving `order.patient` for 50 orders makes 50 calls to `patientService.getById(id)`.

**With DataLoader:** all 50 patient IDs are collected during the same execution tick, then DataLoader makes a single call to `patientService.getByIds([id1, id2, ..., id50])`. Results are mapped back to the correct orders.

Implementation: create a DataLoader per request (not global — you want request-scoped caching to avoid stale data and permission leaks). In Spring for GraphQL, you register a `BatchLoaderRegistry` that maps a key type to a batch loading function:

```java
registry.forTypePair(String.class, Patient.class)
    .registerMappedBatchLoader((ids, env) ->
        patientService.findAllByIds(ids));
```

The resolver then calls `dataLoader.load(order.getPatientId())` which returns a `CompletableFuture`. DataLoader handles the batching, deduplication (if two orders reference the same patient, it's fetched once), and per-request caching.

---

### Q: "How do you handle field-level authorization in GraphQL?"

I implement authorization at **two layers**:

**1. Resolver-level:** Each resolver checks permissions before returning data. In Spring for GraphQL, I use `@PreAuthorize` annotations on resolver methods: `@PreAuthorize("hasRole('PHARMACIST')")` on the `patient.medications` resolver ensures only pharmacists can see medication lists. This integrates with Spring Security and the JWT claims from the authenticated user.

**2. Schema directive:** Define a custom `@auth(requires: ROLE)` directive in the schema. A directive visitor intercepts field resolution and checks the user's roles before the resolver executes. This is declarative and visible in the schema — anyone reading it can see which fields are restricted.

For PHI fields specifically (SSN, diagnosis codes, full address), I'd add a **field-level masking** layer. If the requesting user doesn't have the PHI access role, the resolver returns a masked value (`"***-**-1234"`) instead of null — this way the UI still renders gracefully, and the audit log records who attempted access.

Important: authorization lives in the backend services, not only in the GraphQL layer. The GraphQL gateway is a convenience layer — if someone bypasses it and calls the Patient service directly, the same permission checks must apply at the service level.

---

### Q: "How do you structure a Next.js application consuming a GraphQL API?"

With Next.js App Router, I split by **what needs interactivity vs. what doesn't**:

**Server Components** (default): page layouts, data tables, order summaries — anything that renders static or near-static content. These fetch data at the server level using the GraphQL endpoint directly (via `fetch` with the server-side endpoint URL), so the HTML ships pre-rendered. No JavaScript bundle for these components. For a pharmacy dashboard showing today's orders, the initial table render is a server component — fast first paint, no loading spinner.

**Client Components** (`'use client'`): interactive elements — search filters, real-time status badges, modals for order editing, forms. These use Apollo Client's `useQuery` and `useMutation` hooks. Apollo Client is initialized once in a provider component wrapping the layout.

For **real-time updates** (e.g., live order status), I'd use GraphQL subscriptions over WebSocket for high-frequency updates, or polling with `useQuery({ pollInterval: 5000 })` for lower-frequency status checks — simpler to operate and sufficient for most pharmacy dashboard use cases.

State management: Apollo Client's cache handles server state. For local UI state (modal open/closed, filter selections), React's built-in `useState` and `useContext` are enough. I avoid adding Redux or Zustand unless there's complex cross-component state that context can't handle cleanly.

---

### Q: "How do you design an AKS cluster for a healthcare application?"

**Namespace strategy:** One namespace per environment (dev, staging, prod) is the minimum. Within prod, I'd separate by domain: `pharmacy-orders`, `manufacturing`, `platform-services`. This enables namespace-level resource quotas and RBAC — the manufacturing team can deploy to their namespace but can't touch order services.

**Network policies for HIPAA:** Default-deny all ingress and egress at the namespace level. Then explicitly allow only the traffic paths that should exist: the GraphQL gateway can talk to backend services, backend services can talk to their databases and Service Bus, nothing else. This is enforced via Kubernetes NetworkPolicy resources with Azure CNI. East-west traffic between services uses mTLS via a service mesh (Istio or Linkerd) so all inter-service communication is encrypted in transit.

**Resource limits:** Every pod gets explicit CPU/memory requests and limits. Requests are set based on actual p50 usage (from Prometheus metrics), limits at ~2x requests. This prevents noisy-neighbor problems — a runaway manufacturing batch job can't starve the order processing pods. Horizontal Pod Autoscaler (HPA) scales based on custom metrics (queue depth from Service Bus, request latency) rather than just CPU.

**Node pools:** Separate node pools for different workload profiles — a "general" pool for stateless API services and a "compute" pool with larger VMs for data-heavy operations. AKS node pools also enable Azure's Confidential Computing nodes for workloads handling PHI.

---

### Q: "Azure Service Bus topics vs. queues? How do you guarantee ordering and exactly-once?"

**Queues** for point-to-point: one producer, one consumer group. Example: a work queue of compounding tasks — each task should be processed by exactly one worker.

**Topics with subscriptions** for pub/sub: one event, multiple consumers. Example: when an `OrderValidated` event fires, the Inventory service needs it (to reserve stock), the Notification service needs it (to email the patient), and the Analytics service needs it (to update dashboards). Each has its own subscription with independent processing and retry logic.

**Message ordering:** Service Bus **sessions**. You assign a session ID (e.g., the order ID) to each message. Service Bus guarantees FIFO ordering within a session. All messages for order #12345 are processed in sequence, even if messages for other orders are processed in parallel. The consumer acquires a session lock, processes messages in order, then releases it.

**Exactly-once processing:** True exactly-once is a distributed systems myth — I aim for **effectively-once** via idempotency. Each message gets a unique message ID. The consumer checks a deduplication store (Redis or a database table) before processing. If the message ID exists, skip it. Service Bus also has built-in duplicate detection with a configurable time window.

---

### Q: "Walk me through your observability setup on AKS."

Three pillars, unified by correlation:

**Distributed tracing (OpenTelemetry):** Every incoming request gets a trace ID that propagates through all service calls via HTTP headers (W3C Trace Context). Each service auto-instruments with the OpenTelemetry Java agent, which captures spans for HTTP calls, database queries, and Service Bus operations. Traces export to Azure Monitor Application Insights, where I can see the full request journey across 5+ services and pinpoint which service introduced latency.

**Structured logging:** JSON-formatted logs with mandatory fields: trace ID, span ID, service name, operation, and timestamp. Logs ship to Azure Log Analytics via the AKS Diagnostic Settings integration. Every log line for a given prescription can be queried with a single KQL filter on trace ID. For PHI-related operations, we log the action and the user but never the PHI data itself.

**Metrics (Prometheus + Grafana):** Prometheus scrapes custom metrics from each service — request rate, error rate, latency percentiles (RED method), plus business metrics like orders processed per minute, queue depth, and validation failure rate. Grafana dashboards show both infrastructure health and business KPIs. Alerts fire to PagerDuty: p99 latency > 500ms for 5 minutes, error rate > 1% for 3 minutes, Service Bus dead-letter queue depth > 10.

The unifying principle: any alert should lead to a Grafana dashboard, which shows the anomaly, links to the relevant traces in Application Insights, and those traces link to the correlated logs. Three clicks from alert to root cause.

---

## III. AI/Data & Python (5–8 min)

### Q: "How would you integrate a Python ML model into a Java/Spring Boot backend?"

I'd decouple the model from the application via a **model-serving microservice**. The Python model runs behind a FastAPI endpoint (or TorchServe for PyTorch models) deployed as its own container on AKS. The Spring Boot service calls it via async WebClient — non-blocking, with circuit breakers (Resilience4j) and a fallback for when the model service is unavailable.

Architecture: `Spring Boot → WebClient (async) → FastAPI model service → returns prediction`

Why not embed the model in Java? Two reasons: (1) data scientists work in Python — forcing them to port models to Java creates friction and bugs, (2) the model service has its own scaling and deployment lifecycle. You might need to retrain and redeploy the model weekly without touching the order processing service.

For the contract between services: a well-defined gRPC or REST schema (OpenAPI). The model service accepts a feature vector and returns a prediction with a confidence score. The Spring Boot caller decides what to do with low-confidence predictions (e.g., route to human review).

For batch inference (e.g., nightly drug interaction analysis across all active prescriptions), I'd use a Python pipeline triggered by Azure Data Factory or an AKS CronJob, writing results to a shared data store that the Spring Boot services can query.

---

### Q: "How do you serve a model with sub-100ms latency at scale?"

Four levers:

- **Model optimization:** Quantize the model (FP32 → INT8) using ONNX Runtime. This cuts inference time by 2–4x with minimal accuracy loss. For transformer-based models, use distilled versions (DistilBERT instead of BERT).
- **Infrastructure:** Keep the model warm — no cold starts. Run a minimum of 2 replicas with pre-loaded models (load the model at container startup, not at first request). HPA scales on request latency, not CPU. If GPU inference is needed, use AKS GPU node pools with NVIDIA T4s.
- **Caching:** Many inference requests are repetitive. A drug interaction check for "Lisinopril + Metformin" returns the same result every time. Cache predictions in Redis keyed by the input hash. Cache hit = <1ms instead of 50ms inference.
- **Async when possible:** Not every prediction needs to be synchronous. If the UI can show "checking interactions..." while the prediction runs, use a request/response pattern via Service Bus — submit the prediction request, return immediately, push the result to the frontend via WebSocket when ready.

---

### Q: "Give me a concrete GenAI use case for pharmacy operations."

**Prescription intake triage.** Empower is a compounding pharmacy — prescriptions arrive in various formats (faxed PDFs, electronic prescriptions, phone-in notes). Today, a pharmacist manually reads each one, extracts the medication, dosage, patient info, and prescriber details, then enters it into the system.

A GenAI solution: use a multimodal LLM to extract structured data from prescription images and free-text notes. The model outputs a JSON payload: `{drug, dosage, form, quantity, patient_id, prescriber_npi}`. This goes through a **validation layer** — rules engine checks for valid NPI numbers, drug name against the formulary, dosage within safe ranges. The pharmacist reviews a pre-filled form instead of starting from scratch.

Production considerations that separate this from a demo:

- **Confidence scoring:** The model outputs a confidence score per field. High-confidence fields are pre-filled. Low-confidence fields are highlighted for manual review. The pharmacist always has final approval — the AI assists, it doesn't decide.
- **Audit trail:** Every AI-assisted extraction is logged: input document, model output, confidence scores, and pharmacist's final edits. This is critical for FDA and state board compliance.
- **Feedback loop:** Pharmacist corrections are captured and used for fine-tuning. If the model consistently misreads a specific prescriber's handwriting, that feedback improves future extractions.
- **Cost management:** LLM API calls at scale get expensive. Batch non-urgent prescriptions, use smaller models for simple electronic prescriptions, and reserve the large multimodal model for handwritten faxes.

---

### Q: "How do you build data pipelines that feed both operational reporting and ML model training?"

I'd use a **lambda-style architecture** with a shared event backbone:

**Real-time path (operational reporting):** All domain events (OrderCreated, InventoryReserved, ShipmentDispatched) flow through Azure Service Bus. A stream processor (Azure Stream Analytics or a dedicated Spring Boot consumer) aggregates these into real-time dashboards — orders per hour, average fulfillment time, current queue depth. Data lands in a time-series store (Azure Data Explorer) for operational queries.

**Batch path (ML training):** The same events are also sunk to Azure Data Lake (Parquet format) via an Event Hub capture or a dedicated consumer. A nightly Azure Data Factory pipeline transforms raw events into training-ready feature datasets — joined with patient demographics, drug catalogs, and historical outcomes. Data scientists access this via Azure Databricks notebooks.

The key principle: **events are the single source**. Both paths consume from the same event stream but optimize for different access patterns. Operational reporting needs low-latency, time-windowed aggregations. ML training needs large-scale historical datasets with rich joins.

Feature store (e.g., Feast) sits between the raw data and model training. It ensures the features used in training exactly match those used in production inference — preventing training-serving skew, which is the #1 silent killer of ML models in production.

---

## IV. Staff-Level Leadership & Scenarios (8–10 min)

### Q: "Tell me about a time you drove an architectural decision that other teams disagreed with."

**Situation:** We were building a new order management platform. Two teams wanted to share a single PostgreSQL database between the Order and Inventory services — it was "simpler" and avoided the complexity of eventual consistency.

**Task:** I believed shared databases would create deployment coupling and scaling bottlenecks as we grew, but I needed to convince two team leads and an engineering director.

**Action:** I wrote an RFC that laid out both approaches with concrete trade-offs. For the shared database: faster to build initially, but I modeled what happens at 10x current load — write contention on shared tables, inability to scale services independently, and the fact that every schema migration requires coordinating across two teams. For database-per-service: more upfront work, but each team owns their schema, deploys independently, and we can scale Inventory on a read-replica without affecting Orders.

I also built a small proof-of-concept showing the event-based sync between services — demonstrating that eventual consistency for inventory counts was acceptable (a 2-second delay in reflected stock levels was fine for their SLAs). I presented this at an architecture review with both teams.

**Result:** The teams agreed to database-per-service. Six months later, when we needed to migrate Inventory to a different data store (DynamoDB for better read performance), we did it with zero impact on the Order service. The team lead who initially disagreed told me it was the right call.

---

### Q: "How do you establish engineering standards across teams without becoming a bottleneck?"

Standards should be **self-service, not approval-gates**. Three mechanisms I use:

**1. Paved roads, not mandates:** Instead of writing a 50-page standards document nobody reads, I build **starter templates and libraries**. A Spring Boot service template with pre-configured observability (OpenTelemetry), health checks, CI/CD pipeline, Dockerfile, and Kubernetes manifests. Teams run `create-service --name my-service` and get a production-ready skeleton in 5 minutes. They can deviate, but the default path is the right path.

**2. Architecture Decision Records (ADRs):** Every significant decision gets documented: context, options considered, decision, and consequences. These live in a shared repo. When a new team member asks "why do we use Service Bus instead of Kafka?" — the ADR explains it. This scales my influence without requiring my presence in every meeting.

**3. Lightweight reviews, not gates:** I run a weekly 30-minute architecture office hours — any team can bring a design for feedback. For major changes (new service, new data store, public API change), I do a written async review on the PR/RFC within 24 hours. I'm a reviewer, not an approver — the team owns the decision, and I provide input.

The anti-pattern: becoming the "architecture police" who blocks PRs. That creates resentment and bottlenecks. My job is to make the right thing the easy thing, then trust the teams.

---

### Q: "Describe a technical trade-off between speed and system sustainability."

**Situation:** We had a hard deadline to launch a patient portal feature. The fastest path was adding the new API endpoints directly to an existing monolithic service that was already carrying significant technical debt — no tests for several modules, a tangled dependency graph, and manual deployments.

**Task:** Deliver the feature on time while not making the monolith harder to decompose later.

**Action:** I chose a middle path. Instead of either (a) building inside the monolith or (b) building a full microservice with all the infrastructure, I built the feature as a **separate module within the same codebase** but with strict boundaries — its own package structure, its own database schema (separate schema, same database), and a clean interface to the rest of the monolith. It communicated with existing code only through a defined internal API, not by reaching into shared data models.

This gave us speed (no new deployment pipeline, no service discovery setup) while keeping the code extractable. I documented the extraction plan: when traffic justifies it, this module becomes its own service by swapping the internal API calls for HTTP/event-based communication.

**Result:** We shipped on time. Four months later, when we started the decomposition effort, this module was the easiest to extract — it took 3 days instead of the 3 weeks the other modules required. The team adopted the "modular monolith" approach for all new features from that point forward.

---

### Q: "Walk me through the most complex production incident you've resolved."

**Incident:** Our order processing system started experiencing intermittent 30-second latency spikes every 15–20 minutes during peak hours. No errors in logs — requests just hung and then completed. Customer-facing impact: the checkout page would time out for ~10% of users.

**Detection:** Our p99 latency alert fired in PagerDuty. Grafana showed latency spikes were periodic but not perfectly regular — ruling out a simple cron job.

**Diagnosis:** I traced a slow request using our distributed tracing (Jaeger) and found the bottleneck in the database connection pool. During spikes, all connections were exhausted and new requests queued. But our query latency dashboards showed normal query times — the queries themselves were fast.

Deeper investigation: I enabled connection pool metrics and found that connections were being acquired but not released promptly. The root cause: a code path in the inventory check used a database connection inside a reactive pipeline but had a `subscribe()` call without proper error handling. When an upstream timeout cancelled the reactive chain, the connection wasn't returned to the pool. Over time, connections leaked until the pool was exhausted. The periodic "recovery" happened because the pool's idle timeout eventually reclaimed abandoned connections.

**Mitigation:** Immediate — increased pool max size from 20 to 50 to buy time. Then deployed a fix: wrapped the database call in `usingWhen` (Reactor's resource management operator) which guarantees connection release even on cancellation.

**Prevention:** Added connection pool utilization as a monitored metric with alerts at 80% usage. Added a code review checklist item: "verify all resource acquisition in reactive chains uses `usingWhen` or try-with-resources equivalent." Wrote a custom Reactor lint rule to flag bare `subscribe()` calls in connection-handling code.

---

### Q: "How do you design systems for graceful degradation in a pharmacy context?"

First, classify operations by **criticality**:

- **Critical (must never fail):** Prescription dispensing verification, drug interaction alerts, controlled substance tracking. These get the highest reliability investment — synchronous calls with retries, local fallback caches, and manual override workflows.
- **Important (can degrade):** Real-time inventory counts, shipping estimates, analytics dashboards. If the inventory service is down, show "last known quantity" from a cached snapshot with a "data may be delayed" indicator.
- **Nice-to-have (can be unavailable):** Recommendation engines, non-essential notifications, report generation. Feature-flag these off during incidents.

Implementation patterns:

- **Circuit breakers** (Resilience4j) on every inter-service call. When the Shipping service is down, the circuit opens and the order service returns "shipping estimate unavailable" instead of failing the entire order view.
- **Bulkheads:** Separate thread pools for critical vs. non-critical calls. A slow analytics query can't consume threads needed for prescription processing.
- **Local caches for critical reference data:** Drug interaction databases are cached locally in each service with a 1-hour TTL. If the central drug database is down, the cached version still catches 99% of interactions.
- **Queue-based leveling:** During traffic spikes, non-urgent operations (batch label printing, report generation) are queued in Service Bus and processed when load decreases.

---

### Q: "How do you design audit logging for PHI?"

HIPAA requires tracking who accessed what PHI, when, and why. My approach:

**Architecture:** An **append-only audit event store**, separate from application databases. Every access to PHI (read or write) generates an audit event: `{timestamp, userId, action, resourceType, resourceId, fieldsAccessed, ipAddress, justification}`. Events are immutable — no updates, no deletes. The store uses a write-once medium or has deletion protection enabled.

**Implementation:** Audit events are generated at the **service layer**, not the controller layer — this ensures that PHI access is logged regardless of how the service is called (API, message handler, batch job). I use an aspect-oriented approach: a custom `@AuditPHI` annotation on service methods that handle PHI. An AOP interceptor captures the access details and publishes an audit event to Service Bus, which a dedicated Audit Service persists.

**What NOT to log:** Never log the PHI data itself in the audit trail. Log that "User X accessed Patient Y's medication list" — not the actual medications. The audit log itself becomes a liability if it contains PHI.

**Retention and access:** HIPAA requires 6-year retention. Audit data goes to immutable blob storage (Azure Immutable Blob with a time-based retention policy). Access to audit logs is itself audited and restricted to compliance officers.

**Anomaly detection:** Flag unusual patterns — a user accessing 100 patient records in 10 minutes, access outside business hours, or access to VIP patient records. These generate alerts for the compliance team.

---

### Q: "What architectural patterns ensure data integrity and traceability in regulated environments?"

Four patterns I rely on:

- **Event sourcing:** Store every state change as an immutable event, not just the current state. The current state is derived by replaying events. This gives you a complete, tamper-evident history. "Why was this prescription modified?" — replay the events and see every change, who made it, and when.
- **CQRS (Command Query Responsibility Segregation):** Separate the write model (append events) from the read model (materialized views). The write side enforces business rules and audit requirements. The read side is optimized for queries. This separation means you can add new regulatory reporting views without changing the core write path.
- **Immutable infrastructure:** No in-place updates to production systems. Every deployment is a new container image with a SHA-tagged version. If a regulator asks "what code was running on March 15?" — we point to the exact image hash. Container images are stored in Azure Container Registry with immutability enabled.
- **Cryptographic integrity:** Hash chains on critical data (similar to blockchain concepts). Each audit event includes a hash of the previous event. If someone tampers with a record in the middle, the chain breaks and is detectable. For controlled substance records, this level of integrity is expected.