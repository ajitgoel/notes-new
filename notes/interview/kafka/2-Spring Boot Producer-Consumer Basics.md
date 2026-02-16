**Objective:** In this lab, you will build and run two minimal Spring Boot applications: a producer that sends events via a REST endpoint, and a consumer that logs these events. You will configure them for resilience and observe how consumer groups handle offsets and failures.

**Time to complete:** 45-60 minutes

#### **Project Setup**

We will create two separate Spring Boot projects. The easiest way is to use the Spring Initializr.

1.  Open your web browser to [start.spring.io](https://start.spring.io).
2.  **Create the Producer Project:**
    *   Project: **Maven**
    *   Language: **Java**
    *   Spring Boot: `3.2.x` or higher
    *   Group: `com.example`
    *   Artifact: `kafka-producer`
    *   Packaging: **Jar**
    *   Java: **17**
    *   Dependencies: `Spring Web`, `Spring for Apache Kafka`
    *   Click **GENERATE** and unzip the downloaded file.
3.  **Create the Consumer Project:**
    *   Repeat the process with one change: set the Artifact to `kafka-consumer`.
    *   Click **GENERATE** and unzip the file.

You should now have two project folders: `kafka-producer` and `kafka-consumer`. Open both in your IDE.

---

### **Part 1: The Producer Application (`kafka-producer`)**

#### **Files & Code**

**1. Configure `application.yml`**

Open `src/main/resources/application.properties` and rename it to `application.yml`. Replace its content with the following:

```yaml
# kafka-producer/src/main/resources/application.yml
server:
  port: 8080

spring:
  application:
    name: kafka-producer
  kafka:
    bootstrap-servers: localhost:9092
    producer:
      key-serializer: org.apache.kafka.common.serialization.StringSerializer
      value-serializer: org.springframework.kafka.support.serializer.JsonSerializer
      # --- Resilience Settings ---
      acks: all
      properties:
        enable.idempotence: true # Ensures exactly-once delivery semantics per partition
        # The following are set automatically by enable.idempotence=true, but shown for clarity
        # retries: 2147483647
        # max.in.flight.requests.per.connection: 5
```

**2. Create the Data Schema (`UserActionEvent`)**

Create a new Java class. We'll use a Java `record` for a simple, immutable data object.

```java
// kafka-producer/src/main/java/com/example/kafkaproducer/UserActionEvent.java
package com.example.kafkaproducer;

public record UserActionEvent(String userId, String action, java.time.Instant timestamp) {}
```

**3. Create the REST Controller**

This controller will expose a POST endpoint to receive an action and send it to Kafka.

```java
// kafka-producer/src/main/java/com/example/kafkaproducer/EventsController.java
package com.example.kafkaproducer;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.kafka.core.KafkaTemplate;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RestController;

import java.time.Instant;

@RestController
public class EventsController {

    private static final Logger log = LoggerFactory.getLogger(EventsController.class);
    private final KafkaTemplate<String, UserActionEvent> kafkaTemplate;
    private static final String TOPIC_NAME = "user-actions";

    public EventsController(KafkaTemplate<String, UserActionEvent> kafkaTemplate) {
        this.kafkaTemplate = kafkaTemplate;
    }

    @PostMapping("/events/{userId}")
    public void sendEvent(@PathVariable String userId, @RequestBody String action) {
        UserActionEvent event = new UserActionEvent(userId, action, Instant.now());

        // Send with key (userId) to ensure ordering per user
        kafkaTemplate.send(TOPIC_NAME, event.userId(), event).whenComplete((result, ex) -> {
            if (ex == null) {
                log.info("Sent message=[{}] with offset=[{}] to partition=[{}]",
                        event,
                        result.getRecordMetadata().offset(),
                        result.getRecordMetadata().partition());
            } else {
                log.error("Unable to send message=[{}] due to : {}", event, ex.getMessage());
            }
        });
    }
}
```

---

### **Part 2: The Consumer Application (`kafka-consumer`)**

#### **Files & Code**

**1. Configure `application.yml`**

In the `kafka-consumer` project, rename `application.properties` to `application.yml` and use this configuration:

```yaml
# kafka-consumer/src/main/resources/application.yml
server:
  port: 8081 # Use a different port to avoid conflicts

spring:
  application:
    name: kafka-consumer
  kafka:
    bootstrap-servers: localhost:9092
    consumer:
      group-id: user-action-logger-group
      auto-offset-reset: earliest # Start reading from the beginning of the topic
      key-deserializer: org.apache.kafka.common.serialization.StringDeserializer
      value-deserializer: org.springframework.kafka.support.serializer.JsonDeserializer
      properties:
        spring.json.trusted.packages: "*" # Trust all packages for deserialization
        # For more security, you would list the specific package: com.example.kafkaconsumer
```

**2. Create the Data Schema (`UserActionEvent`)**

Copy the same `UserActionEvent.java` record from the producer project into the consumer project.

```java
// kafka-consumer/src/main/java/com/example/kafkaconsumer/UserActionEvent.java
package com.example.kafkaconsumer;

public record UserActionEvent(String userId, String action, java.time.Instant timestamp) {}
```

**3. Create the Kafka Listener**

This service will listen to the `user-actions` topic and log the messages it receives.

```java
// kafka-consumer/src/main/java/com/example/kafkaconsumer/UserActionsListener.java
package com.example.kafkaconsumer;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.kafka.annotation.KafkaListener;
import org.springframework.kafka.support.KafkaHeaders;
import org.springframework.messaging.handler.annotation.Header;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.stereotype.Service;

@Service
public class UserActionsListener {

    private static final Logger log = LoggerFactory.getLogger(UserActionsListener.class);

    @KafkaListener(topics = "user-actions", groupId = "user-action-logger-group")
    public void listen(
            @Payload UserActionEvent message,
            @Header(KafkaHeaders.RECEIVED_PARTITION) int partition,
            @Header(KafkaHeaders.OFFSET) long offset) {

        log.info("Received message: [{}], from partition: [{}], offset: [{}]",
                message, partition, offset);

        // Here you would add business logic, e.g., save to a database.
    }
}
```

---

### **Step-by-Step Instructions**

**1. Start Kafka Environment**

Navigate to your `kafka-learning-labs` directory and start the Docker containers if they are not already running.

```bash
docker compose up -d
```

**2. Create the Topic**

Create the `user-actions` topic with 3 partitions.

```bash
docker compose exec broker kafka-topics --create \
  --topic user-actions \
  --bootstrap-server localhost:9092 \
  --partitions 3 \
  --replication-factor 1
```

**3. Run the Applications**

*   **Run the Consumer:** Open a terminal in the `kafka-consumer` project root and run:
    ```bash
    ./mvnw spring-boot:run
    ```
*   **Run the Producer:** Open a *new* terminal in the `kafka-producer` project root and run:
    ```bash
    ./mvnw spring-boot:run
    ```

**4. Produce and Consume Messages**

Open a **third terminal** and use `curl` to send some events to the producer's REST endpoint.

```bash
# Send some events for user 'alice'
curl -X POST -H "Content-Type: text/plain" -d "Logged In" http://localhost:8080/events/alice
curl -X POST -H "Content-Type: text/plain" -d "Viewed Dashboard" http://localhost:8080/events/alice

# Send some events for user 'bob'
curl -X POST -H "Content-Type: text/plain" -d "Created Invoice" http://localhost:8080/events/bob
```

**Expected Output:**

*   **Producer Terminal:** You will see logs confirming the messages were sent, including their partition and offset.
    ```
    INFO --- [nio-8080-exec-1] c.e.k.EventsController: Sent message=[UserActionEvent[userId=alice, action=Logged In...]] with offset=[0] to partition=[2]
    INFO --- [nio-8080-exec-1] c.e.k.EventsController: Sent message=[UserActionEvent[userId=alice, action=Viewed Dashboard...]] with offset=[1] to partition=[2]
    INFO --- [nio-8080-exec-1] c.e.k.EventsController: Sent message=[UserActionEvent[userId=bob, action=Created Invoice...]] with offset=[0] to partition=[0]
    ```
*   **Consumer Terminal:** You will see logs showing the messages were received. Notice that all events for 'alice' went to the same partition.
    ```
    INFO --- [ntainer#0-0-C-1] c.e.k.UserActionsListener: Received message: [UserActionEvent[userId=alice, action=Logged In...]], from partition: [2], offset: [0]
    INFO --- [ntainer#0-0-C-1] c.e.k.UserActionsListener: Received message: [UserActionEvent[userId=alice, action=Viewed Dashboard...]], from partition: [2], offset: [1]
    INFO --- [ntainer#0-0-C-1] c.e.k.UserActionsListener: Received message: [UserActionEvent[userId=bob, action=Created Invoice...]], from partition: [0], offset: [0]
    ```

**5. Simulate Failure and Recovery**

*   Stop the consumer application (`Ctrl+C` in its terminal).
*   Send a few more messages using `curl`:
    ```bash
    curl -X POST -H "Content-Type: text/plain" -d "Updated Profile" http://localhost:8080/events/alice
    curl -X POST -H "Content-Type: text/plain" -d "Logged Out" http://localhost:8080/events/bob
    ```
*   Restart the consumer application (`./mvnw spring-boot:run`).

**Expected Output:** The moment the consumer starts, it connects to the broker, finds the last committed offset for its `group-id`, and immediately fetches and processes the messages it missed. This demonstrates Kafka's durable, at-least-once processing guarantee.

**6. Simple Load Test**

Let's send 1,000 messages quickly. In your `curl` terminal, run this simple loop:

```bash
# Bash/Zsh/macOS
for i in {1..1000}; do
  curl -X POST -H "Content-Type: text/plain" -d "Event-$i" "http://localhost:8080/events/user-$((i % 100))" &
done
wait
```
*(Windows users can use a similar loop in PowerShell or WSL).*

Observe the consumer terminal. It will rapidly process the stream of messages, logging them as they arrive.

---

#### **Checkpoint Quiz**

**1. In the producer's application.yml, what is the purpose of acks: all? What is the tradeoff?
- **Purpose:** acks: all provides the strongest delivery guarantee. It means the producer will only consider a message "sent" after the lead broker and all its in-sync follower replicas have successfully received the message. This prevents data loss if a broker fails.
- **Tradeoff:** The tradeoff is higher **latency**, as the producer must wait for acknowledgment from multiple brokers.
**2. What does enable.idempotence: true prevent? How does it relate to message retries?**
- **Purpose:** It prevents duplicate messages caused by producer retries.
- **Relation:** If a producer retries sending a message (e.g., due to a network error), idempotence ensures the broker will accept and write it only once, even if it receives the same message multiple times.
**3. In the consumer's application.yml, what does the group-id property signify? What would happen if you started another consumer instance with the same group-id?**
- **Purpose:** The group-id identifies a set of consumer instances that work together to process a topic. Kafka tracks the consumed offsets for each group
- **What Happens:** Starting another instance with the same group-id would trigger a **rebalance**. Kafka would automatically distribute the topic's partitions among all active instances in the group, allowing them to process messages in parallel.
**4. Why did we need to set spring.json.trusted.packages in the consumer configuration?**  
This is a security measure. The JSON deserializer needs to know which Java classes it is allowed to create from the incoming JSON data. Setting it to "*" is convenient for development but in production, you would list the specific packages (com.example.kafkaconsumer) to prevent deserialization of malicious or unexpected code.
**5. What does the @KafkaListener annotation do? How does it simplify consumer code?**
- **Purpose:** It marks a method as the target for receiving messages from a specific Kafka topic.
- **Simplification:** It abstracts away the complex, low-level consumer poll loop, error handling, and threading, allowing you to focus purely on the business logic for processing each message.
**6. What is the role of auto-offset-reset: earliest? What is the alternative setting and when might you use it?**
- **Role:** earliest tells a consumer what to do when it joins a group with no previously committed offset for its assigned partitions: it should start reading from the very beginning of the topic.
- **Alternative:** The alternative is latest, which would start reading only new messages produced after the consumer starts. You would use latest when you don't care about historical data and only want to process a live stream of events.
**7. If you send 10 messages with the key 'alice' and 5 messages with the key 'bob' to a 3-partition topic, what can you say about the final distribution of those 15 messages?**  
All 10 messages with the key 'alice' will be in **one single partition**, and all 5 messages with the key 'bob' will be in **one single partition**. The 'alice' partition and the 'bob' partition may or may not be the same, but the per-key grouping is guaranteed.

---

#### **Scale-Up Concepts**

*   **Idempotent Producer:** <span style="background:#d3f8b6">Setting `enable.idempotence=true`</span> is a powerful feature. <span style="background:#d3f8b6">If the producer sends a message but doesn't receive an acknowledgment from the broker (e.g., due to a temporary network issue), it will automatically retry.</span> Idempotence ensures that even if the broker received the original message, the retried message will be discarded as a duplicate, guaranteeing each message is written to the log **exactly once** per partition.
*   **<span style="background:#d3f8b6">Consumer Group Scaling</span>:** In our lab, <span style="background:#d3f8b6">one consumer instance handled all 3 partitions. If you were to run a second instance of the `kafka-consumer` application *with the same `group-id`*, Kafka would trigger a **rebalance**. It would automatically reassign the partitions between the two instances.</span> For example, one consumer might get partitions 0 and 1, and the second would get partition 2. This is the primary way to scale message processing.
*   **Retries and Backoff:** Network issues are common. Both producers and consumers can be configured with retry and backoff policies (e.g., "retry up to 3 times, waiting 100ms, then 200ms, then 400ms between attempts"). Spring for Kafka provides robust mechanisms for this, preventing transient failures from causing data loss.

---

#### **Gotchas & Common Pitfalls**

*   **Serialization Errors:** If the producer sends a message with a schema that the consumer doesn't recognize (e.g., a field was added), the consumer will fail during deserialization and enter an error loop. Using a Schema Registry (like Avro) helps enforce schema compatibility.
*   **Consumer Poll Loop:** The `@KafkaListener` hides the underlying `poll()` loop. If your listener code takes too long to execute (e.g., a slow database call), it can cause the consumer to be considered "unhealthy" by the broker, leading to constant rebalancing. Long-running tasks should be handed off to a separate thread pool.
*   **Idempotence Requirements:** The idempotent producer requires `acks=all` and `max.in.flight.requests.per.connection` to be 5 or less. Spring Boot handles this for you, but it's important to know the underlying constraints.

---

#### **Why This Matters in Production**

*   **Data Integrity & Durability (`acks=all`, `idempotence`):** These settings are your primary defense against data loss. <span style="background:#d3f8b6">`acks=all` ensures a message is replicated across multiple brokers before the producer considers it "sent.</span>" Idempotence prevents duplicate messages from transient network errors. Together, they provide a strong foundation for an "exactly-once" processing semantic, which is critical for financial transactions, order processing, and other systems where data accuracy is paramount.
*   **Elastic Scalability (Consumer Groups):** The consumer group model allows you to scale your processing power up or down without downtime. If you have a spike in traffic, you can simply deploy more instances of your consumer service. Kafka handles the partition distribution automatically. This elasticity is crucial for modern, cloud-native applications.
*   <span style="background:#d3f8b6">**Decoupling and Resilience:** </span>The producer doesn't know or care about the consumer. It just sends events to Kafka. <span style="background:#d3f8b6">If the consumer application crashes, the producer can continue accepting data. Once the consumer recovers, it resumes processing from where it left off, thanks to offset tracking.</span> This decoupling makes your entire system more resilient to individual component failures.

You have now completed Lab 2. When you're ready, let me know to proceed with **Lab 3: Kafka Streams Topology**.