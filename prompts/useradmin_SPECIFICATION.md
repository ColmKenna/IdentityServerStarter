# IdentityServer 7 — User Management Admin Specification

## 1. Overview

This specification defines the user/identity management pages for the IdentityServer 7 admin web application. It is a companion to the existing admin spec covering Clients, API Scopes, API Resources, Identity Resources, and Persisted Grants.

Follow all conventions, patterns, and architecture already established in the existing admin site codebase. Inspect the project to determine the identity model, base page classes, layout structure, notification system, confirmation dialog patterns, and authentication setup. Apply those same patterns to the pages defined here.

### Scope

- Admin-side management of ASP.NET Identity users and their IdentityServer operational data
- Read, create, update, and delete operations as defined per entity below

### Out of Scope

- Audit logging (separate spec)
- User self-service operations (registration, password change, 2FA setup by end user)
- External login add/remove by admin (user-initiated only; admin view is read-only)
- Recovery code regeneration (user-managed)
- Force password change on next login (not built-in to ASP.NET Identity)
- User search/filtering (future enhancement — simple table for now)
- Bulk operations
- GDPR/compliance features (future spec)

---

## 2. Authorisation Model

### Claims Structure

Define permission claims following the existing `admin:{entity}` pattern with values `read`, `write`, `delete`:

| Claim Type | Values | Controls |
|------------|--------|----------|
| `admin:users` | `read`, `write`, `delete` | User account CRUD, profile, security settings, account creation/deletion |
| `admin:user_claims` | `read`, `write`, `delete` | User claim management |
| `admin:user_roles` | `read`, `write`, `delete` | User role assignment management |
| `admin:user_grants` | `read`, `delete` | Persisted grants, consent grants (view/revoke) |
| `admin:user_sessions` | `read`, `delete` | Server-side sessions (view/end) |

External logins are read-only, governed by `admin:users=read` — no separate claim needed.

### Policy Definitions

Register policies following the existing pattern in the project. Higher permissions imply lower ones (e.g., `write` implies `read`):

| Policy Name | Required Claim Values |
|-------------|----------------------|
| `UsersRead` | `admin:users` = `read`, `write`, or `delete` |
| `UsersWrite` | `admin:users` = `write` or `delete` |
| `UsersDelete` | `admin:users` = `delete` |
| `UserClaimsRead` | `admin:user_claims` = `read`, `write`, or `delete` |
| `UserClaimsWrite` | `admin:user_claims` = `write` or `delete` |
| `UserClaimsDelete` | `admin:user_claims` = `delete` |
| `UserRolesRead` | `admin:user_roles` = `read`, `write`, or `delete` |
| `UserRolesWrite` | `admin:user_roles` = `write` or `delete` |
| `UserRolesDelete` | `admin:user_roles` = `delete` |
| `UserGrantsRead` | `admin:user_grants` = `read` or `delete` |
| `UserGrantsDelete` | `admin:user_grants` = `delete` |
| `UserSessionsRead` | `admin:user_sessions` = `read` or `delete` |
| `UserSessionsDelete` | `admin:user_sessions` = `delete` |

### Authorisation Enforcement

Apply at three levels, matching existing patterns in the codebase:

1. **Page-level**: `[Authorize(Policy = "...")]` on the PageModel
2. **UI-level**: Conditionally show/hide action buttons and tabs based on claims via `IAuthorizationService`
3. **Handler-level**: Verify policy in each `OnPost*` handler before executing

Tab visibility on the user detail page:

| Tab | Required Claim |
|-----|---------------|
| Profile | `admin:users=read` |
| Security | `admin:users=read` |
| Claims | `admin:user_claims=read` |
| Roles | `admin:user_roles=read` |
| External Logins | `admin:users=read` |
| Grants & Sessions | `admin:user_grants=read` or `admin:user_sessions=read` |

---

## 3. Information Architecture

### Navigation

Add "Users" as a top-level sidebar item alongside the existing sections.

### URL Routing

