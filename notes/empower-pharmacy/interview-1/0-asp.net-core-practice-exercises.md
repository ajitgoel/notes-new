# ASP.NET Core Practice Exercises
## Empower Pharmacy Interview Prep

---

## Exercise 1: Build a Complete API from Scratch
**Difficulty:** ⭐⭐⭐ | **Time:** 25 min | **Topics:** Program.cs, Controllers, DI, DTOs

### Problem

Build a complete Medication API with:

1. **Program.cs** that registers services, middleware, and a DbContext
2. **MedicationsController** with CRUD endpoints
3. **DTOs** with validation for creating and updating
4. **IMedicationService** interface + implementation
5. Proper status codes: 200, 201, 204, 400, 404

Write all the files you'd need. Don't worry about a real database — just define the DbContext and interface.

---

> [!success]- Solution (click to expand)
>
> ```csharp
> // === Program.cs ===
> var builder = WebApplication.CreateBuilder(args);
>
> builder.Services.AddControllers();
> builder.Services.AddDbContext<PharmacyDbContext>(opts =>
>     opts.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
> builder.Services.AddScoped<IMedicationService, MedicationService>();
>
> var app = builder.Build();
>
> app.UseExceptionHandler("/error");
> app.UseHttpsRedirection();
> app.MapControllers();
> app.Run();
>
>
> // === DTOs ===
> public record CreateMedicationDto(
>     [Required, StringLength(200)] string Name,
>     [Required, StringLength(20)] string NdcCode,
>     [Range(0.01, 10000)] decimal UnitPrice
> );
>
> public record UpdateMedicationDto(
>     [Required, StringLength(200)] string Name,
>     [Range(0.01, 10000)] decimal UnitPrice
> );
>
> public record MedicationDto(
>     int Id, string Name, string NdcCode, decimal UnitPrice
> );
>
>
> // === Interface ===
> public interface IMedicationService
> {
>     Task<MedicationDto?> GetByIdAsync(int id);
>     Task<List<MedicationDto>> GetAllAsync();
>     Task<int> CreateAsync(CreateMedicationDto dto);
>     Task UpdateAsync(int id, UpdateMedicationDto dto);
>     Task DeleteAsync(int id);
> }
>
>
> // === Controller ===
> [ApiController]
> [Route("api/[controller]")]
> public class MedicationsController : ControllerBase
> {
>     private readonly IMedicationService _service;
>
>     public MedicationsController(IMedicationService service)
>         => _service = service;
>
>     [HttpGet]
>     public async Task<IActionResult> GetAll()
>         => Ok(await _service.GetAllAsync());
>
>     [HttpGet("{id}")]
>     public async Task<IActionResult> GetById(int id)
>     {
>         var med = await _service.GetByIdAsync(id);
>         return med is null ? NotFound() : Ok(med);
>     }
>
>     [HttpPost]
>     public async Task<IActionResult> Create(CreateMedicationDto dto)
>     {
>         var id = await _service.CreateAsync(dto);
>         return CreatedAtAction(nameof(GetById), new { id }, new { id });
>     }
>
>     [HttpPut("{id}")]
>     public async Task<IActionResult> Update(int id, UpdateMedicationDto dto)
>     {
>         await _service.UpdateAsync(id, dto);
>         return NoContent();
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

## Exercise 2: Write Custom Middleware
**Difficulty:** ⭐⭐⭐ | **Time:** 15 min | **Topics:** Middleware, HttpContext, Logging

### Problem

Write two middlewares:

1. **CorrelationIdMiddleware** — checks if the request has an `X-Correlation-Id` header. If yes, use it. If not, generate a new GUID. Add the correlation ID to the response headers AND make it available to downstream services via `HttpContext.Items`.

2. **ApiKeyMiddleware** — checks for an `X-Api-Key` header. If it matches a configured key, allow the request. If missing or wrong, return `401 Unauthorized` and short-circuit the pipeline.

Register them in the correct order in Program.cs.

---

> [!success]- Solution (click to expand)
>
> ```csharp
> // === CorrelationIdMiddleware ===
> public class CorrelationIdMiddleware
> {
>     private readonly RequestDelegate _next;
>
>     public CorrelationIdMiddleware(RequestDelegate next) => _next = next;
>
>     public async Task InvokeAsync(HttpContext context)
>     {
>         var correlationId = context.Request.Headers["X-Correlation-Id"]
>             .FirstOrDefault() ?? Guid.NewGuid().ToString();
>
>         // Make available to downstream middleware and services
>         context.Items["CorrelationId"] = correlationId;
>
>         // Add to response headers
>         context.Response.OnStarting(() =>
>         {
>             context.Response.Headers["X-Correlation-Id"] = correlationId;
>             return Task.CompletedTask;
>         });
>
>         await _next(context);
>     }
> }
>
>
> // === ApiKeyMiddleware ===
> public class ApiKeyMiddleware
> {
>     private readonly RequestDelegate _next;
>     private readonly string _expectedKey;
>
>     public ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
>     {
>         _next = next;
>         _expectedKey = config["ApiKey"]
>             ?? throw new InvalidOperationException("ApiKey not configured");
>     }
>
>     public async Task InvokeAsync(HttpContext context)
>     {
>         // Skip health check endpoints
>         if (context.Request.Path.StartsWithSegments("/health"))
>         {
>             await _next(context);
>             return;
>         }
>
>         if (!context.Request.Headers.TryGetValue("X-Api-Key", out var key)
>             || key != _expectedKey)
>         {
>             context.Response.StatusCode = 401;
>             await context.Response.WriteAsJsonAsync(
>                 new { error = "Invalid or missing API key" });
>             return; // short-circuit — don't call _next
>         }
>
>         await _next(context);
>     }
> }
>
>
> // === Program.cs registration ===
> app.UseMiddleware<CorrelationIdMiddleware>();  // first: tag every request
> app.UseMiddleware<ApiKeyMiddleware>();          // second: block unauthorized
> app.UseAuthentication();
> app.UseAuthorization();
> app.MapControllers();
> ```

---

## Exercise 3: Options Pattern + Configuration
**Difficulty:** ⭐⭐ | **Time:** 10 min | **Topics:** Configuration, IOptions, Strong typing

### Problem

Given this `appsettings.json`:

```json
{
    "PharmacySettings": {
        "MaxOrdersPerPatientPerDay": 5,
        "DefaultExpirationDays": 90,
        "RequirePharmacistApproval": true,
        "AllowedStates": ["TX", "CA", "NY", "FL"]
    }
}
```

1. Create the strongly-typed options class
2. Register it in Program.cs
3. Inject and use it in a service that validates whether a new order is allowed

---

> [!success]- Solution (click to expand)
>
> ```csharp
> // === Options class ===
> public class PharmacySettings
> {
>     public int MaxOrdersPerPatientPerDay { get; set; }
>     public int DefaultExpirationDays { get; set; }
>     public bool RequirePharmacistApproval { get; set; }
>     public List<string> AllowedStates { get; set; } = new();
> }
>
>
> // === Program.cs ===
> builder.Services.Configure<PharmacySettings>(
>     builder.Configuration.GetSection("PharmacySettings"));
>
>
> // === Service using it ===
> public class OrderValidationService
> {
>     private readonly PharmacySettings _settings;
>     private readonly IOrderRepository _repo;
>
>     public OrderValidationService(
>         IOptions<PharmacySettings> options,
>         IOrderRepository repo)
>     {
>         _settings = options.Value;
>         _repo = repo;
>     }
>
>     public async Task<ValidationResult> ValidateNewOrderAsync(
>         int patientId, string state)
>     {
>         // Check state is allowed
>         if (!_settings.AllowedStates.Contains(state))
>             return ValidationResult.Fail(
>                 $"State '{state}' is not in the allowed list");
>
>         // Check daily order limit
>         var todayCount = await _repo.CountTodayOrdersAsync(patientId);
>         if (todayCount >= _settings.MaxOrdersPerPatientPerDay)
>             return ValidationResult.Fail(
>                 $"Patient has reached the daily limit of " +
>                 $"{_settings.MaxOrdersPerPatientPerDay} orders");
>
>         return ValidationResult.Success();
>     }
> }
> ```

---

## Exercise 4: Global Exception Handling
**Difficulty:** ⭐⭐⭐⭐ | **Time:** 20 min | **Topics:** Filters, Exception handling, Problem Details

### Problem

Build a global exception handling system:

1. Define custom exceptions: `NotFoundException`, `ValidationException`, `ConflictException`
2. Write an `IExceptionFilter` that catches these and returns proper HTTP responses:
   - `NotFoundException` → 404
   - `ValidationException` → 400 with error details
   - `ConflictException` → 409
   - Everything else → 500 with generic message (don't leak internal errors)
3. Log every exception with structured logging
4. Register it globally

---

> [!success]- Solution (click to expand)
>
> ```csharp
> // === Custom Exceptions ===
> public class NotFoundException : Exception
> {
>     public NotFoundException(string entity, object id)
>         : base($"{entity} with id '{id}' was not found") { }
> }
>
> public class ValidationException : Exception
> {
>     public IDictionary<string, string[]> Errors { get; }
>
>     public ValidationException(IDictionary<string, string[]> errors)
>         : base("One or more validation errors occurred")
>     {
>         Errors = errors;
>     }
> }
>
> public class ConflictException : Exception
> {
>     public ConflictException(string message) : base(message) { }
> }
>
>
> // === Exception Filter ===
> public class GlobalExceptionFilter : IExceptionFilter
> {
>     private readonly ILogger<GlobalExceptionFilter> _logger;
>     private readonly IHostEnvironment _env;
>
>     public GlobalExceptionFilter(
>         ILogger<GlobalExceptionFilter> logger,
>         IHostEnvironment env)
>     {
>         _logger = logger;
>         _env = env;
>     }
>
>     public void OnException(ExceptionContext context)
>     {
>         _logger.LogError(context.Exception,
>             "Unhandled exception on {Method} {Path}",
>             context.HttpContext.Request.Method,
>             context.HttpContext.Request.Path);
>
>         var (statusCode, response) = context.Exception switch
>         {
>             NotFoundException nf => (404, new ProblemDetails
>             {
>                 Status = 404,
>                 Title = "Not Found",
>                 Detail = nf.Message
>             } as object),
>
>             ValidationException ve => (400, new
>             {
>                 status = 400,
>                 title = "Validation Failed",
>                 errors = ve.Errors
>             } as object),
>
>             ConflictException ce => (409, new ProblemDetails
>             {
>                 Status = 409,
>                 Title = "Conflict",
>                 Detail = ce.Message
>             } as object),
>
>             _ => (500, new ProblemDetails
>             {
>                 Status = 500,
>                 Title = "Internal Server Error",
>                 Detail = _env.IsDevelopment()
>                     ? context.Exception.Message
>                     : "An unexpected error occurred"
>             } as object)
>         };
>
>         context.Result = new ObjectResult(response)
>             { StatusCode = statusCode };
>         context.ExceptionHandled = true;
>     }
> }
>
>
> // === Register globally in Program.cs ===
> builder.Services.AddControllers(opts =>
> {
>     opts.Filters.Add<GlobalExceptionFilter>();
> });
>
>
> // === Usage in a service ===
> public async Task<Order> GetOrderAsync(int id)
> {
>     return await _repo.GetByIdAsync(id)
>         ?? throw new NotFoundException("Order", id);
>     // Filter automatically returns:
>     // { "status": 404, "title": "Not Found",
>     //   "detail": "Order with id '42' was not found" }
> }
> ```

---

## Exercise 5: Background Service + Health Check
**Difficulty:** ⭐⭐⭐⭐ | **Time:** 20 min | **Topics:** BackgroundService, HealthChecks, DI Scopes

### Problem

Build a `PrescriptionExpirationService` that:

1. Runs every 5 minutes as a `BackgroundService`
2. Finds prescriptions expiring within 24 hours and marks them as "Expiring"
3. Logs how many prescriptions were updated each run
4. Has a corresponding `IHealthCheck` that reports unhealthy if the last run was more than 10 minutes ago

---

> [!success]- Solution (click to expand)
>
> ```csharp
> // === Shared state for health check ===
> public class ExpirationJobStatus
> {
>     public DateTime? LastRunAt { get; set; }
>     public int LastCount { get; set; }
> }
>
>
> // === Background Service ===
> public class PrescriptionExpirationService : BackgroundService
> {
>     private readonly IServiceProvider _provider;
>     private readonly ILogger<PrescriptionExpirationService> _logger;
>     private readonly ExpirationJobStatus _status;
>
>     public PrescriptionExpirationService(
>         IServiceProvider provider,
>         ILogger<PrescriptionExpirationService> logger,
>         ExpirationJobStatus status)
>     {
>         _provider = provider;
>         _logger = logger;
>         _status = status;
>     }
>
>     protected override async Task ExecuteAsync(CancellationToken ct)
>     {
>         while (!ct.IsCancellationRequested)
>         {
>             try
>             {
>                 using var scope = _provider.CreateScope();
>                 var db = scope.ServiceProvider
>                     .GetRequiredService<PharmacyDbContext>();
>
>                 var cutoff = DateTime.UtcNow.AddHours(24);
>                 var expiring = await db.Prescriptions
>                     .Where(p => p.ExpiresAt <= cutoff
>                              && p.Status == "Active")
>                     .ToListAsync(ct);
>
>                 foreach (var rx in expiring)
>                     rx.Status = "Expiring";
>
>                 await db.SaveChangesAsync(ct);
>
>                 _status.LastRunAt = DateTime.UtcNow;
>                 _status.LastCount = expiring.Count;
>
>                 _logger.LogInformation(
>                     "Expiration check complete: {Count} prescriptions marked",
>                     expiring.Count);
>             }
>             catch (Exception ex)
>             {
>                 _logger.LogError(ex, "Expiration check failed");
>             }
>
>             await Task.Delay(TimeSpan.FromMinutes(5), ct);
>         }
>     }
> }
>
>
> // === Health Check ===
> public class ExpirationJobHealthCheck : IHealthCheck
> {
>     private readonly ExpirationJobStatus _status;
>
>     public ExpirationJobHealthCheck(ExpirationJobStatus status)
>         => _status = status;
>
>     public Task<HealthCheckResult> CheckHealthAsync(
>         HealthCheckContext ctx, CancellationToken ct = default)
>     {
>         if (_status.LastRunAt is null)
>             return Task.FromResult(HealthCheckResult.Degraded(
>                 "Expiration job has not run yet"));
>
>         var elapsed = DateTime.UtcNow - _status.LastRunAt.Value;
>         if (elapsed > TimeSpan.FromMinutes(10))
>             return Task.FromResult(HealthCheckResult.Unhealthy(
>                 $"Last run was {elapsed.TotalMinutes:F0} minutes ago"));
>
>         return Task.FromResult(HealthCheckResult.Healthy(
>             $"Last run: {_status.LastRunAt:HH:mm:ss}, " +
>             $"marked {_status.LastCount} prescriptions"));
>     }
> }
>
>
> // === Program.cs ===
> builder.Services.AddSingleton<ExpirationJobStatus>();
> builder.Services.AddHostedService<PrescriptionExpirationService>();
> builder.Services.AddHealthChecks()
>     .AddCheck<ExpirationJobHealthCheck>("expiration-job",
>         tags: new[] { "ready" });
> ```

---

## Recommended Practice Order

1. **Exercise 1** (Full API) — build the complete pattern from scratch
2. **Exercise 4** (Exception Handling) — **most likely in a senior interview**
3. **Exercise 2** (Middleware) — shows you understand the pipeline
4. **Exercise 3** (Options Pattern) — quick win, commonly asked
5. **Exercise 5** (Background Service) — shows production thinking

> [!tip] During the Interview
> When building ASP.NET code live, start with Program.cs and say "let me set up the DI and middleware pipeline first." This signals that you think about application structure before jumping into business logic.