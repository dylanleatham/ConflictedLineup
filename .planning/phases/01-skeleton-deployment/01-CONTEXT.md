# Phase 1: Skeleton Deployment - Context

**Gathered:** 2026-01-25
**Status:** Ready for planning

<domain>
## Phase Boundary

Empty React and .NET applications deployed to Azure with working secrets management. CI/CD pipeline deploys on push to main. Local development environment runs both apps with hot reload. No features — just infrastructure validation.

</domain>

<decisions>
## Implementation Decisions

### Local dev setup
- Run frontend and backend in separate terminals (not combined startup)
- Docker required for local development — always run through containers
- Environment variables in .env files (.env.local for secrets, .env.example committed as template)
- Mock secrets locally — .env files for local dev, Key Vault only in deployed Azure environment

### CI/CD feedback
- Deploy on push to main only — no PR preview deployments
- GitHub UI only for deployment status — no external notifications
- Tests must pass before deployment proceeds (block on failure)
- Single production environment — no separate staging

### Azure resource naming
- Use `conflictedlineup-*` prefix (e.g., conflictedlineup-api, conflictedlineup-web, conflictedlineup-kv)
- Existing Azure subscription will be provided
- Region: Central US
- Custom domain will be configured (user has domain to use)

### Claude's Discretion
- Specific Docker Compose configuration structure
- GitHub Actions workflow file organization
- Azure resource group naming beyond the prefix convention
- Health check endpoint implementation details

</decisions>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches for React + .NET + Azure deployment patterns.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 01-skeleton-deployment*
*Context gathered: 2026-01-25*
