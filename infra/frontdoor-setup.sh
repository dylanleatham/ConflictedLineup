#!/bin/bash
# Azure Front Door and Custom Domain Setup
# Run with: ./infra/frontdoor-setup.sh <custom-domain>
# Example: ./infra/frontdoor-setup.sh app.conflictedlineup.com

set -e

if [ -z "$1" ]; then
  echo "Usage: ./infra/frontdoor-setup.sh <custom-domain>"
  echo "Example: ./infra/frontdoor-setup.sh app.conflictedlineup.com"
  exit 1
fi

CUSTOM_DOMAIN=$1
RESOURCE_GROUP="rg-conflictedlineup-prod-centralus"
FRONT_DOOR_PROFILE="afd-conflictedlineup"
FRONT_DOOR_ENDPOINT="conflictedlineup"
FRONTEND_APP="conflictedlineup-web"
BACKEND_APP="conflictedlineup-api"

echo "Creating Azure Front Door Standard profile..."
az afd profile create \
  --profile-name $FRONT_DOOR_PROFILE \
  --resource-group $RESOURCE_GROUP \
  --sku Standard_AzureFrontDoor

echo "Creating Front Door endpoint..."
az afd endpoint create \
  --endpoint-name $FRONT_DOOR_ENDPOINT \
  --profile-name $FRONT_DOOR_PROFILE \
  --resource-group $RESOURCE_GROUP \
  --enabled-state Enabled

echo "Creating origin group for frontend..."
az afd origin-group create \
  --origin-group-name "frontend-origin-group" \
  --profile-name $FRONT_DOOR_PROFILE \
  --resource-group $RESOURCE_GROUP \
  --probe-path "/" \
  --probe-protocol Https \
  --probe-request-type HEAD \
  --probe-interval-in-seconds 30 \
  --sample-size 4 \
  --successful-samples-required 3

echo "Adding frontend App Service as origin..."
az afd origin create \
  --origin-name "frontend-origin" \
  --origin-group-name "frontend-origin-group" \
  --profile-name $FRONT_DOOR_PROFILE \
  --resource-group $RESOURCE_GROUP \
  --host-name "${FRONTEND_APP}.azurewebsites.net" \
  --origin-host-header "${FRONTEND_APP}.azurewebsites.net" \
  --http-port 80 \
  --https-port 443 \
  --priority 1 \
  --weight 1000 \
  --enabled-state Enabled

echo "Creating origin group for backend API..."
az afd origin-group create \
  --origin-group-name "backend-origin-group" \
  --profile-name $FRONT_DOOR_PROFILE \
  --resource-group $RESOURCE_GROUP \
  --probe-path "/api/health" \
  --probe-protocol Https \
  --probe-request-type GET \
  --probe-interval-in-seconds 30

echo "Adding backend App Service as origin..."
az afd origin create \
  --origin-name "backend-origin" \
  --origin-group-name "backend-origin-group" \
  --profile-name $FRONT_DOOR_PROFILE \
  --resource-group $RESOURCE_GROUP \
  --host-name "${BACKEND_APP}.azurewebsites.net" \
  --origin-host-header "${BACKEND_APP}.azurewebsites.net" \
  --http-port 80 \
  --https-port 443 \
  --priority 1 \
  --weight 1000 \
  --enabled-state Enabled

echo "Creating route for frontend (default)..."
az afd route create \
  --route-name "frontend-route" \
  --endpoint-name $FRONT_DOOR_ENDPOINT \
  --profile-name $FRONT_DOOR_PROFILE \
  --resource-group $RESOURCE_GROUP \
  --origin-group "frontend-origin-group" \
  --patterns-to-match "/*" \
  --supported-protocols Https \
  --https-redirect Enabled \
  --forwarding-protocol HttpsOnly

echo "Creating route for backend API..."
az afd route create \
  --route-name "api-route" \
  --endpoint-name $FRONT_DOOR_ENDPOINT \
  --profile-name $FRONT_DOOR_PROFILE \
  --resource-group $RESOURCE_GROUP \
  --origin-group "backend-origin-group" \
  --patterns-to-match "/api/*" \
  --supported-protocols Https \
  --https-redirect Enabled \
  --forwarding-protocol HttpsOnly

# Get the Front Door endpoint hostname
AFD_HOSTNAME=$(az afd endpoint show \
  --endpoint-name $FRONT_DOOR_ENDPOINT \
  --profile-name $FRONT_DOOR_PROFILE \
  --resource-group $RESOURCE_GROUP \
  --query hostName -o tsv)

echo ""
echo "=== FRONT DOOR CREATED ==="
echo ""
echo "Front Door endpoint: https://${AFD_HOSTNAME}"
echo ""
echo "=== CUSTOM DOMAIN SETUP ==="
echo ""
echo "Step 1: Add DNS records at your DNS provider:"
echo ""
echo "  For apex domain (${CUSTOM_DOMAIN}):"
echo "    Type: ALIAS or ANAME (if supported) or A record"
echo "    Value: ${AFD_HOSTNAME}"
echo ""
echo "  For subdomain (e.g., www.${CUSTOM_DOMAIN} or app.${CUSTOM_DOMAIN}):"
echo "    Type: CNAME"
echo "    Name: www (or app, etc.)"
echo "    Value: ${AFD_HOSTNAME}"
echo ""
echo "Step 2: After DNS propagation (5-30 minutes), run this command to add the custom domain:"
echo ""
echo "  az afd custom-domain create \\"
echo "    --custom-domain-name \"${CUSTOM_DOMAIN//./-}\" \\"
echo "    --profile-name $FRONT_DOOR_PROFILE \\"
echo "    --resource-group $RESOURCE_GROUP \\"
echo "    --host-name \"${CUSTOM_DOMAIN}\" \\"
echo "    --certificate-type ManagedCertificate \\"
echo "    --minimum-tls-version TLS12"
echo ""
echo "Step 3: Associate the custom domain with the endpoint:"
echo ""
echo "  az afd route update \\"
echo "    --route-name frontend-route \\"
echo "    --endpoint-name $FRONT_DOOR_ENDPOINT \\"
echo "    --profile-name $FRONT_DOOR_PROFILE \\"
echo "    --resource-group $RESOURCE_GROUP \\"
echo "    --custom-domains \"${CUSTOM_DOMAIN//./-}\""
echo ""
