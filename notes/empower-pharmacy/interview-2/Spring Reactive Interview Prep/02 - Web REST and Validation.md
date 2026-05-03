# Spring Boot Web, REST & Validation

## REST Controller Patterns

```java
@RestController
@RequestMapping("/api/v1/users")
public class UserController {

    @GetMapping("/{id}")
    public ResponseEntity<User> getUser(@PathVariable String id) {
        return userService.findById(id)
            .map(ResponseEntity::ok)
            .orElseThrow(() -> new ResourceNotFoundException("User", id));
    }

    @GetMapping
    public Page<User> listUsers(
            @RequestParam(defaultValue = "0") int page,
            @RequestParam(defaultValue = "20") int size,
            @RequestParam(defaultValue = "name") String sort) {
        return userService.findAll(
            PageRequest.of(page, size, Sort.by(sort))
        );
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public User createUser(@Valid @RequestBody CreateUserRequest request) {
        return userService.create(request);
    }

    @PutMapping("/{id}")
    public User updateUser(@PathVariable String id,
                           @Valid @RequestBody UpdateUserRequest request) {
        return userService.update(id, request);
    }

    @DeleteMapping("/{id}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void deleteUser(@PathVariable String id) {
        userService.delete(id);
    }
}
```

---

## Validation with Bean Validation (JSR 380)

```java
public record CreateUserRequest(
    @NotBlank(message = "Name is required")
    @Size(min = 2, max = 100)
    String name,

    @NotBlank @Email
    String email,

    @NotNull @Min(18) @Max(150)
    Integer age,

    @Pattern(regexp = "^\\+?[1-9]\\d{1,14}$", message = "Invalid phone")
    String phone
) {}

// Custom constraint
@Target(ElementType.FIELD)
@Retention(RetentionPolicy.RUNTIME)
@Constraint(validatedBy = UniqueEmailValidator.class)
public @interface UniqueEmail {
    String message() default "Email already registered";
    Class<?>[] groups() default {};
    Class<? extends Payload>[] payload() default {};
}

public class UniqueEmailValidator
        implements ConstraintValidator<UniqueEmail, String> {
    @Override
    public boolean isValid(String email, ConstraintValidatorContext ctx) {
        return !userRepository.existsByEmail(email);
    }
}
```

---

## Global Exception Handling

```java
@RestControllerAdvice
public class GlobalExceptionHandler {

    @ExceptionHandler(ResourceNotFoundException.class)
    @ResponseStatus(HttpStatus.NOT_FOUND)
    public ProblemDetail handleNotFound(ResourceNotFoundException ex) {
        ProblemDetail pd = ProblemDetail.forStatus(HttpStatus.NOT_FOUND);
        pd.setTitle("Resource Not Found");
        pd.setDetail(ex.getMessage());
        pd.setProperty("resourceType", ex.getResourceType());
        return pd;
    }

    @ExceptionHandler(MethodArgumentNotValidException.class)
    @ResponseStatus(HttpStatus.BAD_REQUEST)
    public ProblemDetail handleValidation(MethodArgumentNotValidException ex) {
        ProblemDetail pd = ProblemDetail.forStatus(HttpStatus.BAD_REQUEST);
        pd.setTitle("Validation Failed");
        Map<String, String> errors = ex.getBindingResult()
            .getFieldErrors().stream()
            .collect(Collectors.toMap(
                FieldError::getField,
                fe -> fe.getDefaultMessage() != null ? fe.getDefaultMessage() : "invalid",
                (a, b) -> a
            ));
        pd.setProperty("fieldErrors", errors);
        return pd;
    }

    @ExceptionHandler(Exception.class)
    @ResponseStatus(HttpStatus.INTERNAL_SERVER_ERROR)
    public ProblemDetail handleAll(Exception ex) {
        log.error("Unhandled exception", ex);
        ProblemDetail pd = ProblemDetail.forStatus(500);
        pd.setTitle("Internal Server Error");
        return pd; // never expose stack trace
    }
}
```

---

## Content Negotiation & HATEOAS

```java
// Return different formats based on Accept header
@GetMapping(produces = { MediaType.APPLICATION_JSON_VALUE,
                         MediaType.APPLICATION_XML_VALUE })
public User getUser(@PathVariable String id) { ... }

// HATEOAS links
@GetMapping("/{id}")
public EntityModel<User> getUser(@PathVariable String id) {
    User user = userService.findById(id).orElseThrow(...);
    return EntityModel.of(user,
        linkTo(methodOn(UserController.class).getUser(id)).withSelfRel(),
        linkTo(methodOn(OrderController.class).getUserOrders(id)).withRel("orders")
    );
}
```

---

## Interceptors & Filters

```java
// Filter — operates on raw HTTP (Servlet level)
@Component
@Order(1)
public class RequestLoggingFilter extends OncePerRequestFilter {
    @Override
    protected void doFilterInternal(HttpServletRequest req,
            HttpServletResponse res, FilterChain chain) throws ... {
        long start = System.currentTimeMillis();
        chain.doFilter(req, res);
        log.info("{} {} → {} ({}ms)", req.getMethod(), req.getRequestURI(),
            res.getStatus(), System.currentTimeMillis() - start);
    }
}

// Interceptor — operates at Spring MVC level
@Component
public class AuthInterceptor implements HandlerInterceptor {
    @Override
    public boolean preHandle(HttpServletRequest req,
            HttpServletResponse res, Object handler) {
        // return false to block the request
        return true;
    }
}
```

