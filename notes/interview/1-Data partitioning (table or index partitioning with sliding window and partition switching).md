You are my expert instructor. Create practical, step‑by‑step hands‑on labs (30–60 minutes each) with code, commands, and clear verification steps for the following SQL Server performance topics, integrated with Java/Spring and Azure data patterns:

1. Data partitioning (table/index partitioning with sliding window and partition switching).

2. Columnstore indexes (clustered and nonclustered) for mixed OLTP/reporting workloads.

3. In-memory OLTP (memory-optimized tables and natively compiled stored procedures).

4. Query Store (capturing, analyzing, and forcing plans; regression detection).

Context and constraints:

- Primary stack: Java 17, Spring Boot, SQL Server (Developer edition or Azure SQL), AKS/Azure, Kafka/Event Hubs, Terraform optional.

- Prefer running locally via Docker Compose for repeatability; provide Azure equivalents where relevant (Azure SQL, Event Hubs, Synapse).

- Keep labs self-contained and reproducible. Each lab must include: prerequisites, setup, seed data generation, workload simulation, performance measurements, validation queries, troubleshooting notes, and teardown.

- Deliverables must include exact SQL scripts, Docker commands, Spring Boot snippets (JDBC or JPA), and minimal config (application.yaml/properties).

- Use realistic data volumes (e.g., 10–50 million rows for columnstore demos; at least several million rows for partitioning) while offering scaled-down variants for limited hardware.

- Show before/after metrics (execution time, logical reads, CPU, wait stats) and how to gather them (SET STATISTICS IO/TIME, Query Store runtime stats, sys.dm_exec_* DMVs).

- Highlight risks and trade-offs: parameter sniffing, fragmented columnstore segments, checkpoint pressure for in-memory OLTP, lock vs latch contention, logging vs minimal logging.

- For partitioning: demonstrate aligned indexes, filtered stats, sliding window maintenance, partition switching between staging and base tables, and backup/restore implications.

- For columnstore: show batch mode, segment elimination, deltastore behavior, compaction, and when nonclustered columnstore benefits mixed OLTP/reporting tables.

- For in-memory OLTP: contrast SCHEMA_ONLY vs SCHEMA_AND_DATA durability, memory sizing, natively compiled procs, and contention reduction for hot paths (e.g., queue/session tables).

- For Query Store: enable/configure, capture baseline, simulate a plan regression (e.g., index change or parameter sniffing scenario), compare runtime stats, and force a known-good plan; include automatic tuning notes.

- Integration mini‑tasks:

 ▫ Java/Spring Boot producer/consumer sending simulated events (Kafka or Azure Event Hubs Kafka API) landing raw data to storage, then batch-load to SQL Server via staging + partition switching.

 ▫ Offload reporting to Azure Synapse: define a pipeline (Data Factory) that copies data from SQL Server to Synapse (dedicated SQL pool or serverless via data lake), add a materialized view or external table, and compare query costs/perf to OLTP.

- Provide a short “knowledge check” (5 questions) per lab and a “common pitfalls” section with fixes.

- Include teardown scripts/commands to clean resources.

Output format:

- Title, Estimated time, Prereqs.

- Step-by-step instructions with numbered steps.

- Code blocks for SQL, Docker, Spring Boot, and Terraform (if used).

- Measurement and validation section.

- Pitfalls and troubleshooting.

- Teardown.

Start with Lab 1: Partitioning and sliding window with Java/Spring ingestion + partition switching. Then proceed to Columnstore, In-memory OLTP, and Query Store.

----------

Of course. Here is a brief explanation of each topic covered in Lab 1.

### **1. Data Partitioning (General Concept)**
Think of a massive, single-volume encyclopedia. Finding information and adding or removing pages is slow and cumbersome. Data partitioning is like splitting that encyclopedia into separate, labeled volumes (e.g., "Vol 1: A-B", "Vol 2: C-D").
<span style="background:#d3f8b6">In SQL Server, **table partitioning** horizontally divides a large table into smaller, more manageable pieces called partitions. While it appears as a single table to your application, the data is physically stored in these separate chunks based on a specific column's value, known as the **partition key**</span> (in our lab, this was `OrderDate`).
**Key Benefits:**
*   **Performance:** When you query the data with a filter on the partition key (e.g., `WHERE OrderDate = '2024-11-10'`), the database is smart enough to only scan the relevant partition(s). This is called **partition elimination**, and it dramatically reduces the amount of data read from the disk, speeding up queries.
*   **Manageability:** Performing maintenance on a small partition (like rebuilding an index or archiving data) is much faster and less disruptive than performing it on the entire multi-billion-row table.

