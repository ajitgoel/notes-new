# Exercise 5: Cursor Pagination & Filtering

## Scenario

Your product catalog has thousands of items. Implement Relay-style cursor pagination with filtering.

---

## Task 1: Define the Connection Schema

```graphql
# TODO: Implement the full Relay connection spec

type Query {
  products(
    first: Int
    after: String
    last: Int
    before: String
    filter: ProductFilter
  ): ProductConnection!
}

# TODO: Define:
# - ProductConnection (edges, pageInfo, totalCount)
# - ProductEdge (node, cursor)
# - PageInfo (hasNextPage, hasPreviousPage, startCursor, endCursor)
# - ProductFilter input (category, minPrice, maxPrice, inStock, searchTerm)
```

---

## Task 2: Implement Cursor Encoding/Decoding

```java
// TODO: Implement cursor utilities
// Cursors should be opaque to clients (Base64-encoded)
// Internally they encode the sort field value + ID for stability

public class CursorUtils {

    // Encode a cursor from a product
    public static String encode(Product product) {
        // Encode: "price:29.99:id:abc123" → Base64
    }

    // Decode a cursor to get the position
    public static CursorPosition decode(String cursor) {
        // Decode Base64 → extract sort value and ID
    }
}
```

---

## Task 3: Implement the Repository Query

```java
// TODO: Implement cursor-based pagination at the DB level
// Use Spring Data JPA or JDBC

public interface ProductRepository {

    // Forward pagination: after cursor, first N
    // SQL: WHERE (price, id) > (:cursorPrice, :cursorId)
    //      AND <filters>
    //      ORDER BY price ASC, id ASC
    //      LIMIT :first + 1  (extra one to check hasNextPage)

    ProductPage findProducts(
        String afterCursor,
        String beforeCursor,
        Integer first,
        Integer last,
        ProductFilter filter
    );
}
```

---

## Task 4: Implement the Controller

```java
@Controller
public class ProductController {

    @QueryMapping
    public ProductConnection products(
            @Argument Integer first,
            @Argument String after,
            @Argument Integer last,
            @Argument String before,
            @Argument ProductFilter filter) {
        // TODO:
        // 1. Validate: must provide first/after OR last/before (not both)
        // 2. Fetch first+1 items to determine hasNextPage
        // 3. Build edges with cursors
        // 4. Build pageInfo
        // 5. Return connection
    }
}
```

---

## Task 5: Test Pagination

```java
@Test
void firstPage() {
    // query { products(first: 5) { edges { node { name } cursor } pageInfo { hasNextPage endCursor } } }
    // Assert: 5 edges, hasNextPage = true
}

@Test
void secondPage() {
    // query { products(first: 5, after: "<endCursor from page 1>") { ... } }
    // Assert: next 5 items, no overlap with page 1
}

@Test
void filteredPagination() {
    // query { products(first: 10, filter: { category: ELECTRONICS, minPrice: 50 }) { ... } }
    // Assert: all returned products match the filter
}
```

---

## Solution

<details>
<summary>Connection builder (click to reveal)</summary>

```java
@QueryMapping
public ProductConnection products(
        @Argument Integer first,
        @Argument String after,
        @Argument Integer last,
        @Argument String before,
        @Argument ProductFilter filter) {

    int limit = first != null ? first : (last != null ? last : 20);

    // Fetch limit + 1 to detect if there's a next page
    List<Product> items = productRepository.findProducts(
        after, before, limit + 1, filter
    );

    boolean hasMore = items.size() > limit;
    if (hasMore) {
        items = items.subList(0, limit);
    }

    List<ProductEdge> edges = items.stream()
        .map(p -> new ProductEdge(p, CursorUtils.encode(p)))
        .toList();

    PageInfo pageInfo = new PageInfo(
        hasMore,                                    // hasNextPage
        after != null,                              // hasPreviousPage
        edges.isEmpty() ? null : edges.get(0).cursor(),
        edges.isEmpty() ? null : edges.get(edges.size() - 1).cursor()
    );

    int totalCount = productRepository.countProducts(filter);

    return new ProductConnection(edges, pageInfo, totalCount);
}
```
</details>
