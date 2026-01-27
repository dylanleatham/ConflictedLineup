---
phase: 04-track-selection
plan: 01
completed: 2026-01-26
duration: ~3 min
status: complete

subsystem: backend-api, frontend-auth
tags: [oauth, spotify-api, models, dotnet]

dependency-graph:
  requires: [02-01, 03-01]
  provides: [spotify-library-scopes, spotifyapi-web-package, track-models]
  affects: [04-02, 04-03, 04-04]

tech-stack:
  added:
    - SpotifyAPI.Web@7.2.1: Type-safe Spotify API client for .NET
  patterns:
    - C# records for DTOs

key-files:
  created:
    - backend/src/ConflictedLineup.Api/Models/TrackModels.cs
  modified:
    - frontend/src/auth/SpotifyAuthConfig.ts
    - backend/src/ConflictedLineup.Api/ConflictedLineup.Api.csproj

decisions:
  - id: oauth-library-scopes
    choice: Add user-library-read and playlist-read-collaborative scopes
    rationale: Required for accessing saved tracks and collaborative playlists in Phase 4
  - id: spotifyapi-web-package
    choice: Use SpotifyAPI.Web NuGet package
    rationale: Type-safe Spotify API access, maintained library, simpler than raw HTTP

metrics:
  tasks: 2
  commits: 2
  lines-added: ~60
  lines-modified: ~1
---

# Phase 04 Plan 01: Foundation Setup Summary

OAuth library scopes added for saved tracks access, SpotifyAPI.Web 7.2.1 installed, track selection DTOs created.

## What Was Built

### 1. OAuth Scope Updates
Updated `SpotifyAuthConfig.ts` to request library-related permissions:
- `user-library-read` - Access user's saved tracks for "familiar tracks" feature
- `playlist-read-collaborative` - Read collaborative playlists for complete library access

**Note:** Existing users will need to re-authenticate to grant the new permissions. The OAuth library handles this automatically when attempting operations requiring missing scopes.

### 2. SpotifyAPI.Web Package
Installed `SpotifyAPI.Web` version 7.2.1 - a type-safe .NET client for the Spotify Web API:
- Full coverage of Spotify Web API endpoints
- Strongly-typed request/response models
- Built-in retry handling and error management

### 3. Track Selection Models
Created `TrackModels.cs` with 6 record types following the existing pattern in `ExtractionModels.cs`:

| Model | Purpose |
|-------|---------|
| `TrackSelectionRequest` | Frontend request with artist names and Spotify token |
| `TrackSelectionResponse` | Complete response with artist results and skipped artists |
| `ArtistTrackResult` | Per-artist track results (familiar, top, recent) |
| `TrackInfo` | Individual track metadata |
| `SkippedArtist` | Artist that couldn't be matched on Spotify |
| `ArtistProgressUpdate` | Progress updates for streaming responses |

## Commits

| Hash | Type | Description |
|------|------|-------------|
| e78de6d | feat | Add library OAuth scopes for familiar tracks |
| 946b854 | feat | Add SpotifyAPI.Web and track selection models |

## Deviations from Plan

None - plan executed exactly as written.

## Verification Results

- [x] Frontend builds with no TypeScript errors
- [x] Backend builds with no C# compilation errors
- [x] SpotifyAPI.Web package reference in csproj
- [x] OAuth scope includes both new scopes
- [x] All 6 model types compile correctly

## Next Steps

Plan 04-02 will implement the `TrackSelectionService` that uses these models and the SpotifyAPI.Web package to:
1. Search for artists on Spotify
2. Get user's saved tracks for each artist (familiar)
3. Get artist top tracks (discovery)
4. Get artist's recent releases (likely live)
