## Setup — Create the Practice Tables

Run this first to create the schema and sample data.

```sql
-- Create tables
CREATE TABLE Patients (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name VARCHAR(100) NOT NULL,
    Email VARCHAR(200),
    State VARCHAR(2) NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE Pharmacists (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name VARCHAR(100) NOT NULL,
    LicenseState VARCHAR(2) NOT NULL
);

CREATE TABLE Medications (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name VARCHAR(200) NOT NULL,
    NDCCode VARCHAR(20) UNIQUE,
    UnitPrice DECIMAL(10,2) NOT NULL
);

CREATE TABLE Orders (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PatientId INT REFERENCES Patients(Id),
    PharmacistId INT REFERENCES Pharmacists(Id),
    Status VARCHAR(20) NOT NULL DEFAULT 'Pending',
    Total DECIMAL(10,2),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CompletedAt DATETIME2 NULL
);

CREATE TABLE OrderItems (
    Id INT PRIMARY KEY IDENTITY(1,1),
    OrderId INT REFERENCES Orders(Id),
    MedicationId INT REFERENCES Medications(Id),
    Quantity INT NOT NULL,
    LineTotal DECIMAL(10,2) NOT NULL
);

-- Insert sample data
INSERT INTO Patients (Name, Email, State) VALUES
('Alice Johnson', 'alice@email.com', 'TX'),
('Bob Smith', 'bob@email.com', 'TX'),
('Carol Davis', 'carol@email.com', 'CA'),
('Dave Wilson', 'dave@email.com', 'CA'),
('Eve Martinez', 'eve@email.com', 'NY'),
('Frank Lee', NULL, 'TX'),
('Grace Chen', 'grace@email.com', 'FL'),
('Henry Brown', 'henry@email.com', 'FL');

INSERT INTO Pharmacists (Name, LicenseState) VALUES
('Dr. Patel', 'TX'), ('Dr. Kim', 'CA'), ('Dr. Nguyen', 'TX');

INSERT INTO Medications (Name, NDCCode, UnitPrice) VALUES
('Lisinopril 10mg', 'NDC-001', 12.50),
('Metformin 500mg', 'NDC-002', 8.75),
('Testosterone Cypionate', 'NDC-003', 85.00),
('Sertraline 50mg', 'NDC-004', 15.00),
('Custom Compound A', 'NDC-005', 120.00);

INSERT INTO Orders (PatientId, PharmacistId, Status, Total, CreatedAt, CompletedAt) VALUES
(1, 1, 'Completed', 125.00, DATEADD(DAY,-45,GETUTCDATE()), DATEADD(DAY,-44,GETUTCDATE())),
(1, 1, 'Completed', 250.00, DATEADD(DAY,-30,GETUTCDATE()), DATEADD(DAY,-29,GETUTCDATE())),
(2, 1, 'Completed', 85.00,  DATEADD(DAY,-20,GETUTCDATE()), DATEADD(DAY,-19,GETUTCDATE())),
(2, 1, 'Pending',   170.00, DATEADD(DAY,-2,GETUTCDATE()),  NULL),
(3, 2, 'Completed', 340.00, DATEADD(DAY,-15,GETUTCDATE()), DATEADD(DAY,-14,GETUTCDATE())),
(3, 2, 'Processing',120.00, DATEADD(DAY,-1,GETUTCDATE()),  NULL),
(4, 2, 'Completed', 95.00,  DATEADD(DAY,-10,GETUTCDATE()), DATEADD(DAY,-9,GETUTCDATE())),
(5, 3, 'Completed', 510.00, DATEADD(DAY,-25,GETUTCDATE()), DATEADD(DAY,-24,GETUTCDATE())),
(5, 3, 'Cancelled', 200.00, DATEADD(DAY,-5,GETUTCDATE()),  NULL),
(6, 1, 'Pending',   75.00,  DATEADD(DAY,-3,GETUTCDATE()),  NULL),
(7, 3, 'Completed', 240.00, DATEADD(DAY,-8,GETUTCDATE()),  DATEADD(DAY,-7,GETUTCDATE())),
(8, 3, 'Completed', 180.00, DATEADD(DAY,-12,GETUTCDATE()), DATEADD(DAY,-11,GETUTCDATE()));

INSERT INTO OrderItems (OrderId, MedicationId, Quantity, LineTotal) VALUES
(1, 1, 10, 125.00),
(2, 3, 2, 170.00), (2, 4, 4, 60.00), (2, 1, 2, 20.00),
(3, 2, 10, 85.00),
(4, 3, 2, 170.00),
(5, 5, 2, 240.00), (5, 3, 1, 85.00), (5, 1, 1, 15.00),
(6, 5, 1, 120.00),
(7, 2, 5, 43.75), (7, 4, 3, 45.00), (7, 1, 1, 6.25),
(8, 3, 6, 510.00),
(9, 3, 2, 170.00), (9, 1, 2, 30.00),
(10, 2, 5, 43.75), (10, 1, 3, 31.25),
(11, 5, 2, 240.00),
(12, 3, 2, 170.00), (12, 1, 1, 10.00);
```

