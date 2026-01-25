#!/bin/bash
# Azure Infrastructure Setup for Conflicted Lineup
# Run with: ./infra/azure-setup.sh

set -e

# Configuration
RESOURCE_GROUP="rg-conflictedlineup-prod-centralus"
LOCATION="centralus"
APP_SERVICE_PLAN="asp-conflictedlineup-prod"
BACKEND_APP="conflictedlineup-api"
FRONTEND_APP="conflictedlineup-web"
KEY_VAULT="conflictedlineup-kv"
GITHUB_ORG="<GITHUB_ORG>"  # User fills in
GITHUB_REPO="<GITHUB_REPO>"  # User fills in

echo "Creating resource group..."
az group create --name $RESOURCE_GROUP --location $LOCATION

echo "Creating App Service Plan (B1 tier)..."
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux

echo "Creating Backend App Service..."
az webapp create \
  --name $BACKEND_APP \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "DOTNETCORE:9.0"

echo "Enabling system-assigned managed identity for backend..."
az webapp identity assign \
  --name $BACKEND_APP \
  --resource-group $RESOURCE_GROUP

echo "Creating Frontend App Service..."
az webapp create \
  --name $FRONTEND_APP \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "NODE:24-lts"

echo "Creating Key Vault..."
az keyvault create \
  --name $KEY_VAULT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --enable-rbac-authorization true

echo "Adding placeholder secret to Key Vault..."
az keyvault secret set \
  --vault-name $KEY_VAULT \
  --name "placeholder-secret" \
  --value "skeleton-deployment-working"

# Get backend managed identity principal ID
BACKEND_PRINCIPAL_ID=$(az webapp identity show \
  --name $BACKEND_APP \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)

# Get Key Vault resource ID
KEY_VAULT_ID=$(az keyvault show \
  --name $KEY_VAULT \
  --resource-group $RESOURCE_GROUP \
  --query id -o tsv)

echo "Granting backend access to Key Vault secrets..."
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee-object-id $BACKEND_PRINCIPAL_ID \
  --assignee-principal-type ServicePrincipal \
  --scope $KEY_VAULT_ID

echo "Configuring backend app settings with Key Vault reference..."
az webapp config appsettings set \
  --name $BACKEND_APP \
  --resource-group $RESOURCE_GROUP \
  --settings PlaceholderSecret="@Microsoft.KeyVault(SecretUri=https://${KEY_VAULT}.vault.azure.net/secrets/placeholder-secret)"

echo "Creating App Registration for GitHub Actions OIDC..."
APP_ID=$(az ad app create \
  --display-name "conflictedlineup-github-actions" \
  --query appId -o tsv)

echo "Creating Service Principal..."
az ad sp create --id $APP_ID

# Get subscription ID
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

echo "Assigning Website Contributor role to Service Principal..."
az role assignment create \
  --role "Website Contributor" \
  --assignee $APP_ID \
  --scope "/subscriptions/${SUBSCRIPTION_ID}/resourceGroups/${RESOURCE_GROUP}"

echo "Creating federated credential for GitHub Actions..."
az ad app federated-credential create \
  --id $APP_ID \
  --parameters "{
    \"name\": \"github-actions-main\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_ORG}/${GITHUB_REPO}:ref:refs/heads/main\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }"

echo ""
echo "=== SETUP COMPLETE ==="
echo ""
echo "Add these secrets to GitHub repository (Settings -> Secrets -> Actions):"
echo "  AZURE_CLIENT_ID: $APP_ID"
echo "  AZURE_TENANT_ID: $(az account show --query tenantId -o tsv)"
echo "  AZURE_SUBSCRIPTION_ID: $SUBSCRIPTION_ID"
echo ""
echo "Backend URL: https://${BACKEND_APP}.azurewebsites.net"
echo "Frontend URL: https://${FRONTEND_APP}.azurewebsites.net"
