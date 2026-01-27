# Phase 5: Playlist Creation & Results - Research

**Researched:** 2026-01-27
**Domain:** Spotify Web API playlist creation and React results display
**Confidence:** HIGH

## Summary

Phase 5 completes the application flow by creating a Spotify playlist from selected tracks and displaying results to the user. The standard approach uses the Spotify Web API's Create Playlist and Add Items endpoints through the SpotifyAPI.Web library (already in use). The playlist name comes from festival/event context captured during Phase 3 extraction. Results display requires a mobile-responsive React component showing included artists, skipped artists, and a direct link to the created playlist.

Key technical considerations: batch track additions (max 100 per request), rate limiting with Retry-After header support, and festival name context propagation through the user flow.

**Primary recommendation:** Create a dedicated PlaylistService following existing service patterns, extend TrackSelectionResponse to include festival context, and build a results page that transforms the track selection page into playlist confirmation UI.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| SpotifyAPI.Web | 7.2.1 | Spotify Web API client | Already in use for Phase 4, provides fully typed requests/responses for playlist operations |
| Material-UI (MUI) | Latest | React UI components | Already in use for Phase 2-4, provides responsive Grid/Card/List components |
| React Router | Current | Navigation with state | Already in use, supports passing playlist context through navigation |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| N/A | - | No additional libraries needed | Existing stack covers all requirements |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| SpotifyAPI.Web | Raw HTTP calls | Library provides type safety, error handling, rate limiting - no benefit to raw calls |
| MUI Grid | Custom CSS Grid | MUI provides battle-tested responsive breakpoints and mobile optimization |

**Installation:**
No new packages required - all dependencies already installed in Phase 2 and Phase 4.

## Architecture Patterns

### Recommended Project Structure
```
backend/src/ConflictedLineup.Api/
├── Services/
│   ├── SpotifyPlaylistService.cs    # New: Create playlist, add tracks
│   └── SpotifyTrackService.cs       # Exists: Track selection orchestration
├── Controllers/
│   └── PlaylistController.cs        # New: POST /api/playlists/create
├── Models/
│   ├── PlaylistModels.cs            # New: Request/response DTOs
│   └── TrackModels.cs               # Exists: Track selection DTOs

frontend/src/
├── pages/
│   ├── TrackSelectionPage.tsx       # Modify: Add "Create Playlist" action
│   └── PlaylistResultsPage.tsx      # New: Show results with link
├── services/
│   └── playlistApi.ts               # New: API client for playlist creation
├── types/
│   └── playlist.ts                  # New: TypeScript interfaces
```

### Pattern 1: Service Layer for Playlist Operations
**What:** Dedicated service class that encapsulates Spotify playlist API operations
**When to use:** Always - follows established pattern from Phase 4 (SpotifyTrackService)
**Example:**
```csharp
// Source: Existing pattern from SpotifyTrackService.cs
public interface ISpotifyPlaylistService
{
    Task<PlaylistCreationResult> CreatePlaylistAsync(
        string userId,
        string playlistName,
        List<string> trackUris,
        string accessToken);
}

public class SpotifyPlaylistService : ISpotifyPlaylistService
{
    public async Task<PlaylistCreationResult> CreatePlaylistAsync(
        string userId,
        string playlistName,
        List<string> trackUris,
        string accessToken)
    {
        var spotify = new SpotifyClient(accessToken);

        // Step 1: Create empty playlist
        var createRequest = new PlaylistCreateRequest(playlistName)
        {
            Public = false,
            Description = "Created by Conflicted Lineup"
        };
        var playlist = await spotify.Playlists.Create(userId, createRequest);

        // Step 2: Add tracks in batches of 100
        await AddTracksInBatches(spotify, playlist.Id, trackUris);

        return new PlaylistCreationResult(playlist.Id, playlist.ExternalUrls["spotify"]);
    }
}
```

### Pattern 2: Batch Track Addition with Rate Limiting
**What:** Split track URIs into batches of 100, add with retry logic for 429 errors
**When to use:** When adding >100 tracks or when rate limiting is a concern
**Example:**
```csharp
// Source: Spotify Web API documentation + existing ExecuteWithRetryAsync pattern
private async Task AddTracksInBatches(ISpotifyClient spotify, string playlistId, List<string> trackUris)
{
    const int batchSize = 100;

    for (int i = 0; i < trackUris.Count; i += batchSize)
    {
        var batch = trackUris.Skip(i).Take(batchSize).ToList();
        var request = new PlaylistAddItemsRequest(batch);

        await ExecuteWithRetryAsync(async () =>
            await spotify.Playlists.AddItems(playlistId, request));

        // Small delay between batches to avoid rate limiting
        if (i + batchSize < trackUris.Count)
        {
            await Task.Delay(500); // 500ms between batches
        }
    }
}
```

