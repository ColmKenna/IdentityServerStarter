# Refactor Client Admin UI: Dedicated Multiselect Tabs for Grant Types & Scopes

## Bounded Contexts

### **Admin Client Configuration Context**
- **Scope & Boundary Rationale:** Manages the administrative interface for OAuth2/OIDC client configuration. Bounded by the administrative role (`ADMIN`) and the operations needed to persistently configure client credentials, authentication settings, and authorization policies in an IdentityServer instance.
- **Problem Statement:** The current `Edit.cshtml` UI mixes authentication settings and resource permissions (grant types + scopes) within generic tabs, creating visual inconsistency and reduced UX clarity. Grant types are displayed inline within "Authentication & Secrets" using an inline checkbox group; scopes are in a separate tab but lack symmetry.
- **Goals:**
  - Provide dedicated, self-contained tabs for managing grant types and scopes.
  - Use consistent multiselect UI pattern (checkbox grid) for both capabilities.
  - Improve accessibility and form semantics for admin workflows.
  - Maintain current business logic and data binding without backend changes.
- **Anti-goals:**
  - Do not introduce dynamic add/remove buttons for individual grant types or scopes (multiselect checkbox suffices).
  - Do not change the underlying data model or validation logic in `ClientEditViewModel`.
  - Do not add new security checks beyond existing `[Authorize(Roles = "ADMIN")]`.

---

## Ubiquitous Language

| **Term** | **Definition** |
|----------|---|
| **Grant Type** | OAuth2/OIDC authorization flow identifier (e.g., `client_credentials`, `authorization_code`, `implicit`, `hybrid`). Defines the mechanism by which a client obtains access tokens. |
| **Allowed Grant Types** | The set of grant types that a client is permitted to use when requesting tokens from the authorization server. |
| **Scope** | A permission or resource claim (e.g., `api1`, `profile`, `openid`, `offline_access`). Defines what resources a client may access post-authentication. |
| **Allowed Scopes** | The set of scopes that a client is permitted to request during authorization. |
| **Available Grant Types** | The system-wide list of supported grant types offered to administrators when configuring a client. |
| **Available Scopes** | The system-wide list of scopes registered in the IdentityServer configuration that may be granted to a client. |
| **Multiselect Checkbox Group** | A UI component rendering multiple checkboxes in a responsive grid layout, allowing the user to toggle membership in a collection. |
| **Admin Client Configuration Form** | The Razor Page form (`Edit.cshtml` + `Edit.cshtml.cs`) enabling ADMIN-role users to view and modify a client's settings. |

---

## Domain Model (Entities / Value Objects / Aggregates / Services)

### **Entities & Aggregates**
- **Client Aggregate:** Rooted by the `Client` entity (from Duende.IdentityServer.Models). Child value objects include `AllowedGrantTypes` and `AllowedScopes` collections.
- **ClientEditViewModel Aggregate:** A view-specific aggregate (form binding model) that mirrors client configuration state, including:
  - `AllowedGrantTypes: List<string>` — currently selected grant types.
  - `AvailableGrantTypes: List<string>` — system-wide offering of grant types (read-only in form).
  - `AllowedScopes: List<string>` — currently selected scopes.
  - `AvailableScopes: List<string>` — system-wide offering of scopes (read-only in form).

### **Value Objects**
- **GrantTypeSelection:** Immutable record of grant type + selection state. (Implied; not yet modeled in code.)
- **ScopeSelection:** Immutable record of scope + selection state. (Implied; not yet modeled in code.)

### **Domain Services**
- **IClientEditor:** Orchestrates client retrieval and updates. Responsibilities:
  - `GetClientForEditAsync(int id)` → retrieves client and populates available options.
  - `UpdateClientAsync(int id, ClientEditViewModel input)` → persists client configuration changes.

### **Domain Events**
| **Event** | **Trigger Condition** | **Producer** | **Consumer(s)** |
|-----------|-----|--------|-----------|
| `ClientConfigurationUpdated` | User clicks Save on the client edit form and `UpdateClientAsync` succeeds. | `EditModel.OnPostAsync()` | Audit log, optional cache invalidation. |

---

## Integrations & Contracts

