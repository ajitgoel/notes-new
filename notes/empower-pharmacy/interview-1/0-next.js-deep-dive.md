## App Router, Server Components, data fetching, and interview-ready patterns

> [!info] Context
> Empower Pharmacy's job listing includes **React.js and Next.js** in their stack. Dave has full-stack experience across the board. This covers Next.js 14/15 with the App Router — the modern approach.

---

## 1. App Router vs Pages Router

Next.js has two routing systems. The **App Router** (introduced in Next.js 13, stable in 14) is the modern approach and what you should default to in interviews.

```
app/                          ← App Router (modern)
├── layout.tsx                ← root layout (wraps all pages)
├── page.tsx                  ← home page (/)
├── prescriptions/
│   ├── page.tsx              ← /prescriptions
│   ├── [id]/
│   │   └── page.tsx          ← /prescriptions/123
│   └── loading.tsx           ← loading UI for this route
├── api/
│   └── orders/
│       └── route.ts          ← API route: /api/orders
└── error.tsx                 ← error boundary
```

| Feature | App Router | Pages Router |
|---|---|---|
| File location | `app/` directory | `pages/` directory |
| Default rendering | Server Components | Client Components |
| Data fetching | `async` components, `fetch()` | `getServerSideProps`, `getStaticProps` |
| Layouts | Nested layouts (preserved across navigation) | Per-page `_app.tsx` |
| Streaming | Built-in with `loading.tsx` and Suspense | Not supported |
| Status | **Current standard** | Legacy (still supported) |

---

## 2. Server Components vs Client Components

This is the single most important concept in modern Next.js.

### Server Components (default)

Every component in the App Router is a **Server Component** by default. They run on the server and send HTML to the browser — no JavaScript is shipped for them.

```tsx
// app/prescriptions/page.tsx
// This is a Server Component — runs on the server only
export default async function PrescriptionsPage() {
  // You can fetch data directly — no useEffect, no loading state
  const prescriptions = await fetch('https://api.empower.com/prescriptions', {
    headers: { Authorization: `Bearer ${getServerToken()}` }
  }).then(res => res.json());

  return (
    <div>
      <h1>Prescriptions</h1>
      <ul>
        {prescriptions.map((rx: Prescription) => (
          <li key={rx.id}>{rx.medicationName} — {rx.status}</li>
        ))}
      </ul>
    </div>
  );
}
```

**What Server Components CAN do:**
- Fetch data directly (async/await)
- Access backend resources (DB, file system, env vars)
- Keep sensitive logic server-side (API keys, queries)
- Reduce bundle size (no JS shipped to client)

**What Server Components CANNOT do:**
- Use `useState`, `useEffect`, or any React hooks
- Add event handlers (`onClick`, `onChange`)
- Access browser APIs (`window`, `document`, `localStorage`)

### Client Components

Add `"use client"` at the top of a file to make it a Client Component. These ship JavaScript to the browser and can use interactivity.

```tsx
// app/prescriptions/search-bar.tsx
"use client";

import { useState } from "react";

export default function SearchBar({ onSearch }: { onSearch: (q: string) => void }) {
  const [query, setQuery] = useState("");

  return (
    <input
      value={query}
      onChange={(e) => setQuery(e.target.value)}
      onKeyDown={(e) => e.key === "Enter" && onSearch(query)}
      placeholder="Search prescriptions..."
    />
  );
}
```

### The Pattern: Server Component wraps Client Component

```tsx
// app/prescriptions/page.tsx (Server Component)
import SearchBar from "./search-bar";      // Client Component
import PrescriptionList from "./list";      // Server Component

export default async function PrescriptionsPage() {
  const prescriptions = await fetchPrescriptions();  // server-side fetch

  return (
    <div>
      <SearchBar onSearch={filterPrescriptions} />  {/* interactive */}
      <PrescriptionList data={prescriptions} />      {/* static HTML */}
    </div>
  );
}
```

> [!tip] Interview rule of thumb
> "Start with Server Components. Only add `'use client'` when you need interactivity (state, effects, event handlers). Keep Client Components as small and leaf-level as possible."

---

## 3. Data Fetching

### In Server Components (the primary way)

```tsx
// Fetching in a Server Component — no useEffect needed
async function getPatient(id: string) {
  const res = await fetch(`https://api.empower.com/patients/${id}`, {
    cache: "no-store",  // always fresh (SSR)
    // cache: "force-cache",  // cached until revalidated (SSG)
    // next: { revalidate: 60 },  // revalidate every 60 seconds (ISR)
  });

  if (!res.ok) throw new Error("Failed to fetch patient");
  return res.json();
}

