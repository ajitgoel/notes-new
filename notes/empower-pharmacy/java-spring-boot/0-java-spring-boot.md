**Q: Explain dependency injection in Spring**
Spring's IoC container manages bean lifecycle. Beans are registered via `@Component`, `@Service`, `@Repository`, or `@Bean` methods. Injection happens via constructor (preferred), setter, or field injection. Scopes: ==Singleton (default — one instance), Prototype (new per injection), Request (one per HTTP request), Session.==

@Service
public class MedicationService {

    private final MedicationRepository repo;
    private final AuditLogger auditLogger;
    // Constructor injection — preferred, immutable
    public MedicationService(
            MedicationRepository repo,
            AuditLogger auditLogger) {
        this.repo = repo;
        this.auditLogger = auditLogger;
    }
    public List<Medication> findAll() {
        return repo.findAll();
    }
}

**Q: How does Spring Boot auto-configuration work?**
Spring Boot scans the classpath for available libraries and auto-configures beans based on what it finds. If you have `spring-boot-starter-data-jpa` and an H2 dependency, it automatically configures a DataSource, EntityManagerFactory, and TransactionManager. You override defaults via `application.yml` or custom `@Configuration` classes.

**Q: Explain the difference between @Component, @Service, @Repository, and @Controller**All are specializations of `@Component`. `@Repository` adds persistence exception translation. `@Service` is semantic — marks business logic. `@Controller`/`@RestController` enables request mapping. Functionally, Spring treats them identically for DI — the distinction is for readability and specific framework behaviors.

**Q: How do you handle exceptions globally in Spring Boot?**
Use `@ControllerAdvice` with `@ExceptionHandler` methods. This centralizes error handling and keeps controllers clean.
```
@RestControllerAdvice
public class GlobalExceptionHandler {
    @ExceptionHandler(ResourceNotFoundException.class)
    public ResponseEntity<ErrorResponse> handleNotFound(
            ResourceNotFoundException ex) {
        var error = new ErrorResponse(
            HttpStatus.NOT_FOUND.value(),
            ex.getMessage(),
            Instant.now()
        );
        return ResponseEntity.status(404).body(error);
    }
    @ExceptionHandler(ConstraintViolationException.class)
    public ResponseEntity<ErrorResponse> handleValidation(
            ConstraintViolationException ex) {
        var error = new ErrorResponse(400,
            "Validation failed: " + ex.getMessage(),
            Instant.now());
        return ResponseEntity.badRequest().body(error);
    }
}
```

## Building REST APIs in Spring Boot
They build "headless web applications" — this is central to the role
### Clean Controller + Service + Repository

```
@RestController
@RequestMapping("/api/v1/medications")
@Validated
public class MedicationController {
    private final MedicationService service;
    public MedicationController(MedicationService service) {
        this.service = service;
    }
    @GetMapping
    public Page<MedicationDto> list(
            @RequestParam(defaultValue = "0") int page,
            @RequestParam(defaultValue = "20") int size) {
        return service.findAll(PageRequest.of(page, size));
    }
    @GetMapping("/{id}")
    public MedicationDto get(@PathVariable Long id) {
        return service.findById(id);
    }
    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public MedicationDto create(
            @Valid @RequestBody CreateMedicationRequest req) {
        return service.create(req);
    }
    @PutMapping("/{id}")
    public MedicationDto update(
            @PathVariable Long id,
            @Valid @RequestBody UpdateMedicationRequest req) {
        return service.update(id, req);
    }
    @DeleteMapping("/{id}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void delete(@PathVariable Long id) {
        service.delete(id);
    }
}
```
### Validation with Bean Validation
```
public record CreateMedicationRequest(
    @NotBlank @Size(max = 200)
    String name,
    @Pattern(regexp = "^\\d{5}-\\d{4}-\\d{2}$",
            message = "NDC must be in 5-4-2 format")
    String ndc,
    @Positive
    BigDecimal dosage,
    @NotBlank
    String unit
) {}
```

