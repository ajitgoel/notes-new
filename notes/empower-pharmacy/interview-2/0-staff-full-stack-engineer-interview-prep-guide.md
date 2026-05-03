**45-minute technical interview with Tridib Chakraborty, Director of Enterprise Architecture.** Tridib has 20+ years in Java, microservices, cloud (AWS/Azure), NoSQL, and API gateways — and is actively focused on GenAI. Expect architecture-heavy questions anchored in the evaluation stack: React/Next.js, Java + Spring Boot Reactive, GraphQL, Python for AI/Data, and Azure (AKS, Service Bus).

| Duration | Tech Layers | Target Level | Domain |
|----------|-------------|--------------|--------|
| 45 min   | 5           | Staff        | Pharma |

---
## I. Architecture & System Design (10–12 min)

### 1. End-to-end architecture walkthrough `HIGH PRIORITY`

Tridib will likely open by asking you to design or describe a distributed system. Given his enterprise architecture title and the JD's emphasis on pharmacy/manufacturing workflows:

- → *"Design a system that handles prescription order processing at scale — from intake through fulfillment — with traceability at every step."*
- → *"Walk me through how you'd architect a service that coordinates manufacturing workflows across multiple facilities with real-time status tracking."*
- → *"How would you design a backend that handles high-throughput order processing while maintaining HIPAA-compliant audit trails?"*

> **Prep:** Structure your answer around the evaluation stack. Show React/Next.js at the edge, GraphQL as orchestration, Spring Boot Reactive services behind it, Azure Service Bus for async messaging between bounded contexts, and AKS for deployment. Emphasize event sourcing or CDC for auditability — regulatory traceability is a JD priority.

### 2. Microservices decomposition `HIGH PRIORITY`

Tridib's core expertise is microservices and cloud-native design. He will probe how you decompose domains:

- → *"How do you decide service boundaries? Walk me through a real example where you split a monolith or designed microservices from scratch."*
- → *"How do you handle distributed transactions across services — saga pattern, eventual consistency, compensating transactions?"*
- → *"How do you manage data ownership when multiple services need the same data?"*

> **Prep:** Have a concrete example ready from your past work. Reference Domain-Driven Design (bounded contexts, aggregates). Know the trade-offs between choreography vs. orchestration sagas. Mention Azure Service Bus for reliable async communication.

---

## II. Core Stack Deep Dive (15–18 min)

### 3. Java + Spring Boot Reactive `HIGH PRIORITY`

Tridib has deep Java roots. This is where he'll push hardest on technical depth.

- → *"Why Spring WebFlux over traditional Spring MVC? When would you not use reactive?"*
- → *"Explain backpressure in Project Reactor. How does it prevent resource exhaustion in a high-throughput pipeline?"*
- → *"How do you handle blocking calls (JDBC, legacy APIs) inside a reactive pipeline without starving the event loop?"*
- → *"Walk me through how you'd implement a reactive data pipeline that processes incoming prescription orders, validates them, checks inventory, and publishes events — all non-blocking."*
- → *"How do you test reactive code? What's different about debugging Mono/Flux chains vs. imperative code?"*

> **Prep:** Know Mono vs. Flux, `Schedulers.boundedElastic()` for wrapping blocking calls, R2DBC for reactive database access, and how to use StepVerifier for testing. Be ready to discuss thread model differences (event-loop vs. thread-per-request).

### 4. GraphQL orchestration

The stack uses GraphQL as the orchestration layer — Tridib (API gateway expert) will test your understanding of why and how.

- → *"Why GraphQL over REST for this architecture? What problems does it solve and what new problems does it introduce?"*
- → *"How do you prevent N+1 queries in a GraphQL server backed by multiple microservices? Explain DataLoader."*
- → *"How do you handle authorization at the field level in GraphQL?"*
- → *"How would you design a GraphQL schema that federates data from multiple Spring Boot services?"*

> **Prep:** Understand schema stitching vs. Apollo Federation. Know how to batch and cache with DataLoader. Be ready to discuss query complexity limits, persisted queries for security, and how GraphQL subscriptions could enable real-time status updates for pharmacy workflows.

### 5. React / Next.js frontend

Expect a lighter touch here — Tridib's background is backend/architecture — but he'll test full-stack fluency.

- → *"How do you structure a Next.js application that consumes a GraphQL API? Server components vs. client components — when do you use each?"*
- → *"How do you handle real-time updates on the frontend — for example, live order status tracking in a pharmacy dashboard?"*
- → *"What's your approach to state management in a complex React application?"*

