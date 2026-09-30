/**
 * Information about a single track
 */
export interface TrackInfo {
  spotifyTrackId: string;
  name: string;
  artistName: string;
  albumName?: string;
  durationMs: number;
}

/**
 * Tracks selected for one artist: top tracks first, then recent singles
 */
export interface ArtistTrackResult {
  artistName: string;
  spotifyArtistId: string;
  popularity: number;
  topTracks: TrackInfo[];
  recentTracks: TrackInfo[];
}

/**
 * Artist that was skipped (no Spotify match found)
 */
export interface SkippedArtist {
  name: string;
  reason: string;
}

/**
 * Complete response from track selection API
 */
export interface TrackSelectionResponse {
  artists: ArtistTrackResult[];
  skipped: SkippedArtist[];
}

/**
 * Request body for track selection API
 */
export interface TrackSelectionRequest {
  artistNames: string[];
  spotifyAccessToken: string;
}

/**
 * SSE progress event during track selection
 */
export interface ProgressEvent {
  type: 'progress';
  current: number;
  total: number;
  artistName: string;
  phase: string;
}

/**
 * SSE complete event with final results
 */
export interface CompleteEvent {
  type: 'complete';
  result: TrackSelectionResponse;
}

/**
 * SSE error event
 */
export interface ErrorEvent {
  type: 'error';
  message: string;
}

export type SSEEvent = ProgressEvent | CompleteEvent | ErrorEvent;
