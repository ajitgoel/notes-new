# Spring Data JPA & Transactions

## Repository Hierarchy

```
Repository (marker)
  └── CrudRepository (CRUD methods)
       └── ListCrudRepository (returns List instead of Iterable)
            └── JpaRepository (flush, batch, paging)
```

```java
public interface UserRepository extends JpaRepository<User, Long> {
    // Derived queries — Spring generates SQL from method name
    List<User> findByEmailContainingIgnoreCase(String fragment);
    Optional<User> findByEmailAndActiveTrue(String email);
    List<User> findByAgeGreaterThanOrderByNameAsc(int age);
    long countByRole(Role role);
    boolean existsByEmail(String email);
    // JPQL
    @Query("SELECT u FROM User u WHERE u.department.name = :dept AND u.active = true")
    List<User> findActiveByDepartment(@Param("dept") String department);
    // Native SQL
    @Query(value = "SELECT * FROM users WHERE created_at > :since", nativeQuery = true)
    List<User> findRecentUsers(@Param("since") LocalDateTime since);
    // Projections
    @Query("SELECT u.name AS name, u.email AS email FROM User u WHERE u.role = :role")
    List<UserSummary> findSummariesByRole(@Param("role") Role role);
    // Modifying queries
    @Modifying
    @Query("UPDATE User u SET u.active = false WHERE u.lastLogin < :cutoff")
    int deactivateInactiveUsers(@Param("cutoff") LocalDateTime cutoff);
}
```

---

## Entity Mapping

```java
@Entity
@Table(name = "users", indexes = {
    @Index(name = "idx_email", columnList = "email", unique = true)
})
public class User {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false, length = 100)
    private String name;

    @Column(nullable = false, unique = true)
    private String email;

    @Enumerated(EnumType.STRING)
    private Role role;

    @OneToMany(mappedBy = "user", cascade = CascadeType.ALL, orphanRemoval = true)
    private List<Order> orders = new ArrayList<>();

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "department_id")
    private Department department;

    @CreatedDate
    private LocalDateTime createdAt;

    @LastModifiedDate
    private LocalDateTime updatedAt;

    @Version  // optimistic locking
    private Long version;
}
```

### Fetch types
| Relationship | Default | Recommendation |
|-------------|---------|----------------|
| @ManyToOne | EAGER | Switch to LAZY |
| @OneToOne | EAGER | Switch to LAZY |
| @OneToMany | LAZY | Keep LAZY |
| @ManyToMany | LAZY | Keep LAZY |

**Rule**: Always use LAZY. Fetch eagerly only via JOIN FETCH in JPQL when needed.

---

## @Transactional

```java
@Service
public class OrderService {

    @Transactional  // read-write, default propagation = REQUIRED
    public Order placeOrder(PlaceOrderRequest request) {
        User user = userRepo.findById(request.userId())
            .orElseThrow(() -> new ResourceNotFoundException("User", request.userId()));

        Order order = new Order(user, request.items());
        Order saved = orderRepo.save(order);

        inventoryService.reserve(request.items());  // also @Transactional
        notificationService.sendConfirmation(saved); // non-transactional OK

        return saved;
    }

    @Transactional(readOnly = true)  // optimizes: no dirty checking, read replicas
    public Page<Order> findOrders(String userId, Pageable pageable) {
        return orderRepo.findByUserId(userId, pageable);
    }

    @Transactional(propagation = Propagation.REQUIRES_NEW)
    public void logAuditEvent(AuditEvent event) {
        // Runs in a NEW transaction — commits even if outer tx rolls back
        auditRepo.save(event);
    }
}
```

### Propagation types
| Type | Behavior |
|------|----------|
| REQUIRED | Join existing tx or create new (default) |
| REQUIRES_NEW | Always create new tx, suspend existing |
| MANDATORY | Must run inside existing tx or throw |
| SUPPORTS | Run in tx if one exists, otherwise non-tx |
| NOT_SUPPORTED | Suspend existing tx, run non-tx |
| NEVER | Throw if tx exists |

