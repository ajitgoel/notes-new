# Exercise 2: Spring Data JPA Repository Patterns

## Setup

```java
// Given these entities:
@Entity
public class Author {
    @Id @GeneratedValue private Long id;
    private String name;
    private String country;
    @OneToMany(mappedBy = "author", cascade = CascadeType.ALL)
    private List<Book> books;
}

@Entity
public class Book {
    @Id @GeneratedValue private Long id;
    private String title;
    private String isbn;
    private BigDecimal price;
    @Enumerated(EnumType.STRING) private Genre genre;
    private LocalDate publishedDate;
    @ManyToOne(fetch = FetchType.LAZY)
    private Author author;
    @ManyToMany
    private Set<Tag> tags;
}

@Entity
public class Tag {
    @Id @GeneratedValue private Long id;
    private String name;
}
```

---

## Task 1: Derived Query Methods

```java
public interface BookRepository extends JpaRepository<Book, Long> {

    // TODO: Write derived query methods for:
    // 1. Find books by genre
    // 2. Find books with price between min and max
    // 3. Find books by author name (traversing relationship)
    // 4. Find books published after a date, ordered by price descending
    // 5. Count books by genre
    // 6. Check if a book exists by ISBN
    // 7. Find books whose title contains a keyword (case-insensitive)
    // 8. Find top 5 most expensive books
}
```

---

## Task 2: JPQL and Native Queries

```java
// TODO: Write @Query methods for:
// 1. Find all authors who have published more than N books
// 2. Calculate average book price per genre (return DTO projection)
// 3. Find books that have ALL of the given tag names
// 4. Bulk update: set price = price * multiplier for a given genre
// 5. Find authors whose books total revenue exceeds a threshold
```

---

## Task 3: Fix the N+1 Problem

```java
// This code has an N+1 problem. Fix it with 3 different approaches.
@Service
public class BookService {

    public List<BookWithAuthor> getAllBooksWithAuthors() {
        List<Book> books = bookRepo.findAll(); // 1 query
        return books.stream()
            .map(b -> new BookWithAuthor(
                b.getTitle(),
                b.getAuthor().getName() // N queries!
            ))
            .toList();
    }
}

// Approach 1: JOIN FETCH in @Query
// Approach 2: @EntityGraph on repository method
// Approach 3: Batch fetch size in config
```

---

## Task 4: Projections

```java
// TODO: Implement three types of projections

// 1. Interface projection
public interface BookSummary {
    String getTitle();
    BigDecimal getPrice();
    String getAuthorName(); // derived from author.name
}

// 2. Class-based (DTO) projection
public record GenreStats(
    Genre genre,
    long count,
    BigDecimal avgPrice
) {}

// 3. Dynamic projection
// <T> List<T> findByGenre(Genre genre, Class<T> type);
```

---

## Task 5: Specification for Dynamic Queries

```java
// TODO: Implement Specifications for dynamic filtering
// Users can filter by any combination of: genre, minPrice, maxPrice,
// authorCountry, publishedAfter, tag names

public class BookSpecifications {
    public static Specification<Book> withGenre(Genre genre) {
        // return (root, query, cb) -> ...
    }
    public static Specification<Book> priceBetween(BigDecimal min, BigDecimal max) {
        // ...
    }
    // ... more specs
}

// Usage:
// bookRepo.findAll(
//     where(withGenre(FICTION)).and(priceBetween(10, 50))
// );
```

---

## Acceptance Criteria

- [ ] All 8 derived queries return correct results
- [ ] JPQL queries handle joins and aggregations correctly
- [ ] N+1 is eliminated (verified by query logging)
- [ ] Three projection types work correctly
- [ ] Specifications compose dynamically for any filter combination

---

## Complete Solution

### Task 1: Derived Query Methods

```java
public interface BookRepository extends JpaRepository<Book, Long>,
        JpaSpecificationExecutor<Book> {

    // 1. Find books by genre
    List<Book> findByGenre(Genre genre);

    // 2. Find books with price between min and max
    List<Book> findByPriceBetween(BigDecimal min, BigDecimal max);

    // 3. Find books by author name (traversing relationship)
    List<Book> findByAuthorName(String authorName);

    // 4. Find books published after a date, ordered by price descending
    List<Book> findByPublishedDateAfterOrderByPriceDesc(LocalDate date);

    // 5. Count books by genre
    long countByGenre(Genre genre);

    // 6. Check if a book exists by ISBN
    boolean existsByIsbn(String isbn);

    // 7. Find books whose title contains a keyword (case-insensitive)
    List<Book> findByTitleContainingIgnoreCase(String keyword);

    // 8. Find top 5 most expensive books
    List<Book> findTop5ByOrderByPriceDesc();
}
```

### Task 2: JPQL and Native Queries

