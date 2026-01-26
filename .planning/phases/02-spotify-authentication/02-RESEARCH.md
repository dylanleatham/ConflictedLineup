# Phase 2: Spotify Authentication - Research

**Researched:** 2026-01-25
**Domain:** OAuth 2.0 PKCE Flow with Spotify API
**Confidence:** HIGH

## Summary

This phase implements Spotify OAuth authentication using the Authorization Code with PKCE flow (required since November 2025 - implicit grant is deprecated). The architecture involves a React frontend handling the OAuth redirect flow with automatic token refresh, and a .NET backend proxying Spotify API calls with secure token management.

The research confirms that Spotify has strict new security requirements effective November 2025: all apps must use Authorization Code flow with PKCE, HTTP redirect URIs are no longer allowed (except for localhost/127.0.0.1 during development), and the implicit grant flow is completely deprecated.

The recommended approach is a frontend-first OAuth flow using the `react-oauth2-code-pkce` library, which handles PKCE challenge generation, token storage, and automatic refresh. The backend stores the Client Secret in Azure Key Vault and can be used for server-side token refresh when needed for enhanced security.

**Primary recommendation:** Use `react-oauth2-code-pkce` for frontend OAuth handling with localStorage persistence, with automatic token refresh before expiration. Backend provides optional token refresh proxy endpoint for enhanced security.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| react-oauth2-code-pkce | latest | React OAuth PKCE provider | Provider-agnostic, built-in token refresh, localStorage/sessionStorage support |
| SpotifyAPI.Web | 7.2.1 | .NET Spotify API client | Official .NET library, full API coverage, typed responses |
| SpotifyAPI.Web.Auth | 7.2.1 | .NET Spotify auth helpers | PKCEUtil for code generation, token exchange helpers |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Azure.Security.KeyVault.Secrets | latest | Key Vault client | Storing Spotify Client ID/Secret securely |
| Azure.Identity | latest | Managed Identity auth | Accessing Key Vault from App Service |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| react-oauth2-code-pkce | Custom PKCE implementation | More control but significant complexity for code verifier, challenge, token refresh |
| react-oauth2-code-pkce | Auth0/Firebase | Overkill for single provider (Spotify), adds unnecessary dependency |
| localStorage | HttpOnly cookies | More secure but requires backend coordination for every auth check |

**Installation:**
```bash
# Frontend
npm install react-oauth2-code-pkce

# Backend (via NuGet)
dotnet add package SpotifyAPI.Web
dotnet add package SpotifyAPI.Web.Auth
dotnet add package Azure.Security.KeyVault.Secrets
dotnet add package Azure.Identity
```

## Architecture Patterns

### Recommended Project Structure
```
frontend/src/
├── auth/                    # Authentication module
│   ├── AuthProvider.tsx     # Wraps react-oauth2-code-pkce
│   ├── useAuth.ts           # Custom hook exposing auth state
│   ├── SpotifyAuthConfig.ts # OAuth configuration
│   └── AuthCallback.tsx     # Callback handling (if needed)
├── components/
│   ├── LoginPage.tsx        # Pre-auth landing with Spotify button
│   ├── SpotifyLoginButton.tsx # Branded Spotify button
│   ├── UserProfile.tsx      # Header profile with avatar
│   └── ProfileDropdown.tsx  # Logout menu
├── pages/
│   └── UploadPage.tsx       # Post-login destination
└── App.tsx                  # Route management

backend/src/ConflictedLineup.Api/
├── Auth/
│   ├── SpotifyAuthController.cs  # Token refresh endpoint
│   └── SpotifyTokenService.cs    # Token management
├── Services/
│   └── KeyVaultService.cs        # Secret retrieval
└── Program.cs                    # CORS, DI setup
```

### Pattern 1: Frontend-First PKCE Flow
**What:** The frontend handles the complete OAuth flow. Tokens stored in browser, auto-refreshed.
**When to use:** When user needs to make Spotify API calls directly from browser (not this app), or for simpler architecture.
**Example:**
```typescript
// Source: https://github.com/soofstad/react-oauth2-pkce
import { AuthProvider, TAuthConfig, useAuth } from "react-oauth2-code-pkce"

const authConfig: TAuthConfig = {
  clientId: import.meta.env.VITE_SPOTIFY_CLIENT_ID,
  authorizationEndpoint: 'https://accounts.spotify.com/authorize',
  tokenEndpoint: 'https://accounts.spotify.com/api/token',
  redirectUri: window.location.origin + '/callback',
  scope: 'user-read-private user-read-email playlist-modify-public playlist-modify-private',
  decodeToken: false, // Spotify tokens are opaque, not JWTs
  autoLogin: false,   // Don't auto-redirect; show landing page first
  storage: 'local',   // Persist across browser sessions
  onRefreshTokenExpire: (event) => {
    // User needs to re-authenticate
    window.location.href = '/login?expired=true';
  }
}

// In App.tsx
<AuthProvider authConfig={authConfig}>
  <App />
</AuthProvider>
```

