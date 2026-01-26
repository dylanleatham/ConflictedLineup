import { useAuth } from '../auth/useAuth';
import { SpotifyLoginButton } from './SpotifyLoginButton';

export function LoginPage() {
  const { login, error, isLoading } = useAuth();

  return (
    <div className="login-page">
      <div className="login-container">
        <h1>Conflicted Lineup</h1>
        <p className="tagline">Turn any festival lineup into your personalized Spotify playlist</p>

        <div className="visual-example">
          <div className="poster-placeholder">
            <div className="poster-content">
              <div className="poster-title">Festival Poster</div>
              <div className="poster-artists">
                Artist A<br />
                Artist B<br />
                Artist C
              </div>
            </div>
          </div>

          <div className="arrow">→</div>

          <div className="playlist-preview">
            <div className="playlist-header">Your Playlist</div>
            <div className="track-item">
              <div className="track-info">
                <div className="track-name">Popular Song</div>
                <div className="track-artist">Artist A</div>
              </div>
            </div>
            <div className="track-item">
              <div className="track-info">
                <div className="track-name">Hit Track</div>
                <div className="track-artist">Artist B</div>
              </div>
            </div>
            <div className="track-item">
              <div className="track-info">
                <div className="track-name">Fan Favorite</div>
                <div className="track-artist">Artist C</div>
              </div>
            </div>
          </div>
        </div>

        <div className="explanation">
          <p>
            Upload a festival poster or search by name. We'll create a playlist with your
            familiar favorites, top tracks, and recent releases for each artist.
          </p>
        </div>

        <div className="why-spotify">
          <p>We need Spotify access to create your playlist and find tracks you already know.</p>
        </div>

        {error && (
          <div className="error-message">
            <p>Unable to connect to Spotify. Please try again.</p>
          </div>
        )}

        <div className="login-button-container">
          <SpotifyLoginButton onClick={login} isLoading={isLoading} />
        </div>
      </div>
    </div>
  );
}
