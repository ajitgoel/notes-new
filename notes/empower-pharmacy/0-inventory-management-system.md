## 01 — System Requirements

### Functional Requirements

- **Real-time stock tracking** per SKU and compounding facility. Each SKU-location pair tracks: quantity on hand, quantity reserved for in-progress prescriptions, reorder point, and lot/batch number.
- **Prescription-driven reservation** — when a prescription order is accepted, raw ingredients (APIs, bases, excipients) are atomically reserved. If any ingredient is insufficient, the entire order fails.
- **Lot traceability and expiration management** — every ingredient is tracked by lot number with expiration dates. FIFO dispensing enforced. Expired lots auto-quarantined.
- **Automated reorder alerts** — when stock crosses reorder point, alert purchasing via Kafka event. Controlled substances trigger DEA-compliant ordering workflows.
- **Multi-location inventory** — Empower operates from multiple facilities. Inventory is location-specific; no implicit cross-facility transfers.
- **Audit trail** — every stock movement (receipt, reservation, dispense, adjustment, waste) logged immutably for FDA/State Board of Pharmacy compliance.

### Non-Functional Requirements

- **Consistency over availability** — overselling a controlled substance is a regulatory violation, not just a business problem. Strong consistency is non-negotiable.
- **HIPAA compliance** — inventory data that links to patient prescriptions must be encrypted at rest (AES-256) and in transit (TLS 1.3). Access audited.
- **99.95% availability** — pharmacy operations are time-sensitive; compounding can't stall waiting for inventory checks.
- **Latency** — inventory checks and reservations < 200ms p99.

> [!note] Empower-Specific Angle
> A compounding pharmacy doesn't sell finished products off a shelf — it *manufactures* each prescription from raw ingredients. Inventory isn't "100 units of Product X." It's "500g of Testosterone Cypionate powder, lot #TC-2026-0412, expires 2027-01." The reservation model deducts *quantities of raw materials* based on a formula/recipe, not discrete finished goods.

---
## 02 — Capacity Estimation
### SKUs and Locations

| Dimension                    | Estimate                                 | Rationale                                                                  |
| ---------------------------- | ---------------------------------------- | -------------------------------------------------------------------------- |
| Active ingredients (APIs)    | ~2,000 SKUs                              | Compounding pharmacies stock hundreds of active pharmaceutical ingredients |
| Bases, excipients, packaging | ~3,000 SKUs                              | Creams, capsule shells, vials, syringes, etc.                              |
| Compounding facilities       | 5–10 locations                           | Empower's sterile and non-sterile facilities                               |
| Total inventory records      | ~50,000 rows                             | 5,000 SKUs × 10 locations (with lot-level tracking: ~500K)                 |
| Prescriptions/day            | ~10,000–50,000                           | Empower is one of the largest 503A/503B compounders in the US              |
| Peak write QPS               | ~50–100                                  | Reservation + dispense during peak compounding hours                       |
| Storage                      | ~5–10 GB (inventory) + growing audit log | Small dataset, high write frequency                                        |

> [!important] Key Insight
> This is *not* an Amazon-scale problem. 50K inventory rows fits comfortably in a single PostgreSQL instance. The challenge isn't data volume — it's correctness under concurrent writes, lot-level tracking, and regulatory auditability.

---

## 03 — API Design

### Inventory CRUD

``` hl:14
// Inventory endpoints — RESTful, C#/.NET Core controllers
GET    /api/v1/inventory?locationId={id}&sku={sku}
POST   /api/v1/inventory/receive          // Goods receipt (new shipment)
PUT    /api/v1/inventory/{id}/adjust       // Manual adjustment (waste, damage)
GET    /api/v1/inventory/{id}/audit-trail   // Immutable history
GET    /api/v1/inventory/expiring?days=30   // Lots expiring within N days

// Prescription order — the critical path
POST   /api/v1/orders
  Body: {
    "prescriptionId": "rx-uuid",
    "formulaId": "formula-uuid",
    "locationId": "facility-uuid",
    "idempotencyKey": "client-generated-uuid",
    "ingredients": [
      { "skuId": "uuid", "lotId": "uuid", "quantityNeeded": 25.5, "unit": "g" },
      { "skuId": "uuid", "lotId": "uuid", "quantityNeeded": 100, "unit": "ml" }
    ]
  }

// Returns 200 with reservation IDs, or 409 if insufficient stock
POST   /api/v1/orders/{id}/dispense        // Confirms compounding complete
POST   /api/v1/orders/{id}/cancel          // Releases reserved stock
```

