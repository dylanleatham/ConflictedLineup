# Architecture Research

**Domain:** Festival lineup to Spotify playlist conversion web app
**Researched:** 2026-01-24
**Confidence:** HIGH

## Standard Architecture

### System Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     Azure Front Door                         │
│  (CDN, DDoS Protection, SSL Offload, Routing)               │
└────────────────┬────────────────────────────────────────────┘
                 │
    ┌────────────┴─────────────┐
    │                          │
┌───▼───────────────┐   ┌─────▼──────────────┐
│  React SPA        │   │  C# Backend        │
│  (Vite build)     │   │  (ASP.NET Core)    │
│                   │   │                    │
│  ┌─────────────┐  │   │  ┌──────────────┐  │
│  │ Components  │  │   │  │ Minimal APIs │  │
│  ├─────────────┤  │   │  ├──────────────┤  │
│  │ State Mgmt  │  │   │  │ Services     │  │
│  ├─────────────┤  │   │  ├──────────────┤  │
│  │ API Client  │──┼───┼─▶│ Auth Manager │  │
│  └─────────────┘  │   │  ├──────────────┤  │
│                   │   │  │ Background   │  │
│                   │   │  │ Jobs         │  │
│                   │   │  └──────────────┘  │
└───────────────────┘   └─────┬──────────────┘
                               │
         ┌─────────────────────┼────────────────────┐
         │                     │                    │
    ┌────▼────────┐    ┌──────▼────────┐   ┌──────▼──────┐
    │ Azure Key   │    │ Anthropic API │   │ Spotify API │
    │ Vault       │    │ (Claude)      │   │             │
    │             │    │               │   │             │
    │ - Spotify   │    │ - Vision API  │   │ - OAuth 2.0 │
    │   Client ID │    │ - Web Search  │   │ - Web API   │
    │ - Spotify   │    │               │   │             │
    │   Secret    │    │               │   │             │
    │ - Anthropic │    │               │   │             │
    │   API Key   │    │               │   │             │
    └─────────────┘    └───────────────┘   └─────────────┘
