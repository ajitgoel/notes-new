Of course. Let's proceed to the second lab, focusing on Columnstore indexes for accelerating analytics in mixed-workload environments.

***

### **Lab 2: Columnstore Indexes for Mixed OLTP/Reporting Workloads**

**Estimated Time:** 60 minutes

**Prerequisites:**

*   **Docker Desktop:** To run SQL Server.
*   **Java 17+ & Maven:** To build and run the Spring Boot application.
*   **SQL Server Management Studio (SSMS) or Azure Data Studio:** To connect to the SQL Server instance.
*   **Hardware:** At least 8GB RAM allocated to Docker. The data generation scripts are intensive.

---

### **Introduction: Why Columnstore?**

<span style="background:#d3f8b6">Traditional databases store data in a **row-oriented** format (row-store), which is efficient for OLTP workloads (e.g., "get me all information for Order #123"). All columns for a single row are stored together.</span>

<span style="background:#d3f8b6">However, analytical queries often only need a few columns from many rows (e.g., "what is the average sales amount for Product #456 across millions of orders?"). In a row-store, the database must read all the data for each row, even the columns it doesn't need, leading to significant I/O.</span>

<span style="background:#d3f8b6">**Columnstore** indexes flip this model. They store data **column by column** in compressed "segments." This has two huge advantages for analytics:</span>

<span style="background:#d3f8b6">1.  **I/O Reduction:** The database only reads the column segments it needs.</span>
1.  **Batch Mode Execution:** Queries can process data in "batches" of ~900 rows at a time, instead of row-by-row, dramatically improving CPU efficiency.

This lab will explore using a **Nonclustered Columnstore Index (NCCI)** to speed up reporting on an OLTP table and compare it with a **Clustered Columnstore Index (CCI)** for pure analytical tables.

---

### **Step 1: Setup and Environment**

We'll reuse the Docker setup from Lab 1.

1.  **Start the Container:**
    If your SQL Server container is not already running, open a terminal in the directory containing your `docker-compose.yml` file and run:
    ```bash
    docker-compose up -d sql-server
    ```

2.  **Create the Database and Table:**
    Connect to your SQL Server instance (`localhost,1433`). Run the following script to create a new database and a traditional OLTP-style table to store inventory transaction data.

    ```sql
    CREATE DATABASE InventoryDB;
    GO

    USE InventoryDB;
    GO

    -- This is a classic row-store OLTP table.
    -- The clustered primary key is perfect for point lookups (e.g., finding a specific transaction).
    CREATE TABLE InventoryTransactions (
        TransactionID BIGINT IDENTITY(1,1) PRIMARY KEY,
        TransactionDate DATETIME2 NOT NULL,
        ProductID INT NOT NULL,
        WarehouseID INT NOT NULL,
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(18, 2) NOT NULL
    );
    GO
    ```

---

### **Step 2: Seed Data Generation**

To see the benefits of Columnstore, we need a significant amount of data. The following script will generate approximately 10.5 million rows. **This will take several minutes to run.**

```sql
-- Generate ~10.5 million rows of sample data.
-- This simulates a busy inventory system over a couple of years.
SET NOCOUNT ON;
PRINT 'Generating data... this will take a few minutes.';

-- Use a recursive CTE to generate a series of dates
DECLARE @StartDate DATETIME2 = '2023-01-01';
DECLARE @EndDate DATETIME2 = '2025-01-01';

;WITH DateGenerator AS (
    SELECT @StartDate AS GenDate
    UNION ALL
    SELECT DATEADD(hh, 1, GenDate)
    FROM DateGenerator
    WHERE GenDate < @EndDate
)
INSERT INTO InventoryTransactions (TransactionDate, ProductID, WarehouseID, Quantity, UnitPrice)
SELECT
    d.GenDate,
    ABS(CHECKSUM(NEWID())) % 5000 + 100, -- 5000 different products
    ABS(CHECKSUM(NEWID())) % 20 + 1,      -- 20 different warehouses
    (ABS(CHECKSUM(NEWID())) % 100) - 50,  -- Inbound and outbound stock
    ABS(CHECKSUM(NEWID())) % 250 + 10.0
FROM DateGenerator d
CROSS APPLY (
    -- Generate a variable number of transactions per hour
    SELECT TOP (ABS(CHECKSUM(NEWID())) % 150 + 50) 1 AS x
) AS TransactionsPerDate
OPTION (MAXRECURSION 0);

PRINT 'Data generation complete.';
```

---

### **Step 3: Baseline - Analytical Query on Row-Store**

Let's run a typical reporting query to find the total quantity of stock for the top 100 products in a specific year and measure its performance.

1.  **Enable statistics and the execution plan:** In your query window, enable "Actual Execution Plan" (Ctrl+M).

