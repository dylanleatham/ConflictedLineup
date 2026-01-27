import { PlaylistCreationRequest, PlaylistCreationResponse } from '../types/playlist';
import { ArtistTrackResult } from '../types/trackSelection';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

/**
 * Create a Spotify playlist from track selection results
 */
export async function createPlaylist(
  festivalName: string,
  year: number | undefined,
  artists: ArtistTrackResult[],
  spotifyAccessToken: string
): Promise<PlaylistCreationResponse> {
  const request: PlaylistCreationRequest = {
    festivalName,
    year,
    artists,
    spotifyAccessToken,
  };

  const response = await fetch(`${API_URL}/api/playlist/create`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Unknown error' }));
    throw new Error(error.error || `Failed to create playlist: ${response.status}`);
  }

  return response.json();
}
