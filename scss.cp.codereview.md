## Review Summary

**Assumption used for cross-reference:** Consuming components provided are the current Razor/JS usage in `IdentityServerAspNetIdentity`, especially `Pages/Admin/Users/Edit.cshtml` and `wwwroot/js/users-edit.js`.

### Section 1: Summary Table

| Severity | Category | Location | Finding and Suggestion |
|----------|----------|----------|----------------------|
| High | Dead Code / Integration | `wwwroot/css/multiselect.scss` (`.multiselect-*` selectors, lines 42-232) | The new component classes (`.multiselect-fieldset`, `.multiselect-grid`, `.multiselect-option`, etc.) are not referenced in provided consuming components; Roles UI uses legacy `.multi-select`/`.option-*` classes instead. **Suggestion:** Either migrate consuming markup to `.multiselect-*` or remove/park this stylesheet until wired. **Complexity:** Moderate |
| High | Animations & Transitions | `wwwroot/css/multiselect.scss` lines 88, 139 | `transition: all ...` is used on interactive elements (`.multiselect-option`, `.multiselect-input`), causing unnecessary transitions and potential jank. **Suggestion:** Limit to explicit properties (`border-color`, `background-color`, `box-shadow`). **Complexity:** Simple |
| High | Accessibility | `wwwroot/css/site.scss` lines 345, 413, 493 | `outline: none` is applied on focusable link patterns without a guaranteed replacement focus indicator on those elements. **Suggestion:** Keep outline or add explicit `:focus-visible` ring for each affected selector. **Complexity:** Simple |
| High | Accessibility / Motion | `wwwroot/css/site.scss` lines 378, 449, 479, 513 and related transition rules | Multiple transform/size/position transitions (menu/burger/dropdown/sidebar) have no `prefers-reduced-motion` fallback. **Suggestion:** Add `@media (prefers-reduced-motion: reduce)` to disable or shorten these transitions. **Complexity:** Moderate |
| High | Nesting / Specificity | `wwwroot/css/site.scss` under `nav .menu-container button &.open .chevron` | Nested selector depth exceeds 3 levels, increasing specificity and making overrides harder. **Suggestion:** Flatten with component classes (e.g., `.menu-button--open .chevron`) and reduce nesting depth. **Complexity:** Moderate |
| Medium | Variables (Dead) | `wwwroot/css/multiselect.scss` lines 10, 11, 17, 19, 20, 24 | Variables are defined but unused: `$primary-hover`, `$border-color`, `$bg-form`, `$text-primary`, `$text-secondary`, `$spacing-xs`. **Suggestion:** Remove unused variables or use them consistently where intended. **Complexity:** Simple |
| Medium | Responsive / Breakpoints | `wwwroot/css/multiselect.scss` lines 63, 68, 73, 170, 201, 222 | Breakpoints are hardcoded (`768`, `1024`, `575.98`) rather than shared tokens/mixins, reducing consistency across files. **Suggestion:** Centralize breakpoint values in a shared map/mixin and reference by semantic names. **Complexity:** Moderate |
| Medium | Code Clarity / Scope | `wwwroot/css/forms.scss` line 103 (`h5`) | Global `h5` styling inside forms stylesheet can leak into unrelated components. **Suggestion:** Scope heading styling to a form container class or component block. **Complexity:** Simple |
| Medium | Architecture / Consistency | `wwwroot/css/forms.scss` lines 190-252 and `wwwroot/css/multiselect.scss` lines 42-232 | Two parallel multi-select styling systems exist (`.multi-select` + `.option-*` vs `.multiselect-*`), increasing maintenance cost and confusion. **Suggestion:** Choose one system, deprecate the other, and document migration path. **Complexity:** Complex |
| Low | Positive Note | `wwwroot/css/site.scss` theme blocks (`:root`, `[data-theme=*]`) | Theme custom properties are comprehensive and consistently overridden per theme variant, which is a strong foundation for runtime theming. |
| Low | Positive Note | `wwwroot/css/forms.scss` lines 203-214 | `.option-input` uses a proper visually-hidden pattern (not `display:none`), preserving checkbox accessibility for screen readers/keyboard users. |