| Page | Route | Methods |
|------|-------|---------|
| User List | `/Users` | GET |
| Create User | `/Users/Create` | GET, POST |
| User Detail — Profile | `/Users/{userId}` | GET, POST |
| User Detail — Security | `/Users/{userId}/Security` | GET, POST |
| User Detail — Claims | `/Users/{userId}/Claims` | GET, POST |
| User Detail — Roles | `/Users/{userId}/Roles` | GET, POST |
| User Detail — External Logins | `/Users/{userId}/ExternalLogins` | GET |
| User Detail — Grants & Sessions | `/Users/{userId}/GrantsSessions` | GET, POST |
| Delete User | `/Users/{userId}` | POST (handler: `OnPostDeleteAsync`) |

The `{userId}` is the user's `Id` property (string, GUID format).

### Breadcrumbs

| Page | Breadcrumb |
|------|------------|
| User List | Home → Users |
| Create User | Home → Users → Create |
| User Detail (any tab) | Home → Users → {Username} → {Tab Name} |

### User Detail Page Structure

Each tab is a separate Razor Page sharing a common layout partial for the tab bar. The default/landing tab is **Profile**. Only render tabs the admin has claims to see.

---

## 4. Entity Specifications

### 4.1 User Account

#### User Stories

---

##### List: Users

**Task**: Admin views all user accounts in a table

**Preconditions**:
- Required claims: `admin:users=read`

**Steps**:
1. Admin clicks "Users" in the sidebar
2. System loads all users via `UserManager.Users`
3. System renders table with columns: Username, Email, Email Confirmed, Lockout Status, 2FA Enabled

**Acceptance Criteria**:
- [ ] Table displays all users with correct data
- [ ] Email Confirmed and 2FA Enabled show as tick/cross icons
- [ ] Lockout Status shows "Active", "Locked Out (until {date})", or "Disabled" (when `LockoutEnd == DateTimeOffset.MaxValue`)
- [ ] Each row links to user detail page
- [ ] "Create User" button visible only with `admin:users=write`
- [ ] Empty state: "No user accounts found"

---

##### Create: User

**Task**: Admin creates a new user account

**Preconditions**:
- Required claims: `admin:users=write`

**Steps**:
1. Admin clicks "Create User" from User List
2. Admin fills in Username, Email, and optionally a Password
3. Admin clicks "Create"
4. System validates and calls `UserManager.CreateAsync(user)` or `CreateAsync(user, password)` if password provided
5. On success, redirect to user detail page with success toast
6. On failure, display `IdentityResult.Errors` mapped to fields

**Acceptance Criteria**:
- [ ] Username and Email are required
- [ ] Password is optional; if blank, user created without password
- [ ] Successful creation redirects to user detail
- [ ] `IdentityResult` errors displayed inline against relevant fields
- [ ] Duplicate username/email shows user-friendly message

**Validation Rules**:

| Field | Rule | Error Message |
|-------|------|---------------|
| Username | Required | "Username is required" |
| Username | Max 256 characters | "Username must not exceed 256 characters" |
| Username | Unique | "This username is already taken" |
| Username | Allowed characters per `UserOptions.AllowedUserNameCharacters` | "Username contains invalid characters" |
| Email | Required, valid email format | "A valid email address is required" |
| Email | Max 256 characters | "Email must not exceed 256 characters" |
| Email | Unique | "This email is already in use" |
| Password | If provided: meets `PasswordOptions` requirements | Mapped from `IdentityResult.Errors` |

**Edge Cases**:
- Password left blank: User created without password; `HasPasswordAsync` returns `false`
- `CreateAsync` returns errors: Map each `IdentityError.Code` to form field; unknown errors go in summary

---

##### View/Update: User Profile

**Task**: Admin views and edits a user's profile information

**Preconditions**:
- Required claims: `admin:users=read` (view), `admin:users=write` (edit)
- User exists