---

## Exercise 1: JOINs and Filtering
**Difficulty:** ⭐⭐ | **Time:** 10 min

### Problem

Write queries for each:

1. **All patients and their order count** — include patients with zero orders (show 0, not NULL)
2. **Patients who have NEVER placed an order**
3. **Each pharmacist and their total completed revenue** — only show pharmacists with revenue > $200

---

> [!success]- Solution (click to expand)
>
> ```sql
> -- 1. All patients with order count (including zero)
> SELECT
>     p.Name,
>     COUNT(o.Id) AS OrderCount
> FROM Patients p
> LEFT JOIN Orders o ON p.Id = o.PatientId
> GROUP BY p.Name
> ORDER BY OrderCount DESC;
>
> -- 2. Patients who never ordered
> SELECT p.Name, p.Email, p.State
> FROM Patients p
> LEFT JOIN Orders o ON p.Id = o.PatientId
> WHERE o.Id IS NULL;
>
> -- 3. Pharmacist completed revenue > $200
> SELECT
>     ph.Name,
>     COUNT(o.Id) AS CompletedOrders,
>     SUM(o.Total) AS TotalRevenue
> FROM Pharmacists ph
> INNER JOIN Orders o ON ph.Id = o.PharmacistId
> WHERE o.Status = 'Completed'
> GROUP BY ph.Name
> HAVING SUM(o.Total) > 200
> ORDER BY TotalRevenue DESC;
> ```

---

## Exercise 2: Window Functions
**Difficulty:** ⭐⭐⭐ | **Time:** 15 min

### Problem

1. **Rank patients by total spending** — use DENSE_RANK, show their name, total spent, and rank
2. **For each patient, show their orders with a running total** — ordered by date
3. **For each order, show the previous order's total for the same patient** using LAG

---

> [!success]- Solution (click to expand)
>
> ```sql
> -- 1. Rank patients by spending
> SELECT
>     p.Name,
>     SUM(o.Total) AS TotalSpent,
>     DENSE_RANK() OVER (ORDER BY SUM(o.Total) DESC) AS SpendingRank
> FROM Patients p
> INNER JOIN Orders o ON p.Id = o.PatientId
> WHERE o.Status = 'Completed'
> GROUP BY p.Name;
>
> -- 2. Running total per patient
> SELECT
>     p.Name,
>     o.CreatedAt,
>     o.Total,
>     SUM(o.Total) OVER (
>         PARTITION BY p.Id
>         ORDER BY o.CreatedAt
>     ) AS RunningTotal
> FROM Orders o
> INNER JOIN Patients p ON o.PatientId = p.Id
> WHERE o.Status = 'Completed'
> ORDER BY p.Name, o.CreatedAt;
>
> -- 3. Previous order total per patient
> SELECT
>     p.Name,
>     o.CreatedAt,
>     o.Total AS CurrentTotal,
>     LAG(o.Total, 1) OVER (
>         PARTITION BY p.Id
>         ORDER BY o.CreatedAt
>     ) AS PreviousOrderTotal,
>     o.Total - LAG(o.Total, 1) OVER (
>         PARTITION BY p.Id
>         ORDER BY o.CreatedAt
>     ) AS ChangeFromPrevious
> FROM Orders o
> INNER JOIN Patients p ON o.PatientId = p.Id
> ORDER BY p.Name, o.CreatedAt;
> ```

---

## Exercise 3: CTEs and Aggregation
**Difficulty:** ⭐⭐⭐ | **Time:** 15 min

### Problem

Using CTEs, write a query that produces a **pharmacy dashboard summary**:

1. CTE 1: Calculate each state's total completed revenue and patient count
2. CTE 2: Find each state's top-spending patient
3. Final query: Join them together — show State, Revenue, PatientCount, and TopPatientName

---

