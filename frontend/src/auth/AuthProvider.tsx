import { ReactNode, useEffect, useMemo, useState } from 'react';
import { AuthProvider as OAuthProvider, useAuthContext } from 'react-oauth2-code-pkce';
import { AuthContext } from './AuthContext';
import { spotifyAuthConfig } from './SpotifyAuthConfig';
import { AuthState, SpotifyProfile } from './types';

interface AuthProviderProps {
  demoMode: boolean;
  children: ReactNode;
}

/**
 * Spotify OAuth (PKCE) normally; in demo mode, a stand-in that signs in without leaving the page.
 * Either way the app sees the same AuthState through useAuth().
 */
export function AuthProvider({ demoMode, children }: AuthProviderProps) {
  if (demoMode) {
    return <DemoAuthProvider>{children}</DemoAuthProvider>;
  }

  return (
    <OAuthProvider authConfig={spotifyAuthConfig}>
      <SpotifyAuthBridge>{children}</SpotifyAuthBridge>
    </OAuthProvider>
  );
}

async function fetchSpotifyProfile(accessToken: string): Promise<SpotifyProfile> {
  const response = await fetch('https://api.spotify.com/v1/me', {
    headers: { Authorization: `Bearer ${accessToken}` },
  });

  if (!response.ok) {
    throw new Error(`Profile fetch failed: ${response.status}`);
  }

  return response.json();
}

function SpotifyAuthBridge({ children }: { children: ReactNode }) {
  const { token, login, logOut, loginInProgress, error } = useAuthContext();
  const [profile, setProfile] = useState<SpotifyProfile | null>(null);
  const [isLoadingProfile, setIsLoadingProfile] = useState(false);

  useEffect(() => {
    if (token && !profile) {
      setIsLoadingProfile(true);
      fetchSpotifyProfile(token)
        .then(setProfile)
        .catch(() => setProfile(null)) // Fall back to a generic avatar
        .finally(() => setIsLoadingProfile(false));
    }
  }, [token, profile]);

  const auth: AuthState = {
    isAuthenticated: !!token,
    isLoading: loginInProgress || isLoadingProfile,
    profile,
    error: error || null,
    login: () => login(),
    logout: () => logOut(),
    token: token || null,
  };

  return <AuthContext.Provider value={auth}>{children}</AuthContext.Provider>;
}

const DEMO_SESSION_KEY = 'conflicted-lineup-demo-session';

const demoProfile: SpotifyProfile = { id: 'demo', display_name: 'Demo Listener', images: [] };

function readDemoSession(): boolean {
  try {
    return sessionStorage.getItem(DEMO_SESSION_KEY) === '1';
  } catch {
    return false;
  }
}

function writeDemoSession(signedIn: boolean) {
  try {
    if (signedIn) sessionStorage.setItem(DEMO_SESSION_KEY, '1');
    else sessionStorage.removeItem(DEMO_SESSION_KEY);
  } catch {
    // Storage blocked: the demo session just won't survive a reload
  }
}

function DemoAuthProvider({ children }: { children: ReactNode }) {
  const [signedIn, setSignedIn] = useState(readDemoSession);

  const auth = useMemo<AuthState>(() => ({
    isAuthenticated: signedIn,
    isLoading: false,
    profile: signedIn ? demoProfile : null,
    error: null,
    login: () => {
      writeDemoSession(true);
      setSignedIn(true);
    },
    logout: () => {
      writeDemoSession(false);
      setSignedIn(false);
    },
    token: signedIn ? 'demo-token' : null,
  }), [signedIn]);

  return <AuthContext.Provider value={auth}>{children}</AuthContext.Provider>;
}
