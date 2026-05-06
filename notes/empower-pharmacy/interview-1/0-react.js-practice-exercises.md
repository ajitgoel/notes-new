## Empower Pharmacy Interview Prep

---
## Exercise 1: Build a Prescription Search with Debounce
**Difficulty:** ⭐⭐⭐ | **Time:** 20 min | **Topics:** useState, useEffect, custom hooks, TypeScript

### Problem

Build a `PrescriptionSearch` component that:

1. Has a text input for searching prescriptions
2. Uses a custom `useDebounce` hook to wait 300ms after the user stops typing
3. Fetches results from `/api/prescriptions?search=...` using the debounced value
4. Shows loading, error, and empty states
5. Displays results in a list showing medication name, patient, and status
6. Fully typed with TypeScript

---

> [!success]- Solution (click to expand)
>
> ```tsx
> // === hooks/useDebounce.ts ===
> import { useState, useEffect } from "react";

> export function useDebounce(value: T, delay: number): T {
>   const [debouncedValue, setDebouncedValue] = useState(value);
>   useEffect(() => {
>     const timer = setTimeout(() => setDebouncedValue(value), delay);
>     return () => clearTimeout(timer);
>   }, [value, delay]);
>   return debouncedValue;
> }
> // === hooks/useFetch.ts ===
> import { useState, useEffect } from "react";
> export function useFetch(url: string | null) {
>   const [data, setData] = useState(null);
>   const [loading, setLoading] = useState(false);
>   const [error, setError] = useState(null);
>
>   useEffect(() => {
>     if (!url) { setData(null); return; }
>
>     let cancelled = false;
>     setLoading(true);
>     setError(null);
>
>     fetch(url)
>       .then(res => {
>         if (!res.ok) throw new Error(`HTTP ${res.status}`);
>         return res.json();
>       })
>       .then(data => { if (!cancelled) setData(data); })
>       .catch(err => { if (!cancelled) setError(err.message); })
>       .finally(() => { if (!cancelled) setLoading(false); });
>
>     return () => { cancelled = true; };
>   }, [url]);
>
>   return { data, loading, error };
> }
>
>
> // === types.ts ===
> type Prescription = {
>   id: number;
>   medicationName: string;
>   patientName: string;
>   status: "Active" | "Expiring" | "Expired";
>   dosage: number;
> };
>
>
> // === PrescriptionSearch.tsx ===
> import { useState } from "react";
> import { useDebounce } from "./hooks/useDebounce";
> import { useFetch } from "./hooks/useFetch";
>
> export default function PrescriptionSearch() {
>   const [query, setQuery] = useState("");
>   const debouncedQuery = useDebounce(query, 300);
>
>   const url = debouncedQuery.length >= 2
>     ? `/api/prescriptions?search=${encodeURIComponent(debouncedQuery)}`
>     : null;
>
>   const { data: results, loading, error } = useFetch(url);
>
>   return (
>     
>                value={query}
>         onChange={e => setQuery(e.target.value)}
>         placeholder="Search prescriptions (min 2 chars)..."
>         className="w-full p-3 border rounded"
>       />
>
>       {loading && Searching...}
>       {error && Error: {error}}
>
>       {results && results.length === 0 && (
>         No prescriptions found
>       )}
>
>       {results && results.length > 0 && (
>         
>           {results.map(rx => (
>             
>               
>                 {rx.medicationName}
>                 {rx.patientName}
>               
>                                rx.status === "Active" ? "text-green-600" :
>                 rx.status === "Expiring" ? "text-yellow-600" : "text-red-600"
>               }>
>                 {rx.status}
>               
>             
>           ))}
>         
>       )}
>     
>   );
> }
> ```

---

## Exercise 2: Build an Order Builder with useReducer
**Difficulty:** ⭐⭐⭐⭐ | **Time:** 25 min | **Topics:** useReducer, TypeScript, complex state

### Problem

Build an `OrderBuilder` component where a user can:

