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

## Complete Solution

### Schema

```graphql
type Query {
  products(
    first: Int
    after: String
    last: Int
    before: String
    filter: ProductFilter
  ): ProductConnection!
}

type ProductConnection {
  edges: [ProductEdge!]!
  pageInfo: PageInfo!
  totalCount: Int!
}

type ProductEdge {
  node: Product!
  cursor: String!
}

type PageInfo {
  hasNextPage: Boolean!
  hasPreviousPage: Boolean!
  startCursor: String
  endCursor: String
}

input ProductFilter {
  category: Category
  minPrice: Float
  maxPrice: Float
  inStock: Boolean
  searchTerm: String
}
```

### Connection Records

```java
public record ProductConnection(
    List<ProductEdge> edges,
    PageInfo pageInfo,
    int totalCount
) {}

public record ProductEdge(Product node, String cursor) {}

public record PageInfo(
    boolean hasNextPage,
    boolean hasPreviousPage,
    String startCursor,
    String endCursor
) {}

public record ProductFilter(
    Category category,
    BigDecimal minPrice,
    BigDecimal maxPrice,
    Boolean inStock,
    String searchTerm
) {}
```

### Cursor Utilities

```java
public class CursorUtils {

    public static String encode(Product product) {
        String raw = "price:" + product.getPrice() + ":id:" + product.getId();
        return Base64.getEncoder().encodeToString(raw.getBytes(StandardCharsets.UTF_8));
    }

    public static CursorPosition decode(String cursor) {
        String raw = new String(Base64.getDecoder().decode(cursor), StandardCharsets.UTF_8);
        String[] parts = raw.split(":");
        return new CursorPosition(
            new BigDecimal(parts[1]),  // price
            parts[3]                   // id
        );
    }

    public record CursorPosition(BigDecimal price, String id) {}
}
```

### Controller

```java
@Controller
public class ProductController {

    private final ProductRepository productRepository;

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
            hasMore,
            after != null,
            edges.isEmpty() ? null : edges.get(0).cursor(),
            edges.isEmpty() ? null : edges.get(edges.size() - 1).cursor()
        );

        int totalCount = productRepository.countProducts(filter);

        return new ProductConnection(edges, pageInfo, totalCount);
    }
}
```

### Repository (JDBC Implementation)

```java
@Repository
public class ProductRepositoryImpl implements ProductRepository {

    private final JdbcTemplate jdbc;

    public List<Product> findProducts(String afterCursor, String beforeCursor,
                                       int limit, ProductFilter filter) {
        StringBuilder sql = new StringBuilder("SELECT * FROM products WHERE 1=1");
        List<Object> params = new ArrayList<>();

        // Apply cursor position
        if (afterCursor != null) {
            CursorUtils.CursorPosition pos = CursorUtils.decode(afterCursor);
            sql.append(" AND (price, id) > (?, ?)");
            params.add(pos.price());
            params.add(pos.id());
        }

        // Apply filters
        if (filter != null) {
            if (filter.category() != null) {
                sql.append(" AND category = ?");
                params.add(filter.category().name());
            }
            if (filter.minPrice() != null) {
                sql.append(" AND price >= ?");
                params.add(filter.minPrice());
            }
            if (filter.maxPrice() != null) {
                sql.append(" AND price <= ?");
                params.add(filter.maxPrice());
            }
            if (filter.inStock() != null) {
                sql.append(" AND in_stock = ?");
                params.add(filter.inStock());
            }
            if (filter.searchTerm() != null) {
                sql.append(" AND LOWER(name) LIKE LOWER(?)");
                params.add("%" + filter.searchTerm() + "%");
            }
        }

        sql.append(" ORDER BY price ASC, id ASC LIMIT ?");
        params.add(limit);

        return jdbc.query(sql.toString(), productRowMapper, params.toArray());
    }

    public int countProducts(ProductFilter filter) {
        StringBuilder sql = new StringBuilder("SELECT COUNT(*) FROM products WHERE 1=1");
        List<Object> params = new ArrayList<>();
        // Apply same filters as above (extract to shared method)
        return jdbc.queryForObject(sql.toString(), Integer.class, params.toArray());
    }
}
```

### Tests

```java
@Test
void firstPage() {
    tester.document("""
        query {
          products(first: 5) {
            edges { node { name } cursor }
            pageInfo { hasNextPage endCursor }
            totalCount
          }
        }
        """)
        .execute()
        .path("products.edges").entityList(Object.class).hasSize(5)
        .path("products.pageInfo.hasNextPage").entity(Boolean.class).isEqualTo(true)
        .path("products.pageInfo.endCursor").hasValue();
}

@Test
void secondPage() {
    // Get first page
    String endCursor = tester.document("""
        query { products(first: 5) { pageInfo { endCursor } } }
        """)
        .execute()
        .path("products.pageInfo.endCursor")
        .entity(String.class).get();

    // Get second page using the cursor
    tester.document("""
        query($after: String) {
          products(first: 5, after: $after) {
            edges { node { name } }
            pageInfo { hasNextPage hasPreviousPage }
          }
        }
        """)
        .variable("after", endCursor)
        .execute()
        .path("products.pageInfo.hasPreviousPage").entity(Boolean.class).isEqualTo(true);
}
```
