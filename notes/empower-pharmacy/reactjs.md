**1. What is React?** 
A **JavaScript library** (not a framework) for building user interfaces, maintained by Meta. It uses a **component-based architecture** and a **declarative** approach to rendering.

**2. Virtual DOM** 
React maintains a lightweight in-memory copy of the real DOM. On state changes, it diffs the new virtual DOM against the previous one (**reconciliation**) and batches only the minimal real DOM updates needed. This makes updates fast.

**3. JSX** 
A syntax extension that lets you write HTML-like markup inside JavaScript. It compiles down to `React.createElement()` calls. Not required, but universally used.

**4. Components** 
- **Functional Components** — plain functions that accept `props` and return JSX. The standard today.
- **Class Components** — ES6 classes extending `React.Component` with a `render()` method. Legacy but still seen in older codebases.

**5. Props vs State** 

|              |                |                                     |
| ------------ | -------------- | ----------------------------------- |
|              | Props          | State                               |
| **Owned by** | Parent         | Component itself                    |
| **Mutable?** | No (read-only) | Yes (via ‎⁠setState⁠ / ‎⁠useState⁠) |
| **Purpose**  | Pass data down | Manage internal data                |
**6. Key Hooks** 
- `useState` — local state in a functional component.
- `useEffect` — side effects (data fetching, subscriptions). Runs after render; cleanup via return function.
- `useContext` — consume context without nesting.
- `useRef` — mutable ref that persists across renders without causing re-renders.
- `useMemo` **/** `useCallback` — memoize expensive computations / callback references to avoid unnecessary re-renders.
- `useReducer` — state management for complex state logic (dispatch + reducer pattern).

**7. Component Lifecycle (mapped to hooks)** 
- **Mount** → `useEffect(() => { ... }, [])`
- **Update** → `useEffect(() => { ... }, [dep])`
- **Unmount** → `useEffect(() => { return () => cleanup() }, [])`

**8. Conditional Rendering** 
Use ternary operators, `&&` short-circuit, or early returns — there’s no `v-if` directive like Vue.

**9. Lists & Keys** 
When rendering lists with `.map()`, each element needs a stable, unique `key` prop so React can efficiently track which items changed, were added, or removed.

**10. State Management** 
- **Local**: `useState`, `useReducer`
- **Shared/global**: Context API, or external libraries like **Redux**, **Zustand**, **Jotai**, **Recoil**

**11. React Router** 
Client-side routing via `react-router-dom`. Key concepts: `<BrowserRouter>`, `<Route>`, `<Link>`, nested routes, dynamic params (`:id`).

**12. Performance Optimization** 
- `React.memo` — skip re-renders if props haven’t changed.
- `useMemo` / `useCallback` — avoid recalculating or recreating on every render.
- **Code splitting** — `React.lazy()` + `Suspense` for lazy-loading components.
- **Key prop** — correct keys prevent unnecessary DOM teardown/rebuild.

**13. Common Interview Topics** 
- **Controlled vs Uncontrolled components** — form inputs managed by React state vs. DOM refs.
- **Lifting state up** — moving shared state to the nearest common ancestor.
- **Prop drilling** — passing props through many layers; solved by Context or state libraries.
- **Higher-Order Components (HOC)** — a function that wraps a component to add behavior (legacy pattern).
- **Render Props** — sharing logic via a prop that is a function (also mostly replaced by hooks).
- **Error Boundaries** — class components with `componentDidCatch` to catch rendering errors (no hook equivalent yet).
- **Strict Mode** — `<React.StrictMode>` double-invokes certain functions in dev to surface side effects.

**14. React 18+ Features** 
- **Concurrent rendering** — React can interrupt and resume rendering work.
- **Automatic batching** — state updates inside async code are now batched too.
- `useTransition` **/** `useDeferredValue` — mark updates as non-urgent to keep the UI responsive.
- **Server Components (React 19)** — components that run on the server and send rendered output to the client, reducing bundle size.

--------------------

**Key React Hooks — With Examples** 

==**1.** `useState`== 
==Adds local state to a functional component.==

```jsx
function Counter() {
  const [count, setCount] = useState(0);
  return <button onClick={() => setCount(count + 1)}>Clicked {count} times</button>;
}
```

  ==**2.** `useEffect`== 
==Runs side effects after render. The dependency array controls _when_ it re-runs.==
```jsx
function UserProfile({ userId }) {
  const [user, setUser] = useState(null);
  useEffect(() => {
    fetch(`/api/users/${userId}`)
      .then(res => res.json())
      .then(setUser);
    return () => console.log('cleanup on unmount or before next run');
  }, [userId]); // re-runs only when userId changes
  return <div>{user?.name}</div>;
}
```

- `[]` → run once on mount
- `[dep]` → run when `dep` changes
- no array → run after every render (rarely wanted)

