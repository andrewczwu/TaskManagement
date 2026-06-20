# Engineering Requirements Document — Task Management App

**Owner:** Andrew Wu
**Last updated:** 2026-06-20
**Status:** Active
**Companion doc:** [Product Requirements Document](./PRODUCT_REQUIREMENTS.md)

This document describes **how** the product requirements are implemented. Each
section ties back to the functional (FR-*) and non-functional (NFR-*) requirements
in the PRD. The guiding principle is **scope matched to the problem**: a small
task manager needs a small, flat architecture — no layers, patterns, or
infrastructure that the size of the problem doesn't justify.

---

## 1. Tech Stack

| Concern | Choice | Rationale |
|---|---|---|
| Backend | **ASP.NET Core Web API (C#, .NET 10 LTS)** — single project | Idiomatic, one deployable unit; right shape for a handful of CRUD endpoints. Current LTS. |
| Frontend | **React + TypeScript (Vite)** | Fast dev loop, type-safe contract with the API. |
| Database | **SQLite** via **EF Core** | Zero-setup, file-based persistence (NFR-1, NFR-7). |
| Auth | **ASP.NET Core Identity** (bearer tokens) | Framework-provided auth — not hand-rolled. In-process, shares the SQLite store. |
| Client token storage | **`localStorage` + `Authorization: Bearer`** | Simplest fully-finishable client auth (tradeoff in §10). |

---

## 2. Architecture (intentionally flat)

```
repo/
├── README.md
├── docs/
│   ├── PRODUCT_REQUIREMENTS.md
│   └── ENGINEERING_REQUIREMENTS.md
├── api/                        # ASP.NET Core Web API (single project)
│   ├── Program.cs              # DI, Identity, auth, middleware, endpoint wiring
│   ├── Models/                 # TaskItem entity, ApplicationUser
│   ├── Dtos/                   # request/response contracts
│   ├── Services/               # TaskService — business logic, validation, ownership
│   ├── Endpoints/              # Task endpoints (minimal API groups) + MapIdentityApi
│   └── Data/                   # AppDbContext + migrations + app.db (SQLite file)
├── api.Tests/                  # focused backend tests (ownership + validation)
└── web/                        # React + TS (Vite)
    ├── src/api/                # typed fetch client
    ├── src/auth/               # auth context, token storage, route guard
    ├── src/components/         # TaskList, TaskForm, TaskItem, etc.
    └── src/pages/              # Login, Register, Tasks
```

**Principles**
- **One backend project.** Thin endpoints delegate to a single **task service**
  that uses `AppDbContext` (EF Core) directly. No repository or unit-of-work
  abstraction wraps the context — there is exactly one DB implementation, so a
  wrapper adds complexity without value (supports NFR-8). The service is *not* a
  repository; it holds the small amount of task business logic (validation,
  ownership filtering, timestamps).
- **No CQRS / MediatR / mappers-for-one-shape.** Plain DTOs and EF Core.
- EF Core owns persistence and migrations. Ownership is enforced **in the query**
  (`WHERE UserId = currentUserId`), not just at the route.
- **Dependency injection for testability.** The task service depends on
  abstractions (`AppDbContext`, a current-user accessor, `ILogger`) resolved
  through ASP.NET Core's built-in DI container. This keeps endpoints thin and lets
  unit tests construct the service with a substitutable context and a fake user,
  with no HTTP host required (§9). DI here is justified by a real second use case —
  tests — not added speculatively.

---

## 3. Authentication & Authorization

### Approach
Use **ASP.NET Core Identity** with the built-in identity API endpoints
(`AddIdentityApiEndpoints<ApplicationUser>()` + `MapIdentityApi<ApplicationUser>()`),
configured for **bearer tokens**. This provides registration, login, token
issuance, and refresh out of the box, backed by the same EF Core / SQLite store.

- **Registration & login** are handled by the framework endpoints (FR-1.1, FR-1.2).
  Identity provides password hashing and password-strength rules — we never store
  or hash credentials ourselves (NFR-2).
- **Password policy (explicit, configured via `IdentityOptions.Password`).**
  Minimum **8 characters**, with at least **one uppercase letter, one lowercase
  letter, and one digit**. (Special character not required, to reduce friction.)
  This exact policy is **displayed in the sign-up UI** and drives the
  field-specific error messages (FR-1.6, see §7). The policy is defined in one
  place in config so the UI copy and the server rule cannot drift apart.
- **Token** is returned to the client on login and stored in `localStorage`; it is
  sent as `Authorization: Bearer <token>` on every API call (FR-1.4).
- **Token expiry.** Identity access tokens are short-lived (~1 hour). The client
  does **not** implement the refresh flow; instead, **any `401` clears the stored
  token and redirects to login**. This keeps the auth surface small and fully
  finishable; the refresh-token rotation alternative is noted in §10.
- **Sign out** (FR-1.3) clears the client token. (Bearer tokens are stateless;
  see §10 for the production note.)
- **Email confirmation is disabled** (`SignIn.RequireConfirmedAccount = false`,
  the default) so registration → login works immediately without an email sender,
  which is out of scope. This is pinned explicitly so a later config change can't
  silently break login.
- All task endpoints are decorated `[Authorize]`; unauthenticated requests get
  `401` (NFR-2), and the frontend redirects to login (FR-1.5).

### Ownership enforcement (FR-7, NFR-2) — the critical path
- Each `TaskItem` has a `UserId` foreign key to the Identity user.
- The owner is taken from the **authenticated principal** on every request
  (`User` → `ClaimTypes.NameIdentifier` / `UserManager.GetUserId(User)`), **never**
  from the request body or a client-supplied id.
- Every task query is filtered by the current user's id. A task that exists but
  belongs to someone else returns **`404`** (not `403`) so existence is not leaked.
- This is the single highest-risk area and is covered explicitly by tests (§9).

### Architectural Decision: Identity now, external IdP for production

> **Decision.** For this application we use **ASP.NET Core Identity** rather than an
> external identity provider (IdP).
>
> **Why, for this scope.** Identity is the framework's built-in, well-tested auth
> system (proper password hashing, token issuance) — so we are *not* rolling our
> own security primitives. It runs **in-process** and shares the **same SQLite
> store** as task data. This means user accounts persist across restarts with zero
> extra steps and can never drift out of sync with their tasks (one store, one
> source of truth), and it keeps reviewer setup to genuinely zero extra moving
> parts (NFR-7).
>
> **Why a real product would NOT own auth.** In production with real external
> users, we would delegate authentication to a dedicated **IdP** (e.g. Microsoft
> Entra ID, Auth0, or Firebase Auth). Identity (and authentication in
> general) is **security-critical surface area we should not want to own**:
> - **MFA / passwordless / social login** — TOTP, WebAuthn/passkeys, SMS, OAuth
>   providers — are large, evolving features that an IdP delivers and maintains for us.
> - **Threat response** — credential-stuffing defense, breach detection, anomalous-
>   login detection, rotation, and compliance (e.g. SOC 2 controls) are the IdP's
>   core competency, not ours.
> - **Operational burden** — token signing-key rotation, account recovery flows,
>   and audit logging are ongoing work better outsourced.
>
> Offloading this reduces our security blast radius and lets us focus on product.
> Because authn is isolated behind `[Authorize]` and a single "current user id"
> accessor, swapping Identity for an external IdP later is a contained change
> (validate the IdP's tokens; keep the same ownership model keyed on a stable
> subject claim).

---

## 4. Data Model

### ApplicationUser
Provided by ASP.NET Core Identity (`IdentityUser`), persisted to the standard
Identity tables (`AspNetUsers`, etc.): `Id` (string GUID, **PK**), `Email`
(unique), `PasswordHash`, and the rest of the Identity columns. No custom fields
are required for v1. It is the **one** side of the one-to-many relationship with
`TaskItem` (see below).

### TaskItem (the task-management persistence entity)
| Field | Type | Constraints / DB notes | Maps to |
|---|---|---|---|
| Id | GUID | **PK**, generated by the app on create (`Guid.NewGuid()`) | — |
| UserId | string | **FK → `AspNetUsers.Id`**, **required (non-null)**, **indexed** | FR-7 ownership key |
| Title | string | **required**, max length **200**, stored trimmed | FR-2.1, FR-8.1 |
| Description | string? | nullable, max length **2,000** | FR-2.2 |
| IsComplete | bool | **non-null, default `false`** | FR-5 |
| DueDate | DateTime? (UTC) | nullable; stored UTC ISO-8601 | FR-3.4, NFR-6 |
| CreatedAt | DateTime (UTC) | **non-null**, set once on create | — |
| UpdatedAt | DateTime (UTC) | **non-null**, set on every update | FR-4.3 |

**Relationship & integrity.**
- **One `ApplicationUser` has many `TaskItem`s; each `TaskItem` belongs to exactly
  one user** (1‑to‑many via `TaskItem.UserId`). Configured in `AppDbContext` with a
  required FK relationship.
- **Cascade delete:** deleting a user deletes all of their tasks, so no orphaned
  rows can survive (`OnDelete(DeleteBehavior.Cascade)`). There is no UI to delete a
  user in v1, but the constraint keeps the data model correct.
- **Index on `UserId`.** Every task query is filtered by the current user
  (`WHERE UserId = @currentUserId`), so `UserId` is indexed to keep those lookups
  efficient as the table grows. This is the only non-PK index v1 needs.
- **Tasks are never queried or returned without the `UserId` filter** — ownership
  (FR-7) is a property of the persistence layer, not just the API layer.

**Schema management.** The schema is created and evolved with **EF Core
migrations**, checked into the repo and applied automatically on startup (§11), so
a fresh clone gets the correct schema with no manual SQL.

**Timezone rule (NFR-6).** All instants are stored and compared in **UTC**. EF Core
persists `DateTime` to SQLite as ISO-8601 text; the API serializes UTC ISO-8601;
the frontend converts to/from the user's local timezone for display and input.

### Architectural Decision: SQLite now, production-grade DB later

> **Decision.** Persist data with **SQLite** rather than PostgreSQL or SQL Server.
>
> **Why, for this scope.** We are optimizing for **simplicity and ease of setup**.
> SQLite is a single file with no server to install, no container to run, no
> connection string to configure, and no credentials to manage. A reviewer can
> clone the repository and run the app immediately (NFR-7), and the data still
> **persists across restarts** because it lives in a file on disk (NFR-1). For a
> single-user task manager with a handful of CRUD endpoints, SQLite comfortably
> meets every functional and non-functional requirement — adding a database server
> would be setup cost and operational complexity the size of the problem does not
> justify (NFR-8).
>
> **Why a real product would use a production-grade database.** With concurrent
> users and write load, we would move to **PostgreSQL or SQL Server** for
> stronger concurrency (SQLite serializes writes), connection pooling, richer
> types and indexing, backups/replication, and managed hosting.
>
> **Why the switch is low-cost.** Because data access goes through **EF Core**, the
> provider is swappable: changing the database is largely a matter of swapping the
> provider package and connection string and regenerating migrations — the entity
> models, queries, and ownership logic stay the same. We deliberately avoid
> SQLite-specific SQL so the code stays portable.

---

## 5. API Contract

**Versioning.** All endpoints are served under a version prefix — **`/api/v1`** —
so the API can evolve without breaking existing clients. A future breaking change
ships as `/api/v2` while `/api/v1` keeps working, giving clients a window to
migrate. We start at **v1**.

Identity endpoints are mounted under `/api/v1/auth` via `MapIdentityApi`. All
`/api/v1/tasks/*` endpoints require a valid bearer token and operate **only** on
the authenticated user's tasks.

### Auth (provided by Identity)
| Method | Path | Body | Returns |
|---|---|---|---|
| POST | `/api/v1/auth/register` | `{ email, password }` | `200` (then login); `400` validation problem for weak password or **duplicate email** |
| POST | `/api/v1/auth/login` | `{ email, password }` | `200` + bearer token (+ refresh); `401` on bad credentials |

### Tasks (auth required, owner-scoped)
| Method | Path | Body | Returns |
|---|---|---|---|
| GET | `/api/v1/tasks` | — | `200` list of caller's tasks, **ordered** (see below) (FR-3.1) |
| GET | `/api/v1/tasks/{id}` | — | `200` task, or `404` if not caller's (FR-7) |
| POST | `/api/v1/tasks` | `{ title, description?, dueDate?, isComplete? }` | `201` created task (FR-2) |
| PUT | `/api/v1/tasks/{id}` | `{ title, description?, dueDate?, isComplete? }` | `200` updated task (FR-4, FR-5) |
| DELETE | `/api/v1/tasks/{id}` | — | `204` (FR-6) |

**`PUT` semantics.** `PUT` replaces the **editable fields** of the task: `title`
is required; `description` and `dueDate` are set to the supplied value or `null`
if omitted; `isComplete` is set to the supplied value (defaulting to the existing
value). The **complete/incomplete toggle (FR-5)** uses this same endpoint — the
client sends the task's current fields with `isComplete` flipped — so there is no
separate `PATCH` endpoint. `Id`, `UserId`, and `CreatedAt` are never client-settable.

**Default list order (FR-3.5).** `GET /tasks` returns **incomplete tasks first,
then by due date ascending (tasks with no due date last), then most recently
created first**. User-selectable sorting/filtering is out of scope for v1 (a
sensible default replaces it).

**Error shape (consistent).** `{ "error": "message", "details"?: {...} }` (or
ASP.NET `ProblemDetails`/validation problem for `400`s). Status codes:
`400` validation (including duplicate email, returned by Identity as a validation
problem), `401` unauthenticated / bad credentials, `404` not found / not owned.

---

## 6. Validation Rules (FR-8, NFR-2)

Authoritative validation runs **server-side, inside the task service** (guard
clauses that return a typed validation result the endpoint maps to a `400`
`ProblemDetails`). Placing it in the service — rather than only as DTO
annotations at the endpoint — is what makes the validation rules **unit-testable
without an HTTP host** (§9). The client mirrors these rules for UX only;
client-side is never the only gate. (Identity owns email/password validation for
the auth endpoints.)

| Field | Rule | On failure |
|---|---|---|
| Email | valid format, unique | `400` validation problem (handled by Identity) |
| Password | ≥8 chars, ≥1 uppercase, ≥1 lowercase, ≥1 digit (see §3) | `400` with **per-rule** messages (FR-1.6) |
| Title | required, non-empty after trim, ≤200 chars | `400`; **form keeps input** (FR-8.2) |
| Description | optional, ≤2,000 chars | `400` if over cap |
| DueDate | parseable date if provided (past dates **allowed** — overdue tasks are valid) | `400` if unparseable |
| IsComplete | boolean if provided | `400` |

---

## 7. Frontend Architecture (close every loop)

- **Auth context** holds the token (`localStorage`) and current-user state; a
  **route guard** redirects unauthenticated users to login (FR-1.5) and restores
  the session on refresh (FR-1.4).
- **Registration flow.** After a successful register, the client immediately logs
  the user in (register returns no token) and routes to the task list (FR-1.1/1.2).
- **Password requirements are shown (FR-1.6).** The sign-up form displays the
  policy from §3 ("at least 8 characters, with an uppercase letter, a lowercase
  letter, and a number") near the password field. On a `400`, the server's
  per-rule error messages are surfaced so the user sees exactly what to fix; input
  is preserved (FR-8.2).
- **Typed API client** attaches the bearer token, parses the consistent error
  shape, and surfaces messages to the UI. On any **`401` it clears the token and
  redirects to login** (token-expiry handling, see §3).
- **Dev networking.** The frontend calls the API as **same-origin `/api/...`**; the
  Vite dev server **proxies `/api` to the backend** (configured in `vite.config`).
  This avoids CORS entirely and needs no API-URL configuration (NFR-7).
- **State updates are immediate** after create/edit/delete/toggle — local state is
  updated (or the list refetched) so the UI reflects changes without a full reload
  (FR-2.3, FR-4.3, FR-5.2, FR-6.3, NFR-5).
- **Every async action** renders loading and error states; no silent failures
  (FR-8.3, NFR-4). Forms preserve user input on validation error (FR-8.2).
- **Dates** are entered/displayed in local time and converted to UTC on the wire
  (NFR-6).
- **Delete** uses a confirmation step (FR-6.2).

---

## 8. Error Handling & Logging (NFR-4)

- Server returns appropriate status codes with the consistent error body; no stack
  traces leak to clients.
- Unhandled exceptions are caught by a single **global exception-handling
  middleware** and returned as a generic `500` with a safe message.
- **Exception logging.** The exception middleware logs the full exception
  (message, stack trace, and request context) via the built-in `ILogger`
  abstraction before returning the safe `500`. Expected, handled error paths
  (validation `400`, not-found `404`, `401`) are not logged as errors. Logs go to
  the console (and the standard ASP.NET log providers), which is sufficient to
  diagnose failures during development and review.
- Client treats any non-2xx as a handled error path with a user-facing message.

### Architectural Decision: simple exception logging, no observability stack

> **Decision.** Implement **simple, structured exception logging** through the
> framework's `ILogger` (console output), and **do not** build observability or
> monitoring infrastructure.
>
> **Why, for this scope.** `ILogger`-based exception logging gives us what this
> application actually needs — a record of failures with enough context to debug
> them — at effectively zero cost, since it is built into ASP.NET Core. Full
> observability and monitoring (metrics, distributed tracing, log aggregation,
> dashboards, alerting, APM, error-tracking services) is meaningful
> infrastructure that is out of scope for this project. Adding it would be effort spent on scaffolding instead of finishing the product (NFR-8).
>
> **What production would add.** Structured logs shipped to a centralized
> aggregator, an error-tracking service (e.g. Sentry/Application Insights),
> request tracing/correlation Ids, metrics, and alerting on error rates. Because
> logging already goes through the `ILogger` abstraction, swapping the console
> provider for production sinks is a configuration change, not a code rewrite.

---

## 9. Testing Strategy (small, high-value)

Tests live in the `api.Tests` project (xUnit) and concentrate on the two
highest-risk areas (NFR-2, FR-7, FR-8) rather than chasing coverage. Two layers:

**Unit tests (task service).** Because the service is built around DI (§2), tests
construct it with a **SQLite in-memory** `AppDbContext` and a fake current-user
accessor — no HTTP host, no network — so each test is fast, isolated, and
deterministic. (SQLite in-memory is preferred over the EF InMemory provider
because it enforces real relational constraints.)

1. **Ownership enforcement**
   - User A cannot read / update / delete User B's task → not found.
   - Listing tasks returns only the caller's tasks.
2. **Validation**
   - Empty / whitespace-only title rejected.
   - Title over the max length rejected.
   - Invalid due date rejected.

**Integration tests (endpoint level).** A small number using
`WebApplicationFactory` to exercise the real HTTP pipeline + `[Authorize]`:
   - Unauthenticated request to a tasks endpoint → `401`.
   - (If time) one authenticated happy-path create→read.

No trivial "does a button render" tests.

---

## 10. Decisions & Tradeoffs

| Decision | Tradeoff / Production alternative |
|---|---|
| **SQLite** | Zero-setup and persistent. Provider-agnostic via EF Core, so production Postgres is a connection-string + provider swap, not a rewrite. |
| **ASP.NET Core Identity** | Right for this scope; production would delegate to an external IdP for MFA/passkeys/social and to offload security operations (see §3 decision). |
| **Token in `localStorage`** | Simple and finishable, but readable by JS → exposed to XSS. Production would use an httpOnly + `SameSite` cookie with CSRF protection, or rely on the IdP's session handling. |
| **Stateless bearer logout (client clears token)** | Simple; token remains valid until expiry. Production would use short-lived access tokens + refresh rotation / server-side revocation (typically the IdP's job). |
| **Flat single-project architecture** | Optimized for this scope and readability; a larger system would introduce module boundaries as real second use-cases appear. |
| **Simple `ILogger` exception logging** | Logs failures with context at zero cost; no observability/monitoring stack (see §8 decision). Production would ship structured logs to an aggregator + error tracking + alerting via the same abstraction. |

---

## 11. Setup & Run (zero extra moving parts)

Auth runs in-process, so there is **no separate auth service, no Java, no Docker,
no account, and no secrets** to configure. Target reviewer experience:

1. **Backend:** `dotnet run` from `api/` — applies EF Core migrations on startup
   and creates/uses the local `app.db` SQLite file. Runs on the **HTTP profile in
   dev** so there is no `dotnet dev-certs https --trust` step to block the browser.
2. **Frontend:** `npm install && npm run dev` from `web/` — the Vite dev server
   proxies `/api` to the backend (§7), so no CORS or API-URL setup is needed.

That's it — two processes, both standard for the stack. Exact commands and ports
will be documented in the root `README.md`. The SQLite file persists across
restarts (NFR-1) and is **git-ignored** (generated on first run), along with
`bin/`, `obj/`, `node_modules/`, and `dist/`.

---

## 12. Security

How the common web risks are addressed (NFR-2):

- **SQL injection.** All data access goes through **EF Core with LINQ** — queries
  are parameterized by the provider. There is **no raw SQL** (`FromSql`/
  `ExecuteSql`) anywhere, so user input is never concatenated into a query.
- **Cross-site scripting (XSS) / clean HTML output.** The frontend is **React**,
  which **escapes all interpolated values** by default; task titles and
  descriptions render as text, not markup. We use **no `dangerouslySetInnerHTML`,
  `innerHTML`, or `eval`**, so stored user input can't become executable HTML.
- **Input handling.** Validation is **server-authoritative** (§6): the title is
  required and trimmed, lengths are capped (title ≤200, description ≤2000), and the
  client mirrors these for UX only. Unknown JSON fields are ignored (the API binds
  to explicit DTOs), so clients can't set `Id`/`UserId`/timestamps.
- **Security headers (defense-in-depth).** Middleware sets, on every API response:
  `X-Content-Type-Options: nosniff` (no MIME-sniffing), `X-Frame-Options: DENY` and
  `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'` (no framing/
  clickjacking; safe because the API serves only JSON), and `Referrer-Policy:
  no-referrer`. Covered by a test.
- **Authentication & ownership.** Passwords are hashed by Identity; every task query
  is owner-scoped and a non-owned task returns `404` (§3), verified by tests (§9).
- **Known trade-off.** The bearer token is stored in `localStorage`, which is
  readable by JS and thus exposed to XSS (mitigated, not eliminated, by the
  escaping above). Production would move it to an `httpOnly` + `SameSite` cookie
  with CSRF protection, or use an external IdP's session (§10).
- **Production additions (out of scope here).** Enforce **HTTPS/HSTS** at the host,
  add a **CSP for the SPA** at the static-hosting layer (a strict CSP isn't applied
  in dev because Vite's HMR needs inline/eval), rate-limit auth endpoints, and add
  account lockout tuning.
