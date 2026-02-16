**Estimated Time:** 45 minutes

**Prerequisites:**

*   **Docker Desktop & SQL Server:** The same SQL Server 2022 Developer Edition container works perfectly.
*   **SSMS or Azure Data Studio.**
*   **Hardware:** At least 8GB RAM allocated to Docker. In-Memory OLTP consumes more memory than traditional tables.

---

### **Introduction: Why In-Memory OLTP?**

For certain high-throughput OLTP workloads, traditional disk-based tables can become a bottleneck due to **latch contention** and **logging**.

*   **Latch Contention:** When many concurrent threads try to access the same data page in memory (like the last page of an index during rapid inserts), they have to wait on lightweight synchronization objects called latches. This can severely limit scalability.
*   **Logging:** Writing every transaction to the transaction log on disk creates I/O overhead.

**In-Memory OLTP** (codename "Hekaton") solves this by using:

1.  **Memory-Optimized Tables:** Data lives entirely in RAM. The storage structures are completely lock- and latch-free, using optimistic, multi-version concurrency control.
2.  **Natively Compiled Stored Procedures:** T-SQL logic is compiled down to native machine code (`.dll` files in the SQL Server data directory), eliminating the overhead of query interpretation for maximum CPU efficiency on hot paths.

This lab will demonstrate how to migrate a "hot" table (like a session state or IoT ingestion table) to In-Memory OLTP to achieve significant performance gains.

---

### **Step 1: Database and Schema Setup**

In-Memory OLTP requires a dedicated, memory-optimized filegroup.

1.  **Create the Database and Filegroup:**
    Connect to your SQL Server instance and run this script.

    ```sql
    CREATE DATABASE HighThroughputDB
    CONTAINMENT = NONE
    ON PRIMARY (
        NAME = N'HighThroughputDB_data',
        FILENAME = N'/var/opt/mssql/data/HighThroughputDB.mdf'
    )
    LOG ON (
        NAME = N'HighThroughputDB_log',
        FILENAME = N'/var/opt/mssql/data/HighThroughputDB.ldf'
    );
    GO

    -- Add a MEMORY_OPTIMIZED_DATA filegroup
    ALTER DATABASE HighThroughputDB ADD FILEGROUP HighThroughputDB_memop CONTAINS MEMORY_OPTIMIZED_DATA;
    GO

    -- Add a container (file) to the filegroup
    ALTER DATABASE HighThroughputDB ADD FILE (
        NAME = N'HighThroughputDB_memop_container',
        FILENAME = N'/var/opt/mssql/data/HighThroughputDB_memop'
    ) TO FILEGROUP HighThroughputDB_memop;
    GO

    USE HighThroughputDB;
    GO
    ```

2.  **Create Tables: Disk vs. Memory-Optimized:**
    We will create two tables with identical schemas: one traditional disk-based table and one memory-optimized table.

    ```sql
    -- 1. Traditional disk-based table
    CREATE TABLE dbo.DeviceEvents_Disk (
        EventID BIGINT IDENTITY(1,1) PRIMARY KEY,
        DeviceID UNIQUEIDENTIFIER NOT NULL,
        EventTime DATETIME2(7) NOT NULL,
        Payload NVARCHAR(256) NOT NULL
    );
    GO

    -- 2. Memory-optimized table
    -- Note the new syntax for PRIMARY KEY and DURABILITY.
    CREATE TABLE dbo.DeviceEvents_Memory (
        EventID BIGINT IDENTITY(1,1) NOT NULL,
        DeviceID UNIQUEIDENTIFIER NOT NULL,
        EventTime DATETIME2(7) NOT NULL,
        Payload NVARCHAR(256) NOT NULL,

        -- For memory-optimized tables, constraints are defined inline.
        -- A HASH index is ideal for point lookups on a known key.
        -- BUCKET_COUNT should be ~1-2x the expected number of unique keys.
        CONSTRAINT PK_DeviceEvents_Memory PRIMARY KEY NONCLUSTERED HASH (EventID) WITH (BUCKET_COUNT = 2000000)

    ) WITH (
        MEMORY_OPTIMIZED = ON,
        DURABILITY = SCHEMA_AND_DATA -- Data persists across restarts
    );
    GO
    ```
    **Key Concepts:**
    *   `MEMORY_OPTIMIZED = ON`: This is the magic switch.
    *   `DURABILITY = SCHEMA_AND_DATA`: Guarantees that data is saved to disk during checkpoints and is fully recoverable, just like a regular table. The alternative, `SCHEMA_ONLY`, means data is lost on restart (useful for temporary staging data).
    *   **HASH Index:** A hash index is optimized for equality (`=`) lookups. It computes a hash of the key and jumps directly to the memory location. It does not support range scans (`>`, `<`).

---

### **Step 2: Workload Simulation with Java/Spring Boot**

We'll create a simple Spring Boot app to hammer both tables with concurrent inserts and measure the throughput.