### Common pitfall: self-invocation
```java
@Service
public class UserService {
    public void doWork() {
        this.internalMethod(); // ⚠️ @Transactional IGNORED — no proxy
    }

    @Transactional
    public void internalMethod() { ... }
}
// Fix: inject self, extract to another bean, or use TransactionTemplate
```

---

## N+1 in JPA

```java
// Problem: loads department lazily for EACH user
List<User> users = userRepo.findAll(); // 1 query
users.forEach(u -> u.getDepartment().getName()); // N queries

// Fix: JOIN FETCH
@Query("SELECT u FROM User u JOIN FETCH u.department")
List<User> findAllWithDepartment();

// Or: @EntityGraph
@EntityGraph(attributePaths = {"department", "orders"})
List<User> findByActiveTrue();
```

---

## Interview Questions & Answers

### 1. Explain `@Transactional` propagation. When would you use `REQUIRES_NEW`?

Propagation determines how a transactional method behaves when called within an existing transaction:

- **REQUIRED** (default): Join the existing transaction if one exists, otherwise create a new one. Most common — 95% of cases.
- **REQUIRES_NEW**: Always create a new, independent transaction. Suspends the existing one. If the new transaction rolls back, the outer transaction is unaffected.
- **MANDATORY**: Must run inside an existing transaction. Throws `IllegalTransactionStateException` if none exists.
- **SUPPORTS**: Run in a transaction if one exists, otherwise run non-transactionally.
- **NOT_SUPPORTED**: Suspend any existing transaction and run non-transactionally.
- **NEVER**: Throw if a transaction exists.

**Use `REQUIRES_NEW` for**: audit logging (must persist even if the business transaction rolls back), notification records (send confirmation even if a later step fails), or any operation that must commit independently of the outer transaction.

```java
@Transactional
public void placeOrder(OrderRequest req) {
    orderRepo.save(order);          // Part of main transaction
    auditService.logEvent(event);   // REQUIRES_NEW — commits even if placeOrder rolls back
    inventoryService.reserve(items); // If this throws, order rolls back but audit log persists
}
```

### 2. What happens when a `@Transactional` method calls another `@Transactional` method in the same class?

The `@Transactional` annotation on the inner method is **ignored**. Spring implements transactions via AOP proxies — when an external caller invokes a method, the proxy intercepts it and manages the transaction. But when a method calls `this.internalMethod()`, it bypasses the proxy entirely; it's a direct Java method call.

```java
@Service
public class UserService {
    public void outerMethod() {
        this.innerMethod(); // ⚠️ No proxy — @Transactional is ignored
    }

    @Transactional
    public void innerMethod() { ... } // Runs WITHOUT a transaction
}
```

**Fixes**:
1. **Inject self**: `@Autowired UserService self; self.innerMethod();` — goes through the proxy
2. **Extract to another bean**: Move `innerMethod` to a separate `@Service` class
3. **Use `TransactionTemplate`**: Programmatic transaction management, no proxy needed
4. **AspectJ weaving**: Compile-time weaving that instruments the actual class (rare, complex)

### 3. How does Spring Data generate queries from method names? What are the limits?

Spring Data parses the method name into parts: `findBy` + property + operator + `And`/`Or` + more properties. Examples:

- `findByEmail` → `WHERE email = ?`
- `findByNameContainingIgnoreCase` → `WHERE LOWER(name) LIKE LOWER('%?%')`
- `findByAgeGreaterThanOrderByNameAsc` → `WHERE age > ? ORDER BY name ASC`
- `countByRole` → `SELECT COUNT(*) WHERE role = ?`

