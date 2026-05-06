##### **API Gateway** 
**What it does:** ==A single entry point for all client requests. It handles cross-cutting concerns so your microservices don’t have to.==
**Example with Azure APIM:**
```
Client App
    │
    ▼
Azure APIM (api.contoso.com)
    ├── /patients/*   → Patient Service (AKS)
    ├── /prescriptions/* → Rx Service (AKS)
    └── /billing/*    → Billing Service (AKS)
```
==APIM handles JWT validation, rate limiting (e.g., 1000 req/min per client), and request routing.== Your individual services never deal with auth tokens directly — they trust the gateway already validated them.
**In an interview, say:** “We use APIM as a reverse proxy. It validates OAuth tokens, enforces throttling policies, and routes to the correct backend. Services behind it only accept traffic from the gateway’s VNet.”
##### ==**Outbox Pattern**== 
==**The problem it solves:** You need to save data to a database AND publish an event to a message bus. If the DB write succeeds but the event publish fails (or vice versa), your system is inconsistent.== This is the “dual-write” problem.

**How it works:**

```csharp hl:1-2,22-23
// Step 1: In a SINGLE database transaction, write both the entity
// and an outbox record
using var transaction = await _dbContext.Database.BeginTransactionAsync();

var prescription = new Prescription { PatientId = 42, Drug = "Metformin" };
_dbContext.Prescriptions.Add(prescription);

var outboxMessage = new OutboxMessage
{
    Id = Guid.NewGuid(),
    EventType = "PrescriptionCreated",
    Payload = JsonSerializer.Serialize(prescription),
    CreatedAt = DateTime.UtcNow,
    Published = false
};
_dbContext.OutboxMessages.Add(outboxMessage);

await _dbContext.SaveChangesAsync();
await transaction.CommitAsync();
// Both succeed or both fail — atomic!

// Step 2: A background worker polls the Outbox table
// and publishes unpublished messages to Service Bus
public class OutboxPublisher : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var pending = await _db.OutboxMessages
                .Where(m => !m.Published)
                .OrderBy(m => m.CreatedAt)
                .Take(50)
                .ToListAsync();

            foreach (var msg in pending)
            {
                await _serviceBus.SendAsync(msg.Payload);
                msg.Published = true;
            }
            await _db.SaveChangesAsync();
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }
}
```
**Key insight:** The event is guaranteed to be published eventually because it’s in the same database as your business data. No distributed transaction needed.
##### **Saga Pattern** 
==**The problem:** A “place order” operation spans OrderService, InventoryService, and PaymentService. You can’t use a single SQL transaction across three databases.==
**Choreography (event-driven):**
```
OrderService         InventoryService       PaymentService
     │                      │                     │
     ├── OrderCreated ──────►                     │
     │                      ├── InventoryReserved──►
     │                      │                     ├── PaymentCharged
     │                      │                     │
     │   (if payment fails, compensating events flow back)
     │                      ◄── ReleaseInventory──┤
     ◄── CancelOrder ──────┤                     │
```
==Each service listens for events and reacts. If PaymentService fails, it emits `PaymentFailed`, which triggers `ReleaseInventory` in InventoryService and `CancelOrder` in OrderService.==

**Orchestration (central coordinator):**
```csharp
public class PlaceOrderSaga
{
    public async Task Execute(Order order)
    {
        // Step 1
        var reserved = await _inventoryService.ReserveStock(order);
        if (!reserved)
        {
            await _orderService.Cancel(order.Id, "Out of stock");
            return;
        }
        // Step 2
        var charged = await _paymentService.Charge(order);
        if (!charged)
        {
            // Compensating action — undo step 1
            await _inventoryService.ReleaseStock(order);
            await _orderService.Cancel(order.Id, "Payment failed");
            return;
        }
        // Step 3
        await _orderService.Confirm(order.Id);
    }
}
```

**When to use which:**
- **Choreography** — fewer services, simple flows, want loose coupling
- **==Orchestration** — complex flows, need clear visibility, easier to debug (you can see the whole flow in one place)==
##### **Circuit Breaker (Polly)** 
**The problem:** Your service calls a downstream API that’s timing out. Every request hangs for 30 seconds, your thread pool fills up, and now YOUR service is down too. Cascading failure.
```csharp
// In Program.cs / Startup.cs
services.AddHttpClient("PaymentService")
    .AddPolicyHandler(Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .OrResult(r => r.StatusCode == HttpStatusCode.ServiceUnavailable)
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 3,   // 3 failures
            durationOfBreak: TimeSpan.FromSeconds(30) // then open for 30s
        ));
```
**The three states:**
```
CLOSED (normal)                 OPEN (failing fast)
  │  3 consecutive failures       │  Rejects all calls immediately
  │  ─────────────────────►       │  for 30 seconds
  │                               │
  │          ◄────────────────────┤
  │       success on trial call   │
  │                               │
  │                          HALF-OPEN
  │                          (lets ONE request through to test)
```
**In practice:** When the circuit is OPEN, calls fail immediately with an exception instead of waiting 30 seconds to timeout. Your service stays responsive, and the downstream service gets time to recover.

