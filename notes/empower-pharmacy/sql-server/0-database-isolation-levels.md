## Quick Reference Matrix

| Isolation Level | Dirty Read | Non-Repeatable Read | Phantom Read | Performance |
|---|---|---|---|---|
| Read Uncommitted | Possible | Possible | Possible | Fastest |
| Read Committed | **Prevented** | Possible | Possible | Fast |
| Repeatable Read | **Prevented** | **Prevented** | Possible* | Moderate |
| Serializable | **Prevented** | **Prevented** | **Prevented** | Slowest |

*PostgreSQL's Repeatable Read also prevents phantoms via MVCC snapshots — but the SQL standard doesn't require it.*

### Anomaly Definitions

- **Dirty read** — reading uncommitted data from another transaction
- **Non-repeatable read** — reading the same row twice in one transaction and getting different values because another transaction committed in between
- **Phantom** — re-running a range query and getting new rows that another transaction inserted and committed

---
## 1. Read Uncommitted
**Rating: Never Use**
==A transaction can read rows modified by other transactions **that haven't committed yet**.== If that other transaction rolls back, you've acted on data that never existed.
### SQL Example
```sql hl:8
-- Transaction A: pharmacist starts receiving a shipment
BEGIN;
UPDATE inventory_lots SET qty_on_hand = qty_on_hand + 500
  WHERE lot_number = 'TC-2026-0412';
-- NOT YET COMMITTED

-- Transaction B: (Read Uncommitted) checks stock for an order
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
SELECT qty_on_hand FROM inventory_lots
  WHERE lot_number = 'TC-2026-0412';
-- Returns 500 (the uncommitted value)

-- Transaction A rolls back (shipment rejected at QC)
ROLLBACK;
-- Transaction B already reserved against 500g that don't exist
```

> [!danger] Empower Pharmacy Disaster
> A shipment of Testosterone Cypionate arrives. During receiving QC, the system tentatively adds 500g. Before QC is complete, a compounding order reads this uncommitted stock and reserves against it. QC fails — the shipment is rejected and rolled back. The order now references 500g of ingredient that was never actually received. The pharmacist compounds a prescription from stock that doesn't exist. **This is a patient safety and DEA compliance violation.**

PostgreSQL doesn't even implement this level — it silently upgrades to Read Committed. SQL Server supports it but recommends against it.

---
## 2. Read Committed
**Rating: PostgreSQL Default**
==Each statement within a transaction sees only data committed *before that statement began*. But two reads of the same row within one transaction can return different values if another transaction committed in between.======
### SQL Example
```sql
-- Transaction A: reservation flow
BEGIN;
SELECT qty_on_hand - qty_reserved AS available
  FROM inventory_lots
  WHERE lot_number = 'TC-2026-0412';
-- Returns 200g available

-- Transaction B commits a reservation for 180g between A's SELECT and UPDATE

UPDATE inventory_lots
  SET qty_reserved = qty_reserved + 150
  WHERE lot_number = 'TC-2026-0412';
-- Succeeds! But now qty_reserved exceeds qty_on_hand
-- available was 200, B took 180, only 20 left — A reserved 150
COMMIT;
-- OVERSOLD by 130g
```

> [!danger] Empower Pharmacy Problem
> Two pharmacists process orders for the same active ingredient simultaneously. Both read 200g available. Both reserve. Total reserved: 330g against 200g actual. The second compounding run pulls from a jar that's already empty. **This is exactly why Read Committed alone isn't enough — you need SELECT FOR UPDATE to close this gap.**

When It's Fine
==Read Committed **+ explicit row locks** (`SELECT ... FOR UPDATE`) is the sweet spot for Empower. The lock closes the read-then-write gap. Without it, Read Committed is appropriate for read-only dashboards, inventory reports, and queries where stale-by-one-transaction is acceptable.==