### **Data Binding Contract (ASP.NET Core Razor Pages)**
- **Input Binding:** Checkbox arrays in HTML input name format `Input.AllowedGrantTypes[]` and `Input.AllowedScopes[]`.
  - Framework automatically deserializes to `List<string>` properties in `ClientEditViewModel`.
  - Empty collections remain empty; unchecked items are omitted from binding.
- **Validation:** Model-level validation (if any) enforced in `OnPostAsync()` before `UpdateClientAsync()` call.
- **Output:** Form submission via HTTP POST to same page; on success, redirect with `TempData["Success"]` flash message.

### **Client Service Contract (IdentityServerServices)**
- **Interface:** `IClientEditor` (external service layer).
- **Request:** `ClientEditViewModel` (complete client state).
- **Response:** `bool` (success/failure). On validation/logic errors, service returns `false`; page re-populates available options and re-renders form with `ModelState` errors.
- **Sync/Async:** Async (`UpdateClientAsync`).
- **SLA/Timeouts:** Inherited from service layer; no specific timeout defined in Razor Page.

---

## Story Map (Capability → Activities → Stories)

**Capability:** _Provide a unified, consistent multiselect UI for admin client configuration._

- **Activity 1: Reorganize Form Tabs**
  - STORY-1: Extract grant types into dedicated "Allowed Grant Types" tab.
  - STORY-2: Refactor scopes tab to use symmetric multiselect checkbox pattern.

- **Activity 2: Implement Multiselect Component**
  - STORY-3: Design and implement checkbox grid component (HTML/CSS/JS) for both grant types and scopes.

- **Activity 3: Validate & Document**
  - STORY-4: Write acceptance tests for form binding, validation, and persistence.
  - STORY-5: Document UI changes and admin workflow in project README.

---

## User Stories

### STORY-1: Extract Allowed Grant Types into Dedicated Tab

**As an admin, I want grant types to have their own dedicated tab so that I can clearly distinguish authorization flow configuration from authentication settings.**

#### Acceptance Criteria (GWT)

1. **Given** I am on the client edit form with a client loaded,  
   **When** I navigate to a new "Allowed Grant Types" tab,  
   **Then** I see a checkbox grid displaying all available grant types, with currently selected ones pre-checked.

2. **Given** I have unchecked a previously allowed grant type,  
   **When** I submit the form,  
   **Then** the client is saved with the updated allowed grant types, and the success message appears.

3. **Given** I am on the form and no grant types are allowed,  
   **When** I view the "Allowed Grant Types" tab,  
   **Then** all checkboxes appear unchecked.

4. **Given** I check multiple grant types,  
   **When** I submit the form and reload the page,  
   **Then** the selected grant types remain checked (persistence verified).

5. **Given** the form contains validation errors in other tabs,  
   **When** I submit without correcting them,  
   **Then** I remain on the form and the grant types tab retains my selections.

6. **Given** the form is posted with an empty grant types list,  
   **When** the server processes the request,  
   **Then** the client's allowed grant types collection is set to empty and persisted.

#### Test Cases

**Happy Path:**
- Load a client with `AllowedGrantTypes = ["client_credentials", "authorization_code"]`.
- Navigate to "Allowed Grant Types" tab; verify checkboxes for those two types are checked.
- Uncheck "client_credentials", check "implicit" (if available).
- Submit form; verify redirect and success message.
- Reload page; verify new selections persist.

**Edge Cases:**
- No grant types available: verify tab renders with empty grid or appropriate message.
- Single grant type available: verify checkbox renders and behaves correctly.
- Very long grant type names: verify labels wrap gracefully in checkbox grid layout.
- Large number of grant types (10+): verify responsive grid layout on mobile/tablet.

**Error/Exception:**
- Service layer returns false on update: verify error message displayed and form state retained.
- Client ID does not exist: verify 404 on page load.
- Unauthorized user (non-ADMIN): verify 403 Forbidden.
- Form submission with network timeout: verify graceful degradation (browser default retry behavior).

**Validation:**
- Empty collection submission (no checkboxes selected): verify persisted as empty list.
- Duplicate values (if possible via direct HTML manipulation): verify deduplicated or validation error.
- Invalid grant type string (e.g., malicious payload): verify sanitized or validation error raised.

#### Test Data Examples

