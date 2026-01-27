---
phase: 04-track-selection
plan: 03
subsystem: api
tags: [spotify, user-library, orchestrator, deduplication, dependency-injection]

# Dependency graph
requires:
  - phase: 04-02
    provides: SpotifySearchService, SpotifyTopTracksService, SpotifyRecentReleasesService, TrackInfo model
provides:
  - SpotifyUserLibraryService for familiar tracks scanning
  - SpotifyTrackService orchestrator with 3+3+3 formula
  - Full Spotify service DI registration
  - Track deduplication using Spotify Track IDs
affects: [04-04, 05-playlist-creation]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "User library scanning with limits (500 saved tracks, 50 playlists)"
    - "Orchestrator pattern with injected sub-services"
    - "Track deduplication with HashSet<string> and priority ordering"

key-files:
  created:
    - backend/src/ConflictedLineup.Api/Services/SpotifyUserLibraryService.cs
    - backend/src/ConflictedLineup.Api/Services/SpotifyTrackService.cs
  modified:
    - backend/src/ConflictedLineup.Api/Program.cs

key-decisions:
  - "500 saved tracks + 50 playlists max scan limits to prevent API rate limiting and timeouts"
  - "Only scan user-owned playlists (not followed) per CONTEXT.md"
  - "Deduplication priority: familiar > top > recent"
  - "Backfill top tracks when no recent releases (up to 6 total from top)"

patterns-established:
  - "User library service: Paginate() with break conditions for memory-efficient scanning"
  - "Orchestrator: Sequential artist processing with progress reporting"
  - "Deduplication: HashSet<string> with SpotifyTrackId as canonical identifier"

# Metrics
duration: 8min
completed: 2026-01-27
---

# Phase 4 Plan 03: Track Selection Orchestrator Summary

**User library scanning with 500/50 limits and orchestrator service implementing 3+3+3 formula with HashSet deduplication**

## Performance

- **Duration:** 8 min
- **Started:** 2026-01-27T
- **Completed:** 2026-01-27T
- **Tasks:** 3
- **Files modified:** 3

## Accomplishments
- SpotifyUserLibraryService scans saved tracks and user-owned playlists for familiar tracks
- SpotifyTrackService orchestrates all four Spotify services with deduplication
- All Spotify services registered in DI container with Scoped lifetime
- Backfill logic for artists with no recent releases

## Task Commits

Each task was committed atomically:

1. **Task 1: Create user library service for familiar tracks** - `b8ff2e5` (feat)
2. **Task 2: Create track selection orchestrator service** - `294ed82` (feat)
3. **Task 3: Register services in DI container** - `5888aee` (chore)

## Files Created/Modified
- `backend/src/ConflictedLineup.Api/Services/SpotifyUserLibraryService.cs` - Scans saved tracks and user-owned playlists
- `backend/src/ConflictedLineup.Api/Services/SpotifyTrackService.cs` - Orchestrates 3+3+3 track selection with deduplication
- `backend/src/ConflictedLineup.Api/Program.cs` - DI registration for all 5 Spotify services

## Decisions Made
- **Scan limits:** 500 saved tracks + 50 playlists max per RESEARCH.md recommendation (prevents rate limiting and timeouts)
- **Playlist ownership filter:** Only scan playlists where owner.Id == userId (per CONTEXT.md: not followed playlists)
- **Deduplication priority:** familiar > top > recent (preserves personalized tracks first)
- **Backfill strategy:** When recent releases < 3, add more top tracks (up to 6 total from top)

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- Build failed initially due to file lock from running backend process - killed process and rebuild succeeded

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Track selection orchestrator complete and ready for endpoint integration
- 04-04 can now implement POST /api/tracks/select endpoint using ISpotifyTrackService
- All services registered and injectable

---
*Phase: 04-track-selection*
*Completed: 2026-01-27*
