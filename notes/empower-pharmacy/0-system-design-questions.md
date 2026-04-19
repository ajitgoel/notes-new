##### Design a prescription management API that handles 10k requests/sec.
==Azure APIM (gateway, rate limiting)== → stateless .NET services in AKS with HPA → Redis cache for read-heavy patient lookups → Azure SQL with read replicas for queries → Service Bus(like a message queue) for async workflows (refill notifications, prior auth). HIPAA: append-only audit log in immutable Blob for every PHI access, ==mTLS between services==, all secrets via Key Vault.
##### How would you handle a distributed transaction across two microservices?
==**Saga pattern**. Choreography: each service emits domain events and reacts to others' events — looser coupling, harder to debug.== 
**Orchestration**: a central workflow coordinator drives the sequence, issues compensating transactions on failure — easier to trace. 
Use the **outbox pattern** on each service to guarantee events are published atomically with their DB writes.
##### SQL vs NoSQL for patient records?
==SQL (Azure SQL) for structured patient core data== — ACID guarantees, strong consistency, mature HIPAA tooling, easier audit. ==Cosmos DB for high-throughput event logs, device telemetry, or globally distributed read-heavy workloads where eventual consistency is acceptable==. Patient records and prescriptions almost always belong in SQL.

------------
##### **What is “mTLS between services”?** 
==**mTLS = mutual TLS (mutual Transport Layer Security).**==
Regular TLS (what makes “https://” work) is one-way: your browser verifies that the server is who it claims to be, but the server doesn’t verify the browser.
**mTLS is two-way** — both sides verify each other’s identity using certificates.
In a microservices system, you have many services talking to each other internally (the prescription service calls the patient service, which calls the audit service, etc.). ==mTLS means:==
- ==Service A proves its identity to Service B with a certificate==
- ==Service B proves its identity back to Service A with its own certificate==
- ==The traffic between them is encrypted==
**Why it matters for HIPAA:** You’re dealing with patient health data. If a rogue service (or an attacker who broke into the network) tries to call your patient service, it would be rejected because it doesn’t have a valid certificate. It prevents eavesdropping _and_ impersonation between services inside your own network. It’s the difference between “anyone on our internal network can call any service” vs. “only verified, authorized services can talk to each other.”

------------
##### ==**Saga — Orchestration style**== 
Think of this like a **conductor leading an orchestra**. One central coordinator tells everyone what to do and in what order.
1. ==The Orchestrator says: “Order Service, create the order.”==
2. ==Order Service does it and reports back.==
3. ==The Orchestrator says: “Payment Service, charge the card.”==
4. ==If Payment fails, the Orchestrator says: “Order Service, cancel that order.” (This is a **compensating transaction** — basically an “undo” action.)==
==This is **easier to trace and debug** because you can look at the orchestrator’s log and see every step that happened.== The tradeoff is that the orchestrator becomes a central piece that everything depends on.
##### **The Outbox Pattern** 
Here’s a subtle but critical problem: when your Order Service saves an order to its database, it also needs to publish an event (“Order Created”). But what if the database write succeeds and then the event publish fails (maybe the message queue is down for a second)? Now your database says the order exists, but no one else knows about it.
==The **outbox pattern** fixes this by putting the event _inside the same database transaction_ as the data change:==
1. ==In one atomic transaction, write the order to the `Orders` table AND write the event to an `Outbox` table in the same database.==
2. ==A background worker periodically reads the Outbox table and publishes those events to the message bus.==
==Since both writes happen in the same database transaction, they either both succeed or both fail. You never end up with a saved order but a missing event.==