### DTO Mapping
```
@Component
public class MedicationMapper {
    public MedicationDto toDto(Medication entity) {
        return new MedicationDto(
            entity.getId(),
            entity.getName(),
            entity.getNdc(),
            entity.getDosage(),
            entity.getUnit()
        );
    }
    public Medication toEntity(CreateMedicationRequest req) {
        var med = new Medication();
        med.setName(req.name());
        med.setNdc(req.ndc());
        med.setDosage(req.dosage());
        med.setUnit(req.unit());
        return med;
    }
}
```
## Spring Data JPA(Java Persistence API) & Hibernate

```
@Entity
@Table(name = "patients")
public class Patient {
    @Id @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    @Column(nullable = false)
    private String firstName;
    @Column(nullable = false, unique = true)
    private String email;
    // Encrypt PHI at rest using attribute converter
    @Convert(converter = EncryptedStringConverter.class)
    private String ssn;
    @OneToMany(mappedBy = "patient", fetch = FetchType.LAZY)
    private List<Prescription> prescriptions;
}
// Repository — Spring generates implementation
public interface PrescriptionRepository
        extends JpaRepository<Prescription, Long> {
    List<Prescription> findByPatientIdAndStatus(
        Long patientId, PrescriptionStatus status);
    @Query("""
        SELECT p FROM Prescription p
        JOIN FETCH p.patient
        JOIN FETCH p.medications
        WHERE p.id = :id
        """)
    Optional<Prescription> findWithDetailsById(Long id);
    Page<Prescription> findByStatusOrderByCreatedAtDesc(
        PrescriptionStatus status, Pageable pageable);
}
```
### Transaction Management

```
@Service
@Transactional(readOnly = true)  // default read-only
public class PrescriptionService {
    @Transactional  // write transaction
    public PrescriptionDto fillPrescription(Long id) {
        var rx = repo.findById(id)
            .orElseThrow(() -> new ResourceNotFoundException(
                "Prescription not found: " + id));
        rx.setStatus(PrescriptionStatus.FILLED);
        rx.setFilledAt(Instant.now());
        auditLogger.log("PRESCRIPTION_FILLED", id);
        return mapper.toDto(repo.save(rx));
    }
}
```

**HIPAA angle:** Mention encrypting PHI columns via JPA `AttributeConverter`, audit logging with Spring AOP aspects, and the N+1 query problem (use `JOIN FETCH` or `@EntityGraph`). These demonstrate both JPA depth and compliance awareness.
## Spring Security
Critical for a HIPAA-compliant healthcare app
```
@Configuration
@EnableWebSecurity
public class SecurityConfig {
    @Bean
    public SecurityFilterChain filterChain(
            HttpSecurity http) throws Exception {
        http
            .csrf(csrf -> csrf.disable())  // stateless API
            .sessionManagement(sm ->
                sm.sessionCreationPolicy(STATELESS))
            .authorizeHttpRequests(auth -> auth
                .requestMatchers("/api/v1/auth/**").permitAll()
                .requestMatchers("/actuator/health").permitAll()
                .requestMatchers("/api/v1/admin/**")
                    .hasRole("ADMIN")
                .requestMatchers("/api/v1/prescriptions/**")
                    .hasAnyRole("PHARMACIST", "TECH")
                .anyRequest().authenticated()
            )
            .oauth2ResourceServer(oauth2 ->
                oauth2.jwt(Customizer.withDefaults()));

        return http.build();
    }
}
```

**Q: How do you implement method-level security?**
Use `@PreAuthorize` with SpEL expressions for fine-grained access control — e.g., ensuring a pharmacist can only access prescriptions from their own pharmacy.

```
@PreAuthorize("hasRole('PHARMACIST') and " +
    "@pharmacyAccess.canAccess(#pharmacyId, principal)")
public List<PrescriptionDto> getByPharmacy(Long pharmacyId) {
    return repo.findByPharmacyId(pharmacyId)
        .stream().map(mapper::toDto).toList();
}
```

-----------
**1. What is Spring Boot?** 
Spring Boot is an opinionated framework built on top of the Spring Framework that simplifies the creation of production-ready applications. It auto-configures most things, embeds a web server, and eliminates boilerplate XML configuration.

**2. Project Setup** 
A typical Spring Boot app starts with a main class:
```java
@SpringBootApplication
public class MyApp {
    public static void main(String[] args) {
        SpringApplication.run(MyApp.class, args);
    }
}
```
`@SpringBootApplication` combines three annotations:
- `@Configuration` — marks it as a config class
- `@EnableAutoConfiguration` — auto-configures beans based on classpath
- `@ComponentScan` — scans for components in the package and sub-packages

