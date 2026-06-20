# Frontend Test Cases

Manual test cases to validate the product requirements
([`PRODUCT_REQUIREMENTS.md`](./PRODUCT_REQUIREMENTS.md)) through the UI. Each case
traces to the functional (FR-*) / non-functional (NFR-*) requirements it covers.

These are end-to-end checks a person runs in a browser. Automated tests cover the
backend's highest-risk logic (ownership, validation) — see `api.Tests/`.

---

## Setup

1. Start the backend: `dotnet run --project api` (serves on `http://localhost:5171`).
2. Start the frontend: `cd web && npm install && npm run dev` (open `http://localhost:5173`).
3. Use a fresh database for a clean run: stop the backend and delete `api/app.db*`
   before starting, so no prior accounts/tasks exist.

**Test accounts** (register them as part of TC-AUTH-01 / ownership cases):

| Alias  | Email               | Password    |
| ------ | ------------------- | ----------- |
| User A | `alice@example.com` | `Secret123` |
| User B | `bob@example.com`   | `Secret123` |

A valid password meets the policy: ≥8 chars, with an uppercase letter, a lowercase
letter, and a number.

---

## 1. Account & Authentication (FR-1)

| ID | Requirement | Steps | Expected result |
|---|---|---|---|
| TC-AUTH-01 | FR-1.1 | On `/register`, enter `alice@example.com` + `Secret123`, submit | Account created; you are signed in and land on **My Tasks**. |
| TC-AUTH-02 | FR-1.6 | View the register form before submitting | Password requirements are clearly shown near the password field. |
| TC-AUTH-03 | FR-1.6, FR-8.1 | On `/register`, enter a weak password (e.g. `abc`), submit | Rejected with specific messages naming the unmet rules (too short, needs a digit, needs uppercase); the email you typed is preserved. |
| TC-AUTH-04 | FR-1.1, FR-8 | Register again with `alice@example.com` | Rejected with a clear "already taken" message; no duplicate account. |
| TC-AUTH-05 | FR-1.3 | While signed in, click **Sign out** | Returned to the sign-in page. |
| TC-AUTH-06 | FR-1.2 | On `/login`, enter `alice@example.com` + `Secret123`, submit | Signed in; land on **My Tasks**. |
| TC-AUTH-07 | FR-1.2, FR-8.3 | On `/login`, enter a wrong password, submit | Clear "invalid email or password" message; stays on login; input preserved. |
| TC-AUTH-08 | FR-1.4 | While signed in on My Tasks, refresh the browser | Still signed in; tasks still shown (no forced re-login). |
| TC-AUTH-09 | FR-1.5 | While signed out, navigate directly to `/` | Redirected to `/login`. |

---

## 2. Creating Tasks (FR-2, FR-8)

| ID | Requirement | Steps | Expected result |
|---|---|---|---|
| TC-CREATE-01 | FR-2.1, FR-2.3 | Signed in, enter a title only ("Buy milk"), click **Add task** | Task appears in the list **immediately**, no page refresh. |
| TC-CREATE-02 | FR-2.2 | Create a task with a description and a due date | Task shows the description and the due **date** (date-only, no time). |
| TC-CREATE-03 | FR-8.1, FR-8.2 | Leave the title empty (or only spaces) and submit | Rejected with a clear "title is required" message; the description/due date you entered are preserved. |
| TC-CREATE-04 | FR-8.1 | Enter a title longer than 200 characters and submit | Rejected with a clear length message. |
| TC-CREATE-05 | FR-8.2 | After a successful create | The form clears, ready for the next task. |
| TC-CREATE-06 | FR-8.1 | In Due date, try a 6-digit / out-of-range year, submit | Rejected (input is bounded and a clear "valid due date" message shows); nothing is saved. |
| TC-CREATE-07 | FR-1.6, FR-8 | Read the form | Title is marked required (`*`); Description and Due date are marked optional. |

---

## 3. Viewing Tasks (FR-3)

