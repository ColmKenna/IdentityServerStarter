# Setup Plan: `Products` (Razor Pages) secured by `IdentityServerAspNetIdentity`

**Environment:** Duende IdentityServer v7.4.6 · net10.0 · Authority: `https://localhost:5001`
**Authentication Flow:** Authorization Code + PKCE

---

## Step 1: Register the New Client in IdentityServer

**Where:** `src/IdentityServerAspNetIdentity/Config.cs` — add a new `Client` entry to the `Clients` list (after the existing `web` client, line 89).

**Note:** Because the client store uses EF Core `ConfigurationDbContext` seeded from `Config.Clients` on first run, you have two options:
- **Option A (clean DB):** Add the client to `Config.Clients` and re-seed (drop and recreate the database, or clear the `Clients` table).
- **Option B (existing DB):** Add the client directly to the database via a migration seed or the admin UI, AND add it to `Config.Clients` so future seeds include it.

**Client properties:**

| Property | Value |
|---|---|
| `ClientId` | `products` |
| `ClientName` | `Products App` |
| `ClientSecrets` | One secret — generate via `dotnet user-secrets` on the Products project; hash with `.Sha256()` in Config.cs |
| `AllowedGrantTypes` | `GrantTypes.Code` |
| `RequirePkce` | `true` (default in v7, but set explicitly for clarity) |
| `RedirectUris` | `https://localhost:5003/signin-oidc` |
| `PostLogoutRedirectUris` | `https://localhost:5003/signout-callback-oidc` |
| `AllowOfflineAccess` | `true` |
| `AllowedScopes` | `openid`, `profile`, `api1`, `products` |
| `AlwaysIncludeUserClaimsInIdToken` | `true` (matches existing `web` client pattern) |

---

## Step 2: Create the New ASP.NET Core Project

- **Template:** `dotnet new webapp` (Razor Pages template)
- **Framework:** `net10.0`
- **Project name:** `Products`
- **Location:** Create in `src/Products/` (alongside existing `src/WebClient/`, `src/Api/`, etc.)
- **Add to solution:** `dotnet sln Quickstart.sln add src/Products/Products.csproj` — nest under the `src` solution folder

---

## Step 3: Add Required NuGet Packages

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.AspNetCore.Authentication.OpenIdConnect` | `10.0.3` | OIDC handler (matches WebClient) |

No other packages required. The Razor Pages template includes everything else.

---

## Step 4: Configure Authentication in the New Project

In `Program.cs`, configure the following services and middleware:

**Services:**
1. Set `JwtSecurityTokenHandler.DefaultMapInboundClaims` to `false` (prevents claim type remapping)
2. Add authentication with:
   - `DefaultScheme` = `"Cookies"`
   - `DefaultChallengeScheme` = `"oidc"`
3. Add cookie handler with scheme `"Cookies"`
4. Add OpenID Connect handler `"oidc"` with:
   - `Authority` = read from `Configuration["Oidc:Authority"]`, fallback `https://localhost:5001`
   - `ClientId` = read from `Configuration["Oidc:ClientId"]`, fallback `products`
   - `ClientSecret` = read from `Configuration["Oidc:ClientSecret"]` (no hardcoded fallback — throw if missing)
   - `ResponseType` = `"code"`
   - `SaveTokens` = `true`
   - Clear default scopes, then add: `openid`, `profile`, `offline_access`, `api1`, `products`
   - `GetClaimsFromUserInfoEndpoint` = `true`

**Middleware pipeline (in order):**
1. `UseHttpsRedirection`
2. `UseStaticFiles`
3. `UseRouting`
4. `UseAuthentication`
5. `UseAuthorization`
6. `MapRazorPages().RequireAuthorization()` — all pages require auth by default

This mirrors the existing `src/WebClient/Program.cs` pattern exactly.

---

## Step 5: Configure HTTPS and URLs

- **Project URL:** `https://localhost:5003`
- Set in `Properties/launchSettings.json` with a profile named `Products`
- **CORS:** Not required — the Products app is a server-side confidential client that redirects to IdentityServer (no browser-based cross-origin calls)

