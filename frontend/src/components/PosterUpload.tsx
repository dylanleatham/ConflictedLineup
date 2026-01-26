import { useState, useRef } from 'react';
import { Box, Button, Typography, LinearProgress, Alert } from '@mui/material';
import CloudUploadIcon from '@mui/icons-material/CloudUpload';
import { ArtistExtractionResult } from '../types/extraction';
import { validateImageFile, optimizeImage } from '../utils/imageValidation';
import { extractFromPoster } from '../services/extractionApi';
import './PosterUpload.css';

interface PosterUploadProps {
  onExtractionComplete: (result: ArtistExtractionResult) => void;
  onSwitchToSearch: () => void;
}

type ExtractionStatus = 'idle' | 'validating' | 'optimizing' | 'analyzing' | 'processing' | 'complete' | 'error';

const STATUS_MESSAGES: Record<ExtractionStatus, string> = {
  idle: '',
  validating: 'Validating image...',
  optimizing: 'Optimizing for analysis...',
  analyzing: 'Analyzing poster...',
  processing: 'Extracting artist names...',
  complete: 'Extraction complete!',
  error: 'Extraction failed'
};

export function PosterUpload({ onExtractionComplete, onSwitchToSearch }: PosterUploadProps) {
  const [preview, setPreview] = useState<string | null>(null);
  const [status, setStatus] = useState<ExtractionStatus>('idle');
  const [progress, setProgress] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFileSelect = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    // Reset state
    setError(null);
    setProgress(0);

    // Validate
    setStatus('validating');
    setProgress(10);
    const validation = validateImageFile(file);
    if (!validation.valid) {
      setError(validation.error!);
      setStatus('error');
      return;
    }

    // Show preview
    const reader = new FileReader();
    reader.onload = (e) => setPreview(e.target?.result as string);
    reader.readAsDataURL(file);

    try {
      // Optimize
      setStatus('optimizing');
      setProgress(25);
      const base64Image = await optimizeImage(file);

      // Extract
      setStatus('analyzing');
      setProgress(50);

      // Simulate progress updates (actual extraction is async)
      const progressInterval = setInterval(() => {
        setProgress(prev => Math.min(prev + 5, 90));
        setStatus(prev => prev === 'analyzing' ? 'processing' : prev);
      }, 1000);

      const result = await extractFromPoster(base64Image, file.type);

      clearInterval(progressInterval);
      setProgress(100);
      setStatus('complete');

      // Pass results to parent
      onExtractionComplete(result);
    } catch (err) {
      setStatus('error');
      setError(err instanceof Error ? err.message : 'Unknown error occurred');
    }

    // Reset file input for re-selection
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleClick = () => {
    fileInputRef.current?.click();
  };

  const isProcessing = ['validating', 'optimizing', 'analyzing', 'processing'].includes(status);

  return (
    <Box className="poster-upload">
      <input
        ref={fileInputRef}
        type="file"
        accept="image/png,image/jpeg,image/webp"
        onChange={handleFileSelect}
        style={{ display: 'none' }}
      />

      {!preview && status === 'idle' && (
        <Box className="upload-prompt" onClick={handleClick}>
          <CloudUploadIcon sx={{ fontSize: 64, color: 'rgba(255,255,255,0.5)', mb: 2 }} />
          <Typography variant="h6" gutterBottom>
            Upload Festival Poster
          </Typography>
          <Typography variant="body2" color="textSecondary">
            PNG, JPG, or WebP up to 5MB
          </Typography>
          <Button
            variant="contained"
            sx={{ mt: 2 }}
            onClick={handleClick}
          >
            Choose File
          </Button>
        </Box>
      )}

      {preview && (
        <Box className="preview-container">
          <img src={preview} alt="Poster preview" className="poster-preview" />

          {isProcessing && (
            <Box className="extraction-progress">
              <Typography variant="body2" sx={{ mb: 1 }}>
                {STATUS_MESSAGES[status]}
              </Typography>
              <LinearProgress variant="determinate" value={progress} />
            </Box>
          )}
        </Box>
      )}

      {status === 'error' && (
        <Alert
          severity="error"
          sx={{ mt: 2 }}
          action={
            <Box sx={{ display: 'flex', gap: 1 }}>
              <Button color="inherit" size="small" onClick={handleClick}>
                Try Different Image
              </Button>
              <Button color="inherit" size="small" onClick={onSwitchToSearch}>
                Search Instead
              </Button>
            </Box>
          }
        >
          {error}
        </Alert>
      )}

      {preview && status !== 'error' && !isProcessing && (
        <Button
          variant="outlined"
          onClick={handleClick}
          sx={{ mt: 2 }}
        >
          Upload Different Poster
        </Button>
      )}
    </Box>
  );
}
