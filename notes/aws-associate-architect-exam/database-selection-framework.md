# Friday: Database Selection Framework (3 hours)

Perfect - let's dive into one of the most heavily tested topics on SAA-C03. Database questions appear in **all four domains** and often combine multiple concepts.

---

## Part 1: Study - Database Decision Framework (1 hour)

### **The Master Decision Tree**

I'm going to give you a framework that answers 80% of database questions on the exam. Memorize this structure:

```
START: What type of data model do you need?

┌─────────────────────────────────────────────────────────────┐
│ 1. RELATIONAL (structured, ACID transactions, SQL)         │
└─────────────────────────────────────────────────────────────┘
    │
    ├─ Need Oracle/SQL Server specific features?
    │   ├─ YES, Oracle features (PL/SQL, packages, etc.)
    │   │   └─ RDS for Oracle
    │   │       └─ Cost concern? Consider Aurora PostgreSQL + Babelfish
    │   └─ YES, SQL Server features (T-SQL, SSRS, etc.)
    │       └─ RDS for SQL Server
    │
    ├─ MySQL or PostgreSQL compatible?
    │   ├─ High read scalability needed (15+ read replicas)?
    │   │   └─ Aurora (MySQL or PostgreSQL)
    │   ├─ Variable/unpredictable workload?
    │   │   └─ Aurora Serverless v2
    │   ├─ Standard workload, simpler management?
    │   │   └─ RDS (MySQL or PostgreSQL)
    │   └─ Multi-region active-active writes?
    │       └─ Aurora Global Database
    │
    └─ MariaDB compatible?
        └─ RDS for MariaDB or Aurora MySQL

┌─────────────────────────────────────────────────────────────┐
│ 2. KEY-VALUE (high throughput, millisecond latency)        │
└─────────────────────────────────────────────────────────────┘
    │
    └─ DynamoDB
        ├─ Unpredictable traffic → On-Demand pricing
        ├─ Predictable traffic → Provisioned capacity
        ├─ Global distribution → Global Tables
        └─ Caching layer → DAX (DynamoDB Accelerator)

┌─────────────────────────────────────────────────────────────┐
│ 3. DOCUMENT (JSON/XML, flexible schema)                     │
└─────────────────────────────────────────────────────────────┘
    │
    └─ MongoDB compatible?
        └─ Amazon DocumentDB

┌─────────────────────────────────────────────────────────────┐
│ 4. GRAPH (relationships, social networks, fraud detection)  │
└─────────────────────────────────────────────────────────────┘
    │
    └─ Amazon Neptune

┌─────────────────────────────────────────────────────────────┐
│ 5. IN-MEMORY CACHE (sub-millisecond latency)               │
└─────────────────────────────────────────────────────────────┘
    │
    ├─ Simple cache, no persistence needed → ElastiCache Redis
    ├─ Simple data structures only → ElastiCache Memcached
    └─ Advanced features (pub/sub, sorted sets, persistence)
        └─ ElastiCache Redis

┌─────────────────────────────────────────────────────────────┐
│ 6. SPECIALIZED                                              │
└─────────────────────────────────────────────────────────────┘
    │
    ├─ Time-series data (IoT, metrics) → Amazon Timestream
    ├─ Ledger/immutable audit trail → Amazon QLDB
    └─ Data warehouse/analytics → Amazon Redshift
```

---

###### **RDS vs Aurora: The Critical Comparison**

This is **exam gold** - know these differences cold:

| Feature             | RDS                             | Aurora                            |
| ------------------- | ------------------------------- | --------------------------------- |
| **Failover Time**   | 1-2 minutes                     | <30 seconds (automatic)           |
| **Storage**         | EBS-based                       | Distributed, self-healing storage |
| **Performance**     | Standard                        | 5x MySQL, 3x PostgreSQL           |
| **Backups**         | Automated to S3                 | Continuous, incremental           |
| **Multi-AZ**        | Standby instance (different AZ) | 6 copies across 3 AZs (automatic) |
| **Cost**            | Lower                           | \~20% higher                      |
| **Serverless**      | No                              | Yes (Aurora Serverless v2)        |
| **Global Database** | Manual setup                    | Built-in (cross-region <1s lag)   |

