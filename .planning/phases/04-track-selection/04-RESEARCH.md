# Phase 4: Track Selection - Research

**Researched:** 2026-01-26
**Domain:** Spotify Web API integration for track selection and user library access
**Confidence:** HIGH

## Summary

Phase 4 implements personalized track selection for each artist using Spotify Web API's artist search, top tracks, recent releases (albums), and user library endpoints. The research focused on understanding Spotify's API capabilities, rate limiting, pagination patterns, and available .NET libraries for backend integration.

The standard approach is to use **SpotifyAPI-NET** (SpotifyAPI.Web NuGet package v7.2.1+) on the backend for type-safe API access with built-in pagination support. The library provides async/await patterns, automatic token refresh, and comprehensive endpoint coverage. Frontend will send artist names to backend, which orchestrates multiple Spotify API calls per artist (search, top tracks, albums, user library scanning) and returns deduplicated track sets.

Key challenges include handling rate limits (429 responses with Retry-After headers), efficiently scanning large user libraries (50-item pagination limits), and implementing track deduplication logic. Spotify track IDs are the canonical identifiers for deduplication, as the same song can appear multiple times with different track IDs (single version vs album version vs remaster).

**Primary recommendation:** Use SpotifyAPI-NET with async pagination methods (`Paginate` over `PaginateAll`), implement exponential backoff for rate limiting, and limit user library scanning to first 500 saved tracks + first 50 playlists to prevent timeouts.

## Standard Stack

The established libraries/tools for Spotify Web API integration in .NET:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| SpotifyAPI.Web | 7.2.1+ | .NET Spotify Web API client | Official recommendation from Spotify community, 74+ typed endpoints, .NET Standard 2.X support, active maintenance |
| System.Net.Http | Built-in | HTTP client for API calls | Standard .NET HTTP client, used by SpotifyAPI.Web internally |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| SpotifyAPI.Web.Auth | 7.2.1+ | OAuth2 authentication helper | When implementing server-side OAuth flows (not needed for this phase - token from frontend) |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| SpotifyAPI.Web | Direct HttpClient calls to Spotify API | More control but lose type safety, pagination helpers, automatic token handling. Not recommended. |
| SpotifyAPI.Web | SpotifyApi.NetCore | Lightweight but less comprehensive endpoint coverage, less active maintenance |

**Installation:**
```bash
dotnet add package SpotifyAPI.Web
```

## Architecture Patterns

### Recommended Project Structure
```
ConflictedLineup.Api/
├── Controllers/
│   └── TrackSelectionController.cs     # Endpoint: POST /api/tracks/select
├── Services/
│   ├── SpotifyTrackService.cs          # Core track selection logic
│   ├── SpotifySearchService.cs         # Artist search + matching
│   └── SpotifyUserLibraryService.cs    # Familiar tracks from user library
├── Models/
│   ├── TrackSelectionRequest.cs        # Input: artist names, user token
│   ├── TrackSelectionResponse.cs       # Output: tracks per artist + skipped
│   └── SpotifyModels.cs                # Track, Artist DTOs
```

### Pattern 1: Service Layer with SpotifyClient Injection
**What:** Create service classes that accept SpotifyClient instance configured with user's access token
**When to use:** Every API operation requiring user context (this entire phase)
**Example:**
```csharp
// Source: SpotifyAPI-NET documentation - https://johnnycrazy.github.io/SpotifyAPI-NET/docs/getting_started/
public class SpotifyTrackService
{
    private readonly ISpotifyClient _spotify;

    public SpotifyTrackService(string accessToken)
    {
        _spotify = new SpotifyClient(accessToken);
    }

    public async Task<List<SimpleTrack>> GetTopTracksAsync(string artistId)
    {
        var request = new ArtistsTopTracksRequest("US"); // Market required
        var response = await _spotify.Artists.GetTopTracks(artistId, request);
        return response.Tracks.Take(3).ToList();
    }
}
```

