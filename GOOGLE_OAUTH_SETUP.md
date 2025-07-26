# Google OAuth Setup Instructions

## Prerequisites

1. A Google Cloud Platform account
2. .NET 9 SDK installed
3. Node.js and npm installed

## Google Cloud Console Setup

### 1. Create a Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select an existing one
3. Note down your project ID

### 2. Enable Google+ API

1. In the Google Cloud Console, go to "APIs & Services" > "Library"
2. Search for "Google+ API" 
3. Click on it and enable the API

### 3. Create OAuth 2.0 Credentials

1. Go to "APIs & Services" > "Credentials"
2. Click "Create Credentials" > "OAuth 2.0 Client IDs"
3. If prompted, configure the OAuth consent screen:
   - Choose "External" user type
   - Fill in the required information (App name, User support email, etc.)
   - Add your email to test users if in testing mode
4. For Application type, select "Web application"
5. Add the following to "Authorized redirect URIs":
   ```
   http://localhost:5006/signin-google
   https://aiportfolioanalysis.southcentralus.azurecontainer.io/signin-google
   ```
6. Click "Create"
7. Copy the Client ID and Client Secret

## Application Configuration

### 1. Configure User Secrets (Recommended for Development)

The project is already configured with user secrets. Set your Google OAuth credentials securely:

```bash
# Navigate to the project directory
cd AiPortfolioAnalysis.Web

# Set your Google OAuth credentials (replace with your actual values)
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_ACTUAL_GOOGLE_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_ACTUAL_GOOGLE_CLIENT_SECRET"

# Verify the secrets were set correctly
dotnet user-secrets list
```

**Benefits of User Secrets:**
- ✅ Credentials never committed to git
- ✅ Stored securely on your local machine
- ✅ Automatically loaded by .NET in Development environment
- ✅ Shared safely across team members without exposing sensitive data

### 2. Alternative: Environment Variables

You can also use environment variables instead of user secrets:

```bash
export Authentication__Google__ClientId="YOUR_ACTUAL_GOOGLE_CLIENT_ID"
export Authentication__Google__ClientSecret="YOUR_ACTUAL_GOOGLE_CLIENT_SECRET"
```

### 3. For Production (Azure Container Instances with Caddy)

**Important:** With Caddy as reverse proxy, update your Google OAuth configuration:

#### Google Cloud Console Updates:
1. **Authorized JavaScript Origins:**
   ```
   https://aiportfolioanalysis.southcentralus.azurecontainer.io
   ```

2. **Authorized Redirect URIs:**
   ```
   https://aiportfolioanalysis.southcentralus.azurecontainer.io/signin-google
   ```

   **⚠️ CRITICAL: Make sure you have ONLY the HTTPS redirect URI configured in Google Cloud Console. Remove any HTTP or port-specific redirect URIs like:**
   - ❌ `http://aiportfolioanalysis.southcentralus.azurecontainer.io:8080/signin-google`
   - ❌ `http://aiportfolioanalysis.southcentralus.azurecontainer.io:8080/api/auth/login`
   - ❌ Any URLs with port numbers or HTTP protocol

   **✅ The correct redirect URI should be:**
   - `https://aiportfolioanalysis.southcentralus.azurecontainer.io/signin-google`

#### Azure Container Instance Environment Variables:
```bash
GOOGLE_CLIENTID=YOUR_PRODUCTION_CLIENT_ID
GOOGLE_CLIENTSECRET=YOUR_PRODUCTION_CLIENT_SECRET
```

**Note:** Caddy will automatically provision HTTPS certificates via Let's Encrypt.

## Running the Application

1. **Start the .NET API:**
   ```bash
   cd AiPortfolioAnalysis.Web
   dotnet run
   ```

2. **Start the Angular app (in a separate terminal):**
   ```bash
   cd AiPortfolioAnalysis.Web/ClientApp
   npm start
   ```

3. **Navigate to:** `http://localhost:4200`

## Authentication Flow

1. User clicks "Sign in with Google" on the login page
2. User is redirected to Google's OAuth consent screen
3. After successful authentication, user is redirected back to the dashboard
4. The application stores the user session using cookies
5. Subsequent API calls include authentication information

## Security Notes

- Never commit actual Google OAuth credentials to version control
- Use environment variables or secure configuration for production
- Ensure HTTPS is enabled in production
- Review and configure the OAuth consent screen appropriately
- Consider implementing proper session timeout and security headers

## Troubleshooting OAuth Redirect Issues

### Common Problem: Wrong Redirect URL Format

**Symptom:** Google redirects to URLs like:
- `http://aiportfolioanalysis.southcentralus.azurecontainer.io:8080/api/auth/login`
- URLs with HTTP instead of HTTPS
- URLs with port numbers exposed

