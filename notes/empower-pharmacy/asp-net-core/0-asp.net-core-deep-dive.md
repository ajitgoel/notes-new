# ASP.NET Core Deep Dive
## Framework internals, patterns, and interview-ready code

---
## 1. Program.cs — The Application Entry Point

Modern .NET (6+) uses a minimal hosting model. Everything is configured in one file.
```csharp hl:1,11,19
var builder = WebApplication.CreateBuilder(args);
// === SERVICES (DI container) ===
builder.Services.AddControllers();// MVC controllers
builder.Services.AddScoped<IOrderService, OrderService>();      // custom service
builder.Services.AddDbContext<AppDbContext>(opts =>             // EF Core
	opts.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts => { /* JWT config */ });
builder.Services.AddHealthChecks() // health endpoints
    .AddSqlServer(builder.Configuration.GetConnectionString("Default"));
var app = builder.Build();
// === MIDDLEWARE PIPELINE (order matters!) ===
app.UseExceptionHandler("/error");   // 1. catch unhandled exceptions
app.UseHttpsRedirection();            // 2. redirect HTTP → HTTPS
app.UseAuthentication();              // 3. who are you?
app.UseAuthorization();               // 4. are you allowed?
app.MapControllers();                 // 5. route to controllers
app.MapHealthChecks("/health");       // 6. health check endpoint
app.Run();
```

> [!tip] Interview insight
=="Services" go in the top half (before `Build()`). "Middleware" goes in the bottom half (after `Build()`). Services = what's available. Middleware = how requests flow.==

---
## 2. Middleware Pipeline
==Every HTTP request passes through middleware in the order they're registered. Each middleware can:==
- ==**Process and pass** to the next middleware==
- ==**Short-circuit** — return a response without calling the next middleware==
```
Request  →  ExceptionHandler → HTTPS → Auth → Authorization → Controller
Response ←  ExceptionHandler ← HTTPS ← Auth ← Authorization ← Controller
```
### Writing Custom Middleware
```csharp hl:5,8,11,14,21
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
        await _next(context);  // call the next middleware
        _logger.LogInformation("{Method} {Path} → {Status} in {Ms}ms",
            context.Request.Method, context.Request.Path,
            context.Response.StatusCode, sw.ElapsedMilliseconds);
    }
}
// Register in Program.cs:
app.UseMiddleware<RequestTimingMiddleware>();
```
### Common Middleware Order

| Order | Middleware              | Why                                |
| ----- | ----------------------- | ---------------------------------- |
| 1     | Exception Handler       | Catches everything — must be first |
| 2     | HSTS / HTTPS            | Redirect before any processing     |
| 3     | Static Files            | Serve files without hitting auth   |
| 4     | Routing                 | Match URL to endpoint              |
| 5     | CORS                    | Must come before auth              |
| 6     | Authentication          | Identify the user                  |
| 7     | Authorization           | Check permissions                  |
| 8     | Endpoints / Controllers | Handle the request                 |

> [!warning] Order matters
> If you put Authentication AFTER the controller, your `[Authorize]` attributes won't work — the user identity hasn't been set yet.

---

## 3. Controllers vs Minimal APIs

### Controller-Based (traditional, used at Empower)

```csharp hl:1,2,10,17
[ApiController]
[Route("api/[controller]")]
public class PrescriptionsController : ControllerBase
{
    private readonly IPrescriptionService _service;

    public PrescriptionsController(IPrescriptionService service)
        => _service = service;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var rx = await _service.GetByIdAsync(id);
        return rx is null ? NotFound() : Ok(rx);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRxDto dto)
    {
        var id = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }
}
```

### Minimal API (modern, .NET 6+)

```csharp
app.MapGet("/api/prescriptions/{id}", async (int id, IPrescriptionService svc) =>
{
    var rx = await svc.GetByIdAsync(id);
    return rx is null ? Results.NotFound() : Results.Ok(rx);
});

app.MapPost("/api/prescriptions", async (CreateRxDto dto, IPrescriptionService svc) =>
{
    var id = await svc.CreateAsync(dto);
    return Results.Created($"/api/prescriptions/{id}", new { id });
});
```

| Feature | Controllers | Minimal APIs |
|---|---|---|
| Best for | Large apps, teams, complex routing | Small APIs, microservices, prototypes |
| DI | Constructor injection | Parameter injection |
| Filters | Full support (action, exception, auth) | Limited (endpoint filters) |
| Model binding | Automatic from body/query/route | Explicit with attributes |
| Testing | Well-established patterns | Simpler but less conventional |

---

## 4. Configuration & Options Pattern

### Reading Configuration

```csharp
// appsettings.json
{
    "ConnectionStrings": {
        "Default": "Server=...;Database=Pharmacy;..."
    },
    "Kafka": {
        "BootstrapServers": "kafka:9092",
        "GroupId": "billing-service"
    }
}
```

