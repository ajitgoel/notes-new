Of course. Here is the first part of your hands-on technical tutoring session on Kafka.
### Prerequisites

Before we begin, please ensure you have the following software installed and configured on your system:

*   **Docker Desktop:** For running our local Kafka environment. Make sure the Docker daemon is running.
*   **Java 17 (or higher):** The labs will use Java for the Spring Boot applications. Ensure your `JAVA_HOME` environment variable is set correctly.
*   **Maven or Gradle:** For building the Spring Boot applications. The examples will use Maven, but the principles are easily adaptable to Gradle.
*   **VS Code (or your preferred IDE):** For editing the Java and configuration files.
*   **A terminal or command prompt:** All commands will be executed from here.

With these tools in place, you're ready to start Lab 1.

---

### **Lab 1: Local Kafka Setup + CLI Fundamentals**

**Objective:** In this lab, you'll set up a local Kafka environment using Docker Compose and learn to interact with it using command-line tools. You'll create topics, produce messages with and without keys, and observe how data is partitioned.

**Time to complete:** 30-45 minutes

#### **Files & Setup**

1.  Create a new directory for your project, for example, `kafka-learning-labs`.
2.  Inside this directory, create a file named `docker-compose.yml` and paste the following content into it:

    ```yaml
    # docker-compose.yml
    version: '3.8'
    services:
      zookeeper:
        image: confluentinc/cp-zookeeper:7.3.0
        container_name: zookeeper
        environment:
          ZOOKEEPER_CLIENT_PORT: 2181
          ZOOKEEPER_TICK_TIME: 2000

      broker:
        image: confluentinc/cp-kafka:7.3.0
        container_name: broker
        ports:
          - "9092:9092"
        depends_on:
          - zookeeper
        environment:
          KAFKA_BROKER_ID: 1
          KAFKA_ZOOKEEPER_CONNECT: 'zookeeper:2181'
          KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: PLAINTEXT:PLAINTEXT,PLAINTEXT_INTERNAL:PLAINTEXT
          KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092,PLAINTEXT_INTERNAL://broker:29092
          KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
          KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 1
          KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 1
    ```

#### **Step-by-Step Instructions**

**1. Start the Kafka Environment**

Open your terminal, navigate to the `kafka-learning-labs` directory, and run the following command:

```bash
docker compose up -d
```

**Expected Output:** You'll see logs indicating that the `zookeeper` and `broker` containers are starting and running in detached mode (`-d`).

```
[+] Running 2/2
 ✔ Network kafka-learning-labs_default  Created                                                                    0.1s
 ✔ Container zookeeper                  Started                                                                    0.8s
 ✔ Container broker                     Started                                                                    0.8s```

**2. Create a Topic**

Now, let's create a topic named `user-profiles` with 3 partitions and a replication factor of 1. Since we only have one broker, the replication factor must be 1.

Execute the following command in your terminal:

```bash
docker compose exec broker kafka-topics --create \
  --topic user-profiles \
  --bootstrap-server localhost:9092 \
  --partitions 3 \
  --replication-factor 1
```

**Expected Output:**

```
Created topic user-profiles.
```

**3. Describe the Topic**

Let's inspect the topic we just created to see its configuration.

```bash
docker compose exec broker kafka-topics --describe \
  --topic user-profiles \
  --bootstrap-server localhost:9092
```

**Expected Output:** This shows the partition distribution across the broker. You'll see three partitions (0, 1, 2), each with a leader on broker 1.

```
Topic: user-profiles      TopicId: <some-id>      PartitionCount: 3       ReplicationFactor: 1    Configs:
        Topic: user-profiles      Partition: 0    Leader: 1       Replicas: 1     Isr: 1
        Topic: user-profiles      Partition: 1    Leader: 1       Replicas: 1     Isr: 1
        Topic: user-profiles      Partition: 2    Leader: 1       Replicas: 1     Isr: 1
```

**4. Produce Messages (Without Keys)**

Open a **new terminal window**. We will use this as our producer. Run the following command to start a console producer that sends messages to the `user-profiles` topic.

```bash
docker compose exec broker kafka-console-producer \
  --topic user-profiles \
  --bootstrap-server localhost:9092
```

Once the producer starts, you'll see a `>` prompt. Type the following messages, pressing Enter after each one:

```
First message
Another one
A third message for good measure
```

**5. Consume Messages and Observe Partitioning**

Now, open a **third terminal window**. This will be our consumer. We will consume the messages and display their partition.

```bash
docker compose exec broker kafka-console-consumer \
  --topic user-profiles \
  --bootstrap-server localhost:9092 \
  --from-beginning \
  --property print.partition=true
```

**Expected Output:** You will see the messages you sent, each prefixed with its partition number. Notice how Kafka distributed them across different partitions in a round-robin fashion because no key was provided.

```
Partition:1   First message
Partition:2   Another one
Partition:0   A third message for good measure
```
*(Note: The partition assignment may vary)*

**6. Produce Messages (With Keys)**

Go back to your **producer terminal** (the second one). Press `Ctrl+C` to stop it. Now, restart it with the `property` flag to enable key-based producing. The key and value will be separated by a colon (`:`).

```bash
docker compose exec broker kafka-console-producer \
  --topic user-profiles \
  --bootstrap-server localhost:9092 \
  --property "parse.key=true" \
  --property "key.separator=:"
```

At the `>` prompt, enter the following key-value pairs:

```
user1:Alice created a profile
user2:Bob updated his settings
user1:Alice uploaded a photo
user3:Charlie logged in
user2:Bob changed his email
```

**7. Consume Messages and Observe Key-Based Partitioning**

Go to your **consumer terminal** (the third one). You should see the new messages appear.

