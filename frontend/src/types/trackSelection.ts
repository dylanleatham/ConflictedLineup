/**
 * Information about a single track
 */
export interface TrackInfo {
  spotifyTrackId: string;
  name: string;
  artistName: string;
  albumName?: string;
  durationMs: number;
  previewUrl?: string;
}

/**
 * Tracks selected for one artist with familiar/top/recent categories
 */
export interface ArtistTrackResult {
  artistName: string;
  spotifyArtistId: string;
  popularity: number;
  familiarTracks: TrackInfo[];
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
