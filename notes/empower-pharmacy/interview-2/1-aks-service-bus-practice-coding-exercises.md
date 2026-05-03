# AKS & Service Bus — Practice Coding Exercises

> Work through these before your interview. Each builds a skill you'll likely be asked about.

---

## Exercise 1: Deploy a Multi-Container App to AKS

### Goal
Write K8s manifests and deploy a web API + Redis cache to AKS.

### Tasks
1. Create a `Deployment` for a .NET/Node API (use any public image, e.g. `mcr.microsoft.com/dotnet/samples:aspnetapp`)
2. Create a `Deployment` for Redis (`redis:7-alpine`)
3. Create `Service` resources:
   - API: `LoadBalancer` type on port 80
   - Redis: `ClusterIP` on port 6379
4. Add resource requests/limits to both deployments
5. Create a `ConfigMap` with an env var `REDIS_HOST=redis-service` and mount it in the API deployment

### Starter Template
```yaml
# api-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: sample-api
spec:
  replicas: 2
  selector:
    matchLabels:
      app: sample-api
  template:
    metadata:
      labels:
        app: sample-api
    spec:
      containers:
      - name: api
        image: # TODO: pick an image
        ports:
        - containerPort: 8080
        resources:
          requests:
            cpu: "100m"
            memory: "128Mi"
          limits:
            cpu: "250m"
            memory: "256Mi"
        envFrom:
        - configMapRef:
            name: # TODO
```

### Verification
```bash
kubectl get pods -o wide
kubectl get svc
curl http://
```

---

## Exercise 2: Implement HPA + Cluster Autoscaler

### Goal
Configure auto-scaling at both pod and node level.

### Tasks
1. Apply an HPA to the API deployment from Exercise 1:
   - Target: 50% average CPU
   - Min replicas: 2, Max: 10
2. Enable cluster autoscaler on your node pool (min 1, max 5)
3. Generate load and observe scaling:
   ```bash
   # Run a load generator pod
   kubectl run loadgen --image=busybox --restart=Never -- \
     /bin/sh -c "while true; do wget -q -O- http://sample-api-svc; done"
   ```
4. Watch scaling events:
   ```bash
   kubectl get hpa -w
   kubectl get nodes -w
   ```

### Write the HPA manifest
```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: sample-api-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: sample-api
  minReplicas: # TODO
  maxReplicas: # TODO
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: # TODO
```

### Bonus
Add a PodDisruptionBudget that ensures at least 1 pod is always available during node drain.

---

## Exercise 3: Service Bus Queue Consumer in C#

### Goal
Build a .NET console app that sends and receives messages from a Service Bus queue.

### Tasks
1. Create a console app: `dotnet new console -n SBQueueDemo`
2. Add the SDK: `dotnet add package Azure.Messaging.ServiceBus`
3. Implement the following:

```csharp
// Program.cs
using Azure.Messaging.ServiceBus;

// TODO: Replace with your connection string and queue name
string connectionString = "Endpoint=sb://...";
string queueName = "orders";

// --- SENDER ---
await using var client = new ServiceBusClient(connectionString);
ServiceBusSender sender = client.CreateSender(queueName);

// Send 5 messages
for (int i = 1; i <= 5; i++)
{
    var message = new ServiceBusMessage($"Order-{i}")
    {
        // TODO: Set MessageId for duplicate detection
        // TODO: Set a custom property "Priority" = "High" for i > 3
    };
    await sender.SendMessageAsync(message);
    Console.WriteLine($"Sent: {message.Body}");
}

// --- RECEIVER ---
ServiceBusProcessor processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions
{
    MaxConcurrentCalls = 2,
    AutoCompleteMessages = false  // We'll manually complete
});

processor.ProcessMessageAsync += async args =>
{
    string body = args.Message.Body.ToString();
    Console.WriteLine($"Received: {body}");

    // TODO: Simulate processing failure for "Order-3" → dead-letter it
    // TODO: Complete all other messages

    // Hint:
    // await args.CompleteMessageAsync(args.Message);
    // await args.DeadLetterMessageAsync(args.Message, "ProcessingError", "Simulated failure");
};

processor.ProcessErrorAsync += args =>
{
    Console.WriteLine($"Error: {args.Exception.Message}");
    return Task.CompletedTask;
};

await processor.StartProcessingAsync();
Console.ReadKey();
await processor.StopProcessingAsync();
```

### Verification
- Confirm 4 messages completed, 1 dead-lettered
- Read from DLQ: create a receiver for `{queueName}/$deadletterqueue` and inspect the message

---

## Exercise 4: Topic with Filtered Subscriptions

### Goal
Create a Service Bus topic with two subscriptions, each with a different filter.

### Tasks
1. Use Azure CLI or portal to create:
   - Topic: `store-events`
   - Subscription: `high-value` with SQL filter `Amount > 500`
   - Subscription: `all-events` with no filter (true filter)

