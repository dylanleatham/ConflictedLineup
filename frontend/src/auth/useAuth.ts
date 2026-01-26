import { useState, useEffect } from 'react';
import { useAuthContext } from 'react-oauth2-code-pkce';
import { SpotifyProfile, AuthState } from './types';

async function fetchSpotifyProfile(accessToken: string): Promise<SpotifyProfile> {
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

export function useAuth(): AuthState {
  const { token, login, logOut, loginInProgress, error } = useAuthContext();
  const [profile, setProfile] = useState<SpotifyProfile | null>(null);
  const [isLoadingProfile, setIsLoadingProfile] = useState(false);

  useEffect(() => {
    if (token && !profile) {
      setIsLoadingProfile(true);
      fetchSpotifyProfile(token)
        .then((data) => setProfile(data))
        .catch(() => setProfile(null)) // Per CONTEXT.md: continue with generic avatar
        .finally(() => setIsLoadingProfile(false));
    }
  }, [token, profile]);

  return {
    isAuthenticated: !!token,
    isLoading: loginInProgress || isLoadingProfile,
    profile,
    error: error || null,
    login,
    logout: logOut, // Rename for consistency
    token
  };
}