##### **Circuit Breaker — Summary** 
**Problem:** ==A downstream service is failing. Without protection, your service wastes threads waiting on timeouts and eventually goes down too (cascading failure).==
**Solution:** ==Three layers of defense, applied to every outgoing HTTP call:==
1. ==**Timeout (5s)** — Kill any single call that takes too long==
2. ==**Retry (2x with backoff)** — Retry failures at 200ms, then 400ms, in case it was a blip==
3. ==**Circuit breaker (3 strikes → 30s cooldown)** — If 3 calls still fail after retries, stop calling entirely for 30 seconds==
**The circuit has three states:**
- **Closed** — Normal. Requests flow through.
- **Open** — Tripped after 3 failures. All requests get an instant exception (no HTTP call made). Lasts 30 seconds.
- **Half-Open** — After 30s, one test request is allowed through. If it succeeds, circuit closes. If it fails, circuit opens again for another 30s.
**Your code catches** `BrokenCircuitException` and returns a fallback — a cached response, a degraded response, or a clear error message — instead of hanging.
**Two benefits:** Your service stays responsive (instant fail vs. 5s timeout), and the downstream service gets breathing room to recover.
##### **Health Checks** 
**Two types, different purposes:**
```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())     // liveness
    .AddSqlServer(connectionString, name: "database")        // readiness
    .AddAzureServiceBusQueue(sbConnection, "orders");        // readiness

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Name == "self"  // only the basic "am I alive" check
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true  // all checks including DB, Service Bus
});
```
**How Kubernetes uses them:**
- **Liveness (**`/health/live`**):** “Is the process stuck?” If this fails, K8s **restarts** the pod. Keep it simple — just confirms the process is running. Don’t check the database here, or a DB outage will restart every pod for no reason.
- **Readiness (**`/health/ready`**):** “Can this pod handle traffic?” If this fails, K8s **removes the pod from the load balancer** but doesn’t restart it. Check DB connectivity, cache, downstream services here. Once they recover, the pod is added back.
**Interview tip:** If asked “tell me about a microservices pattern you’ve used,” lead with the **Outbox pattern** — it’s specific enough to show depth, and naturally leads into discussing Sagas, eventual consistency, and Service Bus. Most candidates only mention API Gateway and Circuit Breaker.
##### **How Azure APIM Handles JWT Validation** 
==APIM validates JWTs **at the gateway level** using an inbound policy — your backend services never see invalid tokens.==
**The Policy (XML in APIM)** 
```xml
<inbound>
    <validate-jwt header-name="Authorization" 
                  failed-validation-httpcode="401"
                  failed-validation-error-message="Unauthorized">
        <!-- WHERE to get the signing keys -->
        <openid-config url="https://login.microsoftonline.com/{tenant-id}/v2.0/.well-known/openid-configuration" />
        <!-- WHO issued the token (must match) -->
        <issuers>
            <issuer>https://login.microsoftonline.com/{tenant-id}/v2.0</issuer>
        </issuers>
        <!-- WHO is the token for (must match) -->
        <audiences>
            <audience>api://my-prescription-api</audience>
        </audiences>
        <!-- WHAT claims must be present -->
        <required-claims>
            <claim name="roles" match="any">
                <value>Prescriber</value>
                <value>Pharmacist</value>
            </claim>
        </required-claims>
    </validate-jwt>
</inbound>
```

**What Happens Step by Step** 
```
1. Client sends:
   GET /patients/42/prescriptions
   Authorization: Bearer eyJhbGciOiJSUzI1NiIs...

2. APIM receives the request and runs the <validate-jwt> policy:

   a. Fetches signing keys from the OpenID Connect discovery endpoint
      (APIM caches these keys — not fetched on every request)
   
   b. Verifies the JWT signature using the public key
      → Is this token actually signed by Azure AD? (not forged)
   
   c. Checks expiration (exp claim)
      → Is the token still valid?
   
   d. Checks issuer (iss claim)
      → Did it come from OUR Azure AD tenant?
   
   e. Checks audience (aud claim)
      → Was this token issued FOR our API? (not some other app)
   
   f. Checks required claims
      → Does the user have "Prescriber" or "Pharmacist" role?

3. If ANY check fails → 401 Unauthorized (request never reaches your service)

4. If ALL checks pass → request forwarded to backend service
```

**What Your Backend Receives** 
APIM can also extract claims and forward them as headers:
```xml
<inbound>
    <validate-jwt ...>
        <!-- same as above -->
    </validate-jwt>
    <!-- Forward useful claims as headers to the backend -->
    <set-header name="X-User-Id" exists-action="override">
        <value>@(context.Request.Headers["Authorization"]
            .First().Split(' ')[1]
            .AsJwt()?.Claims["oid"]?.FirstOrDefault())</value>
    </set-header>
    <set-header name="X-User-Roles" exists-action="override">
        <value>@(string.Join(",", context.Request.Headers["Authorization"]
            .First().Split(' ')[1]
            .AsJwt()?.Claims["roles"] ?? Array.Empty<string>()))</value>
    </set-header>
</inbound>
```
Now your backend service reads `X-User-Id` and `X-User-Roles` headers without ever touching the JWT itself. It trusts APIM already validated everything.
**Why This Matters for the Interview** 
When they ask “how do you handle auth in microservices,” the answer is:
“==Authentication happens once at the API gateway. APIM validates JWTs against our Azure AD tenant — checks signature, expiry, audience, and required roles.== Backend services trust the gateway and receive user identity as forwarded headers. This avoids duplicating auth logic in every service, and the gateway can reject bad requests before they consume backend resources.”