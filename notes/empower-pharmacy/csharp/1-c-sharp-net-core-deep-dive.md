## Every concept Dave might test, with code examples

---
## 1. Dependency Injection

> [!tip] Why Dave cares
> DI is the backbone of every modern .NET app. At Empower, services talk to multiple data stores, external APIs, and compliance layers. Proper DI makes all of that testable and swappable.

DI means a class **receives** its dependencies from the outside rather than creating them itself.
### The Problem — Tightly Coupled Code

```csharp
public class OrderService
{
    // BAD: This class creates its own dependency
    private readonly SqlOrderRepository _repo = new SqlOrderRepository();

    public Order GetOrder(int id) => _repo.Find(id);
}
// Can't swap for test fake. Can't switch to MongoDB. OrderService "knows" about SQL.
```

### The Fix — Constructor Injection

```csharp
// Step 1: Define a contract
public interface IOrderRepository
{
    Task<Order?> FindAsync(int id);
    Task<List<Order>> GetAllAsync();
    Task SaveAsync(Order order);
}

// Step 2: Depend on the interface
public class OrderService
{
    private readonly IOrderRepository _repo;
    public OrderService(IOrderRepository repo)
    {
        _repo = repo;
    }
    public async Task<Order?> GetOrderAsync(int id)
        => await _repo.FindAsync(id);
}
// Step 3: Register in Program.cs
builder.Services.AddScoped<IOrderRepository, SqlOrderRepository>();
builder.Services.AddScoped<OrderService>();
```
### The Three Lifetimes

| ==Lifetime==       | ==Behavior==                              | ==Use When==                                          |
| ------------------ | ----------------------------------------- | ----------------------------------------------------- |
| ==`AddTransient`== | ==New instance **every time** requested== | ==Lightweight, stateless (validators)==               |
| ==`AddScoped`==    | ==One instance **per HTTP request**==     | ==Most services, repos, DbContext — **the default**== |
| ==`AddSingleton`== | ==One instance for **app lifetime**==     | ==Caches, config, HttpClient factories==              |

> [!warning] Trap
==Injecting a Scoped service into a Singleton causes a "captured dependency" — the Scoped service lives forever. ASP.NET Core throws `InvalidOperationException` in Development mode.==

---

## 2. Async / Await

> [!tip] Why Dave cares
> Empower handles 15,000+ prescriptions daily. Blocking threads kills throughput. Every DB call, API call, and file read should be async.

==`async` marks a method as asynchronous. `await` pauses execution until a task completes — **without blocking the thread**.==
### Basic Pattern

```csharp
public async Task<OrderDto> GetOrderAsync(int id)
{
    // await pauses here — thread is FREE to handle other requests
    var order = await _repo.FindAsync(id);
    if (order is null)
        throw new NotFoundException($"Order {id} not found");
    var items = await _itemRepo.GetByOrderIdAsync(order.Id);
    return new OrderDto(order.Id, order.Patient, items);
}
```
### Skip async/await When Just Passing Through

```csharp
// No logic after the call — return Task directly
public Task<Order?> FindAsync(int id)
    => _context.Orders.FindAsync(id).AsTask();
// Logic after the call — need async/await
public async Task<Order> FindOrThrowAsync(int id)
{
    var order = await _context.Orders.FindAsync(id);
    return order ?? throw new NotFoundException();
}
```

### Parallel Async — When Order Doesn't Matter

```csharp
public async Task<DashboardDto> GetDashboardAsync(int userId)
{
    // Start both WITHOUT awaiting
    var ordersTask = _orderRepo.GetByUserAsync(userId);
    var alertsTask = _alertService.GetActiveAsync(userId);
    // Await both — they run concurrently
    await Task.WhenAll(ordersTask, alertsTask);
    return new DashboardDto(ordersTask.Result, alertsTask.Result);
}
```
### Do / Don't

| ✅ Do                          | ❌ Don't                            |
| ----------------------------- | ---------------------------------- |
| `await SomethingAsync()`      | `.Result` — deadlocks in ASP.NET   |
| `async Task<T> MethodAsync()` | `.Wait()` — same problem           |
| `Task.WhenAll()` for parallel | `async void` — swallows exceptions |

---
## 3. LINQ
> [!tip] Why Dave cares
> His work at Orion involved data flows between multiple applications. LINQ is how .NET developers filter, transform, and aggregate data.
### The Essentials
```csharp
var orders = new List<Order> { /* ... */ };

// Where = filter
var completed = orders.Where(o => o.Status == Status.Completed);

// Select = transform / project
var names = orders.Select(o => o.PatientName);

// OrderBy / OrderByDescending = sort
var recent = orders.OrderByDescending(o => o.CreatedAt);

// First / FirstOrDefault = single item
var latest = orders.OrderByDescending(o => o.CreatedAt).FirstOrDefault();

// Any = boolean check
bool hasPending = orders.Any(o => o.Status == Status.Pending);
```

