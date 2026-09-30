import { ArtistTrackResult, TrackInfo } from '../types/trackSelection';

/** An artist's tracks in playlist order: top tracks, then recent singles */
export function artistTracks(artist: ArtistTrackResult): TrackInfo[] {
  return [...artist.topTracks, ...artist.recentTracks];
}

export function totalTracks(artists: ArtistTrackResult[]): number {
  return artists.reduce((sum, artist) => sum + artist.topTracks.length + artist.recentTracks.length, 0);
}

/** 215000 -> "3:35" */
export function formatDuration(ms: number): string {
  const totalSeconds = Math.floor(ms / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${seconds.toString().padStart(2, '0')}`;
}

/** "Driftwood Valley" + 2026 -> "Driftwood Valley 2026", without doubling a year already in the name */
export function festivalTitle(festivalName: string | undefined, year: number | undefined): string | undefined {
  if (!festivalName) return undefined;
  if (!year || festivalName.includes(String(year))) return festivalName;
  return `${festivalName} ${year}`;
}
