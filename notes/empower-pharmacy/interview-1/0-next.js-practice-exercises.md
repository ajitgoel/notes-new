## Empower Pharmacy Interview Prep

---

## Exercise 1: Build a Prescription List Page
**Difficulty:** ⭐⭐ | **Time:** 15 min | **Topics:** Server Components, data fetching, TypeScript

### Problem

Build a `/prescriptions` page that:

1. Fetches a list of prescriptions from an API (use `fetch` with `no-store`)
2. Displays them in a table with columns: ID, Patient, Medication, Status, Date
3. Color-codes the Status column: green for Active, yellow for Expiring, red for Expired
4. Add a proper `loading.tsx` skeleton
5. Add an `error.tsx` boundary

Define a TypeScript `Prescription` type.

---

> [!success]- Solution (click to expand)
>
> ```tsx
> // === types/prescription.ts ===
> export type Prescription = {
>   id: number;
>   patientName: string;
>   medicationName: string;
>   status: "Active" | "Expiring" | "Expired";
>   dosage: number;
>   createdAt: string;
> };
>
>
> // === app/prescriptions/page.tsx ===
> import { Prescription } from "@/types/prescription";
>
> async function getPrescriptions(): Promise<Prescription[]> {
>   const res = await fetch(`${process.env.API_URL}/prescriptions`, {
>     cache: "no-store",
>   });
>   if (!res.ok) throw new Error("Failed to fetch prescriptions");
>   return res.json();
> }
>
> const statusColors = {
>   Active: "text-green-700 bg-green-100",
>   Expiring: "text-yellow-700 bg-yellow-100",
>   Expired: "text-red-700 bg-red-100",
> };
>
> export default async function PrescriptionsPage() {
>   const prescriptions = await getPrescriptions();
>
>   return (
>     <div>
>       <h1 className="text-2xl font-bold mb-4">Prescriptions</h1>
>       <table className="w-full">
>         <thead>
>           <tr className="border-b text-left">
>             <th className="p-2">ID</th>
>             <th className="p-2">Patient</th>
>             <th className="p-2">Medication</th>
>             <th className="p-2">Status</th>
>             <th className="p-2">Date</th>
>           </tr>
>         </thead>
>         <tbody>
>           {prescriptions.map((rx) => (
>             <tr key={rx.id} className="border-b">
>               <td className="p-2">{rx.id}</td>
>               <td className="p-2">{rx.patientName}</td>
>               <td className="p-2">{rx.medicationName}</td>
>               <td className="p-2">
>                 <span className={`px-2 py-1 rounded text-sm ${statusColors[rx.status]}`}>
>                   {rx.status}
>                 </span>
>               </td>
>               <td className="p-2">
>                 {new Date(rx.createdAt).toLocaleDateString()}
>               </td>
>             </tr>
>           ))}
>         </tbody>
>       </table>
>     </div>
>   );
> }
>
>
> // === app/prescriptions/loading.tsx ===
> export default function Loading() {
>   return (
>     <div className="animate-pulse">
>       <div className="h-8 bg-gray-200 rounded w-48 mb-4" />
>       {[...Array(5)].map((_, i) => (
>         <div key={i} className="h-12 bg-gray-100 rounded mb-2" />
>       ))}
>     </div>
>   );
> }
>
>
> // === app/prescriptions/error.tsx ===
> "use client";
>
> export default function Error({
>   error,
>   reset,
> }: {
>   error: Error;
>   reset: () => void;
> }) {
>   return (
>     <div className="p-8 text-center">
>       <h2 className="text-xl font-bold text-red-600">Failed to load prescriptions</h2>
>       <p className="text-gray-600 mt-2">{error.message}</p>
>       <button
>         onClick={reset}
>         className="mt-4 px-4 py-2 bg-blue-600 text-white rounded"
>       >
>         Try again
>       </button>
>     </div>
>   );
> }
> ```

---

## Exercise 2: Server + Client Component Composition
**Difficulty:** ⭐⭐⭐ | **Time:** 20 min | **Topics:** Server/Client boundary, state, search

### Problem

Build a `/patients` page where:

1. A **Server Component** fetches all patients from the API
2. A **Client Component** `SearchBar` lets users type to filter the list
3. A **Client Component** `PatientCard` shows each patient with an "Archive" button
4. Clicking "Archive" calls a **Server Action** that updates the patient's status
5. After archiving, the page data refreshes

The key: pass server-fetched data DOWN to client components as props.

---

