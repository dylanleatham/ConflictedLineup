import { TAuthConfig } from 'react-oauth2-code-pkce';

export const spotifyAuthConfig: TAuthConfig = {
  clientId: import.meta.env.VITE_SPOTIFY_CLIENT_ID || '',
  authorizationEndpoint: 'https://accounts.spotify.com/authorize',
  tokenEndpoint: 'https://accounts.spotify.com/api/token',
  redirectUri: `${window.location.origin}/callback`,
  scope: 'user-read-private user-read-email playlist-modify-public playlist-modify-private playlist-read-private',
  decodeToken: false, // CRITICAL - Spotify tokens are opaque, not JWTs
  autoLogin: false, // Show landing page first per CONTEXT.md
  storage: 'local', // Persist across browser sessions per CONTEXT.md
  onRefreshTokenExpire: () => {
    // Redirect to login with session expired flag
    window.location.href = '/?session_expired=true';
  }
};
