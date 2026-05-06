## Empower Pharmacy Interview Prep

---

## Exercise 1: Design a DbContext with Relationships
**Difficulty:** ⭐⭐⭐ | **Time:** 20 min | **Topics:** DbContext, Fluent API, Relationships

### Problem

Design a `PharmacyDbContext` for this domain:

- **Patient** has many **Prescriptions**
- Each **Prescription** has one **Medication** and one **Pharmacist**
- Each **Prescription** has many **RefillHistory** records
- A **Pharmacist** can fill many **Prescriptions**

Requirements:
1. Use Fluent API (not annotations) for all configuration
2. Use `IEntityTypeConfiguration<T>` (separate config classes)
3. Add appropriate indexes on foreign keys and Status fields
4. Prevent cascade delete from Patient → Prescriptions (use Restrict)
5. Add a soft-delete global query filter on Patient

---

> [!success]- Solution (click to expand)
>
> ```csharp
> // === Entities ===
> public class Patient
> {
>     public int Id { get; set; }
>     public string Name { get; set; } = string.Empty;
>     public string Email { get; set; } = string.Empty;
>     public string State { get; set; } = string.Empty;
>     public bool IsDeleted { get; set; }
>     public DateTime CreatedAt { get; set; }
>     public List<Prescription> Prescriptions { get; set; } = new();
> }
>
> public class Pharmacist
> {
>     public int Id { get; set; }
>     public string Name { get; set; } = string.Empty;
>     public string LicenseNumber { get; set; } = string.Empty;
>     public List<Prescription> Prescriptions { get; set; } = new();
> }
>
> public class Medication
> {
>     public int Id { get; set; }
>     public string Name { get; set; } = string.Empty;
>     public string NdcCode { get; set; } = string.Empty;
>     public decimal UnitPrice { get; set; }
> }
>
> public class Prescription
> {
>     public int Id { get; set; }
>     public int PatientId { get; set; }
>     public Patient Patient { get; set; } = null!;
>     public int MedicationId { get; set; }
>     public Medication Medication { get; set; } = null!;
>     public int PharmacistId { get; set; }
>     public Pharmacist Pharmacist { get; set; } = null!;
>     public string Status { get; set; } = "Active";
>     public decimal Dosage { get; set; }
>     public int Quantity { get; set; }
>     public DateTime CreatedAt { get; set; }
>     public DateTime ExpiresAt { get; set; }
>     public List<RefillHistory> Refills { get; set; } = new();
> }
>
> public class RefillHistory
> {
>     public int Id { get; set; }
>     public int PrescriptionId { get; set; }
>     public Prescription Prescription { get; set; } = null!;
>     public DateTime RefilledAt { get; set; }
>     public int Quantity { get; set; }
> }
>
>
> // === Configuration ===
> public class PatientConfiguration : IEntityTypeConfiguration<Patient>
> {
>     public void Configure(EntityTypeBuilder<Patient> b)
>     {
>         b.HasKey(p => p.Id);
>         b.Property(p => p.Name).HasMaxLength(100).IsRequired();
>         b.Property(p => p.Email).HasMaxLength(200).IsRequired();
>         b.Property(p => p.State).HasMaxLength(2).IsRequired();
>         b.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
>
>         b.HasQueryFilter(p => !p.IsDeleted);  // soft delete
>
>         b.HasIndex(p => p.Email).IsUnique();
>         b.HasIndex(p => p.State);
>     }
> }
>
> public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
> {
>     public void Configure(EntityTypeBuilder<Prescription> b)
>     {
>         b.HasKey(rx => rx.Id);
>         b.Property(rx => rx.Status).HasMaxLength(20).IsRequired();
>         b.Property(rx => rx.Dosage).HasColumnType("decimal(8,2)");
>         b.Property(rx => rx.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
>
>         b.HasOne(rx => rx.Patient)
>             .WithMany(p => p.Prescriptions)
>             .HasForeignKey(rx => rx.PatientId)
>             .OnDelete(DeleteBehavior.Restrict);
>
>         b.HasOne(rx => rx.Medication)
>             .WithMany()
>             .HasForeignKey(rx => rx.MedicationId)
>             .OnDelete(DeleteBehavior.Restrict);
>
>         b.HasOne(rx => rx.Pharmacist)
>             .WithMany(ph => ph.Prescriptions)
>             .HasForeignKey(rx => rx.PharmacistId)
>             .OnDelete(DeleteBehavior.Restrict);
>
>         b.HasMany(rx => rx.Refills)
>             .WithOne(r => r.Prescription)
>             .HasForeignKey(r => r.PrescriptionId)
>             .OnDelete(DeleteBehavior.Cascade);
>
>         b.HasIndex(rx => rx.Status);
>         b.HasIndex(rx => rx.PatientId);
>         b.HasIndex(rx => new { rx.PatientId, rx.Status });
>     }
> }
>
>
> // === DbContext ===
> public class PharmacyDbContext : DbContext
> {
>     public DbSet<Patient> Patients => Set<Patient>();
>     public DbSet<Pharmacist> Pharmacists => Set<Pharmacist>();
>     public DbSet<Medication> Medications => Set<Medication>();
>     public DbSet<Prescription> Prescriptions => Set<Prescription>();
>     public DbSet<RefillHistory> RefillHistories => Set<RefillHistory>();
>
>     public PharmacyDbContext(DbContextOptions<PharmacyDbContext> opts)
>         : base(opts) { }
>
>     protected override void OnModelCreating(ModelBuilder mb)
>     {
>         mb.ApplyConfigurationsFromAssembly(
>             typeof(PharmacyDbContext).Assembly);
>     }
> }
> ```

