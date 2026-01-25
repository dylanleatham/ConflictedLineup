---
phase: 01-skeleton-deployment
plan: 02
subsystem: infra
tags: [azure, app-service, key-vault, github-actions, oidc, ci-cd]

# Dependency graph
requires:
  - phase: 01-01
    provides: Skeleton apps to deploy
provides:
  - Azure App Service for backend (.NET 9)
  - Azure App Service for frontend (Node 24)
  - Azure Key Vault with RBAC authorization
  - GitHub Actions OIDC authentication for deployments
  - CI/CD pipeline for backend deployment
affects: [01-03-frontend-cicd, 01-04-frontdoor, all-future-deployments]

# Tech tracking
tech-stack:
  added:
    - Azure App Service (B1 Linux)
    - Azure Key Vault (RBAC)
    - GitHub Actions OIDC
    - Managed Identity
  patterns:
    - OIDC federated credentials for passwordless CI/CD
    - Key Vault references in App Service configuration
    - Managed Identity for service-to-service authentication

key-files:
  created:
    - infra/azure-setup.sh
    - infra/README.md
    - .github/workflows/backend-deploy.yml
  modified: []

key-decisions:
  - "Use RBAC authorization for Key Vault instead of access policies"
  - "Use OIDC federated credentials for GitHub Actions instead of service principal secrets"
  - "Use Managed Identity for App Service to Key Vault access"
  - "Website Contributor role for GitHub Actions (minimal required permissions)"

patterns-established:
  - "Azure infrastructure provisioning via CLI scripts in infra/ directory"
  - "GitHub Actions workflows with test job gating deploy job"
  - "Key Vault references in app settings: @Microsoft.KeyVault(SecretUri=...)"

# Metrics
duration: 15min (including user setup time)
completed: 2026-01-25
---

# Phase 01 Plan 02: Backend Azure Infrastructure Summary

**Azure infrastructure setup with App Service, Key Vault, Managed Identity, and GitHub Actions CI/CD with OIDC authentication**

## Performance

- **Duration:** ~15 min (including manual Azure Portal configuration)
- **Completed:** 2026-01-25
- **Tasks:** 3 (2 auto + 1 checkpoint)
- **Files created:** 3

## Accomplishments
- Created comprehensive Azure CLI setup script for infrastructure provisioning
- Created GitHub Actions workflow for backend deployment with OIDC authentication
- User configured Azure infrastructure via Portal (Key Vault RBAC, role assignments)
- GitHub repository secrets configured for CI/CD authentication

## Task Commits

1. **Task 1: Create Azure infrastructure setup script** - `79e85e3` (chore)
   - Files: infra/azure-setup.sh, infra/README.md

2. **Task 2: Create GitHub Actions workflow for backend deployment** - `5ac1956` (feat)
   - Files: .github/workflows/backend-deploy.yml

3. **Task 3: Azure setup checkpoint** - Completed via Azure Portal
   - User configured Key Vault RBAC permissions
   - User added placeholder secret to Key Vault
   - User assigned Website Contributor role to GitHub Actions app
   - User configured GitHub repository secrets

## Azure Resources Created

| Resource | Name | Purpose |
|----------|------|---------|
| Resource Group | rg-conflictedlineup-prod-centralus | Container for all resources |
| App Service Plan | asp-conflictedlineup-prod | B1 Linux hosting plan |
| Backend App Service | conflictedlineup-api | .NET 9 API hosting |
| Frontend App Service | conflictedlineup-web | Node 24 static hosting |
| Key Vault | conflictedlineup-kv | Secrets management |
| App Registration | conflictedlineup-github-actions | OIDC for CI/CD |

## GitHub Secrets Configured

- `AZURE_CLIENT_ID` - App registration for OIDC
- `AZURE_TENANT_ID` - Azure AD tenant
- `AZURE_SUBSCRIPTION_ID` - Target subscription

## Decisions Made

1. **RBAC for Key Vault:** Used `--enable-rbac-authorization true` for fine-grained access control via Azure roles instead of vault access policies

2. **OIDC over service principal secrets:** Federated credentials eliminate need to store and rotate secrets in GitHub

3. **Managed Identity:** Backend App Service uses system-assigned managed identity to access Key Vault without storing credentials

4. **Key Vault references:** App settings use `@Microsoft.KeyVault(SecretUri=...)` syntax for automatic secret injection

## Deviations from Plan

- **Azure Portal used for RBAC assignments:** CLI commands for role assignments failed due to subscription context issues; completed successfully via Azure Portal UI instead

## Issues Encountered

1. **Key Vault RBAC permission error:** Initial `az keyvault secret set` failed because user didn't have Key Vault Secrets Officer role. Resolved by assigning role via Azure Portal.

2. **CLI subscription context:** Role assignment commands failed with "MissingSubscription" error. User completed these steps in Azure Portal instead.

## User Setup Completed

- [x] Azure CLI login
- [x] Key Vault Secrets Officer role for user
- [x] Placeholder secret added to Key Vault
- [x] Key Vault Secrets User role for backend Managed Identity
- [x] Website Contributor role for GitHub Actions app
- [x] GitHub repository secrets configured

## Next Steps

- Push changes to main branch to trigger backend deployment
- Verify backend health endpoint at https://conflictedlineup-api.azurewebsites.net/api/health
- Proceed to Plan 01-04 (Azure Front Door and custom domain)

---
*Phase: 01-skeleton-deployment*
*Completed: 2026-01-25*
