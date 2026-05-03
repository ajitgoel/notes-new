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

## Interview Questions

1. When should you choose WebFlux over MVC? When should you NOT?
2. How does the Netty event loop work vs Tomcat's thread-per-request?
3. What are the tradeoffs of R2DBC vs JPA?
4. How do you handle errors in WebClient calls?
5. What is a RouterFunction and when would you use it over @RestController?
6. How would you stream data to the client using SSE?
