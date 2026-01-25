# Phase 1: Skeleton Deployment - Research

**Researched:** 2026-01-25
**Domain:** Azure deployment, Docker containerization, CI/CD automation
**Confidence:** HIGH

## Summary

Phase 1 requires deploying empty React (frontend) and .NET (backend) applications to Azure with working secrets management, CI/CD pipelines, and local Docker-based development. The standard approach leverages Azure App Service for both frontend and backend, Azure Key Vault with Managed Identity for secrets, GitHub Actions with OpenID Connect for secure CI/CD, and Docker Compose for local development with hot reload.

The research confirms that Azure App Service is the appropriate choice for both frontend and backend (rather than Static Web Apps) given the requirement for Azure Front Door integration, custom domain setup, and unified deployment patterns. GitHub Actions workflows should use OpenID Connect (OIDC) authentication rather than service principal secrets for enhanced security. Docker Compose supports efficient local development with hot reload for both React (Vite) and .NET applications.

**Primary recommendation:** Use Azure App Service for both frontend and backend with separate App Service Plans in the same resource group. Implement GitHub Actions with OIDC authentication, deploy to staging slots first, configure Azure Key Vault with system-assigned managed identities, and use Docker Compose with volume mounts for local hot-reload development.

## Standard Stack

### Core

| Library/Service | Version | Purpose | Why Standard |
|-----------------|---------|---------|--------------|
| Azure App Service | Latest | Host both React and .NET apps | Fully managed PaaS with built-in CI/CD, deployment slots, scaling, and monitoring |
| Azure Key Vault | Latest | Centralized secrets management | Industry standard for Azure secret storage with RBAC and audit logging |
| Azure Front Door | Standard/Premium | Global load balancer, CDN, WAF | Required for custom domain, SSL termination, and global distribution |
| GitHub Actions | Latest | CI/CD automation | Native GitHub integration, free for public repos, Azure-native actions available |
| Docker Compose | v2+ | Local development orchestration | Standard tool for multi-container local development with hot reload support |
| Vite | 5.x+ | React build tool | Modern, fast build tool replacing Create React App, optimized for development and production |
| .NET | 9.0 | Backend runtime | Latest LTS version with improved performance and container support |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| azure/login | v2 | GitHub Actions Azure auth | OIDC authentication in workflows |
| azure/webapps-deploy | v3 | GitHub Actions deployment | Deploy to App Service from workflows |
| Azure CLI | Latest | Infrastructure management | Manual setup, troubleshooting, local testing |
| Node.js | 24.x | Frontend build environment | Build React applications in CI/CD and containers |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| App Service (frontend) | Azure Static Web Apps | Static Web Apps are cheaper ($0-9/month vs $13+/month) but don't support Azure Front Door Premium integration with Private Link, limited to Azure Functions for backend API |
| OIDC Authentication | Service Principal with secrets | Service principals require secret rotation every 1-2 years; OIDC provides passwordless, short-lived tokens with no rotation |
| System-assigned identity | User-assigned identity | User-assigned identities survive resource deletion and can be shared, but add complexity for single-app scenarios |
| Deployment slots | Direct to production | Slots enable zero-downtime deployments and warm-up before swap; skip only for non-critical apps |

**Installation:**
```bash
# Azure CLI (for infrastructure setup)
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# GitHub CLI (for creating repository secrets)
gh auth login
```

## Architecture Patterns

### Recommended Project Structure

```
ConflictedLineup/
├── frontend/
│   ├── src/
│   ├── public/
│   ├── Dockerfile
│   ├── Dockerfile.dev
│   ├── package.json
│   ├── vite.config.ts
│   └── .env.example
├── backend/
│   ├── src/
│   │   └── ConflictedLineup.Api/
│   │       ├── Program.cs
│   │       ├── Controllers/
│   │       │   └── HealthController.cs
│   │       └── ConflictedLineup.Api.csproj
│   ├── Dockerfile
│   ├── Dockerfile.dev
│   └── .env.example
├── docker-compose.yml
├── docker-compose.dev.yml
├── .github/
│   └── workflows/
│       ├── frontend-deploy.yml
│       └── backend-deploy.yml
├── .env.local (gitignored - local secrets)
└── README.md
```

### Pattern 1: Multi-Stage Docker Builds

**What:** Separate build and runtime stages to minimize final image size and exclude development dependencies
**When to use:** All production Dockerfiles for React and .NET

