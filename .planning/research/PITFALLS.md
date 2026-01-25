# Pitfalls Research

**Domain:** Festival lineup to Spotify playlist web app with Anthropic vision API and Azure deployment
**Researched:** 2026-01-24
**Confidence:** HIGH

## Critical Pitfalls

### Pitfall 1: Spotify Extended Access Requirements Blocking Production Use

**What goes wrong:**
As of May 15, 2025, Spotify requires extended quota mode for public apps, demanding: officially registered business entity, app already live and public, minimum 250,000 active monthly users, and operation in major Spotify markets. Over 95% of applications fail these requirements.

**Why it happens:**
Developers start with Spotify's standard developer mode (50 requests/minute, 25 users) assuming they can scale later, unaware that "extended access" is essentially impossible for new/small apps.

**How to avoid:**
This app is friends-only (not public), so it can stay in Development Mode indefinitely. Development Mode supports up to 25 users - verify this is sufficient for your friend group size. If you need 26+ users, you're blocked by Spotify's policies.

**Warning signs:**
- Planning to add more than 25 friends
- Considering making the app public
- Hitting rate limits in Development Mode (50 RPM per user token)

**Phase to address:**
Phase 1 (OAuth setup) - Explicitly document Development Mode limitations and confirm user count stays under 25.

---

### Pitfall 2: Spotify OAuth Redirect URI Configuration Nightmare

**What goes wrong:**
Spotify's OAuth system underwent major security changes on November 27, 2025: (1) HTTP redirect URIs no longer allowed in production, (2) localhost aliases prohibited, (3) Implicit Grant flow removed. Additionally, the dashboard auto-transforms `http://127.0.0.1:8888/callback` to `http://localhost:8888/callback` on save, causing "Invalid redirect URI" errors.

**Why it happens:**
Developers configure redirect URIs in development that don't match production requirements, or the dashboard silently modifies their URIs. Azure App Service default URLs (e.g., `yourapp.azurewebsites.net`) must use HTTPS for Spotify OAuth.

**How to avoid:**
- **Development**: Use `http://127.0.0.1:PORT/callback` (loopback IP literal is still allowed)
- **Production**: Use HTTPS redirect URI with your Azure Front Door custom domain (`https://yourdomain.com/callback`)
- Configure BOTH URIs in Spotify dashboard before deploying
- Test OAuth flow in both environments before going live
- Never use `localhost` - always use `127.0.0.1` for local dev

**Warning signs:**
- "INVALID_CLIENT: Insecure redirect URI" errors
- "INVALID_CLIENT: Invalid redirect URI" errors
- OAuth works locally but fails in Azure
- Redirect URI in dashboard doesn't match what you typed

**Phase to address:**
Phase 2 (Spotify OAuth integration) and Phase 4 (Azure deployment) - Set up both dev and prod redirect URIs from the start.

---

### Pitfall 3: Spotify Refresh Token Invalidation Without Warning

**What goes wrong:**
Refresh tokens obtained through PKCE can be exchanged for an access token **only once**, after which they become invalid. Additionally, Spotify revokes refresh tokens when: user changes password, user revokes app access, or Spotify detects suspicious activity. Apps that don't handle "invalid_grant" errors gracefully will break silently.

**Why it happens:**
Developers assume refresh tokens work like other OAuth providers (reusable multiple times) or fail to implement proper error handling for token revocation scenarios.

**How to avoid:**
- Store refresh tokens securely (Azure Key Vault, never in browser localStorage)
- Implement proper error handling for `invalid_grant` errors → trigger re-authentication flow
- Add `no-cache` headers when requesting new tokens (prevents returning expired tokens from cache)
- For Authorization Code with PKCE: Understand that each token refresh returns a NEW refresh token - always update stored refresh token after each refresh
- Monitor for 401/403 errors and gracefully redirect users to re-authorize

**Warning signs:**
- Users report "logged out" randomly
- Token refresh endpoint returns `invalid_grant` errors
- Authentication works initially but fails after some time
- Users who changed Spotify password can't use your app