### Pattern 2: Async Pagination with Paginate
**What:** Use SpotifyAPI-NET's `Paginate` method for memory-efficient streaming of paginated results
**When to use:** Fetching user playlists, saved tracks, album tracks - any endpoint returning Paging<T>
**Example:**
```csharp
// Source: SpotifyAPI-NET documentation - https://johnnycrazy.github.io/SpotifyAPI-NET/docs/pagination/
public async Task<List<SavedTrack>> GetSavedTracksAsync(int limit = 500)
{
    var savedTracks = new List<SavedTrack>();
    var firstPage = await _spotify.Library.GetTracks(new LibraryTracksRequest { Limit = 50 });

    await foreach (var track in _spotify.Paginate(firstPage))
    {
        savedTracks.Add(track);
        if (savedTracks.Count >= limit) break; // Prevent excessive scanning
    }

    return savedTracks;
}
```

### Pattern 3: Artist-by-Artist Processing with Progress Updates
**What:** Process artists sequentially, sending progress updates after each artist completes
**When to use:** This phase's main workflow to provide real-time feedback
**Example:**
```csharp
// Source: React async best practices - https://blog.pixelfreestudio.com/best-practices-for-handling-async-state-in-frontend-apps/
public async Task<TrackSelectionResponse> SelectTracksAsync(
    List<string> artistNames,
    string userToken,
    IProgress<ArtistProgress> progress)
{
    var results = new Dictionary<string, ArtistTracks>();
    var skipped = new List<string>();

    for (int i = 0; i < artistNames.Count; i++)
    {
        var artistName = artistNames[i];
        progress?.Report(new ArtistProgress
        {
            Current = i + 1,
            Total = artistNames.Count,
            ArtistName = artistName
        });

        try
        {
            var tracks = await ProcessArtistAsync(artistName, userToken);
            results[artistName] = tracks;
        }
        catch (ArtistNotFoundException)
        {
            skipped.Add(artistName);
        }
    }

    return new TrackSelectionResponse { Results = results, Skipped = skipped };
}
```

### Pattern 4: Rate Limit Handling with Exponential Backoff
**What:** Catch 429 responses, read Retry-After header, wait specified time before retry
**When to use:** All Spotify API calls (mandatory for production reliability)
**Example:**
```csharp
// Source: Spotify rate limits documentation - https://developer.spotify.com/documentation/web-api/concepts/rate-limits
public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> apiCall, int maxRetries = 3)
{
    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
        {
            return await apiCall();
        }
        catch (APIException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            if (attempt == maxRetries - 1) throw;

            // Read Retry-After header (in seconds)
            var retryAfter = int.Parse(ex.Response.Headers["Retry-After"] ?? "5");
            await Task.Delay(TimeSpan.FromSeconds(retryAfter));
        }
    }

    throw new Exception("Max retries exceeded");
}
```

### Anti-Patterns to Avoid
- **PaginateAll for large datasets:** SpotifyAPI-NET's `PaginateAll()` loads entire result set into memory. For Search endpoint or users with 1000+ playlists, this causes memory issues. Use `Paginate()` with break conditions instead.
- **Sequential awaits without progress:** Don't await all artists in parallel without chunking - rate limits will kill you. Process sequentially or in small batches (3-5) with progress updates.
- **Ignoring market parameter:** Many endpoints require or benefit from market parameter (e.g., "US"). Without it, content may be unavailable or API returns fewer results.
- **Comparing track names for deduplication:** Track names are not unique (remasters, live versions, different albums). Always use Spotify Track IDs for deduplication.

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Spotify API pagination | Custom offset/limit loop with HttpClient | SpotifyAPI-NET's `Paginate()` or `PaginateAll()` | Library handles next/previous URLs, type conversion, error handling. Pagination logic has edge cases (total count inaccuracies, null next links). |
| Rate limit retry logic | Simple try-catch with fixed delay | SpotifyAPI-NET's built-in retry handlers or custom exponential backoff | Retry-After header varies per request, fixed delays waste time or trigger more 429s. Exponential backoff with jitter is proven pattern. |
| OAuth token refresh | Manual refresh logic with timers | SpotifyAPI-NET's automatic token refresh (when using SpotifyAPI.Web.Auth) | Token expiry handling has edge cases (clock skew, pre-emptive refresh). Library handles it correctly. |
| Track deduplication | String comparison on track names | HashSet<string> with Spotify Track IDs | Same song can have different names (feat. artist variations, remaster suffixes). Track ID is canonical identifier. |
| Artist search result ranking | Custom scoring algorithm | Trust Spotify's first search result | Spotify's search ranking considers name matching, popularity, genre context. Their algorithm is battle-tested. User decision from CONTEXT.md. |

