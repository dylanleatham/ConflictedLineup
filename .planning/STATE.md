# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-01-24)

**Core value:** Users can instantly turn any festival lineup into a personalized discovery playlist without manual artist-by-artist searching.
**Current focus:** Phase 2 - Spotify Authentication

## Current Position

Phase: 2 of 5 (Spotify Authentication)
Plan: 1 of 4 complete
Status: In progress
Last activity: 2026-01-25 — Completed 02-01-PLAN.md

Progress: [█████████████░░░░░░░] 67% (6/9 known plans)

## Performance Metrics

**Velocity:**
- Total plans completed: 6
- Average duration: 8 min
- Total execution time: 0.78 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-skeleton-deployment | 5/5 | 44 min | 9 min |
| 02-spotify-authentication | 1/4 | 3 min | 3 min |

**Recent Trend:**
- Last 5 plans: 01-03 (1min), 01-02 (15min), 01-04 (20min), 01-05 (5min), 02-01 (3min)
- Trend: Auth infrastructure setup faster than infrastructure provisioning

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

Last session: 2026-01-25 (plan execution)
Stopped at: Completed 02-01-PLAN.md (Auth infrastructure)
Resume file: None
Next: Plan 02-02 (Login page with Spotify button)
