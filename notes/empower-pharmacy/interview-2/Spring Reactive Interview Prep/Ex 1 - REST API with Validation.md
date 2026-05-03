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
