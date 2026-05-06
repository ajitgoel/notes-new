## Hooks, patterns, performance, and TypeScript — interview ready

> [!info] Context
> Empower's stack includes **React.js** for frontend. Dave's teams have built UIs across multiple roles. This covers modern React (hooks, functional components, TypeScript) — class components are legacy.

---
## 1. ==useState — Managing Local State==

```tsx hl:14-17,1,7-12
import { useState } from "react";
function PrescriptionForm() {
  // Simple state
  const [medication, setMedication] = useState("");
  const [dosage, setDosage] = useState(0);
  // Object state — always spread to avoid losing fields
  const [form, setForm] = useState({
    patientId: 0,
    medication: "",
    dosage: 0,
    notes: "",
  });
  
  const updateField = (field: string, value: string | number) => {
    setForm(prev => ({ ...prev, [field]: value }));
    // ✅ ...prev preserves other fields
    // ❌ setForm({ [field]: value }) wipes everything else
  };

  // Array state
  const [items, setItems] = useState<string[]>([]);

  const addItem = (item: string) => {
    setItems(prev => [...prev, item]);       // append
  };

  const removeItem = (index: number) => {
    setItems(prev => prev.filter((_, i) => i !== index));  // remove
  };

  return (/* ... */);
}
```

==**State updates are asynchronous**==
==`setState` doesn't update immediately. If you need the previous value to compute the next, use the **callback form**: `setCount(prev => prev + 1)`, not `setCount(count + 1)`==.

---
## 2. useEffect — Side Effects
==`useEffect` runs after render. It's for side effects: API calls,== subscriptions, ==timers==, DOM manipulation.
```tsx hl:1,6,28
import { useEffect, useState } from "react";
function PrescriptionList({ patientId }: { patientId: number }) {
  const [prescriptions, setPrescriptions] = useState<Prescription[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  useEffect(() => {
    let cancelled = false;  // prevent setting state on unmounted component
    async function fetchData() {
      setLoading(true);
      setError(null);
      try {
        const res = await fetch(`/api/patients/${patientId}/prescriptions`);
        if (!res.ok) throw new Error("Failed to fetch");
        const data = await res.json();
        if (!cancelled) {
          setPrescriptions(data);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Unknown error");
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    fetchData();
    return () => { cancelled = true; };  // cleanup on unmount or re-run
  }, [patientId]);  // re-runs when patientId changes
  if (loading) return <Spinner />;
  if (error) return <ErrorMessage message={error} />;
  return <PrescriptionTable data={prescriptions} />;
}
```
### Dependency Array Rules

| Dependency Array   | When Effect Runs                                |
| ------------------ | ----------------------------------------------- |
| ==`[]`==               | ==Once after first render (mount only)==            |
| ==`[patientId]`==      | ==On mount + whenever `patientId` changes==         |
| No array (omitted) | After EVERY render — almost never what you want |
### Cleanup Function
1qsx
```tsx
useEffect(() => {
  const interval = setInterval(() => {
    fetchOrderCount();
  }, 5000);

  // Cleanup: runs when component unmounts OR before effect re-runs
  return () => clearInterval(interval);
}, []);
```

---
## 3. ==useRef — Persistent Values Without Re-renders==
```tsx hl:4,6,23,1
import { useRef, useEffect } from "react";
function SearchInput() {
  // DOM reference
  const inputRef = useRef<HTMLInputElement>(null);
  useEffect(() => {
    inputRef.current?.focus();  // auto-focus on mount
  }, []);

  // Mutable value that persists across renders (doesn't trigger re-render)
  const renderCount = useRef(0);
  renderCount.current += 1;  // doesn't cause re-render

  // Previous value tracking
  const prevPatientId = useRef<number>();

  useEffect(() => {
    if (prevPatientId.current !== undefined
        && prevPatientId.current !== patientId) {
      console.log(`Patient changed from ${prevPatientId.current} to ${patientId}`);
    }
    prevPatientId.current = patientId;
  }, [patientId]);
  return <input ref={inputRef} placeholder="Search..." />;
}
```

**useState vs useRef**
==`useState`: when the UI should re-render when the value changes (form fields, lists, toggles).==
==`useRef`: when you need a value to persist but changing it should NOT re-render== (DOM refs, timers, previous values).

