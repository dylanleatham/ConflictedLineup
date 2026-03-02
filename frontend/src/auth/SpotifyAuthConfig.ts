import { TAuthConfig } from 'react-oauth2-code-pkce';
import { basePath } from '../config';

export const spotifyAuthConfig: TAuthConfig = {
  clientId: import.meta.env.VITE_SPOTIFY_CLIENT_ID || '',
  authorizationEndpoint: 'https://accounts.spotify.com/authorize',
  tokenEndpoint: 'https://accounts.spotify.com/api/token',
  redirectUri: `${window.location.origin}${basePath}callback`,
  scope: 'user-read-private user-read-email playlist-modify-public playlist-modify-private playlist-read-private playlist-read-collaborative user-library-read',
  decodeToken: false, // CRITICAL - Spotify tokens are opaque, not JWTs
  autoLogin: false, // Show landing page first per CONTEXT.md
  storage: 'local', // Persist across browser sessions per CONTEXT.md
  onRefreshTokenExpire: () => {
    // Clear tokens from storage before redirecting to prevent refresh loop
    localStorage.removeItem('ROCP_token');
    localStorage.removeItem('ROCP_refreshToken');
    localStorage.removeItem('ROCP_idToken');
    // Redirect to login with session expired flag
    window.location.href = `${basePath}?session_expired=true`;
  }
};
