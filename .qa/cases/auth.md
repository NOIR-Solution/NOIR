# Auth — Test Cases

> Pages: /login, /forgot-password, /forgot-password/verify, /reset-password, /portal/dashboard, /portal/settings/personal | Last updated: 2026-04-03 | Git ref: f6f5cd3
> Total: 48 cases | P0: 3 | P1: 22 | P2: 17 | P3: 6

---

## Page: Login (`/login`)

### Happy Path

#### TC-AUTH-001: Login with valid tenant admin credentials [P1] [smoke]
- **Pre**: User `admin@noir.local` / `123qwe` exists, user is logged out
- **Steps**:
  1. Navigate to `/login`
  2. Enter email `admin@noir.local`
  3. Enter password `123qwe`
  4. Click "Sign In" button
- **Expected**: Redirected to `/portal` (dashboard). Sidebar visible with user avatar.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ User name shown in sidebar | ☐ Dashboard widgets load

#### TC-AUTH-002: Login with platform admin credentials [P1] [smoke]
- **Pre**: User `platform@noir.local` / `123qwe` exists
- **Steps**:
  1. Navigate to `/login`
  2. Enter email `platform@noir.local`
  3. Enter password `123qwe`
  4. Click "Sign In"
- **Expected**: Redirected to `/portal`. Platform-only sidebar items visible (Tenants, Platform Settings, Developer Logs).
- **Data**: ☐ Platform sidebar items present | ☐ Tenant-only items hidden

#### TC-AUTH-003: Multi-tenant organization selection step [P1] [smoke]
- **Pre**: User belongs to multiple tenants
- **Steps**:
  1. Enter valid multi-tenant user credentials
  2. Click "Sign In"
  3. Organization selection step appears with tenant list
  4. Select a tenant
- **Expected**: Step 2 shows animated transition to organization picker. Selecting tenant completes login and redirects to `/portal`.
- **Data**: ☐ All user tenants shown | ☐ Back button returns to credentials step

#### TC-AUTH-004: Show/hide password toggle [P2]
- **Pre**: On login page
- **Steps**:
  1. Type password in password field
  2. Click eye icon to show password
  3. Click eye-off icon to hide password
- **Expected**: Password toggles between masked (`type=password`) and visible (`type=text`). Icon switches between Eye and EyeOff.

#### TC-AUTH-005: Language switcher on login page [P1] [i18n]
- **Pre**: On login page, default language EN
- **Steps**:
  1. Click language dropdown (top-right)
  2. Select "Tieng Viet"
- **Expected**: All login page text switches to Vietnamese. Labels, placeholders, button text, decorative panel text all update.
- **Visual**: ☐ Light | ☐ Dark

#### TC-AUTH-006: Theme toggle on login page [P1] [visual]
- **Pre**: On login page, light theme
- **Steps**:
  1. Click theme toggle (top-right, next to language switcher)
  2. Observe page appearance
- **Expected**: Login form, background, and card switch to dark theme. Decorative right panel gradient unchanged. Orbital logo adapts.
- **Visual**: ☐ Light | ☐ Dark

#### TC-AUTH-007: Forgot password link navigates correctly [P2]
- **Pre**: On login page
- **Steps**:
  1. Click "Forgot Password?" link below password field
- **Expected**: Navigates to `/forgot-password` page with email input and back-to-login link.

#### TC-AUTH-008: returnUrl redirect after login [P2] [security]
- **Pre**: User is logged out
- **Steps**:
  1. Navigate to `/login?returnUrl=/portal/users`
  2. Login with valid credentials
- **Expected**: After login, redirected to `/portal/users` (not `/portal`).
- **Data**: ☐ Deep link preserved

### Edge Cases

#### TC-AUTH-009: Login with wrong password [P1] [regression]
- **Pre**: Valid email, wrong password
- **Steps**:
  1. Enter email `admin@noir.local`
  2. Enter wrong password `wrongpassword`
  3. Click "Sign In"
- **Expected**: Server error banner appears with "Invalid credentials" message. Form remains on credentials step. Email field retains value.

#### TC-AUTH-010: Login with empty email [P2] [edge-case]
- **Pre**: On login page
- **Steps**:
  1. Leave email field empty
  2. Enter any password
  3. Click "Sign In"
- **Expected**: Client-side validation shows "Email is required" under email field. No API call made.

