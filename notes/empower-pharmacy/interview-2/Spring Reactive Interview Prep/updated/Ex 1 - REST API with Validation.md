# Exercise 1: REST API with Validation & Error Handling

## Objective
Build a Spring Boot REST API for a task management system with proper validation and structured error responses.

---

## Task 1: Define the Entity and DTOs

```java
// TODO: Create a Task entity
// Fields: id (Long), title (String), description (String),
//         status (enum: TODO, IN_PROGRESS, DONE),
//         priority (enum: LOW, MEDIUM, HIGH),
//         dueDate (LocalDate), createdAt, updatedAt

// TODO: Create request DTOs with validation
// CreateTaskRequest:
//   - title: @NotBlank, @Size(max = 200)
//   - description: optional, @Size(max = 2000)
//   - priority: @NotNull
//   - dueDate: @Future

// UpdateTaskRequest:
//   - title: @Size(max = 200) (optional, update if present)
//   - status: valid enum value
//   - priority: valid enum value
```

---

## Task 2: Implement the Controller

```java
@RestController
@RequestMapping("/api/tasks")
public class TaskController {

    // TODO: Implement these endpoints

    // GET /api/tasks?status=TODO&priority=HIGH&page=0&size=20
    //   → Page<TaskResponse>

    // GET /api/tasks/{id}
    //   → TaskResponse (or 404)

    // POST /api/tasks
    //   → TaskResponse (201 Created)

    // PUT /api/tasks/{id}
    //   → TaskResponse (or 404)

    // DELETE /api/tasks/{id}
    //   → 204 No Content (or 404)

    // PATCH /api/tasks/{id}/status
    //   → Update only the status field
}
```

---

## Task 3: Global Exception Handler with ProblemDetail

```java
// TODO: Create @RestControllerAdvice that handles:
// - ResourceNotFoundException → 404 with resource type + id
// - MethodArgumentNotValidException → 400 with per-field errors
// - DataIntegrityViolationException → 409 Conflict
// - All others → 500 Internal Error (no stack trace)

// Use ProblemDetail (RFC 7807) format for all responses
```

---

## Task 4: Write Integration Tests

```java
@SpringBootTest
@AutoConfigureMockMvc
class TaskControllerTest {

    @Autowired MockMvc mockMvc;

    // TODO: Test each scenario
    // 1. Create valid task → 201
    // 2. Create with blank title → 400 with fieldErrors
    // 3. Create with past dueDate → 400
    // 4. Get existing task → 200
    // 5. Get nonexistent → 404 with ProblemDetail
    // 6. Update status → 200
    // 7. Delete → 204, then GET → 404
    // 8. List with filters → 200, verify filtering
}
```

---

## Acceptance Criteria

- [ ] All CRUD operations work correctly
- [ ] Validation errors return 400 with per-field error messages
- [ ] Missing resources return 404 ProblemDetail
- [ ] No stack traces in any error response
- [ ] Pagination and filtering work on list endpoint
- [ ] All 8 test scenarios pass

---

## Complete Solution

### Enums

```java
public enum TaskStatus { TODO, IN_PROGRESS, DONE }
public enum Priority { LOW, MEDIUM, HIGH }
```

### Entity

```java
@Entity
@Table(name = "tasks")
public class Task {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false, length = 200)
    private String title;

    @Column(length = 2000)
    private String description;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private TaskStatus status = TaskStatus.TODO;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private Priority priority;

    private LocalDate dueDate;

    @CreatedDate
    private LocalDateTime createdAt;

    @LastModifiedDate
    private LocalDateTime updatedAt;

    // constructors, getters, setters
}
```

### DTOs

```java
public record CreateTaskRequest(
    @NotBlank(message = "Title is required")
    @Size(max = 200, message = "Title must be at most 200 characters")
    String title,

    @Size(max = 2000, message = "Description must be at most 2000 characters")
    String description,

    @NotNull(message = "Priority is required")
    Priority priority,

    @Future(message = "Due date must be in the future")
    LocalDate dueDate
) {}

public record UpdateTaskRequest(
    @Size(max = 200) String title,
    TaskStatus status,
    Priority priority,
    LocalDate dueDate
) {}

public record UpdateStatusRequest(
    @NotNull(message = "Status is required")
    TaskStatus status
) {}

public record TaskResponse(
    Long id,
    String title,
    String description,
    TaskStatus status,
    Priority priority,
    LocalDate dueDate,
    LocalDateTime createdAt,
    LocalDateTime updatedAt
) {
    public static TaskResponse from(Task task) {
        return new TaskResponse(
            task.getId(), task.getTitle(), task.getDescription(),
            task.getStatus(), task.getPriority(), task.getDueDate(),
            task.getCreatedAt(), task.getUpdatedAt()
        );
    }
}
```

### Repository

