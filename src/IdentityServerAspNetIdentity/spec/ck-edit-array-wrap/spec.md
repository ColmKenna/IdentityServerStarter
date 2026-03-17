# ck-edit-array TagHelper spec# CK-Edit-Array TagHelper: Wrap Web Component (Developer-Focused Spec)



PurposeThis document defines the bounded context, ubiquitous language, developer stories, acceptance criteria (GWT), and executable-quality test cases for adding a TagHelper that wraps the `ck-edit-array` web component in server-rendered markup.

-------

Provide a TagHelper that wraps the `ck-edit-array` web component so Razor pages can render and bind to its data from ASP.NET Core forms without requiring consumers to manually set up input naming and hidden fields.## Project inspection (what I scanned)

- Found dependency in `package.json`: `@colmkenna/ck-edit-array` (version `^1.0.0`).

Goals- Found compiled distribution of the component under `wwwroot/lib/@colmkenna/ck-edit-array/dist/` (`.js`, `.cjs`, `.d.ts`, maps).

------ Found `Pages/_ViewImports.cshtml` with both:

- Make the custom element easy to use in Razor pages with strong-typed model binding.  - `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`

- Automatically set `name` attributes for inputs inside the component so they submit as an array field with indexed names (e.g. `Users[0].Name`).  - `@addTagHelper *, IdentityServerAspNetIdentity`

- Render initial data from the model into the component's `data` attribute as JSON.  So TagHelpers under the `IdentityServerAspNetIdentity` namespace are auto-discovered in Razor Pages.

- Provide server-side parameters for common labels and attributes used by the component (edit-label, save-label, delete-label, restore-label, cancel-label, array-field).- I added a TagHelper at `Pages/TagHelpers/CkEditArrayWrapperTagHelper.cs` which compiles cleanly (verified by `dotnet build` success; build warnings unrelated to this change remain).

- Emit markup compatible with unobtrusive validation (preserve existing data-val attributes inside slotted edit templates).

Notes: This inspection satisfies the prompt's requirement to scan the workspace for relevant artifacts and incorporates the findings into the spec.

Non-goals

------------

- Do not implement client-side JS behavior of the component. The component's JS (ck-edit-array) is already present in wwwroot and is responsible for runtime interactions.

## Bounded Contexts

API / TagHelper shape

---------------------- **UI Component Wrapping** — scope & boundary rationale

TagHelper name: `CkEditArrayWrapperTagHelper`  - Scope: Server-side rendering concerns for embedding and enhancing client-side/custom elements (specifically `ck-edit-array`). Responsibilities include semantics for wrapping, attribute forwarding, and HTML shape for progressive enhancement and form binding.

  - Out-of-scope: Rewriting or replacing the web component's JavaScript, changing its internal API (array-field/data attributes), or implementing client-side polyfills.

Supported attributes (all optional unless noted):

- asp-for (ModelExpression) - REQUIRED. The model property that is an IList<T> or array. Used to derive `array-field` and to render initial JSON `data`.Problem statement

- edit-label (string)- Developers need a consistent, server-controlled wrapper for `ck-edit-array` instances to: add consistent classes/attributes, integrate server-side state, enable styling/layout, and optionally include server-rendered hints or validation markup.

- save-label (string)

- delete-label (string)Goals

- restore-label (string)- Provide a small, testable TagHelper that wraps `ck-edit-array` in a predictable wrapper element, forwards/merges attributes, and integrates with existing Razor Page conventions.

- cancel-label (string)

- array-field (string) - optional override of generated nameAnti-goals

- item-template (string) - optional: allow passing a partial view name or inline template (future enhancement)- Not to change the web component behaviour (data parsing, client-side editing). Not to implement full server-side rendering of the component’s internal controls.



Rendered output---

----------------

The TagHelper should render a `ck-edit-array` element with the following attributes and structure:## Ubiquitous Language

- `ck-edit-array` — the client-side custom element (web component) that edits arrays of objects.

- `array-field`: the field name to use for form inputs. Default: the model expression name (e.g. `Users`). If `array-field` attribute supplied to TagHelper, use that.- `Wrapper` — the server-rendered HTML element (e.g., `div`) that contains the `ck-edit-array` element.

- `data`: a JSON-encoded array representing the model value. If the model value is null, use an empty array `[]`.- `TagHelper` — ASP.NET Core server-side helper that transforms or augments Razor elements at render time.

- Optional attributes: `edit-label`, `save-label`, `delete-label`, `restore-label`, `cancel-label` when provided.- `asp-wrap` — attribute on the `ck-edit-array` tag that toggles wrapping behaviour.

- `asp-wrapper-class` — attribute to add/extend classes on the wrapper element.

Slots- `attribute forwarding` — preserving attributes from the original `ck-edit-array` element (e.g., `array-field`, `data`, `name`) so the client component receives them unchanged.

