# Automatic HTTPS Setup with Caddy for Azure Container Groups

This document describes how to set up automatic HTTPS using Caddy for the AI Portfolio Analysis application running on Azure Container Groups.

## Prerequisites

- Azure CLI installed and configured
- Azure subscription with Container Instance permissions
- Azure Storage Account for Caddy data persistence
- Domain name pointing to your Azure Container Instance (Azure auto-generated domain works!)

## Automatic HTTPS with Caddy

### Why Caddy?

Caddy automatically provisions and renews Let's Encrypt certificates with **zero configuration**. No manual certificate generation, no renewal scripts, no DNS challenges - it all happens automatically!

### Key Benefits:
- ✅ **Automatic certificate provisioning** from Let's Encrypt
- ✅ **Automatic renewal** (happens in background)
- ✅ **Works with Azure Container Instances** auto-generated domains
- ✅ **No manual certificate management** required
- ✅ **HTTP/2 and modern TLS** by default
- ✅ **Built-in security headers** and rate limiting

### How It Works:

1. **First Request**: Caddy automatically requests a certificate from Let's Encrypt
2. **HTTP Challenge**: Caddy handles the ACME challenge automatically
3. **Certificate Storage**: Certificates are stored in Azure File Share
4. **Automatic Renewal**: Caddy renews certificates before expiration

## Deployment Steps

### 1. **Create Azure Storage Account for Caddy Data**:
   ```bash
   # Create Azure Storage Account
   az storage account create \
     --name aiportfolioanalysiscerts \
     --resource-group your-resource-group \
     --location "South Central US" \
     --sku Standard_LRS

   # Create file shares for Caddy data
   az storage share create \
     --name caddy-data \
     --account-name aiportfolioanalysiscerts

   az storage share create \
     --name caddy-config \
     --account-name aiportfolioanalysiscerts
   ```

### 2. **Build and Push Container Images**:
   ```bash
   # Build application container
   docker build -t aiportfolioanalysis:latest .

   # Build Caddy container
   docker build -f Dockerfile.caddy -t aiportfolioanalysis-caddy:latest .

   # Push to Azure Container Registry
   az acr login --name your-registry
   docker tag aiportfolioanalysis:latest your-registry.azurecr.io/aiportfolioanalysis:latest
   docker tag aiportfolioanalysis-caddy:latest your-registry.azurecr.io/aiportfolioanalysis-caddy:latest
   docker push your-registry.azurecr.io/aiportfolioanalysis:latest
   docker push your-registry.azurecr.io/aiportfolioanalysis-caddy:latest
   ```

### 3. **Deploy Azure Container Group**:
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

### 4. **That's It! 🎉**

After deployment:
1. **First HTTPS Request**: Caddy automatically requests a certificate from Let's Encrypt
2. **Certificate Storage**: Certificate is stored in Azure File Share
3. **Automatic Renewal**: Caddy handles renewal automatically (no manual intervention needed)

## Certificate Management

### Automatic Renewal

✅ **No Action Required** - Caddy automatically renews certificates before expiration!

### Monitoring

Check certificate status:
```bash
# View Caddy logs
az container logs \
  --resource-group your-resource-group \
  --name aiportfolioanalysis-https \
  --container-name caddy-proxy

# Check certificate expiration via Caddy admin API
curl https://aiportfolioanalysis.southcentralus.azurecontainer.io:2019/config/apps/tls/certificates
```

## Monitoring and Troubleshooting

1. **Check container logs**:
   ```bash
   az container logs \
     --resource-group your-resource-group \
     --name aiportfolioanalysis-https \
     --container-name caddy-proxy
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
- Caddy configuration includes security headers
- Rate limiting is configured for API endpoints
- HTTP traffic is redirected to HTTPS

## Cost Optimization

- Use Azure Container Groups instead of AKS for lower costs
- Store certificates in Azure File Share instead of Premium storage
- Configure appropriate resource limits for containers
- Consider using Azure Front Door for CDN and DDoS protection in production