### Pattern 3: Festival Context Propagation
**What:** Pass festival name from Phase 3 extraction through to Phase 5 playlist creation
**When to use:** Required for PLAYLIST-02 (playlist named after festival/event)
**Example:**
```typescript
// Extend TrackSelectionResponse to include festival context
export interface TrackSelectionResponse {
  artists: ArtistTrackResult[];
  skipped: SkippedArtist[];
  festivalName?: string;  // NEW: From extraction phase
  year?: number;           // NEW: From extraction phase
}

// Use in playlist creation
const playlistName = festivalName
  ? `${festivalName} ${year || ''}`.trim()
  : 'My Festival Playlist';
```

### Pattern 4: Mobile-Responsive Results Layout
**What:** MUI Grid with responsive breakpoints for artist list and results display
**When to use:** For RESULTS-04 mobile responsiveness requirement
**Example:**
```typescript
// Source: MUI responsive UI documentation
<Grid container spacing={{ xs: 2, md: 3 }}>
  <Grid xs={12} md={8}>
    {/* Artist list - full width on mobile, 2/3 on desktop */}
    <ArtistIncludedList artists={includedArtists} />
  </Grid>
  <Grid xs={12} md={4}>
    {/* Skipped list - full width on mobile, 1/3 on desktop */}
    <SkippedArtistsList artists={skippedArtists} />
  </Grid>
</Grid>
```

### Anti-Patterns to Avoid
- **Adding tracks one at a time:** Use batch API (max 100) to minimize API calls and rate limiting
- **Hardcoded playlist names:** Use festival context from extraction phase per requirements
- **Creating public playlists:** Default to private unless explicitly requested (privacy-first)
- **Missing festival context:** Must propagate from Phase 3 → Phase 4 → Phase 5 for proper playlist naming

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Spotify playlist API | Raw HTTP client with manual JSON serialization | SpotifyAPI.Web library (already installed) | Library handles request/response typing, rate limiting, error codes |
| Track URI batching | Custom loop with manual batch management | Pattern from research with tested batch size (100) | Spotify API has strict 100-item limit, easy to get wrong |
| Rate limit retry | Custom exponential backoff | ExecuteWithRetryAsync pattern (already in SpotifyTrackService) | Existing pattern handles Retry-After header correctly |
| Mobile responsive layout | Custom media queries | MUI Grid with breakpoints | Battle-tested responsive system, consistent with existing pages |

**Key insight:** Phase 4 already established service patterns and rate limiting infrastructure. Reuse these patterns rather than creating new approaches for playlist operations.

## Common Pitfalls

### Pitfall 1: Forgetting Festival Context
**What goes wrong:** Playlist created with generic name like "New Playlist" instead of festival name
**Why it happens:** Festival name captured in Phase 3 but not passed through Phase 4 to Phase 5
**How to avoid:** Extend navigation state and API responses to include festival context
**Warning signs:**
- TrackSelectionResponse doesn't include festivalName field
- Navigate to results page without festival context in state
- API only accepts track URIs without playlist name

### Pitfall 2: Exceeding 100 Tracks Per Request
**What goes wrong:** API returns 400 Bad Request when adding >100 tracks at once
**Why it happens:** Spotify API has hard limit of 100 items per Add Items request
**How to avoid:** Always batch track URIs in groups of 100 max, add sequentially
**Warning signs:**
- Error when processing festivals with >100 total tracks
- Works for small lineups but fails for large festivals
- Error message about request size or URI list length

### Pitfall 3: Rate Limiting Without Retry-After
**What goes wrong:** App gets rate limited (429) and either fails or retries too aggressively
**Why it happens:** Not respecting Retry-After header from Spotify API
**How to avoid:** Reuse ExecuteWithRetryAsync pattern from Phase 4 that reads Retry-After header
**Warning signs:**
- Repeated 429 errors in logs
- Fixed delay retry that doesn't respect Spotify's guidance
- Multiple retries within same second

