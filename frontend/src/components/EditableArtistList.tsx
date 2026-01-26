import { Autocomplete, TextField, Chip, Box } from '@mui/material';
import HelpOutlineIcon from '@mui/icons-material/HelpOutline';
import { useState, useEffect } from 'react';
import { ArtistInfo } from '../types/extraction';
import './EditableArtistList.css';

interface EditableArtistListProps {
  initialArtists: ArtistInfo[];
  onChange: (artists: ArtistInfo[]) => void;
  disabled?: boolean;
}

export function EditableArtistList({ initialArtists, onChange, disabled = false }: EditableArtistListProps) {
  const [artists, setArtists] = useState<ArtistInfo[]>(initialArtists);

  useEffect(() => {
    setArtists(initialArtists);
  }, [initialArtists]);

  const handleChange = (_event: any, newValue: (string | ArtistInfo)[]) => {
    // Convert any raw strings (new entries) to ArtistInfo objects
    const normalized: ArtistInfo[] = newValue.map(item => {
      if (typeof item === 'string') {
        return { name: item.trim(), confidence: 'high' as const };
      }
      return item;
    }).filter(item => item.name.length > 0);

    setArtists(normalized);
    onChange(normalized);
  };

  const handleDelete = (index: number) => {
    const newArtists = artists.filter((_, i) => i !== index);
    setArtists(newArtists);
    onChange(newArtists);
  };

  return (
    <Box className="editable-artist-list">
      <Autocomplete
        multiple
        freeSolo
        disabled={disabled}
        options={[]}
        value={artists}
        onChange={handleChange}
        getOptionLabel={(option) => typeof option === 'string' ? option : option.name}
        isOptionEqualToValue={(option, value) =>
          (typeof option === 'string' ? option : option.name) ===
          (typeof value === 'string' ? value : value.name)
        }
        renderInput={(params) => (
          <TextField
            {...params}
            label="Artists"
            placeholder="Type artist name and press Enter to add"
            helperText={`${artists.length} artist${artists.length !== 1 ? 's' : ''} - click X to remove`}
          />
        )}
        renderTags={(value, getTagProps) =>
          value.map((artist, index) => {
            const { key, ...tagProps } = getTagProps({ index });
            const artistInfo = typeof artist === 'string'
              ? { name: artist, confidence: 'high' as const }
              : artist;

            return (
              <Chip
                key={key}
                label={
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                    {artistInfo.name}
                    {artistInfo.confidence === 'uncertain' && (
                      <HelpOutlineIcon
                        sx={{ fontSize: 16, color: 'warning.main' }}
                        titleAccess="Uncertain extraction - verify spelling"
                      />
                    )}
                  </Box>
                }
                {...tagProps}
                onDelete={() => handleDelete(index)}
                className={artistInfo.confidence === 'uncertain' ? 'uncertain-chip' : ''}
              />
            );
          })
        }
      />
    </Box>
  );
}