### GroupBy — The Tricky One

```csharp
// "How many orders per pharmacist, and total revenue?"
var summary = orders
    .Where(o => o.Status == Status.Completed)
    .GroupBy(o => o.PharmacistId)
    .Select(g => new
    {
        PharmacistId = g.Key,
        Count = g.Count(),
        Revenue = g.Sum(o => o.Total),
        Avg = g.Average(o => o.Total)
    })
    .OrderByDescending(x => x.Revenue)
    .ToList();
```
### SelectMany — Flatten Nested Collections

```csharp
// Each order has Items. Get ALL items across ALL orders:
var allItems = orders.SelectMany(o => o.Items).ToList();
// Distinct medications ordered this month
var meds = orders
    .Where(o => o.CreatedAt.Month == DateTime.Now.Month)
    .SelectMany(o => o.Items)
    .Select(i => i.MedicationName)
    .Distinct()
    .OrderBy(name => name)
    .ToList();
```

> [!warning] IEnumerable vs IQueryable
==With `IQueryable` (EF Core), LINQ translates to SQL — filtering in the database. With `IEnumerable`, filtering happens in memory. Calling `.ToList()` too early pulls the entire table into memory — a classic perf killer.==

---
## 4. REST API Design
### Controller-Based Approach
```csharp
[ApiController]
[Route("api/[controller]")]
public class PrescriptionsController : ControllerBase
{
    private readonly IPrescriptionService _service;

    public PrescriptionsController(IPrescriptionService service)
        => _service = service;

    // GET api/prescriptions/42
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var rx = await _service.GetByIdAsync(id);
        if (rx is null) return NotFound();
        return Ok(rx);
    }

    // POST api/prescriptions
    [HttpPost]
    public async Task<IActionResult> Create(CreateRxDto dto)
    {
        var id = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    // PUT api/prescriptions/42
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateRxDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return NoContent(); // 204
    }

    // DELETE api/prescriptions/42
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
```
### DTO with Validation

```csharp
public record CreateRxDto(
    [Required] int PatientId,
    [Required, StringLength(200)] string MedicationName,
    [Range(1, 1000)] int Quantity,
    string? Notes
);
```
### Status Codes

| Code                  | When                                          |
| --------------------- | --------------------------------------------- |
| ==`200 OK`==          | ==Successful GET or PUT==                     |
| ==`201 Created`==     | ==Successful POST — include Location header== |
| ==`204 No Content`==  | ==Successful DELETE or PUT with no body==     |
| ==`400 Bad Request`== | ==Validation failed==                         |
| ==`404 Not Found`==   | ==Resource doesn't exist==                    |

---
## 5. Interfaces vs Abstract Classes
### Interface — A Contract
```csharp
public interface INotificationService
{
    Task SendAsync(string recipient, string message);
}
// Multiple unrelated classes implement it
public class EmailNotifier : INotificationService { /* ... */ }
public class SmsNotifier : INotificationService { /* ... */ }
// A class can implement MULTIPLE interfaces
public class OrderService : IOrderService, IDisposable { /* ... */ }
```
### Abstract Class — Shared Behavior
```csharp
public abstract class Medication
{
    public string Name { get; set; }
    public decimal Dosage { get; set; }
    // Shared — all subclasses get this
    public string GetLabel() => $"{Name} {Dosage}mg";
    // Abstract — subclasses MUST implement
    public abstract decimal CalculatePrice(int quantity);
}

public class CompoundedMedication : Medication
{
    public decimal CompoundingFee { get; set; }
    public override decimal CalculatePrice(int qty)
        => (Dosage * 0.5m * qty) + CompoundingFee;
}
```

**Rule of thumb:** ==Interface when unrelated classes share a capability. Abstract class when related classes share behavior and state.==

---
## 6. Value Types vs Reference Types
### ==Value Types — Copied on Assignment==
```csharp hl:5,2
int a = 10;
int b = a;      // b gets a COPY
b = 20;
Console.WriteLine(a);  // 10 — unchanged
// Value types: int, double, bool, char, decimal, struct, enum, DateTime, Guid
```
### ==Reference Types — Shared on Assignment==
```csharp hl:5,2
var order1 = new Order { Total = 100 };
var order2 = order1;    // both point to SAME object
order2.Total = 999;
Console.WriteLine(order1.Total);  // 999 — both changed!
// Reference types: class, string (immutable!), array, delegate, record
```

> [!warning] Trap
==`string` is a reference type but behaves like a value type because it's *immutable*. Every "modification" creates a new string. For heavy string manipulation, use `StringBuilder`.==

