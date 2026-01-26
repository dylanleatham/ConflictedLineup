import { useAuth } from '../auth';
import { SpotifyLoginButton } from './SpotifyLoginButton';

export function LoginPage() {
  const { login, isLoading, error } = useAuth();

  return (
    <div className="login-page">
      <div className="login-container">
        <h1 className="login-title">Conflicted Lineup</h1>
        <p className="login-tagline">
          Turn any festival lineup into your personalized Spotify playlist
        </p>

        <div className="visual-example">
          <div className="example-item poster-preview">
            <div className="poster-placeholder">Festival Poster</div>
          </div>
          <div className="example-arrow">→</div>
          <div className="example-item playlist-preview">
            <div className="playlist-mockup">
              <div className="playlist-item">🎵 Artist 1 - Top Track</div>
              <div className="playlist-item">🎵 Artist 2 - Popular Song</div>
              <div className="playlist-item">🎵 Artist 3 - Hit Single</div>
              <div className="playlist-item">🎵 Artist 4 - Recent Release</div>
            </div>
          </div>
        </div>

        <div className="login-explanation">
          <p>
            Upload a festival poster or search by name. We'll create a playlist with your
            familiar favorites, top tracks, and recent releases for each artist.
          </p>
          <p className="why-spotify">
            We need Spotify access to create your playlist and find tracks you already know.
          </p>
        </div>

        {error && (
          <div className="login-error">
            <p>Authentication failed. Please try again.</p>
          </div>
        )}

        <div className="login-button-container">
          <SpotifyLoginButton
            onClick={() => login()}
            isLoading={isLoading}
          />
        </div>
      </div>
    </div>
  );
}