### Options Pattern (strongly typed config)

```csharp
// 1. Define a class matching the JSON shape
public class KafkaOptions
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
}

// 2. Register in Program.cs
builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection("Kafka"));

// 3. Inject wherever you need it
public class OrderEventProducer
{
    private readonly KafkaOptions _options;

    public OrderEventProducer(IOptions<KafkaOptions> options)
    {
        _options = options.Value;
        // _options.BootstrapServers = "kafka:9092"
    }
}
```

| Interface             | Behavior                                               |
| --------------------- | ------------------------------------------------------ |
| `IOptions<T>`         | Reads config once at startup, never changes            |
| `IOptionsSnapshot<T>` | Re-reads per request (scoped). Picks up file changes.  |
| `IOptionsMonitor<T>`  | Singleton that watches for changes and fires callbacks |

---
## 5. Filters (Action Filters, Exception Filters)
==Filters run code **before and after** controller actions. They're the ASP.NET way to handle cross-cutting concerns.==
### Action Filter
```csharp hl:1,13-16,3,5,7
public class ValidateModelFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(context.ModelState);
        }
    }
    public void OnActionExecuted(ActionExecutedContext context) { }
}
// Register globally in Program.cs:
builder.Services.AddControllers(opts =>
{
    opts.Filters.Add<ValidateModelFilter>();
});
```
### Exception Filter
```csharp hl:1,6
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        => _logger = logger;
    public void OnException(ExceptionContext context)
    {
        _logger.LogError(context.Exception, "Unhandled exception");
        context.Result = context.Exception switch
        {
            NotFoundException => new NotFoundObjectResult(
                new { error = context.Exception.Message }),
            ValidationException ve => new BadRequestObjectResult(
                new { errors = ve.Errors }),
            _ => new ObjectResult(
                new { error = "An unexpected error occurred" })
                { StatusCode = 500 }
        };
        context.ExceptionHandled = true;
    }
}
```
### Filter Execution Order
``` hl:1-3
Authorization Filters → Resource Filters → Model Binding →
Action Filters (before) → ACTION → Action Filters (after) →
Exception Filters → Result Filters
```
---
## 6. Authentication & Authorization
### JWT Bearer Authentication
```csharp hl:2,17
// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("PharmacistOnly", policy =>
        policy.RequireRole("Pharmacist"));
    opts.AddPolicy("CanViewPHI", policy =>
        policy.RequireClaim("phi_access", "true"));
});
```

### Using in Controllers

```csharp hl:1,7,11
[Authorize]  // requires any authenticated user
[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    [HttpGet("{id}")]
    [Authorize(Policy = "CanViewPHI")]  // requires phi_access claim
    public async Task<IActionResult> GetPatient(int id) { /* ... */ }

    [HttpPost]
    [Authorize(Roles = "Pharmacist,Admin")]  // role-based
    public async Task<IActionResult> CreatePatient(CreatePatientDto dto) { /* ... */ }
}
```

---

## 7. Logging

```csharp
public class OrderService
{
    private readonly ILogger<OrderService> _logger;

    public OrderService(ILogger<OrderService> logger)
        => _logger = logger;

    public async Task<Order> CreateAsync(CreateOrderDto dto)
    {
        _logger.LogInformation("Creating order for patient {PatientId}",
            dto.PatientId);

        try
        {
            var order = /* ... */;
            _logger.LogInformation("Order {OrderId} created successfully",
                order.Id);
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order for patient {PatientId}",
                dto.PatientId);
            throw;
        }
    }
}
```

> [!tip] Structured logging
> Use `{PlaceholderName}` not string interpolation `$"{value}"`. Structured logging lets tools like New Relic (which Empower uses) search and filter by field.

### Log Levels

| Level | Use |
|---|---|
| `Trace` | Verbose debugging (disabled in prod) |
| `Debug` | Internal flow details |
| `Information` | Normal operations (order created, user logged in) |
| `Warning` | Unexpected but recoverable (retry, fallback) |
| `Error` | Failure that affected a request |
| `Critical` | App-wide failure (DB down, out of memory) |