> [!tip] Interview Signal
> Note the `idempotencyKey`. If the network drops after the server commits but before the client gets the 200, a retry without idempotency would double-reserve stock. This is table stakes for a senior/architect-level answer.

---
## 04 — Database Design — PostgreSQL Schema
```sql
-- Core tables, pharmacy-domain-specific
CREATE TABLE ingredients (
  id            UUID PRIMARY KEY,
  sku           VARCHAR(50) UNIQUE,
  name          VARCHAR(255),
  category      VARCHAR(50),        -- 'API', 'base', 'excipient', 'packaging'
  is_controlled BOOLEAN DEFAULT FALSE,
  dea_schedule  VARCHAR(5),         -- 'II', 'III', 'IV', 'V' or NULL
  unit_of_measure VARCHAR(10),      -- 'g', 'ml', 'ea'
  created_at    TIMESTAMPTZ
);

CREATE TABLE inventory_lots (
  id            UUID PRIMARY KEY,
  ingredient_id UUID REFERENCES ingredients(id),
  location_id   UUID REFERENCES locations(id),
  lot_number    VARCHAR(50) NOT NULL,
  qty_on_hand   DECIMAL(12,4) NOT NULL DEFAULT 0,
  qty_reserved  DECIMAL(12,4) NOT NULL DEFAULT 0,
  expiration_date DATE NOT NULL,
  status        VARCHAR(20) DEFAULT 'active',  -- active|quarantined|expired
  version       INTEGER NOT NULL DEFAULT 1,
  updated_at    TIMESTAMPTZ,
  CONSTRAINT positive_stock CHECK (qty_on_hand - qty_reserved >= 0),
  UNIQUE(ingredient_id, location_id, lot_number)
);

CREATE TABLE prescription_orders (
  id              UUID PRIMARY KEY,
  prescription_id UUID NOT NULL,
  formula_id      UUID NOT NULL,
  location_id     UUID NOT NULL,
  status          VARCHAR(20),     -- reserved|compounding|dispensed|cancelled
  idempotency_key VARCHAR(64) UNIQUE,
  created_at      TIMESTAMPTZ
);

CREATE TABLE order_ingredients (
  order_id      UUID REFERENCES prescription_orders(id),
  lot_id        UUID REFERENCES inventory_lots(id),
  quantity      DECIMAL(12,4),
  PRIMARY KEY (order_id, lot_id)
);

CREATE TABLE inventory_audit_log (
  id            BIGSERIAL PRIMARY KEY,
  lot_id        UUID NOT NULL,
  event_type    VARCHAR(30),       -- receive|reserve|dispense|adjust|expire|cancel
  quantity_delta DECIMAL(12,4),
  reference_id  UUID,              -- order ID or receipt ID
  performed_by  UUID,
  occurred_at   TIMESTAMPTZ DEFAULT NOW(),
  reason        TEXT
);
```

> [!note] Why Lot-Level, Not SKU-Level
> Generic inventory systems track `qty_on_hand` per SKU-location. Pharmacy requires **lot-level granularity**: the same ingredient from two different suppliers has different lot numbers, different expiration dates, and different COA (Certificate of Analysis) data. The `CHECK` constraint on `qty_on_hand - qty_reserved >= 0` is your database-level safety net against overselling — even if application logic has a bug, the DB rejects the transaction.

---
## 05 — High-Level Design

```
                        ┌─────────────────────────────────────────┐
                        │            API Gateway                  │
                        │   (Azure API Management / Kong)         │
                        │   Rate limiting, auth, HIPAA audit log  │
                        └──────┬──────────┬──────────┬────────────┘
                               │          │          │
              ┌────────────────┘          │          └────────────────┐
              ▼                           ▼                          ▼
   ┌──────────────────┐      ┌──────────────────┐       ┌──────────────────┐
   │  Inventory Svc   │      │  Order Svc       │       │  Alert Svc       │
   │  (.NET Core)     │      │  (.NET Core)     │       │  (Python/worker) │
   │                  │      │                  │       │                  │
   │  CRUD, receive,  │      │  Reserve, dispense│      │  Reorder alerts, │
   │  adjust, query   │      │  cancel, idempot.│       │  expiry warnings │
   └───────┬──────────┘      └───────┬──────────┘       └───────┬──────────┘
           │                         │                           │
           │         ┌───────────────┘                           │
           ▼         ▼                                           │
   ┌──────────────────────┐     ┌──────────────┐                │
   │  PostgreSQL           │     │  Redis        │                │
   │  Primary + Sync       │     │  Cache: stock │◄───────────────┘
   │  Replica (RDS/Aurora) │     │  levels, hot  │
   │                       │     │  SKU lookups  │
   └──────────┬────────────┘     └──────────────┘
              │
              ▼
   ┌──────────────────────┐     ┌──────────────────┐
   │  Kafka               │────▶│  Reporting Svc   │
   │  Events: reserved,   │     │  Analytics, BI   │
   │  dispensed, restocked │     │  dashboards      │
   └──────────────────────┘     └──────────────────┘
```