**Limits**:
- Can't express complex joins (JOINs across more than 2 tables)
- No subqueries
- No grouping or aggregation (`GROUP BY`, `HAVING`)
- Method names become unreadable for complex queries: `findByDepartmentNameAndRoleAndActiveTrueAndAgeBetweenOrderByCreatedAtDesc` is a sign you need `@Query`
- Can't do projections (select specific columns) without `@Query`
- No support for database-specific functions

Rule of thumb: derived queries for simple lookups (1-2 conditions), `@Query` with JPQL for anything more complex.

### 4. How do you solve the N+1 problem in JPA? Compare JOIN FETCH vs @EntityGraph.

**The problem**: Loading a list of entities and then accessing a lazy relationship triggers a separate query per entity.

**JOIN FETCH** (JPQL):
```java
@Query("SELECT u FROM User u JOIN FETCH u.department JOIN FETCH u.orders")
List<User> findAllWithDepartmentAndOrders();
```
Explicit, full control. Can add WHERE clauses, ordering. But: can't easily make it conditional (always fetches), and can cause Cartesian product issues with multiple `*ToMany` joins (use `Set` or `@BatchSize`).

**@EntityGraph** (declarative):
```java
@EntityGraph(attributePaths = {"department", "orders"})
List<User> findByActiveTrue();
```
Cleaner, works with derived queries. Spring generates a LEFT JOIN. But: less control, can't add custom WHERE clauses, and the same Cartesian product risk.

**Batch fetching** (Hibernate config):
```properties
spring.jpa.properties.hibernate.default_batch_fetch_size=25
```
Instead of 100 individual queries, Hibernate groups them: `WHERE department_id IN (?, ?, ..., ?)` with 25 IDs at a time. Works globally without code changes. Reduces N+1 to N/25+1.

**Best approach**: Use `@BatchSize` globally as a safety net, and explicit `JOIN FETCH` for hot paths where you know the access pattern.

### => 5. What is optimistic locking (`@Version`)? How does it differ from pessimistic locking?
==**Optimistic locking**== assumes conflicts are rare. ==A `@Version` field (integer or timestamp) tracks the entity version. On update, Hibernate adds `WHERE version = :currentVersion` to the UPDATE. If another transaction modified the row (version changed), the WHERE clause matches zero rows, and Hibernate throws `OptimisticLockException`. The application catches this and retries or informs the user.==
```java
@Version private Long version;
// UPDATE users SET name = ?, version = 3 WHERE id = 1 AND version = 2
// If version is now 3 (someone else updated), 0 rows affected → exception
```
==**Pessimistic locking** locks the row in the database: `SELECT ... FOR UPDATE`.== Other transactions block until the lock is released. Guarantees no conflicts but reduces throughput.

| Aspect | Optimistic | Pessimistic |
|--------|-----------|-------------|
| Concurrency | High | Low (blocks) |
| Conflict handling | Detect + retry | Prevent |
| Use when | Read-heavy, rare conflicts | Write-heavy, frequent conflicts |
| Database load | Lower | Higher (lock management) |
| Deadlock risk | None | Yes |

### 6. Why should you default all relationships to LAZY fetch?

EAGER fetching loads related entities immediately when the parent is loaded, whether you need them or not. With `@ManyToOne(fetch = EAGER)` on every relationship, loading a `User` might trigger: load department → load department's company → load company's CEO → load CEO's orders → ... cascading into dozens of queries.

LAZY fetching loads relationships only when accessed. This means `userRepo.findAll()` only queries the users table. If you need departments, you explicitly fetch them via `JOIN FETCH` or `@EntityGraph`.

**Why LAZY is the default recommendation**:
- **Performance**: You only pay for what you use. Most API calls don't need every relationship.
- **Predictability**: You can see exactly which queries run by looking at your JPQL/EntityGraph declarations.
- **N+1 is controllable**: When you know you need a relationship, you use `JOIN FETCH` intentionally. With EAGER, the N+1 happens silently and you may not notice until production.

The JPA defaults are `EAGER` for `@ManyToOne` and `@OneToOne` — always override these to `LAZY`.