**Exam Trap:** If question mentions "cost-optimized" and workload is standard → RDS. If "high availability" or "read scalability" → Aurora.

---

### **Migration Strategy Decision Tree**

```
Migration Scenario → Strategy

┌─ Same database engine (MySQL → MySQL, Oracle → Oracle)
│  └─ AWS DMS (Database Migration Service) ONLY
│     ├─ Full load (one-time migration)
│     └─ Full load + CDC (continuous replication for minimal downtime)

┌─ Different database engine (Oracle → PostgreSQL)
│  └─ AWS SCT (Schema Conversion Tool) + DMS
│     ├─ Step 1: SCT converts schema, stored procs, triggers
│     ├─ Step 2: DMS migrates data
│     └─ Step 3: Application code changes (if needed)

┌─ Very large database (multi-TB)
│  └─ AWS Snowball Edge + DMS
│     ├─ Step 1: Initial load via Snowball (physical transfer)
│     └─ Step 2: DMS CDC for ongoing changes

┌─ Minimal downtime required
│  └─ DMS with CDC (Change Data Capture)
│     ├─ Full load happens in background
│     ├─ CDC keeps target in sync
│     └─ Cutover when lag is minimal (<1 min)
```

---

### **Read-Heavy Workload Optimization**

**Exam Pattern:** "70% reads, 30% writes" → They want you to think about read replicas

```
Read Optimization Strategy:

RDS:
├─ Create read replicas (up to 5)
├─ Point read traffic to replica endpoints
├─ Asynchronous replication (slight lag acceptable)
└─ Can promote replica to primary (manual failover)

Aurora:
├─ Create reader instances (up to 15)
├─ Use Reader Endpoint (auto load-balances across readers)
├─ Cluster endpoint (write traffic)
├─ Custom endpoints (group specific readers for specific workloads)
└─ Auto-scaling read replicas based on CPU/connections

For Analytics/Reporting:
├─ Dedicated read replica (don't impact production)
├─ Larger instance type for reporting replica
└─ Schedule analytics during off-peak hours
```

---

### **RPO/RTO and Backup Strategy**

**Critical Exam Concept:**

* **RPO (Recovery Point Objective):** How much data can you afford to lose? (time between backups)
* **RTO (Recovery Time Objective):** How quickly must you recover? (downtime tolerance)

```
Backup Strategy by RPO/RTO:

RPO: Hours, RTO: Hours
└─ Automated daily snapshots (RDS default: 7-35 day retention)
   └─ Cost: Lowest

RPO: Minutes, RTO: Minutes
└─ Multi-AZ deployment + automated backups
   └─ Cost: Medium

RPO: Seconds, RTO: <1 minute
└─ Aurora Global Database (cross-region)
   └─ Cost: Highest

Point-in-Time Recovery (PITR):
├─ RDS: 5-minute intervals (up to retention period)
└─ Aurora: 1-second intervals (up to retention period)
```

---

### **Common Exam Scenarios - Pattern Recognition**

**Scenario 1: "Reduce Oracle licensing costs by 60%"**

* **Wrong Answer:** RDS for Oracle (still paying Oracle licenses)
* **Right Answer:** Aurora PostgreSQL with Babelfish OR Schema conversion to Aurora PostgreSQL
* **Why:** Babelfish allows SQL Server compatibility on PostgreSQL (no SQL Server licenses)

**Scenario 2: "Global application, users in 3 continents, need low latency reads"**

* **Wrong Answer:** Cross-region read replicas (manual setup, lag issues)
* **Right Answer:** Aurora Global Database OR DynamoDB Global Tables
* **Why:** Built-in cross-region replication, <1 second lag, automatic failover

**Scenario 3: "E-commerce site, 10,000 req/sec, session data storage"**

* **Wrong Answer:** RDS (not designed for this throughput)
* **Right Answer:** DynamoDB with DAX (in-memory cache) OR ElastiCache Redis
* **Why:** Key-value store for session data, microsecond latency

**Scenario 4: "Financial ledger, immutable audit trail, cryptographically verifiable"**

