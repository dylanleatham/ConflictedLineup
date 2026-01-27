---
phase: 04-track-selection
plan: 02
subsystem: api
tags: [spotify, web-api, artist-search, top-tracks, recent-releases, rate-limiting]

# Dependency graph
requires:
  - phase: 04-01
    provides: SpotifyAPI.Web 7.2.1 library, TrackModels.cs DTOs
provides:
  - ISpotifySearchService for artist name-to-ID lookup
  - ISpotifyTopTracksService for popularity-ranked tracks
  - ISpotifyRecentReleasesService for latest release tracks
  - Rate limiting retry pattern with Retry-After header handling
affects: [04-03-orchestrator, 04-04-controller]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "ISpotifyClient parameter injection (token-based instantiation)"
    - "Rate limiting with Retry-After header and 3 max retries"
    - "US market parameter for content availability"

key-files:
  created:
    - backend/src/ConflictedLineup.Api/Services/SpotifySearchService.cs
    - backend/src/ConflictedLineup.Api/Services/SpotifyTopTracksService.cs
    - backend/src/ConflictedLineup.Api/Services/SpotifyRecentReleasesService.cs
  modified: []

key-decisions:
  - "Accept ISpotifyClient as parameter (not self-instantiating) for orchestrator token control"
  - "Use first Spotify search result per CONTEXT.md trust algorithm decision"
  - "Prioritize singles over album tracks via date comparison and popularity sorting"
  - "90-day window to determine if single is from same album cycle"

patterns-established:
  - "Spotify service pattern: Interface + implementation with ILogger, accepts ISpotifyClient"
  - "Rate limit retry: ExecuteWithRetryAsync with Retry-After header parsing"
  - "FullTrack to TrackInfo mapping for consistent response model"

# Metrics
duration: 3min
completed: 2026-01-27
---

# Phase 4 Plan 2: Spotify Services Summary

**Three core Spotify services (search, top tracks, recent releases) with rate limiting and ISpotifyClient injection**

## Performance

- **Duration:** 3 min
- **Started:** 2026-01-27T04:05:50Z
- **Completed:** 2026-01-27T04:08:52Z
- **Tasks:** 3
- **Files modified:** 3

## Accomplishments
- Artist search service returns first Spotify result or null for unmatched names
- Top tracks service returns up to 3 most popular tracks via Artists.GetTopTracks
- Recent releases service prioritizes singles, sorts album tracks by popularity
- All services implement rate limiting with Retry-After header handling

## Task Commits

Each task was committed atomically:

1. **Task 1: Create Spotify artist search service** - `903447e` (feat)
2. **Task 2: Create Spotify top tracks service** - `6fc6ea5` (feat)
3. **Task 3: Create Spotify recent releases service** - `822d411` (feat)

## Files Created/Modified
- `backend/src/ConflictedLineup.Api/Services/SpotifySearchService.cs` - Artist name lookup with first-result trust
- `backend/src/ConflictedLineup.Api/Services/SpotifyTopTracksService.cs` - Popularity-ranked tracks (max 3)
- `backend/src/ConflictedLineup.Api/Services/SpotifyRecentReleasesService.cs` - Latest release tracks with single prioritization

## Decisions Made
- **ISpotifyClient parameter injection:** Services don't self-instantiate clients. Orchestrator controls token lifecycle and can share client across calls.
- **First search result trust:** Per CONTEXT.md, trust Spotify's ranking algorithm rather than implementing custom verification.
- **Single prioritization logic:** If single is within 90 days of album release (same album cycle) or more recent, use single. Otherwise use album.
- **Popularity sorting for albums:** When recent release is an album, fetch full track details and sort by popularity to find likely singles.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- Build output locked by running process - resolved by building to alternate output directory (obj/verify)

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Three services ready for orchestrator integration in 04-03
- All services use TrackInfo model from TrackModels.cs
- Rate limiting pattern established for consistent retry behavior
- Services accept ISpotifyClient so orchestrator can manage token

---
*Phase: 04-track-selection*
*Completed: 2026-01-27*