1.  **Create/Setup the Spring Boot Project:**
    Use [start.spring.io](https://start.spring.io) with dependencies: `Spring Web`, `Spring Data JDBC`, `SQL Server Driver`.

2.  **Configure `application.properties`:**
    ```properties
    spring.datasource.url=jdbc:sqlserver://localhost:1433;databaseName=HighThroughputDB;encrypt=false;
    spring.datasource.username=sa
    spring.datasource.password=yourStrong(!)Password
    # Increase the connection pool size for our concurrent test
    spring.datasource.hikari.maximum-pool-size=20
    ```

3.  **Create the Workload Service:**
    This service will use a `VirtualThreadPerTaskExecutor` to simulate many concurrent users inserting data.

    ```java
    // WorkloadService.java
    import org.springframework.jdbc.core.JdbcTemplate;
    import org.springframework.stereotype.Service;
    import java.util.UUID;
    import java.util.concurrent.Executors;
    import java.util.concurrent.TimeUnit;

    @Service
    public class WorkloadService {

        private final JdbcTemplate jdbcTemplate;

        public WorkloadService(JdbcTemplate jdbcTemplate) {
            this.jdbcTemplate = jdbcTemplate;
        }

        public void runWorkload(String tableName, int threadCount, int insertsPerThread) throws InterruptedException {
            System.out.printf("--- Starting workload on %s [%d threads, %d inserts each] ---\n",
                tableName, threadCount, insertsPerThread);

            String sql = String.format("INSERT INTO %s (DeviceID, EventTime, Payload) VALUES (?, GETDATE(), ?)", tableName);

            long startTime = System.currentTimeMillis();

            try (var executor = Executors.newVirtualThreadPerTaskExecutor()) {
                for (int i = 0; i < threadCount; i++) {
                    executor.submit(() -> {
                        for (int j = 0; j < insertsPerThread; j++) {
                            jdbcTemplate.update(sql, UUID.randomUUID(), "Sample payload data " + j);
                        }
                    });
                }
            } // Executor automatically shuts down and waits for completion

            long endTime = System.currentTimeMillis();
            long duration = endTime - startTime;
            long totalInserts = (long) threadCount * insertsPerThread;
            long insertsPerSecond = (totalInserts * 1000) / duration;

            System.out.printf("--- Finished workload on %s ---\n", tableName);
            System.out.printf("Total Inserts: %d\n", totalInserts);
            System.out.printf("Duration: %d ms\n", duration);
            System.out.printf("Throughput: %d inserts/sec\n", insertsPerSecond);
            System.out.println("-------------------------------------\n");
        }
    }

    // Runner to execute the tests
    import org.springframework.boot.CommandLineRunner;
    import org.springframework.stereotype.Component;

    @Component
    public class WorkloadRunner implements CommandLineRunner {
        private final WorkloadService workloadService;

        public WorkloadRunner(WorkloadService workloadService) {
            this.workloadService = workloadService;
        }

        @Override
        public void run(String... args) throws Exception {
            final int threads = 16;
            final int inserts = 10000;

            // Warm-up is important, but skipped for lab brevity.

            // Test disk-based table
            workloadService.runWorkload("dbo.DeviceEvents_Disk", threads, inserts);

            // Give the system a moment to breathe
            Thread.sleep(5000);

            // Test memory-optimized table
            workloadService.runWorkload("dbo.DeviceEvents_Memory", threads, inserts);
        }
    }
    ```

4.  **Run the application:** `mvn spring-boot:run`.

---

### **Step 3: Analyze "Before" and "After" Performance**

Observe the output in your console. You should see a dramatic difference in throughput.

**Expected Results:**

*   **`dbo.DeviceEvents_Disk`:** You will likely see throughput in the range of 5,000-15,000 inserts/sec. As concurrency increases, performance will plateau or even decrease due to latch contention (specifically `PAGELATCH_*` waits on the last page of the clustered index).
*   **`dbo.DeviceEvents_Memory`:** Throughput should be significantly higher, often in the range of 100,000-200,000 inserts/sec or more, depending on your hardware. Performance scales much more linearly with the number of cores because there is no latching or locking.

---

### **Step 4: Supercharging with Natively Compiled Stored Procedures**

Simple `INSERT` statements are fast, but what if we have business logic? By moving that logic into a natively compiled stored procedure, we can eliminate network round-trips and interpretation overhead.

1.  **Create the Natively Compiled Procedure:**
    The syntax has specific requirements: it must be schema-bound, use `BEGIN ATOMIC`, and only interact with memory-optimized objects.

    ```sql
    CREATE OR ALTER PROCEDURE dbo.usp_InsertDeviceEvent_Native
        @DeviceID UNIQUEIDENTIFIER,
        @Payload NVARCHAR(256)
    WITH
        NATIVE_COMPILATION,
        SCHEMABINDING
    AS
    BEGIN ATOMIC WITH (
        TRANSACTION ISOLATION LEVEL = SNAPSHOT,
        LANGUAGE = N'us_english'
    )
      INSERT INTO dbo.DeviceEvents_Memory (DeviceID, EventTime, Payload)
      VALUES (@DeviceID, GETDATE(), @Payload);
    END;
    GO
    ```

2.  **Modify the Java Workload:**
    Update the `WorkloadService` to add a new method that calls this stored procedure.

    ```java
    // In WorkloadService.java, add this method:
    public void runNativeWorkload(int threadCount, int insertsPerThread) throws InterruptedException {
        String tableName = "dbo.DeviceEvents_Memory (via native proc)";
        System.out.printf("--- Starting workload on %s [%d threads, %d inserts each] ---\n",
            tableName, threadCount, insertsPerThread);

        String sql = "EXEC dbo.usp_InsertDeviceEvent_Native ?, ?";

        long startTime = System.currentTimeMillis();

        try (var executor = Executors.newVirtualThreadPerTaskExecutor()) {
            for (int i = 0; i < threadCount; i++) {
                executor.submit(() -> {
                    for (int j = 0; j < insertsPerThread; j++) {
                        jdbcTemplate.update(sql, UUID.randomUUID(), "Native payload data " + j);
                    }
                });
            }
        }

        long endTime = System.currentTimeMillis();
        long duration = endTime - startTime;
        long totalInserts = (long) threadCount * insertsPerThread;
        long insertsPerSecond = (totalInserts * 1000) / duration;

        System.out.printf("--- Finished workload on %s ---\n", tableName);
        System.out.printf("Total Inserts: %d\n", totalInserts);
        System.out.printf("Duration: %d ms\n", duration);
        System.out.printf("Throughput: %d inserts/sec\n", insertsPerSecond);
        System.out.println("-------------------------------------\n");
    }

    // In WorkloadRunner.java, add a call to the new method:
    @Override
    public void run(String... args) throws Exception {
        // ... (previous calls) ...

        Thread.sleep(5000);
        workloadService.runNativeWorkload(threads, inserts);
    }
    ```

3.  **Run and Analyze Again:**
    Rerun the Spring Boot app. Compare the throughput of the direct `INSERT` into the memory-optimized table with the throughput of calling the natively compiled procedure. You should see another significant performance boost, as we have minimized both server-side interpretation and client-server chatter.

---

### **Step 5: Common Pitfalls and Troubleshooting**

*   **Memory Sizing:**
    *   **Problem:** Since data lives entirely in RAM, you can run out. SQL Server will throw "out-of-memory" errors for the database.
    *   **Fix:** Carefully size your memory requirements. A rule of thumb is to budget at least **2x the size of your raw data** to account for indexes and row versioning. Use the `sys.dm_db_xtp_table_memory_stats` DMV to monitor memory consumption.

*   **Unsupported Features:**
    *   **Problem:** Not all T-SQL features are supported on memory-optimized tables or in native procedures (e.g., `TRUNCATE TABLE`, `ALTER TABLE` to add/remove columns, cross-database queries).
    *   **Fix:** Review the documentation for limitations before migrating. Plan your schema and logic carefully.

*   **Hash Index Bucket Count:**
    *   **Problem:** If your `BUCKET_COUNT` is too low, you will get many hash collisions, which degrades performance by creating long chains of rows for a single bucket.
    *   **Fix:** Set `BUCKET_COUNT` to 1-2 times the expected cardinality (number of unique values) of the index key. If you get it wrong, you must drop and recreate the index.

---

### **Step 6: Knowledge Check**

1.  What is the primary performance bottleneck that In-Memory OLTP is designed to eliminate for high-concurrency inserts?
    a) Disk I/O speed.
    b) Network latency.
    c) Latch contention on data pages.
    d) CPU speed.

