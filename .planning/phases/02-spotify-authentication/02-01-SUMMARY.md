# Phase 02 Plan 01: Auth Infrastructure Summary

**One-liner:** JWT auth with refresh rotation using react-oauth2-code-pkce library configured for Spotify PKCE flow with localStorage persistence

---
phase: 02-spotify-authentication
plan: 01
subsystem: authentication
tags:
  - oauth
  - spotify
  - pkce
  - react
  - authentication
dependency-graph:
  requires:
    - 01-skeleton-deployment
  provides:
    - spotify-oauth-infrastructure
    - auth-context-provider
    - profile-fetching-hook
  affects:
    - 02-02-login-page
    - 02-03-post-login-ui
tech-stack:
  added:
    - library: react-oauth2-code-pkce
      version: 1.23.4
      purpose: PKCE OAuth flow with automatic token refresh
  patterns:
    - pattern: Frontend-first OAuth with PKCE
      where: frontend/src/auth/*
      why: Spotify requires PKCE, library handles token management
key-files:
  created:
    - frontend/src/auth/types.ts
    - frontend/src/auth/SpotifyAuthConfig.ts
    - frontend/src/auth/AuthProvider.tsx
    - frontend/src/auth/useAuth.ts
    - frontend/src/auth/index.ts
  modified:
    - frontend/package.json
    - frontend/src/main.tsx
decisions:
  - choice: Use react-oauth2-code-pkce for OAuth
    rationale: Provider-agnostic library with built-in PKCE, token refresh, and storage management
    alternatives:
      - Custom PKCE implementation (too complex, security-critical)
      - Auth0/Firebase (overkill for single provider)
  - choice: Set decodeToken to false
    rationale: Spotify tokens are opaque, not JWTs - attempting to decode fails silently
    alternatives:
      - None - this is required for Spotify
  - choice: localStorage for token persistence
    rationale: User sessions should persist across browser restarts per CONTEXT.md
    alternatives:
      - sessionStorage (loses auth on tab close)
      - HttpOnly cookies (requires backend coordination)
  - choice: Graceful profile fetch failure
    rationale: Per CONTEXT.md, continue with generic avatar if profile fetch fails
    alternatives:
      - Block UI until profile loads (poor UX)
metrics:
  duration: 3 minutes
  completed: 2026-01-25
---

## What Was Built

This plan established the OAuth authentication foundation for the application by integrating the react-oauth2-code-pkce library configured for Spotify's PKCE flow. The implementation includes:

1. **OAuth Configuration**: SpotifyAuthConfig with all required Spotify endpoints, scopes, and critical settings including `decodeToken: false` for opaque tokens
2. **AuthProvider Wrapper**: React component wrapping the library's AuthProvider with the Spotify configuration
3. **useAuth Hook**: Custom hook that combines OAuth state with Spotify profile fetching, providing clean API for components
4. **Type Definitions**: TypeScript interfaces for SpotifyProfile and AuthState
5. **App Integration**: AuthProvider wrapping the entire app in main.tsx

The auth module is structured as a self-contained directory (`frontend/src/auth/`) with a barrel export (`index.ts`) for clean imports.

## Tasks Completed

| Task | Description | Status | Commit |
|------|-------------|--------|--------|
| 1 | Install react-oauth2-code-pkce and create auth types | ✓ Complete | 50c9b1b |
| 2 | Create OAuth configuration and AuthProvider | ✓ Complete | e9358f8 |
| 3 | Create useAuth hook with profile fetching | ✓ Complete | 89c94e3 |

**Total**: 3/3 tasks completed

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Incorrect library export name**

- **Found during:** Task 3 - TypeScript compilation
- **Issue:** Imported `useAuth` from react-oauth2-code-pkce, but library exports `useAuthContext`
- **Fix:** Changed import from `useAuth as useOAuthContext` to `useAuthContext`
- **Files modified:** frontend/src/auth/useAuth.ts
- **Commit:** 89c94e3

This was a blocking issue preventing TypeScript compilation. The library documentation used in research referenced the hook generically, but the actual export is `useAuthContext`. Fixed immediately to unblock task completion.

## Technical Details

### OAuth Configuration Highlights

The SpotifyAuthConfig includes several critical settings:

- **decodeToken: false** - Essential for Spotify's opaque tokens (not JWTs)
- **autoLogin: false** - Shows landing page before redirecting to Spotify
- **storage: 'local'** - Persists session across browser restarts
- **onRefreshTokenExpire** - Redirects to `/?session_expired=true` for user re-authentication

### Profile Fetching Strategy

The useAuth hook combines OAuth state with Spotify profile data:

1. Detects when token exists and profile hasn't been fetched
2. Calls Spotify's `/v1/me` endpoint with Bearer token
3. On success: stores profile in state
4. On error: sets profile to null (per CONTEXT.md - continue with generic avatar)
5. Loading state combines OAuth login progress and profile fetch status

### Scopes Requested

The configuration requests these Spotify scopes:
- `user-read-private` - Access to user's private profile data
- `user-read-email` - Access to user's email address
- `playlist-modify-public` - Create/modify public playlists
- `playlist-modify-private` - Create/modify private playlists
- `playlist-read-private` - Read user's private playlists (for familiar tracks feature)

## Verification Results

All verification criteria met:

- ✓ `npm run build` succeeds in frontend directory
- ✓ Auth module structure exists with all required files
- ✓ main.tsx imports and uses AuthProvider
- ✓ No TypeScript errors

Build output: 69 modules transformed, 158.72 kB bundle (51.37 kB gzipped)

## Next Phase Readiness

**Ready for:** Phase 02 Plan 02 (Login Page)

**Provides:**
- `AuthProvider` component ready to wrap app
- `useAuth()` hook exposing: isAuthenticated, isLoading, profile, error, login, logout, token
- Type definitions for SpotifyProfile and AuthState

**Blockers:** None

**Concerns:**
- Spotify app setup required before OAuth can work (Client ID environment variable)
- Redirect URI must be configured in Spotify dashboard to match deployed URL
- These are addressed in plan 02-04 (Spotify app setup and verification)

## Files Reference

**Auth Module Structure:**
```
frontend/src/auth/
├── types.ts               # SpotifyProfile and AuthState interfaces
├── SpotifyAuthConfig.ts   # OAuth configuration for Spotify PKCE
├── AuthProvider.tsx       # Wrapper around react-oauth2-code-pkce
├── useAuth.ts             # Custom hook combining OAuth + profile
└── index.ts               # Barrel exports
```

**Key Exports:**
- `AuthProvider` - Wrap app to enable auth context
- `useAuth()` - Access auth state in components
- `SpotifyProfile` - Type for user profile data
- `AuthState` - Type for auth context value

## Commits

```
89c94e3 feat(02-01): create useAuth hook with profile fetching
e9358f8 feat(02-01): create OAuth config and AuthProvider wrapper
50c9b1b feat(02-01): install OAuth library and create auth types
```

## Lessons Learned

1. **Library exports matter**: Always verify actual export names in node_modules type definitions, not just documentation
2. **Opaque tokens need special handling**: Spotify's decision to use opaque tokens instead of JWTs requires explicit `decodeToken: false` configuration
3. **Graceful degradation works**: Profile fetch errors don't block auth flow - setting null allows UI to show generic avatar
4. **Barrel exports clean up imports**: Single `import { useAuth } from './auth'` instead of path to specific file

---

*Completed: 2026-01-25*
*Duration: 3 minutes*
*Executed by: GSD Phase Executor*
