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

## Interview Questions

1. What's the difference between `@Controller` and `@RestController`?
2. How does Spring MVC process a request from DispatcherServlet to response?
3. Explain the ProblemDetail (RFC 7807) approach to error responses.
4. How do you implement cross-cutting concerns — Filter vs Interceptor vs AOP?
5. How does `@Valid` cascade into nested objects?
6. What are the tradeoffs of global `@RestControllerAdvice` vs per-controller `@ExceptionHandler`?