**3. Dependency Injection (IoC)** 
The core of Spring. You define beans and Spring wires them together.
```java
@Service
public class OrderService {
    private final PaymentGateway paymentGateway;

    // Constructor injection (preferred)
    public OrderService(PaymentGateway paymentGateway) {
        this.paymentGateway = paymentGateway;
    }

    public void placeOrder(Order order) {
        paymentGateway.charge(order.getTotal());
    }
}

@Component
public class StripePaymentGateway implements PaymentGateway {
    public void charge(BigDecimal amount) {
        // call Stripe API
    }
}
```
**Key stereotypes:**

| Annotation        | Purpose                                        |
| ----------------- | ---------------------------------------------- |
| `@Component`      | Generic Spring-managed bean                    |
| `@Service`        | Business logic layer                           |
| `@Repository`     | Data access layer (adds exception translation) |
| `@Controller`     | Web MVC controller                             |
| `@RestController` | `@Controller` + `@ResponseBody`                |
**4. REST Controllers** 
```java
@RestController
@RequestMapping("/api/users")
public class UserController {

    private final UserService userService;

    public UserController(UserService userService) {
        this.userService = userService;
    }

    @GetMapping
    public List<User> getAll() {
        return userService.findAll();
    }

    @GetMapping("/{id}")
    public ResponseEntity<User> getById(@PathVariable Long id) {
        return userService.findById(id)
            .map(ResponseEntity::ok)
            .orElse(ResponseEntity.notFound().build());
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public User create(@Valid @RequestBody CreateUserRequest request) {
        return userService.create(request);
    }

    @DeleteMapping("/{id}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void delete(@PathVariable Long id) {
        userService.delete(id);
    }
}
```

  

**5. Configuration & Profiles** 

**application.yml:**

```yaml
server:
  port: 8080

spring:
  datasource:
    url: jdbc:postgresql://localhost:5432/mydb
    username: admin
    password: secret
  jpa:
    hibernate:
      ddl-auto: validate

app:
  jwt-secret: my-secret-key
  page-size: 25
```

  

**Binding config to a class:**

```java
@Configuration
@ConfigurationProperties(prefix = "app")
public class AppConfig {
    private String jwtSecret;
    private int pageSize;
    // getters and setters
}
```

  

**Profiles** let you swap config per environment:

```yaml
# application-dev.yml
spring:
  datasource:
    url: jdbc:h2:mem:testdb

# application-prod.yml
spring:
  datasource:
    url: jdbc:postgresql://prod-host:5432/mydb
```

  

Activate with: `--spring.profiles.active=dev`

**6. Spring Data JPA** 

Define an entity and a repository — Spring generates the SQL.

```java
@Entity
@Table(name = "users")
public class User {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false)
    private String name;

    @Column(unique = true, nullable = false)
    private String email;

    // getters, setters, constructors
}
```

  

```java
public interface UserRepository extends JpaRepository<User, Long> {

    Optional<User> findByEmail(String email);

    List<User> findByNameContainingIgnoreCase(String name);

    @Query("SELECT u FROM User u WHERE u.createdAt > :since")
    List<User> findRecentUsers(@Param("since") LocalDateTime since);
}
```

  

Spring Data parses the method name and builds the query. No implementation class needed.

**7. Exception Handling** 

Centralized with `@ControllerAdvice`:

```java
@RestControllerAdvice
public class GlobalExceptionHandler {

    @ExceptionHandler(ResourceNotFoundException.class)
    @ResponseStatus(HttpStatus.NOT_FOUND)
    public ErrorResponse handleNotFound(ResourceNotFoundException ex) {
        return new ErrorResponse("NOT_FOUND", ex.getMessage());
    }

    @ExceptionHandler(MethodArgumentNotValidException.class)
    @ResponseStatus(HttpStatus.BAD_REQUEST)
    public ErrorResponse handleValidation(MethodArgumentNotValidException ex) {
        String message = ex.getBindingResult().getFieldErrors().stream()
            .map(e -> e.getField() + ": " + e.getDefaultMessage())
            .collect(Collectors.joining(", "));
        return new ErrorResponse("VALIDATION_ERROR", message);
    }
}
```

  

