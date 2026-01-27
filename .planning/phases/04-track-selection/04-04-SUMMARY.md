---
phase: 04-track-selection
plan: 04
completed: 2026-01-27
duration: ~45 min (including debugging)
status: complete

subsystem: api, frontend
tags: [track-selection, spotify-api, react, endpoint]

dependency-graph:
  requires: [04-03]
  provides: [track-selection-endpoint, track-selection-page]
  affects: [05-playlist-creation]

tech-stack:
  added: []
  patterns:
    - "useRef guard to prevent duplicate API calls"
    - "Navigation state for passing data between pages"
    - "Graceful error handling with retry option"

key-files:
  created:
    - backend/src/ConflictedLineup.Api/Controllers/TrackSelectionController.cs
    - frontend/src/services/trackSelectionApi.ts
    - frontend/src/types/trackSelection.ts
    - frontend/src/pages/TrackSelectionPage.tsx
    - frontend/src/pages/TrackSelectionPage.css
  modified:
    - frontend/src/pages/UploadPage.tsx
    - frontend/src/App.tsx

decisions:
  - id: limit-5-search
    choice: Use Limit=5 for Spotify artist search instead of Limit=1
    rationale: Limit=1 caused Spotify to return wrong/cached results when rate limited
  - id: top-5-tracks-only
    choice: Simplified to top 5 tracks only (disabled familiar/recent)
    rationale: Minimize API calls to avoid rate limiting; can re-enable later
  - id: useref-guard
    choice: Use useRef to prevent duplicate API calls
    rationale: React StrictMode runs effects twice; ref survives re-renders

metrics:
  tasks: 3
  commits: 12 (including fixes)
  lines-added: ~500
---

# Phase 04 Plan 04: API Endpoint & Track Selection Page Summary

**POST /api/tracks/select endpoint and TrackSelectionPage with artist results display**

## Performance

- **Duration:** ~45 min (extended due to debugging rate limiting and search issues)
- **Tasks:** 3
- **Commits:** 12 (3 planned + 9 fixes)

## Accomplishments

- Created TrackSelectionController with POST /api/tracks/select endpoint
- Created frontend API client and TypeScript types
- Created TrackSelectionPage with loading state, results display, and error handling
- Added navigation from UploadPage to TrackSelectionPage with artist state
- Added /callback redirect route for OAuth completion

## Issues Encountered & Resolved

1. **OAuth callback blank page** - Added Navigate redirect for /callback route
2. **Infinite API calls** - React useCallback with unstable deps caused re-render loop; fixed with useRef guard
3. **Wrong artist results (Subtronics)** - Spotify search with Limit=1 returned cached/wrong results when rate limited; fixed by using Limit=5
4. **Rate limiting** - Added 1-second delay between artists, graceful degradation for library scanning

## Key Decisions

- **Simplified track selection**: Disabled familiar tracks and recent releases to minimize API calls. Only fetches top 5 tracks per artist. Can re-enable once rate limiting is better understood.
- **Search limit increase**: Changed from Limit=1 to Limit=5 for artist search to avoid Spotify returning incorrect cached results during rate limiting.

## Task Commits

1. **Task 1: Create track selection API endpoint** - `f64167e`
2. **Task 2: Create frontend API client and types** - `0adcd09`
3. **Task 3: Create track selection page and navigation** - `11a3e2d`

Plus debugging/fix commits for callback redirect, infinite loop, rate limiting, and search issues.

## Verification

- [x] Backend builds successfully
- [x] Frontend builds successfully
- [x] Navigation from UploadPage works
- [x] Track selection returns correct artists
- [x] Results display with artist names and track lists
- [x] "Create Playlist" button visible (Phase 5 placeholder)

## Next Steps

Phase 5 will implement playlist creation - taking the selected tracks and creating a Spotify playlist.

---
*Phase: 04-track-selection*
*Completed: 2026-01-27*
