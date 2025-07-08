#!/bin/bash

# Azure Container Instances Deployment Script for Caddy + ASP.NET
# This script deploys a multi-container setup with Caddy reverse proxy and ASP.NET backend
# Supports multiple environments (test/production)

set -e

# Environment detection
ENVIRONMENT="${DEPLOYMENT_ENVIRONMENT:-production}"
BRANCH_NAME="${GITHUB_REF_NAME:-main}"

echo "🚀 Starting deployment for environment: $ENVIRONMENT"
echo "📝 Branch: $BRANCH_NAME"

# Environment-specific configuration
if [ "$ENVIRONMENT" = "test" ]; then
    RESOURCE_GROUP="${TEST_RESOURCE_GROUP:-aiportfolioanalysis-test-rg}"
    CONTAINER_GROUP_NAME="${TEST_ACI_NAME:-aiportfolioanalysis-test}"
    DNS_NAME="${TEST_DNS_NAME:-aiportfolioanalysis-test}"
    CADDY_CPU="0.05"
    CADDY_MEMORY="0.05"
    ASPNET_CPU="0.1"
    ASPNET_MEMORY="0.1"
    ASPNET_ENV="Development"
    echo "🧪 Deploying to TEST environment"
else
    RESOURCE_GROUP="${PROD_RESOURCE_GROUP:-aiportfolioanalysis-prod-rg}"
    CONTAINER_GROUP_NAME="${PROD_ACI_NAME:-aiportfolioanalysis-prod}"
    DNS_NAME="${PROD_DNS_NAME:-aiportfolioanalysis}"
    CADDY_CPU="0.1"
    CADDY_MEMORY="0.1"
    ASPNET_CPU="0.2"
    ASPNET_MEMORY="0.2"
    ASPNET_ENV="Production"
    echo "🏭 Deploying to PRODUCTION environment"
fi

LOCATION="${AZURE_LOCATION:-southcentralus}"
REGISTRY_NAME="${AZURE_REGISTRY_NAME:-aiportfolioanalysis}"

# Determine image tags based on branch
if [ "$BRANCH_NAME" = "main" ]; then
    IMAGE_TAG="latest"
else
    # For feature branches, use branch-specific tags
    CLEAN_BRANCH=$(echo "$BRANCH_NAME" | sed 's/[^a-zA-Z0-9-]/-/g')
    IMAGE_TAG="$CLEAN_BRANCH-$(echo $GITHUB_SHA | cut -c1-7)"
fi

# Image names - must match CI workflow
CADDY_IMAGE="$REGISTRY_NAME.azurecr.io/caddy-proxy:$IMAGE_TAG"
ASPNET_IMAGE="$REGISTRY_NAME.azurecr.io/aiportfolioanalysis:$IMAGE_TAG"

# Check if logged in to Azure
echo "Checking Azure login status..."
az account show > /dev/null 2>&1 || { echo "Please login to Azure CLI first: az login"; exit 1; }

# Check if required environment variables are set
if [ -z "$GOOGLE_CLIENTID" ] || [ -z "$GOOGLE_CLIENTSECRET" ]; then
    echo "Error: GOOGLE_CLIENTID and GOOGLE_CLIENTSECRET environment variables must be set"
    echo "These should be provided as GitHub secrets"
    exit 1
fi

# Get registry credentials
echo "Getting registry credentials..."
REGISTRY_SERVER=$(az acr show --name $REGISTRY_NAME --resource-group $RESOURCE_GROUP --query "loginServer" --output tsv)
REGISTRY_USERNAME=$(az acr credential show --name $REGISTRY_NAME --resource-group $RESOURCE_GROUP --query "username" --output tsv)
REGISTRY_PASSWORD=$(az acr credential show --name $REGISTRY_NAME --resource-group $RESOURCE_GROUP --query "passwords[0].value" --output tsv)

echo "Using pre-built images from CI pipeline:"
echo "  Caddy: $CADDY_IMAGE"
echo "  ASP.NET: $ASPNET_IMAGE"

# Delete existing container group if it exists
echo "Checking for existing container group..."
if az container show --resource-group $RESOURCE_GROUP --name $CONTAINER_GROUP_NAME > /dev/null 2>&1; then
    echo "Deleting existing container group..."
    az container delete --resource-group $RESOURCE_GROUP --name $CONTAINER_GROUP_NAME --yes
    echo "Waiting for deletion to complete..."
    sleep 30
fi

# Create simplified container group YAML configuration
cat > container-group.yaml <<EOF
apiVersion: 2021-10-01
location: $LOCATION
name: $CONTAINER_GROUP_NAME
properties:
  containers:
  - name: caddy-proxy
    properties:
      image: $CADDY_IMAGE
      ports:
      - port: 80
      - port: 443
      resources:
        requests:
          cpu: $CADDY_CPU
          memoryInGb: $CADDY_MEMORY
  - name: aspnet-backend
    properties:
      image: $ASPNET_IMAGE
      ports:
      - port: 8080
      resources:
        requests:
          cpu: $ASPNET_CPU
          memoryInGb: $ASPNET_MEMORY
      environmentVariables:
      - name: ASPNETCORE_ENVIRONMENT
        value: $ASPNET_ENV
      - name: ASPNETCORE_URLS
        value: http://+:8080
      - name: Authentication__Google__ClientId
        secureValue: $GOOGLE_CLIENTID
      - name: Authentication__Google__ClientSecret
        secureValue: $GOOGLE_CLIENTSECRET
      - name: Frontend__BaseUrl
        value: https://$DNS_NAME.southcentralus.azurecontainer.io
  ipAddress:
    type: Public
    ports:
    - port: 80
    - port: 443
    dnsNameLabel: $DNS_NAME
  osType: Linux
  restartPolicy: Always
  imageRegistryCredentials:
  - server: $REGISTRY_SERVER
    username: $REGISTRY_USERNAME
    password: $REGISTRY_PASSWORD
type: Microsoft.ContainerInstance/containerGroups
EOF

# Deploy container group
echo "Deploying container group..."
az container create \
    --resource-group $RESOURCE_GROUP \
    --file container-group.yaml

# Wait for deployment to complete
echo "Waiting for deployment to complete..."
sleep 10

# Get the FQDN
FQDN=$(az container show --resource-group $RESOURCE_GROUP --name $CONTAINER_GROUP_NAME --query ipAddress.fqdn --output tsv)

echo "Deployment complete!"
echo "Application URL: https://$FQDN"
echo "HTTP URL: http://$FQDN (will redirect to HTTPS)"
echo ""
echo "Note: It may take a few minutes for:"
echo "  - DNS propagation to complete"
echo "  - Caddy to provision SSL certificates"
echo "  - Application to be fully ready"
echo ""
echo "Monitor deployment with:"
echo "  az container logs --resource-group $RESOURCE_GROUP --name $CONTAINER_GROUP_NAME --container-name caddy-proxy"
echo "  az container logs --resource-group $RESOURCE_GROUP --name $CONTAINER_GROUP_NAME --container-name aspnet-backend"

# Clean up temporary file
rm -f container-group.yaml