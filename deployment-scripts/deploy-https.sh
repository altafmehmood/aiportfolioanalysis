#!/bin/bash

# Deploy HTTPS-enabled AI Portfolio Analysis to Azure Container Groups
# This script automates the deployment process

set -e

# Configuration
RESOURCE_GROUP="aiportfolioanalysis-rg"
LOCATION="South Central US"
CONTAINER_GROUP_NAME="aiportfolioanalysis-https"
STORAGE_ACCOUNT_NAME="aiportfolioanalysiscerts"
ACR_NAME="aiportfolioanalysisacr"
DNS_NAME_LABEL="aiportfolioanalysis"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}Starting HTTPS deployment for AI Portfolio Analysis${NC}"

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo -e "${RED}Azure CLI is not installed. Please install it first.${NC}"
    exit 1
fi

# Check if logged in to Azure
if ! az account show &> /dev/null; then
    echo -e "${RED}Please log in to Azure CLI first: az login${NC}"
    exit 1
fi

# Function to check if resource group exists
check_resource_group() {
    if ! az group show --name $RESOURCE_GROUP &> /dev/null; then
        echo -e "${YELLOW}Creating resource group: $RESOURCE_GROUP${NC}"
        az group create --name $RESOURCE_GROUP --location "$LOCATION"
    else
        echo -e "${GREEN}Resource group $RESOURCE_GROUP already exists${NC}"
    fi
}

# Function to create storage account for certificates
create_storage_account() {
    if ! az storage account show --name $STORAGE_ACCOUNT_NAME --resource-group $RESOURCE_GROUP &> /dev/null; then
        echo -e "${YELLOW}Creating storage account: $STORAGE_ACCOUNT_NAME${NC}"
        az storage account create \
            --name $STORAGE_ACCOUNT_NAME \
            --resource-group $RESOURCE_GROUP \
            --location "$LOCATION" \
            --sku Standard_LRS
    else
        echo -e "${GREEN}Storage account $STORAGE_ACCOUNT_NAME already exists${NC}"
    fi

    # Create file shares for Caddy data
    STORAGE_KEY=$(az storage account keys list --resource-group $RESOURCE_GROUP --account-name $STORAGE_ACCOUNT_NAME --query '[0].value' -o tsv)
    
    az storage share create \
        --name caddy-data \
        --account-name $STORAGE_ACCOUNT_NAME \
        --account-key $STORAGE_KEY || true

    az storage share create \
        --name caddy-config \
        --account-name $STORAGE_ACCOUNT_NAME \
        --account-key $STORAGE_KEY || true
}

# Function to create container registry
create_container_registry() {
    if ! az acr show --name $ACR_NAME --resource-group $RESOURCE_GROUP &> /dev/null; then
        echo -e "${YELLOW}Creating container registry: $ACR_NAME${NC}"
        az acr create \
            --resource-group $RESOURCE_GROUP \
            --name $ACR_NAME \
            --sku Basic \
            --location "$LOCATION"
    else
        echo -e "${GREEN}Container registry $ACR_NAME already exists${NC}"
    fi
}

# Function to build and push container images
build_and_push_images() {
    echo -e "${YELLOW}Building and pushing container images${NC}"
    
    # Login to ACR
    az acr login --name $ACR_NAME

    # Build application container
    docker build -t aiportfolioanalysis:latest .
    docker tag aiportfolioanalysis:latest $ACR_NAME.azurecr.io/aiportfolioanalysis:latest
    docker push $ACR_NAME.azurecr.io/aiportfolioanalysis:latest

    # Build Caddy container
    docker build -f Dockerfile.caddy -t aiportfolioanalysis-caddy:latest .
    docker tag aiportfolioanalysis-caddy:latest $ACR_NAME.azurecr.io/aiportfolioanalysis-caddy:latest
    docker push $ACR_NAME.azurecr.io/aiportfolioanalysis-caddy:latest
}

# Function to deploy container group
deploy_container_group() {
    echo -e "${YELLOW}Deploying container group${NC}"
    
    # Get storage account key
    STORAGE_KEY=$(az storage account keys list --resource-group $RESOURCE_GROUP --account-name $STORAGE_ACCOUNT_NAME --query '[0].value' -o tsv)
    
    # Check if required environment variables are set
    if [[ -z "$GOOGLE_CLIENTID" || -z "$GOOGLE_CLIENTSECRET" ]]; then
        echo -e "${RED}Please set GOOGLE_CLIENTID and GOOGLE_CLIENTSECRET environment variables${NC}"
        exit 1
    fi

    # Update ARM template with ACR references
    sed -i.bak "s/aiportfolioanalysis:latest/$ACR_NAME.azurecr.io\/aiportfolioanalysis:latest/g" azure-container-group.json
    sed -i.bak "s/aiportfolioanalysis-caddy:latest/$ACR_NAME.azurecr.io\/aiportfolioanalysis-caddy:latest/g" azure-container-group.json

    # Deploy container group
    az deployment group create \
        --resource-group $RESOURCE_GROUP \
        --template-file azure-container-group.json \
        --parameters \
            containerGroupName=$CONTAINER_GROUP_NAME \
            location="$LOCATION" \
            googleClientId="$GOOGLE_CLIENTID" \
            googleClientSecret="$GOOGLE_CLIENTSECRET" \
            storageAccountName=$STORAGE_ACCOUNT_NAME \
            storageAccountKey="$STORAGE_KEY" \
            dnsNameLabel=$DNS_NAME_LABEL

    # Restore original ARM template
    mv azure-container-group.json.bak azure-container-group.json
}

# Function to display deployment information
display_deployment_info() {
    echo -e "${GREEN}Deployment completed successfully!${NC}"
    echo -e "${GREEN}Application URL: https://$DNS_NAME_LABEL.southcentralus.azurecontainer.io${NC}"
    echo -e "${YELLOW}Next steps:${NC}"
    echo "1. Wait for Caddy to automatically provision SSL certificates (takes 1-2 minutes)"
    echo "2. Update Google OAuth settings with HTTPS redirect URLs"
    echo "3. Test the application and SSL configuration"
    echo "4. Enjoy automatic certificate renewal! 🎉"
}

# Main execution
main() {
    echo -e "${YELLOW}Starting deployment process...${NC}"
    
    check_resource_group
    create_storage_account
    create_container_registry
    build_and_push_images
    deploy_container_group
    display_deployment_info
    
    echo -e "${GREEN}Deployment script completed!${NC}"
}

# Run main function
main "$@"