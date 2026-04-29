==Next.js is a React framework that provides structure, features, and optimizations for building web applications.== Here are the core concepts with examples:

##### **1. File-Based Routing (App Router)** 
==Next.js uses the filesystem for routing. Each folder in `app/` becomes a route segment.==
```
app/
├── page.tsx          → /
├── about/
│   └── page.tsx      → /about
├── blog/
│   ├── page.tsx      → /blog
│   └── [slug]/
│       └── page.tsx  → /blog/my-post (dynamic)
```

```tsx
// app/blog/[slug]/page.tsx
export default function BlogPost({ params }: { params: { slug: string } }) {
  return <h1>Post: {params.slug}</h1>;
}
```

##### ==**2. Server Components vs Client Components**== 
==By default, all components in the App Router are **Server Components**.==
```tsx hl:1
// Server Component (default) — runs on the server, no JS sent to browser
export default async function Dashboard() {
  const data = await fetch('https://api.example.com/stats');
  const stats = await data.json();
  return <div>{stats.users} users</div>;
}
```

```tsx hl:1,2
// Client Component — add "use client" for interactivity
"use client";
import { useState } from "react";
export default function Counter() {
  const [count, setCount] = useState(0);
  return <button onClick={() => setCount(count + 1)}>Count: {count}</button>;
}
```

##### **3. Layouts** 
==Layouts wrap pages and persist across navigations (no re-render).==
```tsx hl:2,7
// app/layout.tsx — root layout (required)
export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>
        <nav>My App</nav>
        {children}
      </body>
    </html>
  );
}
```
==Nested layouts work too — `app/dashboard/layout.tsx` wraps all `/dashboard/*` pages.==

##### **4. Data Fetching** 
==**Server Components** can fetch data directly with `async/await`:==

```tsx hl:2,8,9
// app/users/page.tsx
async function getUsers() {
  const res = await fetch('https://jsonplaceholder.typicode.com/users', {
    next: { revalidate: 3600 }, // revalidate every hour
  });
  return res.json();
}
export default async function UsersPage() {
  const users = await getUsers();
  return (
    <ul>
      {users.map((u: any) => <li key={u.id}>{u.name}</li>)}
    </ul>
  );
}
```

##### ==**5. Server Actions**== 
==Handle form submissions and mutations without writing API routes:==
```tsx hl:3,4,10,5
// app/contact/page.tsx
export default function ContactForm() {
  async function submitForm(formData: FormData) {
    "use server";
    const name = formData.get("name");
    // save to database, send email, etc.
    console.log("Submitted:", name);
  }
  return (
    <form action={submitForm}>
      <input name="name" placeholder="Your name" />
      <button type="submit">Send</button>
    </form>
  );
}
```

##### **6. API Routes (Route Handlers)** 
Create backend endpoints inside the `app/` directory:
```tsx hl:1,2,3,6
// app/api/hello/route.ts
import { NextResponse } from "next/server";
export async function GET() {
  return NextResponse.json({ message: "Hello from the API" });
}
export async function POST(request: Request) {
  const body = await request.json();
  return NextResponse.json({ received: body });
}
```

##### **7. Loading & Error States** 
Next.js has built-in conventions for loading and error UI:
```tsx
// app/dashboard/loading.tsx — shown while page data loads
export default function Loading() {
  return <div>Loading dashboard...</div>;
}
```

```tsx
// app/dashboard/error.tsx — shown when an error occurs
"use client";
export default function Error({ error, reset }: { error: Error; reset: () => void }) {
  return (
    <div>
      <p>Something went wrong: {error.message}</p>
      <button onClick={reset}>Try again</button>
    </div>
  );
}
```

##### **8. Middleware** 
Run code before a request completes (auth checks, redirects, etc.):
```tsx
// middleware.ts (project root)
import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
export function middleware(request: NextRequest) {
  const token = request.cookies.get("auth_token");
  if (!token && request.nextUrl.pathname.startsWith("/dashboard")) {
    return NextResponse.redirect(new URL("/login", request.url));
  }
  return NextResponse.next();
}
export const config = { matcher: ["/dashboard/:path*"] };
```

##### **9. Image & Font Optimization** 
```tsx hl:1,9
import Image from "next/image";
export default function Hero() {
  return (
    <Image
      src="/hero.jpg"
      alt="Hero image"
      width={800}
      height={400}
      priority // loads eagerly for above-the-fold images
    />
  );
}
```

```tsx
// app/layout.tsx — automatic font optimization
import { Inter } from "next/font/google";
const inter = Inter({ subsets: ["latin"] });
export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html className={inter.className}>
      <body>{children}</body>
    </html>
  );
}
```
##### **10. Static vs Dynamic Rendering**

