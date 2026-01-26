import { useState, FormEvent, KeyboardEvent } from 'react';
import {
  TextField,
  Button,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  CircularProgress,
  Alert,
  InputAdornment,
  Box,
} from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import { searchFestivalLineup } from '../services/extractionApi';
import { ArtistInfo } from '../types/extraction';
import './FestivalSearch.css';

interface FestivalSearchProps {
  onSearchComplete: (artists: ArtistInfo[], festivalName: string, year: number, sources: string[]) => void;
  onSwitchToUpload: () => void;
}

export function FestivalSearch({ onSearchComplete, onSwitchToUpload }: FestivalSearchProps) {
  const currentYear = new Date().getFullYear();
  const [query, setQuery] = useState('');
  const [year, setYear] = useState(currentYear);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [statusText, setStatusText] = useState('');

  const handleSearch = async () => {
    if (!query.trim()) {
      setError('Please enter a festival name');
      return;
    }

    setLoading(true);
    setError(null);
    setStatusText(`Searching for ${query} ${year} lineup...`);

    try {
      const result = await searchFestivalLineup(query, year);

      if (result.artists.length === 0) {
        setError('No lineup found. Try uploading a poster instead.');
        setStatusText('');
        return;
      }

      setStatusText(`Found ${result.artists.length} artists!`);
      onSearchComplete(result.artists, result.festivalName, result.year, result.sources);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to search festival';
      setError(message);
      setStatusText('');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    handleSearch();
  };

  const handleKeyPress = (e: KeyboardEvent<HTMLDivElement>) => {
    if (e.key === 'Enter' && !loading) {
      handleSearch();
    }
  };

  return (
    <Box className="festival-search">
      <form className="search-form" onSubmit={handleSubmit}>
        <TextField
          fullWidth
          label="Festival Name"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          onKeyPress={handleKeyPress}
          disabled={loading}
          placeholder="e.g., Coachella, Bonnaroo, Glastonbury"
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <SearchIcon />
              </InputAdornment>
            ),
          }}
        />

        <FormControl className="year-select">
          <InputLabel>Year</InputLabel>
          <Select
            value={year}
            label="Year"
            onChange={(e) => setYear(Number(e.target.value))}
            disabled={loading}
          >
            <MenuItem value={currentYear}>{currentYear}</MenuItem>
            <MenuItem value={currentYear + 1}>{currentYear + 1}</MenuItem>
            <MenuItem value={currentYear + 2}>{currentYear + 2}</MenuItem>
          </Select>
        </FormControl>

        <Button
          variant="contained"
          onClick={handleSearch}
          disabled={loading || !query.trim()}
          className="search-button"
          startIcon={loading ? <CircularProgress size={20} /> : <SearchIcon />}
        >
          {loading ? 'Searching...' : 'Search'}
        </Button>
      </form>

      {statusText && !error && (
        <Box className="status-text">{statusText}</Box>
      )}

      {error && (
        <Alert
          severity="error"
          className="error-alert"
          action={
            <Button color="inherit" size="small" onClick={onSwitchToUpload}>
              Upload Poster
            </Button>
          }
        >
          {error}
        </Alert>
      )}
    </Box>
  );
}
