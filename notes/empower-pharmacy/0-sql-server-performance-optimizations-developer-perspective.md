###### **1. Indexing Strategy** 
**Create indexes on columns used in WHERE, JOIN, and ORDER BY:**
```sql
-- Covering index: includes all columns the query needs
CREATE NONCLUSTERED INDEX IX_Orders_CustomerDate
ON Orders (CustomerID, OrderDate)
INCLUDE (TotalAmount, Status);

-- Query now satisfied entirely from the index (no key lookup)
SELECT OrderDate, TotalAmount, Status
FROM Orders
WHERE CustomerID = 42
ORDER BY OrderDate DESC;
```

**Find missing indexes the engine is asking for:**
```sql
SELECT
    d.statement AS TableName,
    d.equality_columns,
    d.inequality_columns,
    d.included_columns,
    s.avg_user_impact AS [Avg % Improvement],
    s.user_seeks
FROM sys.dm_db_missing_index_details d
JOIN sys.dm_db_missing_index_groups g ON d.index_handle = g.index_handle
JOIN sys.dm_db_missing_index_group_stats s ON g.index_group_handle = s.group_handle
ORDER BY s.avg_user_impact * s.user_seeks DESC;
```
###### **2. Avoid SELECT *** 
```sql
-- Bad: fetches all columns, prevents covering index usage
SELECT * FROM Orders WHERE CustomerID = 42;

-- Good: fetch only what you need
SELECT OrderID, OrderDate, TotalAmount
FROM Orders
WHERE CustomerID = 42;
```
###### **3. Sargable WHERE Clauses** 
Non-sargable queries wrap a column in a function, killing index usage.
```sql
-- Bad: function on column — full scan
SELECT * FROM Orders
WHERE YEAR(OrderDate) = 2026;

-- Good: range predicate — index seek
SELECT * FROM Orders
WHERE OrderDate >= '2026-01-01' AND OrderDate < '2027-01-01';

-- Bad: leading wildcard
SELECT * FROM Customers WHERE LastName LIKE '%son';

-- Good: trailing wildcard uses index
SELECT * FROM Customers WHERE LastName LIKE 'John%';
```

  ###### **4. Parameterized Queries — Avoid SQL Injection & Plan Cache Bloat** 
```csharp
// Bad: string concatenation — injection risk + unique plan per value
var sql = $"SELECT * FROM Users WHERE Email = '{email}'";

// Good: parameterized — safe + plan reuse
using var cmd = new SqlCommand(
    "SELECT UserID, Name FROM Users WHERE Email = @Email", conn);
cmd.Parameters.AddWithValue("@Email", email);
```
###### **5. Pagination Done Right** 
```sql hl:9,6,1,4
-- Bad: OFFSET scales poorly on large tables
SELECT * FROM Products
ORDER BY ProductID
OFFSET 100000 ROWS FETCH NEXT 20 ROWS ONLY;

-- Good: keyset pagination — constant performance
SELECT TOP 20 *
FROM Products
WHERE ProductID > @LastSeenID
ORDER BY ProductID;
```
###### **6. ==Batch Large DML Operations**== 
```sql hl:1,3
-- Bad: single massive delete — locks the entire table, fills the log
DELETE FROM AuditLog WHERE CreatedDate < '2024-01-01';
-- Good: delete in batches
WHILE 1 = 1
BEGIN
    DELETE TOP (5000) FROM AuditLog
    WHERE CreatedDate < '2024-01-01';
    IF @@ROWCOUNT = 0 BREAK;
END
```

  ###### **7. ==Use EXISTS Instead of COUNT for Existence Checks**== 
```sql hl:5
-- Bad: counts every matching row
IF (SELECT COUNT(*) FROM Orders WHERE CustomerID = 42) > 0
    PRINT 'Has orders';
-- Good: stops at first match
IF EXISTS (SELECT 1 FROM Orders WHERE CustomerID = 42)
    PRINT 'Has orders';
```
###### **8. Temp Tables vs Table Variables vs CTEs** 
```sql hl:1,11,14
-- Temp tables: best for large intermediate result sets (stats + indexes)
CREATE TABLE #ActiveCustomers (CustomerID INT PRIMARY KEY);
INSERT INTO #ActiveCustomers
SELECT CustomerID FROM Customers WHERE IsActive = 1;

-- Use it in joins
SELECT o.OrderID, o.TotalAmount
FROM Orders o
JOIN #ActiveCustomers ac ON o.CustomerID = ac.CustomerID;

-- Table variables: fine for small sets (<1000 rows)
DECLARE @IDs TABLE (ID INT);

-- CTEs: readable, but re-evaluated if referenced multiple times
;WITH RecentOrders AS (
    SELECT *, ROW_NUMBER() OVER (PARTITION BY CustomerID ORDER BY OrderDate DESC) rn
    FROM Orders
)
SELECT * FROM RecentOrders WHERE rn = 1;
```
###### **9. Avoid Implicit Conversions** 
```sql hl:1,4,6
-- Bad: PhoneNumber is VARCHAR but parameter is NVARCHAR — scans entire index
SELECT * FROM Customers WHERE PhoneNumber = N'5551234567';

-- Good: match the data type
SELECT * FROM Customers WHERE PhoneNumber = '5551234567';
Check for implicit conversions in your execution plans — look for `CONVERT_IMPLICIT`
```