**Solution:**
1. **Check Google Cloud Console:**
   - Go to [Google Cloud Console](https://console.cloud.google.com/)
   - Navigate to "APIs & Services" > "Credentials"
   - Edit your OAuth 2.0 Client ID
   - In "Authorized redirect URIs", ensure you have ONLY:
     ```
     https://aiportfolioanalysis.southcentralus.azurecontainer.io/signin-google
     ```
   - Remove any HTTP or port-specific URIs

2. **Verify Forwarded Headers:**
   Check application logs to ensure forwarded headers are working:
   ```bash
   # Check container logs
   az container logs --resource-group <your-resource-group> --name <container-group-name>
   ```
   
   Look for log entries showing the forwarded headers configuration.

3. **Test HTTPS Access:**
   ```bash
   curl -I https://aiportfolioanalysis.southcentralus.azurecontainer.io/api/auth/login
   ```
   Should return a redirect to Google OAuth, not an error.

### Environment Variables Verification

Ensure these environment variables are set correctly in production:
```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
Frontend__BaseUrl=https://aiportfolioanalysis.southcentralus.azurecontainer.io
```

## Manual Deployment Configuration

### ⚠️ **Critical: Set OAuth Environment Variables**

Before running any manual deployment, you MUST set your Google OAuth credentials:

```bash
# Set your Google OAuth credentials (replace with your actual values)
export GOOGLE_CLIENTID="your-actual-google-client-id.googleusercontent.com"
export GOOGLE_CLIENTSECRET="your-actual-google-client-secret"

# Verify they're set correctly
echo "GOOGLE_CLIENTID: ${GOOGLE_CLIENTID}"
echo "GOOGLE_CLIENTSECRET: ${GOOGLE_CLIENTSECRET}"
```

**⚠️ Important:** 
- Never commit these values to git
- Use your actual Google OAuth Client ID and Secret from Google Cloud Console
- These environment variables are required for the deployment script to work

### Deployment Commands

```bash
# Set OAuth credentials first
export GOOGLE_CLIENTID="your-client-id"
export GOOGLE_CLIENTSECRET="your-client-secret"

# Run deployment
./deploy-aci.sh
```

### Alternative: Use .env File (NOT committed to git)

Create a `.env` file in your project root (add to .gitignore):

```bash
# .env file (DO NOT COMMIT TO GIT)
GOOGLE_CLIENTID=your-actual-client-id
GOOGLE_CLIENTSECRET=your-actual-client-secret
```

Then source it before deployment:
```bash
source .env
./deploy-aci.sh
```

## Manual Verification Checklist

### ✅ **Step 1: Check Your Current Deployment**
Your current deployment FQDN is: `aiportfolioanalysis-test.southcentralus.azurecontainer.io`

**Important:** Make sure your Google Cloud Console is configured for the correct domain!

### ✅ **Step 2: Update Google Cloud Console (If Needed)**
Since your actual FQDN is `aiportfolioanalysis-test.southcentralus.azurecontainer.io`, update your Google OAuth settings:

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Navigate to "APIs & Services" > "Credentials"
3. Edit your OAuth 2.0 Client ID
4. **Authorized JavaScript Origins:**
   ```
   https://aiportfolioanalysis-test.southcentralus.azurecontainer.io
   ```
5. **Authorized Redirect URIs:**
   ```
   https://aiportfolioanalysis-test.southcentralus.azurecontainer.io/signin-google
   ```

### ✅ **Step 3: Test the OAuth Flow**
1. Open your browser and navigate to:
   ```
   https://aiportfolioanalysis-test.southcentralus.azurecontainer.io/api/auth/login
   ```

2. **Expected Behavior:**
   - You should be redirected to Google's OAuth consent screen
   - The URL should contain `accounts.google.com` 
   - After authentication, you should be redirected back to your app

3. **Check the Network Tab:**
   - Open Developer Tools (F12)
   - Go to Network tab
   - Visit the login URL
   - Look for a 302 redirect to `accounts.google.com`

### ✅ **Step 4: Verify the Redirect URI**
When you're on Google's OAuth page, check the URL parameters:
- Look for `redirect_uri` parameter
- It should be: `https://aiportfolioanalysis-test.southcentralus.azurecontainer.io/signin-google`
- ❌ If you see HTTP or port 8080, your Google config needs updating

### ✅ **Step 5: Test the Complete Flow**
1. Complete the Google OAuth login
2. Verify you're redirected back to your dashboard
3. Check that you're properly authenticated

## Troubleshooting

### Common Issues:

1. **"OAuth Error: invalid_client"**
   - Check that your Client ID and Client Secret are correct
   - Verify the redirect URI matches exactly what's configured in Google Cloud Console

2. **"OAuth Error: redirect_uri_mismatch"**
   - Ensure the redirect URI in Google Cloud Console matches: `http://localhost:5006/signin-google`

3. **CORS Issues**
   - Verify CORS is properly configured in Program.cs
   - Check that the Angular app URL matches the CORS policy

4. **User not redirected after login**
   - Check browser developer tools for any JavaScript errors
   - Verify the callback endpoint is working correctly