```java
public interface TaskRepository extends JpaRepository<Task, Long> {

    Page<Task> findByStatus(TaskStatus status, Pageable pageable);
    Page<Task> findByPriority(Priority priority, Pageable pageable);
    Page<Task> findByStatusAndPriority(TaskStatus status, Priority priority, Pageable pageable);
}
```

### Service

```java
@Service
@Transactional
public class TaskService {

    private final TaskRepository taskRepo;

    public Task findById(Long id) {
        return taskRepo.findById(id)
            .orElseThrow(() -> new ResourceNotFoundException("Task", id));
    }

    public Page<Task> findAll(TaskStatus status, Priority priority, Pageable pageable) {
        if (status != null && priority != null) {
            return taskRepo.findByStatusAndPriority(status, priority, pageable);
        }
        if (status != null) return taskRepo.findByStatus(status, pageable);
        if (priority != null) return taskRepo.findByPriority(priority, pageable);
        return taskRepo.findAll(pageable);
    }

    public Task create(CreateTaskRequest request) {
        Task task = new Task();
        task.setTitle(request.title());
        task.setDescription(request.description());
        task.setPriority(request.priority());
        task.setDueDate(request.dueDate());
        task.setStatus(TaskStatus.TODO);
        return taskRepo.save(task);
    }

    public Task update(Long id, UpdateTaskRequest request) {
        Task task = findById(id);
        if (request.title() != null) task.setTitle(request.title());
        if (request.status() != null) task.setStatus(request.status());
        if (request.priority() != null) task.setPriority(request.priority());
        if (request.dueDate() != null) task.setDueDate(request.dueDate());
        return taskRepo.save(task);
    }

    public Task updateStatus(Long id, TaskStatus status) {
        Task task = findById(id);
        task.setStatus(status);
        return taskRepo.save(task);
    }

    public void delete(Long id) {
        Task task = findById(id);
        taskRepo.delete(task);
    }
}
```

### Controller

```java
@RestController
@RequestMapping("/api/tasks")
public class TaskController {

    private final TaskService taskService;

    @GetMapping
    public Page<TaskResponse> listTasks(
            @RequestParam(required = false) TaskStatus status,
            @RequestParam(required = false) Priority priority,
            @RequestParam(defaultValue = "0") int page,
            @RequestParam(defaultValue = "20") int size) {
        return taskService.findAll(status, priority, PageRequest.of(page, size))
            .map(TaskResponse::from);
    }

    @GetMapping("/{id}")
    public TaskResponse getTask(@PathVariable Long id) {
        return TaskResponse.from(taskService.findById(id));
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public TaskResponse createTask(@Valid @RequestBody CreateTaskRequest request) {
        return TaskResponse.from(taskService.create(request));
    }

    @PutMapping("/{id}")
    public TaskResponse updateTask(@PathVariable Long id,
                                   @Valid @RequestBody UpdateTaskRequest request) {
        return TaskResponse.from(taskService.update(id, request));
    }

    @DeleteMapping("/{id}")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void deleteTask(@PathVariable Long id) {
        taskService.delete(id);
    }

    @PatchMapping("/{id}/status")
    public TaskResponse updateStatus(@PathVariable Long id,
                                     @Valid @RequestBody UpdateStatusRequest request) {
        return TaskResponse.from(taskService.updateStatus(id, request.status()));
    }
}
```

### Exception Handler

```java
@RestControllerAdvice
@Slf4j
public class GlobalExceptionHandler {

    @ExceptionHandler(ResourceNotFoundException.class)
    @ResponseStatus(HttpStatus.NOT_FOUND)
    public ProblemDetail handleNotFound(ResourceNotFoundException ex) {
        ProblemDetail pd = ProblemDetail.forStatus(HttpStatus.NOT_FOUND);
        pd.setTitle("Resource Not Found");
        pd.setDetail(ex.getMessage());
        pd.setProperty("resourceType", ex.getResourceType());
        pd.setProperty("resourceId", ex.getResourceId());
        return pd;
    }

    @ExceptionHandler(MethodArgumentNotValidException.class)
    @ResponseStatus(HttpStatus.BAD_REQUEST)
    public ProblemDetail handleValidation(MethodArgumentNotValidException ex) {
        ProblemDetail pd = ProblemDetail.forStatus(HttpStatus.BAD_REQUEST);
        pd.setTitle("Validation Failed");
        Map<String, String> fieldErrors = ex.getBindingResult().getFieldErrors().stream()
            .collect(Collectors.toMap(
                FieldError::getField,
                fe -> fe.getDefaultMessage() != null ? fe.getDefaultMessage() : "invalid",
                (a, b) -> a
            ));
        pd.setProperty("fieldErrors", fieldErrors);
        return pd;
    }

    @ExceptionHandler(DataIntegrityViolationException.class)
    @ResponseStatus(HttpStatus.CONFLICT)
    public ProblemDetail handleConflict(DataIntegrityViolationException ex) {
        ProblemDetail pd = ProblemDetail.forStatus(HttpStatus.CONFLICT);
        pd.setTitle("Data Conflict");
        pd.setDetail("A conflicting record already exists");
        return pd;
    }

    @ExceptionHandler(Exception.class)
    @ResponseStatus(HttpStatus.INTERNAL_SERVER_ERROR)
    public ProblemDetail handleAll(Exception ex) {
        log.error("Unhandled exception", ex);
        ProblemDetail pd = ProblemDetail.forStatus(HttpStatus.INTERNAL_SERVER_ERROR);
        pd.setTitle("Internal Server Error");
        return pd;
    }
}

public class ResourceNotFoundException extends RuntimeException {
    private final String resourceType;
    private final Object resourceId;

    public ResourceNotFoundException(String type, Object id) {
        super(type + " not found: " + id);
        this.resourceType = type;
        this.resourceId = id;
    }

    public String getResourceType() { return resourceType; }
    public Object getResourceId() { return resourceId; }
}
```