==**3.** `useContext`== 
==Consumes a context value without wrapper nesting.==
```jsx
const ThemeContext = React.createContext('light');
function ThemedButton() {
  const theme = useContext(ThemeContext); // reads nearest <ThemeContext.Provider>
  return <button className={theme}>Click me</button>;
}
// Parent provides the value:
<ThemeContext.Provider value="dark">
  <ThemedButton />
</ThemeContext.Provider>
```

  ==**4.** `useRef`== 
==Holds a mutable value that persists across renders **without triggering re-renders**. Commonly used for DOM access.==
```jsx
function TextInput() {
  const inputRef = useRef(null);
  const focusInput = () => inputRef.current.focus();
  return (
    <>
      <input ref={inputRef} />
      <button onClick={focusInput}>Focus the input</button>
    </>
  );
}
```

==**5.** `useMemo`== 
==Caches the **result** of an expensive computation. Recalculates only when dependencies change.==

```jsx
function FilteredList({ items, query }) {
  const filtered = useMemo(() => {
    return items.filter(item => item.name.includes(query));
  }, [items, query]);
  return filtered.map(item => <div key={item.id}>{item.name}</div>);
}
```

  ==**6.** `useCallback`== 
==Caches a **function reference**. Useful when passing callbacks to memoized child components.==

```jsx
function Parent() {
  const [count, setCount] = useState(0);
  const handleClick = useCallback(() => {
    setCount(c => c + 1);
  }, []); // same function reference across renders
  return <MemoizedChild onClick={handleClick} />;
}
const MemoizedChild = React.memo(({ onClick }) => {
  return <button onClick={onClick}>Increment</button>;
});
```

**Key difference**: `useMemo` caches a _value_, `useCallback` caches a _function_. In fact, `useCallback(fn, deps)` is equivalent to `useMemo(() => fn, deps)`.

**7.** `useReducer` 
Alternative to `useState` for complex state logic. Follows the Redux dispatch/reducer pattern.

```jsx
function reducer(state, action) {
  switch (action.type) {
    case 'increment': return { count: state.count + 1 };
    case 'decrement': return { count: state.count - 1 };
    case 'reset':     return { count: 0 };
    default: throw new Error('Unknown action');
  }
}
function Counter() {
  const [state, dispatch] = useReducer(reducer, { count: 0 });
  return (
    <>
      <span>{state.count}</span>
      <button onClick={() => dispatch({ type: 'increment' })}>+</button>
      <button onClick={() => dispatch({ type: 'decrement' })}>-</button>
      <button onClick={() => dispatch({ type: 'reset' })}>Reset</button>
    </>
  );
}
```

**When to use over** `useState`: multiple related state values, next state depends on previous, or complex update logic.

**8.** `useTransition` **(React 18+)** 
Marks a state update as **non-urgent** so the UI stays responsive during heavy renders.

```jsx
function Search() {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState([]);
  const [isPending, startTransition] = useTransition();

  const handleChange = (e) => {
    setQuery(e.target.value);           // urgent: update input immediately
    startTransition(() => {
      setResults(filterLargeList(e.target.value)); // non-urgent: can be interrupted
    });
  };

  return (
    <>
      <input value={query} onChange={handleChange} />
      {isPending ? <p>Loading...</p> : <ResultsList results={results} />}
    </>
  );
}
```

**Quick Reference Table** 

|Hook|Returns|Purpose|
|---|---|---|
|`useState`|`[value, setter]`|Simple local state|
|`useEffect`|void|Side effects (fetch, subscribe, timers)|
|`useContext`|context value|Read context without nesting|
|`useRef`|`{ current }`|DOM refs / mutable value that doesn’t trigger re-render|
|`useMemo`|cached value|Avoid expensive recalculations|
|`useCallback`|cached function|Stable function reference for child components|
|`useReducer`|`[state, dispatch]`|Complex state with reducer pattern|
|`useTransition`|`[isPending, startTransition]`|Defer non-urgent updates|

--------------

**Common React Interview Topics — With Examples** 
**1. Controlled vs Uncontrolled Components** 
==**Controlled** — React state drives the input value. You have full control.==

```jsx
function Controlled() {
  const [value, setValue] = useState('');
  return <input value={value} onChange={(e) => setValue(e.target.value)} />;
}
```

==**Uncontrolled** — The DOM holds the value. You read it via a ref when needed.==
```jsx
function Uncontrolled() {
  const inputRef = useRef();
  const handleSubmit = () => alert(inputRef.current.value);
  return (
    <>
      <input ref={inputRef} defaultValue="hello" />
      <button onClick={handleSubmit}>Submit</button>
    </>
  );
}
```

**Interview tip**: ==Controlled is preferred in most cases because React is the single source of truth. Uncontrolled is useful for simple forms or integrating with non-React code.==

