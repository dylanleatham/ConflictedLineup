import { ArtistTrackResult, SkippedArtist } from './trackSelection';

/**
 * Request to create a Spotify playlist
 */
export interface PlaylistCreationRequest {
  festivalName: string;
  year?: number;
  artists: ArtistTrackResult[];
  spotifyAccessToken: string;
}

/**
 * Response from playlist creation API
 */
export interface PlaylistCreationResponse {
  playlistId: string;
  /** Null in demo mode, where no playlist is created */
  playlistUrl: string | null;
  playlistName: string;
  trackCount: number;
  artistCount: number;
}

/**
 * Data passed to results page via navigation state
 */
export interface PlaylistResultsState {
  playlist: PlaylistCreationResponse;
  artists: ArtistTrackResult[];
  skipped: SkippedArtist[];
  festivalName?: string;
  year?: number;
}