* **Wrong Answer:** RDS with write-once policy
* **Right Answer:** Amazon QLDB (Quantum Ledger Database)
* **Why:** Built specifically for immutable, verifiable ledgers

**Scenario 5: "IoT sensors, 100,000 data points/second, time-series queries"**

* **Wrong Answer:** DynamoDB (works but not optimized)
* **Right Answer:** Amazon Timestream
* **Why:** Purpose-built for time-series data, automatic tiering

---

## Part 2: Lab 5 - Aurora Multi-Region Setup (2 hours)

Now let's build something. This lab teaches you:

1. Aurora cluster creation and configuration
2. Read replica setup and testing
3. Global Database configuration
4. Failover procedures
5. Monitoring replication lag

### **Lab Architecture**

```
Primary Region (us-east-1)              Secondary Region (us-west-2)
┌─────────────────────────┐            ┌─────────────────────────┐
│ Aurora Cluster          │            │ Aurora Cluster          │
│ ├─ Writer Instance   ◄──┼────────────┼─► Reader Instance       │
│ ├─ Reader Instance 1    │ Replication│    (promoted to writer  │
│ └─ Reader Instance 2    │    <1s lag │     during failover)    │
└─────────────────────────┘            └─────────────────────────┘
         │                                       │
         │                                       │
    Application                            Application
    (write traffic)                       (read traffic / DR)
```

### **Prerequisites**

* AWS Account with access to us-east-1 and us-west-2
* AWS CLI configured (optional but helpful)
* Basic SQL client (psql, MySQL Workbench, or DBeaver)

**Estimated Cost:** \~\$2-4 for 2 hours if you terminate everything properly

---

### **Step 1: Create Primary Aurora Cluster (us-east-1)**

**Via AWS Console:**

1. Navigate to **RDS Console** → **Create database**
2. **Engine options:**
   * Engine type: **Aurora (PostgreSQL Compatible)**
   * Edition: **Aurora PostgreSQL**
   * Version: **Latest stable** (currently 15.x)
   * **Why PostgreSQL?** More exam-relevant than MySQL, better migration scenarios
3. **Templates:**
   * Select: **Dev/Test** (cheaper instances for lab)
4. **Settings:**
   * DB cluster identifier: `aurora-primary-cluster`
   * Master username: `postgres`
   * Master password: `YourPassword123!` (choose something secure)
   * **☑️ Confirm password**
5. **Instance configuration:**
   * DB instance class: **Burstable classes** → `db.t3.medium` (cheapest option)
   * **Exam Note:** Production would use `db.r6g` (memory-optimized)
6. **Availability & durability:**
   * Multi-AZ deployment: **Create an Aurora Replica in a different AZ**
   * **Why?** This creates automatic HA within the region
7. **Connectivity:**
   * VPC: **Default VPC** (or create new if you prefer)
   * Public access: **Yes** (for this lab only - production should be No)
   * VPC security group: **Create new**
     * Name: `aurora-lab-sg`
   * **Exam Note:** Real-world = private subnets + bastion host
8. **Database authentication:**
   * Password authentication (default)
9. **Additional configuration:**
   * Initial database name: `labdb`
   * Backup retention: **1 day** (minimum for lab)
   * **Enable deletion protection:****NO** (so you can delete easily)
10. **Click: Create database**

**Wait time:** 5-10 minutes for cluster creation

---

### **Step 2: Configure Security Group**

While cluster is creating:

1. Go to **EC2 Console** → **Security Groups**
2. Find `aurora-lab-sg`
3. Edit **Inbound rules**:
   * Add rule:
     * Type: **PostgreSQL**
     * Port: **5432**
     * Source: **My IP** (or 0.0.0.0/0 for lab - NOT production!)
4. **Save rules**

**Exam Note:** Production would use:

* Private subnet placement
* Security group allowing only app tier security group
* No public access

---

### **Step 3: Connect and Verify Primary Cluster**

Once cluster is **Available**:

1. Go to **RDS Console** → **Databases** → `aurora-primary-cluster`
2. Click on **Writer instance**
3. Copy **Endpoint** (looks like: `aurora-primary-cluster.cluster-xxxxx.us-east-1.rds.amazonaws.com`)

