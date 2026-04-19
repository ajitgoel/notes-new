**1. Components** 
The building blocks of React. Two ways to write them:
```jsx hl:2-4
// Function component (modern, preferred)
function Greeting({ name }) {
  return <h1>Hello, {name}!</h1>;
}
// Arrow function variant
const Greeting = ({ name }) => <h1>Hello, {name}!</h1>;

// Class component (legacy, but still seen in codebases)
class Greeting extends React.Component {
  render() {
    return <h1>Hello, {this.props.name}!</h1>;
  }
}
```

  **2. JSX** 
JSX is syntactic sugar for `React.createElement()`. It looks like HTML but it’s JavaScript.
```jsx
function UserCard({ user }) {
  return (
    <div className="card">           {/* className, not class */}
      <img src={user.avatar} />       {/* self-closing tags required */}
      <h2>{user.name}</h2>            {/* JS expressions in curly braces */}
      <p>{user.age > 18 ? "Adult" : "Minor"}</p>
      <p style={{ color: "red", fontSize: "14px" }}> {/* double braces for inline styles */}
        Status: {user.active && "Active"}              {/* short-circuit rendering */}
      </p>
    </div>
  );
}
```
  **Key JSX rules:**
- Must return a single root element (use `<>...</>` fragments to avoid extra DOM nodes)
- `className` instead of `class`
- `htmlFor` instead of `for`
- All tags must be closed

**3. Props** 
Data passed **down** from parent to child. Props are read-only.
```jsx hl:3,6
// Parent passes data
function App() {
  return <UserProfile name="Alice" role="Doctor" onLogout={() => console.log("bye")} />;
}
// Child receives and uses it
function UserProfile({ name, role, onLogout }) {
  return (
    <div>
      <h2>{name}</h2>
      <p>{role}</p>
      <button onClick={onLogout}>Logout</button>
    </div>
  );
}
// Default props
function Button({ variant = "primary", children }) {
  return <button className={`btn-${variant}`}>{children}</button>;
}

// children prop — anything between opening and closing tags
<Button variant="danger">Delete Account</Button>
```

**4. State with** `useState` 
State is data that **changes over time** and triggers re-renders.
```jsx hl:2,6
function Counter() {
  const [count, setCount] = useState(0);
  return (
    <div>
      <p>Count: {count}</p>
      <button onClick={() => setCount(count + 1)}>Increment</button>
      <button onClick={() => setCount(prev => prev - 1)}>Decrement</button>
      {/* Use the functional form (prev =>) when new state depends on old state */}
    </div>
  );
}
```

  **State with objects:**
```jsx
function ProfileForm() {
  const [form, setForm] = useState({ name: "", email: "" });
  const handleChange = (e) => {
    setForm(prev => ({
      ...prev,                        // spread existing state
      [e.target.name]: e.target.value  // update only the changed field
    }));
  };
  return (
    <form>
      <input name="name" value={form.name} onChange={handleChange} />
      <input name="email" value={form.email} onChange={handleChange} />
    </form>
  );
}
```

**5.** `useEffect` **— Side Effects** 
For anything outside the render cycle: API calls, subscriptions, timers, DOM manipulation.

```jsx
function UserList() {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  // Runs once on mount (empty dependency array)
  useEffect(() => {
    fetch("/api/users")
      .then(res => res.json())
      .then(data => {
        setUsers(data);
        setLoading(false);
      });
  }, []);

  if (loading) return <p>Loading...</p>;
  return <ul>{users.map(u => <li key={u.id}>{u.name}</li>)}</ul>;
}
```

**Dependency array controls when it runs:**
```jsx
useEffect(() => { ... });           // Runs after every render
useEffect(() => { ... }, []);       // Runs once on mount
useEffect(() => { ... }, [userId]); // Runs when userId changes
// Cleanup function — runs before the effect re-runs or on unmount
useEffect(() => {
  const interval = setInterval(() => tick(), 1000);
  return () => clearInterval(interval);  // cleanup
}, []);
```

**6. Conditional Rendering** 
```jsx
function Dashboard({ user }) {
  // Early return
  if (!user) return <LoginPage />;
  return (
    <div>
      {/* Ternary */}
      {user.role === "admin" ? <AdminPanel /> : <UserPanel />}

      {/* Short-circuit (render only if true) */}
      {user.notifications.length > 0 && <NotificationBadge count={user.notifications.length} />}
    </div>
  );
}
```

  

**7. Lists and Keys** 

```jsx
function TodoList({ todos }) {
  return (
    <ul>
      {todos.map(todo => (
        <li key={todo.id} className={todo.done ? "completed" : ""}>
          {todo.text}
        </li>
      ))}
    </ul>
  );
}
```

**Key rules:**
- `key` must be stable and unique among siblings
- Use IDs from data, **not array index** (index causes bugs with reordering/deleting)
- Keys help React identify which items changed, added, or removed

**8. Event Handling** 
```jsx
function SearchBox() {
  const [query, setQuery] = useState("");
  const handleSubmit = (e) => {
    e.preventDefault();       // prevent page reload
    console.log("Searching:", query);
  };
  return (
    <form onSubmit={handleSubmit}>
      <input
        type="text"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder="Search..."
      />
      <button type="submit">Go</button>
    </form>
  );
}
```

  

Common events: `onClick`, `onChange`, `onSubmit`, `onFocus`, `onBlur`, `onKeyDown`, `onMouseEnter`

**9.** `useRef` **— DOM Access & Persistent Values** 