**Phase to address:**
Phase 2 (Spotify OAuth) - Build token refresh with proper error handling and re-auth flow from day one.

---

### Pitfall 4: Anthropic API Rate Limits with Tier-1 Constraints

**What goes wrong:**
Anthropic uses a 4-tier system based on cumulative credit purchases. Tier 1 ($5 deposit, $100 monthly limit) provides only 50 RPM and 30,000 ITPM (input tokens/minute) for Claude Sonnet 4.x. Organization-level enforcement means ALL API keys share the same limit pool. Festival poster parsing with vision API can easily exceed limits during peak usage.

**Why it happens:**
Developers don't understand that: (1) limits are organization-wide, not per-key, (2) image processing counts as input tokens (~1,600 tokens per poster image), (3) short bursts can trigger rate limits even if average usage is low (e.g., 60 RPM enforced as 1 request/second).

**How to avoid:**
- **Start Tier 2 immediately**: Deposit $40 to get 1,000 RPM (20x increase) - cost is minimal vs. debugging rate limit issues
- **Implement exponential backoff**: Use `retry-after` header from 429 responses (every 429 includes exact wait time)
- **Use prompt caching aggressively**: Cached tokens DON'T count toward ITPM for Sonnet 4.x - cache system prompts, tool definitions, and any repeated content
- **Monitor rate limit headers**: Check `anthropic-ratelimit-*` headers to detect approaching limits before hitting 429s
- **Understand token bucket algorithm**: Capacity continuously replenishes, but bursts can still exceed limits

**Warning signs:**
- 429 errors during testing with multiple friends
- `anthropic-ratelimit-tokens-remaining` approaching zero
- Time-to-first-token increasing (indicates rate limiting kicking in)
- Errors during peak usage times

**Phase to address:**
Phase 3 (Anthropic vision integration) - Start Tier 2, implement retry logic with exponential backoff, set up prompt caching.

---

### Pitfall 5: Anthropic Vision API Image Size Exceeds 5MB Limit

**What goes wrong:**
Anthropic API has a 5MB maximum image size limit (10MB on claude.ai web interface). Festival posters downloaded from web or user uploads often exceed 5MB, especially high-resolution promotional materials. The API rejects oversized images with errors.

**Why it happens:**
Developers assume the API handles image resizing automatically or don't validate image sizes before sending to Claude. High-DPI smartphone photos and marketing materials routinely exceed 5MB.

