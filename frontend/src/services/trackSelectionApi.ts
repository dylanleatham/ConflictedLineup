import { TrackSelectionResponse } from '../types/trackSelection';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:8080';

/**
 * Select tracks for artists using Spotify user's listening history.
 * Returns familiar, top, and recent tracks for each artist.
 */
export async function selectTracksForArtists(
  artistNames: string[],
  spotifyAccessToken: string
): Promise<TrackSelectionResponse> {
  const response = await fetch(`${API_BASE_URL}/api/tracks/select`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      artistNames,
      spotifyAccessToken,
    }),
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Track selection failed' }));
    throw new Error(error.error || 'Failed to select tracks for artists');
  }

  return response.json();
}