---

### Section 2: Detailed Findings

### Dead Code and Cross-Reference Analysis

#### Unconsumed `.multiselect-*` component styles — High | Moderate

**Location:** `wwwroot/css/multiselect.scss` (`.multiselect-fieldset`, `.multiselect-grid`, `.multiselect-option`, `.multiselect-input`, `.multiselect-label`, `.multiselect-pill`)

**Current code:**
```scss
.multiselect-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  ...
}

.multiselect-option {
  ...
}
```

**Issue:** These classes are not present in the provided consuming markup. `Pages/Admin/Users/Edit.cshtml` currently uses `.multi-select`, `.options-list`, `.option-label`, `.option-input`, `.option-pill`. This indicates the newer stylesheet is not integrated yet.

**Suggested fix:**
```scss
// Option A: keep only integrated system
// Remove or archive unused .multiselect-* rules until markup migration is complete.

// Option B: migrate markup to .multiselect-* and remove legacy .multi-select/.option-* rules.
```

**Caveat:** If `.multiselect-*` classes are injected dynamically elsewhere, treat as pending integration rather than definitive dead code.

---

### Animations and Transitions

#### `transition: all` on interactive controls — High | Simple

**Location:** `wwwroot/css/multiselect.scss:88`, `wwwroot/css/multiselect.scss:139`

**Current code:**
```scss
.multiselect-option {
  transition: all $transition-duration $transition-easing;
}

.multiselect-input {
  transition: all $transition-duration $transition-easing;
}
```

**Issue:** `all` can transition unintended properties (layout/paint-heavy), making behavior less predictable and potentially less performant.

**Suggested fix:**
```scss
.multiselect-option {
  transition: border-color $transition-duration $transition-easing,
              background-color $transition-duration $transition-easing,
              box-shadow $transition-duration $transition-easing;
}

.multiselect-input {
  transition: border-color $transition-duration $transition-easing,
              background-color $transition-duration $transition-easing,
              box-shadow $transition-duration $transition-easing;
}
```

#### Missing reduced-motion fallback for menu/sidebar transitions — High | Moderate

**Location:** `wwwroot/css/site.scss` (e.g., lines 378, 449, 479, 513, 537, 620)

**Current code:**
```scss
.burger { transition: transform 0.3s ease; }
.dropdown { transition: max-height 0.3s ease-out; }
.menu-container button .label { transition: opacity 0.3s ease, width 0.3s ease; }
```

**Issue:** Motion-heavy transitions (transform/max-height/width) have no `prefers-reduced-motion` treatment.

**Suggested fix:**
```scss
@media (prefers-reduced-motion: reduce) {
  .burger,
  .menu-container button .chevron,
  .menu-container button .label,
  .menu-container .dropdown,
  .sidebar,
  .sidebar.collapsed ul li::after,
  .main-container > .content {
    transition: none;
    animation: none;
  }
}
```

---

### Accessibility

#### Focus outline removed without a guaranteed replacement — High | Simple

**Location:** `wwwroot/css/site.scss:345`, `wwwroot/css/site.scss:413`, `wwwroot/css/site.scss:493`

**Current code:**
```scss
a:hover,
a:focus {
  color: var(--primary-hover);
  outline: none;
}

.nav-brand:hover,
.nav-brand:focus {
  ...
  outline: none;
}
```

**Issue:** Keyboard users can lose visible focus indication in some states.

**Suggested fix:**
```scss
a:focus-visible,
.nav-brand:focus-visible,
.menu-container .dropdown li a:focus-visible {
  outline: 2px solid var(--focus-border);
  outline-offset: 2px;
}
```

---

### Nesting Depth and Specificity

#### Deep nested navigation selectors increase specificity — High | Moderate

**Location:** `wwwroot/css/site.scss` under `nav .menu-container button &.open .chevron`