```jsx
function AutoFocusInput() {
  const inputRef = useRef(null);

  useEffect(() => {
    inputRef.current.focus();  // directly access the DOM node
  }, []);

  return <input ref={inputRef} placeholder="I auto-focus" />;
}

// Also useful for values that persist across renders without causing re-renders
function StopWatch() {
  const [time, setTime] = useState(0);
  const intervalRef = useRef(null);

  const start = () => {
    intervalRef.current = setInterval(() => setTime(t => t + 1), 1000);
  };
  const stop = () => clearInterval(intervalRef.current);

  return (
    <div>
      <p>{time}s</p>
      <button onClick={start}>Start</button>
      <button onClick={stop}>Stop</button>
    </div>
  );
}
```

  

**10.** `useMemo` **and** `useCallback` **— Performance** 

```jsx
function ExpensiveList({ items, filter }) {
  // useMemo: cache a computed value, recompute only when dependencies change
  const filtered = useMemo(() => {
    return items.filter(item => item.category === filter);
  }, [items, filter]);

  // useCallback: cache a function reference (prevents child re-renders)
  const handleSelect = useCallback((id) => {
    console.log("Selected:", id);
  }, []);

  return filtered.map(item => (
    <ListItem key={item.id} item={item} onSelect={handleSelect} />
  ));
}

const ListItem = React.memo(({ item, onSelect }) => {
  return <div onClick={() => onSelect(item.id)}>{item.name}</div>;
});
```

**When to use:**
- `useMemo` — expensive calculations or derived data
- `useCallback` — functions passed as props to memoized children
- Don’t overuse them — they have their own cost. Only optimize when there’s a measurable problem.

**11. Context API — Avoiding Prop Drilling** 
```jsx
// Create context
const AuthContext = createContext(null);

// Provider wraps the tree
function AuthProvider({ children }) {
  const [user, setUser] = useState(null);

  const login = (userData) => setUser(userData);
  const logout = () => setUser(null);

  return (
    <AuthContext.Provider value={{ user, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

// Custom hook for consuming (cleaner than useContext everywhere)
function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used within AuthProvider");
  return context;
}

// Usage in any nested component
function NavBar() {
  const { user, logout } = useAuth();

  return (
    <nav>
      <span>Welcome, {user?.name}</span>
      <button onClick={logout}>Logout</button>
    </nav>
  );
}

// App setup
function App() {
  return (
    <AuthProvider>
      <NavBar />
      <MainContent />
    </AuthProvider>
  );
}
```

**12. Custom Hooks — Reusable Logic** 
```jsx
// Reusable data-fetching hook
function useFetch(url) {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    let cancelled = false;

    fetch(url)
      .then(res => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return res.json();
      })
      .then(data => { if (!cancelled) { setData(data); setLoading(false); } })
      .catch(err => { if (!cancelled) { setError(err); setLoading(false); } });

    return () => { cancelled = true; };  // cleanup on unmount or URL change
  }, [url]);

  return { data, loading, error };
}

// Usage
function PatientList() {
  const { data: patients, loading, error } = useFetch("/api/patients");

  if (loading) return <Spinner />;
  if (error) return <ErrorMessage message={error.message} />;
  return <ul>{patients.map(p => <li key={p.id}>{p.name}</li>)}</ul>;
}
```

**13.** `useReducer` **— Complex State Logic** 
Better than `useState` when state transitions are complex or interdependent:
```jsx
const initialState = { items: [], loading: false, error: null };

function cartReducer(state, action) {
  switch (action.type) {
    case "ADD_ITEM":
      return { ...state, items: [...state.items, action.payload] };
    case "REMOVE_ITEM":
      return { ...state, items: state.items.filter(i => i.id !== action.payload) };
    case "CLEAR":
      return { ...state, items: [] };
    case "SET_ERROR":
      return { ...state, error: action.payload, loading: false };
    default:
      return state;
  }
}

function ShoppingCart() {
  const [state, dispatch] = useReducer(cartReducer, initialState);

  return (
    <div>
      {state.items.map(item => (
        <div key={item.id}>
          {item.name}
          <button onClick={() => dispatch({ type: "REMOVE_ITEM", payload: item.id })}>
            Remove
          </button>
        </div>
      ))}
      <button onClick={() => dispatch({ type: "CLEAR" })}>Clear Cart</button>
    </div>
  );
}
```

**14. React Router (v6)** 
```jsx
import { BrowserRouter, Routes, Route, Link, Navigate, useParams } from "react-router-dom";

function App() {
  const { user } = useAuth();

  return (
    <BrowserRouter>
      <nav>
        <Link to="/">Home</Link>
        <Link to="/patients">Patients</Link>
      </nav>

      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/login" element={<LoginPage />} />

        {/* Protected route */}
        <Route path="/patients" element={
          user ? <PatientList /> : <Navigate to="/login" />
        } />

        {/* Dynamic route */}
        <Route path="/patients/:id" element={<PatientDetail />} />

        {/* Catch-all */}
        <Route path="*" element={<NotFound />} />
      </Routes>
    </BrowserRouter>
  );
}

function PatientDetail() {
  const { id } = useParams();
  const { data: patient } = useFetch(`/api/patients/${id}`);
  return <div>{patient?.name}</div>;
}
```

**Quick Reference: Hooks Summary** 

| Hook          | Purpose                                                      |
| ------------- | ------------------------------------------------------------ |
| `useState`    | Local component state                                        |
| `useEffect`   | Side effects (API calls, subscriptions, timers)              |
| `useContext`  | Consume context without prop drilling                        |
| `useRef`      | DOM references, mutable values that don’t trigger re-renders |
| `useMemo`     | Memoize expensive computed values                            |
| `useCallback` | Memoize function references                                  |
| `useReducer`  | Complex state with action-based transitions                  |