---
## 4. useMemo & useCallback — Performance
### ==useMemo — cache an expensive calculation==
```tsx hl:1,4,11
import { useMemo } from "react";
function OrderDashboard({ orders }: { orders: Order[] }) {
  // Only recalculates when `orders` changes, not on every render
  const stats = useMemo(() => ({
    total: orders.length,
    pending: orders.filter(o => o.status === "Pending").length,
    revenue: orders.reduce((sum, o) => sum + o.total, 0),
    avgValue: orders.length
      ? orders.reduce((sum, o) => sum + o.total, 0) / orders.length
      : 0,
  }), [orders]);
  return (
    <div>
      <StatCard label="Total Orders" value={stats.total} />
      <StatCard label="Revenue" value={`$${stats.revenue.toFixed(2)}`} />
    </div>
  );
}
```

### useCallback — cache a function reference

```tsx hl:1,4-6,11
import { useCallback, useState } from "react";
function PatientSearch() {
  const [query, setQuery] = useState("");
  // Without useCallback, this creates a new function on every render,
  // causing child components that receive it to re-render unnecessarily
  const handleSearch = useCallback((searchTerm: string) => {
    fetch(`/api/patients?search=${searchTerm}`)
      .then(res => res.json())
      .then(data => setResults(data));
  }, []);  // empty deps = function never changes
  return <SearchInput onSearch={handleSearch} />;
}
```

> [!warning] Don't overuse these
==`useMemo` and `useCallback` have overhead (storing and comparing). Only use them for: (1) genuinely expensive computations, (2) preventing re-renders in child components wrapped in `React.memo`, (3) values passed as dependencies to other hooks.==

---
## 5. Custom Hooks — Reusable Logic
Extract repeated patterns into custom hooks. Any function starting with `use` is a hook.
```tsx
// === useFetch — reusable data fetching ===
function useFetch<T>(url: string) {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    setLoading(true);
    fetch(url)
      .then(res => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return res.json();
      })
      .then(data => { if (!cancelled) setData(data); })
      .catch(err => { if (!cancelled) setError(err.message); })
      .finally(() => { if (!cancelled) setLoading(false); });

    return () => { cancelled = true; };
  }, [url]);

  return { data, loading, error };
}

// Usage
function PatientPage({ id }: { id: number }) {
  const { data: patient, loading, error } = useFetch<Patient>(`/api/patients/${id}`);

  if (loading) return <Spinner />;
  if (error) return <ErrorMessage message={error} />;
  return <PatientProfile patient={patient!} />;
}


// === useDebounce — delay rapid updates ===
function useDebounce<T>(value: T, delay: number): T {
  const [debouncedValue, setDebouncedValue] = useState(value);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedValue(value), delay);
    return () => clearTimeout(timer);
  }, [value, delay]);

  return debouncedValue;
}

// Usage: search only after user stops typing for 300ms
function SearchBar() {
  const [query, setQuery] = useState("");
  const debouncedQuery = useDebounce(query, 300);

  useEffect(() => {
    if (debouncedQuery) {
      searchPatients(debouncedQuery);
    }
  }, [debouncedQuery]);

  return <input value={query} onChange={e => setQuery(e.target.value)} />;
}
```

---
## 6. Context — Sharing State Across Components
```tsx hl:1,9,29
import { createContext, useContext, useState, ReactNode } from "react";
// === Define the context ===
type AuthContextType = {
  user: User | null;
  login: (token: string) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
};
const AuthContext = createContext<AuthContextType | undefined>(undefined);
// === Provider component ===
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const login = async (token: string) => {
    const res = await fetch("/api/auth/me", {
      headers: { Authorization: `Bearer ${token}` },
    });
    const userData = await res.json();
    setUser(userData);
  };
  const logout = () => setUser(null);
  return (
    <AuthContext.Provider value={{ user, login, logout, isAuthenticated: !!user }}>
      {children}
    </AuthContext.Provider>
  );
}
// === Custom hook for consuming ===
export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used within AuthProvider");
  return context;
}
// === Usage in any component ===
function Navbar() {
  const { user, logout, isAuthenticated } = useAuth();
  return (
    <nav>
      {isAuthenticated ? (
        <>
          <span>Welcome, {user!.name}</span>
          <button onClick={logout}>Logout</button>
        </>
      ) : (
        <a href="/login">Login</a>
      )}
    </nav>
  );
}
```

