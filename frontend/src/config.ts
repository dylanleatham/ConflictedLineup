/**
 * Server-side settings the SPA needs before it renders, read once from /api/config at startup.
 */
export interface AppConfig {
  /** Fixture data instead of Claude and Spotify; the Spotify login is replaced by a demo sign-in. */
  demoMode: boolean;
}

export async function loadAppConfig(): Promise<AppConfig> {
  try {
    const response = await fetch('/api/config');
    if (response.ok) {
      const config = await response.json();
      return { demoMode: config.demoMode === true };
    }
  } catch {
    // Backend unreachable: render the normal app, whose API calls will surface the error
  }
  return { demoMode: false };
}
