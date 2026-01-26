import {
  ArtistExtractionResult,
  FestivalSearchResult,
  PosterExtractionRequest,
  FestivalSearchRequest,
} from '../types/extraction';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:8080';

/**
 * Extract artists from a festival poster image using Claude Vision API.
 */
export async function extractFromPoster(
  imageBase64: string,
  mediaType: string
): Promise<ArtistExtractionResult> {
  const request: PosterExtractionRequest = {
    imageBase64,
    mediaType,
  };

  const response = await fetch(`${API_BASE_URL}/api/extraction/poster`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to extract artists' }));
    throw new Error(error.error || 'Failed to extract artists from poster');
  }

  return response.json();
}

/**
 * Search for a festival lineup by name using Claude Web Search API.
 */
export async function searchFestivalLineup(
  festivalName: string,
  year?: number
): Promise<FestivalSearchResult> {
  const request: FestivalSearchRequest = {
    festivalName,
    year,
  };

  const response = await fetch(`${API_BASE_URL}/api/extraction/festival`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to search festival' }));
    throw new Error(error.error || 'Failed to search festival lineup');
  }

  return response.json();
}
