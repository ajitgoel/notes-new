## Empower Pharmacy Interview Prep

Work through these in order. Each exercise builds on concepts from the previous one. **Try solving before looking at the solution.**

---

## Exercise 1: Build a Service with DI
**Difficulty:** ⭐⭐ | **Time:** 15 min | **Topics:** DI, Interfaces, Async

### Problem

Create a `PatientService` that:
1. Depends on an `IPatientRepository` interface (injected via constructor)
2. Has a method `GetActivePatientsByStateAsync(string state)` that returns patients where `IsActive == true` and `State == state`
3. Has a method `DeactivatePatientAsync(int id)` that sets `IsActive = false` and saves

Define the `Patient` model, the interface, and the service class.
### Starter Code

```csharp
public class Patient
{
    // TODO: Define properties
}

public interface IPatientRepository
{
    // TODO: Define methods
}

public class PatientService
{
    // TODO: Implement with DI
}
```

---

> [!success]- Solution (click to expand)
>
> ```csharp
> public class Patient
> {
>     public int Id { get; set; }
>     public string Name { get; set; } = string.Empty;
>     public string State { get; set; } = string.Empty;
>     public bool IsActive { get; set; } = true;
> }
> public interface IPatientRepository
> {
>     Task<List<Patient>> GetByStateAsync(string state);
>     Task<Patient?> GetByIdAsync(int id);
>     Task SaveAsync(Patient patient);
> }
> public class PatientService
> {
>     private readonly IPatientRepository _repo;
>     public PatientService(IPatientRepository repo)
>     {
>         _repo = repo;
>     }
>     public async Task<List<Patient>> GetActivePatientsByStateAsync(string state)
>     {
>         var patients = await _repo.GetByStateAsync(state);
>         return patients.Where(p => p.IsActive).ToList();
>     }
>
>     public async Task DeactivatePatientAsync(int id)
>     {
>         var patient = await _repo.GetByIdAsync(id)
>             ?? throw new InvalidOperationException($"Patient {id} not found");
>
>         patient.IsActive = false;
>         await _repo.SaveAsync(patient);
>     }
> }
>
> // Registration in Program.cs:
> // builder.Services.AddScoped<IPatientRepository, SqlPatientRepository>();
> // builder.Services.AddScoped<PatientService>();
> ```

---

## Exercise 2: REST API Controller
**Difficulty:** ⭐⭐⭐ | **Time:** 20 min | **Topics:** REST, Controllers, DTOs, Status Codes

### Problem

Build a `PrescriptionsController` with these endpoints:

| Method | Route | Returns | Status |
|---|---|---|---|
| GET | `/api/prescriptions/{id}` | Single prescription | 200 or 404 |
| GET | `/api/prescriptions?status=pending` | Filtered list | 200 |
| POST | `/api/prescriptions` | Created ID | 201 with Location |
| DELETE | `/api/prescriptions/{id}` | Nothing | 204 |

Create the `CreatePrescriptionDto` with validation:
- `PatientId` — required
- `MedicationName` — required, max 200 chars
- `Dosage` — required, range 0.1 to 5000
- `Notes` — optional

---

> [!success]- Solution (click to expand)
>
> ```csharp
> public record CreatePrescriptionDto(
>     [Required] int PatientId,
>     [Required, StringLength(200)] string MedicationName,
>     [Required, Range(0.1, 5000)] decimal Dosage,
>     string? Notes
> );
>
> public record PrescriptionDto(
>     int Id, int PatientId, string MedicationName,
>     decimal Dosage, string Status, DateTime CreatedAt
> );
>
> [ApiController]
> [Route("api/[controller]")]
> public class PrescriptionsController : ControllerBase
> {
>     private readonly IPrescriptionService _service;
>
>     public PrescriptionsController(IPrescriptionService service)
>         => _service = service;
>
>     [HttpGet("{id}")]
>     public async Task<IActionResult> GetById(int id)
>     {
>         var rx = await _service.GetByIdAsync(id);
>         return rx is null ? NotFound() : Ok(rx);
>     }
>
>     [HttpGet]
>     public async Task<IActionResult> GetByStatus([FromQuery] string? status)
>     {
>         var results = await _service.GetByStatusAsync(status);
>         return Ok(results);
>     }
>
>     [HttpPost]
>     public async Task<IActionResult> Create(CreatePrescriptionDto dto)
>     {
>         var id = await _service.CreateAsync(dto);
>         return CreatedAtAction(nameof(GetById), new { id }, new { id });
>     }
>
>     [HttpDelete("{id}")]
>     public async Task<IActionResult> Delete(int id)
>     {
>         await _service.DeleteAsync(id);
>         return NoContent();
>     }
> }
> ```

