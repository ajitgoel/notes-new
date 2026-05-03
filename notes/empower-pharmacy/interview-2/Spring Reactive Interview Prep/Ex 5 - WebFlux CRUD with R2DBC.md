# Exercise 5: WebFlux CRUD with R2DBC

## Objective
Build a reactive REST API for a product catalog using WebFlux + R2DBC.

---

## Task 1: Setup & Entity

```yaml
# application.yml
spring:
  r2dbc:
    url: r2dbc:h2:mem:///testdb
    username: sa
    password:
  sql:
    init:
      mode: always
```

```sql
-- schema.sql
CREATE TABLE products (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    description VARCHAR(2000),
    price DECIMAL(10,2) NOT NULL,
    category VARCHAR(50) NOT NULL,
    in_stock BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

```java
// TODO: Create the Product entity with @Table, @Id
// Remember: R2DBC doesn't use JPA annotations
```

---

## Task 2: Repository

```java
public interface ProductRepository extends ReactiveCrudRepository<Product, Long> {

    // TODO: Add these methods
    // 1. Find by category → Flux<Product>
    // 2. Find by name containing (case-insensitive) → Flux<Product>
    // 3. Find in stock products with price less than max → Flux<Product>
    // 4. Custom @Query: count products per category → Flux<CategoryCount>
}
```

---

## Task 3: Service Layer

```java
@Service
public class ProductService {

    // TODO: Implement these reactive methods

    public Mono<Product> findById(Long id) {
        // Return product or Mono.error(NotFoundException)
    }

    public Flux<Product> search(String category, String keyword,
                                BigDecimal maxPrice) {
        // Apply filters dynamically
        // If category provided, filter by category
        // If keyword provided, filter by name containing keyword
        // If maxPrice provided, filter by price <= maxPrice
    }

    @Transactional
    public Mono<Product> create(CreateProductRequest request) {
        // Validate: name must be unique
        // Map request → entity → save
    }

    @Transactional
    public Mono<Product> update(Long id, UpdateProductRequest request) {
        // Find existing → apply changes → save
        // If not found → error
    }

    @Transactional
    public Mono<Void> delete(Long id) {
        // Verify exists → delete
    }
}
```

---

## Task 4: Controller

```java
@RestController
@RequestMapping("/api/products")
public class ProductController {

    // TODO: Implement all endpoints

    // GET /api/products/{id} → Mono<ResponseEntity<Product>>
    //   200 with product OR 404

    // GET /api/products?category=X&keyword=Y&maxPrice=Z → Flux<Product>
    //   Returns streaming JSON array

    // POST /api/products → Mono<ResponseEntity<Product>>
    //   201 Created with Location header

    // PUT /api/products/{id} → Mono<ResponseEntity<Product>>
    //   200 with updated OR 404

    // DELETE /api/products/{id} → Mono<ResponseEntity<Void>>
    //   204 No Content OR 404

    // GET /api/products/stream (produces TEXT_EVENT_STREAM_VALUE)
    //   → Flux<Product> as SSE
}
```

---

## Task 5: WebClient Integration

```java
// TODO: Add a /api/products/{id}/enriched endpoint that:
// 1. Fetches the product from local DB
// 2. Calls an external pricing API: GET /api/pricing/{sku}
//    (use WebClient)
// 3. Calls an external review API: GET /api/reviews?productId={id}
// 4. Combines all three into EnrichedProduct
// 5. The two external calls should run in parallel
// 6. If pricing fails → use product's own price
// 7. If reviews fail → return empty list
// 8. 3-second timeout on external calls
```

---

## Task 6: Tests

```java
@SpringBootTest
@AutoConfigureWebTestClient
class ProductControllerTest {

    @Autowired WebTestClient client;

    // TODO: Write tests for:
    // 1. POST creates product → 201 with body
    // 2. GET by id → 200
    // 3. GET nonexistent → 404
    // 4. PUT updates fields → 200 with updated body
    // 5. DELETE → 204, then GET → 404
    // 6. GET with filters returns filtered results
    // 7. SSE stream returns products
}
```

---

## Acceptance Criteria

- [ ] Full CRUD works reactively (no blocking calls)
- [ ] R2DBC queries execute correctly
- [ ] SSE streaming endpoint works
- [ ] WebClient calls are parallel with timeout + fallback
- [ ] All 7 tests pass with WebTestClient