**Example (React + Vite):**
```dockerfile
# Build stage
FROM node:24-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

# Production stage
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

**Example (.NET 9):**
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ConflictedLineup.Api.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ConflictedLineup.Api.dll"]
```

### Pattern 2: Development Docker Compose with Hot Reload

**What:** Volume mounts and polling for live code changes without container rebuilds
**When to use:** Local development only (docker-compose.dev.yml)

**Example:**
```yaml
# docker-compose.dev.yml
services:
  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile.dev
    volumes:
      - ./frontend:/app
      - /app/node_modules  # Prevent host node_modules from overriding
    environment:
      - CHOKIDAR_USEPOLLING=true
      - VITE_API_URL=http://localhost:8080
    ports:
      - "5173:5173"

  backend:
    build:
      context: ./backend
      dockerfile: Dockerfile.dev
    volumes:
      - ./backend:/app
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - DOTNET_USE_POLLING_FILE_WATCHER=true
    ports:
      - "8080:8080"
```

**Vite config for Docker hot reload:**
```typescript
// vite.config.ts
export default defineConfig({
  server: {
    host: '0.0.0.0',
    port: 5173,
    watch: {
      usePolling: true
    },
    hmr: {
      clientPort: 5173
    }
  }
})
```

### Pattern 3: GitHub Actions Job Dependencies for Test Gating

**What:** Use `needs` keyword to enforce sequential execution where deployment only runs if tests pass
**When to use:** All deployment workflows

**Example:**
```yaml
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '24.x'
      - run: npm ci
      - run: npm test
        env:
          CI: true

  deploy:
    needs: test  # Blocks deployment if tests fail
    runs-on: ubuntu-latest
    permissions:
      id-token: write
      contents: read
    steps:
      - uses: actions/checkout@v4
      - uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
      - name: Build
        run: npm ci && npm run build
      - uses: azure/webapps-deploy@v3
        with:
          app-name: 'conflictedlineup-web'
          package: './dist'
```

### Pattern 4: Key Vault References in App Settings

**What:** Reference secrets stored in Key Vault directly in App Service application settings using special syntax
**When to use:** All secrets in deployed environments (not local development)

**Example:**
```bash
# Azure CLI command to set Key Vault reference
az webapp config appsettings set \
  --resource-group rg-conflictedlineup-prod-centralus \
  --name conflictedlineup-api \
  --settings DatabaseConnectionString="@Microsoft.KeyVault(SecretUri=https://conflictedlineup-kv.vault.azure.net/secrets/db-connection)"
```

**Access in .NET:**
```csharp
// Program.cs - automatically resolved from app settings
var connectionString = builder.Configuration["DatabaseConnectionString"];
```

### Pattern 5: Health Check Endpoint Design

**What:** Comprehensive health check that validates critical dependencies and returns appropriate status codes
**When to use:** All backend applications deployed to App Service

**Example (.NET minimal API):**
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/health", () =>
{
    // Basic health check for skeleton deployment
    return Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
});

// For future phases with dependencies:
app.MapGet("/api/health/detailed", async (IDbConnection db) =>
{
    var checks = new Dictionary<string, string>();

    try
    {
        await db.ExecuteScalarAsync("SELECT 1");
        checks["database"] = "Healthy";
    }
    catch
    {
        checks["database"] = "Unhealthy";
        return Results.StatusCode(500);
    }

    return Results.Ok(new { status = "Healthy", checks, timestamp = DateTime.UtcNow });
});

