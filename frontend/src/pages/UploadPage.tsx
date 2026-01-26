import { useState } from 'react';
import { Alert } from '@mui/material';
import { PosterUpload } from '../components/PosterUpload';
import { FestivalSearch } from '../components/FestivalSearch';
import { EditableArtistList } from '../components/EditableArtistList';
import { ArtistInfo } from '../types/extraction';
import './UploadPage.css';

type Mode = 'upload' | 'search';

interface ExtractionResult {
  artists: ArtistInfo[];
  warning?: string;
  festivalName?: string;
  year?: number;
  sources?: string[];
}

export function UploadPage() {
  const [mode, setMode] = useState<Mode>('upload');
  const [result, setResult] = useState<ExtractionResult | null>(null);
  const [editedArtists, setEditedArtists] = useState<ArtistInfo[]>([]);

  const handlePosterComplete = (artists: ArtistInfo[], warning?: string) => {
    setResult({ artists, warning });
    setEditedArtists(artists);
  };

  const handleSearchComplete = (
    artists: ArtistInfo[],
    festivalName: string,
    year: number,
    sources: string[]
  ) => {
    setResult({ artists, festivalName, year, sources });
    setEditedArtists(artists);
  };

  const handleStartOver = () => {
    setResult(null);
    setEditedArtists([]);
  };

  const handleContinue = () => {
    // TODO: Navigate to track selection page in Phase 4
    console.log('Continue with artists:', editedArtists);
    alert(`Ready to proceed with ${editedArtists.length} artists!\n\n(Track selection will be implemented in Phase 4)`);
  };

  const switchToSearch = () => setMode('search');
  const switchToUpload = () => setMode('upload');

  return (
    <div className="upload-page">
      <div className="upload-header">
        <h1>Create Your Festival Playlist</h1>
        <p className="subtitle">Upload a poster or search for a festival</p>
      </div>

      {!result && (
        <>
          <div className="mode-tabs">
            <button
              className={`mode-tab ${mode === 'upload' ? 'active' : ''}`}
              onClick={() => setMode('upload')}
            >
              Upload Poster
            </button>
            <button
              className={`mode-tab ${mode === 'search' ? 'active' : ''}`}
              onClick={() => setMode('search')}
            >
              Search Festival
            </button>
          </div>

          <div className="component-container">
            {mode === 'upload' ? (
              <PosterUpload
                onExtractionComplete={handlePosterComplete}
                onSwitchToSearch={switchToSearch}
              />
            ) : (
              <FestivalSearch
                onSearchComplete={handleSearchComplete}
                onSwitchToUpload={switchToUpload}
              />
            )}
          </div>
        </>
      )}

      {result && (
        <div className="results-section">
          <div className="results-header">
            <h2>
              {result.festivalName && result.year
                ? `${result.festivalName} ${result.year}`
                : 'Extracted Artists'}
            </h2>
            <span className="artist-count">
              {editedArtists.length} artist{editedArtists.length !== 1 ? 's' : ''}
            </span>
          </div>

          {result.warning && (
            <div className="warning-message">
              <Alert severity="warning">{result.warning}</Alert>
            </div>
          )}

          {result.sources && result.sources.length > 0 && (
            <div className="sources-section">
              <h3>Sources</h3>
              <ul className="sources-list">
                {result.sources.map((source, index) => (
                  <li key={index}>
                    {source.startsWith('http') ? (
                      <a href={source} target="_blank" rel="noopener noreferrer">
                        {source}
                      </a>
                    ) : (
                      source
                    )}
                  </li>
                ))}
              </ul>
            </div>
          )}

          <EditableArtistList
            initialArtists={result.artists}
            onChange={setEditedArtists}
          />

          <div className="continue-section">
            <button className="start-over-button" onClick={handleStartOver}>
              Start Over
            </button>
            <button
              className="continue-button"
              onClick={handleContinue}
              disabled={editedArtists.length === 0}
            >
              Continue to Track Selection
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