---
## 3. ==Repeatable Read==
**Rating: Snapshot Safety**
==The transaction sees a **snapshot** taken at the start of the transaction.== All reads return data as of that snapshot, regardless of what other transactions commit in between. In PostgreSQL, this also prevents phantoms (new rows inserted by others don't appear).
### SQL Example
```sql
-- Transaction A: generating an end-of-day inventory report
SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;
BEGIN;
SELECT ingredient_id, SUM(qty_on_hand) AS total
  FROM inventory_lots
  WHERE location_id = 'houston-facility'
  GROUP BY ingredient_id;
-- Snapshot locked: sees consistent state
-- Meanwhile, Transaction B receives a new shipment and commits
-- Transaction A re-runs the same query...
SELECT ingredient_id, SUM(qty_on_hand) AS total
  FROM inventory_lots
  WHERE location_id = 'houston-facility'
  GROUP BY ingredient_id;
-- Same results as before — snapshot is stable
COMMIT;
```

> [!warning] Empower Pharmacy Problem
> If you use Repeatable Read for the *reservation* flow (instead of just reports), you hit a subtle issue: Transaction A reads the snapshot, sees 200g available, and tries to reserve 150g. But Transaction B already committed a reservation for 180g after A's snapshot was taken. A's UPDATE succeeds against the *current* row state (not the snapshot), but it doesn't know about B's change. In PostgreSQL, this triggers a **serialization failure** (error code 40001), and A must retry. Your application must handle these retries — and with multi-ingredient orders, a retry means re-checking availability for *all* ingredients, not just the conflicting one.

> [!success] Perfect Use At Empower
> Inventory reports, FDA audit queries, end-of-day reconciliation. Any operation that needs to read a consistent snapshot of inventory across multiple queries without worrying about concurrent writes changing the numbers mid-report.

---
## 4. Serializable
**Rating: Full Isolation**
Transactions behave **as if they ran sequentially**, one after another. PostgreSQL implements this via Serializable Snapshot Isolation (SSI) — it detects dependency cycles and aborts one transaction with a serialization failure.
### SQL Example
```sql
-- Both transactions run at SERIALIZABLE

-- Transaction A
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN;
SELECT qty_on_hand - qty_reserved FROM inventory_lots
  WHERE ingredient_id = 'testosterone-cypionate'
    AND location_id = 'houston';
-- 200g available
UPDATE inventory_lots SET qty_reserved = qty_reserved + 150 ...;
COMMIT; -- succeeds

-- Transaction B (concurrent)
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN;
SELECT qty_on_hand - qty_reserved FROM inventory_lots
  WHERE ingredient_id = 'testosterone-cypionate'
    AND location_id = 'houston';
-- Also sees 200g (snapshot from before A committed)
UPDATE inventory_lots SET qty_reserved = qty_reserved + 180 ...;
COMMIT;
-- ERROR: could not serialize access
-- B must RETRY — and on retry sees only 50g left
```

> [!warning] Empower Pharmacy Problem
> Serializable is the *safest*, but the retry rate under moderate concurrency can spike. Every serialization failure means the pharmacist's screen hangs an extra round-trip while the application retries. With multi-ingredient orders (8+ ingredients), the probability of at least one conflict grows multiplicatively. Under peak compounding hours, you could see 10–20% of transactions needing retries. The safety is real, but the UX cost is measurable.

> [!success] When to Reach For It
> Controlled substance dispensing where DEA audit consequences are severe enough to justify the throughput cost. Batch reconciliation operations that touch many rows and need absolute consistency. In practice, `SELECT FOR UPDATE` at Read Committed gives you equivalent safety for the reservation path with far less overhead.

---
## The Right Call for Empower Pharmacy

Recommendation
- ==**Read Committed + SELECT FOR UPDATE** for the reservation/order path. You get row-level pessimistic locking exactly where you need it, without paying for snapshot overhead or serialization retries across the board.==
 - ==**Repeatable Read** for reporting and audit queries that need a stable snapshot.==
 - ==**Serializable** only for controlled substance workflows where the regulatory cost of any anomaly exceeds the throughput cost.==
This isn't one-size-fits-all — the right isolation level varies by operation type within the same system. That's the architect-level insight interviewers are looking for.