| ID | Requirement | Steps | Expected result |
|---|---|---|---|
| TC-VIEW-01 | FR-3.3 | Sign in as a brand-new user with no tasks | A clear empty state is shown (not a blank screen). |
| TC-VIEW-02 | FR-3.1 | Create a few tasks | All of your tasks are listed. |
| TC-VIEW-03 | FR-3.2 | Mark one task complete | Complete and incomplete tasks are visually distinct (e.g. checkbox + strikethrough). |
| TC-VIEW-04 | FR-3.5 | Create tasks with different due dates, some complete | Order is: incomplete first, then soonest due, with no-due-date tasks last. |
| TC-VIEW-05 | FR-3.4, NFR-6 | Create a task with a due date, then view it (optionally change your machine's timezone and reload) | The due **date** displays as the same calendar day regardless of timezone (no day shift). |

---

## 4. Editing Tasks (FR-4)

| ID | Requirement | Steps | Expected result |
|---|---|---|---|
| TC-EDIT-01 | FR-4.2 | Click **Edit** on a task | The form is pre-filled with the task's current title, description, and due date. |
| TC-EDIT-02 | FR-4.1, FR-4.3 | Change the title and save | The change appears in the list immediately. |
| TC-EDIT-03 | FR-4.1 | Edit a task and clear its description/due date, save | Description/due date are removed; other fields unchanged. |
| TC-EDIT-04 | FR-8.1, FR-8.2 | Edit a task, clear the title, save | Rejected with a clear message; your other edits are preserved. |
| TC-EDIT-05 | FR-4 | Click Edit, then **Cancel** | Edit is discarded; the task is unchanged. |

---

## 5. Completing Tasks (FR-5)

| ID | Requirement | Steps | Expected result |
|---|---|---|---|
| TC-DONE-01 | FR-5.1, FR-5.2 | Toggle a task's checkbox to complete | Marked complete (strikethrough) immediately. |
| TC-DONE-02 | FR-5.1 | Toggle a completed task back to incomplete | Returns to incomplete immediately. |
| TC-DONE-03 | FR-5, data integrity | Toggle complete on a task that has a description and due date | Description and due date are **preserved** (not wiped) by the toggle. |

---

## 6. Deleting Tasks (FR-6)

| ID | Requirement | Steps | Expected result |
|---|---|---|---|
| TC-DEL-01 | FR-6.2 | Click **Delete** on a task | A confirmation prompt appears (the task is not deleted yet). |
| TC-DEL-02 | FR-6.2 | Click **No / Cancel** on the confirmation | The task remains. |
| TC-DEL-03 | FR-6.1, FR-6.3 | Click **Delete**, then confirm | The task disappears from the list immediately. |
| TC-DEL-04 | FR-6.3, FR-3.3 | Delete the last remaining task | The empty state is shown again. |

---

## 7. Data Ownership & Privacy (FR-7, NFR-2)

| ID | Requirement | Steps | Expected result |
|---|---|---|---|
| TC-OWN-01 | FR-7.1 | As User A, create a task. Sign out. Register/sign in as User B | User B sees an empty list (none of User A's tasks). |
| TC-OWN-02 | FR-7.2, NFR-2 | As User B, copy a task `id` from User A's session and request `GET /api/v1/tasks/{id}` directly (browser devtools/curl with User B's token) | Returns `404` — User B cannot read User A's task even via direct API call. |
| TC-OWN-03 | FR-7.1 | Sign back in as User A | User A's original task is still present (data is per-user and intact). |

> TC-OWN-02 is the highest-risk requirement and is also covered by automated tests
> (service + endpoint, including a cross-user `404`).

---

## 8. Validation, Feedback & Reliability (FR-8, NFR-3, NFR-4, NFR-5)

| ID | Requirement | Steps | Expected result |
|---|---|---|---|
| TC-UX-01 | FR-8.3, NFR-3 | Perform any create/edit/delete | A loading indication shows while in progress; the result is reflected without a full page reload. |
| TC-UX-02 | NFR-4 | Stop the backend, then try to load tasks or submit a task | A clear, user-facing error message appears — no crash or blank screen. |
| TC-UX-03 | FR-8.2 | Trigger any validation error on a form | The message is specific and the entered input is not erased. |
| TC-UX-04 | NFR-5 | After create/edit/delete/toggle | The list updates immediately (no manual refresh needed). |

---

## 9. Persistence (NFR-1)

| ID | Requirement | Steps | Expected result |
|---|---|---|---|
| TC-PERSIST-01 | NFR-1 | Create a few tasks. **Stop and restart the backend** (`dotnet run` again, without deleting `app.db`). Reload the app and sign in | All previously created tasks are still there — data survives a restart. |

---

## Result log (optional)

| Test ID | Date | Result (Pass/Fail) | Notes |
|---|---|---|---|
| | | | |
