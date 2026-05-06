## ORM internals, patterns, and performance — interview ready

---

## 1. DbContext — Your Database Session

DbContext is the central class. It represents a session with the database: tracks entities, manages connections, and translates LINQ to SQL.

```csharp
public class PharmacyDbContext : DbContext
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Medication> Medications => Set<Medication>();
    public PharmacyDbContext(DbContextOptions<PharmacyDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(PharmacyDbContext).Assembly);
    }
}
// Registration in Program.cs
builder.Services.AddDbContext<PharmacyDbContext>(opts =>
opts.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
```

> [!warning] DbContext lifetime
> DbContext is registered as **Scoped** by default — one instance per HTTP request. NEVER register it as Singleton (it's not thread-safe). In a BackgroundService, create a new scope: `provider.CreateScope()`.

### DbContext is a Unit of Work

```csharp
// All changes are tracked in memory until SaveChanges
var patient = await _db.Patients.FindAsync(42);
patient.Name = "Updated Name";       // tracked automatically
_db.Orders.Add(new Order { ... });    // tracked as Added

await _db.SaveChangesAsync();         // ONE transaction wraps ALL changes
// If either fails, both roll back
```

---

## 2. Entity Configuration — Fluent API vs Data Annotations

### Data Annotations (on the entity class)

```csharp
public class Patient
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(2)]
    public string State { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public List<Order> Orders { get; set; } = new();
}
```

### Fluent API (separate configuration class — preferred for complex models)

```csharp hl:1,3
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Total)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(o => o.Status)
            .HasMaxLength(20)
            .HasDefaultValue("Pending");

        // Relationships
        builder.HasOne(o => o.Patient)
            .WithMany(p => p.Orders)
            .HasForeignKey(o => o.PatientId)
            .OnDelete(DeleteBehavior.Restrict);  // prevent cascade delete

        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);   // delete items when order deleted

        // Indexes
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.CreatedAt);
        builder.HasIndex(o => new { o.PatientId, o.Status });  // composite
    }
}
```

> [!tip] Interview answer
> "I prefer Fluent API for anything beyond simple required/length validations. It keeps entity classes clean and puts all database concerns in one place. Data Annotations are fine for simple DTOs."

---

## 3. Relationships

### One-to-Many (most common)

```csharp
// A Patient has many Orders
public class Patient
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<Order> Orders { get; set; } = new();  // collection nav
}

public class Order
{
    public int Id { get; set; }
    public int PatientId { get; set; }      // FK property
    public Patient Patient { get; set; }     // reference nav
}

// Fluent API (EF can infer this, but explicit is clearer)
builder.HasOne(o => o.Patient)
    .WithMany(p => p.Orders)
    .HasForeignKey(o => o.PatientId);
```

### Many-to-Many

```csharp
// A Medication can appear in many OrderItems, an Order has many OrderItems
// Use a join entity (explicit) for extra properties on the relationship
public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }
    public int MedicationId { get; set; }
    public Medication Medication { get; set; }
    public int Quantity { get; set; }        // extra data on the join
    public decimal LineTotal { get; set; }
}
```

### One-to-One

```csharp
public class Patient
{
    public int Id { get; set; }
    public PatientProfile? Profile { get; set; }  // optional 1:1
}

public class PatientProfile
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; }
    public string Allergies { get; set; }
}

builder.HasOne(p => p.Profile)
    .WithOne(pr => pr.Patient)
    .HasForeignKey<PatientProfile>(pr => pr.PatientId);
```

---

## 4. Querying — LINQ to Entities

### Basic Queries

```csharp
// Find by primary key (uses cache first, then DB)
var patient = await _db.Patients.FindAsync(42);

// Single item by condition
var order = await _db.Orders
    .FirstOrDefaultAsync(o => o.Id == orderId);

// Filtered list
var pendingOrders = await _db.Orders
    .Where(o => o.Status == "Pending")
    .OrderByDescending(o => o.CreatedAt)
    .ToListAsync();
```

### Eager Loading with Include

```csharp
// Load related data in the SAME query (SQL JOIN)
var order = await _db.Orders
    .Include(o => o.Patient)                // one level
    .Include(o => o.Items)                  // collection
        .ThenInclude(i => i.Medication)     // nested
    .FirstOrDefaultAsync(o => o.Id == id);
```

### Projection with Select (often better than Include)

```csharp
// Only fetch the columns you need — generates leaner SQL
var summaries = await _db.Orders
    .Where(o => o.CreatedAt >= startDate)
    .Select(o => new OrderSummaryDto
    {
        Id = o.Id,
        PatientName = o.Patient.Name,       // EF auto-joins
        ItemCount = o.Items.Count(),         // translated to SQL COUNT
        Total = o.Total,
        Status = o.Status
    })
    .ToListAsync();
// No Include needed — EF joins automatically in projections
```

```csharp hl:3,6-7,9,11
N+1 Problem
Without `.Include()` or `.Select()`, accessing `order.Patient` inside a loop triggers a **separate SQL query per iteration**. This is the #1 EF performance killer.
> // BAD: N+1 — each .Patient triggers a query
> var orders = await _db.Orders.ToListAsync();
> foreach (var o in orders)
>     Console.WriteLine(o.Patient.Name);  // 💥 query per order
> // GOOD: Single query with Include
> var orders = await _db.Orders.Include(o => o.Patient).ToListAsync();
> // BETTER: Projection — only fetches needed columns
> var orders = await _db.Orders
>     .Select(o => new { o.Id, PatientName = o.Patient.Name })
>     .ToListAsync();
```

---
## 5. Change Tracking
EF Core tracks every entity it loads. When you call `SaveChanges`, it compares current values to original values and generates SQL for changes.
### Entity States

| State       | Meaning                            | What SaveChanges does |
| ----------- | ---------------------------------- | --------------------- |
| `Added`     | New entity, no DB row yet          | INSERT                |
| `Modified`  | Loaded from DB, properties changed | UPDATE                |
| `Deleted`   | Marked for deletion                | DELETE                |
| `Unchanged` | Loaded from DB, no changes         | Nothing               |
| `Detached`  | Not tracked by this context        | Nothing               |

```csharp hl:9,7,1,5
// EF tracks changes automatically
var patient = await _db.Patients.FindAsync(42);  // Unchanged
patient.Name = "New Name";                        // Modified
await _db.SaveChangesAsync();                     // UPDATE
// Explicit state control
_db.Entry(patient).State = EntityState.Modified;
// No-tracking queries (read-only — faster)
var patients = await _db.Patients
    .AsNoTracking()                               // Detached
    .Where(p => p.IsActive)
    .ToListAsync();
```

> [!tip] Performance rule
> Use `AsNoTracking()` for read-only queries (lists, reports, dashboards). It skips change detection and uses less memory. Only track entities you intend to modify.

---

## 6. Migrations

```bash
# Create a migration
dotnet ef migrations add AddOrderStatus

# Apply to database
dotnet ef database update

# Generate SQL script (for production deployments)
dotnet ef migrations script --idempotent -o migration.sql

# Revert last migration (before applying)
dotnet ef migrations remove
```

### Migration file structure

```csharp
public partial class AddOrderStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Status",
            table: "Orders",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "Pending");

        migrationBuilder.CreateIndex(
            name: "IX_Orders_Status",
            table: "Orders",
            column: "Status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_Orders_Status", "Orders");
        migrationBuilder.DropColumn("Status", "Orders");
    }
}
```

> [!warning] Production migrations
> Never run `dotnet ef database update` in production. Generate an idempotent SQL script and apply through your CI/CD pipeline (Azure DevOps at Empower). This gives you a reviewable, auditable deployment.

---

## 7. Raw SQL & Stored Procedures

```csharp
// Raw SQL query (returns entities)
var orders = await _db.Orders
    .FromSqlRaw("SELECT * FROM Orders WHERE Status = {0}", "Pending")
    .Include(o => o.Patient)
    .ToListAsync();

// Parameterized (safe from SQL injection)
var orders = await _db.Orders
    .FromSqlInterpolated(
        $"SELECT * FROM Orders WHERE PatientId = {patientId}")
    .ToListAsync();

// Non-query (INSERT, UPDATE, DELETE)
await _db.Database.ExecuteSqlInterpolatedAsync(
    $"UPDATE Orders SET Status = 'Cancelled' WHERE Id = {orderId}");

// Stored procedure
var results = await _db.Orders
    .FromSqlRaw("EXEC usp_GetOrdersByStatus @Status = {0}", "Pending")
    .ToListAsync();
```

---

## 8. Concurrency Control

### Optimistic Concurrency with RowVersion

```csharp
public class Order
{
    public int Id { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; }
    [Timestamp]
    public byte[] RowVersion { get; set; }  // auto-managed by SQL Server
}
// Fluent API
builder.Property(o => o.RowVersion).IsRowVersion();
```
### Handling concurrency conflicts
```csharp hl:11,18-20,22-24
public async Task UpdateOrderAsync(int id, UpdateOrderDto dto)
{
    var order = await _db.Orders.FindAsync(id)
        ?? throw new NotFoundException("Order", id);
    order.Status = dto.Status;
    order.Total = dto.Total;
    try
    {
        await _db.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException ex)
    {
        // Someone else modified this row since we loaded it
        var entry = ex.Entries.Single();
        var dbValues = await entry.GetDatabaseValuesAsync();
        if (dbValues is null)
            throw new ConflictException("Order was deleted by another user");
        // Option 1: "Last write wins" — overwrite
        entry.OriginalValues.SetValues(dbValues);
        await _db.SaveChangesAsync();
        
        // Option 2: Reject and tell the user
        throw new ConflictException(
            "Order was modified by another user. Please refresh and try again.");
    }
}
```

---
## 9. Global Query Filters (Soft Delete)

```csharp hl:8-11,14-15
// Entity
public class Patient
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsDeleted { get; set; }  // soft delete flag
}
// In OnModelCreating — applies to ALL queries automatically
builder.HasQueryFilter(p => !p.IsDeleted);
// Now this NEVER returns deleted patients:
var patients = await _db.Patients.ToListAsync();
// SQL: SELECT * FROM Patients WHERE IsDeleted = 0

// To bypass the filter (admin view):
var allPatients = await _db.Patients.IgnoreQueryFilters().ToListAsync();
```

> [!tip] Healthcare use
> Soft delete is critical for HIPAA. You can't truly delete patient records — you mark them deleted but preserve the data for audit and legal compliance. Global filters make this automatic.

---
## 10. Performance Patterns
### Batch operations (EF Core 7+)
```csharp
// ExecuteUpdate — bulk update without loading entities
await _db.Orders
    .Where(o => o.Status == "Pending"
             && o.CreatedAt < DateTime.UtcNow.AddDays(-30))
    .ExecuteUpdateAsync(s => s
        .SetProperty(o => o.Status, "Expired")
        .SetProperty(o => o.ModifiedAt, DateTime.UtcNow));
// Generates a single UPDATE statement — no entity loading

// ExecuteDelete — bulk delete without loading
await _db.AuditLogs
    .Where(l => l.CreatedAt < DateTime.UtcNow.AddYears(-1))
    .ExecuteDeleteAsync();
// Single DELETE statement
```
### Split queries (for complex Includes)
```csharp hl:1,6,8,12,14
// Problem: Include with large collections generates a cartesian explosion
var orders = await _db.Orders
    .Include(o => o.Items)
    .Include(o => o.AuditLogs)
    .ToListAsync();
// If order has 10 items and 20 logs, the JOIN returns 200 rows per order

// Solution: Split into separate queries
var orders = await _db.Orders
    .Include(o => o.Items)
    .Include(o => o.AuditLogs)
    .AsSplitQuery()                // runs separate SQL per Include
    .ToListAsync();
// 3 queries instead of 1 massive JOIN — often faster
```
### Compiled queries (hot paths)
```csharp hl:1-3,9-10
// Pre-compile the query for repeated use — skips LINQ translation overhead
private static readonly Func<PharmacyDbContext, int, Task<Order?>>
    GetOrderById = EF.CompileAsyncQuery(
        (PharmacyDbContext db, int id) =>
            db.Orders
                .Include(o => o.Patient)
                .FirstOrDefault(o => o.Id == id));

// Usage — faster on hot paths
var order = await GetOrderById(_db, orderId);
```
---
## 11. Quick-Fire Interview Q&A

### "DbContext: Scoped, Singleton, or Transient?"
==**Scoped.**== One per HTTP request. ==It's not thread-safe (no Singleton), and creating a new one per injection (Transient) wastes connections== and breaks change tracking across a request.
### =="When would you use AsNoTracking?"==
==Read-only queries== — lists, reports, search results. Anything you don't plan to modify. It skips the change tracker snapshot, uses less memory, and runs faster. Don't use it if you'll call SaveChanges on those entities.
### "How do you handle the N+1 problem?"
Three options: 
(1) `.Include()` for eager loading, 
(2) `.Select()` projection to fetch only needed columns, 
(3) explicit loading with `.Entry().Collection().LoadAsync()`.
I prefer `.Select()` — it's the most efficient because it only fetches what you need.
### "Fluent API or Data Annotations?"
Both work. Data Annotations for simple constraints (Required, MaxLength). Fluent API for relationships, indexes, composite keys, and anything complex. I prefer Fluent API with `IEntityTypeConfiguration<T>` to keep entity classes clean.
### "How do you deploy migrations to production?"
Generate an idempotent SQL script with `dotnet ef migrations script --idempotent`, review it, and apply through the CI/CD pipeline. Never run `database update` directly in production.
### "What's the difference between `Find` and `FirstOrDefault`?"
==`Find()` checks the local change tracker cache first, then queries the DB. `FirstOrDefault()` always queries the DB. Use `Find()` when you have the primary key and want cache hits. Use `FirstOrDefault()` with complex filters.==