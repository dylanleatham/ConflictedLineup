---
phase: 01-skeleton-deployment
plan: 04
subsystem: infra
tags: [azure-front-door, cdn, custom-domain, https, routing]

# Dependency graph
requires:
  - phase: 01-02
    provides: Azure App Services deployed
  - phase: 01-03
    provides: Frontend deployed and accessible
provides:
  - Azure Front Door CDN and routing
  - Custom domain with HTTPS
  - Unified routing (/api/* to backend, /* to frontend)
affects: [all-future-phases, production-urls]

# Tech tracking
tech-stack:
  added:
    - Azure Front Door Standard
    - Azure-managed TLS certificates
  patterns:
    - Path-based routing for frontend/backend separation
    - CDN with health probes for origin monitoring
    - HTTPS-only with automatic redirect

key-files:
  created:
    - infra/frontdoor-setup.sh
  modified: []

key-decisions:
  - "Use Azure Front Door Standard tier for CDN and routing"
  - "Path-based routing: /api/* to backend, /* to frontend"
  - "Azure-managed certificates for custom domain HTTPS"
  - "Complete setup via Azure Portal due to Git Bash path conversion issues"

patterns-established:
  - "Front Door as unified entry point for both frontend and backend"
  - "Origin groups with health probes for each service"
  - "MSYS_NO_PATHCONV=1 for Azure CLI scripts in Git Bash on Windows"

# Metrics
duration: 20min
completed: 2026-01-25
---

# Phase 01 Plan 04: Azure Front Door Summary

**Azure Front Door CDN with custom domain, path-based routing to frontend and backend services**

## Performance

- **Duration:** ~20 min (including Portal configuration)
- **Completed:** 2026-01-25
- **Tasks:** 2 (1 auto + 1 checkpoint)
- **Files created:** 1

## Accomplishments
- Created Azure Front Door setup script for future reference/automation
- Configured Front Door via Azure Portal with origin groups and routes
- Set up custom domain with Azure-managed HTTPS certificate
- Unified routing: /api/* to backend, /* to frontend

## Task Commits

1. **Task 1: Create Azure Front Door setup script** - `56213e9` (chore)
   - Files: infra/frontdoor-setup.sh
   - Fixed Git Bash path conversion issue: `a8deaec`

2. **Task 2: Front Door and custom domain checkpoint** - Completed via Azure Portal
   - User created origin groups for frontend and backend
   - User configured path-based routes
   - User set up custom domain with HTTPS

## Azure Resources Created

| Resource | Name | Purpose |
|----------|------|---------|
| Front Door Profile | afd-conflictedlineup | CDN and routing |
| Endpoint | conflictedlineup | Entry point |
| Origin Group | frontend-origin-group | Routes to frontend App Service |
| Origin Group | backend-origin-group | Routes to backend App Service |
| Route | frontend-route | /* → frontend |
| Route | api-route | /api/* → backend |
| Custom Domain | [user's domain] | Production URL with HTTPS |

## Routing Configuration

| Pattern | Destination | Protocol |
|---------|-------------|----------|
| /api/* | backend-origin-group | HTTPS only |
| /* | frontend-origin-group | HTTPS only |

## Decisions Made

1. **Azure Portal for setup:** Git Bash path conversion issues (`/` → `C:/Program Files/Git/`) made CLI script execution problematic. Completed setup via Portal instead.

2. **Path-based routing:** Single domain serves both frontend and API, simplifying CORS and providing unified entry point.

3. **Azure-managed certificates:** Automatic HTTPS certificate provisioning and renewal.

## Deviations from Plan

- **Completed via Azure Portal instead of CLI script:** Git Bash on Windows converts Unix-style paths, breaking Azure CLI commands with path parameters. Added `MSYS_NO_PATHCONV=1` fix for future runs.

## Issues Encountered

1. **Git Bash path conversion:** `--probe-path "/"` was converted to `C:/Program Files/Git/`. Fixed by adding `export MSYS_NO_PATHCONV=1` to script.

## Verified Working

- [x] Front Door endpoint accessible
- [x] Custom domain resolves with valid HTTPS certificate
- [x] /api/health routes to backend
- [x] / routes to frontend (Hello World page)

---
*Phase: 01-skeleton-deployment*
*Completed: 2026-01-25*
