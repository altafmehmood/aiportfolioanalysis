#!/bin/bash

# Azure Container Instances Deployment Script for Caddy + ASP.NET
# This script deploys a multi-container setup with Caddy reverse proxy and ASP.NET backend
# Designed for GitHub Actions with secrets

set -e

# Configuration - Using GitHub secrets
RESOURCE_GROUP="${AZURE_RESOURCE_GROUP:-aiportfolioanalysis}"
LOCATION="${AZURE_LOCATION:-southcentralus}"
REGISTRY_NAME="${AZURE_REGISTRY_NAME:-aiportfolioanalysis}"
CONTAINER_GROUP_NAME="${AZURE_CONTAINER_GROUP_NAME:-aiportfolioanalysis}"
DNS_NAME="${AZURE_DNS_NAME:-aiportfolioanalysis}"

# Image names - must match CI workflow
CADDY_IMAGE="$REGISTRY_NAME.azurecr.io/caddy-proxy:latest"
ASPNET_IMAGE="$REGISTRY_NAME.azurecr.io/aiportfolioanalysis:latest"

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
          cpu: 0.5
          memoryInGb: 1
  - name: aspnet-backend
    properties:
      image: $ASPNET_IMAGE
      ports:
      - port: 8080
      resources:
        requests:
          cpu: 0.5
          memoryInGb: 1
      environmentVariables:
      - name: ASPNETCORE_ENVIRONMENT
        value: Production
      - name: ASPNETCORE_URLS
        value: http://+:8080
      - name: GOOGLE_CLIENTID
        secureValue: $GOOGLE_CLIENTID
      - name: GOOGLE_CLIENTSECRET
        secureValue: $GOOGLE_CLIENTSECRET
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