**Use Specific Context Markers**

```
@filename.tsx @another-file.js 
Can you refactor the authentication logic to use the new pattern from another-file.js?
```

**Chain Context Across Conversations**

- Use "Continue this conversation" to maintain context
- Reference previous changes: "Building on the refactor from earlier..."
- Use breadcrumbs: "Given the User model we just created..."

**Provide Implementation Constraints**

```
// Cursor: Use React Query, TypeScript strict mode, and ensure accessibility
// Also maintain backward compatibility with existing API calls
```

## **Codebase Navigation & Context**

**Strategic `@` Usage**

- `@web` for searching online documentation
- `@docs` for your project's documentation
- `@codebase` for broad context (use sparingly due to token limits)
- `@folder/subfolder` for targeted context

**Context Window Management**

- Pin important files in the chat for persistent context
- Use "Add to context" selectively rather than dumping entire codebase
- Create focused contexts for different features/modules

**Smart File Referencing**

```
Looking at @components/UserProfile.tsx and @hooks/useAuth.ts, 
how can we optimize the re-rendering when user data changes?
```

## **Advanced Code Generation**

**Multi-Step Instructions**

```
1. First, create the TypeScript interfaces for the API response
2. Then generate the React Query hook
3. Finally, update the component to use the new hook
4. Add error boundary handling throughout
```

**Template-Based Requests**

```
Generate a new feature following this pattern:
- Hook in @hooks/useFeature.ts (follow useAuth pattern)
- Component in @components/Feature/ (follow UserProfile structure)  
- Types in @types/feature.ts
- Tests following @components/UserProfile/UserProfile.test.tsx pattern
```

**Code Style Enforcement**

```
// Cursor: Follow these patterns:
// - Use const assertions for readonly arrays
// - Prefer type over interface for simple types
// - Always destructure props in components
// - Use explicit return types for functions
```

## **Workflow Optimization**

**Custom Cursor Rules** Create a `.cursorrules` file in your project root:

```
You are an expert in React, TypeScript, and Next.js.

Code Style:
- Use functional components with hooks
- Prefer const assertions
- Use explicit return types
- Follow the existing patterns in @components/

Architecture:
- Keep components under 100 lines
- Extract custom hooks for complex logic
- Use composition over inheritance
- Follow the existing folder structure

Testing:
- Write tests for all new components
- Follow the existing test patterns
- Mock external dependencies
- Use descriptive test names
```

**Keyboard Shortcuts Mastery**

- `Cmd+K` → Quick chat without losing focus
- `Cmd+L` → Clear chat and start fresh context
- `Cmd+I` → Inline editing mode
- `Cmd+Shift+L` → Apply AI suggestion across multiple files

## **Advanced Debugging & Refactoring**

**Systematic Debugging**

```
I'm getting this error: [paste error]
Context: @problematic-file.ts @related-hook.ts
Please:
1. Identify the root cause
2. Show the fix with explanation
3. Suggest preventive measures
4. Update related types if needed
```

**Large-Scale Refactoring**

```
I need to migrate from useState to useReducer for state management.
Files involved: @components/Dashboard/ @hooks/useDashboard.ts
Requirements:
- Maintain existing API
- Add undo/redo functionality  
- Preserve TypeScript strict typing
- Update tests accordingly

Please plan this migration step by step.
```

**Performance Optimization Sessions**

```
Analyze performance issues in @components/DataTable.tsx
Consider: memo, useMemo, useCallback, virtualization
Current bundle size: [paste from webpack-bundle-analyzer]
Target: reduce by 30% while maintaining functionality
```

## **Team Collaboration Features**

**Shared Context Documentation**

```
// @team-context.md - Pin this for consistent context
This codebase uses:
- React Query for data fetching
- Zod for validation  
- Tailwind for styling
- Custom hooks pattern from @hooks/useAuth.ts
- Error handling pattern from @components/ErrorBoundary.tsx
```

**Code Review Automation**

```
Review this PR focusing on:
- Security implications (@auth/middleware.ts patterns)
- Performance impact (@hooks/useData.ts patterns)  
- TypeScript strictness
- Test coverage
- Accessibility compliance

Files: @src/components/NewFeature/
```

## **Advanced Configuration**

**Custom Models for Specific Tasks**

- Use faster models for simple autocomplete
- Use more powerful models for complex refactoring
- Switch models based on task complexity

**Privacy & Security Settings**

json

```json
// settings.json
{
  "cursor.privacy.improvedCodebaseIndexing": false, // for sensitive codebases
  "cursor.general.enableAutoImports": true,
  "cursor.cpp.intellisenseEngine": "Default"
}
```

**Ignore Patterns**

json

```json
// .cursorignore
node_modules/
.env*
*.log
dist/
build/
.git/
*.test.ts.snap
coverage/
```

## **Power User Techniques**

**Context Layering**

1. Base context: `.cursorrules` + key architecture files
2. Feature context: Related components + hooks + types
3. Session context: Specific files you're working on
4. Conversation context: Previous chat history

**Batch Operations**

```
Apply this pattern to all components in @components/forms/:
1. Add proper TypeScript types
2. Implement error boundaries  
3. Add loading states
4. Follow accessibility guidelines
5. Update corresponding tests

Process them one by one and ask for confirmation before each.
```

**AI-Assisted Architecture Decisions**

```
I'm designing a new feature for [describe feature].
Current architecture: @src/lib/architecture.md
Constraints: [list constraints]
Options I'm considering: [list options]

Please analyze trade-offs and recommend an approach that fits our existing patterns.
```

## **Integration with External Tools**

**Database Schema Integration**

```
Given this Prisma schema: @prisma/schema.prisma
Generate:
1. TypeScript types
2. CRUD operations
3. React Query hooks
4. Form validation schemas
5. API route handlers
```

**API Documentation Sync**

```
Update our component props based on this API change: @docs/api.md
Affected components: @components/UserProfile/ @components/UserList/
Ensure backward compatibility and add deprecation warnings.
```