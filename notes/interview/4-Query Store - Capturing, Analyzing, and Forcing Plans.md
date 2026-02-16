**Prerequisites:**

*   **Docker Desktop & SQL Server:** The same SQL Server 2022 Developer Edition container.
*   **SSMS or Azure Data Studio:** SSMS is highly recommended for this lab as its built-in Query Store reports are excellent.
*   **Java 17+ & Maven:** To build and run the Spring Boot application for testing.

---

### **Introduction: Why Query Store?**

Before Query Store, diagnosing performance regressions was difficult. A query that was fast yesterday could be slow today due to a statistics update, an index change, or a different parameter value, and proving it was challenging. You had to rely on periodic snapshots of the plan cache, which were volatile and incomplete.

<span style="background:#d3f8b6">**Query Store** is the flight data recorder for your database. It automatically captures a history of queries, execution plans, and performance statistics.</span> This allows you to:

*   Quickly find the most expensive queries (by CPU, reads, duration).
*   Identify when a query gets a new, less efficient execution plan (a "regression").
*   Analyze all historical plans for a given query.
*   **Force** the database to use a specific, known-good plan, overriding the optimizer's choice to ensure stable performance.

This lab will simulate a classic performance regression caused by **parameter sniffing** and show how to fix it using Query Store.

---

### **Step 1: Database and Schema Setup**

We'll create a database with a skewed data distribution, a common scenario for parameter sniffing issues.

1.  **Create the Database and Tables:**
    Connect to your SQL Server instance and run this script.

    ```sql
    CREATE DATABASE CustomerDB;
    GO

    USE CustomerDB;
    GO

    -- Create a table for our customers
    CREATE TABLE dbo.Customers (
        CustomerID INT IDENTITY(1,1) PRIMARY KEY,
        CustomerName NVARCHAR(100) NOT NULL,
        RegistrationDate DATE NOT NULL,
        Region VARCHAR(2) NOT NULL -- e.g., 'US', 'EU', 'AP'
    );
    GO

    -- Create a table for orders
    CREATE TABLE dbo.Orders (
        OrderID BIGINT IDENTITY(1,1) PRIMARY KEY,
        CustomerID INT NOT NULL,
        OrderDate DATETIME2 NOT NULL,
        OrderAmount DECIMAL(18, 2) NOT NULL
    );
    GO

    -- Create a supporting nonclustered index, crucial for finding a specific customer's orders
    CREATE INDEX IX_Orders_CustomerID ON dbo.Orders(CustomerID);
    GO
    ```

2.  **Generate Skewed Data:**
    We will create one "VIP" customer with 1 million orders and 50,000 other customers with only 10 orders each.

    ```sql
    SET NOCOUNT ON;
    PRINT 'Generating skewed data... This may take a minute.';

    -- 1. Insert the "VIP" customer (will be CustomerID 1)
    INSERT INTO dbo.Customers (CustomerName, RegistrationDate, Region)
    VALUES ('VIP Corp', '2022-01-01', 'US');

    -- Insert 1 million orders for the VIP customer
    INSERT INTO dbo.Orders (CustomerID, OrderDate, OrderAmount)
    SELECT TOP 1000000
        1, -- VIP CustomerID
        DATEADD(day, -ABS(CHECKSUM(NEWID())) % 365, GETDATE()),
        ABS(CHECKSUM(NEWID())) % 500 + 20
    FROM sys.all_objects a, sys.all_objects b;

    -- 2. Insert 50,000 regular customers
    INSERT INTO dbo.Customers (CustomerName, RegistrationDate, Region)
    SELECT TOP 50000
        'Customer ' + CAST(ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS NVARCHAR),
        '2023-01-01',
        'EU'
    FROM sys.all_objects a, sys.all_objects b;

    -- Insert 10 orders for each of the 50,000 regular customers
    -- Using a loop for simplicity in this lab
    DECLARE @i INT = 2;
    WHILE @i <= 50001
    BEGIN
        INSERT INTO dbo.Orders (CustomerID, OrderDate, OrderAmount)
        SELECT TOP 10
            @i,
            DATEADD(day, -ABS(CHECKSUM(NEWID())) % 365, GETDATE()),
            ABS(CHECKSUM(NEWID())) % 100 + 10
        FROM sys.all_columns;
        SET @i += 1;
    END;

    PRINT 'Data generation complete.';
    ```

---

### **Step 2: Enable Query Store and Create a Stored Procedure**

1.  **Enable Query Store on the Database:**
    ```sql
    -- Turn on Query Store
    ALTER DATABASE CustomerDB SET QUERY_STORE = ON (OPERATION_MODE = READ_WRITE);

    -- (Optional) Clear any previous data if you're re-running the lab
    ALTER DATABASE CustomerDB SET QUERY_STORE CLEAR;
    GO
    ```

