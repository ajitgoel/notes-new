# Spring WebFlux & Reactive Stack

## WebFlux vs MVC

| Aspect | Spring MVC | Spring WebFlux |
|--------|-----------|----------------|
| Server | Tomcat (Servlet) | Netty (non-blocking) |
| Threading | Thread-per-request | Event loop (few threads) |
| I/O | Blocking | Non-blocking |
| Return types | Plain objects | Mono / Flux |
| Use when | CPU-bound, JDBC | I/O-bound, many concurrent connections |

**Rule of thumb**: WebFlux shines when your app is I/O-bound (calling APIs, waiting on DBs). If you do heavy computation, MVC with a large thread pool may be simpler.

---

## Reactive Controllers

```java
@RestController
@RequestMapping("/api/users")
public class UserController {

    @GetMapping("/{id}")
    public Mono<ResponseEntity<User>> getUser(@PathVariable String id) {
        return userService.findById(id)
            .map(ResponseEntity::ok)
            .defaultIfEmpty(ResponseEntity.notFound().build());
    }

    @GetMapping
    public Flux<User> getAllUsers() {
        return userService.findAll();
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public Mono<User> createUser(@Valid @RequestBody Mono<CreateUserRequest> request) {
        return request.flatMap(userService::create);
    }

    // Server-Sent Events stream
    @GetMapping(value = "/stream", produces = MediaType.TEXT_EVENT_STREAM_VALUE)
    public Flux<User> streamUsers() {
        return userService.findAll().delayElements(Duration.ofMillis(200));
    }
}
```

---

## WebClient — Reactive HTTP Client

```java
@Service
public class ExternalApiClient {

    private final WebClient webClient;

    public ExternalApiClient(WebClient.Builder builder) {
        this.webClient = builder
            .baseUrl("https://api.example.com")
            .defaultHeader("Accept", "application/json")
            .filter(ExchangeFilterFunction.ofRequestProcessor(req -> {
                log.info("Request: {} {}", req.method(), req.url());
                return Mono.just(req);
            }))
            .build();
    }

    public Mono<User> getUser(String id) {
        return webClient.get()
            .uri("/users/{id}", id)
            .retrieve()
            .onStatus(HttpStatusCode::is4xxClientError, response ->
                response.bodyToMono(String.class)
                    .flatMap(body -> Mono.error(new ClientException(body)))
            )
            .onStatus(HttpStatusCode::is5xxServerError, response ->
                Mono.error(new ServiceException("Upstream 500"))
            )
            .bodyToMono(User.class)
            .timeout(Duration.ofSeconds(5))
            .retryWhen(Retry.backoff(3, Duration.ofMillis(200))
                .filter(ex -> ex instanceof ServiceException));
    }

    public Flux<Product> getProducts() {
        return webClient.get()
            .uri("/products")
            .retrieve()
            .bodyToFlux(Product.class);
    }

    public Mono<Order> createOrder(OrderRequest request) {
        return webClient.post()
            .uri("/orders")
            .bodyValue(request)
            .retrieve()
            .bodyToMono(Order.class);
    }
}
```

---

## R2DBC — Reactive Database Access

```java
// Repository
public interface UserRepository extends ReactiveCrudRepository<User, Long> {

    Flux<User> findByRole(Role role);
    Mono<User> findByEmail(String email);

    @Query("SELECT * FROM users WHERE department_id = :deptId ORDER BY name")
    Flux<User> findByDepartment(Long deptId);
}

// Entity (no JPA annotations — R2DBC uses Spring Data annotations)
@Table("users")
public class User {
    @Id
    private Long id;
    private String name;
    private String email;

    @CreatedDate
    private LocalDateTime createdAt;
}

// Service
@Service
public class UserService {

    @Transactional
    public Mono<User> createUser(CreateUserRequest request) {
        return userRepo.findByEmail(request.email())
            .flatMap(existing -> Mono.<User>error(
                new DuplicateException("Email taken")))
            .switchIfEmpty(Mono.defer(() -> {
                User user = new User(request.name(), request.email());
                return userRepo.save(user);
            }));
    }

    @Transactional(readOnly = true)
    public Flux<User> findAll() {
        return userRepo.findAll();
    }
}
```

