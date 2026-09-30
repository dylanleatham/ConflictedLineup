import { PlaylistCreationRequest, PlaylistCreationResponse } from '../types/playlist';
import { ArtistTrackResult } from '../types/trackSelection';
import { postJson } from './http';

/**
 * Create a private Spotify playlist from track selection results. The server appends the year
 * to the name unless the festival name already contains it.
 */
export function createPlaylist(
  festivalName: string,
  year: number | undefined,
  artists: ArtistTrackResult[],
  spotifyAccessToken: string
): Promise<PlaylistCreationResponse> {
  const request: PlaylistCreationRequest = { festivalName, year, artists, spotifyAccessToken };
  return postJson('/api/playlist/create', request, 'Failed to create playlist');
}