**8. Validation** 

Add `spring-boot-starter-validation` and annotate your DTOs:

```java
public class CreateUserRequest {
    @NotBlank(message = "Name is required")
    private String name;

    @Email(message = "Must be a valid email")
    @NotBlank
    private String email;

    @Size(min = 8, message = "Password must be at least 8 characters")
    private String password;
}
```

  

Use `@Valid` on the controller parameter (shown in section 4) to trigger validation.

**9. Bean Scopes & Lifecycle** 

|Scope|Behavior|
|---|---|
|`singleton` (default)|One instance per Spring container|
|`prototype`|New instance every time it’s injected|
|`request`|One per HTTP request|
|`session`|One per HTTP session|

**Lifecycle hooks:**
```java
@Service
public class CacheService {
    @PostConstruct
    public void init() {
        // runs after dependency injection
    }
    @PreDestroy
    public void cleanup() {
        // runs on shutdown
    }
}
```

**10. Actuator (Production Monitoring)** 
Add `spring-boot-starter-actuator` for built-in health checks, metrics, and info endpoints:
```yaml
management:
  endpoints:
    web:
      exposure:
        include: health, info, metrics, env
  endpoint:
    health:
      show-details: always
```
Hit `/actuator/health` to get:
```json
{
  "status": "UP",
  "components": {
    "db": { "status": "UP" },
    "diskSpace": { "status": "UP" }
  }
}
```

**11. Testing** 
```java
// Unit test — mock dependencies
@ExtendWith(MockitoExtension.class)
class OrderServiceTest {

    @Mock private PaymentGateway paymentGateway;
    @InjectMocks private OrderService orderService;

    @Test
    void placeOrder_chargesCorrectAmount() {
        Order order = new Order(new BigDecimal("99.99"));
        orderService.placeOrder(order);
        verify(paymentGateway).charge(new BigDecimal("99.99"));
    }
}

// Integration test — boots the full app
@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
class UserControllerIT {

    @Autowired private TestRestTemplate restTemplate;

    @Test
    void createUser_returns201() {
        var request = new CreateUserRequest("Alice", "alice@test.com", "password123");
        var response = restTemplate.postForEntity("/api/users", request, User.class);
        assertEquals(HttpStatus.CREATED, response.getStatusCode());
    }
}
```
**Quick Reference: Common Starters** 

| Starter                          | What it gives you                                 |
| -------------------------------- | ------------------------------------------------- |
| `spring-boot-starter-web`        | Embedded Tomcat, REST support, JSON serialization |
| `spring-boot-starter-data-jpa`   | Hibernate, Spring Data repositories               |
| `spring-boot-starter-security`   | Authentication & authorization                    |
| `spring-boot-starter-validation` | Bean validation (JSR 380)                         |
| `spring-boot-starter-actuator`   | Health checks, metrics, monitoring                |
| `spring-boot-starter-test`       | JUnit 5, Mockito, Spring Test                     |
This covers the core building blocks. Let me know if you want to go deeper into any area — security, async processing, caching, or anything else.

-------------

**1. DTO Mapping** 

Separate your entity from what you expose over the API:

```java
// DTO — what the API returns (no sensitive fields like SSN)
public record PatientResponse(
    Long id,
    String firstName,
    String lastName,
    String dateOfBirth
) {}

// DTO — what the API accepts
public record CreatePatientRequest(
    @NotBlank String firstName,
    @NotBlank String lastName,
    @NotBlank String dateOfBirth,
    @NotBlank String ssn,
    @NotBlank String diagnosis
) {}
```

**Mapper class:**
```java
@Component
public class PatientMapper {
    public PatientResponse toResponse(Patient entity) {
        return new PatientResponse(
            entity.getId(),
            entity.getFirstName(),
            entity.getLastName(),
            entity.getDateOfBirth().toString()
        );
    }
    public Patient toEntity(CreatePatientRequest request) {
        Patient p = new Patient();
        p.setFirstName(request.firstName());
        p.setLastName(request.lastName());
        p.setDateOfBirth(LocalDate.parse(request.dateOfBirth()));
        p.setSsn(request.ssn());
        p.setDiagnosis(request.diagnosis());
        return p;
    }
}
```

  ==**Why DTOs matter for HIPAA:** You never accidentally serialize PHI (SSN, diagnosis) into an API response. The DTO acts as a security boundary.==

