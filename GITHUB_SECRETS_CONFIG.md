# GitHub Secrets Configuration for New CI/CD Pipeline

This document outlines the required GitHub secrets for the new branch-based deployment pipeline.

## Required Secrets

### Azure Authentication (Shared)
These secrets are used across all environments:

```
AZURE_CLIENT_ID=<your-azure-service-principal-client-id>
AZURE_CLIENT_SECRET=<your-azure-service-principal-client-secret>
AZURE_SUBSCRIPTION_ID=<your-azure-subscription-id>
AZURE_TENANT_ID=<your-azure-tenant-id>
```

### Azure Container Registry (Shared)
```
ACR_REGISTRY=<your-registry-name>.azurecr.io
ACR_USERNAME=<your-registry-username>
ACR_PASSWORD=<your-registry-password>
```

### Production Environment
These secrets configure the production deployment (main branch):

```
PROD_RESOURCE_GROUP=aiportfolioanalysis-prod-rg
PROD_ACI_NAME=aiportfolioanalysis-prod
PROD_DNS_NAME=aiportfolioanalysis
```

### Test Environment
These secrets configure the test deployment (feature branches):

```
TEST_RESOURCE_GROUP=aiportfolioanalysis-test-rg
```

Note: Test ACI names and DNS names are auto-generated based on branch names.

### Google OAuth - Production
```
GOOGLE_CLIENTID=<your-production-google-oauth-client-id>
GOOGLE_CLIENTSECRET=<your-production-google-oauth-client-secret>
```

### Google OAuth - Test (Optional)
If you want separate OAuth applications for test environment:

```
GOOGLE_CLIENTID_TEST=<your-test-google-oauth-client-id>
GOOGLE_CLIENTSECRET_TEST=<your-test-google-oauth-client-secret>
```

If these test secrets are not provided, the workflows will fall back to using the production OAuth secrets.

## Secrets to Remove

If you were using the old authentication method, you can remove:

```
AZURE_CREDENTIALS ❌ (no longer needed)
```

## Environment Setup

### GitHub Environments
The workflows use GitHub environments for deployment protection:

1. **production** - Used for main branch deployments
   - Consider adding protection rules (required reviewers, etc.)
   
2. **test** - Used for feature branch deployments
   - Can be more permissive for faster development cycles

### Azure Resource Groups
Ensure these resource groups exist in your Azure subscription:

```bash
# Create production resource group
az group create --name aiportfolioanalysis-prod-rg --location "South Central US"

# Create test resource group  
az group create --name aiportfolioanalysis-test-rg --location "South Central US"
```

## Workflow Behavior

### CI Pipeline (Enhanced)
- **Triggers**: Push to main or feature branches, PRs to main
- **Builds**: Both ASP.NET and Caddy images
- **Tags**: Branch-specific tags (e.g., `feature-branch-abc123`)
- **Security**: Trivy scanning with SARIF upload

### Production CD Pipeline
- **Triggers**: Successful CI completion on main branch
- **Target**: `aiportfolioanalysis-prod-rg` resource group
- **Resources**: Higher CPU/memory allocation
- **Environment**: Production settings with full health checks

### Test CD Pipeline
- **Triggers**: Successful CI completion on feature branches
- **Target**: `aiportfolioanalysis-test-rg` resource group
- **Resources**: Lower CPU/memory allocation for cost efficiency
- **Environment**: Development settings with basic health checks

### Cleanup Pipeline
- **Triggers**: Feature branch deletion
- **Action**: Removes corresponding test containers automatically
- **Benefit**: Prevents resource accumulation and reduces costs

## Migration Steps

1. **Add new secrets** to GitHub repository settings
2. **Create Azure resource groups** if they don't exist
3. **Set up GitHub environments** (production, test) with appropriate protection rules
4. **Test the pipeline** by creating a feature branch
5. **Remove old secrets** once new pipeline is validated

## Benefits of New Setup

- **Environment Isolation**: Complete separation between test and production
- **Cost Efficiency**: Smaller test containers, automatic cleanup
- **Security**: Maintained Trivy scanning and proper authentication
- **Developer Productivity**: Test changes before merging to main
- **Automated Cleanup**: No manual intervention needed for test environments