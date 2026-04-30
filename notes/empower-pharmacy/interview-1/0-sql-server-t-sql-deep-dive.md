# SQL Server / T-SQL Deep Dive
## Every concept likely to come up in a .NET developer SQL interview

---

## 1. JOINs

The foundation of relational queries. Know these cold.

### INNER JOIN — only matching rows from both tables

```sql
-- "Give me all orders with their patient info"
SELECT o.Id, o.Total, p.Name, p.Email
FROM Orders o
INNER JOIN Patients p ON o.PatientId = p.Id;
-- If a patient has no orders, they don't appear
-- If an order has no patient (broken FK), it doesn't appear
```

### LEFT JOIN — all rows from left table, matches from right

```sql
-- "Give me ALL patients, and their orders if they have any"
SELECT p.Name, o.Id AS OrderId, o.Total
FROM Patients p
LEFT JOIN Orders o ON p.Id = o.PatientId;
-- Patients with no orders show up with NULL for OrderId and Total
```

### RIGHT JOIN — all rows from right table, matches from left

```sql
-- Same as LEFT JOIN but reversed. Rarely used in practice —
-- just swap the table order and use LEFT JOIN instead.
```

### FULL OUTER JOIN — all rows from both, NULLs where no match

```sql
-- "Show me unmatched records on BOTH sides"
SELECT p.Name, o.Id
FROM Patients p
FULL OUTER JOIN Orders o ON p.Id = o.PatientId
WHERE p.Id IS NULL OR o.Id IS NULL;
-- Useful for finding orphaned/unmatched records
```

### CROSS JOIN — every row × every row (cartesian product)

```sql
-- "Generate all possible medication + dosage combinations"
SELECT m.Name, d.Amount
FROM Medications m
CROSS JOIN Dosages d;
-- 10 medications × 5 dosages = 50 rows
```

> [!warning] Interview trap
> "What's the difference between `LEFT JOIN` and `LEFT OUTER JOIN`?" — They're identical. `OUTER` is optional syntax. Same for `INNER JOIN` vs just `JOIN`.

---
## 2. Window Functions

Window functions perform calculations across a set of rows **without collapsing them** (unlike GROUP BY). These come up constantly in interviews.
### ROW_NUMBER, RANK, DENSE_RANK

```sql
-- ROW_NUMBER: unique sequential number (no ties)
-- RANK: same rank for ties, skips next (1, 2, 2, 4)
-- DENSE_RANK: same rank for ties, no skip (1, 2, 2, 3)

SELECT
    PharmacistId,
    Name,
    TotalOrders,
    ROW_NUMBER() OVER (ORDER BY TotalOrders DESC) AS RowNum,
    RANK()       OVER (ORDER BY TotalOrders DESC) AS Rnk,
    DENSE_RANK() OVER (ORDER BY TotalOrders DESC) AS DenseRnk
FROM Pharmacists;

-- Results:
-- PharmacistId | TotalOrders | RowNum | Rnk | DenseRnk
-- 5            | 120         | 1      | 1   | 1
-- 3            | 120         | 2      | 1   | 1    ← tied
-- 8            | 95          | 3      | 3   | 2    ← RANK skips to 3, DENSE_RANK goes to 2
```

### PARTITION BY — window within groups

```sql hl:3-4
-- "Rank each pharmacist's orders within their own state"
SELECT Name, State, TotalOrders,
    ROW_NUMBER() OVER (PARTITION BY State        -- restart numbering per state
        ORDER BY TotalOrders DESC) AS RankInState
FROM Pharmacists;

Name           | State | TotalOrders | RankInState
---------------|-------|-------------|------------
Dr. Patel      | TX    | 120         | 1          ← TX group starts at 1
Dr. Nguyen     | TX    | 85          | 2
Dr. Adams      | TX    | 60          | 3
Dr. Kim        | CA    | 110         | 1          ← CA group restarts at 1
Dr. Lopez      | CA    | 90          | 2
Dr. Singh      | NY    | 95          | 1          ← NY group restarts at 1
Dr. Brown      | NY    | 95          | 2          ← same TotalOrders, but ROW_NUMBER forces unique rank

```
### LAG and LEAD — access previous/next rows

```sql
-- "Compare each day's order count to the previous day"
SELECT
    OrderDate,
    OrderCount,
    LAG(OrderCount, 1) OVER (ORDER BY OrderDate) AS PrevDay,
    OrderCount - LAG(OrderCount, 1) OVER (ORDER BY OrderDate) AS DayOverDayChange
FROM DailyOrderSummary;
```
### Running totals with SUM OVER

