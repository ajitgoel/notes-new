###### **1. Variables & Data Types** 
C# is strongly typed. Variables must declare their type or use `var` for implicit typing.
```csharp
// Value types
int age = 30;
double price = 19.99;
bool isActive = true;
char grade = 'A';
decimal salary = 75000.50m;
// Reference types
string name = "Alice";
int[] numbers = { 1, 2, 3, 4, 5 };
object obj = 42; // boxing
// Implicit typing — compiler infers the type
var message = "Hello"; // inferred as string
var count = 10;        // inferred as int
// Nullable value types
int? nullableAge = null;
int resolved = nullableAge ?? 25; // null-coalescing operator
```

###### **2. Control Flow** 
```csharp
// Pattern matching with switch expressions (C# 8+)
string Classify(int score) => score switch
{
    >= 90 => "A",
    >= 80 => "B",
    >= 70 => "C",
    >= 60 => "D",
    _ => "F"
};
// for, foreach, while
for (int i = 0; i < 5; i++) Console.WriteLine(i);
foreach (var item in new[] { "a", "b", "c" }) Console.WriteLine(item);
// Ternary
string status = age >= 18 ? "Adult" : "Minor";
```

  ###### **3. Methods & Parameters** 
```csharp hl:2-3
// Named and optional parameters
void CreateUser(string name, int age = 25, 
string role = "User")
{
    Console.WriteLine($"{name}, {age}, {role}");
}
CreateUser("Bob");
CreateUser("Alice", role: "Admin");
// out and ref parameters
bool TryParseAge(string input, out int age)
{
    return int.TryParse(input, out age);
}
void Increment(ref int value) => value++;
// params — variable number of arguments
int Sum(params int[] numbers) => numbers.Sum();
Console.WriteLine(Sum(1, 2, 3, 4)); // 10
// Expression-bodied members
double Square(double x) => x * x;
```
###### **4. Classes & Object-Oriented Programming** 
```csharp hl:19
public class Animal
{
    // Properties with backing field shorthand
    public string Name { get; set; }
    public int Age { get; private set; }
    // Constructor
    public Animal(string name, int age)
    {
        Name = name;
        Age = age;
    }
    // Virtual method — can be overridden
    public virtual string Speak() => "...";
    // Override ToString
    public override string ToString() => $"{Name} (Age: {Age})";
}
public class Dog : Animal
{
    public string Breed { get; init; } // init-only property (C# 9+)
    public Dog(string name, int age, string breed) : base(name, age)
    {
        Breed = breed;
    }
    public override string Speak() => "Woof!";
}
// Usage
var dog = new Dog("Rex", 3, "Labrador");
Console.WriteLine(dog.Speak()); // Woof!
```
###### **5. Interfaces & Abstract Classes** 
```csharp hl:1,11,15,16
// Interface — defines a contract
public interface IRepository<T>
{
    T GetById(int id);
    IEnumerable<T> GetAll();
    void Add(T entity);
    void Delete(int id);
    // Default interface method (C# 8+)
    bool Exists(int id) => GetById(id) != null;
}
// Abstract class — partial implementation
public abstract class Shape
{
    public string Color { get; set; }
    public abstract double Area();           // must be implemented
    public virtual void Draw() =>            // can be overridden
        Console.WriteLine($"Drawing {Color} shape");
}
public class Circle : Shape, IRepository<Circle>
{
    public double Radius { get; set; }
    public override double Area() => Math.PI * Radius * Radius;
    // IRepository implementation...
}
```

###### **6. Records & Structs** 
```csharp hl:1-2,5,10-11
// Record — immutable reference type with value equality (C# 9+)
public record Person(string Name, int Age);
var p1 = new Person("Alice", 30);
var p2 = new Person("Alice", 30);
Console.WriteLine(p1 == p2); // true (value equality)
// Non-destructive mutation with `with`
var p3 = p1 with { Age = 31 };
// Record struct (C# 10+)
public readonly record struct Point(double X, double Y);
// Regular struct — value type, stack-allocated
public struct Vector2
{
    public float X { get; init; }
    public float Y { get; init; }
    public float Magnitude() => MathF.Sqrt(X * X + Y * Y);
}
```

