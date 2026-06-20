# Task Manager

A small full-stack to-do application: a .NET API and a React frontend where each
user signs in and manages their own private list of tasks.

- **Backend:** ASP.NET Core Web API (.NET 10), EF Core + SQLite, ASP.NET Core Identity
- **Frontend:** React + TypeScript (Vite)

Requirements live in [`docs/`](./docs): the
[Product Requirements](./docs/PRODUCT_REQUIREMENTS.md) (functional + non-functional),
the [Engineering Requirements](./docs/ENGINEERING_REQUIREMENTS.md) (architecture and
decisions), and [Test Cases](./docs/TEST_CASES.md).

---

## Prerequisites

- **.NET 10 SDK** — <https://dotnet.microsoft.com/download>
- **Node.js 20+** and npm

That's all — no database server, container, or external accounts to set up. SQLite
is a local file and is created automatically on first run.

---

## Running the app

The backend and frontend run as two processes. Start each in its own terminal.

**1. Backend** (from the repo root):

```bash
dotnet run --project api
```

- Serves on <http://localhost:5171>.
- Applies EF Core migrations on startup and creates `api/app.db` (SQLite) if needed.

**2. Frontend** (from the repo root):

```bash
cd web
npm install
npm run dev
```

- Serves on <http://localhost:5173> — **open this in your browser.**
- The Vite dev server proxies `/api` to the backend, so there is no CORS setup and
  no API URL to configure.

Then register an account and start adding tasks. Data persists across restarts
(it lives in `api/app.db`).

> Password requirements (shown on the sign-up form): at least 8 characters, with an
> uppercase letter, a lowercase letter, and a number.

### Running the tests

```bash
dotnet test
```

For manual end-to-end validation through the UI, see
[`docs/TEST_CASES.md`](./docs/TEST_CASES.md) — front-end test cases traced to each
product requirement.

---

## What's built

- **Accounts & auth** — register, sign in, sign out. The session is a bearer token
  kept in `localStorage`, so a refresh keeps you signed in. Routes that show tasks
  require authentication and redirect to sign-in otherwise.
- **Tasks** — full CRUD: create, list, edit, delete, and toggle complete. Each task
  has a title (required), optional description, optional due date, and a completion
  flag.
- **Ownership** — every task belongs to a user, enforced in the data-access query
  (not just the UI). One user can never see or modify another user's tasks; a task
  that isn't yours returns `404`.
- **Validation** — server-authoritative (empty/whitespace title, length limits),
  mirrored on the client. Invalid input is rejected with a clear message and the
  form keeps what you typed.
- **Due dates** — date-only, stored at UTC midnight and shown in UTC so the calendar
  day is stable across timezones; the input is bounded and validated. Past-due
  incomplete tasks show an **Overdue** badge.
- **Feedback** — every action has loading and error states; the list updates
  immediately after create/edit/delete/toggle without a page refresh.
- **Tests** — focused backend tests on the two highest-risk areas: ownership
  enforcement and input validation (service-level + a few endpoint integration
  tests, including a cross-user `404` and an unauthenticated `401`).

---

## Security

- **SQL injection** — all data access is EF Core/LINQ (parameterized); no raw SQL.
- **XSS / clean output** — React escapes all rendered values; no `dangerouslySetInnerHTML`,
  so stored input renders as text, never markup.
- **Input** — server-authoritative validation (required/trimmed title, length caps);
  the API binds to explicit DTOs, so clients can't set `Id`/`UserId`/timestamps.
- **Headers** — every API response sets `X-Content-Type-Options: nosniff`,
  `X-Frame-Options: DENY`, a strict `Content-Security-Policy`, and `Referrer-Policy`.
- **Auth/ownership** — Identity-hashed passwords; tasks are owner-scoped in the query
  (a non-owned task returns `404`), covered by tests.
- **Known trade-off** — the token lives in `localStorage` (XSS-exposed); production
  would use an `httpOnly` cookie or an external IdP (see below). HTTPS/HSTS and a
  SPA-level CSP belong at the production host.

## Key decisions and trade-offs

These are explained more fully in
[`docs/ENGINEERING_REQUIREMENTS.md`](./docs/ENGINEERING_REQUIREMENTS.md); the short
version:

- **Authentication uses ASP.NET Core Identity, not an external IdP.** Identity is
  the framework's built-in, well-tested auth (proper password hashing, token
  issuance) — so it isn't hand-rolled security — and it runs in-process against the
  same SQLite store, which keeps setup to zero extra steps. **A production app with
  real external users should not own auth**: I'd delegate to a dedicated identity
  provider (Entra ID, Auth0, Clerk, etc.) for MFA, passkeys/social login, and
  threat response, which are large, security-critical areas better outsourced.
  Auth is isolated behind `[Authorize]` and a single current-user accessor, so that
  swap is contained.
- **SQLite, not Postgres/SQL Server.** Chosen for true zero-setup and because it
  fully meets this app's needs while still persisting across restarts. Because
  access goes through EF Core, moving to a production database is a provider +
  connection-string change, not a rewrite.
- **Token in `localStorage`.** Simplest approach that's fully finished. The
  trade-off is XSS exposure; a production version would use an `httpOnly` +
  `SameSite` cookie with CSRF protection, or rely on the IdP's session handling.
- **Logging is simple `ILogger` exception logging**, not an observability stack —
  appropriate for this scope.

---

## Deliberately left out

- **No automated frontend tests.** The automated tests target the backend, where
  the highest-risk logic (ownership, validation) lives. The frontend was verified
  by exercising every flow in a browser. With more time I'd add component/E2E tests.
- **No password reset / email confirmation.** These need an email provider, which is
  out of scope. `MapIdentityApi` does expose those endpoints (forgot/reset password,
  confirm email, 2FA, refresh) since it's all-or-nothing, but **only register and
  login are used** by this app; the email-dependent ones are inert without an email
  sender.
- **No user-selectable sorting/filtering or search.** The list ships a sensible
  fixed order (incomplete first, then soonest due, then newest).
- Not built (and not asked for): CI/CD, containerization, deployment, monitoring.

---

## With another day

- Component and end-to-end tests for the frontend flows.
- Move the access token to an `httpOnly` cookie (or adopt an external IdP) to remove
  the `localStorage`/XSS trade-off, plus token refresh for longer sessions.
- User-selectable sorting, filtering, and search.
- Account management (change password, delete account and its data).

---

## Project layout

```
api/         ASP.NET Core Web API — Models, Dtos, Services, Endpoints, Data (+ migrations)
api.Tests/   xUnit tests (ownership + validation)
web/         React + TypeScript frontend (Vite)
docs/        Product & engineering requirements
```