#### TC-AUTH-011: Login with invalid email format [P2] [edge-case]
- **Pre**: On login page
- **Steps**:
  1. Enter "not-an-email" in email field
  2. Enter any password
  3. Click "Sign In"
- **Expected**: Client-side validation shows "Please enter a valid email address". No API call.

#### TC-AUTH-012: Login with empty password [P2] [edge-case]
- **Pre**: On login page
- **Steps**:
  1. Enter valid email
  2. Leave password empty
  3. Click "Sign In"
- **Expected**: Client-side validation shows "Password is required" under password field.

#### TC-AUTH-013: Login button disabled during submission [P2]
- **Pre**: On login page
- **Steps**:
  1. Enter valid credentials
  2. Click "Sign In"
  3. Observe button state during request
- **Expected**: Button shows loading spinner with "Signing in..." text. Button is disabled during request. No duplicate submissions.

#### TC-AUTH-014: Already authenticated user visiting login page [P2] [edge-case]
- **Pre**: User is already logged in
- **Steps**:
  1. Navigate to `/login`
- **Expected**: Automatically redirected to `/portal` without showing login form. Loading spinner shown briefly.

#### TC-AUTH-015: Open redirect prevention on returnUrl [P1] [security]
- **Pre**: User is logged out
- **Steps**:
  1. Navigate to `/login?returnUrl=https://evil.com`
  2. Login with valid credentials
- **Expected**: Redirected to `/portal` (default), NOT to `https://evil.com`. URLs starting with `//` also blocked.

#### TC-AUTH-016: Platform admin returnUrl blocked for tenant-only paths [P2] [security]
- **Pre**: Platform admin user
- **Steps**:
  1. Navigate to `/login?returnUrl=/portal/admin/tenant-settings`
  2. Login as platform admin
- **Expected**: Redirected to `/portal` (not tenant settings, which is tenant-admin-only).

### Visual

#### TC-AUTH-017: Login page responsive layout at 768px [P1] [responsive] [visual]
- **Pre**: On login page
- **Steps**:
  1. Resize browser to 768px width
- **Expected**: Right decorative panel hidden (`hidden lg:flex`). Login form centered, full width. Logo, title, form card, footer all visible without scrolling.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-AUTH-018: Login page decorative panel on desktop [P3] [visual]
- **Pre**: On login page, >= 1024px viewport
- **Steps**:
  1. View login page on desktop
- **Expected**: Left side: login form. Right side: gradient panel with orbital logo, title, description, feature pills (Enterprise Grade, 256-bit Encryption). Animated blobs visible.
- **Visual**: ☐ Light | ☐ Dark

---

## Page: Forgot Password Flow (`/forgot-password`, `/forgot-password/verify`, `/reset-password`)

### Happy Path

#### TC-AUTH-019: Request password reset OTP [P1] [smoke]
- **Pre**: User has valid account
- **Steps**:
  1. Navigate to `/forgot-password`
  2. Enter registered email address
  3. Click submit
- **Expected**: Success toast "OTP sent". Navigates to `/forgot-password/verify`. Session storage populated with `passwordReset` data (sessionToken, maskedEmail, expiresAt, otpLength).

#### TC-AUTH-020: Verify OTP and reset password [P1]
- **Pre**: OTP has been sent to email
- **Steps**:
  1. On verify page, enter correct OTP
  2. Submit OTP
  3. On reset password page, enter new password + confirm
  4. Submit
- **Expected**: Password changed successfully. Redirected to login page or auth success page.

### Edge Cases

#### TC-AUTH-021: Request OTP for non-existent email [P2] [security]
- **Pre**: On forgot password page
- **Steps**:
  1. Enter email that does not exist in system
  2. Submit
- **Expected**: Same success response as valid email (no information leak about account existence). No error shown.

#### TC-AUTH-022: Rate limiting on OTP requests [P2] [edge-case]
- **Pre**: OTP already sent within cooldown period
- **Steps**:
  1. Request OTP again immediately
- **Expected**: Returns existing session (no new OTP). Cooldown message shown if rate limited (429 status).

#### TC-AUTH-023: Invalid OTP entry [P2] [edge-case]
- **Pre**: On OTP verification page
- **Steps**:
  1. Enter incorrect OTP code
  2. Submit
- **Expected**: Error message shown. OTP input cleared (useEffect on error). Can retry.

---

