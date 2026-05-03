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

## Interview Questions

1. Explain `@Transactional` propagation. When would you use `REQUIRES_NEW`?
2. What happens when a `@Transactional` method calls another `@Transactional` method in the same class?
3. How does Spring Data generate queries from method names? What are the limits?
4. How do you solve the N+1 problem in JPA? Compare JOIN FETCH vs @EntityGraph.
5. What is optimistic locking (`@Version`)? How does it differ from pessimistic locking?
6. Why should you default all relationships to LAZY fetch?