---
## Exercise 3: LINQ Data Processing
**Difficulty:** ⭐⭐⭐ | **Time:** 15 min | **Topics:** LINQ, GroupBy, Aggregation
### Problem
Given this data model:
```csharp
public record Order(int Id, int PharmacistId, string PatientState,
    decimal Total, OrderStatus Status, DateTime CreatedAt);
public enum OrderStatus { Pending, Processing, Completed, Cancelled }
```
Write LINQ queries for each:
1. **Top 5 states by completed order revenue** — return state, order count, and total revenue, sorted by revenue descending
2. **Pharmacist daily productivity** — for a given date, return each pharmacist's completed order count and average order value
3. **Unfulfilled orders older than 48 hours** — return Pending or Processing orders where `CreatedAt` is more than 48 hours ago, sorted oldest first
---

> [!success]- Solution (click to expand)
>
> ```csharp
> // 1. Top 5 states by revenue
> var topStates = orders
>     .Where(o => o.Status == OrderStatus.Completed)
>     .GroupBy(o => o.PatientState)
>     .Select(g => new
>     {
>         State = g.Key,
>         OrderCount = g.Count(),
>         Revenue = g.Sum(o => o.Total)
>     })
>     .OrderByDescending(x => x.Revenue)
>     .Take(5)
>     .ToList();
>
> // 2. Pharmacist daily productivity
> var targetDate = new DateTime(2026, 4, 28);
> var productivity = orders
>     .Where(o => o.Status == OrderStatus.Completed
>         && o.CreatedAt.Date == targetDate)
>     .GroupBy(o => o.PharmacistId)
>     .Select(g => new
>     {
>         PharmacistId = g.Key,
>         CompletedCount = g.Count(),
>         AvgOrderValue = g.Average(o => o.Total)
>     })
>     .OrderByDescending(x => x.CompletedCount)
>     .ToList();
>
> // 3. Unfulfilled orders older than 48h
> var cutoff = DateTime.UtcNow.AddHours(-48);
> var stale = orders
>     .Where(o => (o.Status == OrderStatus.Pending
>                || o.Status == OrderStatus.Processing)
>         && o.CreatedAt < cutoff)
>     .OrderBy(o => o.CreatedAt)
>     .ToList();
> ```

---
## Exercise 4: Refactor Untestable Code
**Difficulty:** ⭐⭐⭐⭐ | **Time:** 25 min | **Topics:** DI, Testing, Refactoring
### Problem
This code works but is **untestable**. Refactor it so every dependency is injectable and write one unit test using FakeItEasy (or describe the test structure with any mocking framework).
```csharp
public class OrderProcessor
{
    public void ProcessOrder(int orderId)
    {
        var db = new SqlConnection("Server=prod;Database=Pharmacy;");
        var order = db.Query<Order>($"SELECT * FROM Orders WHERE Id = {orderId}")
                      .FirstOrDefault();

        if (order == null) throw new Exception("Order not found");

        order.Status = "Processed";
        order.ProcessedAt = DateTime.Now;

        db.Execute($"UPDATE Orders SET Status = '{order.Status}', " +
                   $"ProcessedAt = '{order.ProcessedAt}' WHERE Id = {orderId}");

        var emailer = new SmtpEmailService();
        emailer.Send(order.PatientEmail, "Your order has been processed!");

        Console.WriteLine($"Order {orderId} processed.");
    }
}
```

**Identify all the problems**, then rewrite.

---