> [!warning] Context re-render trap
==When the Provider's value changes, ALL consumers re-render — even if they only use one field. For large apps, split contexts by concern (AuthContext, ThemeContext, NotificationContext) rather than one giant AppContext.==

---
## 7. useReducer — Complex State Logic

When state has multiple sub-values or the next state depends on the previous, `useReducer` is cleaner than multiple `useState` calls.
```tsx
import { useReducer } from "react";

type OrderState = {
  items: OrderItem[];
  status: "idle" | "loading" | "success" | "error";
  error: string | null;
  total: number;
};

type OrderAction =
  | { type: "ADD_ITEM"; payload: OrderItem }
  | { type: "REMOVE_ITEM"; payload: number }
  | { type: "SUBMIT_START" }
  | { type: "SUBMIT_SUCCESS" }
  | { type: "SUBMIT_ERROR"; payload: string }
  | { type: "RESET" };

function orderReducer(state: OrderState, action: OrderAction): OrderState {
  switch (action.type) {
    case "ADD_ITEM":
      const newItems = [...state.items, action.payload];
      return {
        ...state,
        items: newItems,
        total: newItems.reduce((sum, i) => sum + i.price * i.quantity, 0),
      };
    case "REMOVE_ITEM":
      const filtered = state.items.filter((_, i) => i !== action.payload);
      return {
        ...state,
        items: filtered,
        total: filtered.reduce((sum, i) => sum + i.price * i.quantity, 0),
      };
    case "SUBMIT_START":
      return { ...state, status: "loading", error: null };
    case "SUBMIT_SUCCESS":
      return { ...state, status: "success", items: [], total: 0 };
    case "SUBMIT_ERROR":
      return { ...state, status: "error", error: action.payload };
    case "RESET":
      return initialState;
    default:
      return state;
  }
}

const initialState: OrderState = {
  items: [], status: "idle", error: null, total: 0,
};

function OrderBuilder() {
  const [state, dispatch] = useReducer(orderReducer, initialState);

  const addMedication = (med: OrderItem) => {
    dispatch({ type: "ADD_ITEM", payload: med });
  };

  const submitOrder = async () => {
    dispatch({ type: "SUBMIT_START" });
    try {
      await fetch("/api/orders", {
        method: "POST",
        body: JSON.stringify({ items: state.items }),
      });
      dispatch({ type: "SUBMIT_SUCCESS" });
    } catch (err) {
      dispatch({ type: "SUBMIT_ERROR", payload: "Failed to submit" });
    }
  };

  return (/* ... */);
}
```

> [!tip] useState vs useReducer
> **useState**: simple, independent values (a toggle, a text field, a counter).
> **useReducer**: related values that change together, complex transitions, or when the next state depends on the current state. Also great when you want to test state logic in isolation.

---

## 8. React.memo, Keys, and Re-renders

### React.memo — skip re-renders when props haven't changed

```tsx hl:1-3
// Without memo: re-renders every time parent re-renders
// With memo: only re-renders if `patient` prop actually changed
const PatientCard = React.memo(function PatientCard({ patient }: { patient: Patient }) {
  return (
    <div>
      <h3>{patient.name}</h3>
      <p>{patient.email}</p>
    </div>
  );
});
```

### Keys — how React identifies list items

```tsx
// ✅ GOOD: stable, unique ID from data
{patients.map(p => <PatientCard key={p.id} patient={p} />)}

// ❌ BAD: index as key — breaks when items are reordered/removed
{patients.map((p, i) => <PatientCard key={i} patient={p} />)}

// ❌ BAD: random key — forces full re-mount every render
{patients.map(p => <PatientCard key={Math.random()} patient={p} />)}
```

### What triggers a re-render?

1. **setState** called on this component
2. **Parent re-renders** (unless wrapped in `React.memo`)
3. **Context value changes** (for consumers of that context)

---

## 9. Component Patterns

### Controlled vs Uncontrolled Inputs