> [!success]- Solution (click to expand)
>
> ```sql
> WITH StateRevenue AS (
>     SELECT
>         p.State,
>         COUNT(DISTINCT p.Id) AS PatientCount,
>         SUM(o.Total) AS TotalRevenue
>     FROM Patients p
>     INNER JOIN Orders o ON p.Id = o.PatientId
>     WHERE o.Status = 'Completed'
>     GROUP BY p.State
> ),
> TopPatients AS (
>     SELECT
>         p.State,
>         p.Name AS TopPatientName,
>         SUM(o.Total) AS PatientSpending,
>         ROW_NUMBER() OVER (
>             PARTITION BY p.State
>             ORDER BY SUM(o.Total) DESC
>         ) AS Rnk
>     FROM Patients p
>     INNER JOIN Orders o ON p.Id = o.PatientId
>     WHERE o.Status = 'Completed'
>     GROUP BY p.State, p.Name
> )
> SELECT
>     sr.State,
>     sr.TotalRevenue,
>     sr.PatientCount,
>     tp.TopPatientName,
>     tp.PatientSpending AS TopPatientRevenue
> FROM StateRevenue sr
> INNER JOIN TopPatients tp
>     ON sr.State = tp.State AND tp.Rnk = 1
> ORDER BY sr.TotalRevenue DESC;
> ```

---

## Exercise 4: Stored Procedure with Error Handling
**Difficulty:** ⭐⭐⭐⭐ | **Time:** 20 min

### Problem

Write a stored procedure `usp_FulfillOrder` that:

1. Accepts `@OrderId INT`
2. Validates the order exists and is in 'Pending' or 'Processing' status
3. Updates the order's Status to 'Completed' and sets CompletedAt to UTC now
4. Inserts an audit log record into an `OrderAuditLog` table
5. Wraps everything in a transaction with TRY/CATCH
6. Throws a meaningful error if the order doesn't exist or has wrong status

---

> [!success]- Solution (click to expand)
>
> ```sql
> -- Create audit table first
> CREATE TABLE OrderAuditLog (
>     Id INT PRIMARY KEY IDENTITY(1,1),
>     OrderId INT NOT NULL,
>     Action VARCHAR(50) NOT NULL,
>     OldStatus VARCHAR(20),
>     NewStatus VARCHAR(20),
>     CreatedAt DATETIME2 DEFAULT GETUTCDATE()
> );
> GO
>
> CREATE OR ALTER PROCEDURE usp_FulfillOrder
>     @OrderId INT
> AS
> BEGIN
>     SET NOCOUNT ON;
>
>     DECLARE @CurrentStatus VARCHAR(20);
>
>     BEGIN TRY
>         BEGIN TRANSACTION;
>
>         -- Get current status (with lock to prevent race conditions)
>         SELECT @CurrentStatus = Status
>         FROM Orders WITH (UPDLOCK, ROWLOCK)
>         WHERE Id = @OrderId;
>
>         -- Validate: order must exist
>         IF @CurrentStatus IS NULL
>             THROW 50001, 'Order not found.', 1;
>
>         -- Validate: must be in fulfillable status
>         IF @CurrentStatus NOT IN ('Pending', 'Processing')
>         BEGIN
>             DECLARE @Msg VARCHAR(200) = CONCAT(
>                 'Order ', @OrderId,
>                 ' cannot be fulfilled. Current status: ',
>                 @CurrentStatus);
>             THROW 50002, @Msg, 1;
>         END;
>
>         -- Update the order
>         UPDATE Orders
>         SET Status = 'Completed',
>             CompletedAt = GETUTCDATE()
>         WHERE Id = @OrderId;
>
>         -- Audit log
>         INSERT INTO OrderAuditLog
>             (OrderId, Action, OldStatus, NewStatus)
>         VALUES
>             (@OrderId, 'FULFILL', @CurrentStatus, 'Completed');
>
>         COMMIT TRANSACTION;
>
>         -- Return the updated order
>         SELECT Id, PatientId, Status, Total, CompletedAt
>         FROM Orders
>         WHERE Id = @OrderId;
>     END TRY
>     BEGIN CATCH
>         IF @@TRANCOUNT > 0
>             ROLLBACK TRANSACTION;
>         THROW;
>     END CATCH;
> END;
> GO
>
> -- Test it:
> EXEC usp_FulfillOrder @OrderId = 4;   -- should succeed (Pending)
> EXEC usp_FulfillOrder @OrderId = 1;   -- should fail (already Completed)
> EXEC usp_FulfillOrder @OrderId = 999; -- should fail (not found)
> ```

---

## Exercise 5: Query Optimization
**Difficulty:** ⭐⭐⭐⭐ | **Time:** 15 min

### Problem

Each query below has a performance problem. Identify the issue and rewrite.

```sql
-- Query A: Find orders from 2026
SELECT * FROM Orders WHERE YEAR(CreatedAt) = 2026;

-- Query B: Find patients by name (case-insensitive)
SELECT * FROM Patients WHERE UPPER(Name) = 'ALICE JOHNSON';

-- Query C: Find orders with patient info for a specific state
SELECT *
FROM Orders o, Patients p
WHERE o.PatientId = p.Id
  AND p.State = 'TX';

-- Query D: Check if any pending orders exist
SELECT COUNT(*) FROM Orders WHERE Status = 'Pending';
-- (used in an IF: IF count > 0 ...)

-- Query E: Get the most recent order per patient
SELECT *
FROM Orders o
WHERE CreatedAt = (
    SELECT MAX(CreatedAt) FROM Orders WHERE PatientId = o.PatientId
);
```