**Connect using psql (if installed):**

bash

```bash
psql -h aurora-primary-cluster.cluster-xxxxx.us-east-1.rds.amazonaws.com \
     -U postgres \
     -d labdb
```

**Or use any SQL client** with these settings:

* Host: [your writer endpoint]
* Port: 5432
* Username: postgres
* Password: [your password]
* Database: labdb

**Test query:**

sql

```sql
-- Check cluster status
SELECT aurora_version();

-- Create sample table
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Insert test data
INSERT INTO users (username) VALUES 
    ('alice'),
    ('bob'),
    ('charlie');

-- Verify
SELECT * FROM users;
```

**Expected output:** 3 rows returned

---

### **Step 4: Add Additional Read Replicas**

1. **RDS Console** → **Databases** → Select `aurora-primary-cluster`
2. Click **Actions** → **Add reader**
3. **Settings:**
   * DB instance identifier: `aurora-primary-reader-1`
   * Instance class: `db.t3.medium`
   * Availability Zone: **Different from writer**
4. **Click: Add reader**

**Repeat to add second reader:**

* DB instance identifier: `aurora-primary-reader-2`

**Wait:** 3-5 minutes per reader

**Exam Concept Being Tested:**

* Aurora can have up to 15 read replicas
* ==Read replicas share the same underlying storage (not like RDS where each replica has its own storage)==
* Replicas can be in different AZs for HA

---

### **Step 5: Test Read Replica Endpoints**

Once readers are **Available**:

1. Note the **cluster endpoints**:
   * **Writer endpoint:**`aurora-primary-cluster.cluster-xxxxx.us-east-1.rds.amazonaws.com`
   * **Reader endpoint:**`aurora-primary-cluster.cluster-ro-xxxxx.us-east-1.rds.amazonaws.com`
2. **Connect to Reader endpoint:**

sql

```sql
psql -h aurora-primary-cluster.cluster-ro-xxxxx.us-east-1.rds.amazonaws.com \
     -U postgres \
     -d labdb
```

3. **Test read:**

sql

```sql
SELECT * FROM users;
```

**Result:** Should return 3 rows (data is shared across cluster)

4. **Test write (should fail):**

sql

```sql
INSERT INTO users (username) VALUES ('dave');
```

**Expected error:**`cannot execute INSERT in a read-only transaction`

**Exam Concept:** Reader endpoint automatically load-balances across all read replicas

---

### **Step 6: Create Aurora Global Database**

Now the advanced part - multi-region setup:

1. **RDS Console** → **Databases** → Select `aurora-primary-cluster`
2. Click **Actions** → **Add AWS Region**
3. **Global database settings:**
   * Global database identifier: `aurora-global-db`
   * Secondary region: **US West (Oregon) / us-west-2**
4. **Secondary cluster configuration:**
   * DB instance class: `db.t3.medium`
   * DB instance identifier: `aurora-secondary-cluster`
   * **Readable replicas:** 1 (to start)
5. **Connectivity:**
   * Use same VPC approach (or default VPC in us-west-2)
   * Public access: **Yes** (lab only)
   * Create new security group: `aurora-lab-sg-west`
6. **Click: Add region**

**Wait time:** 10-15 minutes (this is creating a cross-region replica)

---

### **Step 7: Configure us-west-2 Security Group**

While waiting:

1. **Switch region** to **us-west-2** in AWS Console
2. **EC2 Console** → **Security Groups** → Find `aurora-lab-sg-west`
3. Add inbound rule:
   * Type: PostgreSQL (5432)
   * Source: My IP or 0.0.0.0/0

---

### **Step 8: Test Global Database Replication**

Once secondary cluster is **Available**:

1. **Primary region (us-east-1)** - Connect to writer:

sql

```sql
INSERT INTO users (username) VALUES ('global-user-1');
SELECT * FROM users;
```

**Result:** 4 rows

2. **Wait 2-5 seconds** (replication lag)
3. **Secondary region (us-west-2)** - Connect to reader endpoint:
   * Endpoint looks like: `aurora-secondary-cluster.cluster-ro-xxxxx.us-west-2.rds.amazonaws.com`

sql

```sql
SELECT * FROM users;
```

