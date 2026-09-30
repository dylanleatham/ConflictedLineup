/**
 * POST JSON to the API and parse the JSON reply. Error replies carry `{ error: string }`,
 * which becomes the thrown Error's message.
 */
export async function postJson<T>(path: string, body: unknown, fallbackError: string): Promise<T> {
  const response = await fetch(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });

  if (!response.ok) {
    throw new Error(await readError(response, fallbackError));
  }

  return response.json();
}

export async function readError(response: Response, fallback: string): Promise<string> {
  const body = await response.json().catch(() => null);
  return typeof body?.error === 'string' ? body.error : fallback;
}