> [!success]- Solution (click to expand)
>
> ```tsx
> // === app/actions/patients.ts ===
> "use server";
>
> import { revalidatePath } from "next/cache";
>
> export async function archivePatient(id: number) {
>   await fetch(`${process.env.API_URL}/patients/${id}`, {
>     method: "PATCH",
>     headers: { "Content-Type": "application/json" },
>     body: JSON.stringify({ status: "Archived" }),
>   });
>
>   revalidatePath("/patients");
> }
>
>
> // === app/patients/search-bar.tsx ===
> "use client";
>
> import { useState } from "react";
>
> type Props = {
>   onSearch: (query: string) => void;
> };
>
> export default function SearchBar({ onSearch }: Props) {
>   const [query, setQuery] = useState("");
>
>   return (
>     <input
>       value={query}
>       onChange={(e) => {
>         setQuery(e.target.value);
>         onSearch(e.target.value);
>       }}
>       placeholder="Search patients..."
>       className="w-full p-3 border rounded mb-4"
>     />
>   );
> }
>
>
> // === app/patients/patient-list.tsx ===
> "use client";
>
> import { useState } from "react";
> import SearchBar from "./search-bar";
> import { archivePatient } from "@/app/actions/patients";
>
> type Patient = {
>   id: number;
>   name: string;
>   email: string;
>   state: string;
>   status: string;
> };
>
> export default function PatientList({ patients }: { patients: Patient[] }) {
>   const [filter, setFilter] = useState("");
>
>   const filtered = patients.filter((p) =>
>     p.name.toLowerCase().includes(filter.toLowerCase())
>   );
>
>   return (
>     <div>
>       <SearchBar onSearch={setFilter} />
>       <div className="grid grid-cols-2 gap-4">
>         {filtered.map((p) => (
>           <div key={p.id} className="border rounded p-4">
>             <h3 className="font-bold">{p.name}</h3>
>             <p className="text-sm text-gray-600">{p.email}</p>
>             <p className="text-sm">{p.state} — {p.status}</p>
>             {p.status !== "Archived" && (
>               <button
>                 onClick={() => archivePatient(p.id)}
>                 className="mt-2 text-sm text-red-600 underline"
>               >
>                 Archive
>               </button>
>             )}
>           </div>
>         ))}
>       </div>
>     </div>
>   );
> }
>
>
> // === app/patients/page.tsx (Server Component) ===
> import PatientList from "./patient-list";
>
> async function getPatients() {
>   const res = await fetch(`${process.env.API_URL}/patients`, {
>     cache: "no-store",
>   });
>   if (!res.ok) throw new Error("Failed to fetch patients");
>   return res.json();
> }
>
> export default async function PatientsPage() {
>   const patients = await getPatients();
>
>   return (
>     <div>
>       <h1 className="text-2xl font-bold mb-4">Patients</h1>
>       <PatientList patients={patients} />
>     </div>
>   );
> }
> ```

---

## Exercise 3: API Route with Validation
**Difficulty:** ⭐⭐⭐ | **Time:** 15 min | **Topics:** Route Handlers, validation, status codes

### Problem

Build API routes at `/api/medications` that handle:

1. `GET` — return all medications, with optional `?search=` query param
2. `POST` — create a medication with validation:
   - `name` required, 2-200 chars
   - `ndcCode` required, must match pattern `NDC-\d{3}`
   - `unitPrice` required, must be > 0
   - Return 400 with specific error messages if invalid
3. `GET /api/medications/[id]` — return single medication or 404
4. `PUT /api/medications/[id]` — update with same validation

---