### Pitfall 4: Not Handling Spotify Playlist URL Format
**What goes wrong:** Broken links to playlist or incorrect URL construction
**Why it happens:** Manually constructing URLs instead of using external_urls from API response
**How to avoid:** Use playlist.ExternalUrls["spotify"] from API response (format: https://open.spotify.com/playlist/{id})
**Warning signs:**
- Manual string concatenation to build URLs
- Using internal Spotify URIs (spotify:playlist:id) as web links
- Links that don't work on mobile devices

### Pitfall 5: Mobile Responsiveness Breakage
**What goes wrong:** Results page looks good on desktop but breaks on mobile
**Why it happens:** Fixed widths, improper Grid sizing, missing responsive breakpoints
**How to avoid:** Use MUI Grid with xs, md breakpoints; test on mobile viewport
**Warning signs:**
- Horizontal scrolling on mobile
- Text overflow or truncation
- Columns stacked poorly on small screens

## Code Examples

Verified patterns from official sources and existing codebase:

### Create Playlist and Add Tracks
```csharp
// Source: Spotify Web API Reference + SpotifyAPI.Web IPlaylistsClient
// https://developer.spotify.com/documentation/web-api/reference/create-playlist
// https://github.com/JohnnyCrazy/SpotifyAPI-NET/blob/master/SpotifyAPI.Web/Clients/Interfaces/IPlaylistsClient.cs

public async Task<PlaylistCreationResult> CreatePlaylistAsync(
    string userId,
    string playlistName,
    List<string> trackUris,
    string accessToken)
{
    var spotify = new SpotifyClient(accessToken);

    // Create playlist
    var createRequest = new PlaylistCreateRequest(playlistName)
    {
        Public = false,
        Description = "Created by Conflicted Lineup"
    };

    var playlist = await ExecuteWithRetryAsync(async () =>
        await spotify.Playlists.Create(userId, createRequest));

    if (playlist == null)
    {
        throw new InvalidOperationException("Failed to create playlist");
    }

    // Add tracks in batches of 100
    const int batchSize = 100;
    for (int i = 0; i < trackUris.Count; i += batchSize)
    {
        var batch = trackUris.Skip(i).Take(batchSize).ToList();
        var addRequest = new PlaylistAddItemsRequest(batch);

        await ExecuteWithRetryAsync(async () =>
            await spotify.Playlists.AddItems(playlist.Id, addRequest));

        // Rate limiting prevention - small delay between batches
        if (i + batchSize < trackUris.Count)
        {
            await Task.Delay(500);
        }
    }

    return new PlaylistCreationResult(
        PlaylistId: playlist.Id,
        PlaylistUrl: playlist.ExternalUrls["spotify"],
        PlaylistName: playlist.Name
    );
}
```

### Convert Track Results to Spotify URIs
```csharp
// Source: Existing TrackModels.cs pattern + Spotify URI format
// https://developer.spotify.com/documentation/web-api/concepts/spotify-uris-ids

// In backend service
private List<string> ConvertToTrackUris(TrackSelectionResponse response)
{
    var uris = new List<string>();

    foreach (var artist in response.Artists)
    {
        // Add all track categories in order: familiar, top, recent
        uris.AddRange(artist.FamiliarTracks.Select(t => $"spotify:track:{t.SpotifyTrackId}"));
        uris.AddRange(artist.TopTracks.Select(t => $"spotify:track:{t.SpotifyTrackId}"));
        uris.AddRange(artist.RecentTracks.Select(t => $"spotify:track:{t.SpotifyTrackId}"));
    }

    return uris;
}
```

### Frontend API Client
```typescript
// Source: Existing extractionApi.ts pattern
const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface PlaylistCreationRequest {
  festivalName: string;
  year?: number;
  trackResults: ArtistTrackResult[];
  spotifyAccessToken: string;
}

export interface PlaylistCreationResponse {
  playlistId: string;
  playlistUrl: string;
  playlistName: string;
  trackCount: number;
}

export async function createPlaylist(
  request: PlaylistCreationRequest
): Promise<PlaylistCreationResponse> {
  const response = await fetch(`${API_URL}/api/playlists/create`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.error || 'Failed to create playlist');
  }

  return response.json();
}
```

### Mobile-Responsive Results Layout
```typescript
// Source: MUI Grid documentation + existing page patterns
// https://mui.com/material-ui/react-grid/

import { Grid, Card, CardContent, Typography, Button } from '@mui/material';

export function PlaylistResultsPage() {
  return (
    <div className="results-page">
      <Grid container spacing={{ xs: 2, md: 3 }}>
        {/* Playlist link - full width */}
        <Grid xs={12}>
          <Card sx={{ bgcolor: '#1DB954', color: 'white' }}>
            <CardContent>
              <Typography variant="h5">Playlist Created!</Typography>
              <Button
                href={playlistUrl}
                target="_blank"
                rel="noopener noreferrer"
                sx={{ mt: 2, bgcolor: 'white', color: '#1DB954' }}
              >
                Open in Spotify
              </Button>
            </CardContent>
          </Card>
        </Grid>

        {/* Included artists - 2/3 width on desktop, full on mobile */}
        <Grid xs={12} md={8}>
          <Card>
            <CardContent>
              <Typography variant="h6">
                Artists Included ({includedCount})
              </Typography>
              {/* Artist list */}
            </CardContent>
          </Card>
        </Grid>

        {/* Skipped artists - 1/3 width on desktop, full on mobile */}
        <Grid xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="h6">
                Skipped ({skippedCount})
              </Typography>
              {/* Skipped list */}
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </div>
  );
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Implicit grant OAuth | PKCE flow | 2020 | Already implemented in Phase 2 - no changes needed |
| playlist-modify scope | Separate public/private scopes | 2021 | Already implemented in Phase 2 - have both scopes |
| Single API endpoint for track operations | Separate Create + AddItems endpoints | Current | Must use two-step process: create empty, then add tracks |
| Manual pagination for >100 tracks | Client-side batching | Current | Must batch in application code, no automatic pagination |

**Deprecated/outdated:**
- None relevant to playlist creation (Spotify API is stable for these operations)

## Open Questions

1. **Playlist description content**
   - What we know: API supports optional description field
   - What's unclear: Whether to include artist count, date, or keep simple
   - Recommendation: Simple description "Created by Conflicted Lineup" to avoid complexity; can enhance in v2

2. **Duplicate track handling across artists**
   - What we know: Phase 4 handles duplicates within single artist
   - What's unclear: If Artist A and Artist B both have same collaboration track, add once or twice?
   - Recommendation: Add all tracks (duplicates across artists okay) - maintains per-artist context and simplifies logic

3. **Festival name when not available**
   - What we know: Image extraction may not always detect festival name
   - What's unclear: Fallback naming strategy for unnamed posters
   - Recommendation: Use "My Festival Playlist" as fallback, allow user to rename in Spotify if needed

## Sources

### Primary (HIGH confidence)
- [Spotify Web API - Create Playlist](https://developer.spotify.com/documentation/web-api/reference/create-playlist) - Official endpoint documentation
- [Spotify Web API - Add Items to Playlist](https://developer.spotify.com/documentation/web-api/reference/add-tracks-to-playlist) - Official endpoint documentation with 100-item limit
- [Spotify Web API - Rate Limits](https://developer.spotify.com/documentation/web-api/concepts/rate-limits) - Official rate limiting guidance with Retry-After header
- [SpotifyAPI.Web - IPlaylistsClient](https://github.com/JohnnyCrazy/SpotifyAPI-NET/blob/master/SpotifyAPI.Web/Clients/Interfaces/IPlaylistsClient.cs) - Library interface for Create and AddItems methods
- [MUI Grid Component](https://mui.com/material-ui/react-grid/) - Official responsive grid documentation
- [MUI Responsive UI Guide](https://mui.com/material-ui/guides/responsive-ui/) - Official mobile responsiveness best practices
- Existing codebase patterns:
  - `SpotifyTrackService.cs` - ExecuteWithRetryAsync pattern for rate limiting
  - `TrackSelectionController.cs` - Controller structure and error handling
  - `TrackSelectionPage.tsx` - Page layout and loading states
  - `extractionApi.ts` - API client patterns

### Secondary (MEDIUM confidence)
- [Spotify URIs and IDs](https://developer.spotify.com/documentation/web-api/concepts/spotify-uris-ids) - URI format for track references
- [Spotify Playlist Concepts](https://developer.spotify.com/documentation/web-api/concepts/playlists) - General playlist behavior and limits

### Tertiary (LOW confidence)
- None - all findings verified with official documentation or existing codebase

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - SpotifyAPI.Web 7.2.1 already in use, MUI already in use
- Architecture: HIGH - Patterns established in Phase 4, direct mapping to playlist operations
- Pitfalls: HIGH - Verified against official Spotify API docs (100-item limit, rate limiting, URL format)

**Research date:** 2026-01-27
**Valid until:** ~30 days (Spotify Web API is stable, minimal breaking changes expected)