**2. PHI Encryption with** `AttributeConverter` 
==Encrypt sensitive fields at rest transparently — JPA encrypts on write, decrypts on read.==

```java
@Converter
public class PhiEncryptor implements AttributeConverter<String, String> {
    // In production: load from a secrets manager (AWS KMS, Vault), NOT hardcoded
    private static final String KEY = System.getenv("PHI_ENCRYPTION_KEY");
    private static final String ALGORITHM = "AES/GCM/NoPadding";
    private static final int GCM_TAG_LENGTH = 128;
    private static final int IV_LENGTH = 12;
    @Override
    public String convertToDatabaseColumn(String plaintext) {
        if (plaintext == null) return null;
        try {
            SecretKeySpec keySpec = new SecretKeySpec(
                Base64.getDecoder().decode(KEY), "AES");
            byte[] iv = new byte[IV_LENGTH];
            SecureRandom.getInstanceStrong().nextBytes(iv);

            Cipher cipher = Cipher.getInstance(ALGORITHM);
            cipher.init(Cipher.ENCRYPT_MODE, keySpec,
                new GCMParameterSpec(GCM_TAG_LENGTH, iv));
            byte[] encrypted = cipher.doFinal(plaintext.getBytes(StandardCharsets.UTF_8));
            // Prepend IV to ciphertext so we can extract it on decryption
            byte[] combined = new byte[iv.length + encrypted.length];
            System.arraycopy(iv, 0, combined, 0, iv.length);
            System.arraycopy(encrypted, 0, combined, iv.length, encrypted.length);

            return Base64.getEncoder().encodeToString(combined);
        } catch (Exception e) {
            throw new RuntimeException("PHI encryption failed", e);
        }
    }
    @Override
    public String convertToEntityAttribute(String ciphertext) {
        if (ciphertext == null) return null;
        try {
            byte[] combined = Base64.getDecoder().decode(ciphertext);
            byte[] iv = Arrays.copyOfRange(combined, 0, IV_LENGTH);
            byte[] encrypted = Arrays.copyOfRange(combined, IV_LENGTH, combined.length);

            SecretKeySpec keySpec = new SecretKeySpec(
                Base64.getDecoder().decode(KEY), "AES");
            Cipher cipher = Cipher.getInstance(ALGORITHM);
            cipher.init(Cipher.DECRYPT_MODE, keySpec,
                new GCMParameterSpec(GCM_TAG_LENGTH, iv));

            return new String(cipher.doFinal(encrypted), StandardCharsets.UTF_8);
        } catch (Exception e) {
            throw new RuntimeException("PHI decryption failed", e);
        }
    }
}
```
**Using it on an entity:**
```java hl:10,13
@Entity
@Table(name = "patients")
public class Patient {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    private String firstName;
    private String lastName;
    private LocalDate dateOfBirth;
    @Convert(converter = PhiEncryptor.class)
    @Column(name = "ssn")
    private String ssn;  // encrypted at rest
    @Convert(converter = PhiEncryptor.class)
    @Column(name = "diagnosis", length = 2048)
    private String diagnosis;  // encrypted at rest
    // getters, setters
}
```
The database stores ciphertext. Your Java code works with plaintext. No changes needed in repository or service layers.
**3. Transaction Management** 
```java hl:14,28
@Service
public class PatientService {
    private final PatientRepository patientRepo;
    private final AuditLogRepository auditRepo;
    private final PatientMapper mapper;
    public PatientService(PatientRepository patientRepo,
                          AuditLogRepository auditRepo,
                          PatientMapper mapper) {
        this.patientRepo = patientRepo;
        this.auditRepo = auditRepo;
        this.mapper = mapper;
    }
    // Both saves succeed or both roll back
    @Transactional
    public PatientResponse createPatient(CreatePatientRequest request, String performedBy) {
        Patient patient = mapper.toEntity(request);
        patient = patientRepo.save(patient);
        // HIPAA requires audit trails for PHI access
        auditRepo.save(new AuditLog(
            "CREATE_PATIENT",
            patient.getId(),
            performedBy,
            Instant.now()
        ));
        return mapper.toResponse(patient);
    }
    // Read-only transaction — hints to the DB it can optimize
    @Transactional(readOnly = true)
    public PatientResponse getPatient(Long id, String requestedBy) {
        Patient patient = patientRepo.findById(id)
            .orElseThrow(() -> new ResourceNotFoundException("Patient", id));
        return mapper.toResponse(patient);
    }
    // Rollback only on specific exceptions
    @Transactional(rollbackFor = Exception.class)
    public void transferPatient(Long patientId, Long fromDoctorId, Long toDoctorId) {
        // multiple writes that must be atomic
    }
}
```
**Key rules:**
- `@Transactional` on public methods only (Spring proxies can’t intercept private/protected)
- Calling a `@Transactional` method from **within the same class** bypasses the proxy — the transaction won’t apply. Extract to a separate service if needed.
- ==Default: rolls back on unchecked exceptions (`RuntimeException`)==, commits on checked exceptions. Use `rollbackFor` to customize.
## **4. Spring Security with JWT + Role-Based Access** 
### **4a. Security Configuration** 
```java hl:3,22
@Configuration
@EnableWebSecurity
@EnableMethodSecurity  // enables @PreAuthorize
public class SecurityConfig {
    private final JwtAuthFilter jwtAuthFilter;
    public SecurityConfig(JwtAuthFilter jwtAuthFilter) {
        this.jwtAuthFilter = jwtAuthFilter;
    }
    @Bean
    public SecurityFilterChain filterChain(HttpSecurity http) throws Exception {
        http
            .csrf(csrf -> csrf.disable())  // stateless API, no CSRF needed
            .sessionManagement(sm -> sm
                .sessionCreationPolicy(SessionCreationPolicy.STATELESS))
            .authorizeHttpRequests(auth -> auth
                .requestMatchers("/api/auth/**").permitAll()
                .requestMatchers("/actuator/health").permitAll()
                .requestMatchers("/api/admin/**").hasRole("ADMIN")
                .requestMatchers("/api/patients/**").hasAnyRole("DOCTOR", "NURSE", "ADMIN")
                .anyRequest().authenticated()
            )
            .addFilterBefore(jwtAuthFilter, UsernamePasswordAuthenticationFilter.class);
        return http.build();
    }
    @Bean
    public PasswordEncoder passwordEncoder() {
        return new BCryptPasswordEncoder();
    }
    @Bean
    public AuthenticationManager authenticationManager(
            AuthenticationConfiguration config) throws Exception {
        return config.getAuthenticationManager();
    }
}
```

