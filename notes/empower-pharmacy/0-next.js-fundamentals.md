Next.js is a React framework that provides structure, features, and optimizations for building web applications. Here are the core concepts with examples:

**1. File-Based Routing (App Router)** 

Next.js uses the filesystem for routing. Each folder in `app/` becomes a route segment.

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

**2. Server Components vs Client Components** 

By default, all components in the App Router are **Server Components**.

```tsx
// Server Component (default) — runs on the server, no JS sent to browser
export default async function Dashboard() {
  const data = await fetch('https://api.example.com/stats');
  const stats = await data.json();
  return <div>{stats.users} users</div>;
}
```


```tsx
// Client Component — add "use client" for interactivity
"use client";
import { useState } from "react";

export default function Counter() {
  const [count, setCount] = useState(0);
  return <button onClick={() => setCount(count + 1)}>Count: {count}</button>;
}
```

**3. Layouts** 

Layouts wrap pages and persist across navigations (no re-render).

```tsx
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

Nested layouts work too — `app/dashboard/layout.tsx` wraps all `/dashboard/*` pages.

**4. Data Fetching** 

**Server Components** can fetch data directly with `async/await`:

```tsx
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

  

**5. Server Actions** 

Handle form submissions and mutations without writing API routes:

```tsx
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

  

**6. API Routes (Route Handlers)** 

Create backend endpoints inside the `app/` directory:

```tsx
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

  

**7. Loading & Error States** 

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

  

**8. Middleware** 

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

  

**9. Image & Font Optimization** 

```tsx
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

  

**10. Static vs Dynamic Rendering** 

|Strategy|When|How|
|---|---|---|
|**Static (default)**|Content doesn’t change per request|Pages pre-rendered at build time|
|**Dynamic**|Personalized or real-time data|Use `cookies()`, `headers()`, or `{ cache: 'no-store' }`|
|**ISR**|Mostly static, updates periodically|`fetch(..., { next: { revalidate: 60 } })`|

  

**Quick Start** 

```bash
npx create-next-app@latest my-app --typescript --app
cd my-app
npm run dev
```

  

This gives you the App Router, TypeScript, Tailwind CSS, and ESLint out of the box.

Want me to go deeper on any of these topics — like authentication patterns, database integration, or deployment?