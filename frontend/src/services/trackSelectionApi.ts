import { TrackSelectionResponse, SSEEvent } from '../types/trackSelection';

import { apiBaseUrl } from '../config';

const API_BASE_URL = apiBaseUrl;

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

/**
 * Select tracks with SSE streaming for real-time progress updates.
 * Falls back to non-streaming endpoint on failure.
 */
export async function selectTracksStreaming(
  artistNames: string[],
  spotifyAccessToken: string,
  onProgress: (current: number, total: number, artistName: string, phase: string) => void
): Promise<TrackSelectionResponse> {
  const response = await fetch(`${API_BASE_URL}/api/tracks/select/stream`, {
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
    // Fall back to non-streaming endpoint
    return selectTracksForArtists(artistNames, spotifyAccessToken);
  }

  const reader = response.body?.getReader();
  if (!reader) {
    return selectTracksForArtists(artistNames, spotifyAccessToken);
  }

  const decoder = new TextDecoder();
  let buffer = '';

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });

    // Process complete SSE messages (delimited by double newline)
    const messages = buffer.split('\n\n');
    buffer = messages.pop() || '';

    for (const message of messages) {
      const dataLine = message.trim();
      if (!dataLine.startsWith('data: ')) continue;

      const json = dataLine.slice(6);
      let event: SSEEvent;
      try {
        event = JSON.parse(json);
      } catch {
        continue;
      }

      if (event.type === 'progress') {
        onProgress(event.current, event.total, event.artistName, event.phase);
      } else if (event.type === 'complete') {
        return event.result;
      } else if (event.type === 'error') {
        throw new Error(event.message);
      }
    }
  }

  // If we get here without a complete event, fall back
  return selectTracksForArtists(artistNames, spotifyAccessToken);
}