app.Run();
```

### Anti-Patterns to Avoid

- **Committing .env files with secrets:** Always use .env.example as template, .gitignore actual .env files
- **Using environment variables for secrets in containers:** Use Docker secrets or bind mounts for .env files instead
- **Deploying directly to production slot:** Always deploy to staging slot, test, then swap
- **Single instance deployments:** Always scale to minimum 2 instances for high availability
- **Hardcoded connection strings in code:** Use configuration providers and Key Vault references
- **Running Docker builds in GitHub Actions for .NET/Node:** Build artifacts in CI, deploy compiled output
- **Using localhost in Docker container configs:** Use 0.0.0.0 for servers, service names for inter-container communication
- **Ignoring health check response times:** Must respond within 60 seconds or marked unhealthy

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Secret rotation | Custom secret refresh logic | Key Vault references with no version | Auto-rotates within 24 hours, built-in caching |
| Authentication tokens | JWT generation/validation for Azure | Managed Identity + Azure RBAC | No credentials to manage, automatic rotation, Azure-native |
| SSL certificate management | Custom cert provisioning | Azure-managed certificates | Auto-renewal, free, integrated with Front Door/App Service |
| Health check monitoring | Custom ping service | App Service health check feature | Built-in load balancer integration, automatic instance replacement |
| Container registry | Self-hosted Docker registry | Azure Container Registry | Private, geo-replicated, integrates with App Service |
| CI/CD authentication | Service principal secrets | OIDC federated credentials | No secret rotation, short-lived tokens, more secure |
| Static file serving | Custom Node.js server | Nginx in container or App Service | Battle-tested, optimized, supports compression and caching |
| Environment config switching | Conditional logic in code | Azure deployment slots + slot settings | Test exact production config in staging before swap |

**Key insight:** Azure provides managed services for almost every infrastructure concern. Custom solutions add maintenance burden, security risks, and lack Azure's automatic updates, scaling, and monitoring integrations.

## Common Pitfalls

### Pitfall 1: Wrong Operating System for .NET Framework
**What goes wrong:** Creating Linux-based App Service for .NET Framework applications causes startup failures
**Why it happens:** .NET Framework requires Windows-specific libraries; confusion with .NET Core/.NET 5+
**How to avoid:** Use Linux for .NET 5/6/7/8/9+, Windows only for .NET Framework 4.x
**Warning signs:** Application fails to start with "library not found" errors in logs

### Pitfall 2: Local Configuration Deployed to Cloud
**What goes wrong:** Application connects to localhost database or uses development API keys in production
**Why it happens:** Developers forget to update app settings from local values to Azure values
**How to avoid:**
- Never commit .env files with real values
- Use Azure App Settings and Key Vault references exclusively in deployed environments
- Validate configuration on first deployment to staging slot
**Warning signs:** 500 errors, connection timeouts, or authentication failures immediately after deployment

### Pitfall 3: Database Migration Timing Issues
**What goes wrong:** Application starts but crashes because database schema doesn't match code
**Why it happens:** Migrations added locally but not run on Azure database before deployment
**How to avoid:**
- Run migrations as separate step in CI/CD before deploying application code
- Or configure application to run migrations on startup (acceptable for skeleton phase)
- Never assume database state matches code
**Warning signs:** SQL errors in logs about missing tables/columns immediately after deploy

### Pitfall 4: Health Check Not Allowing Anonymous Access
**What goes wrong:** Health check endpoint returns 401 Unauthorized, App Service marks instances unhealthy
**Why it happens:** Health check path protected by authentication middleware
**How to avoid:**
- Allow anonymous access to health endpoint
- Secure with `x-ms-auth-internal-token` header validation if needed
- Test health endpoint returns 200 without authentication
**Warning signs:** All instances marked unhealthy despite application working fine

### Pitfall 5: Docker Hot Reload Not Working
**What goes wrong:** Code changes in IDE don't trigger container refresh; must rebuild container manually
**Why it happens:** File system polling disabled, volumes not mounted correctly, or Vite watching localhost
**How to avoid:**
- Set `CHOKIDAR_USEPOLLING=true` for Vite/React
- Set `DOTNET_USE_POLLING_FILE_WATCHER=true` for .NET
- Configure Vite server host to `0.0.0.0`
- Mount source directories as volumes, exclude node_modules
**Warning signs:** Must run `docker compose restart` after every code change

### Pitfall 6: GitHub Actions Deployment Fails with 403
**What goes wrong:** Workflow authentication succeeds but deployment fails with authorization error
**Why it happens:** Service principal/managed identity lacks sufficient permissions on App Service
**How to avoid:**
- Assign "Website Contributor" role (not just "Contributor") to identity
- Scope role to specific App Service resource, not entire resource group
- Verify role assignment before first workflow run
**Warning signs:** `az login` succeeds but `azure/webapps-deploy` fails with 403

### Pitfall 7: Deployment Slot Swap Breaks Configuration
**What goes wrong:** Application works in staging but fails after swap to production
**Why it happens:** Configuration isn't marked as "slot setting" so it swaps with application code
**How to avoid:**
- Mark environment-specific settings as "slot settings" in Azure Portal
- Test swap behavior in non-production environment first
- Validate configuration after swap before directing traffic
**Warning signs:** Different behavior before and after slot swap despite same code

### Pitfall 8: Front Door Caching Stale Content
**What goes wrong:** Deploy new version but users see old content for hours
**Why it happens:** Front Door caches responses based on default cache rules
**How to avoid:**
- Configure cache purge in deployment workflow
- Set appropriate Cache-Control headers for static assets
- Use query string versioning for cache busting
**Warning signs:** Deployment succeeds but changes not visible in browser

### Pitfall 9: Insufficient Health Check Timeout
**What goes wrong:** Application healthy but marked unhealthy; instances constantly replaced
**Why it happens:** Cold start takes longer than 60 seconds, health check times out
**How to avoid:**
- Optimize cold start time (lazy load dependencies)
- Return 200 as soon as critical components ready
- Don't perform expensive operations in health check
- Use "Always On" setting to prevent cold starts
**Warning signs:** Instances replaced hourly despite application functioning

### Pitfall 10: Key Vault Network Restrictions Block App Service
**What goes wrong:** App Service can't retrieve secrets from Key Vault despite correct permissions
**Why it happens:** Key Vault firewall blocks traffic from App Service virtual network
**How to avoid:**
- Enable `vnetRouteAllEnabled` on App Service
- Configure Key Vault to allow traffic from App Service VNet
- Or use Key Vault with public access enabled initially
**Warning signs:** "Secret not found" errors despite secret existing and permissions correct

## Code Examples

Verified patterns from official sources:

### Health Check Endpoint with Header Validation (.NET)

```csharp
// Source: https://learn.microsoft.com/en-us/azure/app-service/monitor-instances-health-check
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/health", (HttpContext context) =>
{
    // Validate request is from App Service
    if (context.Request.Headers.TryGetValue("x-ms-auth-internal-token", out var headerValue))
    {
        var encryptionKey = Environment.GetEnvironmentVariable("WEBSITE_AUTH_ENCRYPTION_KEY");
        if (!string.IsNullOrEmpty(encryptionKey))
        {
            var sha = SHA256.Create();
            var hash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey)));

            if (!string.Equals(hash, headerValue, StringComparison.Ordinal))
            {
                return Results.Unauthorized();
            }
        }
    }

    return Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
});

