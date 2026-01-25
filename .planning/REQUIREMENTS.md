# Requirements: Conflicted Lineup

**Defined:** 2026-01-24
**Core Value:** Users can instantly turn any festival lineup into a personalized discovery playlist without manual artist-by-artist searching.

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Input Methods

- [ ] **INPUT-01**: User can upload a festival poster image
- [ ] **INPUT-02**: System extracts artist names from poster using Claude Vision API
- [ ] **INPUT-03**: User can type a festival name to look up lineup
- [ ] **INPUT-04**: System looks up festival lineup using Claude web search
- [ ] **INPUT-05**: User can edit/remove extracted artist names before playlist creation
- [ ] **INPUT-06**: System reads AI prompt from file in codebase (not hardcoded)

### Authentication

- [ ] **AUTH-01**: User must authenticate with Spotify to use the app
- [ ] **AUTH-02**: System uses OAuth PKCE flow (not implicit grant)
- [ ] **AUTH-03**: User session persists across browser refresh
- [ ] **AUTH-04**: User can log out

### Track Selection

- [ ] **TRACK-01**: System fetches 3 top tracks per artist from Spotify
- [ ] **TRACK-02**: System fetches 3 recent releases per artist from Spotify
- [ ] **TRACK-03**: System fetches 3 familiar tracks per artist from user's existing playlists
- [ ] **TRACK-04**: System prevents duplicate tracks across categories
- [ ] **TRACK-05**: System skips artists with no Spotify match

### Playlist Creation

- [ ] **PLAYLIST-01**: System creates playlist on user's Spotify account
- [ ] **PLAYLIST-02**: Playlist is named after the festival/event
- [ ] **PLAYLIST-03**: System adds all selected tracks to playlist

### Results Display

- [ ] **RESULTS-01**: User sees list of artists included in playlist
- [ ] **RESULTS-02**: User sees list of artists that were skipped (no Spotify match)
- [ ] **RESULTS-03**: User receives link to created playlist
- [ ] **RESULTS-04**: UI is mobile-responsive

### Infrastructure

- [ ] **INFRA-01**: Backend deployed to Azure App Service
- [ ] **INFRA-02**: Frontend deployed via Azure (App Service or Static Web App)
- [ ] **INFRA-03**: Secrets stored in Azure Key Vault
- [ ] **INFRA-04**: CI/CD via GitHub Actions
- [ ] **INFRA-05**: End-to-end skeleton deployed before feature implementation

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Enhanced UX

- **UX-01**: User can preview tracks before creating playlist
- **UX-02**: User can customize track count per category (1-5 per type)
- **UX-03**: User can reorder artists before playlist creation

### History & Persistence

- **HIST-01**: User can view previously created playlists
- **HIST-02**: User can regenerate playlist for a festival

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Public launch infrastructure | Friends-only audience, no scale concerns |
| User accounts beyond Spotify | Spotify OAuth sufficient |
| Social features (sharing, friends) | Scope creep, not core value |
| Multi-platform (Apple Music, etc.) | Complexity multiplier, Spotify focus |
| Real-time lineup updates | Maintenance nightmare, not needed |
| Mobile app | Web-first, responsive is fine |
| Track count customization | Defer to v2, 3+3+3 is MVP |
| Track preview | Defer to v2, adds complexity |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| INPUT-01 | TBD | Pending |
| INPUT-02 | TBD | Pending |
| INPUT-03 | TBD | Pending |
| INPUT-04 | TBD | Pending |
| INPUT-05 | TBD | Pending |
| INPUT-06 | TBD | Pending |
| AUTH-01 | TBD | Pending |
| AUTH-02 | TBD | Pending |
| AUTH-03 | TBD | Pending |
| AUTH-04 | TBD | Pending |
| TRACK-01 | TBD | Pending |
| TRACK-02 | TBD | Pending |
| TRACK-03 | TBD | Pending |
| TRACK-04 | TBD | Pending |
| TRACK-05 | TBD | Pending |
| PLAYLIST-01 | TBD | Pending |
| PLAYLIST-02 | TBD | Pending |
| PLAYLIST-03 | TBD | Pending |
| RESULTS-01 | TBD | Pending |
| RESULTS-02 | TBD | Pending |
| RESULTS-03 | TBD | Pending |
| RESULTS-04 | TBD | Pending |
| INFRA-01 | TBD | Pending |
| INFRA-02 | TBD | Pending |
| INFRA-03 | TBD | Pending |
| INFRA-04 | TBD | Pending |
| INFRA-05 | TBD | Pending |

**Coverage:**
- v1 requirements: 27 total
- Mapped to phases: 0
- Unmapped: 27 (pending roadmap creation)

---
*Requirements defined: 2026-01-24*
*Last updated: 2026-01-24 after initial definition*
