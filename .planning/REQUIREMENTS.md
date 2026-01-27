# Requirements: Conflicted Lineup

**Defined:** 2026-01-24
**Core Value:** Users can instantly turn any festival lineup into a personalized discovery playlist without manual artist-by-artist searching.

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Input Methods

- [x] **INPUT-01**: User can upload a festival poster image
- [x] **INPUT-02**: System extracts artist names from poster using Claude Vision API
- [x] **INPUT-03**: User can type a festival name to look up lineup
- [x] **INPUT-04**: System looks up festival lineup using Claude web search
- [x] **INPUT-05**: User can edit/remove extracted artist names before playlist creation
- [x] **INPUT-06**: System reads AI prompt from file in codebase (not hardcoded)

### Authentication

- [x] **AUTH-01**: User must authenticate with Spotify to use the app
- [x] **AUTH-02**: System uses OAuth PKCE flow (not implicit grant)
- [x] **AUTH-03**: User session persists across browser refresh
- [x] **AUTH-04**: User can log out

### Track Selection

- [x] **TRACK-01**: System fetches 3 top tracks per artist from Spotify
- [x] **TRACK-02**: System fetches 3 recent releases per artist from Spotify
- [x] **TRACK-03**: System fetches 3 familiar tracks per artist from user's existing playlists
- [x] **TRACK-04**: System prevents duplicate tracks across categories
- [x] **TRACK-05**: System skips artists with no Spotify match

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

- [x] **INFRA-01**: Backend deployed to Azure App Service
- [x] **INFRA-02**: Frontend deployed via Azure (App Service or Static Web App)
- [x] **INFRA-03**: Secrets stored in Azure Key Vault
- [x] **INFRA-04**: CI/CD via GitHub Actions
- [x] **INFRA-05**: End-to-end skeleton deployed before feature implementation

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
| INPUT-01 | Phase 3 | Complete |
| INPUT-02 | Phase 3 | Complete |
| INPUT-03 | Phase 3 | Complete |
| INPUT-04 | Phase 3 | Complete |
| INPUT-05 | Phase 3 | Complete |
| INPUT-06 | Phase 3 | Complete |
| AUTH-01 | Phase 2 | Complete |
| AUTH-02 | Phase 2 | Complete |
| AUTH-03 | Phase 2 | Complete |
| AUTH-04 | Phase 2 | Complete |
| TRACK-01 | Phase 4 | Complete |
| TRACK-02 | Phase 4 | Complete |
| TRACK-03 | Phase 4 | Complete |
| TRACK-04 | Phase 4 | Complete |
| TRACK-05 | Phase 4 | Complete |
| PLAYLIST-01 | Phase 5 | Pending |
| PLAYLIST-02 | Phase 5 | Pending |
| PLAYLIST-03 | Phase 5 | Pending |
| RESULTS-01 | Phase 5 | Pending |
| RESULTS-02 | Phase 5 | Pending |
| RESULTS-03 | Phase 5 | Pending |
| RESULTS-04 | Phase 5 | Pending |
| INFRA-01 | Phase 1 | Complete |
| INFRA-02 | Phase 1 | Complete |
| INFRA-03 | Phase 1 | Complete |
| INFRA-04 | Phase 1 | Complete |
| INFRA-05 | Phase 1 | Complete |

**Coverage:**
- v1 requirements: 27 total
- Mapped to phases: 27
- Unmapped: 0

---
*Requirements defined: 2026-01-24*
*Last updated: 2026-01-27 after Phase 4 completion*