## Page: Dashboard (`/portal/dashboard`)

### Happy Path

#### TC-AUTH-024: Dashboard loads after login [P1] [smoke]
- **Pre**: Logged in as tenant admin
- **Steps**:
  1. Navigate to `/portal/dashboard` or `/portal`
- **Expected**: Dashboard page loads. Revenue, orders, products, customers widgets visible. Quick action cards present. No loading errors.
- **Visual**: ☐ Light | ☐ Dark
- **Data**: ☐ Widget data populated | ☐ No console errors

#### TC-AUTH-025: Dashboard Quick Action labels not truncated [P2] [regression] [visual]
- **Pre**: On dashboard page (any viewport)
- **Steps**:
  1. Check Quick Action card labels
- **Expected**: All labels fully visible (e.g., "Pending Orders", "Low Stock Alerts"). No "..." truncation. Uses `leading-tight` not `truncate`.
- **Visual**: ☐ Light | ☐ Dark | ☐ 768px

---

## Page: Personal Settings — Profile (`/portal/settings/personal`)

### Happy Path

#### TC-AUTH-026: Profile tab loads with current user data [P1] [smoke]
- **Pre**: Logged in, navigate to `/portal/settings/personal`
- **Steps**:
  1. Page loads on "Profile" section (default)
- **Expected**: Profile form shows current first name, last name, display name, phone number, email (read-only). Avatar visible.
- **Data**: ☐ Fields pre-populated | ☐ Email shown but not editable inline

#### TC-AUTH-027: Update profile information [P1]
- **Pre**: On profile tab
- **Steps**:
  1. Change first name
  2. Change display name
  3. Click "Save"
- **Expected**: Success toast. Profile updated. Sidebar user name updates (via `avatar-updated` window event). Form resets to new values.
- **Data**: ☐ API call succeeds | ☐ Name reflected in sidebar

#### TC-AUTH-028: Upload avatar [P2]
- **Pre**: On profile tab
- **Steps**:
  1. Click avatar upload area
  2. Select image file
- **Expected**: Avatar uploaded. Preview updates. Sidebar avatar updates.

#### TC-AUTH-029: Delete avatar [P2]
- **Pre**: User has an uploaded avatar
- **Steps**:
  1. Click delete/remove avatar button
- **Expected**: Avatar removed. Default avatar/initials shown.

#### TC-AUTH-030: Email change via OTP dialog [P1]
- **Pre**: On profile tab
- **Steps**:
  1. Click "Change Email" button
  2. EmailChangeDialog opens
  3. Enter new email, submit
  4. Enter OTP received at new email
  5. Confirm
- **Expected**: Email updated. Dialog closes. Profile shows new email.

### Edge Cases

#### TC-AUTH-031: Profile form validation — empty required fields [P2] [edge-case]
- **Pre**: On profile tab
- **Steps**:
  1. Clear first name (required field)
  2. Blur field
- **Expected**: Validation error shown under field. Red asterisk on required field labels (auto-detected from Zod schema via `requiredFields`).

#### TC-AUTH-032: Profile form server error display [P2] [edge-case]
- **Pre**: On profile tab
- **Steps**:
  1. Trigger a server error (e.g., network issue)
- **Expected**: `FormErrorBanner` appears at top of form (not toast). Dismissible via X button.

---

## Page: Personal Settings — Security (`/portal/settings/personal?section=security`)

### Happy Path

#### TC-AUTH-033: Change password successfully [P1] [smoke]
- **Pre**: On security section
- **Steps**:
  1. Enter current password
  2. Enter new password
  3. Enter confirm password (matching)
  4. Click "Change Password"
- **Expected**: Success toast. User logged out (all sessions revoked). Redirected to `/login`.
- **Data**: ☐ Can login with new password | ☐ Old password rejected

#### TC-AUTH-034: Session management — view active sessions [P1]
- **Pre**: On security section, scrolled to session management
- **Steps**:
  1. View active sessions list
- **Expected**: Current session marked with "Current" badge (green). Other sessions show device icon (Monitor/Smartphone/Globe), device info, IP, last active time.

#### TC-AUTH-035: Revoke a session [P2]
- **Pre**: Multiple active sessions exist
- **Steps**:
  1. Click revoke/delete button on a non-current session
  2. Confirm in dialog
- **Expected**: Session removed from list. That session can no longer access the application.

### Edge Cases