2.  **Create the Stored Procedure:**
    This procedure retrieves recent orders for a given customer.

    ```sql
    CREATE OR ALTER PROCEDURE dbo.usp_GetRecentCustomerOrders
        @CustomerID INT
    AS
    BEGIN
        SELECT
            o.OrderID,
            o.OrderDate,
            o.OrderAmount,
            c.CustomerName
        FROM
            dbo.Orders AS o
        JOIN
            dbo.Customers AS c ON o.CustomerID = c.CustomerID
        WHERE
            o.CustomerID = @CustomerID
            AND o.OrderDate > DATEADD(year, -1, GETDATE());
    END;
    GO
    ```

---

### **Step 3: Simulating the Performance Regression**

This is the core of the lab. We will demonstrate how the first execution of a procedure can "sniff" a parameter and create a cached plan that is inefficient for subsequent, different parameters.

1.  **Clear the Procedure Cache:**
    This ensures we start with a clean slate.
    ```sql
    ALTER DATABASE SCOPED CONFIGURATION CLEAR PROCEDURE_CACHE;
    ```

2.  **Run 1 (The "Bad" Sniff): Execute for the VIP customer first.**
    The optimizer will see that `CustomerID = 1` returns 1 million rows. It will correctly decide that scanning the entire `Orders` table is more efficient than doing 1 million individual index seeks. This "Scan" plan is then cached.

    ```sql
    SET STATISTICS IO, TIME ON;
    -- Run for the VIP customer with millions of orders
    EXEC dbo.usp_GetRecentCustomerOrders @CustomerID = 1;
    SET STATISTICS IO, TIME OFF;
    ```
    Note the execution time and logical reads.

3.  **Run 2 (The Regression): Execute for a regular customer.**
    Now we execute the *same procedure* with a different parameter. It will reuse the cached "Scan" plan, which is terribly inefficient for fetching just 10 rows.

    ```sql
    SET STATISTICS IO, TIME ON;
    -- Run for a regular customer with only 10 orders
    EXEC dbo.usp_GetRecentCustomerOrders @CustomerID = 12345;
    SET STATISTICS IO, TIME OFF;
    ```
    Compare the metrics. The execution time and **logical reads** for this run will be huge and disproportionate to the tiny amount of data returned. **This is the performance regression.**

---

### **Step 4: Finding and Analyzing the Regression in Query Store**

Now we use the SSMS GUI to diagnose the problem.

1.  In the Object Explorer, navigate to `CustomerDB > Query Store`.
2.  Open the **"Top Resource Consuming Queries"** report.
3.  You should see your stored procedure at or near the top. Click on the bar for the query to select it.
4.  In the bottom-right pane ("Plan Summary"), you will see the plan that was generated (Plan ID 1). It will show a **Clustered Index Scan** on the `Orders` table.
5.  **Generate the "Good" Plan:** To fix this, we need to get the good plan into Query Store. We'll clear the cache and run the procedure for the *small* customer first.

    ```sql
    -- Clear cache and run for the "normal" case first
    ALTER DATABASE SCOPED CONFIGURATION CLEAR PROCEDURE_CACHE;
    GO
    EXEC dbo.usp_GetRecentCustomerOrders @CustomerID = 12345;
    GO
    ```
    This time, the optimizer will see it only needs 10 rows and will generate a much more efficient plan using an **Index Seek** on `IX_Orders_CustomerID`.

6.  **Analyze in Query Store Again:**
    *   Go back to the "Top Resource Consuming Queries" report and click **Refresh**.
    *   Select the same query. In the "Plan Summary" pane, you will now see **two plans** (Plan ID 1 and Plan ID 2).
    *   You can click on each plan to see its shape. One will be a Scan, the other a Seek.
    *   The top-right pane shows the performance history. You will see dots representing each execution. Notice how the executions for Plan 1 (the Scan) have much higher logical reads than the executions for Plan 2 (the Seek).

    

---

### **Step 5: Forcing the Good Plan**

Now that we have identified the more efficient "Seek" plan, we can tell SQL Server to use it for *all* future executions, regardless of the parameter passed in.

1.  In the "Plan Summary" pane, identify the plan that uses the **Index Seek**. This is our "good" plan.
2.  Select that plan.
3.  Click the **"Force Plan"** button at the top of the pane. Click "Yes" to confirm.

**Verification:**
The plan will now have a checkmark next to it, indicating it is forced.

---

### **Step 6: Verifying the Fix**

Let's re-run our test cases. Because we have forced the "Seek" plan, performance for the small customer should remain excellent, and performance for the VIP customer will now also use the seek plan.

1.  **Execute for the regular customer:**
    ```sql
    SET STATISTICS IO, TIME ON;
    EXEC dbo.usp_GetRecentCustomerOrders @CustomerID = 12345;
    SET STATISTICS IO, TIME OFF;
    ```
    This will be just as fast as before.

2.  **Execute for the VIP customer:**
    ```sql
    SET STATISTICS IO, TIME ON;
    EXEC dbo.usp_GetRecentCustomerOrders @CustomerID = 1;
    SET STATISTICS IO, TIME OFF;
    ```
    Check the execution plan. Even though we are querying for the VIP customer, SQL Server used the **forced Index Seek plan**. The logical reads will be much lower than our very first run, providing stable, predictable (if not perfectly optimal for this one edge case) performance. We have successfully resolved the regression.