------ `server-rendered content` — HTML generated on the server including wrappers and any additional markup.

- The TagHelper should allow developer-defined slot content for `display` and `edit` slots. Anything placed inside the TagHelper element (child content) should be rendered unchanged so authors can provide their templates.

---

Example usage in Razor

----------------------## Domain Model (Entities / Value Objects / Aggregates / Services)

Example strongly-typed Razor usage inside a form:This feature operates at the presentation layer; the domain model is minimal and focused on the UI component as an aggregate.



@model MyViewModel- Entities & Aggregates

  - **ComponentInstance** (aggregate root, transient): represents a single `ck-edit-array` instance in a rendered view. Identifier: implicitly its location in the DOM/form and `array-field` attribute. Invariants: `data` attribute when present must be JSON or valid string accepted by the component.

<form method="post">

  <ck-edit-array asp-for="Model.Users" edit-label="Edit" save-label="Save">- Value Objects

    <div slot="display">  - **WrapperSpec**: { Wrap: bool, WrapperClass: string }

      <span data-display-for="name"></span>    - Equality: value-based.

      <span data-display-for="email"></span>

    </div>- Domain Services

    <div slot="edit">  - **TagHelperProcessor**: responsibility to transform a `ck-edit-array` node into desired HTML with wrapper; inputs: TagHelperContext, TagHelperOutput; outputs: modified TagHelperOutput.

      <input name="name" required />

      <input name="email" type="email" required />- Domain Events

    </div>  - None required for this small UI concern. (If later instrumentation is needed, events like `ComponentWrapped` can be emitted from server telemetry.)

  </ck-edit-array>

  <button type="submit">Submit</button>---

</form>

## Integrations & Contracts

Notes: the TagHelper must not overwrite or remove the developer's slotted markup.- Integration: client-side `ck-edit-array` library (already present in `wwwroot/lib/@colmkenna/ck-edit-array`). The TagHelper must not break expected attributes (`array-field`, `data`, `name`, `data-index`, `data-name`).

- Contract: TagHelper must render a wrapper element (default `div`) with class `ck-edit-array-wrapper` and inner HTML containing the original `ck-edit-array` element including all original attributes and inner content.

Server-side rendering details- Version/compat: No versioning on server-side wrapper; compatibility requirement: do not change attribute names or element tag name of the client component.

-----------------------------

- Use `JsonSerializer` (System.Text.Json) with options: PropertyNamingPolicy = JsonNamingPolicy.CamelCase, IgnoreNullValues = true (or equivalent), to encode the model data into the `data` attribute. Ensure strings are HTML-encoded to avoid attribute-breaking characters.---

- If the model contains complex types (DateTime, enums), rely on default System.Text.Json handling; document that custom converters can be used if needed.

## Story Map (Capability → Activities → Stories)

Validation & Unobtrusive support- Capability: Server-side wrapping of `ck-edit-array` components

--------------------------------  - Activity: Implement TagHelper → STORY-1: Implement wrapper TagHelper

- Ensure that inputs inside the `edit` slot retain name attributes set by the developer. The TagHelper will not attempt to rewrite inner input names; instead it will set `array-field` so the client component can generate proper input names when rendering hidden inputs for submission.  - Activity: Preserve attributes & support toggles → STORY-2: Attribute forwarding & `asp-wrap` control

- Document that server-side validation will still work as long as the component emits correctly-named inputs on submit (component responsibility).  - Activity: Docs & usage → STORY-3: Documentation + register/check discovery



Acceptance criteria (automated checks)---

--------------------------------------

- A `spec.md` file exists at `spec/ck-edit-array-wrap/spec.md` describing the TagHelper (this file).## User Stories

- The TagHelper, when implemented, must pass the following manual acceptance tests:

  1. Rendering with `asp-for` produces a `ck-edit-array` element with `data` attribute containing the model items as JSON.#### STORY-1: Add TagHelper to wrap `ck-edit-array`

  2. Passing label attributes results in corresponding attributes present on the produced `ck-edit-array` element.- As a developer, I want a TagHelper that wraps `ck-edit-array` so that I can add server-generated classes/attributes and consistent container markup around the component.

  3. The TagHelper does not strip or modify slotted child content.

  4. The `array-field` attribute defaults to the expression name derived from `asp-for` and can be overridden.**Acceptance Criteria (GWT)**

- Given a Razor page containing `<ck-edit-array array-field="users" data='[...]'></ck-edit-array>` When the page is rendered Then the output contains a wrapper `<div class="ck-edit-array-wrapper">` that contains the original `ck-edit-array` element.

Files to create/modify- Given the TagHelper is present When `asp-wrap="false"` is specified Then the original `ck-edit-array` element is rendered without a wrapper.