```java
public interface AuthorRepository extends JpaRepository<Author, Long> {

    // 1. Find all authors who have published more than N books
    @Query("SELECT a FROM Author a WHERE SIZE(a.books) > :minBooks")
    List<Author> findProlificAuthors(@Param("minBooks") int minBooks);

    // 5. Find authors whose books total revenue exceeds a threshold
    @Query("""
        SELECT a FROM Author a JOIN a.books b
        GROUP BY a
        HAVING SUM(b.price) > :threshold
        """)
    List<Author> findHighRevenueAuthors(@Param("threshold") BigDecimal threshold);
}

public interface BookRepository extends JpaRepository<Book, Long> {

    // 2. Calculate average book price per genre (return DTO projection)
    @Query("""
        SELECT new com.example.dto.GenreStats(b.genre, COUNT(b), AVG(b.price))
        FROM Book b
        GROUP BY b.genre
        """)
    List<GenreStats> findGenreStatistics();

    // 3. Find books that have ALL of the given tag names
    @Query("""
        SELECT b FROM Book b JOIN b.tags t
        WHERE t.name IN :tagNames
        GROUP BY b
        HAVING COUNT(DISTINCT t.name) = :tagCount
        """)
    List<Book> findByAllTags(@Param("tagNames") Set<String> tagNames,
                             @Param("tagCount") long tagCount);

    // 4. Bulk update: set price = price * multiplier for a given genre
    @Modifying
    @Query("UPDATE Book b SET b.price = b.price * :multiplier WHERE b.genre = :genre")
    int updatePriceByGenre(@Param("genre") Genre genre,
                           @Param("multiplier") BigDecimal multiplier);
}
```

### Task 3: Fix N+1 Problem — Three Approaches

```java
// Approach 1: JOIN FETCH in @Query
@Query("SELECT b FROM Book b JOIN FETCH b.author")
List<Book> findAllWithAuthor();

// Approach 2: @EntityGraph on repository method
@EntityGraph(attributePaths = {"author"})
@Override
List<Book> findAll();

// Approach 3: Batch fetch size in application.yml
// spring:
//   jpa:
//     properties:
//       hibernate:
//         default_batch_fetch_size: 25
// This makes Hibernate load authors in batches:
// SELECT * FROM author WHERE id IN (?, ?, ..., ?) -- up to 25 at a time
// Instead of N individual queries, you get ceil(N/25) queries

// Updated service using Approach 1:
@Service
public class BookService {

    public List<BookWithAuthor> getAllBooksWithAuthors() {
        return bookRepo.findAllWithAuthor().stream()
            .map(b -> new BookWithAuthor(b.getTitle(), b.getAuthor().getName()))
            .toList();
        // Now: 1 query with JOIN instead of 1 + N
    }
}
```

### Task 4: Projections

```java
// 1. Interface projection
public interface BookSummary {
    String getTitle();
    BigDecimal getPrice();

    @Value("#{target.author.name}")  // SpEL expression
    String getAuthorName();
}

// In repository:
List<BookSummary> findByGenre(Genre genre);
// Spring generates: SELECT b.title, b.price, a.name FROM book b JOIN author a ...

// 2. Class-based (DTO) projection
public record GenreStats(Genre genre, long count, BigDecimal avgPrice) {}

// In repository (using JPQL constructor expression):
@Query("""
    SELECT new com.example.dto.GenreStats(b.genre, COUNT(b), AVG(b.price))
    FROM Book b GROUP BY b.genre
    """)
List<GenreStats> findGenreStatistics();

// 3. Dynamic projection
<T> List<T> findByGenre(Genre genre, Class<T> type);

// Usage:
List<BookSummary> summaries = bookRepo.findByGenre(Genre.FICTION, BookSummary.class);
List<Book> fullEntities = bookRepo.findByGenre(Genre.FICTION, Book.class);
```

### Task 5: Specifications

```java
public class BookSpecifications {

    public static Specification<Book> withGenre(Genre genre) {
        return (root, query, cb) ->
            genre == null ? null : cb.equal(root.get("genre"), genre);
    }

    public static Specification<Book> priceBetween(BigDecimal min, BigDecimal max) {
        return (root, query, cb) -> {
            if (min == null && max == null) return null;
            if (min != null && max != null)
                return cb.between(root.get("price"), min, max);
            if (min != null) return cb.greaterThanOrEqualTo(root.get("price"), min);
            return cb.lessThanOrEqualTo(root.get("price"), max);
        };
    }

    public static Specification<Book> authorFromCountry(String country) {
        return (root, query, cb) ->
            country == null ? null :
            cb.equal(root.join("author").get("country"), country);
    }

    public static Specification<Book> publishedAfter(LocalDate date) {
        return (root, query, cb) ->
            date == null ? null :
            cb.greaterThan(root.get("publishedDate"), date);
    }

    public static Specification<Book> hasTag(String tagName) {
        return (root, query, cb) ->
            tagName == null ? null :
            cb.equal(root.join("tags").get("name"), tagName);
    }
}

// Usage in service:
@Service
public class BookSearchService {

    private final BookRepository bookRepo;

    public List<Book> search(Genre genre, BigDecimal minPrice,
                             BigDecimal maxPrice, String country,
                             LocalDate publishedAfter) {
        Specification<Book> spec = Specification
            .where(BookSpecifications.withGenre(genre))
            .and(BookSpecifications.priceBetween(minPrice, maxPrice))
            .and(BookSpecifications.authorFromCountry(country))
            .and(BookSpecifications.publishedAfter(publishedAfter));

        return bookRepo.findAll(spec);
    }
}
```