```

### Component Responsibilities

| Component | Responsibility | Typical Implementation |
|-----------|----------------|------------------------|
| **Azure Front Door** | CDN, global routing, SSL termination, DDoS protection | Azure-managed service with routing rules for SPA fallback |
| **React SPA** | User interface, image upload, OAuth flow initiation, playlist display | Vite + React + TypeScript, feature-based folder structure |
| **C# Backend** | API gateway, OAuth token management, Anthropic/Spotify orchestration | ASP.NET Core Minimal APIs with dependency injection |
| **Auth Manager** | Spotify OAuth 2.0 flow (Authorization Code + PKCE), token storage/refresh | Backend-For-Frontend (BFF) pattern with HTTP-only cookies |
| **Services Layer** | Business logic: artist extraction, Spotify search, playlist creation | Service classes injected into API endpoints |
| **Background Jobs** | Async playlist creation for large lineups, retry logic | .NET BackgroundService or Hangfire for production |
| **Azure Key Vault** | Secrets management for API keys and OAuth credentials | Accessed via Managed Identity (no hardcoded credentials) |
| **Anthropic API** | Image vision analysis, artist list extraction, web search for festival data | Claude 4.5 Sonnet via official .NET SDK |
| **Spotify API** | Artist search, track selection, playlist creation, user library access | Authorization Code Flow with PKCE, scopes for playlist modification |

## Recommended Project Structure

### Backend: C# ASP.NET Core

```
ConflictedLineup.Backend/
├── Program.cs                    # Application entry, DI container, Minimal API endpoints
├── appsettings.json              # Non-secret configuration (Key Vault references)
├── appsettings.Development.json  # Local dev overrides
├── Features/                     # Feature-based organization (domain-driven)
│   ├── Authentication/
│   │   ├── SpotifyAuthService.cs      # OAuth flow, token refresh
│   │   ├── AuthEndpoints.cs           # Minimal API: /auth/login, /auth/callback
│   │   └── Models/
│   │       └── SpotifyTokenResponse.cs
│   ├── ArtistExtraction/
│   │   ├── AnthropicService.cs        # Claude API integration
│   │   ├── ArtistExtractorService.cs  # Coordinates vision + web search
│   │   └── Models/
│   │       ├── ExtractedArtist.cs
│   │       └── ExtractionRequest.cs
│   ├── PlaylistCreation/
│   │   ├── SpotifyPlaylistService.cs  # Playlist API calls
│   │   ├── TrackSelectionService.cs   # Pick top tracks per artist
│   │   ├── PlaylistCreationJob.cs     # Background job for async processing
│   │   └── Models/
│   │       ├── PlaylistResult.cs
│   │       └── SkippedArtist.cs
│   └── ImageUpload/
│       ├── ImageProcessingService.cs   # Validate, resize, convert to base64
│       └── UploadEndpoints.cs
├── Infrastructure/
│   ├── KeyVault/
│   │   └── SecretProvider.cs          # Azure Key Vault client wrapper
│   ├── HttpClients/
│   │   ├── AnthropicHttpClient.cs     # Typed HTTP client for Anthropic
│   │   └── SpotifyHttpClient.cs       # Typed HTTP client for Spotify
│   └── BackgroundJobs/
│       └── JobQueue.cs                 # Background task queue
└── wwwroot/                            # Optional: serve React build from same app
```

### Frontend: React + Vite

```
ConflictedLineup.Frontend/
├── public/
│   └── assets/                   # Static images, icons
├── src/
│   ├── main.tsx                  # App entry point
│   ├── App.tsx                   # Root component, routing
│   ├── features/                 # Feature-based organization
│   │   ├── upload/
│   │   │   ├── UploadPage.tsx           # Image upload OR festival name input
│   │   │   ├── ImagePreview.tsx
│   │   │   └── useUpload.ts             # Custom hook for upload logic
│   │   ├── auth/
│   │   │   ├── SpotifyAuthButton.tsx    # Redirect to /auth/login
│   │   │   └── useAuth.ts               # Auth state management
│   │   ├── playlist/
│   │   │   ├── PlaylistResultPage.tsx   # Show created playlist
│   │   │   ├── ArtistList.tsx           # Display extracted artists
│   │   │   ├── SkippedArtists.tsx       # Show artists not found
│   │   │   └── usePlaylist.ts           # Playlist creation hook
│   │   └── landing/
│   │       └── LandingPage.tsx          # Home page
│   ├── components/              # Reusable UI components
│   │   ├── Button/
│   │   │   ├── Button.tsx
│   │   │   └── button.module.css
│   │   ├── Loading/
│   │   │   └── LoadingSpinner.tsx
│   │   └── ErrorBoundary/
│   │       └── ErrorBoundary.tsx
│   ├── api/                     # API client layer
│   │   ├── client.ts                    # Axios instance with base config
│   │   ├── authApi.ts                   # Auth endpoints
│   │   ├── uploadApi.ts                 # Image upload endpoints
│   │   └── playlistApi.ts               # Playlist creation endpoints
│   ├── hooks/                   # Shared custom hooks
│   │   └── useApi.ts                    # Generic API hook with error handling
│   ├── utils/
│   │   ├── imageValidator.ts            # Client-side image validation
│   │   └── errorHandler.ts              # Error formatting utilities
│   └── types/
│       ├── api.types.ts                 # API request/response types
│       └── domain.types.ts              # Business domain types
├── .env.development             # Local API URL
├── .env.production              # Production API URL
├── vite.config.ts               # Vite configuration
└── tsconfig.json                # TypeScript config
```

### Structure Rationale

- **Feature-based (not layer-based):** Groups all code for a feature together (components, services, models) rather than separating by technical layer. This matches Microsoft's 2026 recommendations for domain-driven structure.
- **Minimal APIs over Controllers:** Microsoft recommends Minimal APIs for new projects due to lower overhead and simpler syntax. Each feature can have its own endpoint registration file.
- **Backend-For-Frontend pattern:** Backend manages all OAuth tokens and secrets. Frontend never sees refresh tokens or API keys.
- **React feature folders:** Each feature contains its page component, child components, and custom hooks. Reusable components live separately.
- **Separation of API layer:** Frontend has dedicated `api/` folder with typed HTTP clients, keeping API details out of components.

## Architectural Patterns

### Pattern 1: Backend-For-Frontend (BFF) for OAuth

**What:** Backend acts as OAuth client, stores tokens server-side, issues session cookies to frontend.

**When to use:** Always for SPAs with OAuth flows. Never store access/refresh tokens in browser localStorage.

**Trade-offs:**
- ✅ **Pro:** Tokens never exposed to browser (XSS-resistant), refresh tokens safe on backend
- ✅ **Pro:** Backend can refresh tokens transparently
- ❌ **Con:** Requires session state management on backend

**Example:**
```csharp
// Backend: Auth endpoint
app.MapGet("/auth/login", (HttpContext ctx) =>
{
    var state = GenerateSecureState();
    var pkceChallenge = GeneratePkceChallenge();

    // Store state + PKCE in server session
    ctx.Session.SetString("oauth_state", state);
    ctx.Session.SetString("pkce_verifier", pkceChallenge.Verifier);

    var authUrl = $"https://accounts.spotify.com/authorize?" +
        $"client_id={clientId}&" +
        $"response_type=code&" +
        $"redirect_uri={redirectUri}&" +
        $"scope=playlist-modify-public playlist-modify-private&" +
        $"state={state}&" +
        $"code_challenge={pkceChallenge.Challenge}&" +
        $"code_challenge_method=S256";

    return Results.Redirect(authUrl);
});