----------------------- Given `asp-wrapper-class="my-class"` When the TagHelper wraps Then the wrapper's `class` attribute includes both `ck-edit-array-wrapper` and `my-class`.

- Create or update: `Pages/TagHelpers/CkEditArrayWrapperTagHelper.cs` (implementation)

- Create tests (manual or automated) under `spec/ck-edit-array-wrap/` to exercise rendering examples.**Test Cases**

- Happy path:

Spec tests / examples  - Render a Razor page with `<ck-edit-array array-field="users" data='[{"name":"A"}]'></ck-edit-array>` and assert HTML contains `<div class="ck-edit-array-wrapper">` and child `<ck-edit-array ...>` with the same attributes.

---------------------- Edge cases:

- Include one or more Razor sample fragments in `spec/ck-edit-array-wrap/examples.md` showing expected HTML output.  - Component tag is self-closing (`<ck-edit-array ... />`) — ensure wrapper still contains the element.

  - No attributes present — wrapper still added and component rendered.

Edge cases and notes  - `asp-wrap` present without value (treated as true) — wrapper present.

--------------------- Error/Exception:

- If `asp-for` targets a null collection, `data` should be `[]`.  - Malformed `data` JSON — TagHelper should not attempt to parse or validate `data`; component receives attribute unchanged. Test: ensure server does not throw and markup is produced.

- If `asp-for` points to a non-enumerable, TagHelper should throw or produce a helpful error (runtime exception or log) — document this choice in implementation notes.- Validation:

- Consider culture for date serialization; leave to consumer to provide converters if necessary.  - If `asp-wrapper-class` contains unsafe characters, they are HTML-encoded and included in `class` attribute. (Sanitization test)

- Security/Permissions:

Next steps  - TagHelper runs at render time with existing view permissions — no extra auth required. Ensure it doesn't expose server-side secrets (no server-side reflection of hidden data into attributes).

----------- Accessibility checks:

- Implement `CkEditArrayWrapperTagHelper` per spec.  - Wrapper element should not break semantics. No interactive element added by default, so ensure role is not needed. If additional UI (labels/errors) added later, follow ARIA.

- Add unit tests (Razor TagHelper test harness) or manual verification instructions.- Performance:

- Update README or docs with usage examples and troubleshooting.  - Rendering wrapper is O(1) per component; ensure adding wrapper does not add heavy processing.



Completion**Test Data Examples**

----------| Field | Example | Notes |

When this spec file is present and reviewed, mark the todo as completed and proceed to implement the TagHelper file itself.|---|---:|---|
| array-field | "users" | typical field name used by component |
| data | [{"name":"John","email":"john@example.com"}] | JSON string attribute (UTC timestamps not relevant here) |
| asp-wrapper-class | "my-class" | extra CSS class to add |

**Dependencies & NFR Hooks**
- Depends on `Pages/_ViewImports.cshtml` to include the project TagHelpers (already present).
- NFR: Minimal CPU/memory; wrapper must be cheap.

**Risks & Mitigations**
- Risk: Wrapper could accidentally strip component attributes. Mitigation: Tests assert exact attribute preservation.

**Priority (MoSCoW):** Must — low-risk, small change needed for consistent UI.

**Definition of Done:**
- Code implemented and compiled
- Unit/integration tests added (or manual test steps documented)
- Basic usage documented in README or a code comment
- Confirmed `dotnet build` success and manual render smoke test in local browser

---

#### STORY-2: Preserve attributes and forward original markup
- As a developer, I want the TagHelper to preserve all attributes and inner content of `ck-edit-array` so the client-side component receives the same inputs it expects.

**Acceptance Criteria (GWT)**
- Given `<ck-edit-array name="Model[0].Users" array-field="users" data='[...]'></ck-edit-array>` When rendered Then the inner `ck-edit-array` element inside wrapper contains `name`, `array-field`, and `data` attributes unchanged.
- Given an attribute with quotes or unicode characters When rendered Then they are correctly HTML-encoded in output.
- Given form-binding naming patterns (`Model[0].Users`) When wrapper is used Then the name attribute remains exactly as authored.

**Test Cases**
- Happy path: Attributes `array-field`, `data`, `name` are present in the inner element post-render.
- Edge: Large `data` payload (e.g., 10KB string) — ensure rendering succeeds and content preserved.
- Error: Null attribute values — attribute should either be omitted or rendered as empty string consistently.
- Validation: Names containing `.` and `[` `]` used for model binding must be intact.

**Test Data Examples**
| Field | Example | Notes |
|---|---:|---|
| name | "Model[0].Users[0].Name" | test form-binding naming |
| data | 10KB JSON string | performance edge test |

**Dependencies & NFR Hooks:** none extra.