###### **7. Collections & LINQ** 
```csharp
// Common collections
var list = new List<string> { "apple", "banana", "cherry" };
var dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
var set = new HashSet<int> { 1, 2, 3 };
var queue = new Queue<string>();
var stack = new Stack<int>();
// LINQ — query syntax
var expensive = from p in products
                where p.Price > 50
                orderby p.Price descending
                select new { p.Name, p.Price };
// LINQ — method syntax (more common)
var results = products
    .Where(p => p.Price > 50)
    .OrderByDescending(p => p.Price)
    .Select(p => new { p.Name, p.Price })
    .ToList();
// Useful LINQ methods
int total = numbers.Sum();
double avg = numbers.Average();
var first = products.FirstOrDefault(p => p.Id == 5);
bool any = products.Any(p => p.Price > 100);
var grouped = products.GroupBy(p => p.Category);
var joined = orders.Join(customers,
    o => o.CustomerId, c => c.Id,
    (o, c) => new { o.OrderId, c.Name });
```
###### **8. Generics** 
```csharp
// Generic class
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }
    private Result(T value) { IsSuccess = true; Value = value; }
    private Result(string error) { IsSuccess = false; Error = error; }
    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);
}
// Generic method with constraints
public T Max<T>(T a, T b) where T : IComparable<T>
{
    return a.CompareTo(b) > 0 ? a : b;
}
// Multiple constraints
public void Process<T>(T item)
    where T : class, IDisposable, new()
{
    // T must be a reference type, implement IDisposable, and have a parameterless constructor
}
```
###### **9. Delegates, Events & Lambdas** 
```csharp hl:1-4
// Built-in delegate types
Func<int, int, int> add = (a, b) => a + b;       // returns a value
Action<string> log = msg => Console.WriteLine(msg); // returns void
Predicate<int> isEven = n => n % 2 == 0;           // returns bool

----------
// Invocation
int result = add(10, 5);
log("Processing started..."); // Output: Processing started...
var numbers = new List<int> { 1, 2, 3, 4, 5 };
var evenNumbers = numbers.FindAll(isEven); // Passes the predicate to filter the list
----------

// Events
public class Button
{
    public event EventHandler<string>? Clicked;
    public void Click()
    {
        Clicked?.Invoke(this, "Button was clicked");
    }
}
// Subscribe
var btn = new Button();
btn.Clicked += (sender, msg) => Console.WriteLine(msg);
btn.Click();
// Multicast delegates
Action greet = () => Console.Write("Hello ");
greet += () => Console.Write("World");
greet(); // "Hello World"
```

  ###### **10. Async/Await** 
```csharp hl:15,9,2,5
// Async method
public async Task<string> FetchDataAsync(string url)
{
    using var client = new HttpClient();
    var response = await client.GetAsync(url);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsStringAsync();
}
// Running multiple tasks concurrently
public async Task LoadDashboardAsync()
{
    var usersTask = GetUsersAsync();
    var ordersTask = GetOrdersAsync();
    var statsTask = GetStatsAsync();
    await Task.WhenAll(usersTask, ordersTask, statsTask);
    var users = usersTask.Result;
    var orders = ordersTask.Result;
    var stats = statsTask.Result;
}
// Cancellation
public async Task LongRunningAsync(CancellationToken ct)
{
    for (int i = 0; i < 1000; i++)
    {
        ct.ThrowIfCancellationRequested();
        await Task.Delay(100, ct);
    }
}
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await LongRunningAsync(cts.Token);
```

  ###### **11. Exception Handling** 