> [!success]- Solution (click to expand)
>
> **Problems found:**
> 1. `new SqlConnection` — hard-coded DB dependency
> 2. Raw SQL with string interpolation — SQL injection risk
> 3. `new SmtpEmailService` — can't fake emails in tests
> 4. `DateTime.Now` — non-deterministic, can't test time logic
> 5. `Console.WriteLine` — should use ILogger
> 6. No async — blocks threads
> 7. Generic `Exception` — should use specific exception types
>
> **Refactored:**
>
> ```csharp
> public interface IOrderRepository
> {
>     Task<Order?> GetByIdAsync(int id);
>     Task UpdateAsync(Order order);
> }
>
> public interface INotificationService
> {
>     Task SendOrderConfirmationAsync(string email, int orderId);
> }
>
> public interface IDateTimeProvider
> {
>     DateTime UtcNow { get; }
> }
>
> public class OrderProcessor
> {
>     private readonly IOrderRepository _repo;
>     private readonly INotificationService _notifier;
>     private readonly IDateTimeProvider _clock;
>     private readonly ILogger<OrderProcessor> _logger;
>
>     public OrderProcessor(
>         IOrderRepository repo,
>         INotificationService notifier,
>         IDateTimeProvider clock,
>         ILogger<OrderProcessor> logger)
>     {
>         _repo = repo;
>         _notifier = notifier;
>         _clock = clock;
>         _logger = logger;
>     }
>
>     public async Task ProcessOrderAsync(int orderId)
>     {
>         var order = await _repo.GetByIdAsync(orderId)
>             ?? throw new OrderNotFoundException(orderId);
>
>         order.Status = "Processed";
>         order.ProcessedAt = _clock.UtcNow;
>
>         await _repo.UpdateAsync(order);
>         await _notifier.SendOrderConfirmationAsync(
>             order.PatientEmail, orderId);
>
>         _logger.LogInformation("Order {OrderId} processed", orderId);
>     }
> }
> ```
>
> **Unit test:**
>
> ```csharp
> [Fact]
> public async Task ProcessOrderAsync_UpdatesStatusAndNotifies()
> {
>     // Arrange
>     var fakeRepo = A.Fake<IOrderRepository>();
>     var fakeNotifier = A.Fake<INotificationService>();
>     var fakeClock = A.Fake<IDateTimeProvider>();
>     var fakeLogger = A.Fake<ILogger<OrderProcessor>>();
>
>     var order = new Order
>     {
>         Id = 1,
>         PatientEmail = "patient@test.com",
>         Status = "Pending"
>     };
>     var fixedTime = new DateTime(2026, 4, 28, 12, 0, 0);
>
>     A.CallTo(() => fakeRepo.GetByIdAsync(1)).Returns(order);
>     A.CallTo(() => fakeClock.UtcNow).Returns(fixedTime);
>
>     var processor = new OrderProcessor(
>         fakeRepo, fakeNotifier, fakeClock, fakeLogger);
>
>     // Act
>     await processor.ProcessOrderAsync(1);
>
>     // Assert
>     Assert.Equal("Processed", order.Status);
>     Assert.Equal(fixedTime, order.ProcessedAt);
>
>     A.CallTo(() => fakeRepo.UpdateAsync(order))
>         .MustHaveHappenedOnceExactly();
>
>     A.CallTo(() => fakeNotifier
>         .SendOrderConfirmationAsync("patient@test.com", 1))
>         .MustHaveHappenedOnceExactly();
> }
> ```

---
## Exercise 5: Build a Middleware
**Difficulty:** ⭐⭐⭐ | **Time:** 15 min | **Topics:** Middleware, HttpContext
### Problem
Create a custom middleware called `HipaaAuditMiddleware` that:
1. Logs the request method, path, and user identity **before** the request is processed
2. Logs the response status code and elapsed time **after** the request completes
3. If the path starts with `/api/patients`, also logs `"PHI_ACCESS"` as an audit tag
This is the kind of cross-cutting concern that matters in healthcare software.

---

> [!success]- Solution (click to expand)
>
> ```csharp
> public class HipaaAuditMiddleware
> {
>     private readonly RequestDelegate _next;
>     private readonly ILogger<HipaaAuditMiddleware> _logger;
>
>     public HipaaAuditMiddleware(RequestDelegate next,
>         ILogger<HipaaAuditMiddleware> logger)
>     {
>         _next = next;
>         _logger = logger;
>     }
>
>     public async Task InvokeAsync(HttpContext context)
>     {
>         var path = context.Request.Path;
>         var method = context.Request.Method;
>         var user = context.User.Identity?.Name ?? "anonymous";
>         var isPhi = path.StartsWithSegments("/api/patients");
>
>         _logger.LogInformation(
>             "REQUEST {Method} {Path} by {User}{Phi}",
>             method, path, user,
>             isPhi ? " [PHI_ACCESS]" : "");
>
>         var sw = Stopwatch.StartNew();
>
>         await _next(context);
>
>         sw.Stop();
>
>         _logger.LogInformation(
>             "RESPONSE {Method} {Path} → {Status} in {Ms}ms{Phi}",
>             method, path,
>             context.Response.StatusCode,
>             sw.ElapsedMilliseconds,
>             isPhi ? " [PHI_ACCESS]" : "");
>     }
> }
>
> // Register BEFORE auth middleware
> // app.UseMiddleware<HipaaAuditMiddleware>();
> // app.UseAuthentication();
> // app.UseAuthorization();
> ```

---
## Exercise 6: Generic Repository + EF Core
**Difficulty:** ⭐⭐⭐⭐ | **Time:** 20 min | **Topics:** Generics, EF Core, Async
### Problem
Build a generic `Repository<T>` class that works with any EF Core entity. It should:
1. Implement `IRepository<T>` with: `GetByIdAsync`, `GetAllAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`
2. Use a generic constraint to ensure `T` is a class with an `int Id` property
3. Include a method `FindAsync(Expression<Func<T, bool>> predicate)` that accepts a LINQ predicate
Then create a specialized `OrderRepository` that extends it with `GetByStatusAsync(OrderStatus status)`.