**Current code:**
```scss
nav {
  .menu-container {
    button {
      &.open {
        .chevron {
          transform: rotate(180deg);
        }
      }
    }
  }
}
```

**Issue:** Nesting depth >3 creates selectors that are harder to override and maintain.

**Suggested fix:**
```scss
.menu-button { ... }
.menu-button--open .chevron { transform: rotate(180deg); }
```

---

### Variables and Custom Properties

#### Unused SCSS variables in multiselect module — Medium | Simple

**Location:** `wwwroot/css/multiselect.scss:10,11,17,19,20,24`

**Current code:**
```scss
$primary-hover: var(--primary-hover);
$border-color: var(--border-color);
$bg-form: var(--form-bg);
$text-primary: var(--text-primary);
$text-secondary: var(--text-secondary);
$spacing-xs: 0.25rem;
```

**Issue:** Dead variable declarations add noise and imply non-existent dependencies.

**Suggested fix:**
```scss
// Remove unused declarations
// OR reference them in rules where they were intended.
```

#### Breakpoints are hardcoded instead of tokenized — Medium | Moderate

**Location:** `wwwroot/css/multiselect.scss:63,68,73,170,201,222`

**Current code:**
```scss
@media (min-width: 768px) { ... }
@media (min-width: 1024px) { ... }
@media (max-width: 575.98px) { ... }
```

**Issue:** Inline breakpoint values reduce consistency and make future breakpoint changes error-prone.

**Suggested fix:**
```scss
$breakpoints: (
  sm: 576px,
  md: 768px,
  lg: 1024px
);

// then use mixins or map-get($breakpoints, md/lg)
```

---

### Code Clarity and Organisation

#### Global heading styling in forms module leaks scope — Medium | Simple

**Location:** `wwwroot/css/forms.scss:103`

**Current code:**
```scss
h5 {
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--text-primary);
}
```

**Issue:** This affects all `h5` elements globally, even outside form contexts.

**Suggested fix:**
```scss
.form-group h5,
.multi-select h5 {
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--text-primary);
}
```

#### Parallel multi-select implementations increase maintenance cost — Medium | Complex

**Location:** `wwwroot/css/forms.scss:190-252` and `wwwroot/css/multiselect.scss:42-232`

**Current code:**
```scss
// forms.scss
.multi-select { ... }
.option-pill { ... }

// multiselect.scss
.multiselect-grid { ... }
.multiselect-pill { ... }
```

**Issue:** Two naming systems and behavior models for the same UX pattern create divergence and harder long-term maintenance.

**Suggested fix:**
```scss
// Create one canonical block (recommended: .multiselect)
// Keep temporary aliases only during migration window, then remove legacy selectors.
```

---

### Positive Notes

#### Theming structure is strong and consistent — Low | Simple

**Location:** `wwwroot/css/site.scss` theme sections (`:root`, `[data-theme=dark|blue|green|purple]`)

**Current code:**
```scss
:root { ...custom properties... }
[data-theme=dark] { ...overrides... }
```

**Issue:** None. This is a good pattern worth preserving.

**Suggested fix:**
```scss
// No change needed.
```

#### Accessible visually-hidden checkbox technique is correctly used — Low | Simple

**Location:** `wwwroot/css/forms.scss:203-214`

**Current code:**
```scss
.option-input {
  position: absolute;
  clip: rect(0 0 0 0);
  clip-path: inset(50%);
  height: 1px;
  width: 1px;
  ...
}
```

**Issue:** None. This preserves accessibility better than `display: none`.

**Suggested fix:**
```scss
// No change needed.
```

---

### Section 3: Statistics

## Review Statistics

- **Files reviewed:** 3
- **Total findings:** 11
- **Critical:** 0 | **High:** 5 | **Medium:** 4 | **Low:** 2 | **Modernization:** 0
- **Dead code lines identified:** ~170 lines (`.multiselect-*` block in `multiselect.scss`, pending dynamic-use confirmation)
- **Estimated total remediation effort:** **Simple: 5 | Moderate: 4 | Complex: 1**