| **Field** | **Example** | **Notes** |
|-----------|-----------|----------|
| ClientId | `client-123` | Standard client identifier. |
| AvailableGrantTypes | `["client_credentials", "authorization_code", "implicit", "hybrid", "refresh_token"]` | All registered grant types in system. |
| AllowedGrantTypes (Initial) | `["authorization_code"]` | Client only uses auth code flow initially. |
| AllowedGrantTypes (Updated) | `["authorization_code", "refresh_token"]` | Admin adds refresh token support. |
| AllowedGrantTypes (Empty) | `[]` | No grant types allowed (edge case). |

#### Dependencies & NFR Hooks
- **UI Component:** Checkbox grid component (shared across STORY-2).
- **Data Binding:** ASP.NET Core model binding for `Input.AllowedGrantTypes[]`.
- **Validation:** Inherit from existing `ClientEditViewModel` validation (no new rules).
- **Accessibility:** Form labels and fieldset semantic HTML for screen reader support.

#### Risks & Mitigations

| **Risk** | **Mitigation** |
|---------|---|
| User accidentally submits empty grant types, breaking client. | Add info text: "Client will not be able to request tokens if no grant types are allowed." Consider optional warning modal. |
| Tab ordering confusion (grant types newly placed mid-form). | Document tab purpose in label; consider sequential numbering or context. |
| Responsive grid breaks on very small screens. | Test on mobile; use responsive grid CSS (e.g., Bootstrap `col-*` or CSS Grid). |

#### Priority (MoSCoW)
**MUST** (rationale: Improves UI clarity and aligns with STORY-2 refactoring; core to feature request.)

#### Definition of Done
- [ ] New "Allowed Grant Types" tab added to `Edit.cshtml` after "Authentication & Secrets" tab.
- [ ] Tab renders multiselect checkbox grid (using component from STORY-3).
- [ ] Form binding via `Input.AllowedGrantTypes[]` working end-to-end.
- [ ] Persistence verified: selections saved and reloaded.
- [ ] Validation errors in other tabs do not clear grant types selections.
- [ ] Responsive layout tested on desktop, tablet, mobile.
- [ ] Accessibility: label-input association, fieldset role, ARIA attributes present.
- [ ] No runtime errors in browser console or server logs.
- [ ] Code review completed; no hardcoded strings in UI.
- [ ] Updated Razor Page unit tests (STORY-4).

---

### STORY-2: Refactor Allowed Scopes to Use Multiselect Checkbox Pattern

**As an admin, I want scopes to use a checkbox grid layout identical to grant types so that the form has consistent UX and I can quickly manage resource permissions.**

#### Acceptance Criteria (GWT)

1. **Given** I am on the client edit form with scopes configured,  
   **When** I navigate to the "Scopes" tab,  
   **Then** I see a checkbox grid displaying all available scopes, with currently allowed ones pre-checked.

2. **Given** I have toggled scope selections (checked/unchecked),  
   **When** I submit the form,  
   **Then** the client is saved with the updated scopes and a success message appears.

3. **Given** the form contains errors,  
   **When** I submit the form,  
   **Then** I remain on the form and my scope selections are preserved.

4. **Given** the checkbox grid displays many scopes (20+),  
   **When** I view the tab,  
   **Then** the layout remains responsive and readable (grid wraps, no horizontal scroll).

5. **Given** a scope name is very long,  
   **When** the grid renders,  
   **Then** the label text wraps or truncates gracefully without breaking layout.

6. **Given** I submit the form with no scopes selected,  
   **When** the server processes the request,  
   **Then** the client's allowed scopes collection is set to empty and persisted.

#### Test Cases

**Happy Path:**
- Load a client with `AllowedScopes = ["api1", "profile"]`.
- Navigate to "Scopes" tab; verify checkboxes for those two scopes are checked.
- Uncheck "profile", check "openid" (if available).
- Submit form; verify success and persistence on reload.

**Edge Cases:**
- No scopes available: verify tab renders gracefully (empty state message).
- Single scope available: verify checkbox renders and functions.
- Scope with special characters or Unicode: verify rendered correctly in label.
- Very large scope set (50+ scopes): verify grid responsive; no performance degradation.
- Scope names of varying lengths: verify consistent layout.

