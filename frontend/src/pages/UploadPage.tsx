import { useState, useRef } from 'react';
import { Alert, CircularProgress } from '@mui/material';
import CloudUploadIcon from '@mui/icons-material/CloudUpload';
import { EditableArtistList } from '../components/EditableArtistList';
import { ArtistInfo, ArtistExtractionResult } from '../types/extraction';
import { extractFromPoster, searchFestivalLineup } from '../services/extractionApi';
import { validateImageFile, optimizeImage } from '../utils/imageValidation';
import './UploadPage.css';

export function UploadPage() {
  const currentYear = new Date().getFullYear();

  // Input state
  const [festivalName, setFestivalName] = useState('');
  const [year, setYear] = useState(currentYear);
  const [posterFile, setPosterFile] = useState<File | null>(null);
  const [posterPreview, setPosterPreview] = useState<string | null>(null);

  // Processing state
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [statusText, setStatusText] = useState('');

  // Fallback state - show poster upload when search fails
  const [showPosterFallback, setShowPosterFallback] = useState(false);

  // Result state
  const [result, setResult] = useState<ArtistExtractionResult | null>(null);
  const [editedArtists, setEditedArtists] = useState<ArtistInfo[]>([]);

  const fileInputRef = useRef<HTMLInputElement>(null);

  const canSearch = festivalName.trim().length > 0;
  const canExtractFromPoster = posterFile !== null;

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    const validation = validateImageFile(file);
    if (!validation.valid) {
      setError(validation.error!);
      return;
    }

    setPosterFile(file);
    setError(null);

    // Show preview
    const reader = new FileReader();
    reader.onload = (e) => setPosterPreview(e.target?.result as string);
    reader.readAsDataURL(file);
  };

  const handleRemovePoster = () => {
    setPosterFile(null);
    setPosterPreview(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleSearch = async () => {
    if (!canSearch) return;

    setLoading(true);
    setError(null);

    try {
      setStatusText(`Searching for ${festivalName} ${year} lineup...`);
      const searchResult = await searchFestivalLineup(festivalName, year);

      const extractionResult: ArtistExtractionResult = {
        artists: searchResult.artists,
        festivalName: searchResult.festivalName,
        source: 'web',
        sourceUrl: searchResult.sources[0] || undefined,
      };

      if (extractionResult.artists.length === 0) {
        setError('No artists found for this festival.');
        setShowPosterFallback(true);
        setStatusText('');
        return;
      }

      setResult(extractionResult);
      setEditedArtists(extractionResult.artists);
      setStatusText('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Search failed');
      setShowPosterFallback(true);
      setStatusText('');
    } finally {
      setLoading(false);
    }
  };

  const handleExtractFromPoster = async () => {
    if (!canExtractFromPoster) return;

    setLoading(true);
    setError(null);

    try {
      setStatusText('Analyzing poster...');
      const base64Image = await optimizeImage(posterFile!);
      const extractionResult = await extractFromPoster(base64Image, posterFile!.type);

      if (extractionResult.artists.length === 0) {
        setError('No artists found in poster. Please try a clearer image.');
        setStatusText('');
        return;
      }

      setResult(extractionResult);
      setEditedArtists(extractionResult.artists);
      setStatusText('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Extraction failed');
      setStatusText('');
    } finally {
      setLoading(false);
    }
  };

  const handleStartOver = () => {
    setResult(null);
    setEditedArtists([]);
    setFestivalName('');
    setPosterFile(null);
    setPosterPreview(null);
    setError(null);
    setShowPosterFallback(false);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleContinue = () => {
    console.log('Continue with artists:', editedArtists);
    alert(`Ready to proceed with ${editedArtists.length} artists!\n\n(Track selection coming in Phase 4)`);
  };

  // Years for dropdown: next year through 10 years ago
  const years = Array.from({ length: 12 }, (_, i) => currentYear + 1 - i);

  return (
    <div className="upload-page">
      {!result ? (
        <>
          <div className="upload-header">
            <h1>Turn Any Festival Lineup Into a Playlist</h1>
            <p className="subtitle">
              Enter a festival name and we'll find the lineup and build you a personalized Spotify playlist.
            </p>
          </div>

          <div className="input-card">
            {/* Festival Name Input */}
            <div className="input-group">
              <label className="input-label">Festival Name</label>
              <div className="festival-input-row">
                <input
                  type="text"
                  className="text-input"
                  placeholder="e.g., Coachella, Bonnaroo, EDC Las Vegas"
                  value={festivalName}
                  onChange={(e) => setFestivalName(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                  disabled={loading}
                />
                <select
                  className="year-select"
                  value={year}
                  onChange={(e) => setYear(Number(e.target.value))}
                  disabled={loading}
                >
                  {years.map((y) => (
                    <option key={y} value={y}>{y}</option>
                  ))}
                </select>
              </div>
            </div>

            {/* Error message */}
            {error && (
              <Alert severity="error" sx={{ mt: 2 }}>
                {error}
              </Alert>
            )}

            {/* Status text */}
            {statusText && (
              <div className="status-text">{statusText}</div>
            )}

            {/* Search button */}
            {!showPosterFallback && (
              <button
                className="search-button"
                onClick={handleSearch}
                disabled={!canSearch || loading}
              >
                {loading ? (
                  <>
                    <CircularProgress size={20} color="inherit" />
                    Searching...
                  </>
                ) : (
                  'Find Lineup'
                )}
              </button>
            )}

            {/* Poster fallback - shown only after search fails */}
            {showPosterFallback && (
              <div className="poster-fallback">
                <div className="fallback-divider">
                  <span>Can't find it? Upload a poster</span>
                </div>

                <input
                  ref={fileInputRef}
                  type="file"
                  accept="image/png,image/jpeg,image/webp"
                  onChange={handleFileSelect}
                  style={{ display: 'none' }}
                  disabled={loading}
                />

                {!posterPreview ? (
                  <div
                    className="poster-dropzone"
                    onClick={() => fileInputRef.current?.click()}
                  >
                    <CloudUploadIcon className="upload-icon" />
                    <span>Click to upload poster image</span>
                    <span className="file-hint">PNG, JPG, or WebP up to 5MB</span>
                  </div>
                ) : (
                  <div className="poster-preview-container">
                    <img src={posterPreview} alt="Poster preview" className="poster-preview" />
                    <button
                      className="remove-poster-btn"
                      onClick={handleRemovePoster}
                      disabled={loading}
                    >
                      Remove
                    </button>
                  </div>
                )}

                <div className="fallback-actions">
                  <button
                    className="extract-button"
                    onClick={handleExtractFromPoster}
                    disabled={!canExtractFromPoster || loading}
                  >
                    {loading ? (
                      <>
                        <CircularProgress size={20} color="inherit" />
                        Analyzing...
                      </>
                    ) : (
                      'Extract from Poster'
                    )}
                  </button>
                  <button
                    className="try-again-button"
                    onClick={() => {
                      setShowPosterFallback(false);
                      setError(null);
                    }}
                    disabled={loading}
                  >
                    Try Different Search
                  </button>
                </div>
              </div>
            )}
          </div>
        </>
      ) : (
        <div className="results-section">
          <div className="results-header">
            <h2>{result.festivalName || 'Extracted Artists'}</h2>
            {result.source && (
              <span className="source-badge">
                {result.source === 'web' ? 'From web search' : 'From poster'}
              </span>
            )}
          </div>

          {posterPreview && (
            <div className="results-poster">
              <img src={posterPreview} alt="Festival poster" />
            </div>
          )}

          {result.warning && (
            <Alert severity="warning" sx={{ mb: 2 }}>
              {result.warning}
            </Alert>
          )}

          {result.sourceUrl && (
            <div className="source-link">
              Source: <a href={result.sourceUrl} target="_blank" rel="noopener noreferrer">
                {result.sourceUrl}
              </a>
            </div>
          )}

          <EditableArtistList
            initialArtists={result.artists}
            onChange={setEditedArtists}
          />

          {/* Poster fallback option on results */}
          {!posterPreview && (
            <div className="results-fallback">
              <button
                className="results-fallback-link"
                onClick={() => {
                  setResult(null);
                  setShowPosterFallback(true);
                }}
              >
                Artists unexpected? Upload a poster instead
              </button>
            </div>
          )}

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