app.MapGet("/auth/callback", async (HttpContext ctx, string code, string state) =>
{
    // Validate state, exchange code for tokens
    var tokens = await ExchangeCodeForTokens(code);

    // Store tokens in server-side session or database
    ctx.Session.SetString("access_token", tokens.AccessToken);
    ctx.Session.SetString("refresh_token", tokens.RefreshToken);

    // Set HTTP-only cookie for session ID
    ctx.Response.Cookies.Append("session_id", ctx.Session.Id, new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict
    });

    // Redirect back to frontend
    return Results.Redirect("https://frontend.example.com/playlist");
});
```

### Pattern 2: Service Layer Abstraction

**What:** Separate business logic from API endpoints. Services encapsulate external API calls (Anthropic, Spotify).

**When to use:** Always. Keeps endpoints thin, makes testing easier, enables reuse.

**Trade-offs:**
- ✅ **Pro:** Testable (mock services in unit tests), clear separation of concerns
- ✅ **Pro:** Easy to swap implementations (e.g., cache wrapper)
- ❌ **Con:** More files/classes than inline logic

**Example:**
```csharp
// Service interface
public interface IAnthropicService
{
    Task<List<string>> ExtractArtistsFromImage(byte[] imageData);
    Task<List<string>> SearchFestivalLineup(string festivalName);
}

// Implementation
public class AnthropicService : IAnthropicService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public AnthropicService(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClient = httpClientFactory.CreateClient("Anthropic");
        _apiKey = config["Anthropic:ApiKey"]; // From Key Vault
    }

    public async Task<List<string>> ExtractArtistsFromImage(byte[] imageData)
    {
        var request = new
        {
            model = "claude-sonnet-4-5-20250929",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "image", source = new { type = "base64", media_type = "image/jpeg", data = Convert.ToBase64String(imageData) } },
                        new { type = "text", text = "Extract all artist names from this festival poster. Return as JSON array." }
                    }
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync("/v1/messages", request);
        var result = await response.Content.ReadFromJsonAsync<ClaudeResponse>();

        return ParseArtistNames(result.Content);
    }
}