**Key insight:** Spotify Web API has many subtle behaviors (market-based content filtering, track relinking across regions, pagination inconsistencies, rate limit variability). Using a mature library like SpotifyAPI-NET saves weeks of debugging edge cases that others have already solved.

## Common Pitfalls

### Pitfall 1: Rate Limiting Without Retry-After
**What goes wrong:** Application hits 429 Too Many Requests and either crashes or waits fixed time (e.g., 5 seconds), causing slow performance or continued rate limiting.
**Why it happens:** Spotify's rate limit is calculated on rolling 30-second window with no published absolute numbers. Each 429 response includes `Retry-After` header with specific wait time, but developers often ignore it.
**How to avoid:** Always read `Retry-After` header from 429 responses and wait exactly that long. Implement exponential backoff for safety. SpotifyAPI-NET provides `SimpleRetryHandler` that handles 429/500/502/503 automatically.
**Warning signs:** Getting repeated 429 errors despite throttling, inconsistent performance across users, 24-hour rate limit bans (indicates persistent violations).

### Pitfall 2: Memory Exhaustion from PaginateAll
**What goes wrong:** Application crashes or becomes unresponsive when fetching playlists or saved tracks for users with large libraries (500+ playlists, 5000+ saved tracks).
**Why it happens:** `PaginateAll()` loads entire result set into memory before returning. A user with 1000 playlists × 100 tracks each = 100K items in memory.
**How to avoid:** Use `Paginate()` (IAsyncEnumerable) with explicit limits. CONTEXT.md specifies "reasonable limit" for scanning - recommend 500 saved tracks + 50 playlists maximum. Use `break` to exit loop early.
**Warning signs:** High memory usage during track selection, timeouts for users with large libraries, backend becoming unresponsive.

### Pitfall 3: Artist Name Ambiguity
**What goes wrong:** Searching "Jordan" returns "Jordan Smith" (Christian artist) instead of "Jordan" (DJ), leading to wrong tracks in playlist.
**Why it happens:** Spotify search ranks by popularity + relevance. Common names have multiple matches. First result might not be the artist playing at festival.
**How to avoid:** CONTEXT.md gives Claude discretion on verification. Recommend: For ambiguous names (< 6 characters or common words), verify with genre/popularity check. If artist has < 10K followers or genre mismatch (expected: electronic, got: country), flag for manual review.
**Warning signs:** User reports wrong artist, track genres don't match festival style (pop tracks for techno festival), unusually low follower counts for festival headliners.

### Pitfall 4: Track Deduplication by Name
**What goes wrong:** Playlist contains multiple versions of same song because deduplication used track names, missing that "Song Title" and "Song Title - Remastered" are the same recording.
**Why it happens:** Same song appears in multiple albums (single release, album release, greatest hits, remasters). Track names vary slightly but Spotify assigns different Track IDs.
**How to avoid:** Use `HashSet<string>` with Spotify Track IDs (`track.Id`) for deduplication across familiar/top/recent categories. Track ID is canonical identifier. Consider ISRC for deeper deduplication but not available in all endpoints.
**Warning signs:** Users see "duplicate" songs in playlist, multiple versions of same track (clean/explicit, remaster/original), inflated track counts per artist.