app.Run();
```

### GitHub Actions Workflow with OIDC (.NET Backend)

```yaml
# Source: https://learn.microsoft.com/en-us/azure/app-service/deploy-github-actions
name: Deploy Backend to Azure

on:
  push:
    branches: [ main ]
    paths:
      - 'backend/**'
      - '.github/workflows/backend-deploy.yml'

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore
        working-directory: ./backend/src

      - name: Build
        run: dotnet build --configuration Release --no-restore
        working-directory: ./backend/src

      - name: Test
        run: dotnet test --no-build --verbosity normal
        working-directory: ./backend/src

  deploy:
    needs: test
    runs-on: ubuntu-latest
    permissions:
      id-token: write
      contents: read

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Build and publish
        run: |
          dotnet restore
          dotnet build --configuration Release
          dotnet publish -c Release --property:PublishDir='./publish'
        working-directory: ./backend/src

      - name: Login to Azure
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      - name: Deploy to App Service
        uses: azure/webapps-deploy@v3
        with:
          app-name: 'conflictedlineup-api'
          slot-name: 'staging'
          package: './backend/src/publish'

      - name: Logout
        run: az logout
        if: always()
```

### GitHub Actions Workflow with OIDC (React Frontend)

```yaml
# Source: https://learn.microsoft.com/en-us/azure/app-service/deploy-github-actions
name: Deploy Frontend to Azure

on:
  push:
    branches: [ main ]
    paths:
      - 'frontend/**'
      - '.github/workflows/frontend-deploy.yml'

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '24.x'
          cache: 'npm'
          cache-dependency-path: './frontend/package-lock.json'

      - name: Install dependencies
        run: npm ci
        working-directory: ./frontend

      - name: Run tests
        run: npm test
        working-directory: ./frontend
        env:
          CI: true

  deploy:
    needs: test
    runs-on: ubuntu-latest
    permissions:
      id-token: write
      contents: read

    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '24.x'
          cache: 'npm'
          cache-dependency-path: './frontend/package-lock.json'

      - name: Install and build
        run: |
          npm ci
          npm run build
        working-directory: ./frontend
        env:
          VITE_API_URL: https://conflictedlineup-api.azurewebsites.net

      - name: Login to Azure
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      - name: Deploy to App Service
        uses: azure/webapps-deploy@v3
        with:
          app-name: 'conflictedlineup-web'
          slot-name: 'staging'
          package: './frontend/dist'

      - name: Logout
        run: az logout
        if: always()