**Result:** Should show 4 rows (including 'global-user-1')

4. **Check replication lag:**

sql

```sql
-- In secondary region
SELECT 
    extract(epoch from (now() - pg_last_xact_replay_timestamp())) as replication_lag_seconds;
```

**Typical result:** <1 second

**Exam Concept:** Global Database provides <1 second replication lag across regions

---

### **Step 9: Simulate Regional Failover**

**Scenario:** us-east-1 region fails completely. Promote us-west-2 to primary.

1. **RDS Console** → Switch to **us-west-2**
2. Select `aurora-secondary-cluster`
3. **Actions** → **Remove from Global Database**
4. Confirm removal
5. Once removed, the cluster in us-west-2 becomes **standalone**
6. You can now **write** to it:

sql

```sql
-- Connect to us-west-2 writer endpoint
INSERT INTO users (username) VALUES ('failover-user');
SELECT * FROM users;
```

**Result:** Write succeeds (cluster is now read-write)

**Exam Question Pattern:**

* "Application in us-east-1 fails. What's the RTO?"
* **Answer:** <1 minute (promote secondary cluster, update Route 53)

---

### **Step 10: Monitoring & Observability**

**CloudWatch Metrics to Know for Exam:**

1. **RDS Console** → **Databases** → Select any instance → **Monitoring** tab

Key metrics:

* **DatabaseConnections:** Active connections
* **CPUUtilization:** Should be <70% normally
* **AuroraReplicaLag:** Replication lag in milliseconds
  * **Target:** <100ms for read replicas
  * **Target:** <1000ms for global database
* **ReadLatency / WriteLatency:** Response times
* **VolumeReadIOPs / VolumeWriteIOPs:** Disk I/O

**Exam Scenario:**

* "Read replicas show 5-second lag during peak hours"
* **Solution:** Add more read replicas OR scale up instance type

---

### **Step 11: Cost Analysis (Exam-Critical)**

**Lab Cost Breakdown:**

* Writer instance (db.t3.medium): \~\$0.073/hour
* 2 Readers (db.t3.medium each): \~\$0.073/hour × 2
* Global Database secondary: \~\$0.073/hour
* Storage: \~\$0.10/GB-month (minimal for this lab)
* I/O: Included in Aurora pricing

**Total for 2 hours:**~\$0.60 + storage (~\$2-4 total)

**Exam Cost Optimization Scenarios:**

**Scenario:** "Reduce Aurora costs by 40%"