The **Order Service** owns the critical path — it's the only service that writes reservations against inventory. The Inventory Service handles CRUD and read queries. This separation means a surge of dashboard queries can't starve the order path.

> [!important] Why Synchronous Reservation, Async Everything Else
> The reservation must be synchronous (request-response within the DB transaction) because you need an immediate yes/no answer for the pharmacist. Alerts, analytics, and channel sync happen via Kafka events *after* the transaction commits. This keeps the critical path fast and the rest decoupled.

---
## 06 — Request Flows — Order Fulfillment
```
Rx Order received → Idempotency check → BEGIN TXN → SELECT FOR UPDATE on lots
→ Check available qty → Reserve (decrement available) → COMMIT
→ Publish event to Kafka → Return 200
```
### Step-by-step
1. **Idempotency gate** — check `idempotency_key` in `prescription_orders`. If exists, return the cached result. No DB mutation.
2. **Begin transaction** with `SERIALIZABLE` or `REPEATABLE READ` isolation.
3. **Lock inventory rows** — `SELECT ... FROM inventory_lots WHERE ingredient_id IN (...) AND location_id = ? AND status = 'active' AND expiration_date > NOW() ORDER BY expiration_date ASC FOR UPDATE`. The `ORDER BY expiration_date ASC` enforces FIFO/FEFO (First Expired, First Out).
4. **Check available quantity** — for each ingredient, sum `qty_on_hand - qty_reserved` across qualifying lots. If any ingredient is short, `ROLLBACK` and return 409.
5. **Reserve** — increment `qty_reserved` on the selected lots, insert into `order_ingredients`, insert into `prescription_orders` with status `'reserved'`.
6. **Commit** — the CHECK constraint is the final guard. If a bug somehow tries to reserve more than available, the DB rejects it.
7. **Post-commit** — publish `InventoryReserved` event to Kafka. Alert Service checks if any lot crossed its reorder point.

**What If the Order Has 8 Ingredients?**
==The transaction locks multiple rows. Deadlock risk is real. Mitigate by **always acquiring locks in a deterministic order** — sort ingredient/lot IDs before the SELECT FOR UPDATE==. Both Order A and Order B will attempt locks in the same order, preventing circular waits.

---
## 07 — Pessimistic Locking with SELECT FOR UPDATE
```sql
-- The core concurrency-safe reservation query
BEGIN;
-- Lock rows in deterministic order (by lot ID) to prevent deadlocks
SELECT id, qty_on_hand, qty_reserved
  FROM inventory_lots
  WHERE ingredient_id = 'ingredient-uuid'
    AND location_id = 'facility-uuid'
    AND status = 'active'
    AND expiration_date > NOW()
  ORDER BY expiration_date ASC, id ASC
  FOR UPDATE;
-- Application logic: walk lots FEFO, accumulate until quantity met
-- Then for each lot consumed:
UPDATE inventory_lots
  SET qty_reserved = qty_reserved + 25.5,
      version = version + 1,
      updated_at = NOW()
  WHERE id = 'lot-uuid';
-- The CHECK constraint (qty_on_hand - qty_reserved >= 0)
-- acts as the final safety net
INSERT INTO inventory_audit_log (lot_id, event_type, quantity_delta, reference_id)
  VALUES ('lot-uuid', 'reserve', -25.5, 'order-uuid');
COMMIT;
```

**Concurrent scenario:** Order A and Order B both want 300g of the same ingredient. Only 400g remains across two lots. Order A acquires the lock first, reserves 300g. Order B blocks on `FOR UPDATE`, waits. A commits. B's lock is acquired — it now sees the updated 100g. Insufficient for 300g → B's transaction rolls back, returns 409.

