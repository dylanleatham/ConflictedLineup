# Phase 2: Spotify Authentication - Context

**Gathered:** 2026-01-25
**Status:** Ready for planning

<domain>
## Phase Boundary

Users can log in with Spotify, session persists across browser refresh, and they can log out. OAuth flow with automatic token refresh. No playlist creation or artist lookup in this phase — just authentication working end-to-end.

</domain>

<decisions>
## Implementation Decisions

### Login experience
- Button appears after context explaining what the app does
- Show visual example of poster → playlist transformation before login button
- Brief mention of why Spotify access is needed ("We need playlist access to create your playlist")
- Use official Spotify green button with Spotify logo, following their brand guidelines

### Post-login feedback
- Redirect to new dedicated page/view after successful login (upload poster page)
- Show Spotify display name and profile picture in header
- Seamless transition — no explicit welcome message, just smoothly show the new page
- Logout accessible via profile dropdown (click avatar/name to reveal)

### Session behavior
- Silent token refresh in background — user never notices expiration
- Keep session alive as long as possible (refresh until Spotify revokes)
- On return visit, auto-attempt to restore session automatically
- If refresh fails completely, redirect back to login page

### Error states
- Permission denied: Clear message explaining why permissions are needed + retry button
- Spotify unreachable: Inline error message near login button, keep page functional
- User-friendly error messages only (no technical details exposed)
- If profile fetch fails after login: Continue with generic avatar, don't block functionality

### Claude's Discretion
- Exact button placement and sizing
- Loading state during OAuth redirect
- Animation/transition details
- Generic avatar design when profile unavailable

</decisions>

<specifics>
## Specific Ideas

- Visual example showing poster → playlist transformation should appear before the login button to give users immediate understanding of value
- Profile dropdown pattern for logout (common UX pattern)

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 02-spotify-authentication*
*Context gathered: 2026-01-25*