```csharp
// Try-catch-finally with exception filters
try
{
    var data = await FetchDataAsync(url);
}
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
{
    Console.WriteLine("Resource not found");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"HTTP error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected: {ex.Message}");
    throw; // re-throw preserving stack trace
}
finally
{
    Console.WriteLine("Cleanup runs regardless");
}
// Custom exception
public class OrderNotFoundException : Exception
{
    public int OrderId { get; }
    public OrderNotFoundException(int orderId)
        : base($"Order {orderId} not found")
    {
        OrderId = orderId;
    }
}
```
###### **12. Pattern Matching (C# 8–11)** 
```csharp
// Type patterns
object obj = "hello";
if (obj is string s && s.Length > 3)
    Console.WriteLine(s.ToUpper());
// Property patterns
if (person is { Age: >= 18, Name: var name })
    Console.WriteLine($"{name} is an adult");
// List patterns (C# 11)
int[] nums = { 1, 2, 3, 4, 5 };
var result = nums switch
{
    [1, 2, .., 5] => "Starts with 1,2 and ends with 5",
    [_, _, 3, ..] => "Third element is 3",
    { Length: 0 } => "Empty",
    _ => "Other"
};
// Relational and logical patterns
string WaterState(double tempC) => tempC switch
{
    < 0 => "Solid",
    >= 0 and < 100 => "Liquid",
    >= 100 => "Gas",
    _ => "Unknown"
};
```
###### **13. IDisposable &** `using` 
```csharp hl:2,4,6-13,15,19,20,23
// Implementing IDisposable
public class DatabaseConnection : IDisposable
{
    private bool _disposed = false;
    public void Open() => Console.WriteLine("Connection opened");
    public void Dispose()
    {
        if (!_disposed)
        {
            Console.WriteLine("Connection closed");
            _disposed = true;
        }
    }
}
// using statement — guarantees Dispose() is called
using (var conn = new DatabaseConnection())
{
    conn.Open();
} // Dispose called here
// using declaration (C# 8+) — disposed at end of scope
using var conn2 = new DatabaseConnection();
conn2.Open();
// disposed when the enclosing scope exits
```
###### **14. Extension Methods** 
```csharp hl:1,3,13
public static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
    public static bool IsNullOrEmpty(this string? value)
        => string.IsNullOrEmpty(value);
}
// Usage — reads like a native method
string title = "A very long title that needs truncating";
Console.WriteLine(title.Truncate(20)); // "A very long title th..."
```
###### **15. Dependency Injection (Built-in)** 
```csharp hl:14-15
// Define service
public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
}
public class SmtpEmailService : IEmailService
{
    public async Task SendAsync(string to, string subject, string body)
    {
        // send email via SMTP
    }
}
// Register in Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddTransient<IReportGenerator, PdfReportGenerator>();
var app = builder.Build();
// Inject via constructor
public class OrderController : ControllerBase
{
    private readonly IEmailService _email;
    public OrderController(IEmailService email) => _email = email;
    [HttpPost]
    public async Task<IActionResult> PlaceOrder(Order order)
    {
        // process order...
        await _email.SendAsync(order.Email, "Confirmation", "Your order is placed");
        return Ok();
    }
}
```
###### **16. Nullable Reference Types (C# 8+)** 
```csharp hl:3,6,11
// Enable in .csproj: <Nullable>enable</Nullable>
string nonNullable = "always has a value";  // cannot be null
string? nullable = null;                     // explicitly nullable
// Null-conditional and null-coalescing
int? length = nullable?.Length;
string safe = nullable ?? "default";
string guaranteed = nullable!; // null-forgiving (use sparingly)
// Guard clauses
public void Process(Order? order)
{
    ArgumentNullException.ThrowIfNull(order);
    // order is non-null from here
    Console.WriteLine(order.Id);
}
```

  ###### **17. Span< T > & Memory-Efficient Code**