**Steps**:
1. Admin navigates to Users → selects user → Profile tab (default)
2. System loads user via `UserManager.FindByIdAsync`
3. System displays: User ID (read-only), Username, Email, Email Confirmed toggle, Phone Number, Phone Confirmed toggle
4. Admin modifies fields and clicks "Save"
5. System validates and updates:
   - Username via `UserManager.SetUserNameAsync`
   - Email via `UserManager.SetEmailAsync`
   - Phone via `UserManager.SetPhoneNumberAsync`
   - Confirmed toggles via direct property set + `UpdateAsync`
6. Success toast displayed

**Acceptance Criteria**:
- [ ] Fields editable with `write` claim; read-only with only `read` claim (Save button hidden)
- [ ] Changing email resets `EmailConfirmed` to `false` unless admin explicitly re-confirms
- [ ] Changing phone resets `PhoneNumberConfirmed` to `false`
- [ ] Concurrency conflict shows error with reload option
- [ ] User not found → redirect to User List with error toast

**Validation Rules**:

| Field | Rule | Error Message |
|-------|------|---------------|
| Username | Required | "Username is required" |
| Username | Max 256 characters | "Username must not exceed 256 characters" |
| Username | Unique | "This username is already taken" |
| Username | Allowed characters only | "Username contains invalid characters" |
| Email | Required, valid email format | "A valid email address is required" |
| Email | Max 256 characters | "Email must not exceed 256 characters" |
| Email | Unique | "This email is already in use" |
| Phone Number | Optional, max 30 characters | "Phone number must not exceed 30 characters" |

**Edge Cases**:
- Email changed to current value: No-op, no confirmation reset, success shown
- Phone cleared: `SetPhoneNumberAsync(user, null)`, `PhoneNumberConfirmed` reset to `false`
- Email Confirmed toggled without email change: Allowed
- Concurrent edit: `UpdateAsync` returns concurrency failure → "This record was modified by another user. Please reload and try again."

---

##### Update: User Security — Password Reset

**Task**: Admin resets a user's password

**Preconditions**:
- Required claims: `admin:users=write`

**Steps**:
1. Admin navigates to Security tab
2. System displays whether user has a password (via `HasPasswordAsync`)
3. Admin enters new password and clicks "Reset Password"
4. Confirmation dialog: "This will replace the user's current password. Continue?"
5. On confirm: `RemovePasswordAsync` then `AddPasswordAsync` (skip remove if user has no password)
6. Success toast

**Acceptance Criteria**:
- [ ] "Has Password" indicator shown
- [ ] New password field visible only with `write` claim
- [ ] Confirmation dialog before reset
- [ ] Password validated against `PasswordOptions`; `IdentityResult` errors displayed
- [ ] If user has no password, only `AddPasswordAsync` called

**Validation Rules**:

| Field | Rule | Error Message |
|-------|------|---------------|
| New Password | Required | "A password is required" |
| New Password | Meets `PasswordOptions` | Mapped from `IdentityResult.Errors` |

**Edge Cases**:
- `RemovePasswordAsync` fails: Display error, do not proceed to `AddPasswordAsync`
- `AddPasswordAsync` fails: Display errors; warn that user now has no password; prompt retry

---

##### Update: User Security — Lockout Controls

**Task**: Admin manages a user's lockout state

**Preconditions**:
- Required claims: `admin:users=read` (view), `admin:users=write` (modify)

**Steps**:
1. Admin navigates to Security tab
2. System displays:
   - Account Status: "Active", "Locked Out (until {date})", or "Disabled"
   - Lockout Enabled toggle
   - Failed Access Count with "Reset" button
   - Disable/Enable Account button
3. Actions:
   - **Disable Account**: Confirmation dialog → `SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue)`
   - **Enable Account**: `SetLockoutEndDateAsync(user, null)` (no confirmation needed)
   - **Clear Lockout**: `SetLockoutEndDateAsync(user, null)` + `ResetAccessFailedCountAsync`
   - **Toggle Lockout Enabled**: `SetLockoutEnabledAsync`
   - **Reset Failed Count**: `ResetAccessFailedCountAsync`

**Acceptance Criteria**:
- [ ] Current lockout state displayed accurately
- [ ] Disable Account requires confirmation
- [ ] All modify actions require `write` claim
- [ ] Self-disable triggers self-protection warning (§5)

