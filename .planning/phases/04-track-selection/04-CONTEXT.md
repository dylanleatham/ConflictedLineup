# Phase 4: Track Selection - Context

**Gathered:** 2026-01-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Select personalized tracks for each artist using a 3+3+3 formula: 3 familiar tracks (from user's library), 3 top tracks (popularity-based), and 3 recent releases (likely played live). Deduplicate across categories. Track which artists couldn't be matched.

</domain>

<decisions>
## Implementation Decisions

### Artist Matching
- Use first Spotify search result (trust their ranking algorithm)
- Skip artists not found on Spotify silently, show in final "skipped" list
- No caching — fresh lookup every time
- Claude's discretion: Verification approach for common names (genre/popularity check if needed)

### Familiar Tracks Logic
- Sources: Saved tracks + user-created playlists only (not followed playlists)
- If fewer than 3 familiar tracks found, use fewer (don't backfill)
- Set a limit on playlists/tracks scanned for large libraries (Claude picks reasonable limit)

### Recent Releases Criteria
- "Recent" means from the artist's most recent album/EP/single (last album cycle, not time-based)
- Prioritize singles over album deep cuts — singles are more likely played live
- All release types count: albums, EPs, and singles
- If no recent releases, backfill with additional top tracks

### Processing Feedback
- Artist-by-artist progress updates (not just overall progress bar)
- Show track results as they come in for each artist
- Non-interactive during processing — user waits for completion
- Keep partial results on failure — show what completed, let user proceed or retry

### Claude's Discretion
- Exact API pagination limits for scanning user library
- Verification heuristics for ambiguous artist names
- Specific limit numbers for large library scanning

</decisions>

<specifics>
## Specific Ideas

- 9 tracks per artist (3+3+3) is the target, but fewer is acceptable when categories can't be filled
- Singles are better indicators of live setlists than album tracks
- User's own playlists are a better signal of familiarity than followed playlists

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 04-track-selection*
*Context gathered: 2026-01-26*
