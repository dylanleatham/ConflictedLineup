import { useState, useEffect, useRef } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { Alert, CircularProgress, Collapse, IconButton } from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ExpandLessIcon from '@mui/icons-material/ExpandLess';
import MusicNoteIcon from '@mui/icons-material/MusicNote';
import StarIcon from '@mui/icons-material/Star';
import NewReleasesIcon from '@mui/icons-material/NewReleases';
import { useAuth } from '../auth';
import { selectTracksForArtists } from '../services/trackSelectionApi';
import { createPlaylist } from '../services/playlistApi';
import { PlaylistResultsState } from '../types/playlist';
import { TrackSelectionResponse, ArtistTrackResult, TrackInfo, SkippedArtist } from '../types/trackSelection';
import { ArtistInfo } from '../types/extraction';
import './TrackSelectionPage.css';

function formatDuration(ms: number): string {
  const minutes = Math.floor(ms / 60000);
  const seconds = Math.floor((ms % 60000) / 1000);
  return `${minutes}:${seconds.toString().padStart(2, '0')}`;
}

interface TrackListProps {
  tracks: TrackInfo[];
  category: 'familiar' | 'top' | 'recent';
}

function TrackList({ tracks, category }: TrackListProps) {
  if (tracks.length === 0) {
    return null;
  }

  const categoryConfig = {
    familiar: { icon: <MusicNoteIcon />, label: 'Familiar', color: '#1DB954' },
    top: { icon: <StarIcon />, label: 'Top', color: '#3b82f6' },
    recent: { icon: <NewReleasesIcon />, label: 'Recent', color: '#a855f7' },
  };

  const config = categoryConfig[category];

  return (
    <div className="track-category">
      <div className="category-header" style={{ color: config.color }}>
        {config.icon}
        <span>{config.label}</span>
        <span className="track-count">({tracks.length})</span>
      </div>
      <ul className="track-list">
        {tracks.map((track) => (
          <li key={track.spotifyTrackId} className="track-item">
            <span className="track-name">{track.name}</span>
            <span className="track-duration">{formatDuration(track.durationMs)}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

interface ArtistResultCardProps {
  result: ArtistTrackResult;
}

function ArtistResultCard({ result }: ArtistResultCardProps) {
  const totalTracks = result.familiarTracks.length + result.topTracks.length + result.recentTracks.length;

  return (
    <div className="artist-result-card">
      <div className="artist-result-header">
        <a
          href={`https://open.spotify.com/artist/${result.spotifyArtistId}`}
          target="_blank"
          rel="noopener noreferrer"
          className="artist-name-link"
        >
          {result.artistName}
        </a>
        <span className="artist-track-total">{totalTracks} tracks</span>
      </div>
      <div className="track-categories">
        <TrackList tracks={result.familiarTracks} category="familiar" />
        <TrackList tracks={result.topTracks} category="top" />
        <TrackList tracks={result.recentTracks} category="recent" />
      </div>
    </div>
  );
}

interface SkippedSectionProps {
  skipped: SkippedArtist[];
}

function SkippedSection({ skipped }: SkippedSectionProps) {
  const [expanded, setExpanded] = useState(false);

  if (skipped.length === 0) {
    return null;
  }

  return (
    <div className="skipped-section">
      <button
        className="skipped-toggle"
        onClick={() => setExpanded(!expanded)}
      >
        <span>Skipped Artists ({skipped.length})</span>
        <IconButton size="small">
          {expanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
        </IconButton>
      </button>
      <Collapse in={expanded}>
        <ul className="skipped-list">
          {skipped.map((artist, index) => (
            <li key={index} className="skipped-item">
              <span className="skipped-name">{artist.name}</span>
              <span className="skipped-reason">{artist.reason}</span>
            </li>
          ))}
        </ul>
      </Collapse>
    </div>
  );
}

export function TrackSelectionPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const { token } = useAuth();

  // Get artists and festival context from navigation state
  const navigationState = location.state as {
    artists?: ArtistInfo[];
    festivalName?: string;
    year?: number;
  } | undefined;

  const artists = navigationState?.artists;
  const festivalName = navigationState?.festivalName;
  const year = navigationState?.year;
  const artistNames = artists?.map((a) => a.name) || [];

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<TrackSelectionResponse | null>(null);
  const [creatingPlaylist, setCreatingPlaylist] = useState(false);

  // Track if we've already started fetching to prevent duplicate calls
  const hasFetched = useRef(false);

  const doFetch = async () => {
    if (!token || artistNames.length === 0) {
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await selectTracksForArtists(artistNames, token);
      setResult(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Track selection failed');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    // If no artists passed, redirect back to upload page
    if (!artists || artists.length === 0) {
      navigate('/');
      return;
    }

    // Only fetch once on mount
    if (hasFetched.current) {
      return;
    }
    hasFetched.current = true;

    doFetch();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleStartOver = () => {
    navigate('/');
  };

  const handleCreatePlaylist = async () => {
    if (!result || !token) return;

    setCreatingPlaylist(true);
    setError(null);

    try {
      const playlist = await createPlaylist(
        festivalName || 'My Festival Playlist',
        year,
        result.artists,
        token
      );

      // Navigate to results page with all context
      const resultsState: PlaylistResultsState = {
        playlist,
        artists: result.artists,
        skipped: result.skipped,
        festivalName,
        year,
      };

      navigate('/results', { state: resultsState });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create playlist');
      setCreatingPlaylist(false);
    }
  };

  const handleRetry = () => {
    doFetch();
  };

  // Calculate total tracks for summary
  const totalTracks = result
    ? result.artists.reduce(
        (sum, artist) =>
          sum +
          artist.familiarTracks.length +
          artist.topTracks.length +
          artist.recentTracks.length,
        0
      )
    : 0;

  return (
    <div className="track-selection-page">
      {loading && (
        <div className="loading-section">
          <CircularProgress size={48} sx={{ color: '#1DB954' }} />
          <h2>Selecting Tracks</h2>
          <p>Processing {artistNames.length} artists...</p>
          <p className="loading-hint">
            Finding familiar, top, and recent tracks for each artist
          </p>
        </div>
      )}

      {error && !loading && (
        <div className="error-section">
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
          <div className="error-actions">
            <button className="retry-button" onClick={handleRetry}>
              Try Again
            </button>
            <button className="back-button" onClick={handleStartOver}>
              Start Over
            </button>
          </div>
        </div>
      )}

      {result && !loading && (
        <div className="results-container">
          <div className="results-summary">
            <h1>{festivalName ? `${festivalName}${year ? ` ${year}` : ''}` : 'Track Selection Complete'}</h1>
            <p>
              Found <strong>{totalTracks} tracks</strong> from{' '}
              <strong>{result.artists.length} artists</strong>
            </p>
          </div>

          <div className="artist-results">
            {result.artists.map((artist) => (
              <ArtistResultCard key={artist.spotifyArtistId} result={artist} />
            ))}
          </div>

          <SkippedSection skipped={result.skipped} />

          <div className="action-section">
            <button className="start-over-button" onClick={handleStartOver}>
              Start Over
            </button>
            <button
              className="create-playlist-button"
              onClick={handleCreatePlaylist}
              disabled={totalTracks === 0 || creatingPlaylist}
            >
              {creatingPlaylist ? (
                <>
                  <CircularProgress size={20} color="inherit" />
                  Creating Playlist...
                </>
              ) : (
                'Create Playlist'
              )}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