2. Write a sender that publishes messages with custom properties:
```csharp
var message = new ServiceBusMessage("Purchase event")
{
    ApplicationProperties =
    {
        { "StoreId", "Store-42" },
        { "Amount", 750 },
        { "EventType", "Purchase" }
    }
};
```

3. Write two receivers — one per subscription — and confirm:
   - `high-value` only gets messages where `Amount > 500`
   - `all-events` gets everything

### Azure CLI Setup
```bash
az servicebus topic create \
  --namespace-name mysbns \
  --resource-group myrg \
  --name store-events

az servicebus topic subscription create \
  --namespace-name mysbns \
  --resource-group myrg \
  --topic-name store-events \
  --name high-value

az servicebus topic subscription rule create \
  --namespace-name mysbns \
  --resource-group myrg \
  --topic-name store-events \
  --subscription-name high-value \
  --name high-value-filter \
  --filter-sql-expression "Amount > 500"
```

---

## Exercise 5: KEDA Scaler for Service Bus on AKS

### Goal
Deploy a consumer app to AKS that auto-scales based on Service Bus queue depth.

### Tasks
1. Install KEDA on your AKS cluster:
   ```bash
   az aks update --name myaks --resource-group myrg --enable-keda
   ```

2. Deploy your consumer app (from Exercise 3) as a Deployment with 0 replicas

3. Create a `ScaledObject`:
```yaml
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: order-consumer-scaler
spec:
  scaleTargetRef:
    name: order-consumer
  minReplicaCount: 0    # Scale to zero when idle
  maxReplicaCount: 10
  triggers:
  - type: azure-servicebus
    metadata:
      queueName: orders
      namespace: mysbns
      messageCount: "5"  # 1 pod per 5 messages
    authenticationRef:
      name: sb-trigger-auth
---
apiVersion: keda.sh/v1alpha1
kind: TriggerAuthentication
metadata:
  name: sb-trigger-auth
spec:
  secretTargetRef:
  - parameter: connection
    name: sb-connection-secret
    key: connectionString
```

4. Send 20 messages to the queue and watch pods scale up:
   ```bash
   kubectl get pods -w
   kubectl get scaledobject order-consumer-scaler
   ```

5. After messages drain, confirm pods scale back to 0

---

## Exercise 6: End-to-End — Secure AKS + Service Bus with Workload Identity

### Goal
Connect AKS pods to Service Bus without any connection strings — using workload identity federation.

### Tasks
1. **Enable workload identity on AKS**:
   ```bash
   az aks update --name myaks --resource-group myrg \
     --enable-oidc-issuer --enable-workload-identity
   ```

2. **Create a managed identity**:
   ```bash
   az identity create --name sb-consumer-identity \
     --resource-group myrg
   ```

3. **Assign Service Bus role**:
   ```bash
   az role assignment create \
     --assignee <managed-identity-client-id> \
     --role "Azure Service Bus Data Receiver" \
     --scope <service-bus-namespace-resource-id>
   ```

4. **Create federated credential**:
   ```bash
   az identity federated-credential create \
     --name sb-fed-cred \
     --identity-name sb-consumer-identity \
     --resource-group myrg \
     --issuer <aks-oidc-issuer-url> \
     --subject system:serviceaccount:default:sb-consumer-sa
   ```

5. **Create K8s ServiceAccount with annotation**:
   ```yaml
   apiVersion: v1
   kind: ServiceAccount
   metadata:
     name: sb-consumer-sa
     annotations:
       azure.workload.identity/client-id: "<managed-identity-client-id>"
   ```

6. **Update deployment to use the service account** and the label:
   ```yaml
   spec:
     template:
       metadata:
         labels:
           azure.workload.identity/use: "true"
       spec:
         serviceAccountName: sb-consumer-sa
   ```

7. **Update app code** to use `DefaultAzureCredential` instead of connection string:
   ```csharp
   var client = new ServiceBusClient(
       "mysbns.servicebus.windows.net",
       new DefaultAzureCredential()
   );
   ```

### Verification
- Pod should authenticate to Service Bus with no secrets mounted
- Check pod env vars — workload identity webhook injects `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and token file path

---

## Progression Guide

| Order | Exercise | Builds On | Estimated Time |
|-------|----------|-----------|----------------|
| 1 | Multi-container deploy | — | 30 min |
| 2 | HPA + Autoscaler | Ex 1 | 20 min |
| 3 | SB Queue consumer | — | 30 min |
| 4 | Topic + filters | Ex 3 | 25 min |
| 5 | KEDA scaler | Ex 2 + 3 | 30 min |
| 6 | Workload Identity | Ex 3 + 5 | 40 min |

---

## Tags
#azure #aks #servicebus #coding #exercises #interview