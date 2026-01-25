# Project Research Summary

**Project:** ConflictedLineup
**Domain:** Festival lineup to Spotify playlist conversion web app
**Researched:** 2026-01-24
**Confidence:** HIGH

## Executive Summary

ConflictedLineup is a web application that converts festival lineup posters into Spotify playlists using AI vision for artist extraction and curated track selection. Based on research, experts build this type of product using a Backend-For-Frontend (BFF) architecture: React SPA for image upload and UI, .NET backend managing OAuth flows and API orchestration, Claude Vision API for poster analysis, and Spotify Web API for playlist creation. The recommended stack is .NET 10 + ASP.NET Core Minimal APIs for the backend, React 19 + Vite for the frontend, and Azure App Service with Key Vault for deployment.

The key differentiator is personalized track selection (3 familiar + 3 top + 3 recent per artist), which sets this apart from competitors who only use top tracks. The biggest architectural risk is OAuth token management - refresh tokens must be stored securely on the backend, never in browser localStorage, to prevent XSS attacks. Critical operational risks include: Spotify Development Mode limits (25 users maximum, no extended access for small apps), Anthropic rate limiting (Tier 1 = only 50 RPM, requires immediate upgrade to Tier 2), and Azure-specific deployment pitfalls (port binding, Key Vault permission delays, CORS caching).

The recommended approach is a 6-phase build: (1) Foundation with authentication scaffolding, (2) Spotify OAuth with proper PKCE flow, (3) Artist extraction with Claude Vision and image preprocessing, (4) Track selection with personalization logic, (5) Playlist creation and result display, (6) Azure deployment with production hardening. This order ensures OAuth works before dependent features (track selection, playlist creation) are built, and validates core functionality locally before tackling Azure-specific deployment challenges.

## Key Findings

### Recommended Stack

The technology stack is aligned with Microsoft 2026 best practices for modern web apps: .NET 10 LTS (supported until Nov 2028) provides the backend runtime with built-in AI integration and C# 14 features. React 19.2.1 with the stable Server Components API and React Compiler optimizations handles the frontend. Vite 7.3.1 replaces the deprecated Create React App with 3-10x faster build times. SpotifyAPI-NET 7.2.1 is the most actively maintained C# library for Spotify integration with OAuth PKCE support. Anthropic.SDK 5.9.0 provides .NET 10 compatibility and is more mature than the official beta SDK.

**Core technologies:**
- **.NET 10 LTS**: Backend API framework — Latest LTS with 3 years support, built-in AI integration, and C# 14 features
- **React 19.2.1**: Frontend UI library — Latest stable with Server Components, Actions API, and automatic optimizations via React Compiler
- **ASP.NET Core Minimal APIs**: HTTP endpoints — Microsoft's recommended approach for new APIs, faster and cleaner than controllers
- **Vite 7.3.1**: Build tool — Modern replacement for deprecated Create React App, 3-10x faster builds with native ESM
- **SpotifyAPI-NET 7.2.1**: Spotify client — Most actively maintained C# SDK with OAuth PKCE, 74+ endpoints, typed responses
- **Anthropic.SDK 5.9.0**: Claude client — .NET 10 support, more battle-tested than official beta SDK
- **Azure App Service + Key Vault**: Hosting — 99.99% SLA on Premium v4, passwordless secrets management via Managed Identity
- **TanStack Query 5.90.20**: State management — Industry standard for API caching, background refetching, optimistic updates
- **Tailwind CSS 3.x/4.x**: Styling — Utility-first CSS, AI-friendly, v4 offers 3-10x faster builds with Rust engine

**Critical version requirements:**
- Spotify OAuth MUST use Authorization Code Flow with PKCE (Implicit Grant removed Nov 27, 2025)
- HTTPS redirect URIs required in production (HTTP disabled Nov 27, 2025, except `http://127.0.0.1` for local dev)
- Application Insights must use connection strings (instrumentation keys deprecated Mar 31, 2025)
- Node.js 20.19+ or 22.12+ required by Vite (earlier versions unsupported)

### Expected Features