---

### **2. Index Partitioning (Alignment)**

Just as the table's data is partitioned, its indexes can be partitioned too. <span style="background:#d3f8b6">When an index is partitioned using the exact same partition function and key as its table, it is called an **aligned index**.</span>
**Why is alignment critical?**
*   It enables partition elimination to work effectively for indexes as well as the table data.
*   Most importantly, it makes partition-level operations, especially **partition switching**, extremely fast. If an index is *not* aligned, a simple partition switch becomes impossible or requires a full index rebuild, defeating the purpose of the efficient operation.

---

### **3. Partition Switching**

This is the cornerstone of efficient data loading and archiving in partitioned tables.
<span style="background:#d3f8b6">**Partition switching** is a **metadata-only operation**. Instead of physically moving millions of rows one by one (which is slow, generates massive transaction logs, and causes locking), you are simply telling SQL Server to "re-point" its metadata.</span>
You can instantly:
*   **Switch IN:** Move a fully prepared table of data (a *staging table*) into an empty partition of your main table. This is how you load new data in bulk with near-zero downtime.
*   **Switch OUT:** Move an entire partition from your main table to become a standalone archive table. This is how you archive old data without a slow, resource-intensive `DELETE` operation.
The operation is nearly instantaneous because no data is physically moved.

---

### **4. The "Sliding Window" Scenario**

<span style="background:#d3f8b6">The "sliding window"</span> is a common data management pattern, especially for time-series data (like sales, logs, IoT events), that combines all the concepts above. It <span style="background:#d3f8b6">allows you to keep a "window" of recent data in your main table (e.g., the last 24 months) while efficiently adding new data and archiving the oldest data.</span>
The process works in a cycle, typically run monthly or quarterly:
1.  **Load:** New data for the upcoming period (e.g., January 2025) is loaded into a staging table.
2.  **Split:** A new, empty partition is created at the "end" of the date range to accommodate the new data.
3.  **Switch In:** The staging table is instantly switched into the new, empty partition.
4.  **Switch Out:** The oldest partition (e.g., January 2023) is switched out to become a separate archive table.
5.  **Merge:** The now-empty partition boundary at the "start" of the date range is removed.

The "window" of active data appears to slide forward in time, providing a highly efficient and automated way to manage the lifecycle of massive datasets.

-------

Excellent. As an expert instructor, I will guide you through a series of four in-depth, hands-on labs. Each lab is designed to be self-contained, practical, and directly applicable to real-world scenarios using a modern Java, Spring, and Azure data stack. We will start with the foundational concept of data partitioning and build from there.

Let's begin with the first lab.

***

### **Lab 1: Data Partitioning with Sliding Window and Partition Switching**

**Estimated Time:** 60 minutes

**Prerequisites:**

*   **Docker Desktop:** To run SQL Server and Kafka locally.
*   **Java 17+ & Maven:** To build and run the Spring Boot data producer.
*   **Git:** To clone the project repository.
*   **SQL Server Management Studio (SSMS) or Azure Data Studio:** To connect to the SQL Server instance and run queries.
*   **Hardware:** At least 8GB RAM allocated to Docker.

---

### **Step 1: Setup and Environment Preparation**

First, we'll set up our local environment using Docker Compose, which will spin up SQL Server and Kafka containers.

1.  **Create a Project Directory:**
    Create a new folder for your lab files, e.g., `sql-labs`, and navigate into it.

