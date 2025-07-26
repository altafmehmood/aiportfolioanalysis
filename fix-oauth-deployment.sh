#!/bin/bash

# Helper script to fix Google OAuth deployment issue
# This script helps set up environment variables and redeploy

set -e

echo "🔧 Google OAuth Deployment Fix Script"
echo "======================================"

# Check if Google OAuth credentials are already set
if [ -z "$GOOGLE_CLIENTID" ] || [ -z "$GOOGLE_CLIENTSECRET" ]; then
    echo "❌ Google OAuth credentials not found in environment variables"
    echo ""
    echo "Please set your Google OAuth credentials:"
    echo "1. Get your Client ID and Secret from Google Cloud Console"
    echo "2. Set them as environment variables:"
    echo ""
    echo "   export GOOGLE_CLIENTID=\"your-actual-client-id.googleusercontent.com\""
    echo "   export GOOGLE_CLIENTSECRET=\"your-actual-client-secret\""
    echo ""
    echo "3. Then run this script again"
    echo ""
    echo "📖 For detailed instructions, see GOOGLE_OAUTH_SETUP.md"
    exit 1
fi

echo "✅ Google OAuth credentials found:"
echo "   GOOGLE_CLIENTID: ${GOOGLE_CLIENTID:0:20}..."
echo "   GOOGLE_CLIENTSECRET: ${GOOGLE_CLIENTSECRET:0:10}..."
echo ""

# Verify Azure CLI is logged in
if ! az account show &>/dev/null; then
    echo "❌ Please log in to Azure CLI first:"
    echo "   az login"
    exit 1
fi

echo "✅ Azure CLI is logged in"
echo ""

# Check if we're in the right directory
if [ ! -f "deploy-aci.sh" ]; then
    echo "❌ deploy-aci.sh not found. Please run this script from the project root directory."
    exit 1
fi

echo "✅ Found deployment script"
echo ""

# Set deployment environment to test (since that's what's currently running)
export DEPLOYMENT_ENVIRONMENT="test"

echo "🚀 Starting redeployment with OAuth credentials..."
echo "   Environment: $DEPLOYMENT_ENVIRONMENT"
echo "   Target: aiportfolioanalysis-test"
echo ""

# Run the deployment
./deploy-aci.sh

echo ""
echo "✅ Deployment completed!"
echo ""
echo "🧪 Verification steps:"
echo "1. Wait ~2 minutes for container to fully start"
echo "2. Visit: https://aiportfolioanalysis-test.southcentralus.azurecontainer.io/api/auth/login"
echo "3. Should redirect to Google OAuth (no HTTP or port 8080)"
echo "4. Complete OAuth flow and verify authentication works"
echo ""
echo "📊 Check deployment status:"
echo "   az container show --resource-group aiportfolioanalysis --name aiportfolioanalysis-test --query provisioningState"
echo ""
echo "📋 View logs:"
echo "   az container logs --resource-group aiportfolioanalysis --name aiportfolioanalysis-test --container-name aspnet-backend" 