### Pitfall 5: Missing Market Parameter
**What goes wrong:** API returns empty results or fewer tracks than expected, especially for international artists.
**Why it happens:** Many Spotify endpoints filter content by market (country). Without market parameter, content is considered unavailable. User's account country should take precedence but isn't always reliable.
**How to avoid:** Always pass market parameter to endpoints that accept it. Use "US" as default or detect user's country from profile. For `GetTopTracks`, market is required parameter.
**Warning signs:** Empty track lists for valid artists, inconsistent results between users in different countries, "unavailable in your region" type errors.

### Pitfall 6: OAuth Scope Insufficiency
**What goes wrong:** API returns 403 Forbidden when fetching user's saved tracks or playlists despite valid access token.
**Why it happens:** Different endpoints require different OAuth scopes. `user-library-read` for saved tracks, `playlist-read-private` for private playlists, `playlist-read-collaborative` for collaborative playlists. Frontend must request all needed scopes upfront.
**How to avoid:** Frontend OAuth configuration must include: `user-library-read`, `playlist-read-private`, `playlist-read-collaborative`. Backend should return clear error message indicating missing scope rather than generic 403.
**Warning signs:** 403 errors on specific endpoints, works for some users but not others (depends on their library privacy settings), empty playlist results despite user having playlists.

## Code Examples

Verified patterns from official sources:

### Artist Search (First Result)
```csharp
// Source: Spotify Web API Search documentation - https://developer.spotify.com/documentation/web-api/reference/search
public async Task<string?> SearchArtistAsync(string artistName)
{
    var searchRequest = new SearchRequest(SearchRequest.Types.Artist, artistName);
    var searchResponse = await _spotify.Search.Item(searchRequest);

    if (searchResponse.Artists.Items?.Count > 0)
    {
        var artist = searchResponse.Artists.Items[0]; // First result per CONTEXT.md
        return artist.Id;
    }

    return null; // Artist not found
}
```

### Get Artist's Recent Album
```csharp
// Source: Spotify Web API Get Artist Albums - https://developer.spotify.com/documentation/web-api/reference/get-an-artists-albums
public async Task<SimpleAlbum?> GetMostRecentAlbumAsync(string artistId)
{
    var albumsRequest = new ArtistsAlbumsRequest
    {
        IncludeGroupsParam = ArtistsAlbumsRequest.IncludeGroups.Album |
                            ArtistsAlbumsRequest.IncludeGroups.Single,
        Market = "US",
        Limit = 50 // Get enough to find recent
    };

    var albums = await _spotify.Artists.GetAlbums(artistId, albumsRequest);

    // Albums sorted by release date (newest first) by default
    return albums.Items?.FirstOrDefault();
}
```

### Get Familiar Tracks from User Library
```csharp
// Source: Spotify Web API Get Saved Tracks - https://developer.spotify.com/documentation/web-api/reference/get-users-saved-tracks
public async Task<List<string>> GetFamiliarTracksForArtistAsync(
    string artistId,
    int maxTracksToScan = 500)
{
    var familiarTrackIds = new List<string>();
    var scannedCount = 0;

    var firstPage = await _spotify.Library.GetTracks(new LibraryTracksRequest { Limit = 50 });

    await foreach (var savedTrack in _spotify.Paginate(firstPage))
    {
        scannedCount++;

        // Check if any artist on track matches target artist
        if (savedTrack.Track.Artists.Any(a => a.Id == artistId))
        {
            familiarTrackIds.Add(savedTrack.Track.Id);
            if (familiarTrackIds.Count >= 3) break; // Only need 3 per CONTEXT.md
        }

        if (scannedCount >= maxTracksToScan) break; // Prevent excessive scanning
    }

    return familiarTrackIds;
}
```