### Pattern 2: Backend Token Proxy (Enhanced Security)
**What:** Frontend gets tokens, but token refresh happens through backend which has the Client Secret.
**When to use:** When you want defense-in-depth or future backend Spotify API calls.
**Example:**
```csharp
// Source: https://johnnycrazy.github.io/SpotifyAPI-NET/docs/pkce/
[ApiController]
[Route("api/auth")]
public class SpotifyAuthController : ControllerBase
{
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequest request)
    {
        var response = await new OAuthClient().RequestToken(
            new PKCETokenRefreshRequest(clientId, request.RefreshToken)
        );
        return Ok(new {
            accessToken = response.AccessToken,
            expiresIn = response.ExpiresIn,
            refreshToken = response.RefreshToken // May be new
        });
    }
}
```

### Pattern 3: Auth Context with Loading States
**What:** Wrap auth provider with custom context for loading/error states.
**When to use:** To handle the session restoration on page load gracefully.
**Example:**
```typescript
// Source: Community pattern
export function useSpotifyAuth() {
  const { token, tokenData, login, logOut, loginInProgress, error } = useAuth();

  const [profile, setProfile] = useState<SpotifyProfile | null>(null);
  const [isLoadingProfile, setIsLoadingProfile] = useState(false);

  useEffect(() => {
    if (token && !profile) {
      setIsLoadingProfile(true);
      fetchSpotifyProfile(token)
        .then(setProfile)
        .catch(() => setProfile(null)) // Use generic avatar
        .finally(() => setIsLoadingProfile(false));
    }
  }, [token]);

  return {
    isAuthenticated: !!token,
    isLoading: loginInProgress || isLoadingProfile,
    profile,
    login,
    logout: logOut,
    error
  };
}
```

### Anti-Patterns to Avoid
- **Storing tokens in React state only:** Tokens lost on page refresh; use localStorage via the library
- **Blocking UI on profile fetch failure:** Per CONTEXT.md, continue with generic avatar
- **Exposing Client Secret in frontend:** Never; use PKCE which doesn't require it
- **Using implicit grant flow:** Deprecated by Spotify as of November 2025
- **HTTP redirect URIs in production:** Only HTTPS allowed (except localhost for dev)

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| PKCE code challenge/verifier | Custom crypto generation | react-oauth2-code-pkce | Easy to get wrong, security-critical |
| Token storage with expiry | Manual localStorage with timers | react-oauth2-code-pkce storage | Race conditions, edge cases |
| Silent token refresh | setInterval with token refresh | Library's built-in refresh | Handles token rotation, expiry edge cases |
| Spotify API client | fetch() with manual auth headers | SpotifyAPI.Web | Typed responses, pagination, retry logic |
| Key Vault access | Manual HTTP to Key Vault API | Azure.Security.KeyVault.Secrets | Auth complexity, credential rotation |

**Key insight:** OAuth token management has many edge cases (refresh token rotation, race conditions during concurrent requests, expiry timing). Libraries handle these; custom solutions inevitably miss cases.

## Common Pitfalls