* **Option 1:** Aurora Serverless v2 (scales to zero during low usage)
* **Option 2:** Reserved Instances (1 or 3 year commitment)
* **Option 3:** Reduce read replicas (if not needed)
* **Option 4:** Switch to RDS (if don't need Aurora features)

---

### **Step 12: Cleanup (IMPORTANT - Avoid Charges)**

**Delete in this order:**

1. **Global Database:**
   * RDS Console → us-west-2 → Select `aurora-secondary-cluster`
   * Actions → Delete (if not already removed from global DB)
   * **☑️ Create final snapshot:** NO (lab only)
   * Type: `delete me` to confirm
2. **Primary Cluster:**
   * RDS Console → us-east-1 → Select `aurora-primary-cluster`
   * Actions → Delete
   * **Uncheck: Create final snapshot**
   * **Uncheck: Retain automated backups**
   * Type: `delete me` to confirm
3. **Verify deletion:**
   * Wait 5 minutes
   * Check both regions - should show "Deleting..." then disappear

---

## Part 3: Key Takeaways for Exam (15 minutes)

### **What You Just Learned (Exam-Relevant)**

✅ **Aurora vs RDS:**
========
* Aurora = 15 read replicas, <30s failover, distributed storage
* RDS = 5 read replicas, 1-2min failover, EBS storage

✅ **Aurora Global Database:**

* Cross-region replication <1 second
* Secondary region can be promoted in <1 minute
* Use case: DR, global read scaling

✅ **Cluster Endpoints:**

* **Writer endpoint:** Always points to primary (even after failover)
* **Reader endpoint:** Load-balances across all read replicas
* **Custom endpoints:** Group specific readers for specific workloads

✅ **Replication:**

* Within cluster: Synchronous (6 copies across 3 AZs)
* Read replicas: Asynchronous (millisecond lag)
* Global database: Asynchronous (<1 second lag)

✅ **Failure Scenarios:**

* AZ failure: Automatic failover to replica in different AZ (<30s)
* Region failure: Promote secondary cluster (\~1 min manual process)
* Instance failure: New instance launched automatically

---

### **Practice Questions (Answer These Now)**

**Question 1:** A company has an Aurora PostgreSQL database with a writer and 3 readers in us-east-1. During peak hours, the application experiences slow read queries. The database metrics show:

* Writer CPU: 25%
* Reader CPU: 85%
* Application load: 70% reads, 30% writes

What should you do?

==Add more read replicas (Aurora supports up to 15)==
==**Why:**==
* ==Writer is not bottlenecked (25% CPU)==
* ==Readers are saturated (85% CPU)==
* ==Read-heavy workload (70% reads)==
* ==Adding readers distributes read traffic via reader endpoint==
==**Wrong answers:**==
* ==Scale up writer (CPU is fine)==
* ==Switch to RDS (doesn't solve read scaling)==
* ==Add caching layer (might help, but question asks about database solution)==

**Question 2:** A financial application requires:
* RTO: 1 hour
* RPO: 5 minutes
* Must survive complete regional failure
* Database: Aurora PostgreSQL
What architecture meets these requirements MOST cost-effectively?

==Aurora with automated backups to S3 + cross-region snapshot copy
**Why:**==
==* RTO 1 hour allows time to restore from snapshot==
==* RPO 5 minutes = need automated backups (taken every 5 min)==
==* Cross-region snapshots enable regional DR==
==* More cost-effective than Global Database (which is overkill for 1-hour RTO)==
==**Wrong answers:**==
==* Global Database: Correct technically but over-engineered for 1-hour RTO (costs \~20% more)==
==* Multi-AZ only: Doesn't protect against regional failure==
==* Manual snapshots: Can't meet 5-minute RPO==

**Question 3:** A company migrating from Oracle to AWS must:
* Minimize downtime (<30 minutes)
* Preserve existing stored procedures and triggers
* Reduce licensing costs by 50%+
* Database size: 2TB
What migration strategy should they use?

1. ==AWS SCT to convert schema to Aurora PostgreSQL==
2. ==AWS DMS with CDC (Change Data Capture) for data migration==
3. ==Rewrite stored procedures in PL/pgSQL (PostgreSQL's procedural language)==
==Why:==
* ==Heterogeneous migration (Oracle → PostgreSQL) requires SCT + DMS==
* ==DMS CDC enables minimal downtime (cutover when replication lag <1 min)==
* ==Aurora PostgreSQL eliminates Oracle licensing costs==
* ==2TB fits within DMS capabilities==
==**Alternative if stored procedures are too complex:**==
* ==Aurora PostgreSQL with Babelfish (SQL Server compatibility layer)==
* ==OR RDS Oracle with lower license tier (doesn't meet 50% cost reduction)==
==**Wrong answer:**==
* ==Lift-and-shift to RDS Oracle: Doesn't reduce licensing costs==

## Your Next Steps

You've completed the **Database Selection Framework** module. Here's what you should do next:

### **Immediate (today):**

1. **Create flashcards** for:
   * Aurora vs RDS comparison table
   * Database selection decision tree
   * Migration strategy (SCT vs DMS)
2. **Practice 20 database questions** from your practice exam bank
   * Focus on: database selection, migration, read scaling

### **Tomorrow:**

Continue with the study plan:

* If following the Friday schedule → Move to **Saturday: Database Migration Patterns**
* If you want to tackle your weak areas sequentially → Next is **VPC Architecture** (Wednesday in the plan)

### **Question for You:**

Now that you've completed this module, how confident do you feel about database questions (1-5)?

And which topic would you like to tackle next?

* **Option A:** Continue with database migration (Lab 6: DMS hands-on)
* **Option B:** Jump to VPC/Networking (your Tier 1 weak area)
* **Option C:** IAM deep dive (highest exam weight)

Let me know and I'll prepare the next module!