2.  **Run the baseline query:**

    ```sql
    SET STATISTICS IO, TIME ON;

    -- Baseline Reporting Query
    SELECT TOP 100
        ProductID,
        SUM(Quantity) AS TotalStock
    FROM
        InventoryTransactions
    WHERE
        TransactionDate >= '2024-01-01' AND TransactionDate < '2025-01-01'
    GROUP BY
        ProductID
    ORDER BY
        TotalStock DESC;

    SET STATISTICS IO, TIME OFF;
    ```

3.  **Analyze the "Before" Results:**
    *   **Messages Tab:** Look at the `logical reads`. This number will be very high (likely in the hundreds of thousands or millions). Note the `CPU time` and `elapsed time`.
    *   **Execution Plan:** Hover over the "Clustered Index Scan" operator. Notice the "Actual Execution Mode" is **Row**. The database had to read every single column of every row from the clustered index to satisfy this query.

---

### **Step 4: Creating a Nonclustered Columnstore Index (NCCI)**

Now, we'll add an NCCI to our OLTP table. This creates a separate, columnar copy of the data, optimized for analytics, without changing the underlying row-store table.

```sql
-- Create a nonclustered columnstore index to accelerate analytics.
-- We include the columns most frequently used in our reporting queries.
CREATE NONCLUSTERED COLUMNSTORE INDEX NCCI_InventoryTransactions_Reporting
ON InventoryTransactions (TransactionDate, ProductID, WarehouseID, Quantity);
```

This command might take a minute as it scans the base table and builds the compressed column segments.

---

### **Step 5: "After" - Performance with NCCI**

Rerun the *exact same* analytical query from Step 3.

```sql
SET STATISTICS IO, TIME ON;

-- Rerun the same query
SELECT TOP 100
    ProductID,
    SUM(Quantity) AS TotalStock
FROM
    InventoryTransactions
WHERE
    TransactionDate >= '2024-01-01' AND TransactionDate < '2025-01-01'
GROUP BY
    ProductID
ORDER BY
    TotalStock DESC;

SET STATISTICS IO, TIME OFF;
```

**Analyze the "After" Results:**

*   **Messages Tab:** Compare the metrics. The `logical reads` will be dramatically lower (often a 90%+ reduction). The `CPU time` and `elapsed time` should also be significantly reduced.
*   **Execution Plan:** The plan will now show a "Columnstore Index Scan." Hover over it. The "Actual Execution Mode" is now **Batch**. This is the key to the performance gain. Notice the "Storage" property is "ColumnStore". The optimizer automatically chose our new, more efficient index.

---

### **Step 6: Simulating a Mixed Workload and the Deltastore**

An NCCI is powerful because it allows OLTP operations (`INSERT`, `UPDATE`, `DELETE`) to continue on the base table. New and recently modified rows aren't immediately compressed into the columnstore; they are first stored in a small, row-oriented B-tree structure called a **deltastore**.

1.  **Add new data:**
    ```sql
    -- Simulate ongoing OLTP activity
    INSERT INTO InventoryTransactions (TransactionDate, ProductID, WarehouseID, Quantity, UnitPrice)
    VALUES (GETDATE(), 101, 5, 50, 199.99);
    ```

2.  **Inspect the Columnstore Segments:**
    This DMV shows us the physical structure of our columnstore index.

    ```sql
    SELECT
        object_name(object_id) as TableName,
        index_id,
        partition_number,
        row_group_id,
        state_description,
        total_rows
    FROM sys.dm_db_column_store_row_group_physical_stats
    WHERE object_id = OBJECT_ID('InventoryTransactions');
    ```
    You will see many `COMPRESSED` rowgroups (with up to ~1 million rows each) and one `OPEN` rowgroup. This "OPEN" rowgroup is the deltastore, holding our newly inserted row.

3.  **The Tuple-Mover:**
    A background process called the "tuple-mover" periodically closes the deltastore when it reaches about 1 million rows and compresses its contents into a new segment. We can force this process for maintenance.

    ```sql
    -- Manually trigger the compression of the deltastore
    ALTER INDEX NCCI_InventoryTransactions_Reporting ON InventoryTransactions REORGANIZE;
    ```
4.  **Verify Compression:**
    Rerun the DMV query from step 2. The `OPEN` rowgroup is now gone, and a new `COMPRESSED` rowgroup with a small number of rows has been created.

---

### **Step 7: Java/Spring Boot Integration**

Let's show how easy this is to consume from an application. From the Java code's perspective, nothing changes except for the improved response time.