**Edge Cases**:
- Admin disables own account: Self-protection warning (§5)
- Disabling `LockoutEnabled` while user is locked out: Clear lockout end date to avoid inconsistent state

---

##### Update: User Security — Two-Factor Authentication

**Task**: Admin manages a user's 2FA settings

**Preconditions**:
- Required claims: `admin:users=read` (view), `admin:users=write` (modify)

**Steps**:
1. Admin navigates to Security tab
2. System displays:
   - 2FA Enabled toggle
   - Registered Providers list (via `GetValidTwoFactorProvidersAsync`) — read-only
   - "Reset Authenticator Key" button
3. Actions:
   - **Toggle 2FA**: `SetTwoFactorEnabledAsync` (confirmation required when disabling)
   - **Reset Authenticator Key**: Confirmation dialog → `ResetAuthenticatorKeyAsync`

**Acceptance Criteria**:
- [ ] 2FA status shown as toggle
- [ ] Registered providers listed (read-only); "None" if empty
- [ ] Disabling 2FA requires confirmation: "This will disable two-factor authentication for this user. Continue?"
- [ ] Resetting authenticator requires confirmation: "This will invalidate the user's current authenticator app. They will need to re-enrol. Continue?"
- [ ] All actions require `write` claim

---

##### Action: Force Sign-Out

**Task**: Admin forces a user to sign out of all sessions

**Preconditions**:
- Required claims: `admin:users=write`

**Steps**:
1. Admin clicks "Force Sign-Out" on Security tab
2. Confirmation dialog: "This will invalidate all active sessions for this user, forcing them to sign in again. Continue?"
3. On confirm: `UpdateSecurityStampAsync`
4. Success toast

**Acceptance Criteria**:
- [ ] Confirmation dialog shown
- [ ] `UpdateSecurityStampAsync` called on confirm
- [ ] Self-affecting: Self-protection warning (§5)

---

##### Delete: User

**Task**: Admin deletes a user account

**Preconditions**:
- Required claims: `admin:users=delete`

**Steps**:
1. Admin clicks "Delete User" (available on Profile tab or User List)
2. Confirmation dialog: "This will permanently delete the user account for '{Username}'. This action cannot be undone. Continue?"
3. On confirm: `UserManager.DeleteAsync`
4. Redirect to User List with success toast

**Acceptance Criteria**:
- [ ] Confirmation dialog shows username
- [ ] Redirect to User List on success
- [ ] `IdentityResult` errors displayed on failure
- [ ] Delete button visible only with `delete` claim
- [ ] Self-deletion blocked entirely (§5)

**Edge Cases**:
- Self-deletion: Blocked — "You cannot delete your own account"
- User already deleted: Error toast — "User not found", redirect to list

---

### 4.2 User Claims

#### User Stories

---

##### List: User Claims

**Task**: Admin views all claims for a user

**Preconditions**:
- Required claims: `admin:user_claims=read`

**Steps**:
1. Admin navigates to Claims tab
2. System loads via `UserManager.GetClaimsAsync`
3. Table displayed: Claim Type, Claim Value, Actions

**Acceptance Criteria**:
- [ ] All claims displayed
- [ ] Add button visible only with `write` claim
- [ ] Edit/Remove actions visible per appropriate claims
- [ ] Empty state: "No claims assigned to this user"

---

##### Add: User Claim(s)

**Task**: Admin adds one or more claims

**Preconditions**:
- Required claims: `admin:user_claims=write`

**Steps**:
1. Admin clicks "Add Claim"
2. Form with Claim Type and Claim Value, plus "Add Another" for batch entry
3. Admin clicks "Save"
4. System calls `AddClaimAsync` / `AddClaimsAsync`
5. Success toast, refresh list

**Acceptance Criteria**:
- [ ] At least one claim type/value pair required
- [ ] Multiple claims addable via "Add Another"
- [ ] Duplicate claim warning shown but not blocked (ASP.NET Identity allows duplicates)

**Validation Rules**:

