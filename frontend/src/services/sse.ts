/**
 * Yield the parsed JSON payload of each server-sent event in a response body.
 *
 * EventSource only supports GET, and track selection POSTs a list of artists, so the stream is read by hand.
 * Network chunks don't line up with events: an event can arrive split across chunks, or several in one.
 */
export async function* readSseEvents(body: ReadableStream<Uint8Array>): AsyncGenerator<unknown> {
  const reader = body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  try {
    while (true) {
      const { done, value } = await reader.read();
      buffer += done ? decoder.decode() : decoder.decode(value, { stream: true });

      // Events end with a blank line; keep any trailing partial event for the next chunk
      const events = buffer.split('\n\n');
      buffer = done ? '' : events.pop() ?? '';

      for (const event of events) {
        const data = event
          .split('\n')
          .filter((line) => line.startsWith('data:'))
          .map((line) => line.slice(5).trimStart())
          .join('\n');

        if (data) {
          yield JSON.parse(data);
        }
      }

      if (done) return;
    }
  } finally {
    reader.releaseLock();
  }
}