**Interview Signal**
==`FOR UPDATE` is a *row-level* lock in PostgreSQL, not a table lock. Other orders for different ingredients proceed concurrently without contention. The lock scope is narrow by design.==

---

## 08 — Trade-offs: Pessimistic vs. Optimistic Locking
### Pessimistic (SELECT FOR UPDATE)
- **Guarantees** no oversell — period
- Simple mental model: lock, read, write, release
- Throughput limited by lock hold time
- Deadlock possible (mitigated by deterministic ordering)
- **Best when:** contention is moderate and correctness is paramount
### Optimistic (Version Column)
- No locks held during processing — higher throughput
- `UPDATE ... WHERE version = @expected` — if 0 rows affected, retry
- Retries can cascade under high contention
- Multi-row atomicity is harder — partial updates on retry are messy
- **Best when:** contention is rare and single-row updates dominate

> [!note] The Right Choice for Empower
> **Pessimistic locking wins here.** A compounding pharmacy doing ~50 QPS doesn't have Amazon's contention levels. But it has *zero tolerance* for overselling controlled substances. Optimistic locking's retry-on-conflict model introduces complexity for multi-ingredient orders where partial retries get ugly. The throughput ceiling of pessimistic locking (hundreds of QPS on a single PostgreSQL primary) far exceeds Empower's needs.

> [!tip] Interview Signal
> Don't just pick one. Explain *why* it's right for this domain. "At Amazon's scale I'd consider optimistic. At Empower's scale with regulatory constraints, pessimistic is correct and simpler." That shows you can calibrate solutions to context, not just recite patterns.

---
## 09 — Failure Scenarios and Bottlenecks
### Database Primary Failure
PostgreSQL primary crashes. Automated failover (Patroni on-prem, or RDS Multi-AZ) promotes the **synchronous** replica within 15–30 seconds. In-flight transactions roll back. Clients retry, succeed against the new primary.
The critical detail: **synchronous replication is mandatory**. Async replication risks losing the last few committed reservation decrements — stock that was already reserved reappears as available, causing a double-dispense. In a pharmacy, that's a patient safety issue.
### Kafka Broker Failure
The reservation transaction has already committed to PostgreSQL. Kafka event publishing fails. Use the **transactional outbox pattern**: write the event to an `outbox` table within the same DB transaction. A separate poller (or CDC via Debezium) reads the outbox and publishes to Kafka. Guaranteed at-least-once delivery. Consumers must be idempotent.
### Hot Lot Contention
A popular ingredient (e.g., Testosterone Cypionate) has one active lot. Every order locks the same row. Under sustained load, queue depth grows. Mitigation: split large lots into sub-lots at goods receipt time, spreading locks across multiple rows. Alternatively, batch reservations in a micro-window (50ms) and process as a single transaction.
### Stuck Reservations
An order is reserved but never dispensed or cancelled — stock is locked indefinitely. Implement a **reservation TTL**: a background job scans for reservations older than N hours (configurable per facility) and auto-releases them, publishing a `ReservationExpired` event.

---
## 10 — Future Improvements
### ML-Based Demand Forecasting
Train models on historical prescription volumes, seasonality (flu season drives certain compounds), and provider ordering patterns to predict per-ingredient demand. Auto-adjust reorder points and safety stock levels. The audit log already contains the training data — every dispense event with timestamp, ingredient, and quantity.
### Beyond-Use Date (BUD) Optimization
Compounded medications have BUDs (shorter than manufacturer expiration). ML can optimize compounding schedules to minimize waste — compound closer to shipment time for short-BUD formulas.
### HL7 FHIR Integration
Expose inventory levels via HL7 FHIR `SupplyDelivery` and `SupplyRequest` resources. Enables EHR systems to check ingredient availability before providers write a prescription, reducing rejected orders.
### Event Sourcing for Audit
Replace the mutable `qty_on_hand` + append-only audit log with full event sourcing. Current state derived from event replay. Immutable by design. Simplifies FDA audit responses: "here's every event that affected this lot, in order."

> [!important] Architecture Evolution, Not Rewrite
> Each of these extensions plugs into the existing Kafka event bus and PostgreSQL audit log. No redesign needed — the core architecture was built with these expansion points in mind. That's the mark of a good initial design.