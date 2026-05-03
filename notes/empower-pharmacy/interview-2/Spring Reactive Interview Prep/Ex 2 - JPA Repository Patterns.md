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
