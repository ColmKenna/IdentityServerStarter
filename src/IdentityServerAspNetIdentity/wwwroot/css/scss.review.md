# SCSS Code Review — `site.scss`, `forms.scss`, `multiselect.scss`

**Date:** 2026-02-28  
**Files reviewed:** 3 (1,473 + 252 + 243 = 1,968 total lines)

---

## Section 1: Summary Table

| # | Severity | Category | Location | Finding and Suggestion |
|---|----------|----------|----------|----------------------|
| 1 | High | Nesting | `nav .menu-container button .chevron` (site.scss:447–450) | Nesting depth of 4 levels. Compiles to `nav .menu-container button .chevron` (specificity `0,0,2,2`). **Suggestion:** Flatten to BEM `.menu-container__chevron`. **Complexity:** Moderate |
| 2 | High | Nesting | `nav .menu-container button.open .chevron` (site.scss:458–460) | Nesting depth of 5 levels — deepest in the codebase. **Suggestion:** Use `.menu-container__chevron--open`. **Complexity:** Moderate |
| 3 | High | Nesting | `nav .menu-container .dropdown li a` (site.scss:484–495) | 4-level nesting. **Suggestion:** Use `.dropdown__link`. **Complexity:** Moderate |
| 4 | High | Naming (BEM) | `nav` as block root (site.scss:354–529) | 175-line block uses a bare element selector as root. Creates implicit HTML coupling and high specificity. **Suggestion:** Introduce `.main-nav` block class and use BEM elements. **Complexity:** Complex |
| 5 | High | Naming (BEM) | `.sidebar` descendant chains (site.scss:532–610) | Uses `.sidebar ul li`, `.sidebar.collapsed ul li::after` etc. — chains of 3–4 selectors. **Suggestion:** Refactor to `.sidebar__item`, `.sidebar__icon`, `.sidebar__tooltip`. **Complexity:** Complex |
| 6 | High | Performance | `.multiselect-option` `transition: all` (multiselect.scss:88) | `transition: all 0.2s ease-in-out` watches every property. **Suggestion:** `transition: background-color 0.2s ease-in-out, border-color 0.2s ease-in-out;` **Complexity:** Simple |
| 7 | High | Performance | `.multiselect-input` `transition: all` (multiselect.scss:139) | Same issue on hidden checkbox. **Suggestion:** Specify exact properties. **Complexity:** Simple |
| 8 | High | Dead Code / Cross-Ref | Bootstrap `.btn` in non-Edit pages | `Users/Index`, `Users/Create`, `Clients/Index`, `Clients/Edit`, `Ciba/*`, `Device/*`, `Logout`, `_ConfirmModal`, `Grants/Index` still use Bootstrap classes (`btn`, `btn-primary`, `btn-danger`, `btn-secondary`, `btn-sm`, `btn-outline-primary`, `btn-outline-secondary`, `btn-outline-info`, `btn-outline-danger`) that have **no corresponding styles** since Bootstrap was removed ([_Layout.cshtml:41](file:///Users/colmkenna/Source/IdentityServerStarter/src/IdentityServerAspNetIdentity/Pages/Shared/_Layout.cshtml#L41)). These elements render unstyled. **Suggestion:** Migrate to `button button-*` pattern like `Edit.cshtml` was updated to use. **Complexity:** Moderate |
| 9 | High | Dead Code / Cross-Ref | Bootstrap layout/utility classes | `container`, `row`, `col-md-12`, `d-flex`, `justify-content-between`, `align-items-center`, `gap-2`, `flex-wrap`, `mb-0`, `mb-3`, `me-2`, `table`, `table-striped`, `table-sm`, `table-responsive`, `badge`, `bg-success`, `bg-warning`, `bg-danger`, `bg-secondary`, `text-bg-secondary`, `form-check`, `form-check-input`, `form-check-label`, `alert-dismissible`, `fade`, `show`, `card-body` still used in HTML with no styles. **Suggestion:** Add a utility stylesheet or migrate page-by-page. **Complexity:** Complex |
| 10 | Medium | Dead Code | `.header-grid` (_Nav.cshtml:10) | Class used in HTML but **no style exists** in any SCSS file. **Suggestion:** Add styles or remove the wrapper `<div>`. **Complexity:** Simple |
| 11 | Medium | Dead Code | `.menu-icon` (site.scss:429–432) | Defined in SCSS inside `nav .menu-container` but no element in HTML uses `.menu-icon`. **Caveat:** May be applied dynamically by JS. **Suggestion:** Verify and remove if unused. **Complexity:** Simple |
| 12 | Medium | Dead Code | `.nav-link` (_Nav.cshtml:74,77) | Class used in HTML but no `.nav-link` rule exists in SCSS. **Suggestion:** Either style it or remove the class. **Complexity:** Simple |
| 13 | Medium | Dead Code / Cross-Ref | `.multiselect-*` loaded globally (multiselect.scss) | All 243 lines of `multiselect.css` are loaded via `_Layout.cshtml` on every page but only consumed in `Clients/Edit.cshtml`. **Suggestion:** Move to a per-page reference. **Complexity:** Simple |
| 14 | Medium | Naming (BEM) | `.card-header`, `.card-content` (site.scss:680–693) | Elements of `.card` use single-hyphen instead of BEM `__`. Inconsistent with correct BEM in `.claims-list__header`, `.claim-modal__header`, `.profile-card__header`, `.security-card__header`. **Suggestion:** Rename to `.card__header`, `.card__content`. **Complexity:** Moderate |
| 15 | Medium | Naming (BEM) | `.button-primary`, `.button-danger`, etc. (site.scss:759–870) | Button modifiers use single-hyphen instead of BEM `--`. **Suggestion:** Use `.button--primary`, `.button--danger`, etc. **Complexity:** Moderate (HTML update needed) |
| 16 | Medium | Variables | Hardcoded hover colours (site.scss:792–793, 805–807, 818–819) | `.button-danger :hover` uses `#bd2130`, `.button-warning :hover` uses `#e0a800`/`#d39e00`, `.button-success :hover` uses `#1e7e34`/`#1c7430`. These raw hex values won't adapt to themes. **Suggestion:** Use theme variables or `color-mix()`. **Complexity:** Simple |
| 17 | Medium | Variables | Hardcoded `color: #212529` (site.scss:800, 807, 856) | `.button-warning` and `.button-outline-warning :hover` use raw dark text colour instead of `var(--text-primary)`. **Suggestion:** Use the theme variable. **Complexity:** Simple |
| 18 | Medium | Variables | Multiple raw `rgba()` shadows (site.scss:926, 1012, 1016, 1128, 1131) | Inline shadow values instead of using existing `--box-shadow` or extracting custom properties. **Suggestion:** Extract to `--shadow-sm`, `--shadow-md`. **Complexity:** Simple |
| 19 | Medium | Variables | `.status-badge` hardcoded rgba backgrounds (site.scss:1364, 1369, 1374, 1379, 1384) | `rgba(40, 167, 69, 0.15)`, `rgba(220, 53, 69, 0.15)`, `rgba(108, 117, 125, 0.15)` are light-theme colours that don't adapt. **Suggestion:** Use `color-mix(in srgb, var(--success-color) 15%, transparent)` etc. **Complexity:** Simple |
| 20 | Medium | Variables | `.danger-zone__header` hardcoded rgba (site.scss:1258) | `rgba(220, 53, 69, 0.08)` doesn't adapt to themes. **Suggestion:** Same `color-mix()` approach. **Complexity:** Simple |
| 21 | Medium | Variables | `multiselect.scss` SCSS variable aliases (multiselect.scss:9–21) | `$primary-color: var(--primary-color)` — SCSS variables wrapping `var()` references add indirection with zero benefit. **Suggestion:** Use CSS custom properties directly. **Complexity:** Moderate |
| 22 | Medium | Theming | `forms.scss` `.alert-success`/`.alert-danger` hardcoded alpha (forms.scss:78,84) | Same issue — `rgba(40, 167, 69, 0.15)` and `rgba(220, 53, 69, 0.15)` don't adapt to dark/coloured themes. **Suggestion:** Use `color-mix()` or theme variables. **Complexity:** Simple |
| 23 | Medium | Architecture | `site.scss` monolith (site.scss, 1473 lines) | Single file handles themes, global styles, navigation, sidebar, login, cards, tables, breadcrumbs, claims list, modals, profile cards, security cards, status badges, data tables, empty states, and utilities. **Suggestion:** Split into partials: `_themes.scss`, `_global.scss`, `_nav.scss`, `_sidebar.scss`, `_cards.scss`, `_login.scss`, `_claims.scss`, `_modals.scss`, `_profile.scss`, `_security.scss`, `_data-table.scss`, `_utilities.scss`. **Complexity:** Complex |
| 24 | Medium | Selector Specificity | `img.icon-banner` (site.scss:628) | Unnecessary `img` qualifier. **Suggestion:** Use `.icon-banner`. **Complexity:** Simple |
| 25 | Medium | Duplication | `.alert-warning` in `site.scss` vs `.alert` in `forms.scss` | Warning variant is separate from `.alert` block. **Suggestion:** Move to `forms.scss` as `&.alert-warning`. **Complexity:** Simple |
| 26 | Medium | Duplication | Option-pill in `forms.scss` vs `multiselect.scss` | Two parallel checkbox-selection components: `forms.scss` pills (`.option-pill`) and `multiselect.scss` grid (`.multiselect-option`). Both solve "select multiple items". **Suggestion:** Consolidate or document the distinction. **Complexity:** Complex |
| 27 | Medium | Accessibility | `a:hover, a:focus` `outline: none` (site.scss:345) | Global removal of focus outline on all links without a visible replacement. WCAG 2.4.7 failure for keyboard users. **Suggestion:** Replace `a:focus` rule with `a:focus-visible` and add visible indicator. **Complexity:** Simple |
| 28 | Medium | Accessibility | Missing `prefers-reduced-motion` | `site.scss` defines 15+ `transition` properties and `forms.scss` has several more. None of the three files include a `prefers-reduced-motion` media query (unlike `multiselect.scss` which has `prefers-contrast`). **Suggestion:** Add global reduced-motion fallback. **Complexity:** Simple |
| 29 | Medium | Accessibility | `forms.scss` deprecated `clip` (forms.scss:205) | `clip: rect(0 0 0 0)` is deprecated. Already using `clip-path: inset(50%)`. **Suggestion:** Remove `clip` line. **Complexity:** Simple |
| 30 | Medium | Inline Style | `style="margin-left:auto"` (Edit.cshtml:622, 702) | Inline styles on `<span>` wrappers for "Revoke All" and "End All" buttons. **Suggestion:** Add `.security-card__header-action { margin-left: auto; }` class. **Complexity:** Simple |
| 31 | Low | Code Clarity | `/*# sourceMappingURL=site.scss.map */` (site.scss:910) | Auto-generated source map comment in SCSS source. Should only appear in compiled CSS. **Suggestion:** Remove from `.scss`. **Complexity:** Simple |
| 32 | Low | Code Clarity | Missing semicolons in `forms.scss` | Lines 193, 199, 213, 233, 239, 245, 251 missing trailing semicolons. Inconsistent with rest of codebase. **Suggestion:** Add semicolons. **Complexity:** Simple |
| 33 | Low | Code Clarity | Indentation inconsistency in `multiselect.scss` | Uses 4-space indentation while `site.scss` and `forms.scss` use 2-space. **Suggestion:** Standardise to 2-space across all files. **Complexity:** Simple |
| 34 | Low | Positive Note | `claims-list` BEM (site.scss:982–1080) | Excellent BEM with `&__header`, `&__row`, `&__checkbox`, `&__type`, `&__value`, `&__edit-btn`. Well structured. |
| 35 | Low | Positive Note | `claim-modal` BEM (site.scss:1136–1202) | Clean BEM with `&__content`, `&__header`, `&__title`, `&__close`, `&__body`, `&__footer`. |
| 36 | Low | Positive Note | New `profile-card`, `security-card`, `danger-zone` BEM (site.scss:1206–1287) | Recent additions follow correct BEM with `&__header`, `&__body`, `&__actions`. Consistent pattern. |
| 37 | Low | Positive Note | `security-status` and `status-badge` BEM (site.scss:1324–1387) | Good use of `&__row`, `&__label`, `&__value` and modifier pattern `&--active`, `&--locked`, `&--disabled`, `&--yes`, `&--no`. |
| 38 | Low | Positive Note | `data-table` and `empty-state` BEM (site.scss:1415–1473) | Clean, focused components. `empty-state` uses `&__icon`, `&__text`. |
| 39 | Low | Positive Note | Theming system (site.scss:7–308) | Comprehensive CSS custom property system with 5 themes. Smart use of `var()` references for derived values (tabs, option pills). |
| 40 | Low | Positive Note | `forms.scss` form switch (forms.scss:125–188) | Accessible toggle with `:focus-visible`, `:checked`, `:disabled` pseudo-classes. |
| 41 | Low | Positive Note | `multiselect.scss` contrast/print (multiselect.scss:111–119, 177–184, 227–231, 237–242) | `prefers-contrast: more` and `@media print` queries — good a11y edge-case coverage. |
| 42 | Low | Positive Note | `Edit.cshtml` migration progress | Profile and Security tabs fully migrated from Bootstrap to custom BEM components (`profile-card`, `security-card`, `danger-zone`, `status-badge`, `data-table`, `empty-state`, `count-badge`). Excellent progress. |

---

## Section 2: Detailed Findings

### Nesting Depth and Selector Specificity

#### Navigation Nesting — High | Moderate

**Location:** [site.scss:354–529](file:///Users/colmkenna/Source/IdentityServerStarter/src/IdentityServerAspNetIdentity/wwwroot/css/site.scss#L354-L529)

**Current code:**
```scss
nav {
  .menu-container {
    button {
      .chevron {           // depth 4
      }
      &.open {
        .chevron {         // depth 5
          transform: rotate(180deg);
        }
        ~.dropdown {       // depth 4
          max-height: 300px;
        }
      }
    }
    .dropdown {
      li a {               // depth 4
      }
    }
  }
}
```

**Issue:** Compiles to `nav .menu-container button.open .chevron` (specificity `0,0,2,2`). Overrides become difficult without escalation.

**Suggested fix:**
```scss
.main-nav { /* ... */ }
.main-nav__burger { /* ... */ }
.main-nav__title { /* ... */ }
.main-nav__chevron { /* ... */ }
.main-nav__chevron--open { transform: rotate(180deg); }
.main-nav__dropdown { /* ... */ }
.main-nav__dropdown-link { /* ... */ }
```

---

### Dead Code and Cross-Reference Analysis

#### Bootstrap Classes Still in Use — High | Moderate

**Location:** Multiple `.cshtml` files (not `Edit.cshtml`, which was already migrated)

**Issue:** The `Edit.cshtml` page was successfully migrated to custom `button button-*` classes, but 10+ other pages still use Bootstrap `btn` classes:

| Page | Classes Used |
|------|-------------|
| `Users/Index.cshtml` | `btn btn-primary`, `btn btn-sm btn-outline-primary` |
| `Users/Create.cshtml` | `btn btn-primary`, `btn btn-outline-secondary` |
| `Clients/Index.cshtml` | `btn btn-sm btn-primary`, `btn btn-secondary` |
| `Clients/Edit.cshtml` | `btn btn-secondary`, `btn btn-primary` |
| `Ciba/Consent.cshtml` | `btn btn-primary`, `btn btn-secondary`, `btn btn-outline-info` |
| `Ciba/Index.cshtml` | `btn btn-primary` |
| `Ciba/All.cshtml` | `btn btn-primary` |
| `Device/Index.cshtml` | `btn btn-primary`, `btn btn-secondary`, `btn btn-outline-info` |
| `Logout/Index.cshtml` | `btn btn-primary` |
| `_ConfirmModal.cshtml` | `btn btn-secondary`, `btn btn-danger` |
| `Grants/Index.cshtml` | `btn btn-danger` |
| `Edit.cshtml:448` | `btn btn-outline-danger` (one remaining instance) |

Additionally, `Users/Index.cshtml` uses `badge bg-success`, `badge bg-warning`, `badge bg-danger`, `badge bg-secondary`, `table`, `table-striped` which also have no styles.

**Suggested fix:** Migrate each page to the custom class system, following the pattern established in `Edit.cshtml`.

---

### Variables and Theme Consistency

#### Hardcoded Button Hover Colours — Medium | Simple

**Location:** [site.scss:785–870](file:///Users/colmkenna/Source/IdentityServerStarter/src/IdentityServerAspNetIdentity/wwwroot/css/site.scss#L785-L870)

**Current code:**
```scss
.button-danger {
  &:hover, &:focus {
    background: #bd2130;    // hardcoded
    border-color: #bd2130;  // hardcoded
  }
}
.button-warning {
  color: #212529;           // hardcoded
  &:hover, &:focus {
    background: #e0a800;    // hardcoded
    border-color: #d39e00;  // hardcoded
    color: #212529;         // hardcoded
  }
}
.button-success {
  &:hover, &:focus {
    background: #1e7e34;    // hardcoded
    border-color: #1c7430;  // hardcoded
  }
}
```

**Issue:** These hover colours are hardcoded to light-theme values and won't adapt when the danger/warning/success themed colours are overridden in dark/blue/green/purple themes.

**Suggested fix:**
```scss
.button-danger {
  &:hover, &:focus {
    background: color-mix(in srgb, var(--danger-color) 85%, black);
    border-color: color-mix(in srgb, var(--danger-color) 80%, black);
  }
}
```

---

#### Status Badge Hardcoded Backgrounds — Medium | Simple

**Location:** [site.scss:1353–1387](file:///Users/colmkenna/Source/IdentityServerStarter/src/IdentityServerAspNetIdentity/wwwroot/css/site.scss#L1353-L1387)

**Current code:**
```scss
&--active { background: rgba(40, 167, 69, 0.15); }
&--locked { background: rgba(220, 53, 69, 0.15); }
&--disabled { background: rgba(108, 117, 125, 0.15); }
```

**Issue:** Uses light-theme RGB values. In dark theme, `--success-color` is `#198754` (not `rgb(40, 167, 69)`), so the badge background and text colour are mismatched.

**Suggested fix:**
```scss
&--active { background: color-mix(in srgb, var(--success-color) 15%, transparent); }
&--locked { background: color-mix(in srgb, var(--danger-color) 15%, transparent); }
&--disabled { background: color-mix(in srgb, var(--secondary-color) 15%, transparent); }
```

---

### Accessibility

#### Global Focus Outline Removal — Medium | Simple

**Location:** [site.scss:342–346](file:///Users/colmkenna/Source/IdentityServerStarter/src/IdentityServerAspNetIdentity/wwwroot/css/site.scss#L342-L346)

**Current code:**
```scss
a:hover,
a:focus {
  color: var(--primary-hover);
  outline: none;
}
```

**Issue:** Removes focus indicator on all links. Keyboard users cannot see which link is focused (WCAG 2.4.7 failure).

**Suggested fix:**
```scss
a:hover {
  color: var(--primary-hover);
}
a:focus-visible {
  color: var(--primary-hover);
  outline: 2px solid var(--focus-border);
  outline-offset: 2px;
}
```

---

#### Missing `prefers-reduced-motion` — Medium | Simple

**Issue:** 15+ `transition` properties across `site.scss` and `forms.scss` with no reduced-motion fallback. `multiselect.scss` correctly has `prefers-contrast` but also lacks `prefers-reduced-motion`.

**Suggested fix (add to `site.scss`):**
```scss
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
    scroll-behavior: auto !important;
  }
}
```

---

### Code Clarity

#### Source Map Comment in SCSS Source — Low | Simple

**Location:** [site.scss:910](file:///Users/colmkenna/Source/IdentityServerStarter/src/IdentityServerAspNetIdentity/wwwroot/css/site.scss#L910)

```scss
/*# sourceMappingURL=site.scss.map */
```

**Issue:** Auto-generated output embedded in source. Causes duplicate source map references in compiled CSS.

**Suggested fix:** Delete the line.

---

## Section 3: Statistics

```
Files reviewed:          3
Total lines:             1,968 (site.scss: 1,473 | forms.scss: 252 | multiselect.scss: 243)
Total findings:          33 (excluding positive notes)
  Critical:              0
  High:                  9
  Medium:                21
  Low:                   3
  Modernization:         0
Positive notes:          9
Estimated remediation:
  Simple:                16
  Moderate:              7
  Complex:               4
```

---

## Priority Remediation Order

1. **Migrate remaining Bootstrap `btn` classes** (Finding #8) — highest user-visible impact
2. **Add `prefers-reduced-motion`** (Finding #28) — simple, broad accessibility improvement
3. **Fix global focus outline removal** (Finding #27) — accessibility compliance
4. **Replace hardcoded hover/badge colours with `color-mix()`** (Findings #16, #17, #19, #20, #22) — theme consistency
5. **Replace `transition: all`** (Findings #6, #7) — simple performance win
6. **Remove source map comment** (Finding #31) — quick cleanup
7. **File architecture split** (Finding #23) — longer-term maintainability
8. **Nav/sidebar BEM refactor** (Findings #4, #5) — longer-term quality