**Error/Exception:**
- Service update fails: verify error message and state retained.
- Unauthorized: verify 403.
- Network timeout on submission: verify browser retry/timeout handling.

**Validation:**
- Empty collection (no scopes selected): verify persisted as empty.
- Duplicate selections: verify deduplicated or validation error.
- Invalid scope strings: verify sanitized or validation error.

#### Test Data Examples

| **Field** | **Example** | **Notes** |
|-----------|-----------|----------|
| ClientId | `web-app-456` | Different client type. |
| AvailableScopes | `["openid", "profile", "email", "api1", "offline_access"]` | Registered scopes in system. |
| AllowedScopes (Initial) | `["openid", "profile", "api1"]` | Client has basic OIDC + API access. |
| AllowedScopes (Updated) | `["openid", "profile", "api1", "offline_access"]` | Admin grants offline access. |
| AllowedScopes (Empty) | `[]` | No scopes allowed. |

#### Dependencies & NFR Hooks
- **UI Component:** Shared checkbox grid component from STORY-3.
- **Data Binding:** ASP.NET Core model binding for `Input.AllowedScopes[]`.
- **Validation:** Inherit existing validation rules.
- **Accessibility:** Semantic HTML, ARIA labels for checkboxes.

#### Risks & Mitigations

| **Risk** | **Mitigation** |
|---------|---|
| Admin grants unintended scopes, exposing resources. | Add info text explaining scope purpose; consider scope grouping (identity vs. API). Consider audit trail. |
| Large scope list causes UI rendering lag. | Implement lazy rendering or pagination if 50+ scopes (defer to future optimization). |
| Form reset/cancellation clears scopes. | Ensure no unintended cancellation path; document expected behavior. |

#### Priority (MoSCoW)
**MUST** (rationale: Core to feature request; improves UX consistency.)

#### Definition of Done
- [ ] "Scopes" tab refactored to use checkbox grid layout (remove previous list of columns/rows).
- [ ] Form binding via `Input.AllowedScopes[]` working end-to-end.
- [ ] Persistence verified across page reloads.
- [ ] Responsive layout tested (desktop, tablet, mobile).
- [ ] Validation errors preserved scope selections.
- [ ] Accessibility: label-input, fieldset, ARIA roles present.
- [ ] No console errors or server-side exceptions.
- [ ] Code review completed.
- [ ] Unit tests added/updated in STORY-4.

---

### STORY-3: Design & Implement Multiselect Checkbox Grid Component

**As a developer, I want a reusable checkbox grid component for multiselect forms so that STORY-1 and STORY-2 can leverage a consistent, accessible UI pattern.**

#### Acceptance Criteria (GWT)

1. **Given** a multiselect checkbox grid component is implemented,  
   **When** it is rendered with a collection of items and selected items list,  
   **Then** the correct items appear checked and unchecked according to the input.

2. **Given** the form is submitted,  
   **When** the checkbox inputs have the name attribute `Input.AllowedGrantTypes[]` or similar,  
   **Then** ASP.NET Core model binding correctly deserializes selections into a `List<string>`.

3. **Given** the grid contains many items,  
   **When** viewed on various screen sizes (mobile, tablet, desktop),  
   **Then** the grid layout is responsive and readable without horizontal scrolling.

4. **Given** a checkbox label is very long,  
   **When** rendered,  
   **Then** text wraps or truncates gracefully; layout does not break.

5. **Given** the component is used in both grant types and scopes contexts,  
   **When** it is rendered,  
   **Then** CSS classes and component structure are reusable without duplication.

6. **Given** an admin using assistive technology,  
   **When** they navigate the checkboxes,  
   **Then** screen reader announces label, checked state, and role correctly.

#### Test Cases

**Happy Path:**
- Render component with 5 items; 2 pre-selected.
- Verify 2 checkboxes are checked, 3 unchecked.
- Click unchecked item; verify checked.
- Click checked item; verify unchecked.
- Submit form; verify model binding captures selection state.

**Edge Cases:**
- Zero items: verify empty grid or "no items" message renders without error.
- One item: verify single checkbox renders and behaves.
- 50+ items: verify layout responsive; verify performance acceptable (no lag).
- Very long item names (100+ chars): verify text wraps and layout stable.
- Special characters / Unicode in item names: verify rendered correctly.