| Strategy             | When                                | How                                                      |
| -------------------- | ----------------------------------- | -------------------------------------------------------- |
| **Static (default)** | Content doesn’t change per request  | Pages pre-rendered at build time                         |
| **Dynamic**          | Personalized or real-time data      | Use `cookies()`, `headers()`, or `{ cache: 'no-store' }` |
| **ISR**              | Mostly static, updates periodically | `fetch(..., { next: { revalidate: 60 } })`               |
**Quick Start** 
```bash
npx create-next-app@latest my-app --typescript --app
cd my-app
npm run dev
```
This gives you the App Router, TypeScript, Tailwind CSS, and ESLint out of the box.

-------
#### Next.js Fundamentals & Medium-Level Concepts 

##### **1. File-Based Routing** 
Next.js uses the file system for routing. Files in `app/` (App Router) automatically become routes.

```
app/
├── page.tsx          → /
├── about/page.tsx    → /about
├── blog/[slug]/page.tsx → /blog/:slug
└── shop/[...slug]/page.tsx → /shop/* (catch-all)
```

```tsx
// app/blog/[slug]/page.tsx
export default function BlogPost({ params }: { params: { slug: string } }) {
  return <h1>Post: {params.slug}</h1>;
}
```
##### **2. Server Components vs Client Components** 
By default, all components in the App Router are **Server Components**. Add `"use client"` to opt into client-side interactivity.
```tsx
// Server Component (default) — runs on the server, no JS shipped to browser
export default async function UserList() {
  const users = await fetch('https://api.example.com/users').then(r => r.json());
  return <ul>{users.map(u => <li key={u.id}>{u.name}</li>)}</ul>;
}
```

```tsx
// Client Component — runs in the browser, supports hooks and events
"use client";
import { useState } from "react";
export default function Counter() {
  const [count, setCount] = useState(0);
  return <button onClick={() => setCount(c => c + 1)}>Count: {count}</button>;
}
```
##### **3. Layouts and Nested Layouts** 
Layouts wrap pages and persist across navigations. They don’t re-render when a child route changes.
```tsx
// app/layout.tsx — root layout (required)
export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>
        <nav>Site Nav</nav>
        {children}
      </body>
    </html>
  );
}
```

```tsx
// app/dashboard/layout.tsx — nested layout for /dashboard/*
export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex">
      <aside>Dashboard Sidebar</aside>
      <main>{children}</main>
    </div>
  );
}
```
##### **4. Data Fetching (Server Components)** 
==Fetch data directly in Server Components using `async/await`. Next.js extends `fetch` with caching and revalidation.==

```tsx hl:1,5,7,9
// Static data — cached indefinitely (default)
const data = await fetch('https://api.example.com/posts');
// Revalidate every 60 seconds (ISR)
const data = await fetch('https://api.example.com/posts', {
  next: { revalidate: 60 },
});
// No cache — always fresh
const data = await fetch('https://api.example.com/posts', {
  cache: 'no-store',
});
```
##### **5. Server Actions** 
Server Actions let you run server-side code from client components without building an API route.
```tsx hl:1,2,4
// app/actions.ts
"use server";
import { revalidatePath } from "next/cache";
export async function addTodo(formData: FormData) {
  const title = formData.get("title") as string;
  await db.todos.create({ data: { title } });
  revalidatePath("/todos");
}
```

```tsx hl:2,5
// app/todos/page.tsx
import { addTodo } from "../actions";
export default function Todos() {
  return (
    <form action={addTodo}>
      <input name="title" placeholder="New todo" />
      <button type="submit">Add</button>
    </form>
  );
}
```
##### **6. Loading & Error States** 
Special files `loading.tsx` and `error.tsx` handle UI states automatically per route segment.
```tsx
// app/dashboard/loading.tsx — shows while the page is loading
export default function Loading() {
  return <div className="spinner">Loading dashboard...</div>;
}
```

```tsx
// app/dashboard/error.tsx — catches errors in this segment
"use client";
export default function Error({ error, reset }: { error: Error; reset: () => void }) {
  return (
    <div>
      <h2>Something went wrong: {error.message}</h2>
      <button onClick={reset}>Try again</button>
    </div>
  );
}
```
##### **7. ==Middleware**== 
==Runs before a request is completed. Useful for auth, redirects, and header manipulation.==

```tsx
// middleware.ts (project root)
import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
export function middleware(request: NextRequest) {
  const token = request.cookies.get("auth-token");
  if (!token && request.nextUrl.pathname.startsWith("/dashboard")) {
    return NextResponse.redirect(new URL("/login", request.url));
  }
  return NextResponse.next();
}
export const config = {
  matcher: ["/dashboard/:path*"],
};
```
##### ==**8. API Routes (Route Handlers)**== 
==Create backend endpoints using `route.ts` files in the App Router.==
```tsx
// app/api/users/route.ts
import { NextResponse } from "next/server";
export async function GET() {
  const users = await db.users.findMany();
  return NextResponse.json(users);
}
export async function POST(request: Request) {
  const body = await request.json();
  const user = await db.users.create({ data: body });
  return NextResponse.json(user, { status: 201 });
}
```
##### **9. Dynamic Routes & Parallel Routes** 
**Dynamic segments** use brackets. **Parallel routes** render multiple pages simultaneously in the same layout.

