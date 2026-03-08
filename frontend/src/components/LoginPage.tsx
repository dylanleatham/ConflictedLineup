import { useAuth } from '../auth';
import { SpotifyLoginButton } from './SpotifyLoginButton';

export function LoginPage() {
  const { login, isLoading, error } = useAuth();

  return (
    <div className="login-page">
      <div className="login-container">
        <h1 className="login-title">CONFLICTED LINEUP</h1>
        <p className="login-tagline">
          Turn any festival lineup into your personalized Spotify playlist
        </p>

        <div className="visual-example">
          {/* Example Festival Poster */}
          <div className="example-poster">
            <div className="poster-header">BASS CANYON</div>
            <div className="poster-year">2024</div>
            <div className="poster-lineup">
              <span className="headliner">EXCISION</span>
              <span className="artist">SUBTRONICS</span>
              <span className="artist">WOOLI</span>
              <span className="artist">SVDDEN DEATH</span>
              <span className="artist-small">KAI WACHI • LEVEL UP • JANTSEN</span>
              <span className="artist-small">AUTOMHATE • SAMPLIFIRE • MORE</span>
            </div>
            <div className="poster-footer">THE GORGE • AUG 16-18</div>
          </div>

          <div className="example-arrow">→</div>

          {/* Playlist Preview */}
          <div className="example-playlist">
            <div className="playlist-header-row">
              <span className="playlist-icon">●</span>
              <span>Bass Canyon 2024</span>
            </div>
            <div className="playlist-tracks">
              <div className="track-row">
                <span className="track-title">Rumble</span>
                <span className="track-artist">Excision</span>
              </div>
              <div className="track-row">
                <span className="track-title">Griztronics</span>
                <span className="track-artist">Subtronics</span>
              </div>
              <div className="track-row">
                <span className="track-title">Mammoth</span>
                <span className="track-artist">Wooli</span>
              </div>
              <div className="track-row">
                <span className="track-title">Behemoth</span>
                <span className="track-artist">SVDDEN DEATH</span>
              </div>
            </div>
            <div className="playlist-footer">147 tracks • 9h 23m</div>
          </div>
        </div>

        <div className="login-explanation">
          <p>
            Search for any festival and we'll build you a playlist with
            top tracks and recent releases from every artist on the lineup.
          </p>
          <p className="why-spotify">
            Connect with Spotify to create playlists and discover new music.
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