```tsx
// Controlled — React owns the value
function ControlledInput() {
  const [value, setValue] = useState("");
  return <input value={value} onChange={e => setValue(e.target.value)} />;
}

// Uncontrolled — DOM owns the value
function UncontrolledInput() {
  const inputRef = useRef<HTMLInputElement>(null);
  const handleSubmit = () => {
    console.log(inputRef.current?.value);  // read from DOM
  };
  return <input ref={inputRef} defaultValue="" />;
}
```
### Composition over Props Drilling
```tsx hl:11,1
// ❌ Props drilling — passing data through 4 layers
<App user={user}>
  <Dashboard user={user}>
    <Sidebar user={user}>
      <UserAvatar user={user} />
// ✅ Composition — pass the component itself
function Dashboard({ sidebar }: { sidebar: ReactNode }) {
  return <div className="flex">{sidebar}<main>...</main></div>;
}
<Dashboard sidebar={<Sidebar><UserAvatar user={user} /></Sidebar>} />
// ✅ Or use Context for truly global data (auth, theme)
```
### Error Boundaries
```tsx
import { Component, ReactNode } from "react";

class ErrorBoundary extends Component<
  { children: ReactNode; fallback: ReactNode },
  { hasError: boolean }
> {
  state = { hasError: false };

  static getDerivedStateFromError() {
    return { hasError: true };
  }

  componentDidCatch(error: Error, info: React.ErrorInfo) {
    console.error("Error boundary caught:", error, info);
  }

  render() {
    if (this.state.hasError) return this.props.fallback;
    return this.props.children;
  }
}

// Usage
<ErrorBoundary fallback={<p>Something went wrong</p>}>
  <PrescriptionList />
</ErrorBoundary>
```

---

## 10. TypeScript with React

```tsx
// Props with TypeScript
type PatientCardProps = {
  patient: Patient;
  onArchive: (id: number) => void;
  variant?: "compact" | "full";     // optional with default
  children?: ReactNode;             // optional children
};

function PatientCard({ patient, onArchive, variant = "full" }: PatientCardProps) {
  return (/* ... */);
}


// Event types
const handleClick = (e: React.MouseEvent<HTMLButtonElement>) => { };
const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => { };
const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
  e.preventDefault();
};


// Generic component
type ListProps<T> = {
  items: T[];
  renderItem: (item: T) => ReactNode;
  keyExtractor: (item: T) => string | number;
};

function List<T>({ items, renderItem, keyExtractor }: ListProps<T>) {
  return (
    <ul>
      {items.map(item => (
        <li key={keyExtractor(item)}>{renderItem(item)}</li>
      ))}
    </ul>
  );
}

// Usage — TypeScript infers T from items
<List
  items={patients}
  renderItem={p => <PatientCard patient={p} />}
  keyExtractor={p => p.id}
/>
```

---
## 11. Quick-Fire Interview Q&A

### =="What's the virtual DOM?"==
==A lightweight JavaScript representation of the real DOM. When state changes, React creates a new virtual DOM tree, diffs it against the previous one, and only updates the real DOM where differences exist. This batched, minimal update is why React is fast.==
### "Why can't you call hooks inside conditions or loops?"
React tracks hooks by their call order. If a hook is inside an `if` block that sometimes doesn't run, the order shifts and React maps the wrong state to the wrong hook. Always call hooks at the top level of your component.
### "What's the difference between controlled and uncontrolled components?"
Controlled: React state is the source of truth (`value` + `onChange`). Uncontrolled: the DOM is the source of truth (`ref` + `defaultValue`). Use controlled for forms where you need validation, conditional logic, or to share the value with other components.
### =="How do you prevent unnecessary re-renders?"==
==(1) `React.memo` on child components,== 
==(2) `useMemo` for expensive calculations,== 
==(3) `useCallback` for function props passed to memoized children,== 
==(4) split Context into smaller pieces,== 
==(5) move state as close to where it's used as possible.==
### "When would you use useReducer over useState?"
When state transitions are complex (multiple sub-values that change together), when the next state depends on the previous state in non-trivial ways, or when you want to extract and test state logic independently. Think of it as a mini Redux at the component level.
### "What are the rules of hooks?"
(1) Only call hooks at the top level — never in loops, conditions, or nested functions. (2) Only call hooks from React function components or custom hooks — never from regular JavaScript functions.