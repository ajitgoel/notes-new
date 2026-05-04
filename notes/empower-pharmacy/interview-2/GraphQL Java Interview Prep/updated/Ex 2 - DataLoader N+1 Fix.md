# Exercise 2: Fix the N+1 Problem with DataLoader

## Scenario

You have this working code, but it makes N+1 calls to the review service:

```java
@Controller
public class ProductController {

    @QueryMapping
    public List<Product> products() {
        return productService.findAll(); // 1 call
    }

    @SchemaMapping(typeName = "Product")
    public List<Review> reviews(Product product) {
        // Called once PER product — N+1 problem!
        return reviewService.findByProductId(product.getId());
    }

    @SchemaMapping(typeName = "Product")
    public User seller(Product product) {
        // Also N+1!
        return userService.findById(product.getSellerId());
    }
}
```

When a client queries:
```graphql
query {
  products {
    name
    reviews { rating comment }
    seller { name }
  }
}
```

With 20 products, this makes: 1 (products) + 20 (reviews) + 20 (sellers) = **41 calls**.

---

## Task 1: Fix with @BatchMapping (Spring)

Refactor the `reviews` resolver to use `@BatchMapping`:

```java
// TODO: Replace @SchemaMapping with @BatchMapping
// The method should accept List<Product> and return Map<Product, List<Review>>
// The service call should be: reviewService.findByProductIds(Set<String> ids)
```

---

## Task 2: Fix with DataLoader (manual)

Refactor the `seller` resolver to use a registered DataLoader:

```java
// TODO:
// 1. Create a BatchLoaderRegistry bean that registers a "sellerLoader"
//    - accepts Set<String> sellerIds
//    - calls userService.findByIds(sellerIds) which returns Map<String, User>
//
// 2. Refactor the seller resolver to use the DataLoader
//    - return CompletableFuture<User>
//    - use DataLoader<String, User> parameter
```

---

## Task 3: Verify Batching

Write a test that proves the fix works:

```java
@Test
void productsList_shouldBatchReviewsAndSellers() {
    // TODO:
    // 1. Query products { name reviews { rating } seller { name } }
    // 2. Verify reviewService.findByProductIds was called exactly ONCE
    // 3. Verify userService.findByIds was called exactly ONCE
}
```

---

## Acceptance Criteria

- [ ] Querying 20 products with reviews + sellers makes exactly 3 calls (not 41)
- [ ] The test proves batching via mock verification
- [ ] Both approaches work: @BatchMapping and manual DataLoader

---

## Complete Solution

### @BatchMapping Approach (reviews)

```java
@Controller
public class ProductController {

    @QueryMapping
    public List<Product> products() {
        return productService.findAll(); // 1 query
    }

    // @BatchMapping replaces the N+1 @SchemaMapping
    // Spring collects all Product parents, calls this ONCE
    @BatchMapping
    public Map<Product, List<Review>> reviews(List<Product> products) {
        Set<String> ids = products.stream()
            .map(Product::getId)
            .collect(Collectors.toSet());

        // 1 batch query: SELECT * FROM reviews WHERE product_id IN (?, ?, ...)
        Map<String, List<Review>> reviewsByProductId =
            reviewService.findByProductIds(ids);

        return products.stream().collect(Collectors.toMap(
            p -> p,
            p -> reviewsByProductId.getOrDefault(p.getId(), List.of())
        ));
    }
}
```

### Manual DataLoader Approach (seller)

```java
// Step 1: Register the DataLoader
@Configuration
public class DataLoaderConfig {

    @Bean
    public BatchLoaderRegistry batchLoaderRegistry(UserService userService) {
        return registry -> registry
            .forTypePair(String.class, User.class)
            .registerMappedBatchLoader((sellerIds, env) ->
                Mono.fromCallable(() -> userService.findByIds(sellerIds))
                // returns Map<String, User>
            );
    }
}

// Step 2: Use in the resolver
@Controller
public class ProductController {

    @SchemaMapping(typeName = "Product")
    public CompletableFuture<User> seller(
            Product product,
            DataLoader<String, User> sellerLoader) {
        // DataLoader queues this — doesn't execute immediately
        // At the end of the execution level, all queued IDs are
        // dispatched as a single batch call
        return sellerLoader.load(product.getSellerId());
    }
}
```

### Service Layer (Batch Methods)

```java
@Service
public class ReviewService {

    // Batch method: 1 query for all product IDs
    public Map<String, List<Review>> findByProductIds(Set<String> productIds) {
        // SQL: SELECT * FROM reviews WHERE product_id IN (?, ?, ...)
        List<Review> allReviews = reviewRepo.findByProductIdIn(productIds);
        return allReviews.stream()
            .collect(Collectors.groupingBy(Review::getProductId));
    }
}

@Service
public class UserService {

    // Batch method: 1 query for all seller IDs
    public Map<String, User> findByIds(Set<String> userIds) {
        // SQL: SELECT * FROM users WHERE id IN (?, ?, ...)
        List<User> users = userRepo.findByIdIn(userIds);
        return users.stream()
            .collect(Collectors.toMap(User::getId, u -> u));
    }
}
```

### Verification Test

```java
@SpringBootTest
@AutoConfigureHttpGraphQlTester
class BatchingVerificationTest {

    @Autowired HttpGraphQlTester tester;
    @MockBean ReviewService reviewService;
    @MockBean UserService userService;

    @Test
    void productsList_shouldBatchReviewsAndSellers() {
        // Setup: return data for batch calls
        when(reviewService.findByProductIds(anySet()))
            .thenReturn(Map.of(
                "1", List.of(new Review("r1", "1", 5, "Great", "Alice")),
                "2", List.of(new Review("r2", "2", 4, "Good", "Bob"))
            ));
        when(userService.findByIds(anySet()))
            .thenReturn(Map.of(
                "seller1", new User("seller1", "Seller One"),
                "seller2", new User("seller2", "Seller Two")
            ));

        // Execute: query 20 products with nested reviews + seller
        tester.document("""
            query {
              products {
                name
                reviews { rating comment }
                seller { name }
              }
            }
            """)
            .execute()
            .path("products").entityList(Object.class).hasSizeGreaterThan(0);

        // Verify: batch methods called exactly ONCE (not N times)
        verify(reviewService, times(1)).findByProductIds(anySet());
        verify(userService, times(1)).findByIds(anySet());
    }
}
```