export default async function PatientPage({ params }: { params: { id: string } }) {
  const patient = await getPatient(params.id);
  return <PatientProfile patient={patient} />;
}
```

### Caching strategies

| Strategy | Config | When data updates | Use for |
|---|---|---|---|
| **SSR** (Server-Side) | `cache: "no-store"` | Every request | User-specific data, real-time |
| **SSG** (Static) | `cache: "force-cache"` | At build time only | Marketing pages, docs |
| **ISR** (Incremental) | `next: { revalidate: 60 }` | Every N seconds | Product listings, dashboards |

### In Client Components (when needed)

```tsx
"use client";

import { useEffect, useState } from "react";

export default function LiveOrderCount() {
  const [count, setCount] = useState<number | null>(null);

  useEffect(() => {
    const interval = setInterval(async () => {
      const res = await fetch("/api/orders/count");
      const data = await res.json();
      setCount(data.count);
    }, 5000);

    return () => clearInterval(interval);
  }, []);

  return <span>{count ?? "..."} active orders</span>;
}
```

---

## 4. Layouts & Templates

### Root Layout (required)

```tsx
// app/layout.tsx — wraps EVERY page
export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>
        <nav>{/* persistent navigation */}</nav>
        <main>{children}</main>
        <footer>{/* persistent footer */}</footer>
      </body>
    </html>
  );
}
```

### Nested Layouts (preserved across navigation)

```tsx
// app/dashboard/layout.tsx — wraps all /dashboard/* pages
export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex">
      <Sidebar />             {/* stays mounted during navigation */}
      <div className="flex-1">
        {children}              {/* this part swaps */}
      </div>
    </div>
  );
}
```

> [!tip] Key insight
> Layouts don't re-render when navigating between sibling pages. The sidebar stays mounted, only the `{children}` swaps. This preserves state (scroll position, form inputs) automatically.

---

## 5. Route Handlers (API Routes)

```tsx
// app/api/prescriptions/route.ts
import { NextRequest, NextResponse } from "next/server";

export async function GET(request: NextRequest) {
  const searchParams = request.nextUrl.searchParams;
  const status = searchParams.get("status");

  const prescriptions = await db.prescriptions.findMany({
    where: status ? { status } : undefined,
  });

  return NextResponse.json(prescriptions);
}

export async function POST(request: NextRequest) {
  const body = await request.json();

  // Validate
  if (!body.patientId || !body.medicationName) {
    return NextResponse.json(
      { error: "patientId and medicationName are required" },
      { status: 400 }
    );
  }

  const prescription = await db.prescriptions.create({ data: body });

  return NextResponse.json(prescription, { status: 201 });
}
```

### Dynamic route handler

```tsx
// app/api/prescriptions/[id]/route.ts
export async function GET(
  request: NextRequest,
  { params }: { params: { id: string } }
) {
  const prescription = await db.prescriptions.findUnique({
    where: { id: parseInt(params.id) },
  });

  if (!prescription) {
    return NextResponse.json({ error: "Not found" }, { status: 404 });
  }

  return NextResponse.json(prescription);
}

export async function DELETE(
  request: NextRequest,
  { params }: { params: { id: string } }
) {
  await db.prescriptions.delete({ where: { id: parseInt(params.id) } });
  return new NextResponse(null, { status: 204 });
}
```

---

## 6. Loading, Error, and Not Found

### loading.tsx (automatic Suspense boundary)

```tsx
// app/prescriptions/loading.tsx
export default function Loading() {
  return (
    <div className="animate-pulse">
      <div className="h-8 bg-gray-200 rounded w-1/3 mb-4" />
      <div className="h-4 bg-gray-200 rounded w-full mb-2" />
      <div className="h-4 bg-gray-200 rounded w-full mb-2" />
      <div className="h-4 bg-gray-200 rounded w-2/3" />
    </div>
  );
}
// Shown instantly while page.tsx is fetching data
```

### error.tsx (error boundary)

```tsx
// app/prescriptions/error.tsx
"use client";  // error boundaries MUST be client components

export default function Error({
  error,
  reset,
}: {
  error: Error;
  reset: () => void;
}) {
  return (
    <div>
      <h2>Something went wrong</h2>
      <p>{error.message}</p>
      <button onClick={reset}>Try again</button>
    </div>
  );
}
```

### not-found.tsx

```tsx
// app/prescriptions/[id]/not-found.tsx
export default function NotFound() {
  return (
    <div>
      <h2>Prescription Not Found</h2>
      <p>The prescription you're looking for doesn't exist.</p>
    </div>
  );
}

// Triggered from page.tsx:
import { notFound } from "next/navigation";