---

## Exercise 2: Fix the N+1 Problem
**Difficulty:** ⭐⭐⭐ | **Time:** 15 min | **Topics:** Include, Select, Performance

### Problem

This code has an N+1 problem. It loads all orders, then triggers a separate query per order to get the patient name and item count. **Rewrite it three different ways:**

1. Using `Include` (eager loading)
2. Using `Select` projection (best approach)
3. Using `AsSplitQuery`

```csharp
// BAD CODE — Fix this
public async Task<List<OrderReportDto>> GetOrderReportAsync()
{
    var orders = await _db.Orders.ToListAsync();

    return orders.Select(o => new OrderReportDto
    {
        OrderId = o.Id,
        PatientName = o.Patient.Name,        // N+1!
        MedicationCount = o.Items.Count(),   // N+1!
        Total = o.Total,
        Status = o.Status
    }).ToList();
}
```

---

> [!success]- Solution (click to expand)
>
> ```csharp
> // === Fix 1: Include (eager loading) ===
> public async Task<List<OrderReportDto>> GetOrderReportV1Async()
> {
>     var orders = await _db.Orders
>         .Include(o => o.Patient)
>         .Include(o => o.Items)
>         .ToListAsync();
>
>     return orders.Select(o => new OrderReportDto
>     {
>         OrderId = o.Id,
>         PatientName = o.Patient.Name,
>         MedicationCount = o.Items.Count,
>         Total = o.Total,
>         Status = o.Status
>     }).ToList();
> }
> // Pro: Simple. Con: Loads full Patient and Item entities into memory.
>
>
> // === Fix 2: Select projection (BEST) ===
> public async Task<List<OrderReportDto>> GetOrderReportV2Async()
> {
>     return await _db.Orders
>         .Select(o => new OrderReportDto
>         {
>             OrderId = o.Id,
>             PatientName = o.Patient.Name,
>             MedicationCount = o.Items.Count(),
>             Total = o.Total,
>             Status = o.Status
>         })
>         .ToListAsync();
> }
> // Pro: Single query, only fetches needed columns, no tracking overhead.
> // This is usually the right answer.
>
>
> // === Fix 3: AsSplitQuery ===
> public async Task<List<OrderReportDto>> GetOrderReportV3Async()
> {
>     var orders = await _db.Orders
>         .Include(o => o.Patient)
>         .Include(o => o.Items)
>         .AsSplitQuery()    // 3 separate queries instead of 1 big JOIN
>         .ToListAsync();
>
>     return orders.Select(o => new OrderReportDto
>     {
>         OrderId = o.Id,
>         PatientName = o.Patient.Name,
>         MedicationCount = o.Items.Count,
>         Total = o.Total,
>         Status = o.Status
>     }).ToList();
> }
> // Pro: Avoids cartesian explosion. Con: Multiple round trips.
> ```

---

## Exercise 3: Repository Pattern with EF Core
**Difficulty:** ⭐⭐⭐⭐ | **Time:** 20 min | **Topics:** Generics, Repository, Unit of Work

### Problem

Build:
1. A generic `IRepository<T>` interface with CRUD + `FindAsync(predicate)`
2. A generic `Repository<T>` implementation using EF Core
3. A specific `IPrescriptionRepository` that extends it with `GetExpiringAsync(int days)`
4. An `IUnitOfWork` that exposes repositories and a single `SaveChangesAsync`

---

