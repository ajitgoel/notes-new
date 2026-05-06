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
public interface BookRepository extends JpaRepository<Book, Long>, JpaSpecificationExecutor<Book> {

    // 1. Find books by genre
    List<Book> findByGenre(Genre genre);

    // 2. Find books with price between min and max
    List<Book> findByPriceBetween(BigDecimal min, BigDecimal max);

    // 3. Find books by author name (traversing relationship)
    List<Book> findByAuthorName(String name);

    // 4. Find books published after a date, ordered by price descending
    List<Book> findByPublishedAfterOrderByPriceDesc(LocalDate date);

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

---

## Task 2: JPQL and Native Queries

```java
public interface AuthorRepository extends JpaRepository<Author, Long> {
    // 1. Find all authors who have published more than N books
    @Query("SELECT a FROM Author a WHERE SIZE(a.books) > :n")
    List<Author> findAuthorsWithMoreThanNBooks(@Param("n") int n);

    // 2. Calculate average book price per genre (return DTO projection)
    @Query("SELECT new com.example.GenreStats(b.genre, COUNT(b), AVG(b.price)) FROM Book b GROUP BY b.genre")
    List<GenreStats> getGenreStats();

    // 3. Find books that have ALL of the given tag names
    @Query("SELECT b FROM Book b JOIN b.tags t WHERE t.name IN :tagNames GROUP BY b HAVING COUNT(DISTINCT t.name) = :tagCount")
    List<Book> findBooksWithAllTags(@Param("tagNames") Collection<String> tagNames, @Param("tagCount") long tagCount);

    // 4. Bulk update: set price = price * multiplier for a given genre
    @Modifying
    @Query("UPDATE Book b SET b.price = b.price * :multiplier WHERE b.genre = :genre")
    int updatePriceByGenre(@Param("genre") Genre genre, @Param("multiplier") BigDecimal multiplier);

    // 5. Find authors whose books total revenue exceeds a threshold
    @Query("SELECT a FROM Author a JOIN a.books b GROUP BY a HAVING SUM(b.price) > :threshold")
    List<Author> findProlificAuthors(@Param("threshold") BigDecimal threshold);
}
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
@Query("SELECT b FROM Book b JOIN FETCH b.author")
List<Book> findAllWithAuthor();

// Approach 2: @EntityGraph on repository method
@EntityGraph(attributePaths = {"author"})
List<Book> findAll();

// Approach 3: Batch fetch size in config (or on entity)
// application.yml: spring.jpa.properties.hibernate.default_batch_fetch_size: 10
// OR on Author entity: @BatchSize(size = 10)
@Entity
public class Author { ... }
```

---

## Task 4: Projections

```java
// TODO: Implement three types of projections

// 1. Interface projection
public interface BookSummary {
    String getTitle();
    BigDecimal getPrice();
    @Value("#{target.author.name}") // SpEL for traversing relationships
    String getAuthorName();
}

// 2. Class-based (DTO) projection
public record GenreStats(
    Genre genre,
    long count,
    Double avgPrice // AVG in JPQL returns Double
) {}

// 3. Dynamic projection
<T> List<T> findByGenre(Genre genre, Class<T> type);
```

---

## Task 5: Specification for Dynamic Queries

```java
// TODO: Implement Specifications for dynamic filtering
// Users can filter by any combination of: genre, minPrice, maxPrice,
// authorCountry, publishedAfter, tag names

public class BookSpecifications {
    public static Specification<Book> withGenre(Genre genre) {
        return (root, query, cb) -> genre == null ? null : cb.equal(root.get("genre"), genre);
    }

    public static Specification<Book> priceBetween(BigDecimal min, BigDecimal max) {
        return (root, query, cb) -> {
            if (min == null && max == null) return null;
            if (min == null) return cb.lessThanOrEqualTo(root.get("price"), max);
            if (max == null) return cb.greaterThanOrEqualTo(root.get("price"), min);
            return cb.between(root.get("price"), min, max);
        };
    }

    public static Specification<Book> authorCountry(String country) {
        return (root, query, cb) -> {
            if (country == null) return null;
            return cb.equal(root.join("author").get("country"), country);
        };
    }

    public static Specification<Book> hasTags(Collection<String> tagNames) {
        return (root, query, cb) -> {
            if (tagNames == null || tagNames.isEmpty()) return null;
            return root.join("tags").get("name").in(tagNames);
        };
    }
}

// Usage:
// bookRepo.findAll(
//     where(withGenre(FICTION))
//         .and(priceBetween(min, max))
//         .and(authorCountry("UK"))
// );
```

---

## Acceptance Criteria

- [x] All 8 derived queries return correct results
- [x] JPQL queries handle joins and aggregations correctly
- [x] N+1 is eliminated (verified by query logging)
- [x] Three projection types work correctly
- [x] Specifications compose dynamically for any filter combination