### Pitfall 1: Implicit Grant Still in Code
**What goes wrong:** App uses deprecated implicit flow, breaks after November 2025
**Why it happens:** Old tutorials and examples still show implicit grant
**How to avoid:** Use Authorization Code with PKCE exclusively; verify response_type is "code" not "token"
**Warning signs:** Access token appears in URL fragment (#access_token=...) instead of code parameter

### Pitfall 2: Token Refresh Race Condition
**What goes wrong:** Multiple simultaneous requests trigger multiple refresh attempts
**Why it happens:** Token expires while requests are in-flight
**How to avoid:** Use library with built-in refresh queuing; react-oauth2-code-pkce handles this
**Warning signs:** 401 errors followed by successful retries, intermittent auth failures

### Pitfall 3: Forgetting decodeToken: false
**What goes wrong:** Library tries to decode Spotify's opaque token as JWT, fails silently
**Why it happens:** Default behavior assumes JWT tokens
**How to avoid:** Set `decodeToken: false` in auth config; Spotify tokens are opaque
**Warning signs:** tokenData is undefined, auth appears to work but no decoded claims

### Pitfall 4: HTTP Redirect URI in Production
**What goes wrong:** OAuth flow fails with invalid_redirect_uri error
**Why it happens:** Spotify requires HTTPS for all production redirect URIs since November 2025
**How to avoid:** Use HTTPS in production; only localhost/127.0.0.1 can use HTTP
**Warning signs:** Works locally, fails when deployed

### Pitfall 5: Missing Scopes for Features
**What goes wrong:** API calls fail with 403 after seemingly successful auth
**Why it happens:** Didn't request required scopes during initial authorization
**How to avoid:** Request all needed scopes upfront: user-read-private, user-read-email, playlist-modify-public, playlist-modify-private
**Warning signs:** "Insufficient scope" errors in API responses

### Pitfall 6: Not Handling Refresh Token Expiration
**What goes wrong:** User session silently fails, stuck in broken state
**Why it happens:** Refresh token can expire or be revoked; no handling for this case
**How to avoid:** Implement onRefreshTokenExpire callback; redirect to login page per CONTEXT.md
**Warning signs:** Persistent 401 errors despite "authenticated" state

## Code Examples

Verified patterns from official sources:

### Spotify OAuth Configuration
```typescript
// Source: https://developer.spotify.com/documentation/web-api/tutorials/code-pkce-flow
// Combined with https://github.com/soofstad/react-oauth2-pkce

import { TAuthConfig } from "react-oauth2-code-pkce";

export const spotifyAuthConfig: TAuthConfig = {
  clientId: import.meta.env.VITE_SPOTIFY_CLIENT_ID,
  authorizationEndpoint: 'https://accounts.spotify.com/authorize',
  tokenEndpoint: 'https://accounts.spotify.com/api/token',
  redirectUri: import.meta.env.VITE_REDIRECT_URI,
  // Required scopes for this app
  scope: 'user-read-private user-read-email playlist-modify-public playlist-modify-private playlist-read-private',
  // Spotify tokens are opaque, not JWTs
  decodeToken: false,
  // Don't auto-redirect to login; show landing page first
  autoLogin: false,
  // Persist across browser sessions
  storage: 'local',
  // Handle refresh token expiration
  onRefreshTokenExpire: (event) => {
    // Per CONTEXT.md: redirect back to login page
    window.location.href = '/?session_expired=true';
  }
};
```

### Spotify Login Button (Brand Compliant)
```typescript
// Source: https://developer.spotify.com/documentation/design
// Spotify Green: #1ED760, minimum logo size: 21px for icon

interface SpotifyLoginButtonProps {
  onClick: () => void;
  disabled?: boolean;
}

export function SpotifyLoginButton({ onClick, disabled }: SpotifyLoginButtonProps) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      style={{
        backgroundColor: '#1ED760',
        color: '#000000',
        border: 'none',
        borderRadius: '500px', // Spotify uses pill-shaped buttons
        padding: '14px 32px',
        fontSize: '16px',
        fontWeight: 700,
        display: 'flex',
        alignItems: 'center',
        gap: '8px',
        cursor: disabled ? 'not-allowed' : 'pointer',
        opacity: disabled ? 0.6 : 1,
      }}
    >
      <SpotifyIcon size={24} /> {/* Minimum 21px per brand guidelines */}
      Log in with Spotify
    </button>
  );
}
```

### Fetching User Profile
```typescript
// Source: https://developer.spotify.com/documentation/web-api/reference/get-current-users-profile

interface SpotifyProfile {
  id: string;
  display_name: string | null;
  email: string;
  images: { url: string; height: number; width: number }[];
}

export async function fetchSpotifyProfile(accessToken: string): Promise<SpotifyProfile> {
  const response = await fetch('https://api.spotify.com/v1/me', {
    headers: {
      'Authorization': `Bearer ${accessToken}`
    }
  });

  if (!response.ok) {
    throw new Error(`Profile fetch failed: ${response.status}`);
  }

  return response.json();
}
```

### .NET Token Refresh (Backend)
```csharp
// Source: https://johnnycrazy.github.io/SpotifyAPI-NET/docs/pkce/
using SpotifyAPI.Web;

public class SpotifyTokenService
{
    private readonly string _clientId;

    public SpotifyTokenService(IConfiguration config)
    {
        _clientId = config["Spotify:ClientId"]
            ?? throw new ArgumentException("Spotify:ClientId not configured");
    }

    public async Task<PKCETokenResponse> RefreshTokenAsync(string refreshToken)
    {
        var request = new PKCETokenRefreshRequest(_clientId, refreshToken);
        var response = await new OAuthClient().RequestToken(request);
        return response;
    }
}
```

### Session Restoration on Page Load
```typescript
// The react-oauth2-code-pkce library handles this automatically
// when storage: 'local' is set. On mount, it:
// 1. Checks localStorage for existing tokens
// 2. If found and not expired, restores session
// 3. If expired, attempts silent refresh
// 4. Calls onRefreshTokenExpire if refresh fails

// In your App.tsx, just check isLoading:
function App() {
  const { isAuthenticated, isLoading } = useSpotifyAuth();

  if (isLoading) {
    return <LoadingSpinner />;
  }

  return isAuthenticated ? <UploadPage /> : <LoginPage />;
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Implicit Grant (response_type=token) | Authorization Code + PKCE | November 2025 | Apps using implicit grant no longer work |
| HTTP redirect URIs | HTTPS only (except localhost) | November 2025 | Production apps must use HTTPS |
| Client Secret in SPA | PKCE (no secret needed) | 2020 | Enhanced security for public clients |
| Manual token refresh | Library-managed silent refresh | Ongoing | Better UX, fewer edge cases |

**Deprecated/outdated:**
- Implicit Grant flow: Fully deprecated, returns error
- `response_type=token`: No longer supported
- HTTP redirect URIs: Only localhost/127.0.0.1 allowed for development
- Localhost aliases (e.g., myapp.local): No longer permitted

## Open Questions

Things that couldn't be fully resolved:

1. **Exact Spotify brand assets for login button**
   - What we know: Green is #1ED760, minimum icon size 21px, pill-shaped buttons common
   - What's unclear: Whether Spotify provides official SVG/component for "Log in with Spotify" button
   - Recommendation: Download official assets from https://newsroom.spotify.com/media-kit/logo-and-brand-assets/, use the icon with custom button styling

2. **Token refresh timing strategy**
   - What we know: Access tokens expire after 3600 seconds (1 hour)
   - What's unclear: Optimal pre-emptive refresh timing (library handles this, but we could configure)
   - Recommendation: Use library defaults; they refresh before expiration automatically

3. **Rate limits on token refresh**
   - What we know: Spotify has rate limits on API calls
   - What's unclear: Whether token refresh endpoint has separate limits
   - Recommendation: Monitor for 429 responses; implement exponential backoff if needed

## Sources

### Primary (HIGH confidence)
- [Spotify Authorization Code with PKCE Flow](https://developer.spotify.com/documentation/web-api/tutorials/code-pkce-flow) - Official PKCE documentation
- [Spotify Token Refresh](https://developer.spotify.com/documentation/web-api/tutorials/refreshing-tokens) - Official refresh documentation
- [Spotify Scopes](https://developer.spotify.com/documentation/web-api/concepts/scopes) - Required scopes for API access
- [Spotify Security Requirements Update](https://developer.spotify.com/blog/2025-02-12-increasing-the-security-requirements-for-integrating-with-spotify) - November 2025 migration requirements
- [SpotifyAPI-NET PKCE](https://johnnycrazy.github.io/SpotifyAPI-NET/docs/pkce/) - .NET library PKCE documentation
- [react-oauth2-pkce GitHub](https://github.com/soofstad/react-oauth2-pkce) - React library documentation

### Secondary (MEDIUM confidence)
- [Spotify Design Guidelines](https://developer.spotify.com/documentation/design) - Brand requirements for buttons
- [Spotify Brand Assets](https://newsroom.spotify.com/media-kit/logo-and-brand-assets/) - Official logo downloads
- [Auth0 Token Storage Guide](https://auth0.com/docs/secure/security-guidance/data-security/token-storage) - Best practices for browser token storage

### Tertiary (LOW confidence)
- Community blog posts on PKCE implementation patterns (various)
- Stack Overflow discussions on token refresh timing (anecdotal)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Official Spotify docs + established libraries
- Architecture: HIGH - Standard OAuth patterns, verified with library docs
- Pitfalls: HIGH - Documented in Spotify migration guide and community issues

**Research date:** 2026-01-25
**Valid until:** 2026-02-25 (30 days - OAuth standards are stable)
