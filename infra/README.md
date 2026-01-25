# Azure Infrastructure Setup

This directory contains scripts and documentation for provisioning Azure infrastructure for Conflicted Lineup.

## Prerequisites

1. **Azure CLI installed**: [Install instructions](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
2. **Azure subscription**: Active Azure subscription with Owner or Contributor access
3. **GitHub repository**: Repository where you'll configure GitHub Actions secrets

## Setup Instructions

### 1. Customize Configuration

Edit `azure-setup.sh` and replace placeholders:

```bash
GITHUB_ORG="<GITHUB_ORG>"    # Your GitHub username or organization
GITHUB_REPO="<GITHUB_REPO>"  # Repository name (e.g., ConflictedLineup)
```

### 2. Login to Azure

```bash
az login
az account show  # Verify correct subscription is selected
```

If you have multiple subscriptions, set the correct one:

```bash
az account list --output table
az account set --subscription "YOUR_SUBSCRIPTION_ID"
```

### 3. Run Setup Script

```bash
chmod +x infra/azure-setup.sh
./infra/azure-setup.sh
```

The script will:
- Create resource group in Central US
- Create App Service Plan (B1 tier)
- Create two App Services (backend API, frontend web)
- Create Azure Key Vault with RBAC authorization
- Configure Managed Identity for backend to access Key Vault
- Add placeholder secret to verify Key Vault access
- Create App Registration and Service Principal for GitHub Actions
- Configure OIDC federated credential for passwordless authentication

**Expected duration:** 3-5 minutes

### 4. Configure GitHub Secrets

After the script completes, it will output three values. Add them to your GitHub repository:

1. Go to GitHub repository → **Settings** → **Secrets and variables** → **Actions**
2. Click **New repository secret** for each:
   - Name: `AZURE_CLIENT_ID`, Value: (from script output)
   - Name: `AZURE_TENANT_ID`, Value: (from script output)
   - Name: `AZURE_SUBSCRIPTION_ID`, Value: (from script output)

### 5. Wait for Permission Propagation

Azure RBAC permissions can take 5-10 minutes to propagate. Wait before pushing code that triggers GitHub Actions.

### 6. Verify Deployment

After GitHub Actions runs:

```bash
# Check backend health endpoint
curl https://conflictedlineup-api.azurewebsites.net/api/health

# Expected response:
# {"status":"Healthy","timestamp":"2026-01-25T..."}
```

## Resources Created

| Resource | Name | Purpose |
|----------|------|---------|
| Resource Group | rg-conflictedlineup-prod-centralus | Container for all resources |
| App Service Plan | asp-conflictedlineup-prod | B1 Linux plan for hosting |
| Backend App Service | conflictedlineup-api | .NET 9 API hosting |
| Frontend App Service | conflictedlineup-web | Node 24 static site hosting |
| Key Vault | conflictedlineup-kv | Secrets management with RBAC |
| App Registration | conflictedlineup-github-actions | OIDC authentication for CI/CD |

## Troubleshooting

### Error: "The subscription is not registered to use namespace 'Microsoft.Web'"

```bash
az provider register --namespace Microsoft.Web
az provider register --namespace Microsoft.KeyVault
# Wait 2-3 minutes for registration to complete
az provider show --namespace Microsoft.Web --query "registrationState"
```

### Error: "Key Vault name already exists"

Key Vault names are globally unique. Edit `azure-setup.sh` and change:

```bash
KEY_VAULT="conflictedlineup-kv-YOURNAME"
```

### Error: "Insufficient privileges to complete the operation"

Ensure your Azure account has:
- Subscription-level Contributor or Owner role
- Microsoft Entra ID permissions to create App Registrations

### GitHub Actions failing with "AADSTS700016: Application not found"

OIDC federated credential may not be propagated. Wait 5-10 minutes and retry the workflow.

### Backend can't read Key Vault secrets

1. Verify Managed Identity is enabled:
   ```bash
   az webapp identity show --name conflictedlineup-api --resource-group rg-conflictedlineup-prod-centralus
   ```

2. Verify role assignment exists:
   ```bash
   az role assignment list --assignee <PRINCIPAL_ID_FROM_ABOVE> --scope <KEY_VAULT_ID>
   ```

3. Check App Service logs:
   ```bash
   az webapp log tail --name conflictedlineup-api --resource-group rg-conflictedlineup-prod-centralus
   ```

## Teardown

To delete all resources and avoid ongoing charges:

```bash
az group delete --name rg-conflictedlineup-prod-centralus --yes --no-wait
az ad app delete --id <APP_ID_FROM_SETUP_OUTPUT>
```

**Warning:** This is irreversible and will delete:
- Both App Services
- Key Vault (with 90-day soft delete retention)
- App Service Plan
- All secrets and configurations

## Cost Estimate

- **App Service Plan (B1):** ~$13/month
- **Key Vault:** ~$0.03/10,000 operations + $0.03/secret/month
- **App Service instances:** Included in plan cost
- **Estimated total:** ~$14/month

Free tier alternatives exist but lack production features (custom domains, SSL, scaling).

## Next Steps

After successful deployment:

1. Configure custom domain (optional)
2. Enable Application Insights for monitoring
3. Configure scaling rules
4. Set up deployment slots for staging
5. Add production secrets to Key Vault (Spotify API keys, etc.)