Research shows that users expect automatic poster extraction as the core value proposition, with accurate artist identification and immediate Spotify playlist creation. The personalized track selection approach (3 familiar from user's library + 3 top tracks + 3 recent releases per artist) is a key differentiator - competitors like LineupSupply and spotify-festival-playlist-generator only use generic top tracks. The dual input mode (poster upload OR festival name text search) provides flexibility competitors lack.

**Must have (table stakes):**
- Artist extraction from poster image — Core value prop, users expect automatic OCR/AI extraction despite complex festival typography
- Spotify OAuth integration — Required to create playlists in user's account with PKCE flow
- Playlist creation in user's Spotify account — Core output, what users come for
- Artist name matching to Spotify IDs — Must handle fuzzy matching, special characters, international artists
- Error handling for missing artists — Transparency builds trust when OCR or matching fails
- Track selection logic — Users expect familiar hits, not random deep cuts (differentiation opportunity)
- Mobile-friendly interface — Most festival poster sharing happens on mobile
- Loading states during processing — Image parsing takes 15-30 seconds, needs progress indication
- Direct link to created playlist — Users expect immediate access with deep link to Spotify app

**Should have (competitive advantages):**
- Personalized track selection from user's library — "3 familiar" approach is unique differentiator vs competitors
- Text input for festival name (web search) — Convenience for users without poster image, LineupSupply lacks this
- Recent releases prioritization — "Recent" tracks likely to be played at festival, shows domain understanding
- Artist list review/editing before creation — Users can deselect artists or correct OCR errors, critical for trust
- Preview of track selection before finalizing — Transparency builds trust in curation algorithm
- Playlist naming customization — Low effort, high perceived value personalization touch
- Track count per artist customization — 9-track default is strong, slider (3-15) adds power user flexibility
- Duplicate prevention across artists — Important for large festivals where artist collaboration is common
- Playlist cover art from festival poster — Visual continuity, uses separate Spotify image upload endpoint

**Defer (v2+):**
- Offline festival database for major events — Optimization not core validation, defer until web search proves pain point
- Advanced personalization (genre, mood filters) — Only if users request more control after v1
- Playlist update/regeneration for lineup changes — Defer until users express need
- Analytics dashboard — Different product direction, scope creep

**Anti-features (avoid):**
- Adding ALL tracks from each artist — Creates 2,500+ song playlists, hits Spotify's 10,000 track limit, users won't listen
- Real-time playlist updates as lineup changes — Massive maintenance burden monitoring festival websites
- Social features (sharing, collaborative editing) — Massive scope, requires user accounts beyond Spotify, not aligned with "for friends" scope
- Automatic genre separation into multiple playlists — Genre classification error-prone, creates decision paralysis
- Historical festival archive (browse past lineups) — Data maintenance burden, stale data, storage costs
- Integration with Apple Music/Tidal — Multiplies complexity 3-5x, each service has different APIs
- AI chat interface for recommendations — Scope creep, users want specific output (playlist), not conversation

### Architecture Approach

The recommended architecture uses Backend-For-Frontend (BFF) pattern: React SPA handles UI and image upload, .NET backend manages all OAuth tokens and secrets, stores refresh tokens server-side with HTTP-only cookies, and orchestrates calls to Anthropic and Spotify APIs. This prevents XSS attacks from stealing tokens (critical security requirement for OAuth flows). Azure Front Door provides global CDN routing to serve both SPA and API from a single domain, eliminating CORS complexity. Key Vault with Managed Identity stores all secrets (Spotify Client ID/Secret, Anthropic API key) using passwordless authentication. Background jobs handle large lineup processing (50+ artists) to avoid HTTP timeouts.

**Major components:**
1. **React SPA (Vite)** — User interface for poster upload, festival name input, artist list editing, playlist result display
2. **Backend (ASP.NET Minimal APIs)** — OAuth client, token manager, service orchestrator with dependency injection
3. **Auth Manager Service** — Spotify OAuth 2.0 PKCE flow, token storage/refresh, session management with HTTP-only cookies
4. **Artist Extraction Service** — Anthropic Vision API integration for poster OCR, web search for festival names
5. **Track Selection Service** — Spotify search, personalized track curation (familiar + top + recent), duplicate prevention
6. **Playlist Creation Service** — Spotify playlist creation, batch track addition, skipped artist tracking
7. **Background Job Queue** — Async processing for large lineups, retry logic for API failures
8. **Azure Key Vault** — Secrets storage accessed via Managed Identity (no hardcoded credentials)
9. **Azure Front Door** — CDN, SSL termination, unified domain routing (no CORS issues)

**Key patterns:**
- **Feature-based folder structure** (not layer-based) — Groups all code for a feature together per Microsoft 2026 recommendations
- **Service Layer Abstraction** — Business logic separate from API endpoints, enables testing and reuse
- **Managed Identity for Key Vault** — Zero secrets in code, automatic credential rotation, works locally (Azure CLI) and production (App Identity)
- **Background Jobs for large lineups** — Return job ID immediately, poll for status, show progress to users
- **Image preprocessing before Anthropic** — Validate <5MB, resize to 1568px max, convert PNG to JPEG to avoid limits

**Data flow:**
1. User uploads poster → Frontend validates size → Backend converts to base64
2. Backend sends to Claude Vision API → Extracts artist names → Returns JSON array
3. Backend queries Spotify Search API for each artist → Gets artist IDs and track URIs
4. Backend fetches user's saved tracks (for "familiar" selection) → Cross-references with festival artists
5. Backend creates playlist via Spotify API → Adds tracks in batches → Returns playlist URL
6. Frontend displays playlist link, artist list, skipped artists

### Critical Pitfalls

The research uncovered 14 high-severity pitfalls specific to this stack and domain. The top 5 are Spotify Development Mode limits, OAuth redirect URI configuration, refresh token invalidation, Anthropic rate limiting, and image size validation.

1. **Spotify Extended Access is impossible for small apps** — As of May 2025, extended access requires 250K+ monthly users, registered business entity, and app already public. Development Mode supports only 25 users maximum. **Mitigation:** Accept 25-user limit for friends-only app, never plan to go public. Verify friend group size <25 in Phase 1.

2. **OAuth redirect URI must be HTTPS in production, localhost broken** — Spotify disabled HTTP redirect URIs on Nov 27, 2025 (except `http://127.0.0.1` loopback). Dashboard auto-transforms `localhost` to `127.0.0.1` causing "Invalid redirect URI" errors. **Mitigation:** Use `http://127.0.0.1:PORT/callback` for dev, `https://yourdomain.com/callback` for prod. Configure BOTH in Spotify dashboard before deployment. Never use `localhost`.

3. **Refresh tokens invalidate without warning** — PKCE refresh tokens can be used only once, returning a new refresh token each time. Tokens also revoke when user changes password or revokes app access. **Mitigation:** Always update stored refresh token after each refresh. Implement `invalid_grant` error handling to trigger re-authentication flow. Store tokens server-side (Key Vault or Redis), never in browser localStorage.

4. **Anthropic Tier 1 limits break at scale** — Tier 1 ($5 deposit, $100/mo limit) provides only 50 RPM organization-wide. Festival poster parsing with vision API uses ~1,600 tokens per image. 3 simultaneous users can exceed limits. **Mitigation:** Start Tier 2 immediately ($40 deposit for 1,000 RPM - 20x increase). Implement exponential backoff with `retry-after` header. Use prompt caching for system prompts (>1,024 tokens).

5. **Anthropic Vision API rejects images >5MB** — High-resolution festival posters and smartphone photos routinely exceed 5MB. API returns errors without automatic resizing. **Mitigation:** Validate image size client-side before upload. Resize server-side to max 1568px (optimal for Claude tokenization at ~1,600 tokens). Convert PNG to JPEG for 3-5x size reduction. Use JPEG quality 85-90 for text recognition.

**Additional critical pitfalls:**
- **Azure App Service port binding** — Must use `process.env.PORT`, never hardcode port 3000 or app won't receive traffic
- **Key Vault permission propagation delay** — Role assignments take up to 24 hours to propagate (typically 15-30 min), plan setup ahead
- **Azure Front Door CORS caching** — Caches `Access-Control-Allow-Origin` for first origin, breaks multi-origin apps without dynamic Rules Engine
- **Spotify playlist scope confusion** — Need BOTH `playlist-modify-public` and `playlist-modify-private` scopes even for private playlists
- **PM2 requires `--no-daemon` flag** — Node.js 14+ on Azure App Service requires `pm2 start app.js --no-daemon` or container exits

## Implications for Roadmap

Based on research, suggested phase structure prioritizes authentication foundation before dependent features, validates core functionality locally before Azure deployment, and defers production hardening until MVP is proven with friends.

### Phase 1: Foundation & Project Setup
**Rationale:** Establish development environment and skeleton applications before implementing any business logic. Avoids rework by configuring port handling, Key Vault references, and Node.js versions correctly from the start.

**Delivers:**
- .NET 10 backend with Minimal APIs health check endpoint
- React 19 + Vite frontend with routing skeleton
- Azure resources provisioned (App Service, Key Vault with placeholder secrets)
- Development environment configured (Node 22+, .NET 10 SDK)

**Addresses:**
- Development environment setup (from STACK.md)
- Azure deployment prerequisites (from ARCHITECTURE.md)

**Avoids:**
- Azure App Service port binding issues (Pitfall #6)
- Node.js version mismatch between local and Azure (Pitfall #13)

**Research needed:** None (standard project setup)

---

### Phase 2: Spotify OAuth Integration
**Rationale:** Authentication blocks all Spotify functionality (playlist creation, user library access, track searches). Must be implemented early and correctly as refresh token invalidation issues are hard to debug later. OAuth complexity requires dedicated phase.

**Delivers:**
- Backend endpoints: `/auth/login`, `/auth/callback` with state validation
- Authorization Code Flow with PKCE implementation
- Server-side refresh token storage (HTTP-only cookies)
- Frontend "Login with Spotify" button and auth state management
- Token refresh logic with `invalid_grant` error handling (re-auth flow)

**Uses:**
- SpotifyAPI-NET 7.2.1 (from STACK.md)
- Backend-For-Frontend (BFF) pattern (from ARCHITECTURE.md)

**Addresses:**
- Spotify OAuth requirement (table stakes from FEATURES.md)
- Session management architecture (from ARCHITECTURE.md)

**Avoids:**
- OAuth redirect URI misconfiguration (Pitfall #2)
- Refresh token invalidation without handling (Pitfall #3)
- Playlist scope confusion (Pitfall #10)
- Storing tokens in localStorage (Anti-pattern #1 from ARCHITECTURE.md)

**Research needed:** None (OAuth is well-documented in Spotify docs and PITFALLS.md covers edge cases)

---

### Phase 3: Artist Extraction with Claude Vision
**Rationale:** Artist extraction is the critical path - both poster upload and text input are entry points to the core workflow. Anthropic integration is complex (rate limits, image preprocessing, prompt caching) and needs dedicated phase. Must happen before track selection since you can't select tracks without artist list.

**Delivers:**
- Frontend image upload component with client-side validation (<5MB)
- Backend image processing service (resize to 1568px, PNG→JPEG conversion)
- Anthropic Vision API integration with Claude Sonnet 4.5
- System prompt for artist extraction with JSON output
- Festival name text input with web search integration
- Artist list display UI with manual editing capability
- Anthropic Tier 2 setup ($40 deposit for 1,000 RPM)
- Exponential backoff retry logic for 429 rate limits
- Prompt caching for system prompts (>1,024 tokens)

**Uses:**
- Anthropic.SDK 5.9.0 (from STACK.md)
- Claude Vision API with base64 image input (from ARCHITECTURE.md)
- Image preprocessing patterns (from PITFALLS.md)

**Addresses:**
- Poster upload with artist extraction (MVP feature from FEATURES.md)
- Text input for festival name (differentiator from FEATURES.md)
- Artist list editing (table stakes from FEATURES.md)

**Avoids:**
- Anthropic Tier 1 rate limits (Pitfall #4)
- 5MB image size limit errors (Pitfall #5)
- Prompt caching below 1,024 tokens (Pitfall #11)

**Research needed:** Medium — Claude Vision prompting strategies for festival posters (artistic typography, complex backgrounds). Consider `/gsd:research-phase` for optimal prompt engineering.

---

### Phase 4: Track Selection & Personalization
**Rationale:** Track selection is the intelligence layer and key differentiator (familiar + top + recent). Requires Spotify OAuth working (Phase 2) to access user's saved tracks. Spotify Search API is straightforward but personalization logic needs careful implementation.

**Delivers:**
- Spotify Search service (query artists, batch searches)
- User saved tracks access (for "familiar" selection)
- Track selection algorithm:
  - 3 familiar tracks (cross-reference user's library with festival artists)
  - 3 top tracks (Spotify's popularity metric)
  - 3 recent releases (filter by release date)
- Duplicate prevention across artist selections
- Track count per artist configuration (default 9, customizable 3-15)
- Preview UI showing selected tracks before playlist creation

**Uses:**
- SpotifyAPI-NET 7.2.1 with Search and Library endpoints (from STACK.md)
- Service Layer Abstraction pattern (from ARCHITECTURE.md)

**Addresses:**
- Personalized track selection (key differentiator from FEATURES.md)
- Track selection logic (table stakes from FEATURES.md)
- Duplicate prevention (competitive feature from FEATURES.md)
- Preview before creation (competitive feature from FEATURES.md)

**Avoids:**
- Generic top-tracks-only approach (addresses competitor weakness from FEATURES.md)

**Research needed:** Low — Spotify API is well-documented, track selection logic is business logic not technical complexity.

---

### Phase 5: Playlist Creation & Results Display
**Rationale:** Combines track selection (Phase 4) with Spotify Playlist API to deliver core output. Includes error handling for rate limits and skipped artists. Background job pattern optional for MVP (can add if needed based on testing).

**Delivers:**
- Spotify Playlist creation service (create playlist, add tracks in batches)
- Skipped artists tracking (artists not found in Spotify)
- Playlist naming (default: "[Festival Name] [Year] - Conflicted Lineup", customizable)
- Optional: Background job queue for large lineups (50+ artists)
- Frontend result page with:
  - Direct link to Spotify playlist (deep link for mobile)
  - List of included artists
  - List of skipped artists with transparency messaging
- Loading states with progress indicators

**Uses:**
- SpotifyAPI-NET Playlist endpoints (from STACK.md)
- Optional: Background job pattern with .NET BackgroundService (from ARCHITECTURE.md)

**Addresses:**
- Playlist creation (table stakes from FEATURES.md)
- Link to created playlist (table stakes from FEATURES.md)
- Skipped artists display (table stakes from FEATURES.md)
- Playlist naming customization (competitive feature from FEATURES.md)
- Loading states during processing (table stakes from FEATURES.md)

**Avoids:**
- Synchronous processing timeout for large lineups (Anti-pattern #3 from ARCHITECTURE.md)
- Spotify API rate limits (implement retry logic)

**Research needed:** None (straightforward Spotify API usage)

---

### Phase 6: Azure Deployment & Production Hardening
**Rationale:** Defer deployment complexity until core functionality works locally. Azure-specific issues (Front Door CORS, Key Vault permissions, build configuration) are separate from business logic and can be tackled once MVP is validated.

**Delivers:**
- Azure App Service deployment configuration (Premium v4)
- Azure Key Vault integration with Managed Identity
- Secrets migration from local env vars to Key Vault
- Azure Front Door setup:
  - Routing rules (`/*` → SPA, `/api/*` → Backend)
  - Dynamic CORS headers via Rules Engine
  - Custom domain with HTTPS
- Build automation configuration (`SCM_DO_BUILD_DURING_DEPLOYMENT=true`)
- Application Insights monitoring with connection strings
- Production OAuth redirect URI configuration
- Environment-specific settings (dev vs. prod)

**Uses:**
- Azure App Service, Key Vault, Front Door (from STACK.md)
- Managed Identity pattern (from ARCHITECTURE.md)

**Addresses:**
- Azure deployment architecture (from ARCHITECTURE.md)
- Production security requirements (from PITFALLS.md)

**Avoids:**
- Key Vault permission propagation delay (Pitfall #7)
- Build configuration not running npm install (Pitfall #8)
- Azure Front Door CORS caching (Pitfall #9)
- Secrets exposed in logs (Pitfall #12)

**Research needed:** Medium — Azure Front Door Rules Engine for dynamic CORS is nuanced. PITFALLS.md covers most issues but may need `/gsd:research-phase` for Front Door configuration if team lacks Azure experience.

---

### Phase Ordering Rationale

**Why OAuth comes before extraction:**
- Track selection and playlist creation both depend on Spotify OAuth (can't test without authenticated user)
- OAuth complexity (PKCE, refresh token handling) deserves dedicated phase
- Authentication bugs are hard to debug if mixed with business logic

**Why extraction comes before track selection:**
- Artist list is input to track selection (dependency)
- Vision API integration has unique challenges (rate limits, image preprocessing) worth isolating
- Can test extraction independently without playlist creation

**Why deployment comes last:**
- Azure-specific pitfalls (CORS, Key Vault, port binding) are orthogonal to business logic
- MVP validation with friends can happen locally before tackling deployment complexity
- Production deployment "unlocks" are binary (either works or doesn't), better to debug with working local app

**Grouping rationale:**
- Authentication is its own phase (complex, blocks everything)
- Artist extraction + track selection could theoretically combine but rate limiting concerns (Anthropic + Spotify both have limits) suggest separating for debugging clarity
- Playlist creation is thin (mostly API calls) but includes background job consideration for large lineups

**How this avoids pitfalls:**
- OAuth setup in Phase 2 catches redirect URI issues (Pitfall #2) before deployment
- Anthropic Tier 2 setup in Phase 3 prevents rate limit issues (Pitfall #4) during testing
- Image preprocessing in Phase 3 catches 5MB limit (Pitfall #5) before user testing
- Key Vault setup in Phase 6 allows 30-minute propagation delay (Pitfall #7) before production cutover

### Research Flags

**Phases likely needing deeper research during planning:**

- **Phase 3 (Artist Extraction):** Claude Vision prompt engineering for festival posters with artistic typography and complex backgrounds. PITFALLS.md notes OCR challenges with creative fonts and visual noise. Recommend `/gsd:research-phase` to explore:
  - Optimal system prompt for artist name extraction (JSON schema, handling varied fonts)
  - Image preprocessing strategies (contrast enhancement, noise reduction)
  - Fallback strategies when Vision API confidence is low
  - Web search integration for festival name input (parsing structured vs. unstructured results)

- **Phase 6 (Azure Deployment):** Azure Front Door Rules Engine for dynamic CORS headers. PITFALLS.md warns about caching issues with multi-origin apps. If team lacks Azure experience, recommend `/gsd:research-phase` to explore:
  - Rules Engine syntax for dynamic `Access-Control-Allow-Origin` based on request header
  - Cache purging strategies after CORS configuration changes
  - OPTIONS preflight handling in Rules Engine
  - Alternative: Using single origin by serving React build from Backend's `wwwroot/` (simpler, no CORS)

**Phases with standard patterns (skip research-phase):**

- **Phase 1 (Foundation):** Standard .NET + React project setup, well-documented in STACK.md
- **Phase 2 (Spotify OAuth):** Authorization Code with PKCE is standard OAuth flow, extensively documented in Spotify docs and PITFALLS.md
- **Phase 4 (Track Selection):** Business logic complexity, not technical integration complexity, no external research needed
- **Phase 5 (Playlist Creation):** Straightforward Spotify API usage, FEATURES.md and STACK.md provide clear guidance

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All versions verified with official sources (.NET 10 from Microsoft downloads, React 19.2.1 from React blog, SpotifyAPI-NET 7.2.1 from GitHub releases, Anthropic.SDK 5.9.0 from NuGet). Critical deprecations confirmed (Implicit Grant flow, HTTP redirect URIs, Instrumentation Keys). |
| Features | HIGH | Based on direct competitor analysis (LineupSupply, spotify-festival-playlist-generator, Spotify Gov Ball) and Spotify API documentation. MVP feature set is validated by existing tools in market. Personalization differentiator is clearly articulated. |
| Architecture | HIGH | Backend-For-Frontend pattern is 2025 security best practice for SPAs with OAuth (verified via Microsoft Learn and OAuth.net). Azure deployment architecture matches official Microsoft samples (React + C# API + SQL Database). Pitfall research validates architectural decisions. |
| Pitfalls | HIGH | 14 critical pitfalls sourced from official Spotify developer blog (OAuth changes Nov 2025, Extended Access May 2025), Anthropic rate limit documentation, Azure App Service troubleshooting guides. All pitfalls have concrete prevention strategies tested in real deployments. |

**Overall confidence:** HIGH

Research is based on official documentation (Microsoft, Spotify, Anthropic, Azure), verified version numbers, and concrete competitor analysis. The domain (playlist generation from lineups) has established patterns and existing solutions to reference. Pitfall research uncovered specific dates and thresholds (25-user limit, Nov 27 2025 OAuth changes, 5MB image limit) reducing ambiguity.

### Gaps to Address

**Gap 1: Claude Vision accuracy with artistic festival typography**
- **What's uncertain:** Real-world accuracy of Claude 4.5 Sonnet on festival posters with creative fonts, overlapping text, and complex backgrounds. PITFALLS.md notes this as a challenge but doesn't quantify expected accuracy rate.
- **How to handle:** Benchmark with 10-20 real festival posters during Phase 3. If accuracy falls below 80%, implement preprocessing (contrast enhancement, noise reduction) or prompt engineering improvements. Consider fallback to manual artist list entry if Vision API confidence scores are low.

**Gap 2: Anthropic prompt caching cost savings**
- **What's uncertain:** Actual cost savings from prompt caching given system prompt token count and cache hit rates. Research confirms >1,024 token minimum but doesn't model real usage patterns.
- **How to handle:** Monitor cache hit rates in Anthropic Console after Phase 3 deployment. If cache hit rate is low (<50%), revisit system prompt structure to maximize reusable content (move variable content like festival name outside cached blocks).

**Gap 3: Spotify Web Search API for festival name input**
- **What's uncertain:** Whether web search should use Anthropic's web search tool, custom Google Search API integration, or manual scraping of known festival websites. FEATURES.md lists this as differentiator but ARCHITECTURE.md doesn't specify implementation.
- **How to handle:** Start with Anthropic's web search tool in Phase 3 (simplest, leverages existing Anthropic integration). If results are unreliable (outdated lineups, unofficial sources), add validation step showing user the source URL for confirmation. Defer custom scraping to v2 unless web search proves inadequate.

**Gap 4: 25-user limit enforcement mechanism**
- **What's uncertain:** How to enforce Spotify's 25-user Development Mode limit. Research confirms limit exists (Pitfall #1) but doesn't specify if Spotify enforces automatically or requires manual tracking.
- **How to handle:** Add user count tracking in Phase 2 (store user IDs in database or Redis). Show error message when 26th user attempts to authenticate: "App limited to 25 users (Spotify restriction)". Monitor Spotify developer dashboard for automatic enforcement. If planning to approach limit, communicate with friend group about prioritization.

**Gap 5: Background job storage for large lineups**
- **What's uncertain:** Whether to use in-memory job queue, Redis, or SQL database for background job status tracking. ARCHITECTURE.md mentions Hangfire + SQL for production but suggests simpler approach for MVP.
- **How to handle:** Start with in-memory queue in Phase 5 (no external dependencies). If app crashes lose job state (acceptable for MVP with <25 users). Upgrade to Redis in Phase 6 if multiple App Service instances are needed for scale (unlikely given 25-user limit).

## Sources

All sources aggregated from the four research files with confidence levels.

### Primary (HIGH confidence)

**Official Documentation:**
- [.NET Downloads - Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet) — .NET 10 LTS verification (version 10.0.2 released Jan 13, 2026)
- [React v19.2 - React Blog](https://react.dev/blog) — React version verification (security patch Dec 3, 2025)
- [ASP.NET Core APIs Overview - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis?view=aspnetcore-10.0) — Minimal APIs recommendation for new projects
- [Azure App Service Best Practices - Microsoft Learn](https://learn.microsoft.com/en-us/azure/app-service/app-service-best-practices) — Infrastructure guidance and Premium v4 features
- [Azure Key Vault Developer's Guide - Microsoft Learn](https://learn.microsoft.com/en-us/azure/key-vault/general/developers-guide) — Managed Identity security patterns

**Spotify API:**
- [Updating the Criteria for Web API Extended Access](https://developer.spotify.com/blog/2025-04-15-updating-the-criteria-for-web-api-extended-access) — 25-user Development Mode limit, 250K+ user requirement for extended access
- [Reminder: OAuth Migration - 27 November 2025](https://developer.spotify.com/blog/2025-10-14-reminder-oauth-migration-27-nov-2025) — Implicit Grant removal, HTTP redirect URI deprecation, PKCE requirement
- [Spotify Authorization PKCE Flow](https://developer.spotify.com/documentation/web-api/tutorials/code-pkce-flow) — OAuth implementation details
- [Spotify API Rate Limits - Official Docs](https://developer.spotify.com/documentation/web-api/concepts/rate-limits) — 30-second rolling window, extended quota requirements
- [Spotify API Scopes Documentation](https://developer.spotify.com/documentation/web-api/concepts/scopes) — Playlist scope requirements

**Anthropic API:**
- [Claude API Rate Limits](https://platform.claude.com/docs/en/api/rate-limits) — Tier system (Tier 1 = 50 RPM, Tier 2 = 1,000 RPM)
- [Claude Vision Documentation](https://platform.claude.com/docs/en/build-with-claude/vision) — 5MB image limit, 1568px optimal resolution
- [Prompt Caching](https://platform.claude.com/docs/en/build-with-claude/prompt-caching) — 1,024 token minimum for cache breakpoints
- [Anthropic.SDK NuGet](https://www.nuget.org/packages/Anthropic.SDK) — Package version 5.9.0 (Jan 22, 2026)

**Azure:**
- [Configure Node.js Apps - Azure App Service](https://learn.microsoft.com/en-us/azure/app-service/configure-language-nodejs) — `process.env.PORT` requirement, PM2 configuration
- [Use Key Vault References as App Settings](https://learn.microsoft.com/en-us/azure/app-service/app-service-key-vault-references) — Managed Identity integration, permission propagation delays
- [Using Azure Front Door with CORS](https://learn.microsoft.com/en-us/azure/frontdoor/standard-premium/troubleshoot-cross-origin-resources) — CORS caching issues, Rules Engine solutions

### Secondary (MEDIUM-HIGH confidence)

**Community Resources:**
- [SpotifyAPI-NET Releases](https://github.com/JohnnyCrazy/SpotifyAPI-NET/releases) — Library version 7.2.1 (Oct 12, 2024)
- [React Router Documentation](https://reactrouter.com/) — Latest version 7.12.0
- [TanStack Query Documentation](https://tanstack.com/query/latest) — Version 5.90.20 (Jan 2025)
- [Vite Releases](https://vite.dev/releases) — Version 7.3.1
- [React State Management 2025 Comparison](https://dev.to/cristiansifuentes/react-state-management-in-2025-context-api-vs-zustand-385m) — Zustand vs alternatives
- [xUnit vs NUnit 2025 Comparison](https://www.tatvasoft.com/outsourcing/2025/06/xunit-vs-nunit-vs-mstest.html) — Testing framework recommendation
- [Azure Bicep vs Terraform 2025](https://xebia.com/blog/infrastructure-as-code-on-azure-bicep-vs-terraform-vs-pulumi/) — IaC comparison for Azure

**Architecture Best Practices:**
- [Using OAuth for Single Page Applications | Best Practices](https://curity.io/resources/learn/spa-best-practices/) — Backend-For-Frontend pattern for SPAs
- [Modern Authentication on .NET: OpenID Connect, BFF, SPA](https://docs.abblix.com/docs/net-authentication-openid-connect-bff-spa) — BFF implementation with .NET
- [React Folder Structure in 5 Steps [2025]](https://www.robinwieruch.de/react-folder-structure/) — Feature-based organization
- [Background Jobs in .NET: Hangfire, Quartz, Temporal in 2026](https://medium.com/net-code-chronicles/background-jobs-schedulers-dotnet-abfbf49aa79f) — Background job patterns

**Competitor Analysis:**
- [LineupSupply app coverage - TechCrunch](https://techcrunch.com/2022/09/06/lineupsupplys-app-turns-music-festival-posters-into-spotify-playlists/) — iOS app using Apple Vision Framework for local OCR
- [spotify-festival-playlist-generator - GitHub](https://github.com/AustinLowey/spotify-festival-playlist-generator) — Web app with manual text input, checkbox artist selection
- [Spotify Gov Ball 2026 Experience - Spotify Newsroom](https://newsroom.spotify.com/2026-01-06/gov-ball-lineup-experience/) — Official Spotify integration with personalized listening history

### Tertiary (MEDIUM confidence)

**Technical Deep Dives:**
- [.NET 10 What's New - Medium](https://medium.com/@sparklewebhelp/net-10-whats-new-in-2025-the-complete-guide-c7b030b490df) — Feature overview and C# 14 improvements
- [React 19 Features 2025 - GrapesTech](https://www.grapestechsolutions.com/blog/reactjs-latest-version-19-updates/) — Server Components and Actions API production readiness
- [Anthropic Claude API Pricing 2026: Complete Cost Breakdown](https://www.metacto.com/blogs/anthropic-api-pricing-a-full-breakdown-of-costs-and-integration) — Tier pricing and token costs
- [Claude API Quota Tiers Guide 2026](https://www.aifreeapi.com/en/posts/claude-api-quota-tiers-limits) — Tier requirements and limits
- [Spotify playlist limit (10,000 tracks) - NoteBurner](https://www.noteburner.com/spotify-music-tips/spotify-playlist-limit.html) — Playlist constraints
- [OCR challenges with artistic text - research.aimultiple.com](https://research.aimultiple.com/ocr-technology/) — OCR accuracy factors

---
*Research completed: 2026-01-24*
*Ready for roadmap: yes*