// DI registration in Program.cs
builder.Services.AddHttpClient("Anthropic", client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com");
    client.DefaultRequestHeaders.Add("x-api-key", builder.Configuration["Anthropic:ApiKey"]);
    client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
});
builder.Services.AddScoped<IAnthropicService, AnthropicService>();
```

### Pattern 3: Background Jobs for Playlist Creation

**What:** Offload long-running playlist creation to background job. Return job ID immediately, poll for status.

**When to use:** When processing could take >5 seconds (large lineups with 50+ artists).

**Trade-offs:**
- ✅ **Pro:** Frontend stays responsive, can show progress
- ✅ **Pro:** Enables retry logic if Spotify API fails
- ❌ **Con:** More complex than synchronous processing
- ❌ **Con:** Requires job storage and status polling

**Example:**
```csharp
// Endpoint: Start background job
app.MapPost("/api/playlist/create", async (
    PlaylistRequest request,
    IBackgroundJobQueue jobQueue,
    HttpContext ctx) =>
{
    var jobId = Guid.NewGuid();
    var userId = ctx.Session.GetString("spotify_user_id");

    await jobQueue.EnqueueAsync(async token =>
    {
        var playlistService = ctx.RequestServices.GetRequiredService<ISpotifyPlaylistService>();
        await playlistService.CreatePlaylistAsync(userId, request.Artists, jobId);
    });

    return Results.Ok(new { jobId });
});

// Frontend: Poll for completion
async function createPlaylist(artists) {
    const { jobId } = await api.post('/api/playlist/create', { artists });

    while (true) {
        const status = await api.get(`/api/playlist/status/${jobId}`);

        if (status.isComplete) {
            return status.result;
        }

        await new Promise(resolve => setTimeout(resolve, 2000)); // Poll every 2s
    }
}
```

### Pattern 4: Managed Identity for Key Vault Access

**What:** Use Azure Managed Identity to access Key Vault without storing credentials.

**When to use:** Always on Azure. No hardcoded secrets in appsettings.json.

**Trade-offs:**
- ✅ **Pro:** Zero secrets in code, automatic credential rotation
- ✅ **Pro:** Works seamlessly across dev (local identity) and production (app identity)
- ❌ **Con:** Requires Azure setup, doesn't work on non-Azure hosts

**Example:**
```csharp
// Program.cs: Configure Key Vault with Managed Identity
var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    var keyVaultUrl = new Uri(builder.Configuration["KeyVault:Url"]);
    builder.Configuration.AddAzureKeyVault(
        keyVaultUrl,
        new DefaultAzureCredential() // Uses Managed Identity in Azure, local identity in dev
    );
}

// Access secrets like normal config
var spotifyClientId = builder.Configuration["Spotify--ClientId"];
var spotifyClientSecret = builder.Configuration["Spotify--ClientSecret"];
var anthropicApiKey = builder.Configuration["Anthropic--ApiKey"];
```

## Data Flow

### Flow 1: Image Upload → Playlist Creation

```
[User uploads image on React SPA]
    ↓
[POST /api/upload/image] → Backend validates image size/format
    ↓
[Anthropic Vision API] ← Backend sends base64 image
    ↓ (returns artist names)
[Spotify Search API] ← Backend queries each artist
    ↓ (returns artist IDs + track URIs)
[Spotify Playlist API] ← Backend creates playlist
    ↓ (returns playlist URL)
[Backend stores job result] → Redis or in-memory cache
    ↓
[Frontend polls /api/playlist/status/{jobId}]
    ↓
[Display playlist link + artist list + skipped artists]
```

### Flow 2: Festival Name → Playlist Creation

```
[User types festival name on React SPA]
    ↓
[POST /api/upload/festival-name] → Backend receives name
    ↓
[Anthropic Web Search API] ← Backend requests lineup for festival
    ↓ (returns artist names from web search)
[Spotify Search API] ← Backend queries each artist
    ↓ (returns artist IDs + track URIs)
[Spotify Playlist API] ← Backend creates playlist
    ↓ (returns playlist URL)
[Backend stores job result]
    ↓
[Frontend polls /api/playlist/status/{jobId}]
    ↓
[Display playlist link + artist list + skipped artists]
```

### Flow 3: Spotify OAuth Authentication

```
[User clicks "Login with Spotify" on React SPA]
    ↓
[GET /auth/login] → Backend generates OAuth state + PKCE challenge
    ↓
[Redirect to Spotify] → User authorizes app
    ↓
[Spotify redirects to /auth/callback?code=...]
    ↓
[Backend exchanges code for tokens] ← POST https://accounts.spotify.com/api/token
    ↓
[Store tokens in server session] → Session storage or Redis
    ↓