**Error/Exception:**
- Null or undefined items list: verify graceful error handling (no crash).
- Null or undefined selected list: verify defaults to empty (nothing checked).

**Validation:**
- HTML form submission with component: verify model binding successful.
- Unchecked items omitted from form data: verify framework handles correctly.
- Resubmission after validation error: verify selections retained.

**Accessibility:**
- Screen reader announces each checkbox label and checked state.
- Keyboard navigation (Tab, Space) works correctly.
- ARIA attributes present: `role="group"`, `aria-labelledby` for fieldset.
- High contrast mode: verify labels and checkboxes visible.

#### Test Data Examples

| **Scenario** | **Items** | **Selected** | **Expected Output** |
|-----------|-----------|----------|---|
| Small set | `["item1", "item2", "item3"]` | `["item1", "item3"]` | item1 & item3 checked, item2 unchecked. |
| Large set | `[...50 items...]` | `[...25 items...]` | Grid layout responsive; 25 checked. |
| Empty | `[]` | `[]` | No checkboxes rendered; empty state message. |
| Long names | `["very long item name here", ...]` | `[...]` | Text wraps gracefully. |

#### Dependencies & NFR Hooks
- **Frontend:** HTML5 (semantic `<fieldset>`, `<label>`), CSS (responsive grid), JavaScript (optional; minimal interactivity needed).
- **ASP.NET Core:** Form binding via `name="Input.Property[]"` attribute.
- **Accessibility:** WCAG 2.1 Level AA compliance (labels, roles, keyboard nav).
- **Performance:** Component must render 50+ items without noticeable lag.

#### Risks & Mitigations

| **Risk** | **Mitigation** |
|---------|---|
| Component used inconsistently across codebase. | Document component API; provide examples in code comments. Create shared Razor component if needed. |
| Accessibility violations (missing labels, roles). | Review with accessibility checker (axe, Lighthouse); test with screen reader. |
| CSS conflicts with existing styles. | Isolate component styles in scoped CSS or namespace classes. |

#### Priority (MoSCoW)
**MUST** (rationale: Foundation for STORY-1 and STORY-2; required for consistent UX.)

#### Definition of Done
- [ ] Component HTML structure defined (fieldset, label, input[type=checkbox]).
- [ ] Responsive CSS grid layout implemented and tested on desktop/tablet/mobile.
- [ ] Component integrated into `Edit.cshtml` for grant types tab (STORY-1).
- [ ] Component integrated into `Edit.cshtml` for scopes tab (STORY-2).
- [ ] Form binding verified: checkboxes serialize to `List<string>` via model binding.
- [ ] Accessibility audit completed (WCAG 2.1 AA).
- [ ] Keyboard navigation tested (Tab, Space, Enter).
- [ ] Screen reader tested (NVDA, JAWS, or Mac VoiceOver).
- [ ] No styling regressions in existing form elements.
- [ ] Code reviewed; component documented.

---

### STORY-4: Write Acceptance Tests for Form Binding, Validation, and Persistence

**As a QA/developer, I want comprehensive test coverage for the multiselect form changes so that regressions are caught early and the form behavior is predictable.**

#### Acceptance Criteria (GWT)

1. **Given** a unit test for the EditModel page handler,  
   **When** I call `OnPostAsync()` with valid grant types and scopes selections,  
   **Then** the method calls `UpdateClientAsync()` and returns a redirect with success message.

2. **Given** a unit test,  
   **When** I simulate form binding with an empty grant types list,  
   **Then** the model binding succeeds and `AllowedGrantTypes` is an empty `List<string>`.

3. **Given** a validation test,  
   **When** I submit the form with validation errors (e.g., invalid ClientId),  
   **Then** ModelState is invalid, page is re-rendered, and selections are preserved in the re-rendered form.

4. **Given** an integration test,  
   **When** I submit a complete form with valid grant types and scopes,  
   **Then** the client record in the database (or mock) is updated correctly.

5. **Given** a test for the multiselect component,  
   **When** I render the component with various item counts (0, 1, 5, 50),  
   **Then** the HTML is well-formed and form binding attributes are correct.

6. **Given** a test for pre-population,  
   **When** I load a client edit page,  
   **Then** AvailableGrantTypes and AvailableScopes are populated from the service.

#### Test Cases