1.  **Setup a Spring Boot Project:**
    Use [start.spring.io](https://start.spring.io) to generate a project with these dependencies:
    *   Spring Web
    *   Spring Data JDBC
    *   SQL Server Driver

2.  **Configure `application.properties`:**

```properties 
spring.datasource.url=jdbc:sqlserver://localhost:1433;databaseName=InventoryDB;encrypt=false;
    spring.datasource.username=sa
    spring.datasource.password=yourStrong(!)Password
```
    
3.  **Create a simple service to run the query:**
    ```java
    // ReportingService.java
    import org.springframework.jdbc.core.JdbcTemplate;
    import org.springframework.stereotype.Service;
    import java.util.List;
    import java.util.Map;

    @Service
    public class ReportingService {

        private final JdbcTemplate jdbcTemplate;

        public ReportingService(JdbcTemplate jdbcTemplate) {
            this.jdbcTemplate = jdbcTemplate;
        }

        public void runAnalyticalQuery() {
            String sql = """
                SELECT TOP 100
                    ProductID,
                    SUM(Quantity) AS TotalStock
                FROM
                    InventoryTransactions
                WHERE
                    TransactionDate >= '2024-01-01' AND TransactionDate < '2025-01-01'
                GROUP BY
                    ProductID
                ORDER BY
                    TotalStock DESC
            """;

            System.out.println("Executing analytical query from Java...");
            long startTime = System.currentTimeMillis();

            List<Map<String, Object>> results = jdbcTemplate.queryForList(sql);

            long endTime = System.currentTimeMillis();
            System.out.println("Query finished in " + (endTime - startTime) + " ms.");
            results.forEach(row -> System.out.println("ProductID: " + row.get("ProductID") + ", Stock: " + row.get("TotalStock")));
        }
    }

    // A runner to trigger the service
    import org.springframework.boot.CommandLineRunner;
    import org.springframework.stereotype.Component;

    @Component
    public class QueryRunner implements CommandLineRunner {
        private final ReportingService reportingService;

        public QueryRunner(ReportingService reportingService) {
            this.reportingService = reportingService;
        }

        @Override
        public void run(String... args) {
            reportingService.runAnalyticalQuery();
        }
    }
    ```

4.  **Run the application:** `mvn spring-boot:run`. The console will print the query execution time, which benefits directly from the NCCI in the database.

---

### **Step 8: Common Pitfalls and Troubleshooting**

*   **Columnstore Fragmentation:**
    *   **Problem:** If you frequently run `REORGANIZE` on deltastores with very few rows, or if there's memory pressure during compression, you can end up with many small, compressed rowgroups. This is inefficient and hurts performance. A healthy rowgroup should be close to the max size (~1M rows).
    *   **Diagnosis:** Use the `sys.dm_db_column_store_row_group_physical_stats` DMV and look for compressed rowgroups with a low `total_rows` count.
    *   **Fix:** <span style="background:#d3f8b6">`ALTER INDEX ... REBUILD` will completely rebuild the index, merging small rowgroups into larger, more efficient ones.</span>

*   **Choosing the Right Columns:**
    *   **Problem:** An NCCI that includes wide columns (`VARCHAR(MAX)`, `NVARCHAR(MAX)`) or columns not used in analytical queries bloats the index size and provides no benefit.
    *   **Fix:** <span style="background:#d3f8b6">Only include columns that are frequently used in aggregations, filtering, and grouping in your analytical queries.</span>

*   **When NOT to use an NCCI:**
    *   <span style="background:#d3f8b6">An NCCI adds overhead to every `INSERT`, `UPDATE`, and `DELETE`. On extremely high-throughput OLTP tables where every microsecond matters, this overhead may not be acceptable.</span> In such cases, offloading data to a separate reporting database (using Kafka, CDC, etc.) is a better pattern.

---

### **Step 9: Knowledge Check**

1.  What is the primary benefit of "Batch Mode" execution?
    a) It processes data row-by-row.
    <span style="background:#d3f8b6">b) It processes data in chunks of ~900 rows, which is more CPU-efficient.</span>
    c) It only works on in-memory tables.
    d) It makes `INSERT` statements faster.
2.  In a nonclustered columnstore index, where are newly inserted or updated rows stored initially?
    a) In a compressed segment.
    b) Directly in the clustered index.
    c) In the transaction log only.
    <span style="background:#d3f8b6">d) In a row-store structure called a deltastore.</span>
3.  Which command is used to manually force the compression of a deltastore?
    a) `ALTER INDEX ... REBUILD`
    <span style="background:#d3f8b6">b) `ALTER INDEX ... REORGANIZE`</span>
    c) `UPDATE STATISTICS`
    d) `CREATE STATISTICS`
4.  You have a large data warehouse table used only for reporting. It is never updated. What is the most appropriate index type?
    a) A nonclustered row-store index.
    b) A nonclustered columnstore index (NCCI).
    <span style="background:#d3f8b6">c) A clustered columnstore index (CCI).</span>
    d) A heap (no clustered index).
5.  What is "segment elimination"?
    <span style="background:#d3f8b6">a) The query optimizer skipping the read of column segments for columns not needed by the query.</span>
    b) The deletion of old rowgroups.
    c) The process of the tuple-mover cleaning the deltastore.
    d) The query optimizer ignoring an index.

*(Answers: 1-b, 2-d, 3-b, 4-c, 5-a)*

---

### **Step 10: Teardown**

1.  **Drop the Database:**
    Connect to SQL Server and run:
    ```sql
    USE master;
    GO
    DROP DATABASE InventoryDB;
    GO
    ```

2.  **Stop the Container:**
    From your terminal, run:
    ```bash
    docker-compose down
    ```