2.  **Create `docker-compose.yml`:**
    This file defines our SQL Server and Kafka services. Save the following as `docker-compose.yml`:

    ```yaml
    version: '3.8'
    services:
      sql-server:
        image: mcr.microsoft.com/mssql/server:2022-latest
        container_name: sql-server-lab1
        ports:
          - "1433:1433"
        environment:
          ACCEPT_EULA: "Y"
          MSSQL_SA_PASSWORD: "yourStrong(!)Password"
          MSSQL_PID: "Developer"
        volumes:
          - sql-data:/var/opt/mssql

      zookeeper:
        image: confluentinc/cp-zookeeper:7.3.0
        container_name: zookeeper
        environment:
          ZOOKEEPER_CLIENT_PORT: 2181

      kafka:
        image: confluentinc/cp-kafka:7.3.0
        container_name: kafka
        ports:
          - "9092:9092"
        depends_on:
          - zookeeper
        environment:
          KAFKA_BROKER_ID: 1
          KAFKA_ZOOKEEPER_CONNECT: 'zookeeper:2181'
          KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: PLAINTEXT:PLAINTEXT,PLAINTEXT_INTERNAL:PLAINTEXT
          KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092,PLAINTEXT_INTERNAL://kafka:29092
          KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
          KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS: 0

    volumes:
      sql-data:
    ```

3.  **Start the Containers:**
    Open a terminal in your project directory and run:

    ```bash
    docker-compose up -d
    ```

    Verification: Run `docker ps` to ensure `sql-server-lab1` and `kafka` are running. It might take a minute for SQL Server to initialize.

---

### **Step 2: Database and Table Preparation (SQL)**

Connect to your local SQL Server instance using SSMS or Azure Data Studio.

*   **Server:** `localhost,1433`
*   **Authentication:** SQL Server Authentication
*   **Login:** `sa`
*   **Password:** `yourStrong(!)Password`

Run the following script to create the database, a partition function, a partition scheme, and our main table.

```sql
-- Create the database
CREATE DATABASE SalesDB;
GO

USE SalesDB;
GO

-- 1. Create a Partition Function
-- We will partition our sales data by month.
-- RANGE RIGHT means the boundary value belongs to the partition on its right.
CREATE PARTITION FUNCTION SalesByMonthRange (DATE)
AS RANGE RIGHT FOR VALUES (
    '2024-02-01', '2024-03-01', '2024-04-01', '2024-05-01', '2024-06-01', '2024-07-01',
    '2024-08-01', '2024-09-01', '2024-10-01', '2024-11-01', '2024-12-01', '2025-01-01'
);
GO

-- 2. Create a Partition Scheme
-- This maps the partitions defined by the function to filegroups.
-- We'll use the PRIMARY filegroup for simplicity.
CREATE PARTITION SCHEME SalesByMonthScheme
AS PARTITION SalesByMonthRange
ALL TO ([PRIMARY]);
GO

-- 3. Create the Partitioned Table
-- The key is to specify the partition scheme and the partitioning column (OrderDate) in the CREATE TABLE statement.
CREATE TABLE Sales (
    SalesID BIGINT IDENTITY(1,1) NOT NULL,
    OrderID VARCHAR(50) NOT NULL,
    OrderDate DATE NOT NULL,
    ProductID INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10, 2) NOT NULL,
    TotalAmount AS (Quantity * UnitPrice)
) ON SalesByMonthScheme(OrderDate);
GO

-- 4. Create a Clustered Index (Aligned)
-- For a partitioned table, the clustered index must include the partitioning key.
-- This is an "aligned" index because it's built on the same partition scheme.
CREATE CLUSTERED INDEX CIX_Sales_OrderDate
ON Sales(OrderDate)
ON SalesByMonthScheme(OrderDate);
GO

-- 5. Create a Staging Table for new data (for partition switching)
-- It must have the EXACT same structure, constraints, and be on the same filegroup as the target partition.
CREATE TABLE Sales_Staging (
    -- This column must be NOT NULL to match the target table.
    -- It CANNOT have the IDENTITY property.
    SalesID BIGINT NOT NULL,
    OrderID VARCHAR(50) NOT NULL,
    OrderDate DATE NOT NULL,
    ProductID INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10, 2) NOT NULL,
    TotalAmount AS (Quantity * UnitPrice),
    -- Add a check constraint to ensure data integrity for the switch operation.
    -- This constraint MUST match the boundary of the target partition.
    CONSTRAINT CHK_Sales_Staging_DateRange CHECK (OrderDate >= '2025-01-01' AND OrderDate < '2025-02-01')
);  
GO 
-- The clustered index remains the same 
CREATE CLUSTERED INDEX CIX_Sales_Staging_OrderDate ON Sales_Staging(OrderDate); GO
```