#### TC-AUTH-036: Password mismatch validation [P2] [edge-case]
- **Pre**: On change password form
- **Steps**:
  1. Enter current password
  2. Enter new password "NewPass123"
  3. Enter confirm password "DifferentPass"
  4. Submit
- **Expected**: Validation error "Passwords do not match" on confirm field. Form not submitted.

#### TC-AUTH-037: New password same as current [P2] [edge-case]
- **Pre**: On change password form
- **Steps**:
  1. Enter current password "123qwe"
  2. Enter new password "123qwe"
  3. Submit
- **Expected**: Validation error "New password must differ from current". Client-side Zod `.refine()` check.

#### TC-AUTH-038: Wrong current password [P2] [edge-case]
- **Pre**: On change password form
- **Steps**:
  1. Enter wrong current password
  2. Enter valid new + confirm passwords
  3. Submit
- **Expected**: Server error mapped to `currentPassword` field via `handleFormError`. Inline error under "Current Password" field.

---

## Page: Personal Settings — Appearance (`/portal/settings/personal?section=appearance`)

### Happy Path

#### TC-AUTH-039: Select density option [P2]
- **Pre**: On appearance section
- **Steps**:
  1. View 3 density options: Compact, Normal, Comfortable
  2. Click "Compact"
- **Expected**: Selection highlighted. Table row heights across app change to compact density. Persisted in context.

#### TC-AUTH-040: Theme toggle from appearance settings [P2] [visual]
- **Pre**: On appearance section
- **Steps**:
  1. Toggle theme (if theme controls exist here)
- **Expected**: Theme switches immediately. All page elements adapt.
- **Visual**: ☐ Light | ☐ Dark

---

## Page: Personal Settings — API Keys (`/portal/settings/personal?section=api-keys`)

### Happy Path

#### TC-AUTH-041: View API keys list [P1]
- **Pre**: On API Keys section
- **Steps**:
  1. View API keys tab
- **Expected**: List of API keys with name, status (Active/Revoked), scopes, created date, expiry date. Empty state if no keys.

#### TC-AUTH-042: Create new API key [P1]
- **Pre**: On API keys tab
- **Steps**:
  1. Click "Create API Key"
  2. Fill in name, select permissions, set expiry
  3. Submit
- **Expected**: Key created. Secret shown ONCE in dialog (copy button). After closing, secret cannot be retrieved again.
- **Data**: ☐ Key appears in list | ☐ Secret copyable | ☐ Status "Active"

#### TC-AUTH-043: Rotate API key [P2]
- **Pre**: Active API key exists
- **Steps**:
  1. Click rotate button on a key
  2. Confirm rotation
- **Expected**: New secret generated and shown. Old secret invalidated. Key ID remains same.

#### TC-AUTH-044: Revoke API key [P2]
- **Pre**: Active API key exists
- **Steps**:
  1. Click revoke button on a key
  2. Enter revocation reason
  3. Confirm
- **Expected**: Key status changes to "Revoked" (red badge). Key can no longer authenticate API calls.

---

## Page: Personal Settings — URL Tab Navigation

### Happy Path

#### TC-AUTH-045: Direct URL to security section [P2]
- **Pre**: Logged in
- **Steps**:
  1. Navigate directly to `/portal/settings/personal?section=security`
- **Expected**: Page loads with security section active. Left nav highlights "Security" item.

#### TC-AUTH-046: Section switching via left nav [P1]
- **Pre**: On personal settings page
- **Steps**:
  1. Click "Profile" in left nav
  2. Click "Security"
  3. Click "Appearance"
  4. Click "API Keys"
- **Expected**: Each section loads with transition animation (opacity). URL updates with `?section=` param. Active nav item highlighted in blue.

---

## Cross-Cutting: Security

#### TC-AUTH-047: Unauthenticated user redirected to login [P0] [security] [smoke]
- **Pre**: User is not logged in
- **Steps**:
  1. Navigate to `/portal/dashboard`
- **Expected**: Redirected to `/login?returnUrl=/portal/dashboard`. Login form shown.

#### TC-AUTH-048: Session expiry handling [P0] [security]
- **Pre**: User is logged in, token expires
- **Steps**:
  1. Wait for session/token to expire (or manually clear token)
  2. Attempt any authenticated action
- **Expected**: User redirected to login page. Appropriate message shown. Return URL preserved for re-login.
