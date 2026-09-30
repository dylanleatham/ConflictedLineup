import { TAuthConfig } from 'react-oauth2-code-pkce';

// The keys react-oauth2-code-pkce persists tokens under
const TOKEN_STORAGE_KEYS = ['ROCP_token', 'ROCP_refreshToken', 'ROCP_idToken'];

export function clearStoredTokens() {
  TOKEN_STORAGE_KEYS.forEach((key) => localStorage.removeItem(key));
}

export const spotifyAuthConfig: TAuthConfig = {
  clientId: import.meta.env.VITE_SPOTIFY_CLIENT_ID || '',
  authorizationEndpoint: 'https://accounts.spotify.com/authorize',
  tokenEndpoint: 'https://accounts.spotify.com/api/token',
  redirectUri: `${window.location.origin}/callback`,
  // Only what the app uses: the profile name for the header, and creating a private playlist
  scope: 'user-read-private playlist-modify-private',
  decodeToken: false, // Spotify access tokens are opaque, not JWTs
  autoLogin: false, // Show the landing page first
  storage: 'local', // Stay signed in across browser sessions
  onRefreshTokenExpire: () => {
    // Clear tokens before redirecting, or the library retries the dead refresh token in a loop
    clearStoredTokens();
    window.location.href = '/?session_expired=true';
  },
};
