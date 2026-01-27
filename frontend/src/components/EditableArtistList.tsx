import { TextField, Chip, Box, Typography } from '@mui/material';
import HelpOutlineIcon from '@mui/icons-material/HelpOutline';
import AddIcon from '@mui/icons-material/Add';
import CheckIcon from '@mui/icons-material/Check';
import CloseIcon from '@mui/icons-material/Close';
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
  const [newArtist, setNewArtist] = useState('');
  const [editingIndex, setEditingIndex] = useState<number | null>(null);
  const [editValue, setEditValue] = useState('');

  useEffect(() => {
    setArtists(initialArtists);
  }, [initialArtists]);

  const handleAddArtist = () => {
    const trimmed = newArtist.trim();
    if (trimmed && !artists.some(a => a.name.toLowerCase() === trimmed.toLowerCase())) {
      const updated = [...artists, { name: trimmed, confidence: 'high' as const }];
      setArtists(updated);
      onChange(updated);
      setNewArtist('');
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      handleAddArtist();
    }
  };

  const handleDelete = (index: number) => {
    const newArtists = artists.filter((_, i) => i !== index);
    setArtists(newArtists);
    onChange(newArtists);
  };

  const startEditing = (index: number) => {
    if (disabled) return;
    setEditingIndex(index);
    setEditValue(artists[index].name);
  };

  const cancelEditing = () => {
    setEditingIndex(null);
    setEditValue('');
  };

  const saveEdit = () => {
    if (editingIndex === null) return;

    const trimmed = editValue.trim();
    if (!trimmed) {
      // Empty name = delete
      handleDelete(editingIndex);
    } else {
      const updated = artists.map((artist, i) =>
        i === editingIndex
          ? { ...artist, name: trimmed, confidence: 'high' as const }
          : artist
      );
      setArtists(updated);
      onChange(updated);
    }
    setEditingIndex(null);
    setEditValue('');
  };

  const handleEditKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      saveEdit();
    } else if (e.key === 'Escape') {
      cancelEditing();
    }
  };

  return (
    <Box className="editable-artist-list">
      {/* Add artist input */}
      <Box className="add-artist-section">
        <TextField
          fullWidth
          size="small"
          value={newArtist}
          onChange={(e) => setNewArtist(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Add missing artist..."
          disabled={disabled}
          InputProps={{
            endAdornment: newArtist.trim() && (
              <AddIcon
                onClick={handleAddArtist}
                sx={{ cursor: 'pointer', color: '#1DB954', '&:hover': { color: '#1ed760' } }}
              />
            )
          }}
        />
        <Typography variant="caption" className="add-hint">
          Press Enter to add
        </Typography>
      </Box>

      {/* Artist chips */}
      <Box className="artist-chips-container">
        <Typography variant="subtitle2" className="chips-header">
          {artists.length} artist{artists.length !== 1 ? 's' : ''} in lineup
          <span className="edit-hint">Click to edit</span>
        </Typography>
        <Box className="artist-chips">
          {artists.map((artist, index) => (
            editingIndex === index ? (
              <Box key={`edit-${index}`} className="chip-edit-container">
                <input
                  type="text"
                  className="chip-edit-input"
                  value={editValue}
                  onChange={(e) => setEditValue(e.target.value)}
                  onKeyDown={handleEditKeyDown}
                  autoFocus
                />
                <CheckIcon
                  className="chip-edit-action save"
                  onClick={saveEdit}
                />
                <CloseIcon
                  className="chip-edit-action cancel"
                  onClick={cancelEditing}
                />
              </Box>
            ) : (
              <Chip
                key={`${artist.name}-${index}`}
                label={
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                    {artist.name}
                    {artist.confidence === 'uncertain' && (
                      <HelpOutlineIcon
                        sx={{ fontSize: 14, color: '#ff9800' }}
                        titleAccess="Uncertain extraction - verify spelling"
                      />
                    )}
                  </Box>
                }
                onClick={() => startEditing(index)}
                onDelete={disabled ? undefined : () => handleDelete(index)}
                className={artist.confidence === 'uncertain' ? 'uncertain-chip' : ''}
                size="small"
              />
            )
          ))}
        </Box>
      </Box>
    </Box>
  );
}