> [!success]- Solution (click to expand)
>
> ```tsx
> // === app/api/medications/route.ts ===
> import { NextRequest, NextResponse } from "next/server";
>
> type MedicationInput = {
>   name?: string;
>   ndcCode?: string;
>   unitPrice?: number;
> };
>
> function validate(body: MedicationInput) {
>   const errors: Record<string, string> = {};
>
>   if (!body.name || body.name.length < 2 || body.name.length > 200)
>     errors.name = "Name is required (2-200 characters)";
>
>   if (!body.ndcCode || !/^NDC-\d{3}$/.test(body.ndcCode))
>     errors.ndcCode = "NDC code must match format NDC-XXX";
>
>   if (!body.unitPrice || body.unitPrice <= 0)
>     errors.unitPrice = "Unit price must be greater than 0";
>
>   return Object.keys(errors).length ? errors : null;
> }
>
> export async function GET(request: NextRequest) {
>   const search = request.nextUrl.searchParams.get("search");
>
>   const medications = await db.medications.findMany({
>     where: search
>       ? { name: { contains: search, mode: "insensitive" } }
>       : undefined,
>     orderBy: { name: "asc" },
>   });
>
>   return NextResponse.json(medications);
> }
>
> export async function POST(request: NextRequest) {
>   const body: MedicationInput = await request.json();
>   const errors = validate(body);
>
>   if (errors) {
>     return NextResponse.json({ errors }, { status: 400 });
>   }
>
>   const medication = await db.medications.create({
>     data: {
>       name: body.name!,
>       ndcCode: body.ndcCode!,
>       unitPrice: body.unitPrice!,
>     },
>   });
>
>   return NextResponse.json(medication, { status: 201 });
> }
>
>
> // === app/api/medications/[id]/route.ts ===
> import { NextRequest, NextResponse } from "next/server";
>
> type Params = { params: { id: string } };
>
> export async function GET(request: NextRequest, { params }: Params) {
>   const medication = await db.medications.findUnique({
>     where: { id: parseInt(params.id) },
>   });
>
>   if (!medication) {
>     return NextResponse.json({ error: "Not found" }, { status: 404 });
>   }
>
>   return NextResponse.json(medication);
> }
>
> export async function PUT(request: NextRequest, { params }: Params) {
>   const body = await request.json();
>   const errors = validate(body);
>
>   if (errors) {
>     return NextResponse.json({ errors }, { status: 400 });
>   }
>
>   try {
>     const medication = await db.medications.update({
>       where: { id: parseInt(params.id) },
>       data: body,
>     });
>     return NextResponse.json(medication);
>   } catch {
>     return NextResponse.json({ error: "Not found" }, { status: 404 });
>   }
> }
> ```

---

## Exercise 4: Middleware for Auth + Role-Based Access
**Difficulty:** ⭐⭐⭐⭐ | **Time:** 15 min | **Topics:** Middleware, auth, redirects

### Problem

Write a `middleware.ts` that:

1. Allows public access to `/`, `/login`, and `/api/health`
2. Redirects unauthenticated users to `/login` for all other routes
3. Checks for a role in the session cookie — if the user hits `/admin/*` without the `admin` role, redirect to `/unauthorized`
4. Adds an `X-Request-Id` header to all responses

---

> [!success]- Solution (click to expand)
>
> ```tsx
> // middleware.ts
> import { NextResponse } from "next/server";
> import type { NextRequest } from "next/server";
>
> const PUBLIC_PATHS = ["/", "/login", "/api/health"];
> const ADMIN_PATHS = ["/admin"];
>
> export function middleware(request: NextRequest) {
>   const { pathname } = request.nextUrl;
>
>   // Add request ID to all responses
>   const response = NextResponse.next();
>   response.headers.set("X-Request-Id", crypto.randomUUID());
>
>   // Allow public paths
>   if (PUBLIC_PATHS.some((p) => pathname === p || pathname.startsWith("/api/health"))) {
>     return response;
>   }
>
>   // Check authentication
>   const session = request.cookies.get("session-token")?.value;
>   if (!session) {
>     const loginUrl = new URL("/login", request.url);
>     loginUrl.searchParams.set("returnTo", pathname);
>     return NextResponse.redirect(loginUrl);
>   }
>
>   // Check admin role for admin paths
>   if (ADMIN_PATHS.some((p) => pathname.startsWith(p))) {
>     const role = request.cookies.get("user-role")?.value;
>     if (role !== "admin") {
>       return NextResponse.redirect(new URL("/unauthorized", request.url));
>     }
>   }
>
>   return response;
> }
>
> export const config = {
>   matcher: [
>     // Match all paths except static files and _next
>     "/((?!_next/static|_next/image|favicon.ico).*)",
>   ],
> };
> ```

---

## Exercise 5: Full Feature — Dashboard with Nested Layout
**Difficulty:** ⭐⭐⭐⭐ | **Time:** 25 min | **Topics:** Layouts, parallel data fetching, Suspense

### Problem

Build a `/dashboard` section with:

1. A **dashboard layout** with a persistent sidebar showing navigation links
2. `/dashboard` page showing summary stats (total patients, active prescriptions, revenue)
3. `/dashboard/recent` page showing the 10 most recent orders
4. Both pages fetch data independently as Server Components
5. Use `loading.tsx` for each page independently (streaming)
6. The sidebar stays mounted when switching between pages

---