---

### **Step 3: Data Ingestion with Java/Spring Boot**

Now, let's create a simple Spring Boot application to generate and send sales data to a Kafka topic. A separate consumer (which we won't build here, for brevity) would typically read this data and load it into SQL Server, often using a staging table. For this lab, we'll simulate that final loading step manually.

1.  **Create a Spring Boot Project:**
    Use [start.spring.io](https://start.spring.io) to generate a project with these dependencies:
    *   Spring Web
    *   Spring for Apache Kafka
    *   Spring Data JPA
    *   SQL Server Driver

2.  **Configure `application.properties`:**
    Add Kafka and database connection details.

    ```properties
    # Kafka
    spring.kafka.bootstrap-servers=localhost:9092
    spring.kafka.producer.key-serializer=org.apache.kafka.common.serialization.StringSerializer
    spring.kafka.producer.value-serializer=org.springframework.kafka.support.serializer.JsonSerializer

    # Database (not actively used for sending, but good practice)
    spring.datasource.url=jdbc:sqlserver://localhost:1433;databaseName=SalesDB;encrypt=false
    spring.datasource.username=sa
    spring.datasource.password=yourStrong(!)Password
    ```

3.  **Create a Data Producer:**
    This component will generate and send messages.

    ```java
    // SalesEvent.java
    public record SalesEvent(String orderId, String orderDate, int productId, int quantity, double unitPrice) {}

    // SalesProducerService.java
    import org.springframework.kafka.core.KafkaTemplate;
    import org.springframework.stereotype.Service;
    import java.time.LocalDate;
    import java.time.format.DateTimeFormatter;
    import java.util.UUID;
    import java.util.concurrent.ThreadLocalRandom;

    @Service
    public class SalesProducerService {
        private final KafkaTemplate<String, SalesEvent> kafkaTemplate;
        private static final String TOPIC = "sales-events";

        public SalesProducerService(KafkaTemplate<String, SalesEvent> kafkaTemplate) {
            this.kafkaTemplate = kafkaTemplate;
        }

        public void generateAndSendSalesData(int numberOfRecords, String month) {
            System.out.println("Generating " + numberOfRecords + " records for month: " + month);
            LocalDate startDate = LocalDate.parse(month + "-01");
            LocalDate endDate = startDate.plusMonths(1);
            DateTimeFormatter formatter = DateTimeFormatter.ISO_LOCAL_DATE;

            for (int i = 0; i < numberOfRecords; i++) {
                LocalDate randomDate = startDate.plusDays(ThreadLocalRandom.current().nextInt(0, endDate.getDayOfMonth()));
                SalesEvent event = new SalesEvent(
                    UUID.randomUUID().toString(),
                    randomDate.format(formatter),
                    ThreadLocalRandom.current().nextInt(1, 1000),
                    ThreadLocalRandom.current().nextInt(1, 5),
                    ThreadLocalRandom.current().nextDouble(10.0, 500.0)
                );
                kafkaTemplate.send(TOPIC, event);
            }
             System.out.println("Finished sending records.");
        }
    }

    // A simple runner to trigger the producer
    import org.springframework.boot.CommandLineRunner;
    import org.springframework.stereotype.Component;

    @Component
    public class DataGeneratorRunner implements CommandLineRunner {
        private final SalesProducerService producerService;

        public DataGeneratorRunner(SalesProducerService producerService) {
            this.producerService = producerService;
        }

        @Override
        public void run(String... args) throws Exception {
            // Generate data for a few partitions
            producerService.generateAndSendSalesData(1_000_000, "2024-10-01");
            producerService.generateAndSendSalesData(1_200_000, "2024-11-01");
        }
    }
    
    pom.xml:
    <?xml version="1.0" encoding="UTF-8"?>
<project xmlns="http://maven.apache.org/POM/4.0.0" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xsi:schemaLocation="http://maven.apache.org/POM/4.0.0 https://maven.apache.org/xsd/maven-4.0.0.xsd">
    <modelVersion>4.0.0</modelVersion>

    <!-- Inherit defaults from Spring Boot -->
    <parent>
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-parent</artifactId>
        <version>3.2.5</version> <!-- Or any recent stable version -->
        <relativePath/> <!-- lookup parent from repository -->
    </parent>

    <!-- Project Coordinates -->
    <groupId>com.example</groupId>
    <artifactId>sql-labs-producer</artifactId>
    <version>0.0.1-SNAPSHOT</version>
    <name>sql-labs-producer</name>
    <description>Data producer for SQL Server Performance Labs</description>

    <!-- Project Properties -->
    <properties>
        <java.version>17</java.version>
    </properties>

    <!-- Project Dependencies -->
    <dependencies>
        <!-- Core Spring Boot starter for web applications -->
        <dependency>
            <groupId>org.springframework.boot</groupId>
            <artifactId>spring-boot-starter-web</artifactId>
        </dependency>

        <!-- Spring Boot starter for using Spring Data JPA with Hibernate -->
        <dependency>
            <groupId>org.springframework.boot</groupId>
            <artifactId>spring-boot-starter-data-jpa</artifactId>
        </dependency>

        <!-- Spring Boot starter for Apache Kafka -->
        <dependency>
            <groupId>org.springframework.kafka</groupId>
            <artifactId>spring-kafka</artifactId>
        </dependency>

        <!-- Microsoft SQL Server JDBC Driver -->
        <dependency>
            <groupId>com.microsoft.sqlserver</groupId>
            <artifactId>mssql-jdbc</artifactId>
            <scope>runtime</scope>
        </dependency>

        <!-- Optional: Lombok to reduce boilerplate code (e.g., getters, setters) -->
        <dependency>
            <groupId>org.projectlombok</groupId>
            <artifactId>lombok</artifactId>
            <optional>true</optional>
        </dependency>

        <!-- Spring Boot starter for testing -->
        <dependency>
            <groupId>org.springframework.boot</groupId>
            <artifactId>spring-boot-starter-test</artifactId>
            <scope>test</scope>
        </dependency>

        <!-- Spring Kafka test support -->
        <dependency>
            <groupId>org.springframework.kafka</groupId>
            <artifactId>spring-kafka-test</artifactId>
            <scope>test</scope>
        </dependency>
    </dependencies>

    <!-- Build Configuration -->
    <build>
        <plugins>
            <!-- Spring Boot Maven Plugin to create executable JARs -->
            <plugin>
                <groupId>org.springframework.boot</groupId>
                <artifactId>spring-boot-maven-plugin</artifactId>
                <configuration>
                    <excludes>
                        <exclude>
                            <groupId>org.projectlombok</groupId>
                            <artifactId>lombok</artifactId>
                        </exclude>
                    </excludes>
                </configuration>
            </plugin>
        </plugins>
    </build>

</project>
    ```

4.  **Run the application:**
    `mvn spring-boot:run`
    This will send over two million messages to the `sales-events` Kafka topic.

---

### **Step 4: The Hands-On Lab: Sliding Window and Partition Switching**

Now we simulate a batch ETL process that loads data for a new month (`2025-01-01`) and archives the oldest month (`2024-02-01`).

#### **Part A: Initial Data Load**

First, let's bulk-load the data we "produced" into the main table to simulate an existing populated table. For this lab, we'll use a script. A real-world process would use a tool like `bcp`, SSIS, or a Java batch job.

```sql
-- Simulate loading data for a few months directly into the main table.
-- This represents our pre-existing data.
-- (This may take a minute or two to run)
SET NOCOUNT ON;
INSERT INTO Sales (OrderID, OrderDate, ProductID, Quantity, UnitPrice)
SELECT
    NEWID(),
    DATEADD(day, ABS(CHECKSUM(NEWID())) % 31, '2024-10-01'),
    ABS(CHECKSUM(NEWID())) % 1000 + 1,
    ABS(CHECKSUM(NEWID())) % 5 + 1,
    ABS(CHECKSUM(NEWID())) % 500 + 10
FROM sys.all_objects a, sys.all_objects b;

INSERT INTO Sales (OrderID, OrderDate, ProductID, Quantity, UnitPrice)
SELECT
    NEWID(),
    DATEADD(day, ABS(CHECKSUM(NEWID())) % 30, '2024-11-01'),
    ABS(CHECKSUM(NEWID())) % 1000 + 1,
    ABS(CHECKSUM(NEWID())) % 5 + 1,
    ABS(CHECKSUM(NEWID())) % 500 + 10
FROM sys.all_objects a, sys.all_objects b;
```

#### **Part B: Verify Initial Partitions**

Let's see how our data is distributed across the partitions.

```sql
SELECT
    p.partition_number AS PartitionNumber,
    f.value AS BoundaryValue,
    p.rows AS NumberOfRows
FROM sys.partitions p
JOIN sys.indexes i ON p.object_id = i.object_id AND p.index_id = i.index_id
JOIN sys.partition_schemes ps ON i.data_space_id = ps.data_space_id
JOIN sys.partition_functions pf ON ps.function_id = pf.function_id
JOIN sys.partition_range_values f ON pf.function_id = f.function_id AND p.partition_number = f.boundary_id + 1
WHERE
    p.object_id = OBJECT_ID('Sales') AND i.index_id = 1 -- Clustered Index
ORDER BY
    p.partition_number;
```

You should see rows populated in the partitions corresponding to October and November 2024.

#### **Part C: Loading New Data via Staging Table**

Now, we simulate loading the data for January 2025. This data would typically come from our Kafka topic via a consumer job. We'll insert it directly into the `Sales_Staging` table.

```sql
-- Load one million records for January 2025 into the staging table.
INSERT INTO Sales_Staging (SalesID, OrderID, OrderDate, ProductID, Quantity, UnitPrice)
SELECT TOP 1000000
    -1, -- Provide a non-null placeholder for SalesID
    NEWID(),
    '2025-01-15', -- For simplicity, all on one day that fits the check constraint
    ABS(CHECKSUM(NEWID())) % 1000 + 1,
    ABS(CHECKSUM(NEWID())) % 5 + 1,
    ABS(CHECKSUM(NEWID())) % 500 + 10
FROM sys.all_objects a, sys.all_objects b;
```

#### **Part D: The Partition Switch**

This is the core operation. We switch the now-populated `Sales_Staging` table into an empty partition in our main `Sales` table. This is a metadata-only operation and is nearly instantaneous.

```sql
-- The target partition is 13, for data >= '2025-01-01'.
ALTER TABLE Sales_Staging SWITCH TO Sales PARTITION 13;
```

**Verification:** Re-run the partition verification query from Part B. You will now see 1,000,000 rows in Partition 13. Notice how fast the `ALTER TABLE` command was, regardless of the row count.

#### **Part E: Implementing the Sliding Window**

Our data is growing. We need to prepare for the *next* month (February 2025) and archive the *oldest* month (February 2024, which is currently partition 2).

1.  **Archive Old Data:** Switch out the oldest partition to an archive table.

    ```sql
    -- 1. Create an archive table with the exact same structure
    CREATE TABLE Sales_Archive_Feb2024 (
        SalesID BIGINT,
        OrderID VARCHAR(50) NOT NULL,
        OrderDate DATE NOT NULL,
        ProductID INT NOT NULL,
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(10, 2) NOT NULL,
        TotalAmount AS (Quantity * UnitPrice)
    );
    CREATE CLUSTERED INDEX CIX_Archive_Feb2024 ON Sales_Archive_Feb2024(OrderDate);
    GO

    -- 2. Switch out partition 2 (data for Feb 2024)
    ALTER TABLE Sales SWITCH PARTITION 2 TO Sales_Archive_Feb2024;
    ```
    This partition is now empty and ready to be reused. The old data is safely isolated in `Sales_Archive_Feb2024`.

2.  **Slide the Window:** We "slide" the partition window forward by merging the now-empty oldest boundary and splitting the newest boundary to create a placeholder for future data.

    ```sql
    -- 1. Merge the empty boundary for Feb 2024.
    -- This removes the boundary point '2024-02-01'.
    ALTER PARTITION FUNCTION SalesByMonthRange() MERGE RANGE ('2024-02-01');
    GO

    -- 2. Create a new boundary for Feb 2025.
    -- This reuses the filegroup from the merged partition.
    ALTER PARTITION SCHEME SalesByMonthScheme NEXT USED [PRIMARY];
    ALTER PARTITION FUNCTION SalesByMonthRange() SPLIT RANGE ('2025-02-01');
    GO
    ```

**Final Verification:** Run the partition verification query one last time. You will see that the boundary values have shifted. The first partition now holds data for `< '2024-03-01'`, and there's a new, empty partition at the end for data `>= '2025-02-01'`.

---

### **Step 5: Measurement and Performance**

Let's see *why* partitioning is beneficial for maintenance and querying.

1.  **Query Performance (Partition Elimination):**
    Enable actual execution plans in SSMS/Azure Data Studio and run these queries.

    ```sql
    SET STATISTICS IO, TIME ON;

    -- Query 1: Scans only the partition for November 2024
    -- Look for "Partition" in the Clustered Index Scan operator properties.
    SELECT SUM(TotalAmount) FROM Sales WHERE OrderDate >= '2024-11-01' AND OrderDate < '2024-12-01';

    -- Query 2: Scans multiple partitions
    SELECT SUM(TotalAmount) FROM Sales WHERE OrderDate >= '2024-10-01' AND OrderDate < '2024-12-01';

    -- Query 3: Scans the entire table (if no date filter is applied)
    SELECT SUM(TotalAmount) FROM Sales;

    SET STATISTICS IO, TIME OFF;
    ```
    In the "Messages" tab, compare the **logical reads**. Query 1 will have significantly fewer logical reads than Query 3 because SQL Server only has to access the relevant partition(s). This is called **partition elimination**.

---

### **Step 6: Common Pitfalls and Troubleshooting**

*   **Partition Switching Fails:**
    *   **Reason:** The most common cause is a schema mismatch. The staging table must be an exact mirror of the main table (column order, data types, nullability, collations). The check constraint on the staging table must also perfectly align with the target partition's boundaries.
    *   **Fix:** Use a script to generate the staging table's DDL from the main table to ensure it matches. Double-check your check constraints.

*   **Performance Degradation:**
    *   **Reason:** Non-aligned indexes. If you have non-clustered indexes that are not partitioned on the same scheme, partition maintenance operations (like `SPLIT` or `MERGE`) can be very slow as the entire index needs to be rebuilt.
    *   **Fix:** Ensure all indexes on the partitioned table are aligned (created on the same partition scheme).

*   **Large Number of Partitions:**
    *   **Reason:** Having too many partitions (thousands) can increase metadata overhead and slightly slow down query compilation times.
    *   **Fix:** Choose a reasonable partition granularity (e.g., monthly, not daily, unless required). Most systems perform well with up to a few hundred partitions.

---

### **Step 7: Knowledge Check**

1.  What is the primary benefit of `ALTER TABLE ... SWITCH PARTITION`?
    a) It physically moves data row by row.
    <span style="background:#d3f8b6">b) It is a fast, metadata-only operation.</span>
    c) It automatically archives old data.
    d) It re-indexes the table.