---

## Interview Questions & Answers

### 1. What's the difference between `@Controller` and `@RestController`?

`@RestController` = `@Controller` + `@ResponseBody`. With `@Controller`, each method must either return a view name (for server-side rendering) or annotate the method with `@ResponseBody` to write directly to the HTTP response. `@RestController` applies `@ResponseBody` to every method automatically, so return values are serialized to JSON/XML via `HttpMessageConverter`.

Use `@Controller` for server-side HTML (Thymeleaf, FreeMarker). Use `@RestController` for REST APIs.

### 2. How does Spring MVC process a request from DispatcherServlet to response?

1. **`DispatcherServlet`** receives the HTTP request (it's the front controller, mapped to `/`)
2. **`HandlerMapping`** finds the matching controller method based on URL, HTTP method, headers, and params. `RequestMappingHandlerMapping` handles `@RequestMapping`-based controllers.
3. **`HandlerAdapter`** invokes the controller method, handling argument resolution (`@PathVariable`, `@RequestBody`, `@Valid`) via `HandlerMethodArgumentResolver`s
4. **Controller method** executes, returns an object or `ResponseEntity`
5. **`HttpMessageConverter`** serializes the return value (Jackson for JSON, JAXB for XML)
6. **`HandlerExceptionResolver`** handles any thrown exceptions — `@ExceptionHandler` methods, `@RestControllerAdvice`, or default error handling
7. **Response** is written to the client

`HandlerInterceptor`s can hook in before/after the handler. Filters (`OncePerRequestFilter`) wrap the entire servlet processing.

### 3. Explain the ProblemDetail (RFC 7807) approach to error responses.

RFC 7807 defines a standard JSON structure for error responses:
```json
{
  "type": "https://api.example.com/errors/not-found",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "User with id '999' was not found",
  "instance": "/api/users/999",
  "resourceType": "User"
}
```

Spring 6+ has native support via `ProblemDetail` class. You create one in your `@ExceptionHandler`:
```java
ProblemDetail pd = ProblemDetail.forStatus(404);
pd.setTitle("Resource Not Found");
pd.setDetail(ex.getMessage());
pd.setProperty("resourceType", "User");  // custom extension field
```

Benefits: standardized format clients can parse predictably, extensible with custom properties, and self-documenting with the `type` URI. Much better than ad-hoc `{"error": "something went wrong"}` responses.

### 4. How do you implement cross-cutting concerns — Filter vs Interceptor vs AOP?

**Servlet Filter** (`OncePerRequestFilter`): Operates at the HTTP level, before Spring MVC. Has access to raw `HttpServletRequest`/`HttpServletResponse`. Use for: logging, CORS, authentication, request/response wrapping, compression. Runs for ALL requests, including static resources.

**HandlerInterceptor**: Operates at the Spring MVC level, after handler mapping but before/after handler execution. Has access to the matched handler (which controller method). Use for: authorization checks, audit logging, locale resolution, setting model attributes. Only runs for requests mapped to controllers.

**AOP (`@Aspect`)**: Operates at the method level, wrapping any Spring bean method. Use for: transaction management, caching, metrics, retry logic. Most flexible but most abstract — harder to debug.

**Decision rule**: Use Filters for HTTP-level concerns (security, logging), Interceptors for controller-level concerns (auth, audit), AOP for service-level concerns (transactions, caching, metrics).

### 5. How does `@Valid` cascade into nested objects?

`@Valid` on a method parameter triggers Bean Validation for that object. To validate nested objects, annotate the field with `@Valid`:

```java
public record CreateOrderRequest(
    @NotBlank String customerId,
    @Valid @NotNull Address shippingAddress,  // ← @Valid cascades
    @Valid @NotEmpty List<@Valid OrderItem> items  // ← validates each item
) {}

public record Address(
    @NotBlank String street,
    @NotBlank String city,
    @Size(min = 5, max = 5) String zip
) {}
```

Without `@Valid` on the field, Spring validates only the top-level object. With `@Valid`, it recursively validates nested objects and collection elements. Validation errors for nested fields use dot notation in the field name: `shippingAddress.zip`.

### 6. What are the tradeoffs of global `@RestControllerAdvice` vs per-controller `@ExceptionHandler`?

**Global `@RestControllerAdvice`**: One place for all error handling. Consistent error format across the entire API. DRY. But can become a kitchen sink — you may end up handling exceptions that only one controller throws.

**Per-controller `@ExceptionHandler`**: Handles exceptions specific to that controller. More focused, but duplicates common handling (validation errors, not-found) across controllers.

**Best practice**: Use global `@RestControllerAdvice` for common exceptions (validation, not-found, internal error). Use per-controller `@ExceptionHandler` only for controller-specific exceptions that need special handling. You can scope `@RestControllerAdvice` to specific packages: `@RestControllerAdvice(basePackages = "com.example.admin")` for admin-specific error handling.
