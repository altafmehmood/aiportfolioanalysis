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

### 3. For Production

Use secure configuration management:
- **Azure**: Azure Key Vault
- **AWS**: AWS Secrets Manager  
- **Docker**: Environment variables
- **Kubernetes**: Kubernetes Secrets

Example for environment variables in production:
```bash
Authentication__Google__ClientId=YOUR_PRODUCTION_CLIENT_ID
Authentication__Google__ClientSecret=YOUR_PRODUCTION_CLIENT_SECRET
```

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