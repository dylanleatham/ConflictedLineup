import { describe, expect, it } from 'vitest';
import { readSseEvents } from './sse';

/** A response body that delivers exactly these chunks, as a network would */
function streamOf(...chunks: string[]): ReadableStream<Uint8Array> {
  const encoder = new TextEncoder();
  return new ReadableStream({
    start(controller) {
      chunks.forEach((chunk) => controller.enqueue(encoder.encode(chunk)));
      controller.close();
    },
  });
}

async function collect(stream: ReadableStream<Uint8Array>): Promise<unknown[]> {
  const events: unknown[] = [];
  for await (const event of readSseEvents(stream)) events.push(event);
  return events;
}

describe('readSseEvents', () => {
  it('parses one event per blank-line-terminated block', async () => {
    const events = await collect(streamOf('data: {"n":1}\n\ndata: {"n":2}\n\n'));

    expect(events).toEqual([{ n: 1 }, { n: 2 }]);
  });

  it('reassembles an event split across chunks', async () => {
    const events = await collect(streamOf('data: {"type":"prog', 'ress","current":3}\n', '\n'));

    expect(events).toEqual([{ type: 'progress', current: 3 }]);
  });

  it('reassembles a multi-byte character split across chunks', async () => {
    const bytes = new TextEncoder().encode('data: {"artist":"Beyoncé"}\n\n');
    const split = bytes.indexOf(0xc3) + 1; // between the two bytes of "é"
    const stream = new ReadableStream<Uint8Array>({
      start(controller) {
        controller.enqueue(bytes.slice(0, split));
        controller.enqueue(bytes.slice(split));
        controller.close();
      },
    });

    expect(await collect(stream)).toEqual([{ artist: 'Beyoncé' }]);
  });

  it('delivers a final event that has no trailing blank line', async () => {
    const events = await collect(streamOf('data: {"n":1}\n\ndata: {"type":"complete"}'));

    expect(events).toEqual([{ n: 1 }, { type: 'complete' }]);
  });

  it('ignores comments and other fields', async () => {
    const events = await collect(streamOf(': keep-alive\n\nevent: progress\nid: 7\ndata: {"n":1}\n\n'));

    expect(events).toEqual([{ n: 1 }]);
  });
});
