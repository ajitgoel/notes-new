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
@Table("products")
public class Product {
    @Id
    private Long id;
    private String name;
    private String description;
    private BigDecimal price;
    private String category;
    private boolean inStock;
    @CreatedDate
    private LocalDateTime createdAt;
    // constructors, getters, setters
    public Product() {}
    public Product(String name, String description, BigDecimal price,
                   String category, boolean inStock) {
        this.name = name;
        this.description = description;
        this.price = price;
        this.category = category;
        this.inStock = inStock;
    }
}
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
    Flux<Product> findByCategory(String category);
    @Query("SELECT * FROM products WHERE LOWER(name) LIKE LOWER(CONCAT('%', :keyword, '%'))")
    Flux<Product> findByNameContaining(String keyword);
    @Query("SELECT * FROM products WHERE in_stock = true AND price <= :maxPrice")
    Flux<Product> findInStockUnderPrice(BigDecimal maxPrice);
    @Query("SELECT category, COUNT(*) as count FROM products GROUP BY category")
    Flux<CategoryCount> countByCategory();
}
public record CategoryCount(String category, long count) {}
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

---

## Complete Solution

### Entity

```java
@Table("products")
public class Product {
    @Id
    private Long id;
    private String name;
    private String description;
    private BigDecimal price;
    private String category;
    private boolean inStock;

    @CreatedDate
    private LocalDateTime createdAt;

    // constructors, getters, setters
    public Product() {}

    public Product(String name, String description, BigDecimal price,
                   String category, boolean inStock) {
        this.name = name;
        this.description = description;
        this.price = price;
        this.category = category;
        this.inStock = inStock;
    }
}
```

### Repository

```java
public interface ProductRepository extends ReactiveCrudRepository<Product, Long> {

    Flux<Product> findByCategory(String category);

    @Query("SELECT * FROM products WHERE LOWER(name) LIKE LOWER(CONCAT('%', :keyword, '%'))")
    Flux<Product> findByNameContaining(String keyword);

    @Query("SELECT * FROM products WHERE in_stock = true AND price <= :maxPrice")
    Flux<Product> findInStockUnderPrice(BigDecimal maxPrice);

    @Query("SELECT category, COUNT(*) as count FROM products GROUP BY category")
    Flux<CategoryCount> countByCategory();
}

public record CategoryCount(String category, long count) {}
```

### Service

```java
@Service
public class ProductService {

    private final ProductRepository productRepo;

    @Transactional(readOnly = true)
    public Mono<Product> findById(Long id) {
        return productRepo.findById(id)
            .switchIfEmpty(Mono.error(
                new NotFoundException("Product not found: " + id)));
    }

    @Transactional(readOnly = true)
    public Flux<Product> search(String category, String keyword,
                                BigDecimal maxPrice) {
        if (category != null) {
            return productRepo.findByCategory(category);
        }
        if (keyword != null) {
            return productRepo.findByNameContaining(keyword);
        }
        if (maxPrice != null) {
            return productRepo.findInStockUnderPrice(maxPrice);
        }
        return productRepo.findAll();
    }

    @Transactional
    public Mono<Product> create(CreateProductRequest request) {
        Product product = new Product(
            request.name(), request.description(),
            request.price(), request.category(), true
        );
        return productRepo.save(product);
    }

    @Transactional
    public Mono<Product> update(Long id, UpdateProductRequest request) {
        return productRepo.findById(id)
            .switchIfEmpty(Mono.error(new NotFoundException("Product not found: " + id)))
            .flatMap(existing -> {
                if (request.name() != null) existing.setName(request.name());
                if (request.description() != null) existing.setDescription(request.description());
                if (request.price() != null) existing.setPrice(request.price());
                if (request.category() != null) existing.setCategory(request.category());
                if (request.inStock() != null) existing.setInStock(request.inStock());
                return productRepo.save(existing);
            });
    }

    @Transactional
    public Mono<Void> delete(Long id) {
        return productRepo.findById(id)
            .switchIfEmpty(Mono.error(new NotFoundException("Product not found: " + id)))
            .flatMap(productRepo::delete);
    }
}
```

### Controller

```java
@RestController
@RequestMapping("/api/products")
public class ProductController {

    private final ProductService productService;
    private final EnrichmentService enrichmentService;

    @GetMapping("/{id}")
    public Mono<ResponseEntity<Product>> getProduct(@PathVariable Long id) {
        return productService.findById(id)
            .map(ResponseEntity::ok)
            .onErrorResume(NotFoundException.class,
                ex -> Mono.just(ResponseEntity.notFound().build()));
    }

    @GetMapping
    public Flux<Product> searchProducts(
            @RequestParam(required = false) String category,
            @RequestParam(required = false) String keyword,
            @RequestParam(required = false) BigDecimal maxPrice) {
        return productService.search(category, keyword, maxPrice);
    }

    @PostMapping
    public Mono<ResponseEntity<Product>> createProduct(
            @Valid @RequestBody Mono<CreateProductRequest> request) {
        return request.flatMap(productService::create)
            .map(product -> ResponseEntity
                .created(URI.create("/api/products/" + product.getId()))
                .body(product));
    }

    @PutMapping("/{id}")
    public Mono<ResponseEntity<Product>> updateProduct(
            @PathVariable Long id,
            @Valid @RequestBody UpdateProductRequest request) {
        return productService.update(id, request)
            .map(ResponseEntity::ok)
            .onErrorResume(NotFoundException.class,
                ex -> Mono.just(ResponseEntity.notFound().build()));
    }

    @DeleteMapping("/{id}")
    public Mono<ResponseEntity<Void>> deleteProduct(@PathVariable Long id) {
        return productService.delete(id)
            .thenReturn(ResponseEntity.noContent().<Void>build())
            .onErrorResume(NotFoundException.class,
                ex -> Mono.just(ResponseEntity.notFound().build()));
    }

    @GetMapping(value = "/stream", produces = MediaType.TEXT_EVENT_STREAM_VALUE)
    public Flux<Product> streamProducts() {
        return productService.search(null, null, null)
            .delayElements(Duration.ofMillis(200));
    }

    @GetMapping("/{id}/enriched")
    public Mono<EnrichedProduct> getEnrichedProduct(@PathVariable Long id) {
        return enrichmentService.getEnriched(id);
    }
}
```