```

### Docker Compose for Local Development with Hot Reload

```yaml
# Source: https://medium.com/@pathakavani10/orchestrating-a-full-stack-application-with-docker-compose-react-net-core-and-sql-server-cddcd5b819ed
# docker-compose.dev.yml
services:
  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile.dev
    volumes:
      - ./frontend:/app
      - /app/node_modules
    environment:
      - CHOKIDAR_USEPOLLING=true
      - VITE_API_URL=http://backend:8080
    ports:
      - "5173:5173"
    depends_on:
      - backend

  backend:
    build:
      context: ./backend
      dockerfile: Dockerfile.dev
    volumes:
      - ./backend:/app
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - DOTNET_USE_POLLING_FILE_WATCHER=true
    ports:
      - "8080:8080"
```

### Dockerfile.dev for React with Vite Hot Reload

```dockerfile
# Source: https://medium.com/@sankettikam17/dockerizing-your-react-app-with-hot-reloading-yarn-and-vite-a-smooth-development-workflow-303ae51ac11a
FROM node:24-alpine

WORKDIR /app

# Install dependencies
COPY package*.json ./
RUN npm install

# Copy source code
COPY . .

EXPOSE 5173

# Start dev server
CMD ["npm", "run", "dev"]
```

### Dockerfile.dev for .NET with Hot Reload

```dockerfile
# Source: https://learn.microsoft.com/en-us/dotnet/core/docker/build-container
FROM mcr.microsoft.com/dotnet/sdk:9.0

WORKDIR /app

