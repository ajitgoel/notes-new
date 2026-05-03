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

## Solution

<details>
<summary>@BatchMapping approach (click to reveal)</summary>

```java
@BatchMapping
public Map<Product, List<Review>> reviews(List<Product> products) {
    Set<String> ids = products.stream()
        .map(Product::getId)
        .collect(Collectors.toSet());
    Map<String, List<Review>> reviewsByProductId =
        reviewService.findByProductIds(ids);
    return products.stream().collect(Collectors.toMap(
        p -> p,
        p -> reviewsByProductId.getOrDefault(p.getId(), List.of())
    ));
}
```
</details>

<details>
<summary>Manual DataLoader approach (click to reveal)</summary>

```java
@Configuration
public class DataLoaderConfig implements BatchLoaderRegistry {
    @Override
    public void registerBatchLoaders(BatchLoaderRegistry registry) {
        registry.forTypePair(String.class, User.class)
            .registerMappedBatchLoader((sellerIds, env) ->
                Mono.fromCallable(() ->
                    userService.findByIds(sellerIds)
                )
            );
    }
}

// In controller
@SchemaMapping(typeName = "Product")
public CompletableFuture<User> seller(
        Product product,
        DataLoader<String, User> sellerLoader) {
    return sellerLoader.load(product.getSellerId());
}
```
</details>
