# Conflicted Lineup

## What This Is

A web app that converts festival lineup posters or festival names into personalized Spotify playlists. Users upload an image or type a festival name, authenticate with Spotify, and receive a curated playlist featuring top tracks per artist — with infrastructure for mixing familiar favorites, discovery hits, and recent releases.

## Core Value

Users can instantly turn any festival lineup into a personalized discovery playlist without manual artist-by-artist searching.

## Current State

**v1 MVP shipped: 2026-01-28**

- 5,157 lines of TypeScript/C#/CSS across 129 files
- Deployed to Azure (App Service, Front Door, Key Vault)
- CI/CD via GitHub Actions
- Spotify Development Mode (25 user limit)

**Tech Stack:**
- Frontend: React 18 + Vite 5 + TypeScript
- Backend: .NET 9 Minimal API
- AI: Anthropic Claude (Vision + Web Search)
- Auth: Spotify OAuth PKCE

## Requirements

### Validated

- Upload festival poster image to extract artist names — v1
- Type festival name to look up lineup via AI web search — v1
- Authenticate with Spotify using OAuth PKCE — v1
- Extract artist names using Claude Vision and web search — v1
- Edit artist list before creating playlist — v1
- Create Spotify playlist named after festival/event — v1
- Fetch top tracks per artist from Spotify — v1
- Skip unmatched artists and show which were skipped — v1
- Display results with playlist link and mobile-responsive UI — v1

### Active

(No active requirements — v1 complete)

### Out of Scope

- Public launch — sharing with friends only, no scale concerns
- User accounts — Spotify OAuth is sufficient, no separate user system
- Playlist history — no saving/browsing of previously generated playlists
- Mobile app — web-first, responsive is fine
- Artist selection/filtering — all matched artists go into playlist

## Context

- Spotify Development Mode limits app to 25 users — sufficient for friends/family beta
- Consider upgrading to Extended Quota Mode when ready for wider release
- Rate limiting required 1-second delays between Spotify API calls
- Familiar tracks and recent releases infrastructure exists but disabled

## Constraints

- **Frontend:** React
- **Backend:** C# (.NET)
- **Infrastructure:** Azure Front Door, Azure Key Vault, Azure App Service
- **CI/CD:** GitHub Actions
- **AI Provider:** Anthropic (Claude with vision + web search)
- **Auth:** Spotify OAuth (required for all features)

## Local Development

- **Frontend URL:** http://127.0.0.1:5175 (NOT 5173 — Spotify OAuth redirect requires 5175)
- **Backend URL:** http://localhost:8080
- **Important:** Spotify Developer Dashboard has 127.0.0.1:5175 registered as redirect URI. Using any other port will break OAuth.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Deploy skeleton first | Avoid deployment surprises when codebase is complex | Good — caught Azure/Front Door issues early |
| Spotify OAuth PKCE | No client secret needed, secure for SPAs | Good — simpler setup, works correctly |
| Search-first UX | Festival name search as primary, poster upload as fallback | Good — more intuitive flow |
| Claude SDK web search tool | More reliable than prompt-based web search instructions | Good — consistent results |
| Top 5 tracks only | Simplified to avoid rate limiting | Revisit — enable familiar/recent when rate limiting addressed |
| Use 127.0.0.1 for local dev | Spotify doesn't support localhost for OAuth | Good — required for Spotify compatibility |

---
*Last updated: 2026-01-28 after v1 milestone*
