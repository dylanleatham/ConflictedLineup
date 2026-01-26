export interface ArtistInfo {
  name: string;
  confidence: 'high' | 'uncertain';
}

export interface ArtistExtractionResult {
  artists: ArtistInfo[];
  warning?: string;
}

export interface FestivalSearchResult {
  festivalName: string;
  year: number;
  artists: ArtistInfo[];
  sources: string[];
}

export interface PosterExtractionRequest {
  imageBase64: string;
  mediaType: string;
}

export interface FestivalSearchRequest {
  festivalName: string;
  year?: number;
}