1. Add medications to an order (from a dropdown)
2. Set quantity for each medication
3. Remove items from the order
4. See a running total that updates automatically
5. Submit the order (mock API call with loading/success/error states)
6. Reset the form after successful submission

Use `useReducer` — not multiple `useState` calls. Define the full `Action` union type.

---

> [!success]- Solution (click to expand)
>
> ```tsx
> import { useReducer } from "react";
>
> // === Types ===
> type Medication = { id: number; name: string; unitPrice: number };
> type OrderItem = Medication & { quantity: number };
>
> type State = {
>   items: OrderItem[];
>   total: number;
>   status: "idle" | "submitting" | "success" | "error";
>   error: string | null;
> };
>
> type Action =
>   | { type: "ADD_ITEM"; payload: Medication }
>   | { type: "SET_QUANTITY"; payload: { id: number; quantity: number } }
>   | { type: "REMOVE_ITEM"; payload: number }
>   | { type: "SUBMIT_START" }
>   | { type: "SUBMIT_SUCCESS" }
>   | { type: "SUBMIT_ERROR"; payload: string }
>   | { type: "RESET" };
>
> const calcTotal = (items: OrderItem[]) =>
>   items.reduce((sum, i) => sum + i.unitPrice * i.quantity, 0);
>
> function reducer(state: State, action: Action): State {
>   switch (action.type) {
>     case "ADD_ITEM": {
>       const exists = state.items.find(i => i.id === action.payload.id);
>       if (exists) return state; // prevent duplicates
>       const items = [...state.items, { ...action.payload, quantity: 1 }];
>       return { ...state, items, total: calcTotal(items) };
>     }
>     case "SET_QUANTITY": {
>       const items = state.items.map(i =>
>         i.id === action.payload.id
>           ? { ...i, quantity: Math.max(1, action.payload.quantity) }
>           : i
>       );
>       return { ...state, items, total: calcTotal(items) };
>     }
>     case "REMOVE_ITEM": {
>       const items = state.items.filter(i => i.id !== action.payload);
>       return { ...state, items, total: calcTotal(items) };
>     }
>     case "SUBMIT_START":
>       return { ...state, status: "submitting", error: null };
>     case "SUBMIT_SUCCESS":
>       return { items: [], total: 0, status: "success", error: null };
>     case "SUBMIT_ERROR":
>       return { ...state, status: "error", error: action.payload };
>     case "RESET":
>       return { items: [], total: 0, status: "idle", error: null };
>     default:
>       return state;
>   }
> }
>
> const MEDICATIONS: Medication[] = [
>   { id: 1, name: "Lisinopril 10mg", unitPrice: 12.50 },
>   { id: 2, name: "Metformin 500mg", unitPrice: 8.75 },
>   { id: 3, name: "Testosterone Cypionate", unitPrice: 85.00 },
> ];
>
> export default function OrderBuilder() {
>   const [state, dispatch] = useReducer(reducer, {
>     items: [], total: 0, status: "idle", error: null,
>   });
>
>   const handleSubmit = async () => {
>     dispatch({ type: "SUBMIT_START" });
>     try {
>       await fetch("/api/orders", {
>         method: "POST",
>         headers: { "Content-Type": "application/json" },
>         body: JSON.stringify({ items: state.items }),
>       });
>       dispatch({ type: "SUBMIT_SUCCESS" });
>     } catch {
>       dispatch({ type: "SUBMIT_ERROR", payload: "Failed to submit order" });
>     }
>   };
>
>   if (state.status === "success") {
>     return (
>       
>         Order submitted!
>          dispatch({ type: "RESET" })}>
>           New Order
>         
>       
>     );
>   }
>
>   return (
>     
>       Build Order
>
>        {
>         const med = MEDICATIONS.find(m => m.id === Number(e.target.value));
>         if (med) dispatch({ type: "ADD_ITEM", payload: med });
>         e.target.value = "";
>       }}>
>         Add medication...
>         {MEDICATIONS.map(m => (
>           {m.name} (${m.unitPrice})
>         ))}
>       
>
>       {state.items.map(item => (
>         
>           {item.name}
>                        type="number"
>             min={1}
>             value={item.quantity}
>             onChange={e => dispatch({
>               type: "SET_QUANTITY",
>               payload: { id: item.id, quantity: Number(e.target.value) },
>             })}
>             className="w-20 p-1 border rounded"
>           />
>           ${(item.unitPrice * item.quantity).toFixed(2)}
>            dispatch({ type: "REMOVE_ITEM", payload: item.id })}>
>             Remove
>           
>         
>       ))}
>
>       Total: ${state.total.toFixed(2)}
>
>       {state.error && {state.error}}
>
>                onClick={handleSubmit}
>         disabled={state.items.length === 0 || state.status === "submitting"}
>       >
>         {state.status === "submitting" ? "Submitting..." : "Submit Order"}
>       
>     
>   );
> }
> ```