| Field | Rule | Error Message |
|-------|------|---------------|
| Claim Type | Required, max 256 chars | "Claim type is required" / "Claim type must not exceed 256 characters" |
| Claim Value | Optional, max 1024 chars | "Claim value must not exceed 1024 characters" |

---

##### Remove: User Claim(s)

**Task**: Admin removes one or more claims

**Preconditions**:
- Required claims: `admin:user_claims=delete`

**Steps**:
1. Admin selects claims via checkboxes
2. Clicks "Remove Selected"
3. Confirmation dialog: "Remove {n} claim(s) from this user?"
4. System calls `RemoveClaimsAsync`
5. Success toast, refresh list

**Acceptance Criteria**:
- [ ] Checkbox selection for bulk removal
- [ ] Confirmation dialog with count
- [ ] Self-protection warning if removing own `admin:*` claims (§5)

---

##### Replace: User Claim

**Task**: Admin replaces a claim (atomic swap)

**Preconditions**:
- Required claims: `admin:user_claims=write`

**Steps**:
1. Admin clicks "Edit" on a claim row
2. Inline edit or modal with current values pre-filled
3. Admin modifies type and/or value, clicks "Save"
4. System calls `ReplaceClaimAsync(user, oldClaim, newClaim)`
5. Success toast

**Acceptance Criteria**:
- [ ] Current values pre-filled
- [ ] Both type and value editable
- [ ] Validation rules apply to new values
- [ ] Self-protection warning if replacing own admin claims with weaker values (§5)

---

### 4.3 User Roles

#### User Stories

---

##### List: User Roles

**Task**: Admin views roles assigned to a user

**Preconditions**:
- Required claims: `admin:user_roles=read`

**Steps**:
1. Admin navigates to Roles tab
2. System loads via `UserManager.GetRolesAsync`
3. Table of assigned roles with Remove action

**Acceptance Criteria**:
- [ ] All assigned roles displayed
- [ ] Add button visible with `write` claim; Remove with `delete` claim
- [ ] Empty state: "This user is not assigned to any roles"

---

##### Add: User to Role(s)

**Task**: Admin assigns a user to roles

**Preconditions**:
- Required claims: `admin:user_roles=write`

**Steps**:
1. Admin clicks "Add to Role"
2. Multi-select of available roles (excluding already-assigned)
3. Admin selects and clicks "Save"
4. System calls `AddToRolesAsync`
5. Success toast, refresh list

**Acceptance Criteria**:
- [ ] Only unassigned roles shown
- [ ] Multiple selection supported
- [ ] Button disabled when user is in all available roles (tooltip: "User is in all available roles")

**Validation Rules**:

| Field | Rule | Error Message |
|-------|------|---------------|
| Role selection | At least one required | "Select at least one role" |
| Role | Must exist | "Role '{name}' does not exist" |
| Role | User not already in role | "User is already in role '{name}'" |

---

##### Remove: User from Role(s)

**Task**: Admin removes a user from roles

**Preconditions**:
- Required claims: `admin:user_roles=delete`

**Steps**:
1. Admin selects roles via checkboxes
2. Clicks "Remove Selected"
3. Confirmation dialog: "Remove this user from {n} role(s)?"
4. System calls `RemoveFromRolesAsync`
5. Success toast, refresh list

**Acceptance Criteria**:
- [ ] Confirmation dialog with count
- [ ] Self-protection warning if removing self from admin role (§5)

---

### 4.4 External Logins

##### View: External Logins

**Task**: Admin views external login providers linked to a user

**Preconditions**:
- Required claims: `admin:users=read`

**Steps**:
1. Admin navigates to External Logins tab
2. System loads via `UserManager.GetLoginsAsync`
3. Read-only table: Provider Name, Provider Key

**Acceptance Criteria**:
- [ ] `ProviderDisplayName` shown (fallback to `LoginProvider` if null)
- [ ] No add/remove actions
- [ ] Empty state: "No external logins linked to this user"

---

### 4.5 Grants & Sessions

#### User Stories

---

##### View/Revoke: Persisted Grants

**Task**: Admin views and revokes persisted grants for a user