**Happy Path:**
- Load client, select 3 grant types and 2 scopes, submit, verify persistence.
- Reload page; verify selections retained.
- Deselect one grant type, submit, verify update persisted.

**Edge Cases:**
- Submit with zero grant types; verify persisted as empty.
- Submit with zero scopes; verify persisted as empty.
- Submit with all available items selected; verify all persisted.
- Rapid form resubmission (simulated double-click); verify idempotency (last write wins or duplicate prevention).

**Error/Exception:**
- Service returns false on update; verify error message and form state retained.
- Service throws exception; verify 500 error or graceful error page.
- Database constraint violation (if any); verify error handling.

**Validation:**
- Invalid model state; verify form re-renders with validation summary and selections retained.
- Client ID does not exist; verify 404.
- Unauthorized role; verify 403.

#### Test Data Examples

| **Test Scenario** | **Input AllowedGrantTypes** | **Input AllowedScopes** | **Expected Result** |
|---|---|---|---|
| Happy path | `["client_credentials", "authorization_code"]` | `["api1", "profile"]` | Persisted successfully. |
| Empty grant types | `[]` | `["api1"]` | Persisted (empty grants allowed). |
| Empty scopes | `["client_credentials"]` | `[]` | Persisted (empty scopes allowed). |
| Both empty | `[]` | `[]` | Persisted. |

#### Dependencies & NFR Hooks
- **Testing Framework:** xUnit or MSTest (based on project standards).
- **Mocking:** Moq for `IClientEditor` mock.
- **Assertions:** FluentAssertions or native assertions.
- **Integration Tests:** Database or in-memory data store.

#### Risks & Mitigations

| **Risk** | **Mitigation** |
|---------|---|
| Tests become brittle if model binding changes. | Use integration tests with real form binding; avoid testing ASP.NET Core internals. |
| Coverage gaps in error paths. | Explicitly test all error branches (service failure, validation, 404, 403). |
| Tests slow or flaky. | Use async/await correctly; mock external dependencies; use in-memory DB for speed. |

#### Priority (MoSCoW)
**SHOULD** (rationale: Important for regression prevention; essential before merging to main branch.)

#### Definition of Done
- [ ] Unit tests for `EditModel.OnPostAsync()` with various input scenarios.
- [ ] Unit tests for form binding (empty lists, populated lists).
- [ ] Unit tests for validation error handling and state preservation.
- [ ] Integration tests for end-to-end form submission and database persistence.
- [ ] Tests for multiselect component rendering (0, 1, 5, 50+ items).
- [ ] All tests pass on local machine and in CI/CD pipeline.
- [ ] Code coverage target met (e.g., 80%+ for affected code paths).
- [ ] Test documentation: describe each test's purpose.
- [ ] Code review completed; tests reviewed for quality.

---

### STORY-5: Document UI Changes and Admin Workflow

**As a maintainer, I want clear documentation of the refactored UI changes so that future developers understand the form structure and can extend it without confusion.**

#### Acceptance Criteria (GWT)

1. **Given** the project documentation,  
   **When** a developer reads the client admin form structure,  
   **Then** they understand the tab layout, multiselect component, and form binding approach.

2. **Given** the README or inline code comments,  
   **When** a developer needs to add a new grant type or scope,  
   **Then** the steps to add it are clear and require minimal trial-and-error.

3. **Given** the project wiki or ADR (Architecture Decision Record),  
   **When** someone asks why the multiselect checkbox pattern was chosen,  
   **Then** the rationale is documented (UX consistency, accessibility, binding simplicity).

4. **Given** API documentation or component comments,  
   **When** a developer wants to reuse the multiselect component elsewhere,  
   **Then** usage examples and props are clear.

#### Test Cases

**Documentation Completeness:**
- README section covers: tab layout, multiselect component, adding new grant types/scopes.
- Code comments in `Edit.cshtml` explain key sections (fieldset, name attribute, binding).
- `EditModel` page handler documented with method signatures and responsibilities.

**Clarity & Accuracy:**
- No broken links in documentation.
- Screenshots or diagrams showing tab layout (if applicable).
- Example code snippets are valid Razor/C#.
- References to external docs (ASP.NET Core, IdentityServer) are correct.