### Integration Tests

```java
@SpringBootTest
@AutoConfigureMockMvc
@Transactional
class TaskControllerTest {

    @Autowired MockMvc mockMvc;
    @Autowired ObjectMapper objectMapper;
    @Autowired TaskRepository taskRepo;

    private Task savedTask;

    @BeforeEach
    void setup() {
        Task task = new Task();
        task.setTitle("Test Task");
        task.setDescription("Description");
        task.setPriority(Priority.MEDIUM);
        task.setStatus(TaskStatus.TODO);
        task.setDueDate(LocalDate.now().plusDays(7));
        savedTask = taskRepo.save(task);
    }

    @Test
    void createValidTask_returns201() throws Exception {
        var request = new CreateTaskRequest("New Task", "Desc", Priority.HIGH,
            LocalDate.now().plusDays(5));

        mockMvc.perform(post("/api/tasks")
                .contentType(MediaType.APPLICATION_JSON)
                .content(objectMapper.writeValueAsString(request)))
            .andExpect(status().isCreated())
            .andExpect(jsonPath("$.title").value("New Task"))
            .andExpect(jsonPath("$.priority").value("HIGH"))
            .andExpect(jsonPath("$.status").value("TODO"))
            .andExpect(jsonPath("$.id").isNumber());
    }

    @Test
    void createWithBlankTitle_returns400WithFieldErrors() throws Exception {
        var request = Map.of("title", "", "priority", "HIGH");

        mockMvc.perform(post("/api/tasks")
                .contentType(MediaType.APPLICATION_JSON)
                .content(objectMapper.writeValueAsString(request)))
            .andExpect(status().isBadRequest())
            .andExpect(jsonPath("$.title").value("Validation Failed"))
            .andExpect(jsonPath("$.fieldErrors.title").exists());
    }

    @Test
    void createWithPastDueDate_returns400() throws Exception {
        var request = Map.of("title", "Task", "priority", "LOW",
            "dueDate", LocalDate.now().minusDays(1).toString());

        mockMvc.perform(post("/api/tasks")
                .contentType(MediaType.APPLICATION_JSON)
                .content(objectMapper.writeValueAsString(request)))
            .andExpect(status().isBadRequest())
            .andExpect(jsonPath("$.fieldErrors.dueDate").exists());
    }

    @Test
    void getExistingTask_returns200() throws Exception {
        mockMvc.perform(get("/api/tasks/{id}", savedTask.getId()))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$.title").value("Test Task"));
    }

    @Test
    void getNonexistent_returns404ProblemDetail() throws Exception {
        mockMvc.perform(get("/api/tasks/{id}", 99999))
            .andExpect(status().isNotFound())
            .andExpect(jsonPath("$.title").value("Resource Not Found"))
            .andExpect(jsonPath("$.resourceType").value("Task"));
    }

    @Test
    void updateStatus_returns200() throws Exception {
        var request = Map.of("status", "IN_PROGRESS");

        mockMvc.perform(patch("/api/tasks/{id}/status", savedTask.getId())
                .contentType(MediaType.APPLICATION_JSON)
                .content(objectMapper.writeValueAsString(request)))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$.status").value("IN_PROGRESS"));
    }

    @Test
    void deleteTask_returns204_thenGetReturns404() throws Exception {
        mockMvc.perform(delete("/api/tasks/{id}", savedTask.getId()))
            .andExpect(status().isNoContent());

        mockMvc.perform(get("/api/tasks/{id}", savedTask.getId()))
            .andExpect(status().isNotFound());
    }

    @Test
    void listWithFilters_returnsFilteredResults() throws Exception {
        mockMvc.perform(get("/api/tasks")
                .param("status", "TODO")
                .param("priority", "MEDIUM")
                .param("page", "0")
                .param("size", "10"))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$.content").isArray())
            .andExpect(jsonPath("$.content[0].status").value("TODO"));
    }
}
```