### R2DBC vs JPA
| Feature | JPA/Hibernate | R2DBC |
|---------|-------------|-------|
| Lazy loading | Yes | No |
| Relationships | @OneToMany etc. | Manual joins |
| Caching | L1/L2 cache | None |
| Complexity | Higher, more features | Lower, more explicit |
| Thread model | Blocking | Non-blocking |

---

## Functional Endpoints (Router Functions)

```java
@Configuration
public class RouterConfig {

    @Bean
    public RouterFunction<ServerResponse> routes(UserHandler handler) {
        return RouterFunctions.route()
            .path("/api/users", b -> b
                .GET("/{id}", handler::getUser)
                .GET("", handler::listUsers)
                .POST("", handler::createUser)
            )
            .build();
    }
}

@Component
public class UserHandler {

    public Mono<ServerResponse> getUser(ServerRequest request) {
        String id = request.pathVariable("id");
        return userService.findById(id)
            .flatMap(user -> ServerResponse.ok().bodyValue(user))
            .switchIfEmpty(ServerResponse.notFound().build());
    }
}
```

---

## Interview Questions & Answers

### 1. When should you choose WebFlux over MVC? When should you NOT?

**Choose WebFlux when**:
- Your app is I/O-bound: calling REST APIs, waiting on databases, streaming data. WebFlux uses non-blocking I/O, so a handful of threads can handle thousands of concurrent connections.
- You need to handle many concurrent connections with limited resources (real-time dashboards, chat, IoT).
- Your entire stack is reactive (R2DBC, reactive Redis, WebClient). Half-reactive half-blocking negates the benefits.
- You need streaming/SSE endpoints.

**Do NOT choose WebFlux when**:
- Your app is CPU-bound (image processing, ML inference, heavy computation). Non-blocking I/O doesn't help — you still need CPU cycles.
- You use blocking libraries (JDBC, JPA/Hibernate, synchronous SDKs). Blocking on a Netty event loop thread deadlocks the server. You'd need to wrap everything in `subscribeOn(boundedElastic)`, which defeats the purpose.
- Your team is unfamiliar with reactive programming. The debugging, mental model, and error handling are significantly more complex. The productivity cost may outweigh the performance benefit.
- Your throughput requirements are moderate. Spring MVC with a 200-thread pool handles most workloads fine.

### 2. How does the Netty event loop work vs Tomcat's thread-per-request?

**Tomcat (thread-per-request)**: Maintains a pool of ~200 threads. Each incoming request is assigned a dedicated thread that handles the entire request lifecycle — including waiting for database queries, HTTP calls, and file I/O. During I/O waits, the thread is blocked (doing nothing, but unavailable for other requests). 200 concurrent slow requests = pool exhausted.

**Netty (event loop)**: Uses a small number of threads (typically 2 × CPU cores). When a request arrives, the event loop accepts it, registers I/O operations with the OS (epoll/kqueue), and moves on to the next request. When I/O completes, the OS notifies Netty, and the event loop resumes processing. No thread is blocked during I/O waits.

The result: 8 Netty threads can handle thousands of concurrent I/O-bound requests, because threads spend their time processing results instead of waiting. But this only works if you NEVER block — a single blocking call on an event loop thread stops it from processing ANY other request.

### 3. What are the tradeoffs of R2DBC vs JPA?