### Track Deduplication Across Categories
```csharp
// Source: Spotify URIs and IDs - https://developer.spotify.com/documentation/web-api/concepts/spotify-uris-ids
public class ArtistTracks
{
    public List<string> FamiliarTrackIds { get; set; } = new();
    public List<string> TopTrackIds { get; set; } = new();
    public List<string> RecentTrackIds { get; set; } = new();

    public List<string> GetDeduplicatedTrackIds()
    {
        var seen = new HashSet<string>();
        var result = new List<string>();

        // Prioritize familiar > top > recent
        foreach (var trackId in FamiliarTrackIds.Concat(TopTrackIds).Concat(RecentTrackIds))
        {
            if (seen.Add(trackId)) // Returns false if already exists
            {
                result.Add(trackId);
            }
        }

        return result;
    }
}
```

### Progress Reporting Pattern
```csharp
// Source: .NET IProgress<T> documentation
public interface IArtistProgress
{
    void ReportProgress(string artistName, int current, int total, List<string> trackIds);
}

public async Task ProcessArtistsWithProgressAsync(
    List<string> artistNames,
    string userToken,
    IArtistProgress progressReporter)
{
    for (int i = 0; i < artistNames.Count; i++)
    {
        var artistName = artistNames[i];
        var trackIds = await SelectTracksForArtistAsync(artistName, userToken);

        progressReporter.ReportProgress(
            artistName,
            current: i + 1,
            total: artistNames.Count,
            trackIds
        );
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| SpotifyAPI-NET v5.x synchronous methods | SpotifyAPI-NET v7.x async/await throughout | v6.0 (2020) | All API methods now async. Must update old examples using `.Result` or `.Wait()` |
| Manual OAuth flow with WebBrowser control | Authorization Code with PKCE flow | 2021 (Spotify security update) | PKCE required for public clients. Frontend using react-oauth2-code-pkce already compliant |
| GetUserPlaylists() returns everything | Returns owned + followed, not collaborative by default | Ongoing API behavior | Must request `playlist-read-collaborative` scope explicitly |
| Album/Single/EP distinction by track count | Also considers total duration | 2024 refinement | Releases under 30min with ≤6 tracks = EP/Single. Affects "recent releases" logic |
| Fixed rate limits (documented numbers) | Dynamic rolling window (30-second) | 2025 API update | No published rate limits. Must handle 429 responses gracefully |

**Deprecated/outdated:**
- **Development Mode API quotas:** As of May 2025, Spotify requires 250K monthly active users for Extended Quota Mode. Development mode has lower, undocumented limits. Apps in development may hit limits faster than expected.
- **SpotifyAPI.Web.Auth for client-side:** Library's embedded OAuth server designed for desktop apps. For web apps, frontend should handle OAuth flow (already done with react-oauth2-code-pkce).

## Open Questions

Things that couldn't be fully resolved:

1. **How to determine if album track is a "single" for live performance prioritization**
   - What we know: Spotify API provides album type (album/single/compilation) and track count, but doesn't flag individual tracks as "singles" within albums. Can identify standalone single releases via `include_groups=single`.
   - What's unclear: Within an album, how to identify which tracks were promoted as singles (radio play, likely live performance). Track `popularity` field might indicate this but isn't documented as reliable indicator.
   - Recommendation: For "recent releases," fetch artist's most recent singles (`include_groups=single`) first, then fall back to tracks from most recent album. Prioritize tracks with higher `popularity` score within album. This aligns with CONTEXT.md: "prioritize singles over album deep cuts."

2. **Optimal pagination limits for large user libraries**
   - What we know: Spotify API limits pagination to 50 items per request. Users can have 10,000+ saved tracks and 1000+ playlists. Scanning everything would take 200+ API calls (rate limit risk) and 30+ seconds (poor UX).
   - What's unclear: What's the right balance between thorough scanning (find more familiar tracks) and performance (fast results)?
   - Recommendation: CONTEXT.md says "Claude picks reasonable limit." Recommend: Scan first 500 saved tracks (10 API calls) + first 50 playlists (1 API call for list, then fetch tracks for playlists with matching artist names only). Add timeout of 10 seconds max for familiar track scanning per artist. If fewer than 3 found, that's acceptable per CONTEXT.md.

3. **Artist name ambiguity verification threshold**
   - What we know: CONTEXT.md gives Claude discretion on "verification approach for common names (genre/popularity check if needed)."
   - What's unclear: What constitutes "common name" that needs verification? What are appropriate thresholds for genre/popularity checks?
   - Recommendation: Implement simple heuristic: If artist name is ≤5 characters OR matches common word dictionary (top 1000 English words), perform verification. Verification: Check if `followers.total < 10000` (unusually low for festival artist) or `genres` array empty (suspicious). Flag these for manual review in response rather than auto-skipping. Let user confirm/override.

## Sources

### Primary (HIGH confidence)
- [Spotify Web API Official Documentation](https://developer.spotify.com/documentation/web-api) - Complete API reference, current as of 2026
- [Spotify Rate Limits Documentation](https://developer.spotify.com/documentation/web-api/concepts/rate-limits) - Rate limiting behavior, Retry-After header usage
- [Get Artist's Top Tracks Endpoint](https://developer.spotify.com/documentation/web-api/reference/get-an-artists-top-tracks) - Endpoint specification with parameters
- [Get User's Saved Tracks Endpoint](https://developer.spotify.com/documentation/web-api/reference/get-users-saved-tracks) - Library access with pagination
- [Spotify Search Endpoint](https://developer.spotify.com/documentation/web-api/reference/search) - Artist search parameters and response structure
- [Get Artist's Albums Endpoint](https://developer.spotify.com/documentation/web-api/reference/get-an-artists-albums) - Album filtering with include_groups parameter
- [Spotify URIs and IDs Documentation](https://developer.spotify.com/documentation/web-api/concepts/spotify-uris-ids) - Track ID vs URI explanation
- [SpotifyAPI-NET GitHub Repository](https://github.com/JohnnyCrazy/SpotifyAPI-NET) - Official .NET library, 7.2.1+ stable
- [SpotifyAPI-NET Documentation](https://johnnycrazy.github.io/SpotifyAPI-NET/docs/getting_started/) - Installation and usage patterns
- [SpotifyAPI-NET Pagination Guide](https://johnnycrazy.github.io/SpotifyAPI-NET/docs/pagination/) - Paginate vs PaginateAll usage

### Secondary (MEDIUM confidence)
- [React Async State Best Practices](https://blog.pixelfreestudio.com/best-practices-for-handling-async-state-in-frontend-apps/) - Progress tracking patterns (2026)
- [API Pagination Best Practices](https://www.merge.dev/blog/api-pagination-best-practices) - General pagination guidance (2026)
- [Spotify API Rate Limiting Challenges](https://apipark.com/technews/O4zBQwTk.html) - Community insights on rate limit behavior
- [Albums vs Singles Definition](https://dittomusic.com/en/blog/albums-vs-eps-vs-singles-a-guide-to-releasing-music) - Industry standards for release types (2026)

### Tertiary (LOW confidence)
- Community discussions on Spotify deduplication tools - Third-party solutions indicate API doesn't provide native deduplication
- Spotify Community posts on rate limiting - Anecdotal evidence of 429 behavior, not authoritative

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - SpotifyAPI-NET is widely used, well-documented, officially recognized by Spotify community
- Architecture: HIGH - Patterns verified against official SpotifyAPI-NET documentation and .NET best practices
- Pitfalls: HIGH - Rate limiting, pagination, scope requirements verified with official Spotify documentation
- Recent releases logic: MEDIUM - Album type filtering documented, but "singles vs deep cuts" requires heuristic (popularity score)
- Library scanning limits: MEDIUM - No official guidance on "reasonable limits," recommendation based on rate limit math and UX expectations

**Research date:** 2026-01-26
**Valid until:** 2026-02-23 (30 days for stable APIs, though Spotify occasionally announces changes)