**Preconditions**:
- Required claims: `admin:user_grants=read` (view), `admin:user_grants=delete` (revoke)

**Steps**:
1. Admin navigates to Grants & Sessions tab
2. System loads via `IPersistedGrantStore.GetAllAsync` filtered by `SubjectId = user.Id`
3. Table: Type, Client ID, Created, Expiration, Status (active/expired/consumed)
4. "Revoke" per row and "Revoke All" button (with `delete` claim)
5. Confirmation dialogs:
   - Single: "Revoke this {type} grant for client '{ClientId}'?"
   - All: "Revoke all grants for this user? This will sign them out of all clients and revoke all tokens."
6. System calls `RemoveAsync(key)` or `RemoveAllAsync(filter)`
7. Success toast, refresh list

**Acceptance Criteria**:
- [ ] Grants displayed with type and status
- [ ] `Data` field never displayed
- [ ] Expired/consumed grants visually distinguished
- [ ] Consent grants clearly labelled and individually revocable
- [ ] Empty state: "No grants found for this user"

**Edge Cases**:
- Grant already revoked by another admin: Info toast — "Grant was already removed", refresh list

---

##### View/End: Server-Side Sessions

**Task**: Admin views and ends server-side sessions for a user

**Preconditions**:
- Required claims: `admin:user_sessions=read` (view), `admin:user_sessions=delete` (end)
- Server-side session management enabled in IdentityServer config

**Steps**:
1. Admin views Sessions section of Grants & Sessions tab
2. System loads via `IServerSideSessionStore.GetSessionsAsync` filtered by `SubjectId`
3. Table: Session ID, Display Name, Created, Last Renewed, Expires
4. "End Session" per row and "End All Sessions" button (with `delete` claim)
5. Confirmation dialogs:
   - Single: "End this session? The user will be signed out of this session."
   - All: "End all sessions for this user? They will be signed out everywhere."
6. System calls `DeleteSessionAsync(key)` or `DeleteSessionsAsync(filter)`
7. Success toast, refresh list

**Acceptance Criteria**:
- [ ] Sessions displayed with timing info
- [ ] If server-side sessions not enabled: "Server-side session management is not enabled"
- [ ] Empty state: "No active sessions for this user"
- [ ] Ending own session triggers self-protection warning (§5)

---

## 5. Self-Protection Rules

Self-detection: Compare the edited user's `Id` against the current admin's `Id` from the `sub` claim.

### Warning Scenarios (Allow with Confirmation)

| Scenario | Dialog Text |
|----------|-------------|
| Disable own account | "You are about to disable your own account. You will be signed out and unable to access the admin site. Continue?" |
| Remove own admin claims (`admin:*`) | "Removing this claim will revoke your access to {section}. Continue?" |
| Remove self from admin role | "Removing yourself from this role may revoke your admin access. Continue?" |
| Force sign-out on self | "This will end your current session. You will need to sign in again. Continue?" |
| End own session | "This will end your current session. You will need to sign in again. Continue?" |
| Revoke own grants | "This will revoke your own grants. You may need to re-authenticate with affected clients. Continue?" |

Warning dialogs must default focus to "Cancel".

### Blocked Scenarios (Prevent Entirely)

| Scenario | Error Message |
|----------|---------------|
| Delete own account | "You cannot delete your own account" |

Hide the action button; return `400 Bad Request` if POST handler invoked directly.

---

## 6. Cross-Cutting Concerns

### Error Handling

Follow the existing error handling patterns in the codebase. Additionally:

- **`IdentityResult` errors**: Map `IdentityError.Code` to user-friendly messages:

| Error Code | Message |
|------------|---------|
| `DuplicateUserName` | "This username is already taken" |
| `DuplicateEmail` | "This email is already in use" |
| `InvalidEmail` | "The email address format is invalid" |
| `InvalidUserName` | "Username contains invalid characters" |
| `PasswordTooShort` | "Password must be at least {n} characters" |
| `PasswordRequiresDigit` | "Password must contain at least one digit" |
| `PasswordRequiresUpper` | "Password must contain at least one uppercase letter" |
| `PasswordRequiresLower` | "Password must contain at least one lowercase letter" |
| `PasswordRequiresNonAlphanumeric` | "Password must contain at least one special character" |
| `PasswordRequiresUniqueChars` | "Password must contain at least {n} unique characters" |
| `UserAlreadyInRole` | "User is already in role '{role}'" |
| `UserNotInRole` | "User is not in role '{role}'" |
| (Unknown) | "An error occurred: {IdentityError.Description}" |