# Copy project file and restore
COPY src/*.csproj ./
RUN dotnet restore

# Copy source code
COPY src/ ./

EXPOSE 8080

# Use dotnet watch for hot reload
CMD ["dotnet", "watch", "run", "--urls", "http://+:8080"]
```

### Vite Configuration for Docker Hot Reload

```typescript
// Source: https://github.com/tarikulwebx/react-app-dockerize
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0',
    port: 5173,
    watch: {
      usePolling: true,
      interval: 1000
    },
    hmr: {
      clientPort: 5173
    }
  }
})
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Create React App | Vite | 2021-2023 | 10-100x faster dev server, smaller bundles, better DX |
| Service principal secrets | OIDC federated credentials | 2022-2023 | No secret rotation, enhanced security, simpler setup |
| Azure Front Door Classic | Front Door Standard/Premium | 2025 deprecation | Managed certificates deprecated Aug 2025, full retirement Mar 2027 |
| .NET 6/7/8 | .NET 9 | Nov 2024 | Performance improvements, better container support |
| Docker Compose v1 | Docker Compose v2 | 2022-2023 | Better performance, maintained actively, written in Go |
| Environment variables for secrets | Docker secrets / Key Vault | 2023-2024 | Reduced leak risk, audit logging, automatic rotation |

**Deprecated/outdated:**
- **Azure Front Door Classic**: Retirement March 31, 2027; must migrate to Standard/Premium by Aug 2025 for new domains
- **Create React App**: Unmaintained; official React docs now recommend Vite or Next.js
- **GitHub Actions service principal with secrets**: OIDC is now recommended method
- **App Service deployment credentials (FTP)**: Use GitHub Actions, Azure DevOps, or `az webapp up` instead

## Open Questions

Things that couldn't be fully resolved:

1. **Azure Front Door tier selection (Standard vs Premium)**
   - What we know: Premium supports Private Link to App Service, WAF with custom rules, advanced routing
   - What's unclear: Whether Standard tier suffices for initial skeleton deployment given no Private Link requirement yet
   - Recommendation: Start with Standard tier for cost savings, upgrade to Premium when Private Link or advanced WAF needed in later phases

2. **Staging slot swap vs blue-green deployment**
   - What we know: Staging slots swap configuration and warm up instances before production
   - What's unclear: Whether user prefers auto-swap on deployment or manual swap after validation
   - Recommendation: Manual swap initially for validation, configure auto-swap once confidence established

3. **App Service Plan sizing for skeleton deployment**
   - What we know: Minimum B1 tier for deployment slots, minimum 2 instances for health check benefits
   - What's unclear: Expected traffic profile and whether to optimize for cost or performance initially
   - Recommendation: Start with B1 tier (2 instances) for cost optimization, scale up as needed in future phases

4. **Custom domain provider and DNS hosting**
   - What we know: User has custom domain to use; Azure Front Door requires DNS validation
   - What's unclear: Whether DNS is hosted in Azure DNS or external provider
   - Recommendation: If using external DNS (GoDaddy, Cloudflare, etc.), provide manual TXT/CNAME record instructions; if Azure DNS, use automated record creation

## Sources

### Primary (HIGH confidence)
- [Microsoft Learn - Deploy to Azure App Service using GitHub Actions](https://learn.microsoft.com/en-us/azure/app-service/deploy-github-actions)
- [Microsoft Learn - Use Key Vault References as App Settings](https://learn.microsoft.com/en-us/azure/app-service/app-service-key-vault-references)
- [Microsoft Learn - Monitor the Health of App Service Instances](https://learn.microsoft.com/en-us/azure/app-service/monitor-instances-health-check)
- [Microsoft Learn - Deployment Best Practices - Azure App Service](https://learn.microsoft.com/en-us/azure/app-service/deploy-best-practices)
- [Microsoft Learn - How to Add a Custom Domain - Azure Front Door](https://learn.microsoft.com/en-us/azure/frontdoor/standard-premium/how-to-add-custom-domain)
- [Docker Docs - Secrets in Compose](https://docs.docker.com/compose/how-tos/use-secrets/)
- [Microsoft Learn - Managed Identities - Azure App Service](https://learn.microsoft.com/en-us/azure/app-service/overview-managed-identity)
- [GitHub Docs - Configuring OpenID Connect in Azure](https://docs.github.com/en/actions/security-for-github-actions/security-hardening-your-deployments/configuring-openid-connect-in-azure)

### Secondary (MEDIUM confidence)
- [Medium - Orchestrating a Full-Stack Application with Docker Compose: React, .NET Core, and SQL Server](https://medium.com/@pathakavani10/orchestrating-a-full-stack-application-with-docker-compose-react-net-core-and-sql-server-cddcd5b819ed)
- [Medium - Dockerizing Your React App with Hot Reloading (Yarn and Vite)](https://medium.com/@sankettikam17/dockerizing-your-react-app-with-hot-reloading-yarn-and-vite-a-smooth-development-workflow-303ae51ac11a)
- [Medium - Azure OIDC Authentication in GitHub Actions](https://momosuke-san.medium.com/azure-oidc-authentication-in-github-actions-a-secure-step-by-step-setup-azure-login-687e9a1ff933)
- [Microsoft Community Hub - Understanding 'Always On' vs. Health Check in Azure App Service](https://techcommunity.microsoft.com/blog/appsonazureblog/understanding-always-on-vs-health-check-in-azure-app-service/4399899)
- [Azure Dive - Azure Static Web Apps in depth](https://www.azuredive.net/2025/09/azure-static-web-apps-in-depth/)
- [Microsoft Learn Community - Azure Resources Naming Convention and Tagging](https://learn.microsoft.com/en-us/community/content/azure-resources-naming-convention-and-tagging)

### Tertiary (LOW confidence)
- [GitHub Community - How to deploy only after test passes](https://github.com/orgs/community/discussions/62999)
- [OneUptime Blog - How to Set Up Hot Reloading in Docker](https://oneuptime.com/blog/post/2026-01-06-docker-hot-reloading/view)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Official Microsoft documentation and Docker docs confirm all tools and patterns
- Architecture: HIGH - Verified through official examples and current Microsoft Learn tutorials
- Pitfalls: MEDIUM - Based on troubleshooting docs and community discussions; some from direct experience reports

**Research date:** 2026-01-25
**Valid until:** 2026-02-25 (30 days - relatively stable Azure platform features)

**Notes:**
- Azure Front Door Classic retirement timeline creates urgency to use Standard/Premium tier
- OIDC authentication is now standard; service principal approach is legacy
- Vite has replaced Create React App as community standard for React
- .NET 9 is latest stable version as of research date