---

## Exercise 3: Build a Context-Based Notification System
**Difficulty:** ⭐⭐⭐ | **Time:** 15 min | **Topics:** Context, custom hooks, TypeScript

### Problem

Build a notification system with:

1. A `NotificationProvider` that manages a list of notifications
2. A `useNotification` hook that exposes `addNotification` and `removeNotification`
3. A `NotificationBar` component that renders active notifications
4. Support three types: `success`, `error`, `info`
5. Auto-dismiss after 5 seconds

---

> [!success]- Solution (click to expand)
>
> ```tsx
> import { createContext, useContext, useState, useCallback, ReactNode } from "react";
>
> type NotificationType = "success" | "error" | "info";
>
> type Notification = {
>   id: string;
>   message: string;
>   type: NotificationType;
> };
>
> type NotificationContextType = {
>   notifications: Notification[];
>   addNotification: (message: string, type: NotificationType) => void;
>   removeNotification: (id: string) => void;
> };
>
> const NotificationContext = createContext(undefined);
>
> export function NotificationProvider({ children }: { children: ReactNode }) {
>   const [notifications, setNotifications] = useState([]);
>
>   const removeNotification = useCallback((id: string) => {
>     setNotifications(prev => prev.filter(n => n.id !== id));
>   }, []);
>
>   const addNotification = useCallback((message: string, type: NotificationType) => {
>     const id = crypto.randomUUID();
>     setNotifications(prev => [...prev, { id, message, type }]);
>
>     // Auto-dismiss after 5 seconds
>     setTimeout(() => removeNotification(id), 5000);
>   }, [removeNotification]);
>
>   return (
>            value={{ notifications, addNotification, removeNotification }}
>     >
>       {children}
>       
>     
>   );
> }
>
> export function useNotification() {
>   const context = useContext(NotificationContext);
>   if (!context) throw new Error("useNotification must be within NotificationProvider");
>   return context;
> }
>
> const colors = {
>   success: "bg-green-100 border-green-500 text-green-800",
>   error: "bg-red-100 border-red-500 text-red-800",
>   info: "bg-blue-100 border-blue-500 text-blue-800",
> };
>
> function NotificationBar() {
>   const { notifications, removeNotification } = useNotification();
>
>   return (
>     
>       {notifications.map(n => (
>                    key={n.id}
>           className={`p-3 border-l-4 rounded shadow ${colors[n.type]} flex justify-between`}
>         >
>           {n.message}
>            removeNotification(n.id)} className="ml-4">×
>         
>       ))}
>     
>   );
> }
>
>
> // === Usage in any component ===
> function OrderActions({ orderId }: { orderId: number }) {
>   const { addNotification } = useNotification();
>
>   const handleFulfill = async () => {
>     try {
>       await fetch(`/api/orders/${orderId}/fulfill`, { method: "POST" });
>       addNotification("Order fulfilled successfully!", "success");
>     } catch {
>       addNotification("Failed to fulfill order", "error");
>     }
>   };
>
>   return Fulfill Order;
> }
> ```

---

## Exercise 4: Performance Optimization
**Difficulty:** ⭐⭐⭐⭐ | **Time:** 20 min | **Topics:** React.memo, useMemo, useCallback, re-renders