---

> [!success]- Solution (click to expand)
>
> ```csharp
> public interface IEntity
> {
>     int Id { get; set; }
> }
>
> public interface IRepository<T> where T : class, IEntity
> {
>     Task<T?> GetByIdAsync(int id);
>     Task<List<T>> GetAllAsync();
>     Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);
>     Task AddAsync(T entity);
>     Task UpdateAsync(T entity);
>     Task DeleteAsync(int id);
> }
>
> public class Repository<T> : IRepository<T> where T : class, IEntity
> {
>     protected readonly PharmacyDbContext _ctx;
>
>     public Repository(PharmacyDbContext ctx) => _ctx = ctx;
>
>     public async Task<T?> GetByIdAsync(int id)
>         => await _ctx.Set<T>().FindAsync(id);
>
>     public async Task<List<T>> GetAllAsync()
>         => await _ctx.Set<T>().ToListAsync();
>
>     public async Task<List<T>> FindAsync(
>         Expression<Func<T, bool>> predicate)
>         => await _ctx.Set<T>().Where(predicate).ToListAsync();
>
>     public async Task AddAsync(T entity)
>     {
>         _ctx.Set<T>().Add(entity);
>         await _ctx.SaveChangesAsync();
>     }
>
>     public async Task UpdateAsync(T entity)
>     {
>         _ctx.Set<T>().Update(entity);
>         await _ctx.SaveChangesAsync();
>     }
>
>     public async Task DeleteAsync(int id)
>     {
>         var entity = await GetByIdAsync(id);
>         if (entity is not null)
>         {
>             _ctx.Set<T>().Remove(entity);
>             await _ctx.SaveChangesAsync();
>         }
>     }
> }
>
> // Specialized repository
> public interface IOrderRepository : IRepository<Order>
> {
>     Task<List<Order>> GetByStatusAsync(OrderStatus status);
> }
>
> public class OrderRepository : Repository<Order>, IOrderRepository
> {
>     public OrderRepository(PharmacyDbContext ctx) : base(ctx) { }
>
>     public async Task<List<Order>> GetByStatusAsync(OrderStatus status)
>         => await _ctx.Orders
>             .Include(o => o.Patient)
>             .Where(o => o.Status == status)
>             .OrderByDescending(o => o.CreatedAt)
>             .ToListAsync();
> }
> ```

---

## Recommended Practice Order

1. **Exercise 1** (DI basics) — warm-up
2. **Exercise 3** (LINQ) — practice the GroupBy muscle memory
3. **Exercise 4** (Refactoring) — **most likely to mirror the actual interview**
4. **Exercise 2** (REST API) — full controller pattern
5. **Exercise 5** (Middleware) — healthcare context bonus points
6. **Exercise 6** (Generics + EF) — if time allows

> [!tip] During the Interview
> If you get stuck, talk through your approach: "I'd define an interface first, inject it via constructor, and write the implementation separately." Showing how you think matters more than perfect syntax.


---------

==`SaveAsync` **returns** `Task`**, not** `Task<T>` — meaning it doesn’t return a value. It just signals “done.”== So there’s nothing to `return`.
Here’s the distinction:
```csharp
// SaveAsync returns Task (no value) — just await it
public async Task DeactivatePatientAsync(int id)
{
    // ...
    await _repo.SaveAsync(patient);  // no return needed
}

// FindAsync returns Task<Patient?> (a value) — you CAN return it
public async Task<Patient?> GetByIdAsync(int id)
{
    return await _repo.FindAsync(id);  // returns the Patient
}
```

**When would you use** `return await`**?**
Only when the method has a return type (`Task<T>`) and you need to pass a value back to the caller:
```csharp
// return await — because the caller needs the Order back
public async Task<Order> CreateOrderAsync(OrderDto dto)
{
    var order = new Order(dto);
    await _repo.SaveAsync(order);
    return await _repo.GetByIdAsync(order.Id);  // caller gets the saved order
}
```

**And when can you skip** `async/await` **entirely?**
When you’re just passing through a single call with no logic after it:
```csharp
// No async/await needed — just forward the Task
public Task<Patient?> GetByIdAsync(int id)
    => _repo.FindAsync(id);

// BUT if you have logic after the await, you need async/await:
public async Task<Patient> GetOrThrowAsync(int id)
{
    var p = await _repo.FindAsync(id);   // need the result HERE
    return p ?? throw new Exception();   // to do this check
}
```
**TL;DR:** `DeactivatePatientAsync` returns `Task` (void-equivalent in async). There’s no value to return. The `await` just ensures the save completes before the method finishes.

==A `Task` is C#‘s way of representing **work that will complete in the future**.==