### **4b. JWT Utility** 
```java
@Component
public class JwtUtil {
    @Value("${app.jwt-secret}")
    private String secret;
    private static final long EXPIRATION_MS = 3600000; // 1 hour
    public String generateToken(UserDetails userDetails) {
        Map<String, Object> claims = Map.of(
            "roles", userDetails.getAuthorities().stream()
                .map(GrantedAuthority::getAuthority)
                .toList()
        );
        return Jwts.builder()
            .setClaims(claims)
            .setSubject(userDetails.getUsername())
            .setIssuedAt(new Date())
            .setExpiration(new Date(System.currentTimeMillis() + EXPIRATION_MS))
            .signWith(Keys.hmacShaKeyFor(secret.getBytes()), SignatureAlgorithm. HS256)
            .compact();
    }
    public String extractUsername(String token) {
        return extractClaim(token, Claims::getSubject);
    }
    public boolean isTokenValid(String token, UserDetails userDetails) {
        String username = extractUsername(token);
        return username.equals(userDetails.getUsername()) && !isTokenExpired(token);
    }
    private boolean isTokenExpired(String token) {
        return extractClaim(token, Claims::getExpiration).before(new Date());
    }
    private <T> T extractClaim(String token, Function<Claims, T> resolver) {
        Claims claims = Jwts.parserBuilder()
            .setSigningKey(Keys.hmacShaKeyFor(secret.getBytes()))
            .build()
            .parseClaimsJws(token)
            .getBody();
        return resolver.apply(claims);
    }
}
```

  ### **4c. JWT Authentication Filter** 

