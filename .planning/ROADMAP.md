# Roadmap: Conflicted Lineup

## Overview

This roadmap delivers a festival-to-playlist web app in 5 phases, starting with deployed skeleton infrastructure to validate Azure deployment early, then building authentication, AI-powered artist extraction, personalized track selection, and playlist creation incrementally. Each phase delivers verifiable user-facing capabilities that build on previous work.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Skeleton Deployment** - End-to-end infrastructure deployed before features
- [x] **Phase 2: Spotify Authentication** - OAuth working in production environment
- [ ] **Phase 3: Artist Extraction** - AI-powered poster parsing and festival lookup
- [ ] **Phase 4: Track Selection** - Personalized curation with familiar + top + recent tracks
- [ ] **Phase 5: Playlist Creation & Results** - Complete workflow from input to Spotify playlist

## Phase Details

### Phase 1: Skeleton Deployment
**Goal**: Empty React and .NET applications deployed to Azure with secrets management working
**Depends on**: Nothing (first phase)
**Requirements**: INFRA-01, INFRA-02, INFRA-03, INFRA-04, INFRA-05
**Success Criteria** (what must be TRUE):
  1. Frontend displays "Hello World" page accessible via HTTPS custom domain
  2. Backend returns 200 OK from health check endpoint at /api/health
  3. GitHub Actions successfully deploys both frontend and backend on push to main
  4. Azure Key Vault stores placeholder secrets accessible via Managed Identity
  5. Local development environment runs both apps with hot reload
**Plans**: 5 plans in 4 waves

Plans:
- [x] 01-01-PLAN.md — Local development setup (React + .NET + Docker)
- [x] 01-02-PLAN.md — Backend Azure infrastructure and CI/CD
- [x] 01-03-PLAN.md — Frontend CI/CD workflow
- [x] 01-04-PLAN.md — Azure Front Door and custom domain
- [x] 01-05-PLAN.md — Final verification checkpoint

### Phase 2: Spotify Authentication
**Goal**: Users can log in with Spotify and session persists across browser refresh
**Depends on**: Phase 1
**Requirements**: AUTH-01, AUTH-02, AUTH-03, AUTH-04
**Success Criteria** (what must be TRUE):
  1. User can click "Login with Spotify" button and complete OAuth flow
  2. User session persists after browser refresh without re-authentication
  3. User can log out and authentication state clears
  4. Refresh tokens update automatically when expired without user intervention
  5. App displays user's Spotify profile name after successful login
**Plans**: 4 plans in 3 waves

Plans:
- [x] 02-01-PLAN.md — Auth infrastructure (OAuth config, AuthProvider, useAuth hook)
- [x] 02-02-PLAN.md — Login page with context and Spotify button
- [x] 02-03-PLAN.md — Post-login UI (header, profile dropdown, upload page)
- [x] 02-04-PLAN.md — Spotify app setup and end-to-end verification

### Phase 3: Artist Extraction
**Goal**: Users can upload poster images or type festival names to extract artist lists with editing capability
**Depends on**: Phase 2
**Requirements**: INPUT-01, INPUT-02, INPUT-03, INPUT-04, INPUT-05, INPUT-06
**Success Criteria** (what must be TRUE):
  1. User can upload festival poster image (PNG/JPG) under 5MB via file picker
  2. System extracts artist names from poster and displays them as editable list within 30 seconds
  3. User can type festival name and system returns lineup via web search
  4. User can manually add, remove, or edit artist names before proceeding
  5. System reads AI prompt from codebase file, not hardcoded strings
**Plans**: 5 plans in 3 waves

Plans:
- [ ] 03-01-PLAN.md — Backend API with Claude SDK (extraction endpoints, prompt files)
- [ ] 03-02-PLAN.md — Frontend MUI setup and editable chip list component
- [ ] 03-03-PLAN.md — Poster upload UI (file picker, preview, extraction)
- [ ] 03-04-PLAN.md — Festival search UI (autocomplete, web search)
- [ ] 03-05-PLAN.md — Integration and end-to-end verification

### Phase 4: Track Selection
**Goal**: System selects personalized tracks for each artist using familiar + top + recent logic
**Depends on**: Phase 3
**Requirements**: TRACK-01, TRACK-02, TRACK-03, TRACK-04, TRACK-05
**Success Criteria** (what must be TRUE):
  1. System fetches 3 top tracks per artist from Spotify's popularity rankings
  2. System fetches 3 recent releases per artist prioritizing tracks likely played live
  3. System identifies 3 familiar tracks per artist from user's saved tracks and playlists
  4. Duplicate tracks are excluded when same song appears in multiple categories
  5. Artists without Spotify matches are skipped and tracked separately
**Plans**: TBD

Plans:
- [ ] 04-01: [TBD during planning]

### Phase 5: Playlist Creation & Results
**Goal**: Users receive complete Spotify playlist with visibility into included and skipped artists
**Depends on**: Phase 4
**Requirements**: PLAYLIST-01, PLAYLIST-02, PLAYLIST-03, RESULTS-01, RESULTS-02, RESULTS-03, RESULTS-04
**Success Criteria** (what must be TRUE):
  1. System creates playlist on user's Spotify account named after festival/event
  2. Playlist contains all selected tracks from Phase 4 track selection
  3. User receives direct link to playlist that opens in Spotify app or web player
  4. User sees list of artists successfully included in playlist with track counts
  5. User sees list of artists that were skipped with reason (no Spotify match)
  6. Results page is mobile-responsive and displays correctly on phone screens
**Plans**: TBD

Plans:
- [ ] 05-01: [TBD during planning]

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Skeleton Deployment | 5/5 | ✓ Complete | 2026-01-25 |
| 2. Spotify Authentication | 4/4 | ✓ Complete | 2026-01-26 |
| 3. Artist Extraction | 0/5 | Planned | - |
| 4. Track Selection | 0/TBD | Not started | - |
| 5. Playlist Creation & Results | 0/TBD | Not started | - |

---
*Roadmap created: 2026-01-24*
*Last updated: 2026-01-26*