```csharp
// Span<T> — stack-allocated view into contiguous memory, zero allocations
void ProcessData(Span<int> data)
{
    for (int i = 0; i < data.Length; i++)
        data[i] *= 2;
}
int[] array = { 1, 2, 3, 4, 5 };
ProcessData(array.AsSpan());
// Slicing without allocating a new array
Span<int> slice = array.AsSpan(1..4); // [2, 3, 4]
// ReadOnlySpan for strings — avoids Substring allocations
ReadOnlySpan<char> span = "Hello, World!".AsSpan();
ReadOnlySpan<char> hello = span[..5]; // "Hello" — no allocation
```
###### **18. Channels & Producer-Consumer** 
```csharp
using System.Threading.Channels;
var channel = Channel.CreateBounded<int>(capacity: 10);
// Producer
async Task ProduceAsync(ChannelWriter<int> writer)
{
    for (int i = 0; i < 100; i++)
    {
        await writer.WriteAsync(i);
    }
    writer.Complete();
}
// Consumer
async Task ConsumeAsync(ChannelReader<int> reader)
{
    await foreach (var item in reader.ReadAllAsync())
    {
        Console.WriteLine($"Processed: {item}");
    }
}
// Run both concurrently
await Task.WhenAll(
    ProduceAsync(channel.Writer),
    ConsumeAsync(channel.Reader)
);
```
###### **19. Attributes & Reflection** 
```csharp
// Custom attribute
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class CacheAttribute : Attribute
{
    public int DurationSeconds { get; }
    public CacheAttribute(int durationSeconds) => DurationSeconds = durationSeconds;
}
// Apply attribute
public class ProductService
{
    [Cache(300)]
    public List<Product> GetAll() => db.Products.ToList();
}
// Read attribute via reflection
var method = typeof(ProductService).GetMethod("GetAll");
var attr = method?.GetCustomAttribute<CacheAttribute>();
if (attr != null)
    Console.WriteLine($"Cache for {attr.DurationSeconds}s");
```
###### **20. Common Design Patterns** 
```csharp hl:41-56
// Builder pattern
var query = new QueryBuilder()
    .Select("Name", "Age")
    .From("Users")
    .Where("Age > 18")
    .OrderBy("Name")
    .Build();
public class QueryBuilder
{
    private readonly List<string> _columns = new();
    private string _table = "";
    private string _where = "";
    private string _orderBy = "";
    public QueryBuilder Select(params string[] cols) { _columns.AddRange(cols); return this; }
    public QueryBuilder From(string table) { _table = table; return this; }
    public QueryBuilder Where(string condition) { _where = condition; return this; }
    public QueryBuilder OrderBy(string column) { _orderBy = column; return this; }
    public string Build() =>
        $"SELECT {string.Join(", ", _columns)} FROM {_table}"
        + (_where != "" ? $" WHERE {_where}" : "")
        + (_orderBy != "" ? $" ORDER BY {_orderBy}" : "");
}

// Strategy pattern with DI
public interface IPricingStrategy
{
    decimal Calculate(decimal basePrice);
}
public class RegularPricing : IPricingStrategy
{
    public decimal Calculate(decimal basePrice) => basePrice;
}
public class PremiumPricing : IPricingStrategy
{
    public decimal Calculate(decimal basePrice) => basePrice * 0.8m;
}

-----
### Factory/Registry Approach (Dynamic Selection)
If you need to choose the strategy based on user type at runtime:
public class PricingFactory
{
    public IPricingStrategy GetStrategy(string customerType)
    {
        return customerType switch
        {
            "Premium" => new PremiumPricing(),
            _ => new RegularPricing()
        };
    }
}
// --- Usage ---
var factory = new PricingFactory();
var strategy = factory.GetStrategy("Premium");
decimal finalPrice = strategy.Calculate(100m);
```

--------
==A `record` is a special type of class designed for **holding data**. It gives you a bunch of useful behavior for free that you’d otherwise have to write yourself.==

**Regular class vs record** 
```csharp
// CLASS — you write all of this yourself
public class CreateRxDto
{
    public int PatientId { get; init; }
    public string MedicationName { get; init; }
    // You'd need to manually write:
    // - Equals() and GetHashCode()
    // - ToString()
    // - Deconstruction
    // - Copy with modifications
}
// RECORD — one line, you get all of that for free
public record CreateRxDto(int PatientId, string MedicationName);
```

**What you get for free** 
**1. Value-based equality** 
```csharp hl:6-8
// Classes compare by REFERENCE (are these the same object?)
var a = new PatientClass { Name = "Alice" };
var b = new PatientClass { Name = "Alice" };
a == b  // FALSE — different objects in memory
// Records compare by VALUE (do the properties match?)
var a = new PatientRecord("Alice");
var b = new PatientRecord("Alice");
a == b  // TRUE — same data
```
**2. Nice ToString()** 
```csharp hl:3
var rx = new CreateRxDto(42, "Lisinopril");
// Class:  "CreateRxDto"  (useless)
// Record: "CreateRxDto { PatientId = 42, MedicationName = Lisinopril }"
```
**3. Easy copying with** `with` 
```csharp hl:3,4
var original = new CreateRxDto(42, "Lisinopril");
// Create a copy with one property changed
var modified = original with { MedicationName = "Metformin" };
// original is unchanged — records are immutable by default
```
**When to use what** 

| Use a…     | When…                                                                         |
| ---------- | ----------------------------------------------------------------------------- |
| **record** | Data carriers — DTOs, API requests/responses, value objects                   |
| **class**  | Things with behavior, mutable state, or complex logic (services, controllers) |
| **struct** | Small, lightweight value types (coordinates, money amounts)                   |

**The syntax options** 
```csharp hl:2,5-8,12
// Positional — most concise (properties are init-only)
public record CreateRxDto(int PatientId, string MedicationName);

// With body — when you need extra members
public record OrderDto(int Id, decimal Total)
{
    public string FormattedTotal => $"${Total:F2}";
}

// Record class vs record struct (C# 10+)
public record class  OrderDto(int Id);  // reference type (default)
public record struct Point(double X, double Y);  // value type
```
That’s why you see `record` used for DTOs throughout the interview prep code — it’s less boilerplate for objects that just carry data around.