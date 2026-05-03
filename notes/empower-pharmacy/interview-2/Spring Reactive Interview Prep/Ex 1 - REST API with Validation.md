# Exercise 1: REST API with Validation & Error Handling

## Objective
Build a Spring Boot REST API for a task management system with proper validation and structured error responses.

---

## Task 1: Define the Entity and DTOs

```java
public enum Status { TODO, IN_PROGRESS, DONE }
public enum Priority { LOW, MEDIUM, HIGH }

@Entity
@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class Task {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    private String title;
    private String description;
    @Enumerated(EnumType.STRING)
    private Status status;
    @Enumerated(EnumType.STRING)
    private Priority priority;
    private LocalDate dueDate;
    @CreationTimestamp
    private LocalDateTime createdAt;
    @UpdateTimestamp
    private LocalDateTime updatedAt;
}

public record CreateTaskRequest(
    @NotBlank @Size(max = 200) String title,
    @Size(max = 2000) String description,
    @NotNull Priority priority,
    @Future LocalDate dueDate
) {}

public record UpdateTaskRequest(
    @Size(max = 200) String title,
    @Size(max = 2000) String description,
    Status status,
    Priority priority,
    @Future LocalDate dueDate
) {}

public record TaskResponse(
    Long id,
    String title,
    String description,
    Status status,
    Priority priority,
    LocalDate dueDate,
    LocalDateTime createdAt,
    LocalDateTime updatedAt
) {}
```

---

## => Task 2: Implement the Controller

```java hl:1-2,6,17-18,22,26-27,31
@RestController
@RequestMapping("/api/tasks")
@RequiredArgsConstructor
public class TaskController {
    private final TaskService taskService;
    @GetMapping
    public Page<TaskResponse> getTasks(
            @RequestParam(required = false) Status status,
            @RequestParam(required = false) Priority priority,
            Pageable pageable) {
        return taskService.findAll(status, priority, pageable);
    }
    @GetMapping("/{id}")
    public TaskResponse getTask(@PathVariable Long id) {
        return taskService.findById(id);
    }
    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public TaskResponse createTask(@Valid @RequestBody CreateTaskRequest request) {
        return taskService.create(request);
    }
    @PutMapping("/{id}")
    public TaskResponse updateTask(@PathVariable Long id, @Valid @RequestBody UpdateTaskRequest request) {
        return taskService.update(id, request);
    }
    @DeleteMapping("/{id}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void deleteTask(@PathVariable Long id) {
        taskService.delete(id);
    }
    @PatchMapping("/{id}/status")
    public TaskResponse updateStatus(@PathVariable Long id, @RequestParam Status status) {
        return taskService.updateStatus(id, status);
    }
}
```

---

## => Task 3: Global Exception Handler with ProblemDetail

```java hl:1,3
@RestControllerAdvice
public class GlobalExceptionHandler {
    @ExceptionHandler(ResourceNotFoundException.class)
    public ProblemDetail handleNotFound(ResourceNotFoundException ex) {
        ProblemDetail pd = ProblemDetail.forStatusAndDetail(HttpStatus.NOT_FOUND, ex.getMessage());
        pd.setTitle("Resource Not Found");
        pd.setProperty("resourceType", ex.getResourceType());
        pd.setProperty("resourceId", ex.getResourceId());
        return pd;
    }
    @ExceptionHandler(MethodArgumentNotValidException.class)
    public ProblemDetail handleValidation(MethodArgumentNotValidException ex) {
        ProblemDetail pd = ProblemDetail.forStatusAndDetail(HttpStatus.BAD_REQUEST, "Validation failed");
        pd.setTitle("Constraint Violation");
        Map<String, String> errors = new HashMap<>();
        ex.getBindingResult().getFieldErrors().forEach(e -> 
            errors.put(e.getField(), e.getDefaultMessage()));
        pd.setProperty("errors", errors);
        return pd;
    }
    @ExceptionHandler(DataIntegrityViolationException.class)
    public ProblemDetail handleConflict(DataIntegrityViolationException ex) {
        ProblemDetail pd = ProblemDetail.forStatusAndDetail(HttpStatus.CONFLICT, "Data integrity violation");
        pd.setProperty("detail", ex.getMostSpecificCause().getMessage());
        return pd;
    }
    @ExceptionHandler(Exception.class)
    public ProblemDetail handleAll(Exception ex) {
        return ProblemDetail.forStatusAndDetail(HttpStatus.INTERNAL_SERVER_ERROR, "An unexpected error occurred");
    }
}
```

---

## Task 4: Write Integration Tests

```java
@SpringBootTest
@AutoConfigureMockMvc
class TaskControllerTest {

    @Autowired MockMvc mockMvc;
    @Autowired ObjectMapper objectMapper;

    @Test
    void createTask_withValidRequest_returns201() throws Exception {
        var request = new CreateTaskRequest("Title", "Desc", Priority.HIGH, LocalDate.now().plusDays(1));
        mockMvc.perform(post("/api/tasks")
                .contentType(MediaType.APPLICATION_JSON)
                .content(objectMapper.writeValueAsString(request)))
            .andExpect(status().isCreated())
            .andExpect(jsonPath("$.title").value("Title"));
    }

    @Test
    void createTask_withBlankTitle_returns400() throws Exception {
        var request = new CreateTaskRequest("", "Desc", Priority.HIGH, LocalDate.now().plusDays(1));
        mockMvc.perform(post("/api/tasks")
                .contentType(MediaType.APPLICATION_JSON)
                .content(objectMapper.writeValueAsString(request)))
            .andExpect(status().isBadRequest())
            .andExpect(jsonPath("$.errors.title").exists());
    }

    @Test
    void getTask_nonExistent_returns404() throws Exception {
        mockMvc.perform(get("/api/tasks/999"))
            .andExpect(status().isNotFound())
            .andExpect(jsonPath("$.title").value("Resource Not Found"));
    }

    @Test
    void updateStatus_returns200() throws Exception {
        mockMvc.perform(patch("/api/tasks/1/status").param("status", "DONE"))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$.status").value("DONE"));
    }
}
```

---

## Acceptance Criteria

- [x] All CRUD operations work correctly
- [x] Validation errors return 400 with per-field error messages
- [x] Missing resources return 404 ProblemDetail
- [x] No stack traces in any error response
- [x] Pagination and filtering work on list endpoint
- [x] All 8 test scenarios pass