### Problem

This component re-renders excessively. Identify all the performance issues and fix them:

```tsx
function Dashboard({ orders }: { orders: Order[] }) {
  const [filter, setFilter] = useState("all");

  const filteredOrders = orders.filter(o =>
    filter === "all" ? true : o.status === filter
  );

  const stats = {
    total: orders.length,
    pending: orders.filter(o => o.status === "Pending").length,
    revenue: orders.reduce((sum, o) => sum + o.total, 0),
  };

  const handleExport = () => {
    exportToCSV(filteredOrders);
  };

  return (
    
      
      
      
    
  );
}
```

Fix: (1) memoize `filteredOrders`, (2) memoize `stats`, (3) wrap `handleExport` in `useCallback`, (4) wrap child components in `React.memo`.

---

> [!success]- Solution (click to expand)
>
> ```tsx
> import { useState, useMemo, useCallback, memo } from "react";
>
> // Memoized child components — only re-render when their props change
> const StatsBar = memo(function StatsBar({ stats }: { stats: Stats }) {
>   return (
>     
>       Total: {stats.total}
>       Pending: {stats.pending}
>       Revenue: ${stats.revenue.toFixed(2)}
>     
>   );
> });
>
> const FilterButtons = memo(function FilterButtons({
>   filter, onChange
> }: {
>   filter: string; onChange: (f: string) => void
> }) {
>   return (
>     
>       {["all", "Pending", "Completed", "Cancelled"].map(f => (
>                    key={f}
>           onClick={() => onChange(f)}
>           className={filter === f ? "font-bold" : ""}
>         >
>           {f}
>         
>       ))}
>     
>   );
> });
>
> const OrderTable = memo(function OrderTable({
>   orders, onExport
> }: {
>   orders: Order[]; onExport: () => void
> }) {
>   return (
>     
>       Export CSV
>       {/* render orders */}
>     
>   );
> });
>
>
> // Parent component with memoized values
> function Dashboard({ orders }: { orders: Order[] }) {
>   const [filter, setFilter] = useState("all");
>
>   // useMemo: only recompute when orders or filter changes
>   const filteredOrders = useMemo(() =>
>     orders.filter(o => filter === "all" ? true : o.status === filter),
>     [orders, filter]
>   );
>
>   // useMemo: only recompute when orders changes (not on filter change)
>   const stats = useMemo(() => ({
>     total: orders.length,
>     pending: orders.filter(o => o.status === "Pending").length,
>     revenue: orders.reduce((sum, o) => sum + o.total, 0),
>   }), [orders]);
>
>   // useCallback: stable function reference for memoized child
>   const handleExport = useCallback(() => {
>     exportToCSV(filteredOrders);
>   }, [filteredOrders]);
>
>   return (
>     
>       
>       
>       
>     
>   );
> }
> ```
>
> **What changed:**
> - `filteredOrders`: `useMemo` — only refilters when `orders` or `filter` change
> - `stats`: `useMemo` — only recalculates when `orders` change, NOT when filter changes
> - `handleExport`: `useCallback` — stable reference so `OrderTable` doesn't re-render
> - Child components: `React.memo` — skip re-render if props haven't changed
> - `StatsBar` no longer re-renders when filter changes (its `stats` prop is stable)

---

## Exercise 5: Build a Form with Validation
**Difficulty:** ⭐⭐⭐ | **Time:** 20 min | **Topics:** Controlled inputs, validation, submit handling

### Problem

Build a `PatientRegistrationForm` with:

1. Fields: name, email, state (dropdown), phone (optional)
2. Validation: name required (2+ chars), email required + valid format, state required
3. Show field-level errors on blur (not on every keystroke)
4. Disable submit button until all required fields are valid
5. Show loading state during submission
6. TypeScript for all props and state

---