2.  For a partition switch to succeed, the staging table must have:
    a) At least one row of data.
    b) A different schema than the target table.
    <span style="background:#d3f8b6">c) An identical schema and be on the same filegroup as the target partition.</span>
    d) A primary key.

3.  What is "partition elimination"?
    a) Deleting a partition.
    <span style="background:#d3f8b6">b) The query optimizer ignoring partitions that do not contain relevant data.</span>
    c) Merging two partitions together.
    d) Switching a partition to an archive table.

4.  An index is considered "aligned" when:
    a) It is created on the `PRIMARY` filegroup.
<span style="background:#d3f8b6">    b) It uses the same partition function and scheme as the table.</span>
    c) It is a clustered index.
    d) It has the same name as the table.

5.  The `SPLIT RANGE` command is used to:
    a) Remove a partition boundary.
    <span style="background:#d3f8b6">b) Divide an existing partition into two.</span>
    c) Move data between partitions.
    d) Delete all data in a partition.

*(Answers: 1-b, 2-c, 3-b, 4-b, 5-b)*

---

### **Step 8: Teardown**

To clean up your local environment, run the following commands from your project directory:

```bash
# Stop and remove the containers
docker-compose down

# Optional: Remove the SQL Server data volume
docker volume rm sql-labs_sql-data
```

This concludes Lab 1. You have successfully implemented a partitioned table, loaded data using a simulated streaming pipeline, and performed a sliding window maintenance operation.

*(Proceed to the next lab when ready.)*