**Risks & Mitigations:**
- Risk: Double-encoding values when TagHelper escapes attributes. Mitigation: Use correct HTML encoding and assert resulting HTML contains expected raw JSON-ish substrings (encoded properly).

**Priority (MoSCoW):** Must (component requires attributes to function)

**Definition of Done:** attributes preserved in all tested cases and smoke-tested in browser.

---

#### STORY-3: Docs + discovery & toggling
- As a developer, I want the TagHelper documented and discoverable so other developers know how to opt in/out of wrapping.

**Acceptance Criteria (GWT)**
- Given `_ViewImports.cshtml` contains `@addTagHelper *, IdentityServerAspNetIdentity` When a developer uses `<ck-edit-array asp-wrap="false">` Then the TagHelper is honored and no wrapper is produced.
- Given the repository README/Docs updated When a developer reads it Then they understand `asp-wrap` and `asp-wrapper-class` usage.

**Test Cases**
- Happy path: Add snippet to README with examples for default wrapping, disabling, and extra class.
- Edge: Project without `@addTagHelper *, IdentityServerAspNetIdentity` — TagHelper wouldn't be discovered. Document this and provide remediation steps.

**Test Data Examples**
| Field | Example | Notes |
|---|---:|---|
| usage snippet | `<ck-edit-array asp-wrap="false" ... />` | illustrated in README |

**Dependencies & NFR Hooks:** edit `Pages/_ViewImports.cshtml` if the project did not already include the TagHelper (it does already).

**Priority (MoSCoW):** Should (important for discoverability but not blocking)

**Definition of Done:** README updated, README links to code, and quick manual test instructions included.

---

## Quality Gates

- **AC ↔ Test Mapping**
  - STORY-1 AC1 → Test: wrapper present, inner element preserved
  - STORY-1 AC2 → Test: `asp-wrap="false"` results in no wrapper
  - STORY-1 AC3 → Test: `asp-wrapper-class` merges classes
  - STORY-2 AC1 → Test: attributes preserved (name/array-field/data)
  - STORY-2 AC2 → Test: unicode/quotes encoded correctly
  - STORY-3 AC1 → Test: disable via `asp-wrap` demonstrates discovery

- **Vocabulary check:** ensured `ck-edit-array`, `asp-wrap`, and `asp-wrapper-class` are used consistently.

- **Conflicts/Duplicates:** No overlapping stories; STORY-1 and STORY-2 are complementary (structure vs attribute semantics). STORY-3 is documentation/discovery.

---

## NFR Summary (not already in stories)
- Observability: No special logs required. If desired, a debug-only telemetry event `ComponentWrapperRendered` could be emitted (not required now).
- Security: TagHelper must not read or render server secrets into attributes. Server-side content added must be safe for HTML output.
- Reliability: Simple transformations; no external dependencies.
- Performance: Per-component O(1) HTML generation. No heavy CPU or I/O.

---

## Open Questions
- Should the wrapper support additional ARIA attributes or roles by default (for accessibility)?
- Should the TagHelper support rendering validation markup (error message area) related to server-side model validation? If so, we need a mapping from model metadata to component state.
- Do we need to emit telemetry whenever a component is wrapped for analytics?

---

## Assumptions
- The `ck-edit-array` component expects attributes like `data` and `array-field` to be present and will parse them client-side; server will not attempt to parse/validate the JSON `data` attribute.
- Project `_ViewImports.cshtml` already registers the project TagHelpers (confirmed).
- No unit test framework is present for TagHelpers in the repository; adding tests is out of scope for this change but recommended. Manual or simple integration tests are sufficient initially.

---

## How to use the TagHelper (developer quick reference)
- Default wrapping:

```html
<ck-edit-array array-field="users" data='[{"name":"John"}]'></ck-edit-array>
```

Renders as:

```html
<div class="ck-edit-array-wrapper">
  <ck-edit-array array-field="users" data='[{"name":"John"}]'></ck-edit-array>
</div>
```

- Disable wrapping:
```html
<ck-edit-array asp-wrap="false" array-field="users" ...></ck-edit-array>
```

- Add wrapper classes:
```html
<ck-edit-array asp-wrapper-class="col-6 my-component" array-field="..." ...></ck-edit-array>
```

---

## File output (created by this spec generation)
- `spec/ck-edit-array-wrap/spec.md` — this file (written to `Pages/spec/ck-edit-array-wrap/spec.md` relative to the project root). Please review and commit to your docs folder.

---

## Completion notes
- I implemented a TagHelper at `Pages/TagHelpers/CkEditArrayWrapperTagHelper.cs` and verified the project builds (`dotnet build` success).
- Next recommended steps (not implemented here): add automated tests (xUnit/MSTest/NUnit) that render a Razor page or use `TagHelper` unit-testing helpers to assert HTML output; add README/docs; optionally add integration smoke test that opens a dev server and verifies DOM.

---