> [!success]- Solution (click to expand)
>
> ```csharp
> // === Generic Repository ===
> public interface IRepository<T> where T : class
> {
>     Task<T?> GetByIdAsync(int id);
>     Task<List<T>> GetAllAsync();
>     Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);
>     void Add(T entity);
>     void Update(T entity);
>     void Remove(T entity);
> }
>
> public class Repository<T> : IRepository<T> where T : class
> {
>     protected readonly PharmacyDbContext _db;
>
>     public Repository(PharmacyDbContext db) => _db = db;
>
>     public async Task<T?> GetByIdAsync(int id)
>         => await _db.Set<T>().FindAsync(id);
>
>     public async Task<List<T>> GetAllAsync()
>         => await _db.Set<T>().ToListAsync();
>
>     public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
>         => await _db.Set<T>().Where(predicate).ToListAsync();
>
>     public void Add(T entity) => _db.Set<T>().Add(entity);
>     public void Update(T entity) => _db.Set<T>().Update(entity);
>     public void Remove(T entity) => _db.Set<T>().Remove(entity);
> }
>
>
> // === Specific Repository ===
> public interface IPrescriptionRepository : IRepository<Prescription>
> {
>     Task<List<Prescription>> GetExpiringAsync(int days);
>     Task<List<Prescription>> GetByPatientAsync(int patientId);
> }
>
> public class PrescriptionRepository
>     : Repository<Prescription>, IPrescriptionRepository
> {
>     public PrescriptionRepository(PharmacyDbContext db) : base(db) { }
>
>     public async Task<List<Prescription>> GetExpiringAsync(int days)
>         => await _db.Prescriptions
>             .Include(rx => rx.Patient)
>             .Include(rx => rx.Medication)
>             .Where(rx => rx.Status == "Active"
>                       && rx.ExpiresAt <= DateTime.UtcNow.AddDays(days))
>             .OrderBy(rx => rx.ExpiresAt)
>             .ToListAsync();
>
>     public async Task<List<Prescription>> GetByPatientAsync(int patientId)
>         => await _db.Prescriptions
>             .Include(rx => rx.Medication)
>             .Where(rx => rx.PatientId == patientId)
>             .OrderByDescending(rx => rx.CreatedAt)
>             .ToListAsync();
> }
>
>
> // === Unit of Work ===
> public interface IUnitOfWork : IDisposable
> {
>     IRepository<Patient> Patients { get; }
>     IPrescriptionRepository Prescriptions { get; }
>     IRepository<Medication> Medications { get; }
>     Task<int> SaveChangesAsync();
> }
>
> public class UnitOfWork : IUnitOfWork
> {
>     private readonly PharmacyDbContext _db;
>
>     public IRepository<Patient> Patients { get; }
>     public IPrescriptionRepository Prescriptions { get; }
>     public IRepository<Medication> Medications { get; }
>
>     public UnitOfWork(PharmacyDbContext db)
>     {
>         _db = db;
>         Patients = new Repository<Patient>(db);
>         Prescriptions = new PrescriptionRepository(db);
>         Medications = new Repository<Medication>(db);
>     }
>
>     public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
>     public void Dispose() => _db.Dispose();
> }
>
>
> // === Program.cs ===
> builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
>
>
> // === Usage in a service ===
> public class PrescriptionService
> {
>     private readonly IUnitOfWork _uow;
>
>     public PrescriptionService(IUnitOfWork uow) => _uow = uow;
>
>     public async Task RefillAsync(int rxId)
>     {
>         var rx = await _uow.Prescriptions.GetByIdAsync(rxId)
>             ?? throw new NotFoundException("Prescription", rxId);
>
>         rx.Refills.Add(new RefillHistory
>         {
>             RefilledAt = DateTime.UtcNow,
>             Quantity = rx.Quantity
>         });
>
>         await _uow.SaveChangesAsync(); // single transaction
>     }
> }
> ```

---

## Exercise 4: Bulk Operations and Performance
**Difficulty:** ⭐⭐⭐⭐ | **Time:** 15 min | **Topics:** ExecuteUpdate, ExecuteDelete, Batch

### Problem

You have 50,000 prescription records. Write efficient queries for:

1. **Expire** all Active prescriptions where ExpiresAt is in the past (set Status = "Expired") — WITHOUT loading entities
2. **Purge** audit log records older than 2 years — WITHOUT loading entities
3. **Generate a report** of prescriptions-per-state with total revenue — using projection, not Include
4. **Find the top 5 medications** by number of prescriptions filled this month

---