```java hl:2
@Component
public class JwtAuthFilter extends OncePerRequestFilter {
    private final JwtUtil jwtUtil;
    private final UserDetailsService userDetailsService;
    public JwtAuthFilter(JwtUtil jwtUtil, UserDetailsService userDetailsService) {
        this.jwtUtil = jwtUtil;
        this.userDetailsService = userDetailsService;
    }
    @Override
    protected void doFilterInternal(HttpServletRequest request,
                                     HttpServletResponse response,
                                     FilterChain chain) throws ServletException, IOException {
        String header = request.getHeader("Authorization");
        if (header == null || !header.startsWith("Bearer ")) {
            chain.doFilter(request, response);
            return;
        }
        String token = header.substring(7);
        String username = jwtUtil.extractUsername(token);
        if (username != null &&
            SecurityContextHolder.getContext().getAuthentication() == null) {
            UserDetails userDetails = userDetailsService.loadUserByUsername(username);
            if (jwtUtil.isTokenValid(token, userDetails)) {
                var authToken = new UsernamePasswordAuthenticationToken(
                    userDetails, null, userDetails.getAuthorities());
                authToken.setDetails(
                    new WebAuthenticationDetailsSource().buildDetails(request));
                SecurityContextHolder.getContext().setAuthentication(authToken);
            }
        }
        chain.doFilter(request, response);
    }
}
```

  ### **4d. Auth Controller (Login/Register)** 

```java hl:15-17,19,26
@RestController
@RequestMapping("/api/auth")
public class AuthController {
    private final AuthenticationManager authManager;
    private final UserService userService;
    private final JwtUtil jwtUtil;
    public AuthController(AuthenticationManager authManager,
                          UserService userService, JwtUtil jwtUtil) {
        this.authManager = authManager;
        this.userService = userService;
        this.jwtUtil = jwtUtil;
    }
    @PostMapping("/login")
    public AuthResponse login(@Valid @RequestBody LoginRequest request) {
        authManager.authenticate(
            new UsernamePasswordAuthenticationToken(
                request.username(), request.password()));
        UserDetails user = userService.loadUserByUsername(request.username());
        String token = jwtUtil.generateToken(user);
        return new AuthResponse(token);
    }
    @PostMapping("/register")
    @ResponseStatus(HttpStatus.CREATED)
    public AuthResponse register(@Valid @RequestBody RegisterRequest request) {
        UserDetails user = userService.register(request);
        String token = jwtUtil.generateToken(user);
        return new AuthResponse(token);
    }
}
```

### **4e. Method-Level Role Security** 

```java hl:9,20
@RestController
@RequestMapping("/api/patients")
public class PatientController {
    private final PatientService patientService;
    public PatientController(PatientService patientService) {
        this.patientService = patientService;
    }
    @GetMapping("/{id}")
    @PreAuthorize("hasAnyRole('DOCTOR', 'NURSE')")
    public PatientResponse getPatient(@PathVariable Long id, Principal principal) {
        return patientService.getPatient(id, principal.getName());
    }
    @PostMapping
    @PreAuthorize("hasRole('DOCTOR')")
    public PatientResponse create(@Valid @RequestBody CreatePatientRequest request,
                                   Principal principal) {
        return patientService.createPatient(request, principal.getName());
    }
    @DeleteMapping("/{id}")
    @PreAuthorize("hasRole('ADMIN')")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void delete(@PathVariable Long id) {
        patientService.delete(id);
    }
}
```

**Full Pattern Summary (How the layers connect)** 

```
HTTP Request
  → JwtAuthFilter (extracts & validates JWT, sets SecurityContext)
    → SecurityFilterChain (checks URL-level role permissions)
      → @RestController (validates DTO with @Valid, checks @PreAuthorize)
        → @Service (business logic, @Transactional boundaries)
          → @Repository (Spring Data JPA, auto-generated queries)
            → Entity (PhiEncryptor encrypts SSN/diagnosis before DB write)
              → Database (stores ciphertext)
```

  

This gives you defense in depth: authentication (JWT), authorization (roles at URL + method level), input validation (Bean Validation), data protection (PHI encryption), audit logging (transactional), and API safety (DTOs hiding sensitive fields).

Let me know if you want me to go deeper on any of these — testing the security layer, refresh tokens, CORS config, etc.