export default async function PrescriptionPage({ params }: Props) {
  const rx = await getPrescription(params.id);
  if (!rx) notFound();  // renders not-found.tsx
  return <PrescriptionDetail rx={rx} />;
}
```

---

## 7. Server Actions

Server Actions let you run server-side code from a form submit or button click — no API route needed.

```tsx
// app/prescriptions/new/page.tsx
export default function NewPrescriptionPage() {
  async function createPrescription(formData: FormData) {
    "use server";  // this function runs on the server

    const data = {
      patientId: parseInt(formData.get("patientId") as string),
      medicationName: formData.get("medicationName") as string,
      dosage: parseFloat(formData.get("dosage") as string),
    };

    await db.prescriptions.create({ data });
    redirect("/prescriptions");  // redirect after creation
  }

  return (
    <form action={createPrescription}>
      <input name="patientId" type="number" required />
      <input name="medicationName" required />
      <input name="dosage" type="number" step="0.1" required />
      <button type="submit">Create Prescription</button>
    </form>
  );
}
```

### Server Action in a separate file (reusable)

```tsx
// app/actions/prescriptions.ts
"use server";

import { revalidatePath } from "next/cache";

export async function updateStatus(id: number, status: string) {
  await db.prescriptions.update({
    where: { id },
    data: { status },
  });

  revalidatePath("/prescriptions");  // refresh cached data
}
```

```tsx
// app/prescriptions/[id]/status-button.tsx
"use client";

import { updateStatus } from "@/app/actions/prescriptions";

export default function StatusButton({ id }: { id: number }) {
  return (
    <button onClick={() => updateStatus(id, "Completed")}>
      Mark Complete
    </button>
  );
}
```

---

## 8. Middleware

Runs BEFORE every request. Used for auth, redirects, headers.

```tsx
// middleware.ts (root of project)
import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

export function middleware(request: NextRequest) {
  // Check auth token
  const token = request.cookies.get("session-token");

  if (!token && request.nextUrl.pathname.startsWith("/dashboard")) {
    return NextResponse.redirect(new URL("/login", request.url));
  }

  // Add correlation ID header
  const response = NextResponse.next();
  response.headers.set("X-Correlation-Id", crypto.randomUUID());

  return response;
}

// Only run on specific paths
export const config = {
  matcher: ["/dashboard/:path*", "/api/:path*"],
};
```

---

## 9. Environment Variables

```bash
# .env.local
DATABASE_URL=postgresql://...
API_SECRET_KEY=sk-...                  # server only (no NEXT_PUBLIC_ prefix)
NEXT_PUBLIC_API_URL=https://api.empower.com  # exposed to browser
```

```tsx
// Server Component — can access all env vars
const dbUrl = process.env.DATABASE_URL;       // ✓ works
const apiKey = process.env.API_SECRET_KEY;    // ✓ works

// Client Component — only NEXT_PUBLIC_ vars
const apiUrl = process.env.NEXT_PUBLIC_API_URL;  // ✓ works
const secret = process.env.API_SECRET_KEY;       // ✗ undefined
```

> [!warning] Security
> Never put secrets in `NEXT_PUBLIC_` variables — they're embedded in the JavaScript bundle and visible to anyone. API keys, DB URLs, and auth secrets should only be accessed in Server Components, Route Handlers, or Server Actions.

---

## 10. Quick-Fire Interview Q&A

### "When would you use a Client Component vs Server Component?"
Server Component by default — it's lighter, more secure, and fetches data without loading states. Switch to Client only when you need interactivity: `useState`, `useEffect`, event handlers, or browser APIs.
### "What's the difference between `cache: 'no-store'` and `revalidate`?"
`no-store` fetches fresh data on every request (SSR). `revalidate: 60` caches the result and re-fetches at most every 60 seconds (ISR). Use `no-store` for user-specific data, `revalidate` for shared data that changes occasionally.
### "How do layouts work in the App Router?"
Layouts wrap their child routes and **don't re-render** when navigating between sibling pages. State is preserved. The root layout is required and wraps everything. Nested layouts are optional per route segment.
### "What are Server Actions?"
Functions marked with `"use server"` that run on the server but can be called from Client Components (via form `action` or `onClick`). They replace simple API routes for mutations. Use `revalidatePath` or `revalidateTag` to refresh cached data after a mutation.
### "How does `loading.tsx` work?"
Next.js wraps each route segment in a React Suspense boundary. `loading.tsx` is the fallback UI shown while the `page.tsx` is fetching data. It renders instantly — no spinner-on-the-whole-page delay. You get streaming HTML out of the box.
### "How do you handle auth in Next.js?"
Middleware for route protection (redirect unauthenticated users), Server Components for reading the session, and Client Components for login forms. Libraries like NextAuth.js/Auth.js handle the heavy lifting. Never check auth only on the client — always verify server-side.