---
## 8. Health Checks
```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("Default")!,
        name: "database",
        tags: new[] { "ready" })
    .AddCheck<KafkaHealthCheck>("kafka", tags: new[] { "ready" });

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false  // no checks — just "is the app running?"
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

### Custom Health Check

```csharp
public class KafkaHealthCheck : IHealthCheck
{
    private readonly IProducer<string, string> _producer;
    public KafkaHealthCheck(IProducer<string, string> producer)
        => _producer = producer;
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            _producer.Flush(TimeSpan.FromSeconds(3));
            return Task.FromResult(HealthCheckResult.Healthy("Kafka reachable"));
        }
        catch
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Kafka unreachable"));
        }
    }
}
```

> [!info] Liveness vs Readiness
> `/health/live` = "is the process running?" (for Kubernetes restarts). `/health/ready` = "can it handle traffic?" (checks DB, Kafka, etc.). Kubernetes uses these to decide whether to route traffic to this instance.

---
## 9. Background Services (Hosted Services)
Long-running tasks that run alongside your API — perfect for Kafka consumers.
```csharp hl:18-19
public class OrderEventConsumer : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly ILogger<OrderEventConsumer> _logger;
    public OrderEventConsumer(IServiceProvider provider,
        ILogger<OrderEventConsumer> logger)
    {
        _provider = provider;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Order consumer starting");
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Create a NEW scope for each iteration (for scoped services)
                using var scope = _provider.CreateScope();
                var handler = scope.ServiceProvider
                    .GetRequiredService<IOrderHandler>();
                await handler.ProcessNextAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order event");
                await Task.Delay(1000, ct);  // back off on errors
            }
        }
    }
}
// Register in Program.cs
builder.Services.AddHostedService<OrderEventConsumer>();
```

> [!warning] DI Scope trap
==BackgroundService is a singleton, but DbContext is scoped. You MUST create a scope with `CreateScope()` to get scoped services. Injecting DbContext directly into the constructor will throw.==

---

## 10. Model Validation

```csharp
public record CreatePrescriptionDto
{
    [Required(ErrorMessage = "Patient is required")]
    public int PatientId { get; init; }
    [Required, StringLength(200, MinimumLength = 2)]
    public string MedicationName { get; init; } = string.Empty;
    [Range(0.1, 5000, ErrorMessage = "Dosage must be between 0.1 and 5000")]
    public decimal Dosage { get; init; }
    [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Invalid NDC code")]
    public string? NdcCode { get; init; }
}
```

==With `[ApiController]`, validation is automatic — invalid models return `400 Bad Request` before your action code runs.==

### Custom Validation

```csharp hl:1
public class FutureDateAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(
        object? value, ValidationContext context)
    {
        if (value is DateTime date && date <= DateTime.UtcNow)
            return new ValidationResult("Date must be in the future");
        return ValidationResult.Success;
    }
}

// Usage:
public record ScheduleDto
{
    [FutureDate]
    public DateTime ScheduledAt { get; init; }
}
```

### FluentValidation (alternative)

```csharp hl:15-16,1
public class CreateRxValidator : AbstractValidator<CreatePrescriptionDto>
{
    public CreateRxValidator()
    {
        RuleFor(x => x.PatientId).GreaterThan(0);
        RuleFor(x => x.MedicationName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Dosage).InclusiveBetween(0.1m, 5000m);
        RuleFor(x => x.NdcCode)
            .Matches(@"^\d{10,11}$")
            .When(x => x.NdcCode != null);
    }
}

// Register in Program.cs:
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateRxValidator>();
```

---
## 11. Quick-Fire Interview Q&A
### =="What does `[ApiController]` do?"==
Three things: ==(1) automatic model validation (returns 400 for invalid models),== (2) binding source inference (`[FromBody]` is implied for complex types), (3) problem details responses for errors. It's a convenience attribute — you can do all this manually.
### =="How does routing work?"==
Two layers. **Conventional routing** (`MapControllerRoute`) matches URL patterns globally. ==**Attribute routing** (`[Route]`, `[HttpGet]`) is defined on each controller/action. In API projects, attribute routing is standard.==
### "What's the difference between `AddSingleton`, `AddScoped`, `AddTransient`?"
==Singleton = one instance for the app. Scoped = one instance per HTTP request. Transient = new instance every time it's injected. DbContext is always scoped. Caches and HttpClient factories are singletons.== Validators and formatters are usually transient.
### "How do you handle errors globally?"
Three options: ==(1) `app.UseExceptionHandler()` middleware — catches everything, (2) `IExceptionFilter` — catches controller exceptions only,== (3) Problem Details middleware (.NET 7+) — standardized error format. I'd use exception middleware for API-wide handling and filters for controller-specific logic.
### "What's the difference between `IApplicationBuilder` and `WebApplication`?"
`WebApplication` (modern .NET 6+) IS an `IApplicationBuilder` — plus it also implements `IHost` and `IEndpointRouteBuilder`. It merges what used to be `Startup.Configure` and `Program.Main` into a single unified API.
### "How do you handle CORS(Cross Origin Resource Sharing)?"
Register in services: `builder.Services.AddCors(opts => opts.AddPolicy("AllowFrontend", p => p.WithOrigins("https://app.empower.com").AllowAnyMethod().AllowAnyHeader()))`. Apply with `app.UseCors("AllowFrontend")` — must come AFTER routing but BEFORE auth.