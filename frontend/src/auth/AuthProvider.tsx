import { AuthProvider as OAuthProvider } from 'react-oauth2-code-pkce';
import { spotifyAuthConfig } from './SpotifyAuthConfig';

interface AuthProviderProps {
  children: React.ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  return (
    <OAuthProvider authConfig={spotifyAuthConfig}>
      {children}
    </OAuthProvider>
  );
}