**Expected Output:** Observe that messages with the same key (`user1`, `user2`) are always sent to the same partition. This is crucial for ordering messages related to the same entity.

```
... (previous messages)
Partition:0   user1:Alice created a profile
Partition:2   user2:Bob updated his settings
Partition:0   user1:Alice uploaded a photo
Partition:1   user3:Charlie logged in
Partition:2   user2:Bob changed his email
```

**8. Clean Up**

Once you are finished, you can stop and remove the containers. Go to your first terminal window and run:

```bash
docker compose down
```

---

#### **Checkpoint Quiz**

**1. What is the role of ZooKeeper in the docker-compose.yml file we used?**  
<span style="background:#d3f8b6">ZooKeeper acts as the coordination service for the Kafka cluster.</span> It is responsible for managing critical metadata, such as which brokers are currently online, the configuration of topics (like partition count and replication settings), and electing a "controller" broker that manages the state of partitions and replicas. In simpler terms, it's the cluster's source of truth. (Note: Newer versions of Kafka can run in KRaft mode, which removes the ZooKeeper dependency, but our setup uses the classic ZooKeeper-based architecture).

**2. What does the replication-factor define when creating a topic? Why was it set to 1 in our lab?**  
The replication-factor specifies how many copies of each partition's log will be maintained across the cluster. <span style="background:#d3f8b6">A replication factor of 3, for example, means there will be one leader replica and two follower replicas stored on three different brokers. This is the primary mechanism for fault tolerance.</span> We set it to 1 in our lab because our cluster consists of only a single broker. You cannot have more replicas than the number of available brokers.

**3. How does Kafka decide which partition to send a message to when no key is provided?**  
<span style="background:#d3f8b6">When a message is sent without a key (i.e., the key is null), the producer distributes the messages across all available partitions of the topic in a **round-robin** fashion. </span>This is done to ensure a generally even distribution of data and load across the partitions.

**4. What is the guarantee that Kafka provides for messages sent with the same key?**  
<span style="background:#d3f8b6">Kafka guarantees that all messages sent with the **same key** will always be delivered to the **same partition**.</span> Because a single partition is only ever read by one consumer within a consumer group, this ensures that all messages for that key are processed in the order they were sent. This is known as **per-key ordering**.

**5. What does the --from-beginning flag do in the kafka-console-consumer command?**  
<span style="background:#d3f8b6">By default, a new consumer group starts reading messages from the end of a topic (the "latest" offset), meaning it only sees messages produced after it starts. The --from-beginning flag overrides this behavior, telling the consumer to start reading from the very first available message in the partition logs (offset 0). </span>This is useful for reprocessing all the data in a topic.

**6. <span style="background:#d3f8b6">If you create a topic with 5 partitions and a replication factor of 3,</span> how many total partition replicas exist in the cluster?**  
<font color="#2DC26B">There would be a total of **15** partition replicas. </font>The calculation is simply (number of partitions) * (replication factor), so 5 * 3 = 15.

**7. What happens <span style="background:#d3f8b6">if you try to produce a message to a topic that does not exist?**  </span>
<span style="background:#d3f8b6">By default, the Kafka broker is configured with auto.create.topics.enable=true. This means if a producer sends a message to a topic that doesn't exist, the broker will **automatically create it**</span> using the default cluster settings for partition count and replication factor. In production, this is often disabled to prevent accidental topic creation from client-side typos.

---

#### **Scale-Up Concepts**

*   **Partitions:** Partitions are the fundamental unit of parallelism in Kafka. A topic is split into multiple partitions, and data is distributed among them. This allows multiple consumers to read from a topic simultaneously, dramatically increasing throughput.
*   **Keys:** As you saw, keys determine which partition a message lands in. By default, Kafka uses a hash of the key to map it to a partition. This ensures that all messages for the same key are processed in order by the same consumer.
*   **Consumer Groups:** A consumer group is a set of consumers that cooperate to consume data from a topic. Each partition is assigned to exactly one consumer within the group. If you add more consumers to the group (up to the number of partitions), Kafka automatically rebalances the partition assignments among them, allowing you to scale consumption.

---

#### **Gotchas & Common Pitfalls**

*   **Connection Refused:** If you run a CLI command immediately after `docker compose up`, you might get a "Connection refused" error. This usually means the Kafka broker is still starting up. Wait 15-20 seconds and try again.
*   **Mismatched `--bootstrap-server`:** Always ensure the server address (`localhost:9092` in our case) is correct. In a real cluster, this would be a list of broker addresses.
*   **Forgetting `--from-beginning`:** If you start a consumer after messages have been produced, you won't see them unless you use this flag. By default, a new consumer group starts reading from the end of the log.
*   **Key Separator Issues:** When using `kafka-console-producer` with keys, ensure your separator character doesn't appear in the key itself.

---

#### **Why This Matters in Production**

*   **Throughput & Scalability:** Partitions are the secret to Kafka's high throughput. By splitting a topic into, say, 50 partitions, you can have up to 50 consumers in a group reading in parallel, enabling massive data ingestion and processing rates.
*   **Reliability & Ordering:** Using keys (e.g., `customerId`, `deviceId`) is critical for preserving the order of events for a specific entity. This is essential for stateful processing, where the sequence of events matters (e.g., `OrderCreated` -> `OrderPaid` -> `OrderShipped`). Mismanaging keys can lead to chaotic, incorrect state changes.
*   **Fault Tolerance:** While our lab used a `replication-factor` of 1, production topics are almost always set to 3 or more. This means each partition's data is copied to multiple brokers. If one broker fails, another can take over as the leader for that partition, ensuring no data is lost and the topic remains available.

You have now completed Lab 1. Once you are comfortable with these concepts and have reviewed the quiz, let me know, and I will provide Lab 2.