[Set HTTP-only session cookie]
    ↓
[Redirect to frontend /dashboard]
    ↓
[Frontend makes authenticated requests] → All requests include session cookie
```

### Key Data Flows

1. **Secrets flow (Key Vault → Backend):** On startup, backend loads secrets from Azure Key Vault using Managed Identity. Secrets never leave backend.

2. **Image flow (Frontend → Anthropic):** Frontend uploads image as multipart/form-data, backend converts to base64 and sends to Anthropic Vision API.

3. **Token flow (Backend ← Spotify → Backend → Frontend):** Backend manages all OAuth tokens. Frontend only gets session cookie. Backend refreshes tokens transparently.

4. **Progress flow (Background job → Frontend):** Backend starts job, returns job ID. Frontend polls status endpoint. Backend updates job status as it progresses.

## Scaling Considerations

| Scale | Architecture Adjustments |
|-------|--------------------------|
| **0-1k users** | Single Azure App Service (Backend + Frontend), in-memory session storage, synchronous playlist creation (no background jobs) |
| **1k-10k users** | Separate App Services for frontend/backend, Azure Front Door for CDN, background jobs with Hangfire + SQL storage, Redis for session/token caching |
| **10k-100k users** | Scale out App Services (multiple instances), Azure SQL for persistent data, Azure Blob Storage for uploaded images (don't store in request), rate limiting for Anthropic/Spotify APIs |
| **100k+ users** | Consider Azure Functions for background jobs (consumption plan), Cosmos DB for global distribution, API Management for throttling, separate Anthropic/Spotify API keys per region |

### Scaling Priorities

1. **First bottleneck: Anthropic API rate limits**
   - **Symptom:** 429 errors when processing many images concurrently
   - **Fix:** Implement queue-based processing with rate limiting (e.g., 10 requests/minute). Use Hangfire or Azure Service Bus to throttle requests.
   - **Alternative:** Cache artist extraction results for popular festivals

2. **Second bottleneck: Spotify API rate limits**
   - **Symptom:** 429 errors when creating playlists or searching artists
   - **Fix:** Batch requests where possible (search up to 50 artists in single request). Implement exponential backoff retry logic.
   - **Alternative:** Pre-populate artist database with Spotify IDs for common festivals

3. **Third bottleneck: Session storage**
   - **Symptom:** Slow session lookups, memory pressure on App Service
   - **Fix:** Move from in-memory sessions to Redis (Azure Cache for Redis). Enable sticky sessions in Azure Front Door if using multiple backend instances.

## Anti-Patterns

### Anti-Pattern 1: Storing OAuth Tokens in Frontend localStorage

**What people do:** Store Spotify access/refresh tokens in React state or localStorage to avoid backend complexity.

**Why it's wrong:**
- Any XSS vulnerability allows attacker to steal refresh token (long-lived, grants full access)
- Tokens visible in browser dev tools
- Violates OAuth 2.0 security best practices for SPAs (2026 standards require BFF pattern)

**Do this instead:** Use Backend-For-Frontend pattern. Backend stores tokens in server-side session or database. Frontend gets HTTP-only session cookie. Backend refreshes tokens transparently.

### Anti-Pattern 2: Deploying React SPA and Backend as Separate Origins Without CORS

**What people do:** Deploy React to `frontend.azurewebsites.net` and backend to `backend.azurewebsites.net`, then struggle with CORS errors.

**Why it's wrong:**
- Requires CORS configuration (security risk if misconfigured)
- Session cookies don't work across domains (SameSite=None required, less secure)
- Extra network hop, more latency

**Do this instead:**
- **Option A (Preferred):** Serve React build from backend's `wwwroot/` folder. Single origin, no CORS, cookies work.
- **Option B:** Use Azure Front Door to route `example.com/` → React (Blob Storage) and `example.com/api/*` → Backend (App Service). Single origin from client perspective.

### Anti-Pattern 3: Synchronous Processing for Large Lineups

**What people do:** Process 50+ artists synchronously in single HTTP request, causing 30+ second response times and gateway timeouts.

**Why it's wrong:**
- Azure App Service default timeout is 230 seconds, but Azure Front Door may timeout earlier
- Poor user experience (browser timeout, no progress indication)
- No retry logic if Spotify/Anthropic API fails mid-process

**Do this instead:** Use background job pattern. Return job ID immediately, process asynchronously, frontend polls for completion. Show progress bar as artists are processed.

### Anti-Pattern 4: Hardcoding API Keys in appsettings.json

**What people do:** Store Anthropic API key and Spotify secrets directly in `appsettings.json` or environment variables on App Service.

**Why it's wrong:**
- Keys visible in git history if accidentally committed
- No automatic rotation
- Difficult to manage across environments (dev/staging/prod)

**Do this instead:** Store all secrets in Azure Key Vault. Use Managed Identity to access them. Reference in appsettings.json: `"Anthropic:ApiKey": "KeyVault:AnthropicApiKey"`.

### Anti-Pattern 5: Using Implicit Flow for Spotify OAuth

**What people do:** Use Spotify's deprecated Implicit Flow (returns access token directly in URL fragment).

**Why it's wrong:**
- **Deprecated as of November 27, 2025.** No longer supported by Spotify.
- Tokens visible in browser history
- No refresh token, forcing re-authentication

**Do this instead:** Use Authorization Code Flow with PKCE. Backend exchanges authorization code for tokens, stores refresh token securely.

## Integration Points

### External Services

| Service | Integration Pattern | Notes |
|---------|---------------------|-------|
| **Anthropic API** | HTTP REST via official .NET SDK (`Anthropic.SDK` NuGet) | Use prompt caching for repeated festival names (90% cost savings). Rate limit: check current tier (free tier is limited). |
| **Spotify API** | HTTP REST, OAuth 2.0 Authorization Code + PKCE | Scopes needed: `playlist-modify-public`, `playlist-modify-private`. Rate limit: ~180 requests/minute per user. Use batch search endpoints. |
| **Azure Key Vault** | Azure SDK via Managed Identity (`Azure.Security.KeyVault.Secrets`) | Local dev: use `az login` to authenticate. Production: assign Managed Identity to App Service with "Key Vault Secrets User" role. |
| **Azure Front Door** | CDN for static assets, routing to backend | Configure rule: `/*` → React (catch-all for SPA routing), `/api/*` → Backend. Disable caching for `/api/*` routes. |

### Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| **React ↔ Backend** | HTTP JSON APIs (axios or fetch) | Backend returns structured JSON. Use TypeScript types generated from C# models (consider NSwag or OpenAPI generator). |
| **Auth Manager ↔ Services** | Direct method calls (DI) | Services receive `userId` or `accessToken` from Auth Manager. Services don't directly access session storage. |
| **Background Job ↔ Services** | DI with scoped lifetime | Background jobs create new service scope per job to avoid lifetime issues with HttpContext. |
| **Backend ↔ Azure Key Vault** | Azure SDK, loaded at startup | Secrets cached in Configuration object. Don't reload on every request (performance hit). |

## Deployment Architecture

### Recommended Azure Resources

```
Resource Group: rg-conflicted-lineup-prod
├── Azure Front Door: fd-conflicted-lineup
│   ├── Route: /* → Static Web App (React build)
│   └── Route: /api/* → App Service (Backend)
│
├── Static Web App: swa-conflicted-lineup-frontend
│   └── Vite build output (index.html, assets/*)
│
├── App Service: app-conflicted-lineup-backend
│   ├── Runtime: .NET 8 (or latest LTS)
│   ├── Plan: B1 or higher (supports custom domains, SSL)
│   └── Managed Identity: ENABLED
│
├── Key Vault: kv-conflicted-lineup
│   ├── Secret: Spotify--ClientId
│   ├── Secret: Spotify--ClientSecret
│   └── Secret: Anthropic--ApiKey
│
└── Azure Cache for Redis (optional, for production scale)
    └── Used for: Session storage, job status cache
```

### Alternative: Simpler Single-Resource Deployment

For MVP/early development, simplify to single App Service:

```
Resource Group: rg-conflicted-lineup-dev
├── App Service: app-conflicted-lineup
│   ├── Runtime: .NET 8
│   ├── wwwroot/: React build output (served as static files)
│   └── Managed Identity: ENABLED
│
└── Key Vault: kv-conflicted-lineup-dev
    ├── Secret: Spotify--ClientId
    ├── Secret: Spotify--ClientSecret
    └── Secret: Anthropic--ApiKey
```

**Pros:** Simple, single deployment, no CORS, cheaper
**Cons:** Harder to scale independently, frontend/backend coupled

## Build Order Implications

Based on dependencies between components, recommended build order:

### Phase 1: Foundation (no external dependencies)
1. **Backend skeleton:** Minimal API with health check endpoint
2. **React skeleton:** Vite app with routing, basic landing page
3. **Azure resources:** App Service, Key Vault (secrets manually added)

### Phase 2: Authentication (depends on Spotify developer account)
4. **Spotify OAuth flow:** Backend endpoints + frontend auth button
5. **Session management:** HTTP-only cookies, token storage
6. **Managed Identity:** Connect backend to Key Vault

### Phase 3: Artist Extraction (depends on Anthropic API key)
7. **Image upload:** Frontend component + backend endpoint
8. **Anthropic integration:** Service for vision API
9. **Festival name search:** Anthropic web search integration

### Phase 4: Playlist Creation (depends on Spotify OAuth working)
10. **Spotify search service:** Query artists, fetch top tracks
11. **Playlist creation service:** Create playlist, add tracks
12. **Result display:** Frontend page showing playlist link + artist list

### Phase 5: Production Readiness
13. **Background jobs:** Async processing for large lineups
14. **Error handling:** Retry logic, skipped artists tracking
15. **Azure Front Door:** CDN setup, custom domain
16. **Monitoring:** Application Insights, logging

**Key dependency notes:**
- Can't test Spotify playlist creation without OAuth working (Phase 2 must complete first)
- Can't test Anthropic integration without API key in Key Vault (Phase 1 Key Vault must exist)
- Background jobs (Phase 5) can be deferred for MVP; start with synchronous processing in Phase 4
- Azure Front Door (Phase 5) not required for MVP; can deploy backend + frontend to single App Service initially

## Sources

### Microsoft Official Documentation
- [Architect modern web applications with ASP.NET Core and Azure](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/)
- [React Web App with C# API and SQL Database on Azure](https://learn.microsoft.com/en-us/samples/azure-samples/todo-csharp-sql/todo-csharp-sql/)
- [Choose between controller-based APIs and minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis?view=aspnetcore-8.0)
- [Best practices for secrets management - Azure Key Vault](https://learn.microsoft.com/en-us/azure/key-vault/secrets/secrets-best-practices)
- [Azure Front Door Documentation](https://learn.microsoft.com/en-us/azure/frontdoor/)

### Spotify API Documentation
- [Authorization Code Flow | Spotify for Developers](https://developer.spotify.com/documentation/web-api/tutorials/code-flow)
- [OAuth Migration - November 27, 2025](https://developer.spotify.com/blog/2025-10-14-reminder-oauth-migration-27-nov-2025)

### Anthropic API Documentation
- [Anthropic Claude API Pricing 2026: Complete Cost Breakdown](https://www.metacto.com/blogs/anthropic-api-pricing-a-full-breakdown-of-costs-and-integration)
- [Claude Developer Platform](https://docs.anthropic.com/en/release-notes/api)

### Architecture Best Practices
- [Using OAuth for Single Page Applications | Best Practices](https://curity.io/resources/learn/spa-best-practices/)
- [Modern Authentication on .NET: OpenID Connect, BFF, SPA](https://docs.abblix.com/docs/net-authentication-openid-connect-bff-spa)
- [Background Jobs in .NET: Hangfire, Quartz, Temporal in 2026](https://medium.com/net-code-chronicles/background-jobs-schedulers-dotnet-abfbf49aa79f)
- [React Folder Structure in 5 Steps [2025]](https://www.robinwieruch.de/react-folder-structure/)

---
*Architecture research for: Conflicted Lineup - Festival lineup to Spotify playlist converter*
*Researched: 2026-01-24*