> [!success]- Solution (click to expand)
>
> ```tsx
> import { useState, FormEvent } from "react";
>
> type FormData = {
>   name: string;
>   email: string;
>   state: string;
>   phone: string;
> };
>
> type FormErrors = Partial>;
>
> const STATES = ["TX", "CA", "NY", "FL", "NJ"];
>
> function validate(data: FormData): FormErrors {
>   const errors: FormErrors = {};
>   if (data.name.length < 2) errors.name = "Name must be at least 2 characters";
>   if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(data.email)) errors.email = "Invalid email";
>   if (!data.state) errors.state = "State is required";
>   return errors;
> }
>
> export default function PatientRegistrationForm() {
>   const [form, setForm] = useState({
>     name: "", email: "", state: "", phone: "",
>   });
>   const [errors, setErrors] = useState({});
>   const [touched, setTouched] = useState>(new Set());
>   const [submitting, setSubmitting] = useState(false);
>
>   const updateField = (field: keyof FormData, value: string) => {
>     setForm(prev => ({ ...prev, [field]: value }));
>   };
>
>   const handleBlur = (field: keyof FormData) => {
>     setTouched(prev => new Set(prev).add(field));
>     const fieldErrors = validate(form);
>     setErrors(fieldErrors);
>   };
>
>   const allErrors = validate(form);
>   const isValid = Object.keys(allErrors).length === 0;
>
>   const handleSubmit = async (e: FormEvent) => {
>     e.preventDefault();
>     setTouched(new Set(["name", "email", "state"]));
>
>     if (!isValid) {
>       setErrors(allErrors);
>       return;
>     }
>
>     setSubmitting(true);
>     try {
>       await fetch("/api/patients", {
>         method: "POST",
>         headers: { "Content-Type": "application/json" },
>         body: JSON.stringify(form),
>       });
>       alert("Patient registered!");
>       setForm({ name: "", email: "", state: "", phone: "" });
>       setTouched(new Set());
>     } catch {
>       setErrors({ name: "Registration failed. Please try again." });
>     } finally {
>       setSubmitting(false);
>     }
>   };
>
>   const showError = (field: keyof FormData) =>
>     touched.has(field) && errors[field];
>
>   return (
>     
>       
>         Name *
>                    value={form.name}
>           onChange={e => updateField("name", e.target.value)}
>           onBlur={() => handleBlur("name")}
>           className="w-full p-2 border rounded"
>         />
>         {showError("name") && {errors.name}}
>       
>
>       
>         Email *
>                    type="email"
>           value={form.email}
>           onChange={e => updateField("email", e.target.value)}
>           onBlur={() => handleBlur("email")}
>           className="w-full p-2 border rounded"
>         />
>         {showError("email") && {errors.email}}
>       
>
>       
>         State *
>                    value={form.state}
>           onChange={e => updateField("state", e.target.value)}
>           onBlur={() => handleBlur("state")}
>           className="w-full p-2 border rounded"
>         >
>           Select state
>           {STATES.map(s => {s})}
>         
>         {showError("state") && {errors.state}}
>       
>
>       
>         Phone (optional)
>                    value={form.phone}
>           onChange={e => updateField("phone", e.target.value)}
>           className="w-full p-2 border rounded"
>         />
>       
>
>                type="submit"
>         disabled={!isValid || submitting}
>         className="px-6 py-2 bg-blue-600 text-white rounded disabled:opacity-50"
>       >
>         {submitting ? "Registering..." : "Register Patient"}
>       
>     
>   );
> }
> ```

---

## Recommended Practice Order

1. **Exercise 1** (Search + Debounce) — custom hooks + data fetching fundamentals
2. **Exercise 4** (Performance) — **most common senior interview question**
3. **Exercise 2** (useReducer) — complex state management
4. **Exercise 5** (Form + Validation) — practical, shows production patterns
5. **Exercise 3** (Context) — global state architecture

> [!tip] During the Interview
> When asked to build a component, start by saying: "Let me define the types first, then the state shape, then the render." This shows you think before you code. For performance questions, always explain the WHY: "I'd memoize this because the parent re-renders on filter change, but StatsBar's data hasn't changed."