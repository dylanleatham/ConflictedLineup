# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-01-24)

**Core value:** Users can instantly turn any festival lineup into a personalized discovery playlist without manual artist-by-artist searching.
**Current focus:** Phase 5 - Playlist Creation & Results

## Current Position

Phase: 5 of 5 (Playlist Creation & Results) - COMPLETE
Plan: 3 of 3 complete (05-03)
Status: All phases complete - application verified and production-ready
Last activity: 2026-01-27 — Completed 05-03-PLAN.md (End-to-End Verification)

Progress: [████████████████████] 100% (21/21 known plans)

## Performance Metrics

**Velocity:**
- Total plans completed: 21
- Average duration: 10 min
- Total execution time: 3.9 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-skeleton-deployment | 5/5 | 44 min | 9 min |
| 02-spotify-authentication | 4/4 | 28 min | 7 min |
| 03-artist-extraction | 5/5 | 63 min | 13 min |
| 04-track-selection | 4/4 | 59 min | 15 min |
| 05-playlist-creation-results | 3/3 | 26 min | 9 min |

**Recent Trend:**
- Last 5 plans: 04-04 (45min with debugging), 05-01 (3min), 05-02 (8min), 05-03 (15min with bug fixes)
- Trend: Fast execution with verification catching integration bugs

*Updated after each plan completion*

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- Deploy skeleton first: Avoid deployment surprises when codebase is complex (affects Phase 1 priority)
- Prompt in codebase file: User has existing tested prompt, keeps it version controlled (affects Phase 3 implementation)
- Require Spotify auth upfront: Need playlist access for familiar tracks feature (affects Phase 2 architecture)

**CRITICAL - Local Development URLs:**
- Frontend: http://127.0.0.1:5175 (NOT 5173 — Spotify OAuth requires this exact port)
- Backend: http://localhost:5000
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

**From 03-01 execution:**
- Use claude-sonnet-4-5-20250514 model for both vision and web search
- Store prompts in text files with .csproj copy to output for version control and easy updates
- Return partial results with warnings instead of hard failures for graceful degradation
- Limit base64 images to 7MB (~5MB raw) to prevent memory issues
- File-based prompts read from Prompts/*.txt in AppContext.BaseDirectory
- Web search enabled via Anthropic console settings (not SDK Tools parameter)

**From 03-02 execution:**
- MUI Autocomplete with freeSolo for editable artist chip lists
- Uncertain artists marked with question mark icon inline (not separate section)
- Dark theme with Spotify green accents for consistency with login page

**From 03-03 execution:**
- Hidden file input with click handlers for picker (no drag-drop per context decision)
- 5MB max file size and 1568px max dimension for Claude API optimization
- Multi-stage progress bar with status transitions for user feedback during extraction
- Error alerts with alternative action buttons (Try Different Image / Search Instead)

**From 03-04 execution:**
- Year dropdown shows current year + 2 future years for festival planning
- Enter key triggers search for better UX
- Error alerts include fallback action buttons for seamless mode switching
- Search status text shows during API calls for user feedback

**From 04-01 execution:**
- Use SpotifyAPI.Web 7.2.1 for type-safe Spotify API access
- OAuth scopes extended with user-library-read and playlist-read-collaborative
- C# records for all track selection DTOs

**From 04-02 execution:**
- ISpotifyClient parameter injection: Orchestrator controls token, services accept client as parameter
- First Spotify search result trust per CONTEXT.md algorithm decision
- Singles prioritized over album tracks via 90-day date comparison and popularity sorting
- Rate limiting pattern: ExecuteWithRetryAsync with Retry-After header parsing, max 3 retries

**From 04-03 execution:**
- User library scan limits: 500 saved tracks + 50 playlists max (prevents rate limiting and timeouts)
- Only scan user-owned playlists (not followed) per CONTEXT.md
- Deduplication priority: familiar > top > recent (HashSet<string> with SpotifyTrackId)
- Backfill strategy: When recent releases < 3, add more top tracks (up to 6 total from top)

**From 04-04 execution:**
- Spotify search Limit=5 instead of Limit=1: Prevents wrong cached results when rate limited
- Simplified to top 5 tracks only: Disabled familiar/recent to avoid rate limiting (can re-enable later)
- useRef guard for API calls: Prevents duplicate calls from React StrictMode double-mounting
- 1-second delay between artists: Prevents Spotify rate limiting during batch processing
- /callback redirect route: OAuth callback needs explicit redirect to / after completion

**From 05-01 execution:**
- Private playlists by default with "Created by Conflicted Lineup" description
- Batch size of 100 tracks per request (Spotify API limit) with 500ms delays between batches
- Server-side track URI extraction from ArtistTrackResult rather than passing raw URIs from frontend
- Use ExternalUrls["spotify"] for playlist URL (not manual construction)
- Fetch user ID from Spotify UserProfile.Current() API (avoid requiring frontend to pass it)

**From 05-02 execution:**
- CSS Grid over MUI Grid for responsive layout: MUI v7 changed API (removed item prop), CSS Grid more compatible
- Festival context propagates via React Router navigation state: UploadPage → TrackSelectionPage → PlaylistResultsPage
- Loading state with CircularProgress during playlist creation for user feedback
- Results page shows success banner, artist list, skipped list, and "Open in Spotify" button

**From 05-03 execution:**
- Clear OAuth tokens before redirect to prevent refresh token loop: onRefreshTokenExpire callback + main.tsx early cleanup
- Explicit route "api/playlist" instead of [controller] placeholder: Matches pattern from other controllers
- End-to-end verification catches integration bugs missed in isolated testing (OAuth loop, routing issues)

### Pending Todos

None yet.

### Blockers/Concerns

**All phases complete - no current blockers**

**For future deployment:**
- Spotify Development Mode limited to 25 users maximum - sufficient for friends/family beta
- Azure infrastructure already configured in Phase 1
- Consider extended quota mode when ready for wider public release

## Session Continuity

Last session: 2026-01-27
Stopped at: Completed 05-03-PLAN.md - End-to-End Verification
Resume file: None
Next: All planned phases complete! MVP delivered and production-ready.

**Application status:**
- Complete end-to-end flow verified working
- All critical bugs fixed during testing
- Mobile-responsive UI confirmed
- Ready for Azure deployment and beta testing
