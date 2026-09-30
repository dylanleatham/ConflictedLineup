export interface SpotifyProfile {
  id: string;
  display_name: string | null;
  images: { url: string; height: number; width: number }[];
}

export interface AuthState {
  isAuthenticated: boolean;
  isLoading: boolean;
  profile: SpotifyProfile | null;
  error: string | null;
  login: () => void;
  logout: () => void;
  token: string | null;
}
