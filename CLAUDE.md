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
1. **Clone and setup repository:**
   ```bash
   git clone <repository-url>
   cd aiportfolioanalysis
   ```

2. **Configure Google OAuth credentials using user secrets:**
   ```bash
   cd AiPortfolioAnalysis.Web
   dotnet user-secrets set "Authentication:Google:ClientId" "your-client-id"
   dotnet user-secrets set "Authentication:Google:ClientSecret" "your-client-secret"
   ```

3. **Install dependencies:**
   ```bash
   # Backend dependencies (auto-restored on build)
   dotnet restore
   
   # Frontend dependencies
   cd ClientApp
   npm install
   cd ..
   ```

4. **Development servers:**
   ```bash
   # Terminal 1: Backend (from AiPortfolioAnalysis.Web/)
   dotnet run
   
   # Terminal 2: Frontend (from AiPortfolioAnalysis.Web/ClientApp/)
   ng serve
   ```

5. **Access application:**
   - Frontend: http://localhost:4200
   - Backend API: https://localhost:5001
   - Swagger (if enabled): https://localhost:5001/swagger

**Development Workflow:**
- Backend changes: Auto-reload with `dotnet watch run`
- Frontend changes: Auto-reload with `ng serve`
- Full build test: `dotnet build` (includes frontend build)
- Production build: `dotnet publish`

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

## Testing Strategy

### Backend Testing (.NET)
**Framework:** Currently not configured - Recommended: xUnit + Moq
```bash
# Add test project (if not exists)
dotnet new xunit -n AiPortfolioAnalysis.Tests
dotnet add AiPortfolioAnalysis.Tests reference AiPortfolioAnalysis.Web

# Run tests
dotnet test                           # All tests
dotnet test --logger trx             # With test results
dotnet test --collect:"XPlat Code Coverage"  # With coverage
```

**Test Categories:**
- **Unit Tests:** Services, utilities, business logic
- **Integration Tests:** API endpoints, database operations
- **Authentication Tests:** OAuth flow, session management

### Frontend Testing (Angular)
**Frameworks:** Jasmine + Karma (unit), Cypress/Playwright (e2e)
```bash
# From ClientApp directory
ng test                              # Unit tests (watch mode)
ng test --watch=false               # Single run
ng test --code-coverage             # With coverage report
ng e2e                              # End-to-end tests (if configured)
```

**Test Files:**
- Unit tests: `*.spec.ts`
- E2E tests: `cypress/e2e/*.cy.ts` or `e2e/*.spec.ts`
- Coverage reports: `coverage/` directory

### Testing Best Practices
**Test Structure:**
- Follow AAA pattern (Arrange, Act, Assert)
- Use descriptive test names
- Mock external dependencies
- Test edge cases and error conditions

**Pre-commit Testing:**
```bash
# Full test suite before committing
dotnet build                        # Ensure compilation
dotnet test                         # Backend tests
cd ClientApp && ng test --watch=false && ng lint  # Frontend tests & linting
```

**CI/CD Testing:**
- All tests must pass before merge
- Maintain minimum code coverage thresholds
- Include smoke tests for critical paths
- Test OAuth integration in staging environment

## Deployment & DevOps

### Container Deployment
**Docker Configuration:**
- `Dockerfile` in root directory
- Multi-stage build: Node.js for frontend → .NET runtime for backend
- Frontend builds integrated into .NET publish process

**Build Process:**
```bash
# Local container build
docker build -t aiportfolioanalysis .
docker run -p 8080:8080 aiportfolioanalysis

# Production build
dotnet publish -c Release
```

### Environment Configuration
**Development:**
- Frontend: http://localhost:4200
- Backend: https://localhost:5001
- OAuth redirect: localhost URLs

**Production (Azure Container Instances):**
- Application URL configured in `appsettings.json`
- Environment variables: `GOOGLE_CLIENTID`, `GOOGLE_CLIENTSECRET`
- HTTPS termination at container level
- Health checks configured for container orchestration

### CI/CD Pipeline
**Pipeline stages:**
1. **Build:** Compile backend + frontend
2. **Test:** Run unit tests + linting
3. **Security:** Dependency scanning, secrets detection
4. **Package:** Build container image
5. **Deploy:** Push to container registry → Deploy to Azure

**Deployment Requirements:**
- All tests passing
- Security scans clean
- Code review approved
- Feature flags configured (if applicable)

### Monitoring & Observability
- Application logs via .NET logging providers
- Container health checks
- Performance monitoring (configure Application Insights)
- Error tracking and alerting

## Architecture Notes

- **Standalone Components:** Angular app uses Angular 20's standalone component architecture
- **State Management:** Simple BehaviorSubject-based state in AuthService
- **API Communication:** HttpClient with credentials for authenticated requests
- **Security:** HTTP-only cookies, CORS configuration, secure cookie policies
- **SPA Integration:** .NET serves Angular app and provides proxy for development

## Security & Code Quality

