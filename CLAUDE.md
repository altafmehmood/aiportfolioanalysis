# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Architecture

This is a full-stack web application built with ASP.NET Core 9.0 backend and Angular 20 frontend. The application implements Google OAuth authentication and serves as an AI Portfolio Analysis dashboard.

**Backend (ASP.NET Core 9.0):**
- `AiPortfolioAnalysis.Web/Program.cs` - Main application configuration with OAuth, CORS, and SPA integration
- Authentication using Google OAuth with cookie-based sessions
- Weather API endpoint for demo purposes
- Built-in SPA static file serving and development proxy

**Frontend (Angular 20):**
- `AiPortfolioAnalysis.Web/ClientApp/` - Angular application with standalone components
- Uses Angular's new standalone component architecture (no NgModule)
- Services for authentication (`auth.ts`) and weather data (`weather.ts`)
- Components: `LoginComponent`, `DashboardComponent`
- Routing configured in `app.routes.ts`

## Development Commands

**Prerequisites:**
- .NET 9 SDK
- Node.js and npm
- Google OAuth credentials configured (see GOOGLE_OAUTH_SETUP.md)

**Backend (.NET):**
```bash
# From project root
cd AiPortfolioAnalysis.Web
dotnet run                    # Start backend on https://localhost:5001
dotnet build                  # Build the application
dotnet test                   # Run tests (if any)
```

**Frontend (Angular):**
```bash
# From ClientApp directory
cd AiPortfolioAnalysis.Web/ClientApp
npm install                   # Install dependencies
ng serve                      # Start dev server on http://localhost:4200
ng build                      # Build for production
ng test                       # Run unit tests with Karma
ng generate component name    # Generate new component
```

**Full Development Setup:**
1. Configure Google OAuth credentials using user secrets:
   ```bash
   cd AiPortfolioAnalysis.Web
   dotnet user-secrets set "Authentication:Google:ClientId" "your-client-id"
   dotnet user-secrets set "Authentication:Google:ClientSecret" "your-client-secret"
   ```
2. Start backend: `dotnet run` (from AiPortfolioAnalysis.Web/)
3. Start frontend: `ng serve` (from AiPortfolioAnalysis.Web/ClientApp/)
4. Access application at http://localhost:4200

## Key Configuration

**OAuth Configuration:**
- Google OAuth credentials stored in user secrets for development
- Production requires `GOOGLE_CLIENTID` and `GOOGLE_CLIENTSECRET` environment variables
- Frontend URL configured via `Frontend:BaseUrl` in appsettings.json

**Environment Configuration:**
- Development: Frontend runs on localhost:4200, backend on localhost:5001
- Production: Configured for Azure Container Instances with specific URLs
- CORS policies differ between development and production

**Authentication Flow:**
- Users authenticate via `/api/auth/login` → Google OAuth → `/api/auth/callback`
- Session managed with HTTP-only cookies
- Frontend receives user data via query parameters after successful OAuth

## Testing

**Backend:**
- No specific test framework configured yet
- Use standard .NET testing practices with xUnit or NUnit

**Frontend:**
- Jasmine/Karma configured for unit testing
- Test files: `*.spec.ts`
- Run tests: `ng test`

## Deployment

Application is configured for containerized deployment:
- `Dockerfile` in root directory
- Frontend builds integrated into .NET publish process
- Production configuration in `appsettings.json` points to Azure Container Instances

## Architecture Notes

- **Standalone Components:** Angular app uses Angular 20's standalone component architecture
- **State Management:** Simple BehaviorSubject-based state in AuthService
- **API Communication:** HttpClient with credentials for authenticated requests
- **Security:** HTTP-only cookies, CORS configuration, secure cookie policies
- **SPA Integration:** .NET serves Angular app and provides proxy for development