- **Concurrency conflicts**: "This record was modified by another user. Please reload and try again." with Reload button.

### Confirmation Dialogs

| Operation | Required |
|-----------|----------|
| Delete user | Yes |
| Disable account | Yes |
| Enable account | No |
| Reset password | Yes |
| Force sign-out | Yes |
| Disable 2FA | Yes |
| Reset authenticator key | Yes |
| Remove claim(s) | Yes |
| Remove from role(s) | Yes |
| Revoke grant (single/all) | Yes |
| End session (single/all) | Yes |
| Any self-affecting operation | Yes (see §5) |

---

## Appendix A: Method Reference

| Admin Operation | Method(s) |
|----------------|-----------|
| List users | `UserManager.Users` |
| Find user | `UserManager.FindByIdAsync(string)` |
| Create user | `UserManager.CreateAsync(user)` / `CreateAsync(user, password)` |
| Delete user | `UserManager.DeleteAsync(user)` |
| Update user | `UserManager.UpdateAsync(user)` |
| Set username | `UserManager.SetUserNameAsync(user, string)` |
| Set email | `UserManager.SetEmailAsync(user, string)` |
| Set phone | `UserManager.SetPhoneNumberAsync(user, string?)` |
| Check password | `UserManager.HasPasswordAsync(user)` |
| Remove password | `UserManager.RemovePasswordAsync(user)` |
| Add password | `UserManager.AddPasswordAsync(user, string)` |
| Set lockout end | `UserManager.SetLockoutEndDateAsync(user, DateTimeOffset?)` |
| Set lockout enabled | `UserManager.SetLockoutEnabledAsync(user, bool)` |
| Reset failed count | `UserManager.ResetAccessFailedCountAsync(user)` |
| Get 2FA enabled | `UserManager.GetTwoFactorEnabledAsync(user)` |
| Set 2FA enabled | `UserManager.SetTwoFactorEnabledAsync(user, bool)` |
| Get 2FA providers | `UserManager.GetValidTwoFactorProvidersAsync(user)` |
| Reset authenticator | `UserManager.ResetAuthenticatorKeyAsync(user)` |
| Force sign-out | `UserManager.UpdateSecurityStampAsync(user)` |
| Get claims | `UserManager.GetClaimsAsync(user)` |
| Add claim(s) | `UserManager.AddClaimAsync` / `AddClaimsAsync` |
| Remove claim(s) | `UserManager.RemoveClaimAsync` / `RemoveClaimsAsync` |
| Replace claim | `UserManager.ReplaceClaimAsync(user, oldClaim, newClaim)` |
| Get roles | `UserManager.GetRolesAsync(user)` |
| Add to role(s) | `UserManager.AddToRoleAsync` / `AddToRolesAsync` |
| Remove from role(s) | `UserManager.RemoveFromRoleAsync` / `RemoveFromRolesAsync` |
| Check role | `UserManager.IsInRoleAsync(user, string)` |
| Get external logins | `UserManager.GetLoginsAsync(user)` |
| List grants | `IPersistedGrantStore.GetAllAsync(PersistedGrantFilter)` |
| Remove grant | `IPersistedGrantStore.RemoveAsync(string key)` |
| Remove all grants | `IPersistedGrantStore.RemoveAllAsync(PersistedGrantFilter)` |
| List sessions | `IServerSideSessionStore.GetSessionsAsync(SessionFilter)` |
| Remove session | `IServerSideSessionStore.DeleteSessionAsync(string key)` |
| Remove all sessions | `IServerSideSessionStore.DeleteSessionsAsync(SessionFilter)` |
