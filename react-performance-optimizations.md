Here are the most impactful client-side React performance optimizations:
###### **1. Memoization — Avoid Unnecessary Re-renders** 
==`React.memo` — Skip re-rendering when props haven’t changed:==
```jsx
const ExpensiveList = React.memo(({ items }) => {
  return items.map(item => <li key={item.id}>{item.name}</li>);
});
```

`useMemo` — Cache expensive computations:
```jsx
const sorted = useMemo(
  () => items.sort((a, b) => a.price - b.price),
  [items]
);
```

`useCallback` — Stabilize function references passed as props:
```jsx
const handleClick = useCallback((id) => {
  setSelected(id);
}, []);

return <ChildComponent onClick={handleClick} />;
```
###### **2. Code Splitting & Lazy Loading** 
Load components only when needed:
```jsx
const Dashboard = React.lazy(() => import('./Dashboard'));
function App() {
  return (
    <Suspense fallback={<Spinner />}>
      <Dashboard />
    </Suspense>
  );
}
```
Route-level splitting with React Router:
```jsx
const routes = [
  { path: '/settings', lazy: () => import('./pages/Settings') },
];
```

###### **3. Virtualize Long Lists** 
Render only visible items. Use `react-window` or `@tanstack/virtual`:
```jsx
import { FixedSizeList } from 'react-window';
<FixedSizeList height={400} itemCount={10000} itemSize={35} width="100%">
  {({ index, style }) => (
    <div style={style}>Row {index}</div>
  )}
</FixedSizeList>
```

###### **4. Avoid Prop Drilling — Use Context Wisely** 
Split contexts to prevent unrelated re-renders:
```jsx
// Bad: one giant context re-renders everything
const AppContext = createContext({ user, theme, cart });

// Good: separate contexts
const UserContext = createContext(user);
const ThemeContext = createContext(theme);
```
###### **5. Debounce Expensive Input Handlers** 
```jsx
import { useDeferredValue } from 'react';

function Search({ query }) {
  const deferredQuery = useDeferredValue(query);
  // Expensive filtering uses the deferred value
  const results = useMemo(() => filterItems(deferredQuery), [deferredQuery]);
  return <ResultsList results={results} />;
}
```

Or classic debounce for API calls:
```jsx
const debouncedSearch = useMemo(
  () => debounce((q) => fetchResults(q), 300),
  []
);
```
###### **6. Optimize Images** 
```jsx
// Use native lazy loading
<img src="photo.jpg" loading="lazy" alt="..." />

// Next.js Image component handles sizing, formats, lazy load
import Image from 'next/image';
<Image src="/photo.jpg" width={800} height={600} alt="..." />
```

###### **7. Key Prop Best Practices** 
```jsx
// Bad: index as key causes unnecessary DOM churn on reorder
items.map((item, i) => <Card key={i} {...item} />)

// Good: stable unique ID
items.map(item => <Card key={item.id} {...item} />)
```

###### **8. useTransition for Non-Urgent Updates** 
Keep the UI responsive while heavy state updates process in the background:

```jsx
const [isPending, startTransition] = useTransition();

function handleTabChange(tab) {
  startTransition(() => {
    setActiveTab(tab); // heavy render won't block input
  });
}
```

  ###### **9. Avoid Inline Object/Array Literals in JSX** 
```jsx
// Bad: new object every render → child always re-renders
<Child style={{ color: 'red' }} />

// Good: stable reference
const style = useMemo(() => ({ color: 'red' }), []);
<Child style={style} />
```
###### **10. Bundle Size Hygiene** 

- **Tree-shake** — import only what you need: `import { debounce } from 'lodash-es'` not `import _ from 'lodash'`
- **Analyze** — use `npx source-map-explorer` or `webpack-bundle-analyzer` to find bloat
- **Replace heavy libraries** — e.g. `date-fns` instead of `moment`, `zustand` instead of Redux for simple state

**Quick Decision Guide** 

|Problem|Fix|
|---|---|
|Child re-renders when parent updates|`React.memo` + `useCallback`|
|Expensive computation on every render|`useMemo`|
|Large bundle size|Code splitting + `React.lazy`|
|Long list (1000+ items)|Virtualization|
|Search/input lag|`useDeferredValue` or debounce|
|Heavy state update blocks UI|`useTransition`|
The biggest wins usually come from **virtualization**, **code splitting**, and **targeted memoization** — start there before micro-optimizing.