---

> [!success]- Solution (click to expand)
>
> ```sql
> -- Query A: Non-sargable — YEAR() prevents index use
> -- FIX: Use date range
> SELECT Id, PatientId, Total, Status, CreatedAt
> FROM Orders
> WHERE CreatedAt >= '2026-01-01' AND CreatedAt < '2027-01-01';
>
> -- Query B: Non-sargable — UPPER() prevents index use
> -- FIX: SQL Server uses case-insensitive collation by default
> SELECT Id, Name, Email, State
> FROM Patients
> WHERE Name = 'Alice Johnson';
> -- If you truly need case-insensitive on case-sensitive collation:
> -- WHERE Name COLLATE Latin1_General_CI_AS = 'alice johnson'
>
> -- Query C: Old-style join + SELECT *
> -- FIX: Explicit JOIN, specify columns
> SELECT o.Id, o.Total, o.Status, p.Name, p.Email
> FROM Orders o
> INNER JOIN Patients p ON o.PatientId = p.Id
> WHERE p.State = 'TX';
>
> -- Query D: COUNT(*) scans all matching rows just to check existence
> -- FIX: Use EXISTS — stops at first match
> IF EXISTS (SELECT 1 FROM Orders WHERE Status = 'Pending')
>     PRINT 'There are pending orders';
>
> -- Query E: Correlated subquery runs once per row
> -- FIX: Use ROW_NUMBER window function
> ;WITH Ranked AS (
>     SELECT *,
>         ROW_NUMBER() OVER (
>             PARTITION BY PatientId
>             ORDER BY CreatedAt DESC
>         ) AS Rn
>     FROM Orders
> )
> SELECT Id, PatientId, Total, Status, CreatedAt
> FROM Ranked
> WHERE Rn = 1;
> ```

---

## Exercise 6: PIVOT and Reporting
**Difficulty:** ⭐⭐⭐ | **Time:** 10 min

### Problem

Write a query that produces this output — a cross-tab of pharmacists vs. order statuses showing the count of each:

```
PharmacistName | Pending | Processing | Completed | Cancelled
Dr. Patel      | 2       | 0          | 3         | 0
Dr. Kim        | 0       | 1          | 2         | 0
Dr. Nguyen     | 0       | 0          | 2         | 1
```

---

> [!success]- Solution (click to expand)
>
> ```sql
> SELECT
>     ph.Name AS PharmacistName,
>     ISNULL([Pending], 0) AS Pending,
>     ISNULL([Processing], 0) AS Processing,
>     ISNULL([Completed], 0) AS Completed,
>     ISNULL([Cancelled], 0) AS Cancelled
> FROM (
>     SELECT ph.Name, o.Status
>     FROM Orders o
>     INNER JOIN Pharmacists ph ON o.PharmacistId = ph.Id
> ) AS src
> PIVOT (
>     COUNT(Status)
>     FOR Status IN ([Pending], [Processing], [Completed], [Cancelled])
> ) AS pvt
> ORDER BY PharmacistName;
>
> -- Alternative without PIVOT (more portable):
> SELECT
>     ph.Name AS PharmacistName,
>     SUM(CASE WHEN o.Status = 'Pending' THEN 1 ELSE 0 END) AS Pending,
>     SUM(CASE WHEN o.Status = 'Processing' THEN 1 ELSE 0 END) AS Processing,
>     SUM(CASE WHEN o.Status = 'Completed' THEN 1 ELSE 0 END) AS Completed,
>     SUM(CASE WHEN o.Status = 'Cancelled' THEN 1 ELSE 0 END) AS Cancelled
> FROM Orders o
> INNER JOIN Pharmacists ph ON o.PharmacistId = ph.Id
> GROUP BY ph.Name
> ORDER BY ph.Name;
> ```

---

## Recommended Practice Order

1. **Exercise 1** (JOINs) — warm up with the fundamentals
2. **Exercise 2** (Window Functions) — these impress interviewers
3. **Exercise 5** (Optimization) — **most likely to come up in a senior interview**
4. **Exercise 3** (CTEs) — shows you can structure complex queries cleanly
5. **Exercise 4** (Stored Procedure) — full production pattern with error handling
6. **Exercise 6** (PIVOT) — bonus points

> [!tip] During the Interview
> When asked to write SQL, always clarify: "Do you want just the result set, or should I handle edge cases like NULLs?" This shows you think about data quality — critical in healthcare.