### WebClient Integration (Enrichment Service)

```java
@Service
public class EnrichmentService {

    private final ProductService productService;
    private final WebClient pricingClient;
    private final WebClient reviewClient;

    public EnrichmentService(ProductService productService,
                             WebClient.Builder builder) {
        this.productService = productService;
        this.pricingClient = builder.baseUrl("http://pricing-api:8081").build();
        this.reviewClient = builder.baseUrl("http://review-api:8082").build();
    }

    public Mono<EnrichedProduct> getEnriched(Long id) {
        return productService.findById(id)
            .flatMap(product -> {
                Mono<BigDecimal> priceMono = pricingClient.get()
                    .uri("/api/pricing/{sku}", product.getId())
                    .retrieve()
                    .bodyToMono(PricingResponse.class)
                    .map(PricingResponse::price)
                    .timeout(Duration.ofSeconds(3))
                    .onErrorReturn(product.getPrice());  // fallback to own price

                Mono<List<Review>> reviewsMono = reviewClient.get()
                    .uri("/api/reviews?productId={id}", product.getId())
                    .retrieve()
                    .bodyToFlux(Review.class)
                    .collectList()
                    .timeout(Duration.ofSeconds(3))
                    .onErrorReturn(List.of());  // fallback to empty list

                return Mono.zip(priceMono, reviewsMono,
                    (price, reviews) -> new EnrichedProduct(
                        product, price, reviews
                    ));
            });
    }
}

public record EnrichedProduct(Product product, BigDecimal currentPrice,
                               List<Review> reviews) {}
```

### Tests

```java
@SpringBootTest
@AutoConfigureWebTestClient
class ProductControllerTest {

    @Autowired WebTestClient client;
    @Autowired ProductRepository productRepo;

    @BeforeEach
    void setup() {
        productRepo.deleteAll().block();
    }

    @Test
    void createProduct_returns201() {
        client.post().uri("/api/products")
            .contentType(MediaType.APPLICATION_JSON)
            .bodyValue(new CreateProductRequest("Laptop", "A laptop",
                BigDecimal.valueOf(999.99), "ELECTRONICS"))
            .exchange()
            .expectStatus().isCreated()
            .expectBody()
            .jsonPath("$.name").isEqualTo("Laptop")
            .jsonPath("$.id").isNumber();
    }

    @Test
    void getById_returns200() {
        Product saved = productRepo.save(
            new Product("Phone", "A phone", BigDecimal.valueOf(599), "ELECTRONICS", true)
        ).block();

        client.get().uri("/api/products/{id}", saved.getId())
            .exchange()
            .expectStatus().isOk()
            .expectBody()
            .jsonPath("$.name").isEqualTo("Phone");
    }

    @Test
    void getNonexistent_returns404() {
        client.get().uri("/api/products/{id}", 99999)
            .exchange()
            .expectStatus().isNotFound();
    }

    @Test
    void updateProduct_returns200() {
        Product saved = productRepo.save(
            new Product("Old Name", null, BigDecimal.TEN, "BOOKS", true)
        ).block();

        client.put().uri("/api/products/{id}", saved.getId())
            .contentType(MediaType.APPLICATION_JSON)
            .bodyValue(Map.of("name", "New Name"))
            .exchange()
            .expectStatus().isOk()
            .expectBody()
            .jsonPath("$.name").isEqualTo("New Name");
    }

    @Test
    void deleteProduct_returns204_thenGetReturns404() {
        Product saved = productRepo.save(
            new Product("ToDelete", null, BigDecimal.ONE, "BOOKS", true)
        ).block();

        client.delete().uri("/api/products/{id}", saved.getId())
            .exchange()
            .expectStatus().isNoContent();

        client.get().uri("/api/products/{id}", saved.getId())
            .exchange()
            .expectStatus().isNotFound();
    }

    @Test
    void searchWithFilters_returnsFilteredResults() {
        productRepo.save(new Product("Laptop", null, BigDecimal.valueOf(999), "ELECTRONICS", true)).block();
        productRepo.save(new Product("Book", null, BigDecimal.valueOf(20), "BOOKS", true)).block();

        client.get().uri("/api/products?category=ELECTRONICS")
            .exchange()
            .expectStatus().isOk()
            .expectBodyList(Product.class)
            .hasSize(1)
            .value(products -> {
                assert products.get(0).getName().equals("Laptop");
            });
    }

    @Test
    void streamProducts_returnsSSE() {
        productRepo.save(new Product("P1", null, BigDecimal.TEN, "A", true)).block();
        productRepo.save(new Product("P2", null, BigDecimal.TEN, "B", true)).block();

        client.get().uri("/api/products/stream")
            .accept(MediaType.TEXT_EVENT_STREAM)
            .exchange()
            .expectStatus().isOk()
            .expectBodyList(Product.class)
            .hasSize(2);
    }
}
```