```tsx
// Parallel routes — app/layout.tsx renders two slots at once
// app/@analytics/page.tsx and app/@dashboard/page.tsx
export default function Layout({
  children,
  analytics,
  dashboard,
}: {
  children: React.ReactNode;
  analytics: React.ReactNode;
  dashboard: React.ReactNode;
}) {
  return (
    <div>
      {children}
      <div className="grid grid-cols-2">
        {dashboard}
        {analytics}
      </div>
    </div>
  );
}
```
##### **10. Intercepting Routes** 
Intercept a route to show it in a modal while keeping the background page intact. Uses `(.)`, `(..)`, `(..)(..)`, or `(...)` conventions.
```
app/
├── feed/page.tsx
├── feed/@modal/(.)photo/[id]/page.tsx  → intercepts /photo/[id] when navigated from /feed
└── photo/[id]/page.tsx                 → direct URL still works as full page
```
##### **11. Image & Font Optimization** 
```tsx
import Image from "next/image";
import { Inter } from "next/font/google";
const inter = Inter({ subsets: ["latin"] });
export default function Home() {
  return (
    <div className={inter.className}>
      <Image
        src="/hero.jpg"
        alt="Hero"
        width={1200}
        height={600}
        priority  // preloads above-the-fold images
      />
    </div>
  );
}
```
##### **12. Metadata & SEO** 
```tsx
// app/layout.tsx — static metadata
import type { Metadata } from "next";
export const metadata: Metadata = {
  title: { default: "My App", template: "%s | My App" },
  description: "A Next.js application",
  openGraph: { images: ["/og.png"] },
};
```

```tsx
// app/blog/[slug]/page.tsx — dynamic metadata
export async function generateMetadata({ params }): Promise<Metadata> {
  const post = await getPost(params.slug);
  return { title: post.title, description: post.excerpt };
}
```
##### **13. Static Generation with** `generateStaticParams` 
Pre-render dynamic routes at build time.
```tsx
// app/blog/[slug]/page.tsx
export async function generateStaticParams() {
  const posts = await getAllPosts();
  return posts.map((post) => ({ slug: post.slug }));
}
export default async function BlogPost({ params }: { params: { slug: string } }) {
  const post = await getPost(params.slug);
  return <article><h1>{post.title}</h1><p>{post.content}</p></article>;
}
```
##### **14. Route Groups** 
Organize routes without affecting the URL structure using `(groupName)`.
```
app/
├── (marketing)/
│   ├── about/page.tsx    → /about
│   └── pricing/page.tsx  → /pricing
├── (app)/
│   ├── dashboard/page.tsx → /dashboard
│   └── settings/page.tsx  → /settings
```
Each group can have its own `layout.tsx`, so marketing pages and app pages use different layouts.
##### **15. Streaming & Suspense** 
Stream UI from the server using React Suspense for faster perceived load times.
```tsx
import { Suspense } from "react";
async function SlowComponent() {
  const data = await fetch("https://api.example.com/slow", { cache: "no-store" });
  const json = await data.json();
  return <div>{json.result}</div>;
}
export default function Page() {
  return (
    <div>
      <h1>Dashboard</h1>
      <Suspense fallback={<p>Loading analytics...</p>}>
        <SlowComponent />
      </Suspense>
    </div>
  );
}
```
##### **16. Environment Variables** 
```
# .env.local
DATABASE_URL=postgres://localhost:5432/mydb
NEXT_PUBLIC_API_URL=https://api.example.com  # exposed to browser
```
- ==Variables prefixed with `NEXT_PUBLIC_` are bundled into client-side code.==
- ==All other variables are server-only and never leaked to the browser.==
- ==Access with `process.env.DATABASE_URL` (server) or `process.env.NEXT_PUBLIC_API_URL` (client).==

##### **17. Caching Architecture (Summary)** 

|Mechanism|Where|Purpose|
|---|---|---|
|Request Memoization|Server|Deduplicates identical `fetch` calls in a single render|
|Data Cache|Server|Persists `fetch` results across requests and deploys|
|Full Route Cache|Server|Caches rendered HTML and RSC payload at build time|
|Router Cache|Client|Caches visited route segments for instant back/forward nav|
|Use `revalidatePath()` or `revalidateTag()` to invalidate caches on demand.|||