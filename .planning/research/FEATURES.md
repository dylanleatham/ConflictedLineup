# Feature Research

**Domain:** Festival lineup to Spotify playlist converter
**Researched:** 2026-01-24
**Confidence:** HIGH

## Feature Landscape

### Table Stakes (Users Expect These)

Features users assume exist. Missing these = product feels incomplete.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Artist extraction from poster | Core value proposition - users expect automatic extraction | HIGH | Requires OCR/AI for text extraction. Challenge: artistic typography, varied fonts, complex backgrounds. LineupSupply uses Apple Vision Framework for local processing |
| Spotify OAuth integration | Required to create playlists in user's account | MEDIUM | Standard OAuth flow. Must handle token refresh. Spotify requires OAuth for playlist creation |
| Playlist creation | Core output - users expect a clickable playlist | LOW | Spotify API provides straightforward endpoint. Watch for rate limits (30-second rolling window) |
| Artist name matching to Spotify | Users expect accurate artist identification | MEDIUM | Spotify search API handles fuzzy matching reasonably well. Challenge: similar artist names, special characters, international artists |
| Error handling for missing artists | Users expect transparency about what wasn't found | LOW | Simple list of unmatched artists. Critical for user trust when OCR/matching fails |
| Track selection logic | Users expect reasonable tracks, not random deep cuts | MEDIUM | Must balance familiar hits with discovery. Your approach (3 familiar + 3 top + 3 recent) is sound and differentiated |
| Mobile-friendly interface | Most festival poster sharing happens on mobile | MEDIUM | Responsive design essential. Photo upload from camera roll is expected behavior |
| Loading states during processing | Image parsing and API calls take time | LOW | Progress indicators prevent perceived failure. Critical for UX when processing 20+ artists |
| Direct link to created playlist | Users expect immediate access to results | LOW | Spotify playlist URL after creation. Should open in Spotify app on mobile |

### Differentiators (Competitive Advantage)

