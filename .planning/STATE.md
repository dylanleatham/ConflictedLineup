# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-01-24)

**Core value:** Users can instantly turn any festival lineup into a personalized discovery playlist without manual artist-by-artist searching.
**Current focus:** Phase 2 - Spotify Authentication (Complete)

## Current Position

Phase: 3 of 5 (Artist Extraction)
Plan: 1 of 3 complete
Status: In progress
Last activity: 2026-01-26 — Completed 03-02-PLAN.md

Progress: [████████████████████] 100% (10/10 known plans)

## Performance Metrics

**Velocity:**
- Total plans completed: 10
- Average duration: 7 min
- Total execution time: 1.22 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-skeleton-deployment | 5/5 | 44 min | 9 min |
| 02-spotify-authentication | 4/4 | 28 min | 7 min |
| 03-artist-extraction | 1/3 | 3 min | 3 min |

**Recent Trend:**
- Last 5 plans: 02-01 (3min), 02-02 (5min), 02-03 (5min), 02-04 (15min), 03-02 (3min)
- Trend: Pure component work (no checkpoints) executes quickly

*Updated after each plan completion*

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- Deploy skeleton first: Avoid deployment surprises when codebase is complex (affects Phase 1 priority)
- Prompt in codebase file: User has existing tested prompt, keeps it version controlled (affects Phase 3 implementation)
- Require Spotify auth upfront: Need playlist access for familiar tracks feature (affects Phase 2 architecture)
- 9 tracks per artist (3+3+3): Balance between familiar, discovery, and likely-to-be-played-live (affects Phase 4 track selection)

**From 01-01 execution:**
- Vite polling for Docker hot reload: Ensures file watching works in containers across all platforms
- Separate dev/prod Dockerfiles: Cleaner separation than environment-based conditional logic
- Anonymous volume for node_modules: Prevents host mount from overwriting container dependencies

**From 01-02 execution:**
- Use RBAC for Key Vault instead of access policies for fine-grained control
- Use OIDC federated credentials for GitHub Actions (no stored secrets)
- Use Managed Identity for App Service to Key Vault access

**From 01-03 execution:**
- Use pm2 serve for Azure App Service static file hosting with SPA routing support
- Set VITE_API_URL during build to point to Azure backend URL

**From 01-04 execution:**
- Use Azure Front Door Standard as unified entry point for frontend and backend
- Path-based routing: /api/* to backend, /* to frontend
- Azure-managed TLS certificates for custom domain HTTPS

**From 02-01 execution:**
- Use react-oauth2-code-pkce for OAuth: Provider-agnostic library with built-in PKCE, token refresh, and storage
- Set decodeToken: false for Spotify: Tokens are opaque, not JWTs
- localStorage for token persistence: Sessions persist across browser restarts
- Graceful profile fetch failure: Continue with generic avatar if profile fetch fails

**From 02-02 execution:**
- Use inline styles for SpotifyLoginButton to ensure exact brand compliance
- Show visual example (poster → playlist) before login button to establish value proposition
- Use gradient background for login page to create visual impact

**From 02-03 execution:**
- Sticky header for persistent profile access throughout authenticated experience
- Generic avatar with initials when Spotify profile image unavailable
- Click-outside dropdown handler with 100ms delay to prevent immediate close on open click
- Placeholder cards for upload and search ready for Phase 3 implementation

**From 02-04 execution:**
- Use 127.0.0.1 for local Spotify OAuth (Spotify doesn't support localhost as redirect)
- PKCE flow only needs Client ID, no Client Secret required
- Wrap login() calls to prevent event object serialization errors

**From 03-02 execution:**
- MUI Autocomplete with freeSolo: Enables both controlled chips (from extraction) and free-text additions by users
- Question mark icon for uncertain artists: Visual indicator appears inline with artist name rather than separate section
- Orange tint for uncertain chips: Border and background color change draws attention without being alarming

### Pending Todos

None yet.

### Blockers/Concerns

**Phase 1 considerations:**
- Anthropic Tier 2 setup required early ($40 deposit for 1,000 RPM) to avoid rate limits during Phase 3 testing
- Spotify Development Mode limited to 25 users maximum - verify friend group size acceptable before proceeding
- Azure Key Vault permission propagation can take 15-30 minutes - plan setup timing accordingly

**Phase 3 considerations:**
- Claude Vision accuracy with artistic festival typography is unvalidated - may need prompt engineering iteration
- User has pre-built prompt file that needs integration into codebase structure

## Session Continuity

Last session: 2026-01-26 (plan execution)
Stopped at: Completed 03-02-PLAN.md - MUI Integration and Artist Chip List
Resume file: None
Next: Continue Phase 3 - run /gsd:execute-phase 3 for next plan