### Security Best Practices
**Authentication & Authorization:**
- Google OAuth with HTTP-only cookies
- Secure cookie configuration (SameSite, Secure flags)
- Session timeout and refresh mechanisms
- CORS policies for cross-origin requests

**Data Protection:**
- Never log sensitive user data
- Use HTTPS in production (TLS 1.2+)
- Sanitize all user inputs
- Implement proper error handling (don't expose stack traces)

**Secrets Management:**
- Development: .NET User Secrets
- Production: Environment variables or Azure Key Vault
- Never commit secrets to repository
- Rotate secrets regularly

### Code Quality Standards
**Backend (.NET):**
```bash
dotnet format                        # Code formatting
dotnet build --warningsaserrors     # Treat warnings as errors
dotnet pack --configuration Release # Package validation
```

**Frontend (Angular):**
```bash
ng lint                             # ESLint + Angular-specific rules
ng lint --fix                      # Auto-fix linting issues
npm audit                          # Security vulnerability scan
npm audit fix                      # Auto-fix vulnerabilities
```

**Code Review Checklist:**
- [ ] No hardcoded secrets or sensitive data
- [ ] Proper error handling and logging
- [ ] Input validation and sanitization
- [ ] Unit tests for new functionality
- [ ] Following established patterns and conventions
- [ ] Performance considerations addressed
- [ ] Security implications reviewed

### Development Tools
**Recommended IDE Extensions:**
- C# Dev Kit (VS Code)
- Angular Language Service
- ESLint
- Prettier
- SonarLint

**Pre-commit Hooks (Optional):**
- Code formatting
- Lint checks
- Unit test execution
- Security scanning

## Git Workflow & Best Practices
- Never commit to main branch directly. Always use feature, hotfix or release branches.

### Branch Management
- **Main branch:** `main` - Production-ready code only
- **Feature branches:** Create from `main` using descriptive names
  - Format: `feature/description-of-feature`
  - Examples: `feature/oauth-integration`, `feature/user-dashboard`
- **Hotfix branches:** `hotfix/description-of-fix`
- **Release branches:** `release/version-number` (if needed)

### Daily Workflow
1. **Start of day:** Always sync with remote
   ```bash
   git checkout main
   git pull origin main
   ```

2. **Creating feature branch:**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Regular commits:** Make small, logical commits
   ```bash
   git add .
   git commit -m "feat: add user authentication middleware"
   git push origin feature/your-feature-name
   ```

4. **Before pushing:** Ensure code quality
   ```bash
   dotnet build                    # Backend build
   cd ClientApp && ng build        # Frontend build
   dotnet test                     # Run tests if available
   ```

### Commit Message Conventions
Follow [Conventional Commits](https://www.conventionalcommits.org/) specification:

- `feat:` New features
- `fix:` Bug fixes
- `docs:` Documentation changes
- `style:` Code style changes (formatting, etc.)
- `refactor:` Code refactoring
- `test:` Adding or updating tests
- `chore:` Maintenance tasks, dependency updates
- `ci:` CI/CD pipeline changes
- `perf:` Performance improvements
- `security:` Security-related changes

**Examples:**
```
feat: implement Google OAuth authentication
fix: resolve CORS policy issues in production
docs: update API documentation for auth endpoints
security: implement HTTP-only cookie sessions
```

### Pull Request Process
1. **Create PR:** From feature branch to `main`
2. **PR Title:** Use conventional commit format
3. **PR Description:** Include:
   - Summary of changes
   - Testing performed
   - Breaking changes (if any)
   - Related issues/tickets

4. **Before merging:**
   - All CI checks pass
   - Code review approved
   - No merge conflicts
   - Feature tested in development environment

### Code Review Guidelines
- **Review checklist:**
  - [ ] Code follows project conventions
  - [ ] No secrets or sensitive data exposed
  - [ ] Proper error handling implemented
  - [ ] Tests added for new functionality
  - [ ] Documentation updated if needed
  - [ ] Performance considerations addressed

### Merge Strategy
- **Squash and merge:** For feature branches (keeps main history clean)
- **Regular merge:** For hotfixes and small direct commits
- **Never force push** to `main` branch

### Emergency Procedures
**Hotfix process:**
1. Create hotfix branch from `main`
2. Make minimal necessary changes
3. Test thoroughly
4. Create PR with `hotfix:` prefix
5. Fast-track review and merge
6. Deploy immediately
7. Backport to development branches if needed

### Security Practices
- **Never commit:**
  - API keys, passwords, tokens
  - User secrets or sensitive configuration
  - Large binary files or build artifacts
  - Personal development configurations

- **Use .gitignore effectively:**
  - IDE-specific files
  - Build outputs
  - Temporary files
  - Environment-specific configs

### Git Hooks (Recommended)
Consider setting up pre-commit hooks for:
- Code formatting (`dotnet format`, `ng lint`)
- Build verification
- Test execution
- Commit message validation