**Completeness:**
- Deployment/build instructions mention any new dependencies (none expected for this story).
- Known limitations or future improvements listed (e.g., "pagination not implemented for 50+ scopes").

#### Test Data Examples

| **Document** | **Section** | **Example Content** |
|---|---|---|
| README.md | "Client Admin UI" | Overview of tabs, multiselect pattern. |
| Edit.cshtml | Inline comments | `<!-- Multiselect checkbox grid for grant types --> ...` |
| ADR-001 | "Multiselect Pattern Decision" | Rationale for checkbox grid vs. multi-select HTML element. |
| Component docs | "Reusable Checkbox Grid" | Usage: `@Html.Partial("CheckboxGrid", new { items, selected, fieldName })` |

#### Dependencies & NFR Hooks
- **Format:** Markdown (for README), inline C#/Razor comments, optional ADR format.
- **Audience:** Developers; secondary audience is operations/support.
- **Maintenance:** Update if component is extended or tab structure changes.

#### Risks & Mitigations

| **Risk** | **Mitigation** |
|---------|---|
| Documentation becomes outdated. | Schedule doc review during quarterly refactoring sprints. Link docs to related code via comments. |
| Screenshots become incorrect after UI tweaks. | Use diagrams or high-level description instead of screenshots; easier to maintain. |

#### Priority (MoSChoW)
**SHOULD** (rationale: Important for team knowledge; aids future maintenance.)

#### Definition of Done
- [ ] README.md updated with "Client Admin Form" section.
- [ ] Inline code comments added to `Edit.cshtml` and `Edit.cshtml.cs`.
- [ ] Multiselect component usage documented (API, examples, props).
- [ ] ADR created or existing architecture docs updated.
- [ ] All links and code examples verified.
- [ ] Technical review completed; no spelling/grammar errors.
- [ ] Documentation accessible to team (e.g., linked from project README or wiki).

---

## Quality Gates

### AC ↔ Test Mapping

| **Story** | **Acceptance Criterion** | **Test Case(s)** |
|---|---|---|
| STORY-1 | Tab displays available GTs; selected ones pre-checked. | Happy Path: Load client; verify checkboxes. |
| STORY-1 | Submit updates client and shows success message. | Happy Path: Uncheck GT, submit, verify redirect. |
| STORY-1 | No GTs allowed: persisted as empty list. | Edge Case: Empty collection submission. |
| STORY-1 | Validation errors preserve selections. | Error/Exception: Form with validation errors. |
| STORY-2 | Tab displays available scopes; selected ones pre-checked. | Happy Path: Load client; verify checkboxes. |
| STORY-2 | Submit updates client and shows success message. | Happy Path: Toggle scope, submit, verify persistence. |
| STORY-2 | Responsive layout on mobile/tablet/desktop. | Edge Case: Mobile/tablet/desktop rendering. |
| STORY-3 | Component renders items with correct checked state. | Happy Path: Render with 5 items, 2 selected. |
| STORY-3 | Form binding captures selections. | Happy Path: Submit form; verify model binding. |
| STORY-3 | Responsive grid layout (50+ items). | Edge Case: Large item set. |
| STORY-3 | Accessibility: labels, roles, screen reader. | Accessibility: Screen reader test. |
| STORY-4 | OnPostAsync() with valid inputs persists successfully. | Unit Test: Happy path form submission. |
| STORY-4 | Empty GT/scope list binding works. | Unit Test: Empty list binding. |
| STORY-4 | Validation errors preserve state. | Unit Test: Validation error handling. |
| STORY-5 | UI changes documented in README. | Documentation: README section exists. |
| STORY-5 | Multiselect component usage documented. | Documentation: Code comments, examples present. |

### Vocabulary Check

- **Divergences:** None identified. Ubiquitous Language terms ("Grant Type", "Allowed Grant Types", "Scope", "Multiselect Checkbox Group") used consistently across stories and acceptance criteria.

### Conflicts/Duplicates

- **Potential Overlap:** STORY-1 and STORY-2 both implement multiselect UI patterns; mitigated by STORY-3 (shared component).
- **No conflicts identified.**

---

## NFR Summary