---
## 7. Generics
### Generic Method
```csharp
public T GetOrDefault<T>(Dictionary<string, T> dict, string key, T fallback)
{
    return dict.TryGetValue(key, out var value) ? value : fallback;
}

// Compiler infers T
var price = GetOrDefault(prices, "aspirin", 0m);       // T = decimal
var name = GetOrDefault(names, "patient42", "Unknown"); // T = string
```
### Generic Repository Pattern
```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync();
    Task AddAsync(T entity);
}
public class Repository<T> : IRepository<T> where T : class
{
    private readonly AppDbContext _ctx;
    public Repository(AppDbContext ctx) => _ctx = ctx;
    public async Task<T?> GetByIdAsync(int id)
        => await _ctx.Set<T>().FindAsync(id);
    public async Task<List<T>> GetAllAsync()
        => await _ctx.Set<T>().ToListAsync();
    public async Task AddAsync(T entity)
    {
        _ctx.Set<T>().Add(entity);
        await _ctx.SaveChangesAsync();
    }
}
```

**Common constraints:** `class` (reference type), `struct` (value type), `new()` (parameterless constructor), `IComparable` (implements interface).

---

## 8. Entity Framework Core

### DbContext

```csharp
public class PharmacyDbContext : DbContext
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Order> Orders => Set<Order>();

    public PharmacyDbContext(DbContextOptions<PharmacyDbContext> opts)
        : base(opts) { }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Order>(e =>
        {
            e.HasOne(o => o.Patient)
             .WithMany(p => p.Orders)
             .HasForeignKey(o => o.PatientId);
        });
    }
}
```

### Querying

```csharp
// Eager loading with Include
var order = await _ctx.Orders
    .Include(o => o.Patient)
    .Include(o => o.Items)
    .FirstOrDefaultAsync(o => o.Id == id);

// Projection — only fetch what you need
var summaries = await _ctx.Orders
    .Where(o => o.CreatedAt >= startDate)
    .Select(o => new OrderSummaryDto
    {
        Id = o.Id,
        PatientName = o.Patient.Name,
        ItemCount = o.Items.Count(),
        Total = o.Total
    })
    .ToListAsync(); // SQL executes HERE
```

> [!warning] N+1 Problem
==Without `.Include()`, accessing `order.Patient` in a loop triggers a separate SQL query per order. Use `.Include()` for eager loading, or `.Select()` to project — often the better approach.==

---
## 9. Middleware Pipeline
Every HTTP request passes through middleware in registration order. Each can process, pass to next, or short-circuit.
```csharp
// Program.cs — ORDER MATTERS
var app = builder.Build();

app.UseExceptionHandler("/error");  // 1. Catch exceptions
app.UseHttpsRedirection();           // 2. HTTP → HTTPS
app.UseAuthentication();              // 3. Who are you?
app.UseAuthorization();               // 4. Are you allowed?
app.MapControllers();                 // 5. Route to controller

// Request flows DOWN: 1 → 2 → 3 → 4 → 5
// Response flows UP:  5 → 4 → 3 → 2 → 1
```
### Custom Middleware
```csharp
public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(RequestDelegate next,
        ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        await _next(context);  // call next middleware
        sw.Stop();

        _logger.LogInformation("{Method} {Path} → {Status} in {Ms}ms",
            context.Request.Method, context.Request.Path,
            context.Response.StatusCode, sw.ElapsedMilliseconds);
    }
}

// Register
app.UseMiddleware<RequestTimingMiddleware>();
```
---
## 10. Unit Testing

> [!tip] Why Dave cares
> He "championed unit testing efforts" at Orion using FakeItEasy. Testing is a core value for him.

### Arrange / Act / Assert

```csharp
[Fact]
public async Task GetOrderAsync_ReturnsOrder_WhenExists()
{
    // ARRANGE
    var fakeRepo = A.Fake<IOrderRepository>();
    var expected = new Order { Id = 1, Total = 99.99m };
    A.CallTo(() => fakeRepo.FindAsync(1)).Returns(expected);
    var service = new OrderService(fakeRepo);

    // ACT
    var result = await service.GetOrderAsync(1);

    // ASSERT
    Assert.NotNull(result);
    Assert.Equal(99.99m, result.Total);
}

[Fact]
public async Task GetOrderAsync_ReturnsNull_WhenNotFound()
{
    var fakeRepo = A.Fake<IOrderRepository>();
    A.CallTo(() => fakeRepo.FindAsync(999)).Returns((Order?)null);
    var service = new OrderService(fakeRepo);
    var result = await service.GetOrderAsync(999);
    Assert.Null(result);
}
```
### Making Code Testable
**Untestable** — hard-coded `new`:
```csharp
public class OrderService
{
    var repo = new SqlRepo();       // can't swap for fake
    var logger = new FileLogger();  // can't swap for fake
}
```

**Testable** — injected interfaces:
```csharp
public class OrderService
{
    public OrderService(IOrderRepo repo, ILogger logger) { }
}
```

> [!info] Note
> Dave uses FakeItEasy, but concepts are the same in Moq or NSubstitute. Pattern is always: create fake → configure behavior → inject → assert.