> **Prep:** Know Next.js App Router, React Server Components, and how to integrate Apollo Client or urql with GraphQL. Be ready to discuss SSR vs. CSR trade-offs for healthcare dashboards where SEO matters less but performance matters more.

### 6. Azure platform (AKS & Service Bus)

Both JDs emphasize Azure heavily. Tridib's profile lists AWS/Azure cloud architecture as core expertise.

- → *"How do you design an AKS cluster for a healthcare application — namespace strategy, resource limits, network policies for HIPAA isolation?"*
- → *"When would you use Azure Service Bus topics vs. queues? How do you guarantee message ordering and exactly-once processing?"*
- → *"How do you implement zero-downtime deployments on AKS — blue/green vs. canary? How do you handle database migrations during rolling deploys?"*
- → *"Walk me through your observability setup — logging, metrics, distributed tracing across microservices on AKS."*

> **Prep:** Know Azure Monitor + Application Insights, Prometheus/Grafana on AKS, and distributed tracing with OpenTelemetry. Understand Service Bus dead-letter queues, duplicate detection, and sessions for ordered processing. The JD specifically calls out "observability through logging, monitoring, and alerting frameworks."

---

## III. AI/Data & Python (5–8 min)

### 7. AI integration in backend systems `TRENDING TOPIC`

Tridib's LinkedIn headline includes "GenAI" and his posts focus on enterprise AI adoption. He'll want to see practical AI thinking, not hype.

- → *"How would you integrate a Python ML model into a Java/Spring Boot backend? What's the serving architecture?"*
- → *"The JD mentions real-time inference. How do you serve a model with sub-100ms latency at scale?"*
- → *"How would you use GenAI to improve pharmacy operations — give me a concrete, production-ready example, not a demo."*
- → *"How do you build data pipelines that feed both operational reporting and ML model training?"*

> **Prep:** Discuss model serving via FastAPI or TorchServe behind a gateway, with Spring Boot calling it via async HTTP. Mention feature stores, A/B testing for model rollouts, and MLOps pipelines. For GenAI: think prescription validation, drug interaction checking, or automated documentation — grounded, healthcare-relevant use cases. Tridib specifically posted about the 95% of organizations that fail to get past AI pilots.

---

## IV. Staff-Level Leadership & Scenarios (8–10 min)

### 8. Technical leadership & influence

Staff-level = force multiplier. Tridib leads large multi-shore teams and values engineering standards.

- → *"Tell me about a time you drove an architectural decision that other teams initially disagreed with. How did you build consensus?"*
- → *"How do you establish engineering standards across teams without becoming a bottleneck?"*
- → *"Describe a situation where you had to make a technical trade-off between speed and system sustainability. What did you choose and why?"*

> **Prep:** Use STAR format. Emphasize writing RFCs/ADRs, running architecture reviews, and enabling teams through reusable platform components (a JD responsibility). Show you operate at the "what should we build and why" level, not just "how do I build it."

### 9. Production reliability & incident response

The JD calls out "resolve complex production issues impacting system stability and throughput."

- → *"Walk me through the most complex production incident you've resolved. How did you diagnose it? What systemic changes did you make afterward?"*
- → *"How do you design systems for graceful degradation in a pharmacy context where downtime directly impacts patients?"*

> **Prep:** Have a war story ready. Show structured thinking: detection → diagnosis → mitigation → root cause → prevention. Mention circuit breakers (Resilience4j), bulkheads, and how you'd architect for partial availability in a pharmacy system where some operations are more critical than others.

### 10. Healthcare domain & compliance

Empower operates in a highly regulated environment. Both JDs mention HIPAA and regulatory traceability.

- → *"How do you design audit logging for a system that handles PHI (Protected Health Information)?"*
- → *"What architectural patterns do you use to ensure data integrity and traceability in regulated environments?"*

> **Prep:** Even if you don't have healthcare experience, demonstrate you understand: immutable audit logs, encryption at rest and in transit, role-based access control at the service level, data residency requirements, and how event sourcing provides a natural audit trail. Mention HL7/FHIR awareness if applicable.

---

## V. Questions to Ask Tridib

Asking sharp questions signals staff-level thinking. Tailor these to Tridib's role and interests:

- "You recently joined Empower as Director of Enterprise Architecture — what's the biggest architectural challenge you're tackling right now?"
- "How far along is the migration to the reactive Spring Boot stack, and what drove that decision over traditional MVC?"
- "What does the boundary look like between the GraphQL orchestration layer and the underlying services — is it a BFF pattern or a unified gateway?"
- "Where are you seeing the most promising ROI from AI/ML in pharmacy operations today?"
- "How does the engineering team balance velocity on new features with the compliance overhead of operating in a regulated space?"