| **NFR** | **Scope** | **Requirement** | **Verification** |
|---|---|---|---|
| **Accessibility** | STORY-3, STORY-1, STORY-2 | WCAG 2.1 Level AA compliance; keyboard nav, screen reader support. | Axe DevTools audit; NVDA/JAWS testing. |
| **Responsive Design** | STORY-3, STORY-1, STORY-2 | Layout readable on desktop, tablet, mobile (breakpoints: 768px, 576px). | Manual testing on multiple devices/browser zoom. |
| **Performance** | STORY-3 | Component renders 50+ items without noticeable lag. | Measure render time; target <200ms. |
| **Security** | N/A (existing) | Authorization check (`[Authorize(Roles = "ADMIN")]`) remains; no new privilege elevation. | Code review; existing tests pass. |
| **Usability** | STORY-1, STORY-2 | Consistent UI pattern across both tabs; clear labels and help text. | Usability review; internal testing. |
| **Maintainability** | STORY-3, STORY-5 | Component reusable; code documented; low coupling. | Code review; documentation completeness. |

---

## Open Questions

1. **Scope Grouping:** Should scopes be grouped by category (e.g., Identity vs. API) in the checkbox grid, or displayed as a flat list? (Affects STORY-2 UI design.)
2. **Large Scale (50+ items):** Should the checkbox grid implement pagination or virtual scrolling if 50+ items are present? (Defer to future optimization if UX testing shows need.)
3. **Scope Selection Warnings:** Should there be a warning or confirmation dialog if the admin deselects all scopes? (May improve safety; requires business decision.)
4. **Grant Type Dependencies:** Are there any implicit dependencies between grant types (e.g., "refresh_token" only valid with "authorization_code")? If yes, should the UI prevent invalid combinations? (Affects validation logic.)
5. **Audit Trail:** Should form changes be logged to an audit table for compliance? (Currently out of scope but may be future requirement.)
6. **Component Reuse Scope:** Will the multiselect checkbox component be reused elsewhere in the admin panel, or only for grant types and scopes? (Affects design decisions: generic component vs. form-specific.)

---

## Assumptions

1. **Existing Data Model:** `ClientEditViewModel` and the underlying `Client` model will not be refactored; only the Razor view and component structure change.
2. **Service Layer Stability:** `IClientEditor.UpdateClientAsync()` is assumed to work correctly and will not be modified in this feature.
3. **Available Options Immutability:** `AvailableGrantTypes` and `AvailableScopes` are read-only during form interaction; no dynamic addition of new grant types/scopes in this feature.
4. **Form Binding:** ASP.NET Core's model binding for `Input.AllowedGrantTypes[]` arrays is assumed to work as documented (standard behavior).
5. **No New Persistence:** Changes are assumed to use existing persistence layer; no new database migrations or schema changes required.
6. **Browser Support:** Form assumes modern browsers (ES2015+, CSS Grid, Flexbox); no IE11 support required.
7. **Accessible Component:** Multiselect component assumes semantic HTML5 and does not rely on third-party JS libraries (e.g., Select2); minimal JS preferred.
8. **Current UI Location:** Grant types multiselect is currently embedded in "Authentication & Secrets" tab; extraction and relocation is assumed to be non-breaking (no downstream nav references).

---

## Summary

This specification defines a phased refactoring of the IdentityServer client admin form UI to provide dedicated tabs and consistent multiselect UI patterns for grant types and scopes. The work spans 5 stories:

1. **STORY-1:** Extract grant types to a dedicated tab.
2. **STORY-2:** Refactor scopes to use consistent checkbox grid.
3. **STORY-3:** Design and implement the shared multiselect component.
4. **STORY-4:** Write comprehensive test coverage.
5. **STORY-5:** Document changes and admin workflow.

**Key Design Principles:**
- **Consistency:** Identical multiselect UI pattern for both grant types and scopes.
- **Accessibility:** WCAG 2.1 Level AA compliance; keyboard and screen reader support.
- **Responsiveness:** Layout works on desktop, tablet, and mobile.
- **Simplicity:** Minimal backend changes; leverage ASP.NET Core model binding.
- **Maintainability:** Reusable component; clear documentation.

**Prioritization:** MUST (STORY-1, STORY-2, STORY-3), SHOULD (STORY-4, STORY-5).

**Definition of Done:** All acceptance criteria met, tests passing, accessibility audit passed, documentation complete, code reviewed.