---

### **Step 7: Java/Spring Boot Integration**

The beauty of this fix is that it requires **zero application code changes**.

1.  **Create a Spring Boot Project:**
    Use [start.spring.io](https://start.spring.io) with dependencies: `Spring Web`, `Spring Data JDBC`, `SQL Server Driver`.

2.  **Configure `application.properties`:**
```properties
    spring.datasource.url=jdbc:sqlserver://localhost:1433;databaseName=CustomerDB;encrypt=false;
    spring.datasource.username=sa
    spring.datasource.password=yourStrong(!)Password
```

3.  **Create a REST Controller:**
    ```java
    import org.springframework.jdbc.core.JdbcTemplate;
    import org.springframework.web.bind.annotation.GetMapping;
    import org.springframework.web.bind.annotation.PathVariable;
    import org.springframework.web.bind.annotation.RestController;
    import java.util.List;
    import java.util.Map;

    @RestController
    public class OrderController {
        private final JdbcTemplate jdbcTemplate;

        public OrderController(JdbcTemplate jdbcTemplate) {
            this.jdbcTemplate = jdbcTemplate;
        }

        @GetMapping("/orders/{customerId}")
        public List<Map<String, Object>> getCustomerOrders(@PathVariable int customerId) {
            long startTime = System.currentTimeMillis();
            List<Map<String, Object>> results = jdbcTemplate.queryForList(
                "EXEC dbo.usp_GetRecentCustomerOrders @CustomerID = ?", customerId);
            long endTime = System.currentTimeMillis();
            System.out.printf("Request for CustomerID %d completed in %d ms\n",
                customerId, (endTime - startTime));
            return results;
        }
    }
    ```
4.  **Test the endpoint:**
    *   First, unforce the plan in SSMS (Select the forced plan and click "Unforce Plan").
    *   Run the Spring Boot App: `mvn spring-boot:run`.
    *   Hit the endpoint for the VIP customer: `curl http://localhost:8080/orders/1`
    *   Hit the endpoint for a regular customer: `curl http://localhost:8080/orders/12345`. The response will be very slow, and you'll see a long execution time printed in the Java console.
    *   Now, **force the good plan again in SSMS.**
    *   Hit the endpoint for the regular customer again: `curl http://localhost:8080/orders/12345`. The response is now nearly instantaneous.

---

### **Step 8: Common Pitfalls and Troubleshooting**

*   **Forced Plan Becomes Invalid:** If you drop an index that a forced plan relies on, queries will fail until you unforce the plan. Query Store will automatically unforce the plan if it detects such a change, but it's good practice to manage this manually during deployments.
*   **Forgetting to Unforce:** After creating a better index or rewriting a query, you might forget to unforce the old plan. This can prevent the optimizer from using your new, superior solution.
*   **Azure SQL Automatic Tuning:** Azure SQL Database has an "Automatic plan correction" feature that uses Query Store data to automatically detect and fix regressions by forcing the last known-good plan. It's a powerful feature that automates this entire lab.

---

### **Step 9: Knowledge Check**

1.  What is a query performance regression?
    a) A query that uses a lot of CPU.
    b) A query that fails with an error.
    <span style="background:#d3f8b6">c) When a query begins using a new, less efficient execution plan than it used previously.</span>
    d) When Query Store is turned off.

2.  Parameter Sniffing occurs when:
    a) The database server inspects network packets.
    b) <span style="background:#d3f8b6">The query optimizer creates and caches a plan based on the specific parameter value from the *first* execution.</span>
    c) A user provides an incorrect parameter.
    d) An index is missing.

3.  What is the primary function of `sp_query_store_force_plan`?
    a) It tells the optimizer to recompile a query on every execution.
    b) It deletes all bad plans from Query Store.
    c) <span style="background:#d3f8b6">It instructs the query optimizer to always use a specific, pre-existing plan for a query.</span>
    d) It runs a query with elevated permissions.

4.  Which Query Store report is the best starting point for finding overall performance issues?
    a) "Queries With High Variation"
    b) <span style="background:#d3f8b6">"Top Resource Consuming Queries"</span>
    c) "Forced Plans"
    d) "Regressed Queries"

5.  The fix applied using Query Store plan forcing required how many lines of application code change?
    a) One line.
    b) A complete rewrite of the data access layer.
    c) <span style="background:#d3f8b6">Zero lines.</span>
    d) It depends on the framework.

*(Answers: 1-c, 2-b, 3-c, 4-b, 5-c)*

---

### **Step 10: Teardown**

1.  **Drop the Database:**
    ```sql
    USE master;
    GO
    DROP DATABASE CustomerDB;
    GO
    ```

2.  **Stop the Container:**
    From your terminal, run: `docker-compose down`.