| Aspect | JPA/Hibernate | R2DBC |
|--------|-------------|-------|
| **Thread model** | Blocking (thread-per-query) | Non-blocking (reactive) |
| **Relationships** | `@OneToMany`, `@ManyToOne`, lazy loading | Manual — write explicit JOIN queries or separate queries |
| **Caching** | L1 cache (session), L2 cache (shared) | None — every query hits the database |
| **Schema management** | DDL generation, Flyway integration | Schema managed externally |
| **Query building** | JPQL, Criteria API, derived queries | `@Query` with native SQL, `DatabaseClient` |
| **Maturity** | 20+ years, massive ecosystem | Younger, fewer features |
| **Complexity** | Higher (lazy loading pitfalls, session management) | Lower but more manual |

**Choose R2DBC** when: you're already using WebFlux and need non-blocking DB access. The manual relationship handling is the price of non-blocking I/O.

**Choose JPA** when: you have complex domain models with many relationships, need caching, or your team knows it well. Use with Spring MVC.

### 4. How do you handle errors in WebClient calls?

WebClient provides status-based error handling with `onStatus()` and general error handling with `onErrorResume()`:

```java
webClient.get().uri("/users/{id}", id).retrieve()
    // Status-specific handling
    .onStatus(status -> status.value() == 404, response ->
        Mono.error(new UserNotFoundException(id)))
    .onStatus(HttpStatusCode::is4xxClientError, response ->
        response.bodyToMono(ErrorResponse.class)
            .flatMap(body -> Mono.error(new ClientException(body.message()))))
    .onStatus(HttpStatusCode::is5xxServerError, response ->
        Mono.error(new ServiceException("Upstream server error")))
    .bodyToMono(User.class)
    // Timeout
    .timeout(Duration.ofSeconds(5))
    // Retry on transient errors
    .retryWhen(Retry.backoff(3, Duration.ofMillis(200))
        .filter(ex -> ex instanceof ServiceException))
    // Fallback
    .onErrorResume(TimeoutException.class, ex -> getCachedUser(id));
```

**Important**: Without `onStatus()`, WebClient throws `WebClientResponseException` for 4xx/5xx. With `onStatus()`, you control the exception type, which matters for retry filtering — you don't want to retry 404s.

### 5. What is a RouterFunction and when would you use it over @RestController?

`RouterFunction` is the functional alternative to annotation-based controllers. Instead of `@GetMapping`/`@PostMapping`, you define routes as functions:

```java
RouterFunctions.route()
    .GET("/users/{id}", handler::getUser)
    .POST("/users", handler::createUser)
    .build();
```

**Use RouterFunction when**: you want to build routes programmatically (conditional routes, generated routes), you prefer functional style, or you're building a lightweight microservice where annotation scanning overhead matters.

**Use @RestController when**: your team is familiar with the annotation style, you want IDE navigation (click on URL → jump to handler), you're using Spring Security's method-level annotations, or you have many endpoints.

In practice, most teams use `@RestController`. `RouterFunction` is more common in lightweight reactive microservices and in frameworks built on WebFlux.

### 6. How would you stream data to the client using SSE?

Server-Sent Events (SSE) enable the server to push events to the client over a long-lived HTTP connection:

```java
@GetMapping(value = "/events", produces = MediaType.TEXT_EVENT_STREAM_VALUE)
public Flux<ServerSentEvent<OrderUpdate>> streamUpdates(@RequestParam String userId) {
    return orderService.watchUpdates(userId)
        .map(update -> ServerSentEvent.<OrderUpdate>builder()
            .id(update.getId())
            .event("order-update")
            .data(update)
            .retry(Duration.ofSeconds(5))
            .build());
}
```

Key points:
- Set `produces = TEXT_EVENT_STREAM_VALUE` to signal SSE format
- Return `Flux<T>` — each emitted item becomes an SSE event
- Use `ServerSentEvent` wrapper for control over event ID, type, and retry interval
- WebFlux keeps the connection open and writes events as the Flux emits
- Client connects with `EventSource` (browser) or `WebClient` with streaming
- `delayElements()` can throttle emission rate to prevent overwhelming clients