###### **10. Efficient JOIN Patterns** 
```sql hl:1,6
-- Bad: correlated subquery runs per row
SELECT c.Name,
       (SELECT COUNT(*) FROM Orders WHERE CustomerID = c.CustomerID) AS OrderCount
FROM Customers c;

-- Good: single pass with JOIN + GROUP BY
SELECT c.Name, COUNT(o.OrderID) AS OrderCount
FROM Customers c
LEFT JOIN Orders o ON c.CustomerID = o.CustomerID
GROUP BY c.Name;
```
###### **11. ==Use SET NOCOUNT ON in Stored Procedures**== 

```sql hl:4
CREATE PROCEDURE usp_GetOrders @CustomerID INT
AS
BEGIN
    SET NOCOUNT ON; -- eliminates "X rows affected" messages, reduces network traffic
    SELECT OrderID, OrderDate, TotalAmount
    FROM Orders
    WHERE CustomerID = @CustomerID
    ORDER BY OrderDate DESC;
END
```

  ###### **12. Query Store — Find Regressions** 

```sql
-- Enable Query Store
ALTER DATABASE MyDB SET QUERY_STORE = ON;

-- Find top resource-consuming queries
SELECT TOP 10
    qt.query_sql_text,
    rs.avg_duration / 1000.0 AS avg_ms,
    rs.avg_logical_io_reads,
    rs.count_executions
FROM sys.query_store_query_text qt
JOIN sys.query_store_query q ON qt.query_text_id = q.query_text_id
JOIN sys.query_store_plan p ON q.query_id = p.query_id
JOIN sys.query_store_runtime_stats rs ON p.plan_id = rs.plan_id
ORDER BY rs.avg_duration * rs.count_executions DESC;
```
###### **13. ==Transaction Isolation — Read Uncommitted for Reports**== 

```sql
-- For reporting queries where dirty reads are acceptable
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
-- or
SELECT SUM(TotalAmount) FROM Orders WITH (NOLOCK)
WHERE OrderDate >= '2026-01-01';

-- Better long-term: enable RCSI at database level
ALTER DATABASE MyDB SET READ_COMMITTED_SNAPSHOT ON;
-- Now readers never block writers, no NOLOCK hints needed
```
###### **14. ==Avoid Cursors — Use Set-Based Logic**== 

```sql hl:1,11
-- Bad: row-by-row cursor
DECLARE cur CURSOR FOR SELECT OrderID, Amount FROM Orders;
OPEN cur;
FETCH NEXT FROM cur INTO @id, @amt;
WHILE @@FETCH_STATUS = 0
BEGIN
    UPDATE Orders SET Tax = @amt * 0.08 WHERE OrderID = @id;
    FETCH NEXT FROM cur INTO @id, @amt;
END

-- Good: single set-based statement
UPDATE Orders SET Tax = Amount * 0.08;
```
###### **15. Execution Plan Red Flags to Watch For** 

| Warning in Plan                           | What to Do                                          |
| ----------------------------------------- | --------------------------------------------------- |
| **Table Scan** / **Clustered Index Scan** | Add or fix an index                                 |
| **Key Lookup**                            | Add missing columns to INCLUDE                      |
| **Sort** (with high cost)                 | Add index matching ORDER BY                         |
| **CONVERT_IMPLICIT**                      | Fix data type mismatches                            |
| **Thick arrows** between operators        | Check estimated vs actual rows — stats may be stale |
| **Hash Match** on small tables            | May indicate missing index on join column           |
**Update statistics when estimates are off:**

```sql
UPDATE STATISTICS Orders WITH FULLSCAN;
```

**Priority Checklist** 

| Impact      | Optimization                      |
| ----------- | --------------------------------- |
| **Highest** | Proper indexes + covering indexes |
| **High**    | Sargable WHERE clauses            |
| **High**    | Parameterized queries             |
| **High**    | Set-based over cursors            |
| **Medium**  | Batch large DML                   |
| **Medium**  | Keyset pagination                 |
| **Medium**  | EXISTS over COUNT                 |
| **Medium**  | Read Committed Snapshot Isolation |
| **Lower**   | SET NOCOUNT ON, avoid SELECT *    |
Start with the execution plan for your slowest queries — the plan tells you exactly where time is spent. Fix those first before applying broad rules.