> [!success]- Solution (click to expand)
>
> ```tsx
> // === app/dashboard/layout.tsx ===
> import Link from "next/link";
>
> export default function DashboardLayout({
>   children,
> }: {
>   children: React.ReactNode;
> }) {
>   return (
>     <div className="flex min-h-screen">
>       <aside className="w-64 border-r p-6">
>         <h2 className="font-bold text-lg mb-4">Dashboard</h2>
>         <nav className="space-y-2">
>           <Link href="/dashboard" className="block p-2 rounded hover:bg-gray-100">
>             Overview
>           </Link>
>           <Link href="/dashboard/recent" className="block p-2 rounded hover:bg-gray-100">
>             Recent Orders
>           </Link>
>         </nav>
>       </aside>
>       <main className="flex-1 p-8">{children}</main>
>     </div>
>   );
> }
>
>
> // === app/dashboard/page.tsx ===
> async function getStats() {
>   const [patients, prescriptions, revenue] = await Promise.all([
>     fetch(`${process.env.API_URL}/stats/patients`, { cache: "no-store" }).then(r => r.json()),
>     fetch(`${process.env.API_URL}/stats/prescriptions`, { cache: "no-store" }).then(r => r.json()),
>     fetch(`${process.env.API_URL}/stats/revenue`, { cache: "no-store" }).then(r => r.json()),
>   ]);
>   return { patients, prescriptions, revenue };
> }
>
> export default async function DashboardPage() {
>   const stats = await getStats();
>
>   return (
>     <div>
>       <h1 className="text-2xl font-bold mb-6">Overview</h1>
>       <div className="grid grid-cols-3 gap-6">
>         <StatCard title="Total Patients" value={stats.patients.total} />
>         <StatCard title="Active Rx" value={stats.prescriptions.active} />
>         <StatCard title="Monthly Revenue" value={`$${stats.revenue.monthly.toLocaleString()}`} />
>       </div>
>     </div>
>   );
> }
>
> function StatCard({ title, value }: { title: string; value: string | number }) {
>   return (
>     <div className="border rounded-lg p-6">
>       <p className="text-sm text-gray-500">{title}</p>
>       <p className="text-3xl font-bold mt-1">{value}</p>
>     </div>
>   );
> }
>
>
> // === app/dashboard/loading.tsx ===
> export default function Loading() {
>   return (
>     <div className="animate-pulse">
>       <div className="h-8 bg-gray-200 rounded w-32 mb-6" />
>       <div className="grid grid-cols-3 gap-6">
>         {[1, 2, 3].map((i) => (
>           <div key={i} className="h-24 bg-gray-100 rounded-lg" />
>         ))}
>       </div>
>     </div>
>   );
> }
>
>
> // === app/dashboard/recent/page.tsx ===
> async function getRecentOrders() {
>   const res = await fetch(`${process.env.API_URL}/orders?limit=10&sort=desc`, {
>     cache: "no-store",
>   });
>   return res.json();
> }
>
> export default async function RecentOrdersPage() {
>   const orders = await getRecentOrders();
>
>   return (
>     <div>
>       <h1 className="text-2xl font-bold mb-6">Recent Orders</h1>
>       <table className="w-full">
>         <thead>
>           <tr className="border-b text-left text-sm text-gray-500">
>             <th className="p-3">Order ID</th>
>             <th className="p-3">Patient</th>
>             <th className="p-3">Total</th>
>             <th className="p-3">Status</th>
>             <th className="p-3">Date</th>
>           </tr>
>         </thead>
>         <tbody>
>           {orders.map((order: any) => (
>             <tr key={order.id} className="border-b">
>               <td className="p-3">#{order.id}</td>
>               <td className="p-3">{order.patientName}</td>
>               <td className="p-3">${order.total.toFixed(2)}</td>
>               <td className="p-3">{order.status}</td>
>               <td className="p-3">{new Date(order.createdAt).toLocaleDateString()}</td>
>             </tr>
>           ))}
>         </tbody>
>       </table>
>     </div>
>   );
> }
>
>
> // === app/dashboard/recent/loading.tsx ===
> export default function Loading() {
>   return (
>     <div className="animate-pulse">
>       <div className="h-8 bg-gray-200 rounded w-48 mb-6" />
>       {[...Array(10)].map((_, i) => (
>         <div key={i} className="h-12 bg-gray-100 rounded mb-2" />
>       ))}
>     </div>
>   );
> }
> ```

---

## Recommended Practice Order

1. **Exercise 1** (Prescription list) — warm up with Server Components
2. **Exercise 2** (Server + Client composition) — **the #1 concept interviewers test**
3. **Exercise 3** (API routes) — full CRUD with validation
4. **Exercise 4** (Middleware) — auth and role-based access
5. **Exercise 5** (Dashboard) — shows you understand layouts and streaming

> [!tip] During the Interview
> Lead with: "I default to Server Components and only add `'use client'` for interactive pieces." This shows you understand the mental model. Then explain the data flow: "Server Component fetches data, passes it as props to a Client Component that handles the UI interactions."