Features that set the product apart. Not required, but valuable.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Personalized track selection (familiar tracks from user's library) | Your "3 familiar" approach is unique - competitors mostly use top tracks only | MEDIUM | Requires querying user's saved tracks/playlists and cross-referencing with festival artists. High perceived value for personalization |
| Text input for festival name (web search) | Convenience for users who don't have poster image | MEDIUM-HIGH | Requires web search integration or festival database API. LineupSupply doesn't offer this. Adds significant value |
| Recent releases prioritization | "Recent" tracks likely to be played live at festival | LOW | Spotify API allows filtering by release date. Smart UX that shows understanding of festival context |
| Artist list review/editing before playlist creation | Users can deselect artists or correct OCR mistakes | MEDIUM | Gives users control. Critical for fixing OCR errors. spotify-festival-playlist-generator on GitHub offers this |
| Preview of track selection before finalizing | Transparency builds trust in curation algorithm | MEDIUM | Shows exactly what will be added. Reduces "mystery box" anxiety |
| Playlist naming customization | Personalization of playlist title | LOW | Simple but appreciated touch. Default could be "[Festival Name] [Year] - Conflicted Lineup" |
| Track count per artist customization | Power users may want more/fewer tracks per artist | LOW | Your 9-track default is strong (more than competitors), but slider (3-15 tracks) adds flexibility |
| Duplicate prevention across artist selections | Ensures variety when artists collaborate or are featured on each other's tracks | MEDIUM | Complex: must track added track IDs and skip duplicates. Important for large festivals where collaboration is common |
| Playlist cover art from festival poster | Visual continuity from poster to playlist | LOW-MEDIUM | Spotify allows custom playlist images. Would require image upload endpoint (has separate rate limit) |
| Offline festival database for major festivals | Faster processing for known festivals, no web search needed | MEDIUM | Could cache lineups for Coachella, Lollapalooza, etc. Reduces dependency on web search |

### Anti-Features (Commonly Requested, Often Problematic)

Features that seem good but create problems.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Adding ALL tracks from each artist | "More music is better" mindset | Creates unwieldy playlists (50 artists × 50 tracks = 2,500 songs = 166 hours). Users won't listen. Hits Spotify's 10,000 track limit quickly | Stick to 9-15 tracks max per artist. Quality over quantity. Your 9-track approach is optimal |
| Real-time playlist updates as festival lineup changes | Sounds helpful for lineup changes | Complex: requires monitoring festival websites, dealing with dropped artists, handling partial updates. High maintenance for private app | One-time generation. Users can regenerate if lineup changes significantly |
| Social features (sharing, collaborative editing) | "All apps need social" mentality | Massive scope increase. Requires user accounts, auth beyond Spotify, moderation. Not aligned with "for friends" scope | Keep it simple. Users can share Spotify playlist links natively |
| Automatic genre separation into multiple playlists | Seems organized | Genre classification is subjective and error-prone. Creates decision paralysis (which playlist to listen to?). Fragments the festival experience | Single unified playlist maintains festival vibe. Users can use Spotify's filters if needed |
| Historical festival archive (browse past lineups) | Database of festivals sounds valuable | Massive data maintenance burden. Scraping issues, stale data, storage costs. Not core to value prop | Focus on current festivals. Users can save playlists for their own history |
| Integration with other streaming services (Apple Music, Tidal) | Wider audience | Multiplies complexity 3-5x. Each service has different APIs, limits, auth flows. Maintenance nightmare | Spotify only for MVP. It's the dominant platform for this use case |
| AI chat interface for festival recommendations | "AI makes everything better" | Adds complexity and unpredictability. Users want specific output (playlist), not conversation. Token costs for private app | Keep it focused: input (poster/name) → output (playlist) |
| Detailed analytics (listening stats, festival attendance predictions) | Data visualization is popular | Scope creep into different product category. Requires persistent storage, user accounts. Not aligned with core value | Link to Spotify's native analytics. Stay focused on playlist generation |

## Feature Dependencies

```
[Spotify OAuth]
    └──required by──> [Playlist Creation]
    └──required by──> [User Saved Tracks Access] (for "familiar" tracks)
    └──required by──> [Playlist Cover Upload]

[Artist Extraction] (via poster OR text input)
    └──required by──> [Artist Name Matching]
                          └──required by──> [Track Selection]
                                              └──required by──> [Playlist Creation]

[Artist List Review/Editing]
    ├──enhances──> [Artist Name Matching] (user can fix errors)
    └──requires──> [Artist Extraction] (must have initial list)

[Track Preview]
    ├──requires──> [Track Selection]
    └──blocks──> [Playlist Creation] (happens before final creation)

[Personalized Track Selection]
    ├──requires──> [User Saved Tracks Access]
    └──enhances──> [Track Selection]

[Text Input for Festival Name]
    ├──alternative to──> [Poster Upload]
    └──requires──> [Web Search Integration]

[Duplicate Prevention]
    └──enhances──> [Track Selection] (filtering layer)
```

### Dependency Notes

- **Spotify OAuth blocks everything**: No Spotify functionality works without it. Should be first feature implemented.
- **Artist Extraction is the critical path**: Both poster upload and text input are entry points. System should work if EITHER works.
- **Track Selection is the intelligence layer**: This is where your differentiation happens (familiar + top + recent). Worth investing time here.
- **Preview before creation reduces regret**: Users can see what they're getting before committing. Important for trust.
- **Personalization requires saved tracks access**: Extended OAuth scope needed for accessing user's library.

## MVP Definition

### Launch With (v1)

Minimum viable product — what's needed to validate the concept with friends.

- [x] **Poster upload with artist extraction** — Core value prop. Use AI vision API (GPT-4 Vision, Claude Vision, or similar) for OCR.
- [x] **Spotify OAuth** — Required for playlist creation. Standard flow with refresh tokens.
- [x] **Artist list display with manual editing** — Shows extracted artists, allows corrections. Critical for handling OCR errors.
- [x] **Track selection: 3 familiar + 3 top + 3 recent per artist** — Your differentiated approach. Makes MVP competitive.
- [x] **Playlist creation** — Core output. Include error handling for rate limits.
- [x] **Skipped artists list** — Shows what couldn't be matched. Transparency builds trust.
- [x] **Link to created playlist** — Deep link to Spotify.
- [x] **Basic responsive design** — Must work on mobile (primary use case for poster uploads).

**MVP Scope Rationale**: These 8 features deliver the core value loop: upload → extract → curate → create playlist. Everything else is enhancement.

### Add After Validation (v1.x)

Features to add once core is working and friends are using it.

- [ ] **Text input for festival name** — Adds convenience. Implement after poster upload works reliably. Requires web search or festival API integration.
- [ ] **Track count customization** — Power user feature. Easy to add once core selection logic is proven.
- [ ] **Playlist naming customization** — Nice touch. Low effort, high perceived value.
- [ ] **Preview before creation** — Quality of life improvement. Helps users understand selection logic.
- [ ] **Duplicate prevention** — Refines track selection. Important for larger festivals where collaboration is common.
- [ ] **Playlist cover art from poster** — Visual polish. Requires Spotify image upload endpoint (different rate limit).

**Trigger for v1.x features**: After 5-10 friends have successfully created playlists and provided feedback.

### Future Consideration (v2+)

Features to defer until product-market fit is established.

- [ ] **Offline festival database** — Optimization, not core value. Defer until web search is proven pain point.
- [ ] **Advanced personalization** — Genre preferences, mood filtering, etc. Only if users request more control.
- [ ] **Playlist update/regeneration** — Managing existing playlists. Defer until users express need.
- [ ] **Analytics dashboard** — View which artists were most popular, track adds, etc. Different product direction.

**Why defer**: These features add complexity without validating core assumption (do people want festival playlists from posters?).

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| Poster upload + extraction | HIGH | HIGH | P1 (MVP) |
| Spotify OAuth | HIGH | MEDIUM | P1 (MVP) |
| Artist list editing | HIGH | LOW | P1 (MVP) |
| Personalized track selection | HIGH | MEDIUM | P1 (MVP - differentiator) |
| Playlist creation | HIGH | LOW | P1 (MVP) |
| Skipped artists display | MEDIUM | LOW | P1 (MVP - transparency) |
| Link to playlist | HIGH | LOW | P1 (MVP) |
| Responsive design | HIGH | MEDIUM | P1 (MVP) |
| Text input for festival name | HIGH | MEDIUM-HIGH | P2 (post-MVP) |
| Track count customization | MEDIUM | LOW | P2 (post-MVP) |
| Playlist naming | MEDIUM | LOW | P2 (post-MVP) |
| Track preview | MEDIUM | MEDIUM | P2 (post-MVP) |
| Duplicate prevention | MEDIUM | MEDIUM | P2 (post-MVP) |
| Playlist cover art | LOW | MEDIUM | P2 (post-MVP) |
| Festival database | LOW | MEDIUM | P3 (future) |
| Advanced personalization | MEDIUM | HIGH | P3 (future) |
| Playlist updates | LOW | MEDIUM | P3 (future) |
| Analytics | LOW | HIGH | P3 (future) |

**Priority key:**
- P1: Must have for launch — these deliver the core value proposition
- P2: Should have, add when possible — quality of life improvements
- P3: Nice to have, future consideration — scope expansion features

## Competitor Feature Analysis

| Feature | LineupSupply | spotify-festival-playlist-generator (GitHub) | Spotify Gov Ball (Official) | Our Approach |
|---------|--------------|---------------------------------------------|------------------------------|--------------|
| Poster OCR | ✅ Apple Vision Framework (local) | ❌ Manual text input only | ❌ N/A (single festival) | ✅ AI Vision API (GPT-4V/Claude) |
| Text festival name input | ❌ | ✅ Festival name search | ✅ | ✅ (v1.x) |
| Artist selection UI | ❌ Auto-includes all | ✅ Checkbox selection | ❌ Auto-includes all | ✅ Edit extracted list |
| Personalized tracks | ❌ Top tracks only | ❌ Top tracks only | ✅ User listening history + new artists | ✅ 3 familiar + 3 top + 3 recent |
| Track count per artist | Unknown (likely 5-10) | ✅ Customizable | Unknown | ✅ 9 default, customizable (v1.x) |
| Preview before creation | ❌ | ❌ | ❌ | ✅ (v1.x) |
| Skipped artists transparency | Unknown | ❌ | ❌ | ✅ Explicit list |
| Playlist cover art | Unknown | ❌ | ✅ Custom artwork | ✅ (v1.x) |
| Mobile optimized | ✅ Mobile app | ❌ Desktop focus | ✅ In-app experience | ✅ Responsive web |
| Platform | iOS only | Web | Spotify app | Web (cross-platform) |

**Competitive positioning**:
- **vs LineupSupply**: We add text input option, artist editing, and personalized track selection. They have native iOS app (better UX).
- **vs GitHub projects**: We add poster OCR and personalization. They have customization features.
- **vs Spotify Official**: We're multi-festival and user-controlled. They have integration and brand power.

**Our differentiation**: Personalized track selection (familiar + top + recent) + dual input modes (poster + text) + user control (editing).

## Technical Constraints Affecting Features

### Spotify API Limitations
- **Rate limit**: 30-second rolling window. Extended quota required for production. Affects: bulk track addition for large festivals.
- **Playlist limit**: 10,000 tracks per playlist. Affects: maximum festival size (10,000 ÷ 9 tracks = ~1,111 artists).
- **Search accuracy**: Fuzzy matching helps but not perfect. Affects: artist name matching quality.
- **Auth scopes**: Need `playlist-modify-public`, `playlist-modify-private`, `user-library-read` (for familiar tracks). Affects: OAuth implementation.

### OCR/Vision API Challenges
- **Artistic typography**: Festival posters use creative fonts. Affects: extraction accuracy.
- **Background complexity**: Visual noise reduces OCR accuracy. Affects: need for manual editing feature.
- **Text size variation**: Headliners vs. small print. Affects: may need font size filtering.
- **Language support**: International festivals. Affects: OCR language detection needs.

### Web Search for Festival Names
- **Result reliability**: Search results may be outdated or unofficial. Affects: need for result validation/user confirmation.
- **Rate limits**: Search APIs have limits. Affects: caching strategy needed.
- **Parsing structure**: Festival websites have varied structures. Affects: may need multiple strategies.

## User Research Insights

Based on analysis of existing tools and ecosystem patterns:

**User Expectations**:
1. **Speed**: Users expect playlist creation in under 30 seconds after upload. OCR and API calls should be optimized.
2. **Accuracy**: 80%+ artist extraction accuracy is threshold for trust. Below that, feels broken.
3. **Transparency**: Users want to see what's being added. "Black box" generation creates anxiety.
4. **Control**: Users want ability to fix mistakes. OCR isn't perfect, matching isn't perfect.
5. **Mobile-first**: Festival posters are shared on Instagram/social. Upload flow must work on mobile.

**Pain Points with Existing Solutions**:
1. **Generic playlists**: Top tracks only feels impersonal. Users want connection to their taste.
2. **No editing**: When OCR fails, no way to fix. Frustrating dead end.
3. **Platform lock-in**: iOS-only apps exclude Android users.
4. **No festival name search**: Requires having poster image. Inconvenient.

**Opportunities**:
1. **Personalization**: Your familiar tracks approach addresses "generic playlist" pain.
2. **Flexibility**: Dual input (poster + text) covers more use cases than competitors.
3. **Transparency**: Showing skipped artists and allowing preview builds trust.

## Sources

**Competitor Analysis**:
- [LineupSupply app coverage - TechCrunch](https://techcrunch.com/2022/09/06/lineupsupplys-app-turns-music-festival-posters-into-spotify-playlists/)
- [spotify-festival-playlist-generator - GitHub](https://github.com/AustinLowey/spotify-festival-playlist-generator)
- [Spotify Gov Ball 2026 Experience - Spotify Newsroom](https://newsroom.spotify.com/2026-01-06/gov-ball-lineup-experience/)

**Festival Discovery Apps**:
- [FestGPS.app - Music festival matchmaker](https://edmhousenetwork.com/discover-your-perfect-music-festival-with-festgps-app-the-ultimate-festival-matchmaker/)
- [Festiverse - Festival lineup app](https://apps.apple.com/us/app/festiverse/id6744997695)
- [FEST App - Artist and lineup tracking](https://festapp.io/)

**Music Discovery & Personalization**:
- [CORRD - Personalized music discovery](https://corrd.fm/)
- [Last.fm - Listening history recommendations](https://www.last.fm/)
- [The Echo Nest - Taste Profile technology](https://the.echonest.com/solutions/musicdiscovery/)

**Playlist Curation Best Practices**:
- [Spotify playlist curation guide - Free Your Music](https://freeyourmusic.com/blog/playlist-curation-music-discovery)
- [Playlist automation with Zapier](https://zapier.com/blog/perfect-spotify-playlist/)
- [Why curated playlists outperform algorithmic - Klangspot](https://klangspot.com/why-curated-playlists-might-be-better-than-algorithmic-ones/)

**Technical Constraints**:
- [Spotify API Rate Limits - Official Docs](https://developer.spotify.com/documentation/web-api/concepts/rate-limits)
- [Spotify playlist limit (10,000 tracks) - NoteBurner](https://www.noteburner.com/spotify-music-tips/spotify-playlist-limit.html)
- [OCR challenges with artistic text - research.aimultiple.com](https://research.aimultiple.com/ocr-technology/)

---
*Feature research for: Festival lineup to Spotify playlist converter*
*Researched: 2026-01-24*
*Confidence: HIGH - Based on competitor analysis, Spotify API documentation, and music discovery ecosystem patterns*