> [!success]- Solution (click to expand)
>
> ```csharp
> // 1. Bulk expire — single UPDATE, no entity loading
> var expiredCount = await _db.Prescriptions
>     .Where(rx => rx.Status == "Active"
>                && rx.ExpiresAt < DateTime.UtcNow)
>     .ExecuteUpdateAsync(s => s
>         .SetProperty(rx => rx.Status, "Expired"));
> // Generates: UPDATE Prescriptions SET Status = 'Expired'
> //            WHERE Status = 'Active' AND ExpiresAt < @now
>
>
> // 2. Bulk purge — single DELETE
> var deletedCount = await _db.AuditLogs
>     .Where(l => l.CreatedAt < DateTime.UtcNow.AddYears(-2))
>     .ExecuteDeleteAsync();
>
>
> // 3. Report with projection — no Include needed
> var stateReport = await _db.Prescriptions
>     .GroupBy(rx => rx.Patient.State)
>     .Select(g => new
>     {
>         State = g.Key,
>         PrescriptionCount = g.Count(),
>         TotalRevenue = g.Sum(rx => rx.Medication.UnitPrice * rx.Quantity),
>         ActiveCount = g.Count(rx => rx.Status == "Active")
>     })
>     .OrderByDescending(x => x.TotalRevenue)
>     .ToListAsync();
>
>
> // 4. Top 5 medications this month
> var startOfMonth = new DateTime(
>     DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
>
> var topMeds = await _db.Prescriptions
>     .Where(rx => rx.CreatedAt >= startOfMonth)
>     .GroupBy(rx => rx.Medication.Name)
>     .Select(g => new
>     {
>         MedicationName = g.Key,
>         TimesPrescibed = g.Count(),
>         TotalQuantity = g.Sum(rx => rx.Quantity)
>     })
>     .OrderByDescending(x => x.TimesPrescibed)
>     .Take(5)
>     .ToListAsync();
> ```

---

## Exercise 5: Concurrency and Transactions
**Difficulty:** ⭐⭐⭐⭐ | **Time:** 20 min | **Topics:** RowVersion, Transactions, Conflict handling

### Problem

Build a `TransferPrescriptionAsync` method that:

1. Moves a prescription from one pharmacist to another
2. Uses optimistic concurrency (RowVersion) to detect conflicts
3. Wraps the transfer + audit log insert in an explicit transaction
4. Handles `DbUpdateConcurrencyException` with a meaningful error
5. Returns the updated prescription

---

> [!success]- Solution (click to expand)
>
> ```csharp
> public class PrescriptionTransferService
> {
>     private readonly PharmacyDbContext _db;
>     private readonly ILogger<PrescriptionTransferService> _logger;
>
>     public PrescriptionTransferService(
>         PharmacyDbContext db,
>         ILogger<PrescriptionTransferService> logger)
>     {
>         _db = db;
>         _logger = logger;
>     }
>
>     public async Task<Prescription> TransferAsync(
>         int prescriptionId, int newPharmacistId)
>     {
>         // Validate new pharmacist exists
>         var pharmacist = await _db.Pharmacists.FindAsync(newPharmacistId)
>             ?? throw new NotFoundException("Pharmacist", newPharmacistId);
>
>         // Use explicit transaction for multi-step operation
>         await using var transaction = await _db.Database
>             .BeginTransactionAsync();
>
>         try
>         {
>             var rx = await _db.Prescriptions
>                 .Include(r => r.Pharmacist)
>                 .FirstOrDefaultAsync(r => r.Id == prescriptionId)
>                 ?? throw new NotFoundException("Prescription", prescriptionId);
>
>             var oldPharmacistId = rx.PharmacistId;
>             var oldPharmacistName = rx.Pharmacist.Name;
>
>             // Update the prescription
>             rx.PharmacistId = newPharmacistId;
>
>             // Create audit record
>             _db.Set<PrescriptionAuditLog>().Add(new PrescriptionAuditLog
>             {
>                 PrescriptionId = prescriptionId,
>                 Action = "TRANSFER",
>                 Details = $"Transferred from {oldPharmacistName} " +
>                           $"to {pharmacist.Name}",
>                 CreatedAt = DateTime.UtcNow
>             });
>
>             await _db.SaveChangesAsync();
>             await transaction.CommitAsync();
>
>             _logger.LogInformation(
>                 "Prescription {RxId} transferred from pharmacist {Old} to {New}",
>                 prescriptionId, oldPharmacistId, newPharmacistId);
>
>             return rx;
>         }
>         catch (DbUpdateConcurrencyException)
>         {
>             await transaction.RollbackAsync();
>             throw new ConflictException(
>                 "Prescription was modified by another user. " +
>                 "Please refresh and try again.");
>         }
>         catch
>         {
>             await transaction.RollbackAsync();
>             throw;
>         }
>     }
> }
> ```

---

## Recommended Practice Order

1. **Exercise 2** (N+1 Fix) — most common performance question
2. **Exercise 4** (Bulk Operations) — shows you know EF 7+ features
3. **Exercise 1** (DbContext Design) — full model setup from scratch
4. **Exercise 3** (Repository + UoW) — architectural pattern Dave uses
5. **Exercise 5** (Concurrency) — production-level safety

> [!tip] During the Interview
> When Dave asks about EF Core, lead with: "The first thing I check is whether queries use `.Select()` projection or `.Include()` — projection is almost always more efficient." This signals you think about performance first.