import { TrackSelectionResponse, SSEEvent } from '../types/trackSelection';
import { postJson, readError } from './http';
import { readSseEvents } from './sse';

type OnProgress = (current: number, total: number, artistName: string, phase: string) => void;

/**
 * Resolve each artist on Spotify and choose their tracks, reporting progress per artist as the
 * server streams it.
 */
export async function selectTracksStreaming(
  artistNames: string[],
  spotifyAccessToken: string,
  onProgress: OnProgress
): Promise<TrackSelectionResponse> {
  const response = await fetch('/api/tracks/select/stream', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ artistNames, spotifyAccessToken }),
  });

  if (!response.ok) {
    throw new Error(await readError(response, 'Failed to select tracks for artists'));
  }

  if (!response.body) {
    // No streaming support: fall back to the plain endpoint
    return postJson('/api/tracks/select', { artistNames, spotifyAccessToken }, 'Failed to select tracks for artists');
  }

  for await (const event of readSseEvents(response.body) as AsyncGenerator<SSEEvent>) {
    switch (event.type) {
      case 'progress':
        onProgress(event.current, event.total, event.artistName, event.phase);
        break;
      case 'complete':
        return event.result;
      case 'error':
        throw new Error(event.message);
    }
  }

  throw new Error('Track selection ended without a result. Try again.');
}