**How to avoid:**
- **Validate image size client-side** before upload: Reject or warn if >5MB
- **Resize images server-side**: Scale down to max 1568px on longest edge (optimal for Claude's tokenization - ~1,600 tokens)
- **Use appropriate quality settings**: JPEG quality 85-90 is sufficient for text recognition
- **For repeated images**: Use Files API (supports up to 500MB, reusable across requests) - upload once, reference by file_id
- **Consider format conversion**: Convert PNG to JPEG for photos (often 3-5x size reduction)

**Warning signs:**
- Image upload errors without clear messaging
- Some festival posters work, others fail mysteriously
- Users with iPhone 15 Pro Max photos failing (108MP camera = huge files)

**Phase to address:**
Phase 3 (Anthropic vision integration) - Add image validation and resizing before API calls.

---

### Pitfall 6: Azure App Service Node.js Port Configuration Failure

**What goes wrong:**
Azure App Service on Windows hosts Node.js apps with IISNode, requiring apps to listen on `process.env.PORT`. Apps hardcoded to port 3000 or 8080 won't receive traffic and show "You do not have permission to view this directory or page" errors.

**Why it happens:**
Developers copy local development server code that binds to fixed ports, unaware that Azure dynamically assigns ports via environment variables.

**How to avoid:**
```javascript
const port = process.env.PORT || 3000; // Always use process.env.PORT
app.listen(port, () => {
  console.log(`Server running on port ${port}`);
});
```
- Test with `PORT=8080 node server.js` locally to verify dynamic port handling
- Never hardcode ports in production code
- For Linux App Service, same pattern applies (different architecture but same requirement)

**Warning signs:**
- App works locally but shows errors in Azure
- Azure logs show "Application didn't respond" or timeout errors
- Health check pings failing

**Phase to address:**
Phase 1 (skeleton app) - Set up proper port handling before first Azure deployment.

---

### Pitfall 7: Azure Key Vault Managed Identity Permission Propagation Delay

**What goes wrong:**
After assigning a managed identity to App Service and granting Key Vault permissions, changes can take up to 24 hours to propagate due to backend caching. Apps fail with "access denied" errors despite correct RBAC configuration.

**Why it happens:**
Azure's managed identity service caches tokens per resource URI for ~24 hours. Developers expect instant permission propagation like AWS IAM, but Azure's architecture requires patience.

**How to avoid:**
- **Set up Key Vault access BEFORE deploying app code**: Configure managed identity + permissions, wait 15-30 minutes, verify with Azure CLI, then deploy
- **Use system-assigned identity by default**: Simpler than user-assigned for single-app scenarios
- **Verify with Azure CLI**:
  ```bash
  az webapp identity show --name <app> --resource-group <rg>
  az keyvault secret show --vault-name <vault> --name <secret>
  ```
- **For user-assigned identities**: Must explicitly set `keyVaultReferenceIdentity` app setting, or Key Vault references will use system-assigned identity and fail

**Warning signs:**
- "Access denied" errors from Key Vault despite correct role assignments shown in portal
- Intermittent failures accessing secrets (caching inconsistencies)
- Works after waiting several hours "for no reason"

**Phase to address:**
Phase 5 (Azure Key Vault integration) - Set up permissions at least 1 hour before first secret access attempt.

---

### Pitfall 8: Azure App Service Build Configuration Not Running npm install

**What goes wrong:**
When deploying via FTP/S, GitHub Actions, or Azure Pipelines, App Service Build Service (Oryx) is disabled by default. Dependencies aren't installed, and the app crashes with "Cannot find module" errors.

**Why it happens:**
Developers assume `npm install` runs automatically like Heroku. Azure only runs build automation for Git/Zip deployments with Kudu, not external build systems unless explicitly enabled.

**How to avoid:**
- **For GitHub Actions/Pipelines**: Add `SCM_DO_BUILD_DURING_DEPLOYMENT=true` app setting to enable Oryx
- **For Git/Zip with Kudu**: Build automation runs automatically (no action needed)
- **For FTP/S**: Manually upload `node_modules/` or switch to Git/Zip deployment
- **Best practice**: Use GitHub Actions with build step in workflow:
  ```yaml
  - run: npm ci --production
  - run: zip -r app.zip .
  - uses: azure/webapps-deploy@v2
  ```
- Include build tools (Grunt, Bower, Gulp) in `dependencies`, not `devDependencies`, if needed in production

**Warning signs:**
- "Cannot find module 'express'" or similar errors in logs
- App works locally but crashes immediately in Azure
- `node_modules/` folder missing in deployed files

**Phase to address:**
Phase 4 (Azure deployment) - Configure build settings before first deployment.

---

### Pitfall 9: Azure Front Door CORS Caching Breaks Multi-Origin Apps

**What goes wrong:**
Azure Front Door caches the `Access-Control-Allow-Origin` header for the first CORS origin. When a different origin makes a request, Front Door serves the cached header, causing CORS violations.

**Why it happens:**
Front Door's default behavior caches response headers per URL, not per Origin header. If `http://localhost:3000` requests first, the response includes `Access-Control-Allow-Origin: http://localhost:3000`. When `https://yourdomain.com` requests the same URL, Front Door returns the cached localhost header.

**How to avoid:**
- **Use Rules Engine to set CORS headers dynamically**:
  ```
  IF Origin header exists
  THEN Set Access-Control-Allow-Origin to {origin_header}
  ```
- **Purge cache after CORS configuration changes**: Stale headers persist until cache expires
- **For OPTIONS preflight**: Ensure Rules Engine handles OPTIONS method with proper CORS headers
- **For single origin**: Set `Access-Control-Allow-Origin: *` (wildcard) to avoid caching issues
- **Test with multiple origins** before going live (dev localhost + prod domain)

**Warning signs:**
- CORS works from localhost but fails from production domain (or vice versa)
- "Access-Control-Allow-Origin does not match" errors after app was working
- Errors appear only for some users, not others (different cached responses)

**Phase to address:**
Phase 6 (Azure Front Door setup) - Configure dynamic CORS headers from start.

---

## Moderate Pitfalls

### Pitfall 10: Spotify Playlist Scope Confusion (Public vs Private)

**What goes wrong:**
Creating playlists defaults to `public: true`, requiring `playlist-modify-public` scope. Developers request only `playlist-modify-private` scope, then API returns 403 "Insufficient Client Scope" errors. Additionally, `public: "false"` (string) vs `public: false` (boolean) causes unexpected behavior.

**Prevention:**
- Request BOTH scopes during OAuth: `playlist-modify-public playlist-modify-private`
- Explicitly set `public: false` (boolean) when creating playlists if you want private
- Understand that "public: false" still allows direct link access - it only hides from profile/search
- For collaborative playlists: MUST set `public: false` AND request both scopes

**Warning signs:**
- 403 errors when creating playlists despite having "playlist" scopes
- Playlist visibility not matching expectations
- String/boolean type confusion in API requests

**Phase to address:**
Phase 2 (Spotify OAuth) - Request both playlist scopes from start.

---

### Pitfall 11: Anthropic Prompt Caching Minimum Token Threshold

**What goes wrong:**
Claude 3.7 Sonnet requires at least 1,024 tokens per cache breakpoint. If you add a cache breakpoint before reaching 1,024 tokens, inference succeeds but the prefix isn't cached, wasting the caching overhead without benefits.

**Prevention:**
- Calculate token counts before setting cache breakpoints (system prompts + tool definitions typically exceed 1,024)
- For this app: Cache the vision system prompt + artist extraction instructions (likely 500+ tokens each = easily 1,024+)
- Use Files API for images (uploaded files don't need caching, already stored)
- Monitor cache hit rates in Anthropic Console to verify caching is working

**Warning signs:**
- Cache hit rate showing 0% despite setting cache breakpoints
- No cost savings from "cached" content
- Billing shows full input token costs

**Phase to address:**
Phase 3 (Anthropic integration) - Verify system prompt exceeds 1,024 tokens before enabling caching.

---

### Pitfall 12: Azure App Service Environment Variables Exposed in Logs

**What goes wrong:**
Application logging captures environment variables in stack traces and error messages. Secrets like Spotify Client Secret or Anthropic API keys appear in plaintext in Azure logs.

**Prevention:**
- Use Azure Key Vault references for all secrets:
  ```
  SPOTIFY_CLIENT_SECRET=@Microsoft.KeyVault(SecretUri=https://vault.vault.azure.net/secrets/spotify-secret/)
  ```
- Never log `process.env` directly
- Configure Application Insights to redact secrets:
  ```javascript
  appInsights.defaultClient.config.filterSensitiveData = true;
  ```
- Review logs before enabling public access to ensure no secrets leaked
- Use managed identity for Azure services (no keys needed)

**Warning signs:**
- Secrets visible in Azure Portal logs
- API keys in Application Insights telemetry
- Stack traces showing environment variable values

**Phase to address:**
Phase 5 (Azure Key Vault) - Migrate all secrets to Key Vault before logging is enabled.

---

### Pitfall 13: Node.js Version Mismatch Between Local and Azure

**What goes wrong:**
App works locally with Node 20 but Azure defaults to Node 18 LTS, causing syntax errors or missing API support.

**Prevention:**
- **Set Node version explicitly** in Azure:
  ```bash
  # Windows App Service
  az webapp config appsettings set --name <app> --resource-group <rg> --settings WEBSITE_NODE_DEFAULT_VERSION="~20"

  # Linux App Service
  az webapp config set --resource-group <rg> --name <app> --linux-fx-version "NODE|20-lts"
  ```
- Use tilde syntax (`~20`) for automatic patch updates
- Verify in Azure logs that correct version is running
- Test with same Node version locally (use `.nvmrc` or `.node-version`)

**Warning signs:**
- "Unexpected token" errors in Azure but not locally
- Features work locally but fail in Azure
- Logs show different Node version than expected

**Phase to address:**
Phase 4 (Azure deployment) - Set Node version in first deployment configuration.

---

### Pitfall 14: PM2 Configuration Missing `--no-daemon` Flag

**What goes wrong:**
Node.js 14+ on Azure App Service requires PM2 to run with `--no-daemon` flag. Without it, container exits immediately after starting app, causing continuous restart loops.

**Prevention:**
- Set startup command if using custom script:
  ```bash
  az webapp config set --resource-group <rg> --name <app> --startup-file "pm2 start app.js --no-daemon"
  ```
- Or ensure auto-detection works: name entry file `server.js`, `app.js`, or `index.js`
- For `process.json` or `ecosystem.config.js`: No manual config needed (auto-detected)

**Warning signs:**
- App starts then immediately crashes in Azure logs
- Container restart loop every few seconds
- "Application didn't respond to HTTP pings" errors

**Phase to address:**
Phase 4 (Azure deployment) - Configure PM2 startup in initial deployment.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Skip image resizing, send raw uploads to Claude | Faster implementation | 5MB limit errors, wasted tokens (cost), slow processing | Never - resizing is <20 lines of code |
| Store refresh tokens in browser localStorage | Simple client-side auth | Tokens stolen via XSS, users logged out randomly | Never - use httpOnly cookies or server-side session |
| Use wildcard CORS (`Access-Control-Allow-Origin: *`) | No origin management needed | Security risk if credentials used, some browsers block | Friends-only app with no sensitive data (acceptable) |
| Hardcode Spotify redirect URI in code | Fast development | Breaks when deploying to prod, can't test locally | Never - use environment variables |
| Skip prompt caching for Anthropic | Simpler initial implementation | 10x higher costs, slower responses, rate limit issues | MVP only - add caching by Phase 3 completion |
| Use Implicit Grant flow (deprecated) | Simpler OAuth flow | Removed by Spotify Nov 2025 - app will break | Never - use Authorization Code with PKCE |
| Store API keys in app settings, not Key Vault | Faster Azure setup | Secrets in logs, harder to rotate, less secure | Never - Key Vault is critical for production |
| Skip retry logic for 429 errors | Faster MVP | App breaks during peak usage, poor UX | MVP only - add before user testing |

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| Spotify OAuth | Using `localhost` in redirect URI | Use `http://127.0.0.1:PORT` for dev, `https://domain.com` for prod |
| Spotify Playlist Creation | Requesting only one scope (public OR private) | Request both `playlist-modify-public` and `playlist-modify-private` |
| Spotify Development Mode | Assuming you can scale to 100s of users | Accept 25-user limit, or don't use Spotify (extended access is impossible for new apps) |
| Anthropic Vision | Sending raw image uploads without validation | Validate size <5MB, resize to 1568px max, convert PNG→JPEG if needed |
| Anthropic Rate Limits | Staying in Tier 1 | Start Tier 2 ($40 deposit) immediately for 20x RPM increase |
| Azure App Service | Hardcoding port 3000 | Use `process.env.PORT` for dynamic port assignment |
| Azure Key Vault | Expecting instant permission propagation | Wait 15-30 min after role assignment before accessing secrets |
| Azure Front Door | Setting static CORS headers | Use Rules Engine to set headers dynamically based on Origin request header |

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Not caching Anthropic prompts | High API costs, slow responses, rate limit 429s | Cache system prompt + instructions (>1,024 tokens), monitor cache hit rate | Immediately in production (Tier 1 = 50 RPM total) |
| Sending full-resolution poster images | 429 errors, timeout errors, high costs | Resize to 1568px max before sending to Claude | 3-5 simultaneous users parsing posters |
| Synchronous festival lineup parsing | UI freeze, timeout errors | Use async/await, show progress indicator, consider WebSockets for real-time updates | Single user parsing 50+ artist lineup |
| Not implementing retry with exponential backoff | Random failures, poor UX during peak times | Use `retry-after` header, exponential backoff with jitter, max 3 retries | Peak usage (Fri/Sat evenings when festivals announced) |
| Storing all user playlists in memory | Memory leaks, crashes | Use database or Redis cache with TTL, paginate large result sets | 20+ concurrent users (approaching 25-user limit) |

## Security Mistakes

| Mistake | Risk | Prevention |
|---------|------|------------|
| Storing Spotify Client Secret in frontend code | Secret leaked in browser, impersonation attacks | Never send client secret to frontend - use backend OAuth flow only |
| Not validating Spotify OAuth state parameter | CSRF attacks, unauthorized access | Generate random state, store in session, validate on callback |
| Logging API responses with user data | PII exposure, GDPR violations | Sanitize logs, use structured logging with redaction, avoid logging full responses |
| Not using HTTPS for redirect URIs in production | Token interception, MITM attacks | Enforce HTTPS for all production OAuth redirects (Spotify requires this anyway) |
| Exposing Anthropic API key in frontend | Unlimited API usage, cost explosion | Use backend proxy, implement rate limiting per user, monitor for abuse |
| Not setting CORS origins properly | XSS attacks from malicious sites | Set specific allowed origins, never use `*` if credentials involved |
| Storing refresh tokens in localStorage | XSS token theft, session hijacking | Use httpOnly cookies or encrypted server-side storage |

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| No feedback during slow Claude vision processing | Users think app is broken (15-30s poster parsing) | Show progress indicator, explain "Analyzing poster...", consider streaming responses |
| Silent failure when 25-user limit reached | New friends can't use app, no error message | Detect user count, show clear error: "App limited to 25 users (Spotify restriction)" |
| No error handling for invalid poster images | "Something went wrong" with no context | Explain: "Poster image too large (max 5MB)" or "Can't read text in image - try higher quality" |
| Creating playlist without confirmation | Users don't know playlist name/URL | Show success message with direct link to Spotify playlist |
| Not explaining OAuth permissions | Users distrust app requesting broad scopes | Show explanation: "We need playlist permissions to create your festival playlist" |
| No handling for Spotify re-auth needed | Users stuck after token expires | Detect invalid_grant, show "Reconnect Spotify" button with clear explanation |
| Rate limit 429 shows raw error | Frustrating experience, users abandon | Show: "High traffic - retrying in 5 seconds..." with countdown |

## "Looks Done But Isn't" Checklist

- [ ] **OAuth flow**: Often missing state parameter CSRF validation — verify state matches between init and callback
- [ ] **Token refresh**: Often missing new refresh token persistence after PKCE refresh — verify updated token is saved
- [ ] **Image upload**: Often missing client-side size validation — verify 5MB check before API call
- [ ] **Playlist creation**: Often missing error handling for insufficient scopes — verify 403 scope errors show re-auth UI
- [ ] **Azure deployment**: Often missing NODE_ENV=production setting — verify production mode active
- [ ] **Key Vault**: Often missing managed identity role assignment verification — verify Key Vault access works before going live
- [ ] **CORS**: Often missing OPTIONS preflight handling — verify preflight succeeds from prod domain
- [ ] **Rate limiting**: Often missing retry-after header usage — verify 429 responses wait correct duration

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Spotify refresh token invalidated | LOW | Detect `invalid_grant` error → redirect user to re-authorize → store new tokens |
| Hit Anthropic Tier 1 rate limits in production | LOW | Immediate: Implement retry with `retry-after` header. Long-term: Deposit $40 for Tier 2 (takes effect instantly) |
| 5MB image uploaded to Claude | LOW | Catch error → resize image server-side → retry request automatically (transparent to user) |
| Exceeded 25 Spotify Development Mode users | HIGH | No recovery possible - either remove existing users or abandon Spotify integration (extended access unattainable) |
| CORS misconfigured on Azure Front Door | MEDIUM | Update Rules Engine to set dynamic headers → purge entire cache → retest from all origins (15-30 min downtime) |
| Key Vault permissions not propagated (24hr delay) | MEDIUM | Wait 15-30 minutes after role assignment → verify with Azure CLI → if still failing, recreate managed identity (rare) |
| Secrets leaked in Azure logs | HIGH | Rotate all exposed secrets immediately → purge logs → add Key Vault references → audit for data exposure |
| Node.js version mismatch crashes app | LOW | Set `WEBSITE_NODE_DEFAULT_VERSION` app setting → restart app (5 min fix) |

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Spotify Extended Access blocked | Phase 1 (Planning) | Confirm friends-only scope, verify <25 users needed |
| OAuth redirect URI misconfiguration | Phase 2 (Spotify OAuth) | Test OAuth from both `http://127.0.0.1` and `https://yourdomain.com` |
| Refresh token invalidation | Phase 2 (Spotify OAuth) | Trigger `invalid_grant` error manually, verify re-auth flow works |
| Playlist scope confusion | Phase 2 (Spotify OAuth) | Create both public and private playlists successfully |
| Anthropic rate limits (Tier 1) | Phase 3 (Vision integration) | Deposit $40 for Tier 2 before first deployment, verify tier in console |
| 5MB image size limit | Phase 3 (Vision integration) | Upload 10MB test image, verify automatic resize to <5MB |
| Prompt caching <1,024 tokens | Phase 3 (Vision integration) | Check cache hit rate in Console after first requests |
| Azure App Service port binding | Phase 4 (Deployment) | Deploy to Azure, verify health check succeeds |
| Build configuration (npm install) | Phase 4 (Deployment) | Check Azure logs show `npm install` ran, verify node_modules exists |
| Node.js version mismatch | Phase 4 (Deployment) | Check logs show Node 20, not 18 or 16 |
| PM2 daemon flag | Phase 4 (Deployment) | Verify app stays running, no restart loops in logs |
| Key Vault permission delay | Phase 5 (Key Vault) | Set up managed identity + permissions → wait 30 min → verify secret access |
| Secrets exposed in logs | Phase 5 (Key Vault) | Review logs before production, verify no plaintext secrets visible |
| Azure Front Door CORS caching | Phase 6 (Front Door) | Test from localhost:3000 AND prod domain, verify both work |

## Sources

### Spotify API
- [Updating the Criteria for Web API Extended Access](https://developer.spotify.com/blog/2025-04-15-updating-the-criteria-for-web-api-extended-access)
- [Reminder: OAuth Migration - 27 November 2025](https://developer.spotify.com/blog/2025-10-14-reminder-oauth-migration-27-nov-2025)
- [Spotify API Scopes Documentation](https://developer.spotify.com/documentation/web-api/concepts/scopes)
- [Authorization Code Flow](https://developer.spotify.com/documentation/web-api/tutorials/code-flow)
- [Redirect URIs](https://developer.spotify.com/documentation/web-api/concepts/redirect_uri)

### Anthropic API
- [Claude API Rate Limits](https://platform.claude.com/docs/en/api/rate-limits)
- [Claude Vision Documentation](https://platform.claude.com/docs/en/build-with-claude/vision)
- [Prompt Caching](https://platform.claude.com/docs/en/build-with-claude/prompt-caching)
- [Claude API Quota Tiers Guide 2026](https://www.aifreeapi.com/en/posts/claude-api-quota-tiers-limits)

### Azure App Service
- [Configure Node.js Apps - Azure App Service](https://learn.microsoft.com/en-us/azure/app-service/configure-language-nodejs)
- [Node.js Best Practices and Troubleshooting](https://learn.microsoft.com/en-us/azure/app-service/app-service-web-nodejs-best-practices-and-troubleshoot-guide)
- [Use Key Vault References as App Settings](https://learn.microsoft.com/en-us/azure/app-service/app-service-key-vault-references)
- [Managed Identities](https://learn.microsoft.com/en-us/azure/app-service/overview-managed-identity)

### Azure Front Door
- [Using Azure Front Door with CORS](https://learn.microsoft.com/en-us/azure/frontdoor/standard-premium/troubleshoot-cross-origin-resources)

---
*Pitfalls research for: Conflicted Lineup (Festival to Spotify Playlist App)*
*Researched: 2026-01-24*
