# SCSS Remediation Plan (`scss.review.md` + `scss.cp.codereview.md`)

**Date:** 2026-02-28  
**Inputs compared:**
- `/scss.cp.codereview.md` (targeted findings)
- `/src/IdentityServerAspNetIdentity/wwwroot/css/scss.review.md` (expanded findings + migration inventory)

## 1) Consolidated Backlog (Deduplicated)

### A. Shared in both reviews (highest confidence)
1. Replace `transition: all` in `multiselect.scss`.
2. Add `prefers-reduced-motion` fallback.
3. Fix focus visibility where `outline: none/0` is used.
4. Resolve duplicated multi-select systems (`.option-*` vs `.multiselect-*`).
5. Reduce deep selector nesting in nav/menu.

### B. Additional items from `scss.review.md` (broader codebase migration)
1. Migrate remaining Bootstrap class usage in Razor pages (`btn`, `table`, `badge`, layout utilities).
2. Fix theme hardcoding (`#hex`, fixed `rgba`) in button/status/alert styles.
3. Address style-orphan classes (`.header-grid`, `.nav-link`) and possible dead class (`.menu-icon`).
4. Remove source-map comment from SCSS source and minor formatting consistency issues.
5. Longer-term architecture split of `site.scss` and BEM normalization.

---

## 2) Delivery Strategy

Use **4 implementation phases** to reduce regression risk and keep PRs reviewable.

## Phase 1 — Fast Safety & Accessibility Wins (1 PR)

- [x] **Phase 1 Complete**

### Scope
- `site.scss`, `forms.scss`, `multiselect.scss`

### Tasks
- [x] Replace `transition: all` in `multiselect.scss` with explicit properties.
- [x] Add global reduced-motion block in `site.scss`:
  - [x] disable/reduce transitions and animations for nav/menu/sidebar/dropdowns.
- [x] Replace global `a:focus` outline removal with `:focus-visible` pattern.
- [x] Add/keep explicit focus indicators for nav links/brand/dropdown links.
- [x] Remove deprecated `clip` from `.option-input` if `clip-path` is already present.
- [x] Remove source map comment from `site.scss` source.

### Acceptance Criteria
- Keyboard focus remains visibly clear across links/nav controls.
- Motion is reduced when OS reduced-motion is enabled.
- No `transition: all` remains in SCSS source.
- No visual regression on Edit page nav/sidebar/forms interactions.

### Effort
- **Simple / Moderate** (~0.5–1 day)

---

## Phase 2 — Bootstrap Usage Remediation (2 PRs)

- [x] **Phase 2 Complete**

### Scope
- Razor views currently still using removed Bootstrap classes.

### Tasks
- [x] Inventory all Bootstrap class usage in `IdentityServerAspNetIdentity/Pages/**`.
- [x] Migrate classes to existing design system equivalents:
  - [x] `btn*` -> `button button-*`/`button-outline-*`
  - [x] table/badge/alerts/form-check/layout utility classes -> existing custom classes or new scoped utility classes.
- [x] Handle one-off remaining Bootstrap usage in `Edit.cshtml` (`btn btn-outline-danger`).
- [x] If no exact replacement exists, create **minimal local utility class** in SCSS (scoped and documented).

### Recommended split
- [x] **PR 2A:** Buttons/actions/forms migration.
- [x] **PR 2B:** Tables/badges/layout utility replacement.

### Acceptance Criteria
- No Bootstrap-only classes remain in Razor pages (or each remaining one is explicitly justified + styled).
- Affected pages render consistently with current theme system.

### Effort
- **Moderate / Complex** (~1.5–3 days)

---

## Phase 3 — Multi-select Consolidation (1 PR)

- [x] **Phase 3 Complete**

### Decision (required before coding)
Choose canonical implementation:
- [x] **Option A (recommended):** Keep existing `.multi-select` + `.option-*` currently used in `Edit.cshtml`, retire `.multiselect-*`.
- [ ] **Option B:** Migrate markup to `.multiselect-*`, retire `.option-*`.

### Tasks
- [x] Pick canonical class system and document it.
- [x] Remove duplicate/orphaned rules from non-canonical system.
- [x] If migration path needed, add temporary alias selectors for one release.
- [x] Ensure stylesheet loading scope is correct (global vs page-level for multiselect CSS).

### Acceptance Criteria
- Exactly one multi-select implementation remains as canonical.
- No duplicate style systems for same UI pattern.
- Roles UI and any other consumers still function and look consistent.

### Effort
- **Moderate / Complex** (~1–2 days)

---

## Phase 4 — Theming + Architecture Hardening (multi-PR, incremental)

- [x] **Phase 4 Complete**

### Scope
- `site.scss` modernization and maintainability improvements.

### Tasks
- [x] Replace hardcoded theme-breaking values:
  - [x] button hover hex values
  - [x] status badge and alert fixed `rgba`
  - [x] other raw color/shadow literals where theme tokens exist.
- [x] Resolve medium dead-code/style-orphan findings:
  - [x] `.header-grid`, `.nav-link`, verify `.menu-icon` usage.
- [x] Reduce high-nesting nav/sidebar selectors (introduce BEM-style component classes).
- [ ] Optional but recommended: split `site.scss` into partials (`_themes`, `_global`, `_nav`, `_sidebar`, `_cards`, etc.) with a single entrypoint.

### Acceptance Criteria
- Color system is token-driven and theme-safe.
- Nesting depth reduced in nav/sidebar hotspots.
- `site.scss` maintainability improved with module split (if in scope).

### Effort
- **Complex** (~3–5 days, can be staged)

---

## 3) Suggested Execution Order

- [x] Phase 1 (safety/a11y/perf quick wins)
- [x] Phase 2A/2B (Bootstrap migration)
- [x] Phase 3 (multi-select convergence)
- [x] Phase 4 (theming + architecture refactor)

---

## 4) Risk Controls

- Keep PRs small and page-scoped where possible.
- Validate on key pages after each PR:
  - `Admin/Users/Edit`
  - `Admin/Users/Index`
  - `Admin/Users/Create`
  - `Admin/Clients/*`
  - `Ciba/*`
  - `Device/Index`
  - `Logout/Index`
- Verify all themes (`default`, `dark`, `blue`, `green`, `purple`) after theming updates.
- Use side-by-side screenshots for regression checks on nav/sidebar/tables/forms.

---

## 5) Definition of Done (overall)

- [x] No critical/high accessibility regressions.
- [x] No `transition: all` in SCSS source.
- [x] Reduced-motion support present.
- [x] Bootstrap dependency fully removed from Razor class usage (or explicitly reintroduced and documented).
- [x] Multi-select pattern unified.
- [x] Theme tokens used instead of hardcoded light-theme color values in priority components.
- [x] Remaining architectural debt tracked as explicit follow-up items if not completed in this cycle.
