import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { Card, CardContent, Typography, Button, List, ListItem, ListItemText, Divider, Box } from '@mui/material';
import OpenInNewIcon from '@mui/icons-material/OpenInNew';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutline';
import { PlaylistResultsState } from '../types/playlist';
import { ArtistTrackResult, SkippedArtist } from '../types/trackSelection';
import { totalTracks } from '../utils/tracks';
import './PlaylistResultsPage.css';

function ArtistIncludedList({ artists }: { artists: ArtistTrackResult[] }) {
  return (
    <List dense className="artist-list">
      {artists.map((artist) => {
        const trackCount = totalTracks([artist]);
        return (
          <ListItem key={artist.spotifyArtistId} className="artist-list-item">
            <CheckCircleIcon className="artist-check-icon" />
            <ListItemText
              primary={artist.artistName}
              secondary={`${trackCount} tracks`}
            />
          </ListItem>
        );
      })}
    </List>
  );
}

function SkippedArtistList({ skipped }: { skipped: SkippedArtist[] }) {
  if (skipped.length === 0) {
    return null;
  }

  return (
    <List dense className="skipped-list">
      {skipped.map((artist, index) => (
        <ListItem key={index} className="skipped-list-item">
          <ErrorOutlineIcon className="skipped-icon" />
          <ListItemText
            primary={artist.name}
            secondary={artist.reason}
          />
        </ListItem>
      ))}
    </List>
  );
}

export function PlaylistResultsPage() {
  const location = useLocation();
  const navigate = useNavigate();

  const state = location.state as PlaylistResultsState | undefined;

  // Direct navigation or a reload loses the router state; start over
  if (!state) {
    return <Navigate to="/" replace />;
  }

  const { playlist, artists, skipped } = state;

  const handleStartNew = () => {
    navigate('/');
  };

  return (
    <div className="results-page">
      <Box className="results-grid">
        {/* Success banner - full width */}
        <Box className="grid-full-width">
          <Card className="success-card">
            <CardContent className="success-content">
              <CheckCircleIcon className="success-icon" />
              <Typography variant="h4" component="h1" className="success-title">
                Playlist Created!
              </Typography>
              <Typography variant="h6" className="playlist-name">
                {playlist.playlistName}
              </Typography>
              <Typography variant="body1" className="playlist-stats">
                {playlist.trackCount} tracks from {playlist.artistCount} artists
              </Typography>
              {playlist.playlistUrl ? (
                <Button
                  variant="contained"
                  size="large"
                  href={playlist.playlistUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  startIcon={<OpenInNewIcon />}
                  className="open-spotify-button"
                >
                  Open in Spotify
                </Button>
              ) : (
                <Typography variant="body2" className="demo-notice">
                  Demo mode: nothing was created in Spotify. With a Spotify login this
                  playlist would now be in your library.
                </Typography>
              )}
            </CardContent>
          </Card>
        </Box>

        {/* Artists included - full width when no skipped, else shares row */}
        <Box className={`grid-artists ${skipped.length === 0 ? 'grid-artists-full' : ''}`}>
          <Card className="artists-card">
            <CardContent>
              <Typography variant="h6" className="section-title">
                Artists Included ({artists.length})
              </Typography>
              <Divider sx={{ my: 1 }} />
              <ArtistIncludedList artists={artists} />
            </CardContent>
          </Card>
        </Box>

        {/* Skipped artists - 1/3 width on desktop, full on mobile */}
        {skipped.length > 0 && (
          <Box className="grid-skipped">
            <Card className="skipped-card">
              <CardContent>
                <Typography variant="h6" className="section-title">
                  Skipped ({skipped.length})
                </Typography>
                <Divider sx={{ my: 1 }} />
                <SkippedArtistList skipped={skipped} />
              </CardContent>
            </Card>
          </Box>
        )}

        {/* Start new button - full width */}
        <Box className="grid-full-width">
          <div className="action-section">
            <Button
              variant="outlined"
              size="large"
              onClick={handleStartNew}
              className="start-new-button"
            >
              Create Another Playlist
            </Button>
          </div>
        </Box>
      </Box>
    </div>
  );
}
