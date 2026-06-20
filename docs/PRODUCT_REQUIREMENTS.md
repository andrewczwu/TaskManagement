# Product Requirements Document — Task Management App

**Owner:** Andrew Wu
**Last updated:** 2026-06-20
**Status:** Active

---

## 1. Overview

A simple, reliable **to-do task management web application** that lets a person
keep track of their own tasks. Each user has a private list of tasks they can
create, review, update, complete, and delete. The product's goal is to make
everyday task tracking simple and reliable. Tasks are never lost. They are never
shared by accident. The interface always shows the user what is happening.

This document describes **what** the product must do (functional requirements)
and the **qualities** it must have (non-functional requirements). Engineering
details — how these requirements are implemented — live in the
[Engineering Requirements Document](./ENGINEERING_REQUIREMENTS.md).

---

## 2. Users & Goals

| Persona | Goal |
|---|---|
| **Task owner** (primary, only user type) | Capture tasks quickly, see what's outstanding, mark things done, and trust that their list is private and persistent. |

There is no admin or multi-role concept. Every user is an equal, self-service
task owner who only ever interacts with their own data.

---

## 3. Functional Requirements

### FR-1 — Account & Authentication
- **FR-1.1** A new user can create an account.
- **FR-1.2** A returning user can sign in to access their tasks.
- **FR-1.3** A signed-in user can sign out.
- **FR-1.4** A user's session persists across page refreshes; the user is not
  forced to sign in again on every visit until they sign out or the session expires.
- **FR-1.5** Pages that show or modify tasks are accessible only to signed-in
  users; an unauthenticated visitor is directed to sign in.
- **FR-1.6** The sign-up form **clearly states the password requirements** up
  front, so the user knows what kind of password will be accepted before they
  submit. If a password is rejected, the message explains **which requirement was
  not met** (not just "invalid password").

### FR-2 — Task Creation
- **FR-2.1** A signed-in user can create a task with a **title** (required).
- **FR-2.2** A task may optionally include a **description** and a **due date**.
- **FR-2.3** A newly created task appears in the user's list **immediately**,
  without a manual page refresh.

### FR-3 — Viewing Tasks
- **FR-3.1** A user can view a list of all of their own tasks.
- **FR-3.2** The list clearly distinguishes **complete** from **incomplete** tasks.
- **FR-3.3** When the user has no tasks, the app shows a clear **empty state**
  rather than a blank screen.
- **FR-3.4** Due dates are displayed correctly in the **user's local time**,
  regardless of where the user or the server is located.
- **FR-3.5** The list has a **sensible default order** so it never looks random:
  outstanding (incomplete) tasks come first, the soonest-due tasks are surfaced
  near the top, and tasks without a due date fall to the bottom. (User-selectable
  sorting is a future consideration, §6.)

### FR-4 — Editing Tasks
- **FR-4.1** A user can edit an existing task's title, description, and due date.
- **FR-4.2** The edit experience is **pre-filled** with the task's current values.
- **FR-4.3** Saved changes appear in the list **immediately**.

### FR-5 — Completing Tasks
- **FR-5.1** A user can mark a task **complete**, and mark a complete task
  **incomplete** again.
- **FR-5.2** The change in status is reflected **immediately** in the list.

### FR-6 — Deleting Tasks
- **FR-6.1** A user can delete a task.
- **FR-6.2** Deletion requires a **confirmation** step to prevent accidents.
- **FR-6.3** A deleted task disappears from the list **immediately**.

### FR-7 — Data Ownership & Privacy
- **FR-7.1** A user can only ever see and act on **their own** tasks.
- **FR-7.2** No user can view, edit, complete, or delete another user's tasks
  through any part of the product.

### FR-8 — Input Validation & Feedback
- **FR-8.1** The app rejects invalid input — e.g. an empty/whitespace-only title,
  or a malformed due date — with a **clear, specific message**.
- **FR-8.2** When a submission fails validation, the user's **entered input is
  preserved**, not erased.
- **FR-8.3** Every action communicates its outcome: the user sees a **loading**
  indication while it is in progress and a **clear error message** if it fails —
  no silent failures.

---

## 4. Non-Functional Requirements

### NFR-1 — Persistence & Durability
Tasks and accounts are stored durably. Data **survives application restarts**;
nothing the user creates is lost when the app stops and starts again.

### NFR-2 — Security & Privacy
- Authentication is required for all task data.
- Ownership isolation (FR-7) is enforced **on the server**, not only hidden in the
  UI — the data boundary cannot be bypassed by crafting requests directly.
- Credentials are never stored in plain text.

### NFR-3 — Usability
- The interface is **clear and usable** over decorative. Every screen has an
  obvious primary action.
- All key states are designed for: loading, empty, success, and error.

### NFR-4 — Reliability & Error Handling
The app handles the unhappy path gracefully. Failures (network, validation,
not-found) produce a **user-facing response**, never a crash or a blank screen.

### NFR-5 — Responsiveness
Common actions (list, create, edit, complete, delete) feel **immediate** for a
realistic personal task list, and the UI updates without full-page reloads.

### NFR-6 — Correctness of Dates & Time
Due dates behave correctly **regardless of timezone** — what the user enters is
what they see, and dates are stored unambiguously.

### NFR-7 — Setup Simplicity
A developer can **clone the repository and run the app** by following the README,
with minimal prerequisites and no hidden manual steps.

### NFR-8 — Maintainability
The codebase is structured so a new developer can understand it quickly. Scope and
complexity are matched to the size of the problem.

---

## 5. Out of Scope (v1)

These are intentionally **not** part of the first version. They are reasonable
future additions but are excluded to keep the product focused and finishable.

- Sharing tasks or collaboration between users.
- Roles/permissions beyond a single self-service user type.
- Task categories, tags, labels, or projects.
- Reminders, notifications, or recurring tasks.
- Subtasks or task dependencies.
- Search, sort, and filter (see §6 — candidate for fast-follow).
- Mobile-native apps; the product is a web application.
- Offline support.

---

## 6. Future Considerations

Likely next steps once the core is solid:

- **User-selectable filter & sort** — by completion status and due date (v1 ships
  a fixed default order, FR-3.5).
- **Search** across task titles/descriptions.
- **Overdue highlighting** to surface what's urgent at a glance.
- **Categories/labels** for organizing larger lists.
- **Account management** — change password, delete account and all associated data.

---

## 7. Success Criteria

The v1 is "done" when every functional requirement works **end to end** — a user
can complete each flow from action to visible result — and every non-functional
requirement holds, most importantly: data persists across restarts, a user can
never reach another user's data, and invalid input is rejected with clear feedback.
