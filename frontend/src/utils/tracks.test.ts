import { describe, expect, it } from 'vitest';
import { ArtistTrackResult, TrackInfo } from '../types/trackSelection';
import { artistTracks, festivalTitle, formatDuration, totalTracks } from './tracks';

const track = (id: string): TrackInfo => ({ spotifyTrackId: id, name: id, artistName: 'A', durationMs: 200_000 });

const artist = (top: string[], recent: string[]): ArtistTrackResult => ({
  artistName: 'A',
  spotifyArtistId: 'a',
  popularity: 50,
  topTracks: top.map(track),
  recentTracks: recent.map(track),
});

describe('tracks', () => {
  it('lists top tracks before recent singles', () => {
    expect(artistTracks(artist(['t1', 't2'], ['r1'])).map((t) => t.spotifyTrackId)).toEqual(['t1', 't2', 'r1']);
  });

  it('counts every track across artists', () => {
    expect(totalTracks([artist(['t1', 't2'], ['r1']), artist(['t3'], [])])).toBe(4);
    expect(totalTracks([])).toBe(0);
  });

  it.each([
    [215_000, '3:35'],
    [60_000, '1:00'],
    [59_999, '0:59'],
    [605_000, '10:05'],
  ])('formats %i ms as %s', (ms, expected) => {
    expect(formatDuration(ms)).toBe(expected);
  });

  it.each([
    ['Driftwood Valley', 2026, 'Driftwood Valley 2026'],
    ['Driftwood Valley 2026', 2026, 'Driftwood Valley 2026'],
    ['Driftwood Valley', undefined, 'Driftwood Valley'],
    [undefined, 2026, undefined],
  ])('titles %s + %s as %s', (name, year, expected) => {
    expect(festivalTitle(name, year)).toBe(expected);
  });
});