```sql hl:5
-- "Running total of revenue by date"
SELECT
    OrderDate,
    DailyRevenue,
    SUM(DailyRevenue) OVER (ORDER BY OrderDate) AS RunningTotal,
    AVG(DailyRevenue) OVER (
        ORDER BY OrderDate
        ROWS BETWEEN 6 PRECEDING AND CURRENT ROW
    ) AS SevenDayAvg
FROM DailyRevenue;

OrderDate   | DailyRevenue | RunningTotal
------------|-------------|-------------
2026-04-01  | 1,200       | 1,200        ← just this day
2026-04-02  | 800         | 2,000        ← 1,200 + 800
2026-04-03  | 1,500       | 3,500        ← 1,200 + 800 + 1,500
2026-04-04  | 950         | 4,450        ← all four days summed
2026-04-05  | 0           | 4,450        ← no revenue, total unchanged
2026-04-06  | 2,100       | 6,550        ← keeps accumulating

```

> [!tip] Key insight
> Window functions let you do analytics (rankings, running totals, comparisons) **without** self-joins or subqueries. They're cleaner and usually faster.

---

## 3. Common Table Expressions (CTEs)

==A CTE is a named, temporary result set that exists only for the duration of the query.== Think of it as an inline view.
### Basic CTE

```sql hl:2,10,12
-- "Find patients who have spent more than $1000 total"
WITH PatientSpending AS (
    SELECT
        PatientId,
        SUM(Total) AS TotalSpent,
        COUNT(*) AS OrderCount
    FROM Orders
    WHERE Status = 'Completed'
    GROUP BY PatientId
)
SELECT p.Name, ps.TotalSpent, ps.OrderCount
FROM PatientSpending ps
INNER JOIN Patients p ON ps.PatientId = p.Id
WHERE ps.TotalSpent > 1000
ORDER BY ps.TotalSpent DESC;
```
### Multiple CTEs chained together

```sql
WITH
ActivePatients AS (
    SELECT Id, Name, State
    FROM Patients
    WHERE IsActive = 1
),
PatientOrders AS (
    SELECT
        ap.Id, ap.Name, ap.State,
        COUNT(o.Id) AS OrderCount,
        SUM(o.Total) AS Revenue
    FROM ActivePatients ap
    INNER JOIN Orders o ON ap.Id = o.PatientId
    GROUP BY ap.Id, ap.Name, ap.State
)
SELECT State, COUNT(*) AS Patients, SUM(Revenue) AS StateRevenue
FROM PatientOrders
GROUP BY State
ORDER BY StateRevenue DESC;
```
### Recursive CTE — hierarchical data

```sql
-- "Get the full org chart under a manager"
WITH OrgChart AS (
    -- Anchor: the top-level manager
    SELECT Id, Name, ManagerId, 0 AS Level
    FROM Employees
    WHERE Id = 1

    UNION ALL

    -- Recursive: each person's direct reports
    SELECT e.Id, e.Name, e.ManagerId, oc.Level + 1
    FROM Employees e
    INNER JOIN OrgChart oc ON e.ManagerId = oc.Id
)
SELECT * FROM OrgChart
ORDER BY Level, Name;
```
---
## 4. CTE vs Temp Table vs Table Variable

