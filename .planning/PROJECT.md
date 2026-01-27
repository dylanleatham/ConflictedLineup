# Conflicted Lineup

## What This Is

A web app that converts festival lineup posters or festival names into personalized Spotify playlists. Users upload an image or type a festival name, authenticate with Spotify, and receive a curated playlist featuring 9 tracks per artist — mixing familiar favorites, discovery hits, and recent releases likely to be played live.

## Core Value

Users can instantly turn any festival lineup into a personalized discovery playlist without manual artist-by-artist searching.

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] User can upload a festival poster image to extract artist names
- [ ] User can type a festival name to look up lineup via AI web search
- [ ] User must authenticate with Spotify to use the app
- [ ] System extracts artist names using Anthropic AI with user-provided prompt from codebase file
- [ ] System creates Spotify playlist named after the festival/event
- [ ] Playlist includes per artist: 3 top tracks, 3 tracks from user's existing playlists, 3 recent releases
- [ ] Duplicate tracks across categories are excluded
- [ ] User sees list of artists that made it into the playlist
- [ ] User sees which artists were skipped (no Spotify match)
- [ ] User receives link to the created playlist

### Out of Scope

- Public launch — sharing with friends only, no scale concerns
- User accounts — Spotify OAuth is sufficient, no separate user system
- Playlist history — no saving/browsing of previously generated playlists
- Mobile app — web-first, responsive is fine
- Artist selection/filtering — all matched artists go into playlist

## Context

- User has a pre-built prompt for artist extraction that will be read from a file in the codebase
- Small audience (friends) means less concern about rate limits, abuse prevention
- Anthropic Claude with web search capability for looking up festival lineups by name
- Vision capability needed for poster image parsing

## Constraints

- **Frontend:** React
- **Backend:** C# (.NET)
- **Infrastructure:** Azure Front Door, Azure Key Vault, Azure App Service
- **CI/CD:** GitHub Actions
- **AI Provider:** Anthropic (Claude with vision + web search)
- **Auth:** Spotify OAuth (required for all features)
- **Deployment Strategy:** End-to-end skeleton deployed first, then features — mitigate deployment issues early

## Local Development

- **Frontend URL:** http://127.0.0.1:5175 (NOT 5173 — Spotify OAuth redirect requires 5175)
- **Backend URL:** http://localhost:5000
- **Important:** Spotify Developer Dashboard has 127.0.0.1:5175 registered as redirect URI. Using any other port will break OAuth.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| 9 tracks per artist (3+3+3) | Balance between familiar, discovery, and likely-to-be-played-live | — Pending |
| Require Spotify auth upfront | Need playlist access for "familiar tracks" feature | — Pending |
| Deploy skeleton first | Avoid deployment surprises when codebase is complex | — Pending |
| Prompt in codebase file | User has existing tested prompt, keeps it version controlled | — Pending |

---
*Last updated: 2026-01-24 after initialization*