==**2. Lifting State Up**== 
==When two sibling components need to share state, move it to their closest common parent.==
```jsx
function Parent() {
  const [text, setText] = useState('');
  return (
    <>
      <InputChild text={text} onChange={setText} />
      <DisplayChild text={text} />
    </>
  );
}
function InputChild({ text, onChange }) {
  return <input value={text} onChange={(e) => onChange(e.target.value)} />;
}
function DisplayChild({ text }) {
  return <p>You typed: {text}</p>;
}
```
The parent **owns** the state; children receive it via props.

==**3. Prop Drilling (and how to fix it)**== 
==**The problem** — passing props through many layers that don’t use them:==
```jsx
// ❌ Prop drilling
<App>           // has `user`
  <Layout user={user}>        // doesn't use it, just passes it
    <Sidebar user={user}>     // doesn't use it, just passes it
      <Avatar user={user} />  // actually needs it
    </Sidebar>
  </Layout>
</App>
```
**The fix** — Context API:
```jsx
const UserContext = React.createContext();
function App() {
  const [user] = useState({ name: 'Alice', avatar: '/alice.png' });
  return (
    <UserContext.Provider value={user}>
      <Layout />
    </UserContext.Provider>
  );
}
// Avatar reads it directly — no drilling needed
function Avatar() {
  const user = useContext(UserContext);
  return <img src={user.avatar} alt={user.name} />;
}
```

**4. Higher-Order Components (HOC)** 
A function that takes a component and returns a new component with added behavior. Legacy pattern, mostly replaced by hooks.

```jsx
function withLogger(WrappedComponent) {
  return function EnhancedComponent(props) {
    useEffect(() => {
      console.log(`${WrappedComponent.name} mounted`);
    }, []);

    return <WrappedComponent {...props} />;
  };
}

const ButtonWithLogging = withLogger(Button);

// Usage: <ButtonWithLogging label="Click" />
```

**Interview tip**: Know the pattern but mention that custom hooks are the modern replacement.

**5. Render Props** 
Sharing logic by passing a function as a prop that returns JSX.
```jsx
function MouseTracker({ render }) {
  const [pos, setPos] = useState({ x: 0, y: 0 });
  const handleMouseMove = (e) => setPos({ x: e.clientX, y: e.clientY });
  return <div onMouseMove={handleMouseMove}>{render(pos)}</div>;
}
// Usage
<MouseTracker render={({ x, y }) => <p>Mouse is at ({x}, {y})</p>} />
```

**Modern equivalent with a custom hook**:
```jsx
function useMousePosition() {
  const [pos, setPos] = useState({ x: 0, y: 0 });

  useEffect(() => {
    const handler = (e) => setPos({ x: e.clientX, y: e.clientY });
    window.addEventListener('mousemove', handler);
    return () => window.removeEventListener('mousemove', handler);
  }, []);

  return pos;
}

function MyComponent() {
  const { x, y } = useMousePosition();
  return <p>Mouse is at ({x}, {y})</p>;
}
```

  

==**6. Error Boundaries**== 
==Class components that catch JavaScript errors in their child tree and show a fallback UI.== **No hook equivalent exists yet.**

```jsx
class ErrorBoundary extends React.Component {
  state = { hasError: false };
  static getDerivedStateFromError(error) {
    return { hasError: true };
  }
  componentDidCatch(error, info) {
    console.error('Caught by boundary:', error, info);
  }
  render() {
    if (this.state.hasError) {
      return <h2>Something went wrong.</h2>;
    }
    return this.props.children;
  }
}
// Usage
<ErrorBoundary>
  <RiskyComponent />
</ErrorBoundary>
```

**Interview tip**: Error boundaries only catch errors during rendering, lifecycle methods, and constructors — **not** in event handlers, async code, or server-side rendering. For event handlers, use regular try/catch.

==**7. Strict Mode**== 
==A development-only wrapper that helps find problems early. It does **not** render any visible UI.==

```jsx
<React.StrictMode>
  <App />
</React.StrictMode>
```

**What it does in dev mode**:
- **Double-invokes** functions like component bodies, `useState` initializers, and `useEffect` to expose impure logic.
- Warns about deprecated lifecycle methods.
- Detects unexpected side effects.

- ```jsx
    // This will log TWICE in dev with StrictMode — helps you notice the side effect
    function Example() {
      console.log('rendered'); // you'll see this twice
      const [count, setCount] = useState(() => {
        console.log('initializing'); // also twice
        return 0;
      });
      return <div>{count}</div>;
    }
    ```

**Interview tip**: The double-invoke only happens in development. Production builds behave normally.
**Quick Cheat Sheet** 

|Topic|Modern alternative|Still relevant?|
|---|---|---|
|Controlled/Uncontrolled|—|Yes, core concept|
|Lifting State Up|—|Yes, core concept|
|Prop Drilling|Context / Zustand / Redux|Yes, know the problem + solutions|
|HOCs|Custom hooks|Know it, but use hooks|
|Render Props|Custom hooks|Know it, but use hooks|
|Error Boundaries|None (class only)|Yes, no hook replacement|
|Strict Mode|—|Yes, know what it does|