2.  What does `DURABILITY = SCHEMA_ONLY` signify for a memory-optimized table?
    a) The table schema is durable, but the data is lost upon server restart.
    b) Both the schema and data are fully durable.
    c) The table cannot have indexes.
    d) The table is read-only.

3.  Natively compiled stored procedures are converted into:
    a) An interpreted execution plan.
    b) Java bytecode.
    c) Native machine code (a DLL file).
    d) A CLR assembly.

4.  A `HASH` index is most efficient for which type of operation?
    a) Range scans (e.g., `WHERE ID > 100`).
    b) Point lookups on an exact key (e.g., `WHERE ID = 100`).
    c) `LIKE` searches.
    d) `ORDER BY` clauses.

5.  What is a common cause of performance degradation in a HASH index?
    a) Having too many buckets.
    b) Using a `UNIQUEIDENTIFIER` as the key.
    c) Setting the `BUCKET_COUNT` too low, causing excessive hash collisions.
    d) Reorganizing the index.

*(Answers: 1-c, 2-a, 3-c, 4-b, 5-c)*

---

### **Step 7: Teardown**

1.  **Drop the Database:**
    ```sql
    USE master;
    GO
    DROP DATABASE HighThroughputDB;
    GO
    ```

2.  **Stop the Container:**
    From your terminal, run: `docker-compose down`.