| Feature         | CTE                       | Temp Table (#)                  | Table Variable (@)           |
| --------------- | ------------------------- | ------------------------------- | ---------------------------- |
| **Scope**       | Single statement only     | Entire session/batch            | Entire batch                 |
| **Indexed**     | No                        | Yes — clustered + non-clustered | Limited (primary key only)   |
| **Statistics**  | No                        | Yes — optimizer uses them       | No — always estimates 1 row  |
| **Best for**    | Readability, one-time use | Large datasets, multiple reuses | Small datasets (< 1000 rows) |
| **Transaction** | N/A                       | Participates in transactions    | Does NOT roll back           |

```sql
-- TEMP TABLE: when you reuse results or need indexes
CREATE TABLE #RecentOrders (
    OrderId INT,
    PatientId INT,
    Total DECIMAL(10,2),
    INDEX IX_Patient (PatientId)  -- can add indexes
);

INSERT INTO #RecentOrders
SELECT Id, PatientId, Total
FROM Orders
WHERE CreatedAt >= DATEADD(DAY, -30, GETDATE());

-- Use it multiple times
SELECT * FROM #RecentOrders WHERE Total > 100;
SELECT PatientId, COUNT(*) FROM #RecentOrders GROUP BY PatientId;

DROP TABLE #RecentOrders;

-- TABLE VARIABLE: small, quick, no stats
DECLARE @StatusCounts TABLE (
    Status VARCHAR(20),
    Cnt INT
);

INSERT INTO @StatusCounts
SELECT Status, COUNT(*) FROM Orders GROUP BY Status;
```

 **Performance trap**
==Table variables don't have statistics, so the optimizer always estimates 1 row. For anything over ~100 rows, a temp table almost always performs better.==

---

## 5. Indexes

### Clustered Index — the table's physical sort order

```sql
-- A table can have ONE clustered index (usually the primary key)
-- The data IS the index — rows are physically stored in this order
CREATE CLUSTERED INDEX IX_Orders_Id ON Orders(Id);

-- This is what happens when you say:
-- PRIMARY KEY (Id) — it creates a clustered index by default
```

### Non-Clustered Index — a separate lookup structure

```sql
-- "We query orders by Status constantly — make it fast"
CREATE NONCLUSTERED INDEX IX_Orders_Status
ON Orders(Status);

-- COVERING INDEX: includes extra columns to avoid going back to the table
CREATE NONCLUSTERED INDEX IX_Orders_Status_Covering
ON Orders(Status)
INCLUDE (PatientId, Total, CreatedAt);
-- Now a query filtering on Status and selecting those columns
-- is answered entirely from the index — no "key lookup" needed
```

### When to add indexes

```sql hl:7,5,6
-- Check what the optimizer wants:
-- Run your query → look at execution plan → green "Missing Index" suggestion

-- Rule of thumb:
-- ✅ Columns in WHERE, JOIN ON, ORDER BY
-- ✅ Foreign keys (PatientId, OrderId)
-- ❌ Columns with low cardinality (bit flags, Status with 3 values on huge table)
-- ❌ Tables with heavy INSERT/UPDATE (indexes slow writes)
```

> [!tip] Interview answer
> "I'd look at the execution plan, check for key lookups and table scans, and add a covering index on the filtered columns with INCLUDEs for the selected columns."

---

## 6. Stored Procedures

```sql hl:4
CREATE OR ALTER PROCEDURE usp_GetOrdersByDateRange
    @StartDate DATE,
    @EndDate DATE,
    @Status VARCHAR(20) = NULL  -- optional parameter
AS
BEGIN
    SET NOCOUNT ON;  -- suppress "X rows affected" messages

    SELECT
        o.Id,
        p.Name AS PatientName,
        o.Total,
        o.Status,
        o.CreatedAt
    FROM Orders o
    INNER JOIN Patients p ON o.PatientId = p.Id
    WHERE o.CreatedAt >= @StartDate
      AND o.CreatedAt < DATEADD(DAY, 1, @EndDate)
      AND (@Status IS NULL OR o.Status = @Status)
    ORDER BY o.CreatedAt DESC;
END;

-- Call it:
EXEC usp_GetOrdersByDateRange '2026-01-01', '2026-03-31', 'Completed';
EXEC usp_GetOrdersByDateRange '2026-01-01', '2026-03-31';  -- all statuses
```

### Output parameters

```sql hl:5,13
CREATE OR ALTER PROCEDURE usp_CreateOrder
    @PatientId INT,
    @MedicationName VARCHAR(200),
    @Quantity INT,
    @NewOrderId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Orders (PatientId, MedicationName, Quantity, Status, CreatedAt)
    VALUES (@PatientId, @MedicationName, @Quantity, 'Pending', GETUTCDATE());

    SET @NewOrderId = SCOPE_IDENTITY();
END;

-- Call it:
DECLARE @Id INT;
EXEC usp_CreateOrder 42, 'Lisinopril', 90, @Id OUTPUT;
SELECT @Id AS NewOrderId;
```

---
## 7. Transactions and Error Handling

```sql hl:7-8,22-30
CREATE OR ALTER PROCEDURE usp_TransferOrder
    @OrderId INT,
    @NewPharmacistId INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        -- Update the order
        UPDATE Orders
        SET PharmacistId = @NewPharmacistId,
            ModifiedAt = GETUTCDATE()
        WHERE Id = @OrderId;
        IF @@ROWCOUNT = 0
            THROW 50001, 'Order not found.', 1;
        -- Log the transfer
        INSERT INTO OrderAuditLog (OrderId, Action, Details, CreatedAt)
        VALUES (@OrderId, 'TRANSFER',
                CONCAT('Transferred to pharmacist ', @NewPharmacistId),
                GETUTCDATE());

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        -- Re-throw the error
        THROW;
    END CATCH;
END;
```

### Isolation Levels

| Level | Dirty Reads | Non-Repeatable Reads | Phantom Reads | Use When |
|---|---|---|---|---|
| READ UNCOMMITTED | Yes | Yes | Yes | Quick-and-dirty reports (rare) |
| READ COMMITTED | No | Yes | Yes | **Default in SQL Server** |
| REPEATABLE READ | No | No | Yes | Need consistent re-reads |
| SERIALIZABLE | No | No | No | Full consistency (slow) |
| SNAPSHOT | No | No | No | Read consistency without blocking |

```sql
-- Set for a single query
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

-- NOLOCK hint (equivalent to READ UNCOMMITTED) — use sparingly
SELECT * FROM Orders WITH (NOLOCK) WHERE Status = 'Pending';
```

> [!warning] NOLOCK trap
> `WITH (NOLOCK)` can read uncommitted data (dirty reads), read rows twice, or skip rows entirely. Only use for approximate counts or dashboards where accuracy isn't critical. Never for financial or medical data.

---

## 8. Query Optimization

### Reading Execution Plans

Key things to look for:
1. **Table Scan** → missing index, add one
2. **Key Lookup** → your index doesn't cover all needed columns, add INCLUDE
3. **Hash Match** → large unsorted join, consider an index on the join column
4. **Sort** → expensive if on large datasets, consider an index with the sort order
5. **Fat arrows** → lots of rows flowing through that step

### Common Performance Fixes

```sql
-- ❌ BAD: Function on column prevents index use
SELECT * FROM Orders WHERE YEAR(CreatedAt) = 2026;

-- ✅ GOOD: Sargable — index can be used
SELECT * FROM Orders
WHERE CreatedAt >= '2026-01-01' AND CreatedAt < '2027-01-01';

-- ❌ BAD: Leading wildcard — full scan
SELECT * FROM Patients WHERE Name LIKE '%smith%';

-- ✅ GOOD: Trailing wildcard — index seek
SELECT * FROM Patients WHERE Name LIKE 'Smith%';

-- ❌ BAD: SELECT * pulls unnecessary columns
SELECT * FROM Orders WHERE Status = 'Pending';

-- ✅ GOOD: Select only what you need
SELECT Id, PatientId, Total FROM Orders WHERE Status = 'Pending';

-- ❌ BAD: OR can prevent index use
SELECT * FROM Orders WHERE PatientId = 5 OR PharmacistId = 10;

-- ✅ GOOD: UNION ALL lets each branch use its own index
SELECT * FROM Orders WHERE PatientId = 5
UNION ALL
SELECT * FROM Orders WHERE PharmacistId = 10 AND PatientId != 5;
```

> [!tip] Sargable
> "SARGable" = **S**earch **ARG**ument **able**. A WHERE clause is sargable if the optimizer can use an index seek. Wrapping a column in a function (`YEAR()`, `ISNULL()`, `CONVERT()`) makes it non-sargable.

---

## 9. MERGE Statement (Upsert)

```sql hl:2-5,10,13
-- "Insert new medications, update existing ones"
MERGE INTO Medications AS target
USING StagingMedications AS source
ON target.NDCCode = source.NDCCode
WHEN MATCHED THEN
    UPDATE SET
        target.Name = source.Name,
        target.Price = source.Price,
        target.ModifiedAt = GETUTCDATE()
WHEN NOT MATCHED BY TARGET THEN
    INSERT (NDCCode, Name, Price, CreatedAt)
    VALUES (source.NDCCode, source.Name, source.Price, GETUTCDATE())
WHEN NOT MATCHED BY SOURCE THEN
    DELETE;
-- Always end MERGE with a semicolon!
```

---

## 10. PIVOT / UNPIVOT

```sql
-- "Show order counts per status as columns"
SELECT PharmacistId, [Pending], [Processing], [Completed], [Cancelled]
FROM (
    SELECT PharmacistId, Status
    FROM Orders
) AS src
PIVOT (
    COUNT(Status)
    FOR Status IN ([Pending], [Processing], [Completed], [Cancelled])
) AS pvt;

-- Result:
-- PharmacistId | Pending | Processing | Completed | Cancelled
-- 1            | 5       | 3          | 42        | 2
-- 2            | 8       | 1          | 37        | 5
```

---

## 11. Useful Functions to Know

```sql hl:3,14,19-23,27
-- String
SELECT CONCAT('Patient: ', Name, ' (', State, ')') FROM Patients;
SELECT STRING_AGG(Name, ', ') FROM Patients WHERE State = 'TX';  -- SQL 2017+
SELECT LEFT(Name, 1) + '***' AS Masked FROM Patients;  -- HIPAA masking

-- Date
SELECT GETUTCDATE();                              -- current UTC time
SELECT DATEADD(DAY, -30, GETUTCDATE());           -- 30 days ago
SELECT DATEDIFF(HOUR, CreatedAt, GETUTCDATE());   -- hours since created
SELECT FORMAT(CreatedAt, 'yyyy-MM-dd');            -- formatted string

-- NULL handling
SELECT ISNULL(MiddleName, '') FROM Patients;       -- replace NULL
SELECT COALESCE(Phone, Email, 'No contact') FROM Patients; -- first non-NULL

-- Conditional
SELECT
    Name,
    CASE
        WHEN Total > 500 THEN 'High'
        WHEN Total > 100 THEN 'Medium'
        ELSE 'Low'
    END AS ValueTier
FROM Orders;

-- IIF (shorthand CASE for SQL 2012+)
SELECT IIF(IsActive = 1, 'Active', 'Inactive') AS Status FROM Patients;
```