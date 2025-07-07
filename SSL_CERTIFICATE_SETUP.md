# SSL Certificate Setup for Azure Container Groups

This document describes how to set up SSL certificates for the AI Portfolio Analysis application running on Azure Container Groups.

## Prerequisites

- Azure CLI installed and configured
- Azure subscription with Container Instance permissions
- Azure Storage Account for certificate persistence
- Domain name pointing to your Azure Container Instance

## Certificate Management Options

### Option 1: Let's Encrypt Manual Certificate (Recommended)

1. **Generate Let's Encrypt Certificate Locally**:
   ```bash
   # Install certbot locally
   sudo apt-get update
   sudo apt-get install certbot

   # Generate certificate (replace with your domain)
   sudo certbot certonly --manual --preferred-challenges dns \
     -d aiportfolioanalysis.southcentralus.azurecontainer.io
   ```

2. **Upload Certificates to Azure File Share**:
   ```bash
   # Create Azure Storage Account
   az storage account create \
     --name aiportfolioanalysiscerts \
     --resource-group your-resource-group \
     --location "South Central US" \
     --sku Standard_LRS

   # Create file shares
   az storage share create \
     --name ssl-certs \
     --account-name aiportfolioanalysiscerts

   az storage share create \
     --name ssl-private \
     --account-name aiportfolioanalysiscerts

   # Upload certificates
   az storage file upload \
     --share-name ssl-certs \
     --source /etc/letsencrypt/live/your-domain/fullchain.pem \
     --path aiportfolioanalysis.crt \
     --account-name aiportfolioanalysiscerts

   az storage file upload \
     --share-name ssl-private \
     --source /etc/letsencrypt/live/your-domain/privkey.pem \
     --path aiportfolioanalysis.key \
     --account-name aiportfolioanalysiscerts
   ```

### Option 2: Azure Key Vault Integration (Advanced)

1. **Store certificates in Azure Key Vault**
2. **Use Azure Container Groups with Key Vault integration**
3. **Mount certificates as secrets**

## Deployment Steps

1. **Build and Push Container Images**:
   ```bash
   # Build application container
   docker build -t aiportfolioanalysis:latest .

   # Build NGINX container
   docker build -f Dockerfile.nginx -t aiportfolioanalysis-nginx:latest .

   # Push to Azure Container Registry
   az acr login --name your-registry
   docker tag aiportfolioanalysis:latest your-registry.azurecr.io/aiportfolioanalysis:latest
   docker tag aiportfolioanalysis-nginx:latest your-registry.azurecr.io/aiportfolioanalysis-nginx:latest
   docker push your-registry.azurecr.io/aiportfolioanalysis:latest
   docker push your-registry.azurecr.io/aiportfolioanalysis-nginx:latest
   ```

2. **Deploy Azure Container Group**:
   ```bash
   # Update azure-container-group.json with your values
   az deployment group create \
     --resource-group your-resource-group \
     --template-file azure-container-group.json \
     --parameters \
       googleClientId="your-google-client-id" \
       googleClientSecret="your-google-client-secret" \
       storageAccountName="aiportfolioanalysiscerts" \
       storageAccountKey="your-storage-account-key"
   ```

## Certificate Renewal

Let's Encrypt certificates expire every 90 days. To renew:

1. **Generate new certificate locally**:
   ```bash
   sudo certbot renew --manual --preferred-challenges dns
   ```

2. **Upload new certificate to Azure File Share**:
   ```bash
   az storage file upload \
     --share-name ssl-certs \
     --source /etc/letsencrypt/live/your-domain/fullchain.pem \
     --path aiportfolioanalysis.crt \
     --account-name aiportfolioanalysiscerts \
     --overwrite
   ```

3. **Restart container group**:
   ```bash
   az container restart \
     --resource-group your-resource-group \
     --name aiportfolioanalysis-https
   ```

## Monitoring and Troubleshooting

1. **Check container logs**:
   ```bash
   az container logs \
     --resource-group your-resource-group \
     --name aiportfolioanalysis-https \
     --container-name nginx-proxy
   ```

2. **Test SSL configuration**:
   ```bash
   # Test SSL certificate
   openssl s_client -connect aiportfolioanalysis.southcentralus.azurecontainer.io:443 -servername aiportfolioanalysis.southcentralus.azurecontainer.io

   # Test HTTP redirect
   curl -I http://aiportfolioanalysis.southcentralus.azurecontainer.io
   ```

3. **Common issues**:
   - Certificate not found: Check Azure File Share mounts
   - SSL handshake failures: Verify certificate chain and private key
   - OAuth redirect errors: Update Google OAuth settings with HTTPS URLs

## Security Considerations

- Certificates are stored in Azure File Share with restricted access
- Private keys are mounted with read-only permissions
- NGINX configuration includes security headers
- Rate limiting is configured for API endpoints
- HTTP traffic is redirected to HTTPS

## Cost Optimization

- Use Azure Container Groups instead of AKS for lower costs
- Store certificates in Azure File Share instead of Premium storage
- Configure appropriate resource limits for containers
- Consider using Azure Front Door for CDN and DDoS protection in production