import {
  ArtistExtractionResult,
  FestivalSearchResult,
  PosterExtractionRequest,
  FestivalSearchRequest,
} from '../types/extraction';
import { postJson } from './http';

/**
 * Extract artists from a festival poster image using Claude's vision, cross-checked against the web.
 */
export function extractFromPoster(imageBase64: string, mediaType: string): Promise<ArtistExtractionResult> {
  const request: PosterExtractionRequest = { imageBase64, mediaType };
  return postJson('/api/extraction/poster', request, 'Failed to extract artists from poster');
}

/**
 * Find a festival's lineup by name using Claude with web search.
 */
export function searchFestivalLineup(festivalName: string, year?: number): Promise<FestivalSearchResult> {
  const request: FestivalSearchRequest = { festivalName, year };
  return postJson('/api/extraction/festival', request, 'Failed to search festival lineup');
}