---

## Step 6: Configure App Settings

**`appsettings.json`:**
- No secrets — only non-sensitive defaults if desired

**`appsettings.Development.json`:**
- `Oidc:Authority` = `https://localhost:5001`
- `Oidc:ClientId` = `products`

**Client secret:** Store via `dotnet user-secrets set "Oidc:ClientSecret" "your-secret-here"` in the Products project directory. The same plaintext value must be SHA256-hashed in the IdentityServer `Config.cs` client definition.

---

## Step 7: Add Authorization to Pages / Endpoints

- `MapRazorPages().RequireAuthorization()` applies a global authorization requirement (same as WebClient)
- For product-specific authorization, add policies checking the `canViewProducts` and `canAmendProduct` claims (these come from the `products` identity resource scope)
- Pages that only display products: authorize with a policy requiring the `canViewProducts` claim
- Pages that edit products: authorize with a policy requiring the `canAmendProduct` claim
- To allow anonymous access to specific pages (e.g. a landing page), use the `[AllowAnonymous]` attribute or `PageConventions.AllowAnonymousToPage()`

---

## Step 8: Verification Steps

1. **Ensure the database is seeded** with the new `products` client (either re-seed or insert manually)
2. **Start IdentityServer** (`https://localhost:5001`)
3. **Start the Products project** (`https://localhost:5003`)
4. Navigate to `https://localhost:5003` — expect redirect to IdentityServer login
5. Log in as `alice` / `Pass123$`
6. Verify redirect back to `https://localhost:5003` with an authenticated session
7. Verify that the `canViewProducts` and `canAmendProduct` claims are present (add a debug page that displays `User.Claims` to confirm)
8. Test logout — navigate to a logout endpoint, verify redirect to IdentityServer's end session page, then back to `https://localhost:5003/signout-callback-oidc`

---

## Completed Pre-requisites (IdentityServer side)

The following changes have already been applied:

- **Client registered** in `Config.cs` — Client ID: `products`, secret: `productsrazor` (SHA256-hashed)
- **Alice** — claims added: `canViewProducts`, `canAmendProduct` (full access)
- **Bob** — claims added: `canViewProducts`, `canAmendProduct` (full access)
- **Charlie** (new user) — `CharlieDay@email.com` / `Pass123$` — claim: `canViewProducts` only (view-only)

## Missing Information Report

| Item | Placeholder / Assumption | How to Resolve |
|---|---|---|
| Client secret in Products project | Secret is `productsrazor` — needs to be stored in Products project | Run `dotnet user-secrets set "Oidc:ClientSecret" "productsrazor"` in `src/Products/` |
| Database re-seed | New client and user won't appear in existing DB | Drop and re-create the DB, or manually insert the new client/user/claims |

---

## Plan Summary

| Step | Action | Key Details |
|---|---|---|
| 1 | Register client in IdentityServer | Client ID: `products` · Grant type: Code + PKCE · Scopes: `openid`, `profile`, `api1`, `products` · Location: `Config.cs` + DB re-seed |
| 2 | Create project | `dotnet new webapp -n Products -f net10.0` in `src/` · Add to `Quickstart.sln` |
| 3 | Add NuGet packages | `Microsoft.AspNetCore.Authentication.OpenIdConnect` v10.0.3 |
| 4 | Configure authentication | Authority: `https://localhost:5001` · Scheme: Cookies + OIDC · Scopes: `openid`, `profile`, `offline_access`, `api1`, `products` |
| 5 | Configure HTTPS and URLs | `https://localhost:5003` · No CORS needed |
| 6 | Configure app settings | `appsettings.Development.json` + `dotnet user-secrets` for client secret |
| 7 | Add authorization | Global `RequireAuthorization()` + claim-based policies for `canViewProducts` / `canAmendProduct` |
| 8 | Verify | Start both projects → login redirect → authenticated session → check claims → logout |

**Unresolved Items:** 2 — see Missing Information Report above.
