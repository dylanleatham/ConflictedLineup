# Security policy

Conflicted Lineup is a personal project. Its Azure deployment has been retired and there are no
published releases, so the only supported version is `main`, run locally.

## Reporting a vulnerability

Please report security issues privately through GitHub's
[private vulnerability reporting](https://github.com/dylanleatham/ConflictedLineup/security/advisories/new)
rather than in a public issue. I'll acknowledge the report and follow up there.

## Scope and threat model

- **No accounts, no database.** The app stores nothing server-side. Each request carries the
  user's Spotify access token, which the API uses for that request only and never logs or persists.
- **Spotify sign-in is OAuth 2.0 with PKCE**, done entirely in the browser: there is no client
  secret to leak. The app asks for the two scopes it uses (`user-read-private` for the profile
  name, `playlist-modify-private` to create the playlist) and creates playlists as private.
  The Spotify client ID is public by design and is passed in at build time.
- **The Anthropic API key stays on the server**, read from the environment (a gitignored `.env`
  locally; a Container Apps secret when it was deployed). `.env.example` files hold placeholders
  only. Deploys used OpenID Connect, so no long-lived Azure credential was stored in GitHub.
- **Known gap:** the lineup endpoints (`/api/extraction/*`) spend the server's Anthropic credit and
  are not authenticated. That was acceptable for a deployment whose Spotify app was limited to an
  allow-list of users, but anyone redeploying this publicly should put those endpoints behind the
  Spotify sign-in (verify the token with `GET /v1/me`) and add ASP.NET Core rate limiting.
- **Uploads** are validated for type (PNG, JPEG, WebP) and size (5 MB) on both sides, downscaled in
  the browser, and sent to Claude; they are never written to disk.
