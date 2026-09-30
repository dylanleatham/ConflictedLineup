/**
 * Captures the README screenshots and demo GIF from the app running in demo mode.
 *
 *   docker compose up --build            (from the repo root; serves http://localhost:8080)
 *   npm run screenshots                  (from frontend/)
 *
 * Drives an installed Edge or Chrome through playwright-core, so no browser download.
 * The GIF is made from Playwright's screen recording with ffmpeg, which must be on PATH.
 */
import { execFileSync } from 'node:child_process';
import { mkdir, readdir, rm } from 'node:fs/promises';
import path from 'node:path';
import { chromium } from 'playwright-core';
import sharp from 'sharp';

const BASE = process.env.DEMO_URL ?? 'http://localhost:8080';
const OUT = path.resolve('..', 'docs', 'screenshots');
const VIDEO_DIR = path.join(OUT, '.video');
const DESKTOP = { width: 1280, height: 800 };
const PHONE = { width: 390, height: 844 };
const PNG = { palette: true, quality: 100, effort: 10, compressionLevel: 9 };

async function launch() {
  for (const channel of ['msedge', 'chrome']) {
    try {
      return await chromium.launch({ channel });
    } catch {
      // try the next installed browser
    }
  }
  throw new Error('Needs an installed Microsoft Edge or Google Chrome');
}

async function save(page, name, { settle = true } = {}) {
  await page.mouse.move(0, 0); // no hover glow on whatever the last click left the cursor over
  if (settle) await page.waitForTimeout(1200); // let entrance animations finish
  const buffer = await page.screenshot();
  await sharp(buffer).png(PNG).toFile(path.join(OUT, `${name}.png`));
  console.log(`  ${name}.png`);
}

/** The whole flow, pausing on each step long enough to read it in the recording */
async function walkThrough(page, { shoot, pause = 0 }) {
  await page.goto(BASE);
  await page.getByRole('button', { name: 'Try the demo' }).waitFor();
  await page.waitForTimeout(pause * 2);
  await shoot('login');

  await page.getByRole('button', { name: 'Try the demo' }).click();
  await page.getByPlaceholder(/Coachella/).pressSequentially('Driftwood Valley', { delay: pause ? 70 : 0 });
  await page.waitForTimeout(pause);
  await shoot('search');

  await page.getByRole('button', { name: 'Find Lineup' }).click();
  await page.getByText('artists in lineup').waitFor();
  await page.waitForTimeout(pause * 2);
  await shoot('lineup');

  await page.getByRole('button', { name: 'Continue to Track Selection' }).click();
  await page.getByText('Fetching tracks for').waitFor();
  await shoot('progress', { settle: false }); // mid-stream, before it completes

  await page.getByRole('button', { name: 'Create Playlist' }).waitFor({ timeout: 30_000 });
  await page.waitForTimeout(pause);
  await shoot('tracks');
  if (pause) {
    await page.mouse.wheel(0, 900);
    await page.waitForTimeout(pause * 2);
    await page.mouse.wheel(0, -900);
  }

  await page.getByRole('button', { name: 'Create Playlist' }).click();
  await page.getByText('Playlist Created!').waitFor();
  await page.waitForTimeout(pause * 3);
  await shoot('created');
}

async function screenshots(browser, viewport, suffix) {
  const context = await browser.newContext({ viewport, deviceScaleFactor: 2, isMobile: viewport.width < 768, hasTouch: viewport.width < 768 });
  const page = await context.newPage();
  await walkThrough(page, { shoot: (name, options) => save(page, `${name}${suffix}`, options) });
  await context.close();
}

async function recording(browser) {
  await rm(VIDEO_DIR, { recursive: true, force: true });
  const context = await browser.newContext({ viewport: DESKTOP, recordVideo: { dir: VIDEO_DIR, size: DESKTOP } });
  const page = await context.newPage();
  await walkThrough(page, { shoot: async () => {}, pause: 700 });
  await context.close();

  const [video] = await readdir(VIDEO_DIR);
  const webm = path.join(VIDEO_DIR, video);
  const gif = path.join(OUT, 'demo.gif');
  // Two-pass palette keeps the neon gradients from banding
  execFileSync('ffmpeg', [
    '-y', '-loglevel', 'error', '-i', webm,
    '-vf', 'fps=10,scale=800:-1:flags=lanczos,split[a][b];[a]palettegen=max_colors=128[p];[b][p]paletteuse=dither=bayer:bayer_scale=4',
    gif,
  ]);
  await rm(VIDEO_DIR, { recursive: true, force: true });
  console.log('  demo.gif');
}

await mkdir(OUT, { recursive: true });
const browser = await launch();
try {
  console.log(`Capturing from ${BASE}`);
  await screenshots(browser, DESKTOP, '');
  await screenshots(browser, PHONE, '-phone');
  await recording(browser);
} finally {
  await browser.close();
}
