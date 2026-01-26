# Phase 3: Artist Extraction - Research

**Researched:** 2026-01-26
**Domain:** AI vision text extraction, file upload UI, editable lists
**Confidence:** HIGH

## Summary

Phase 3 combines Claude Vision API for poster text extraction with web search for festival lineup discovery, wrapped in a React UI for file upload and artist list editing. The standard approach uses:

1. **Claude Vision API** for OCR-quality text extraction from festival posters
2. **Claude Web Search Tool** for festival name lookups (no separate endpoint)
3. **Next.js Server Actions** for file upload handling
4. **MUI Autocomplete** with freeSolo mode for editable chip-based artist lists
5. **FileReader API** for client-side image preview before upload

The critical insight: Claude Vision achieves OCR-level accuracy (top-tier alongside Gemini 2.5 Pro) but requires careful image optimization and prompt engineering. Festival posters with artistic typography are challenging—expect to iterate on extraction prompts and provide manual editing UI as fallback.

**Primary recommendation:** Use Server Actions for file upload (simpler than API routes), implement MUI Autocomplete with freeSolo for editable chips, optimize images client-side before sending to Claude Vision, and read prompts from codebase files using Node.js fs module.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| @anthropic-ai/sdk | Latest | Claude Vision & Web Search API | Official Anthropic SDK, supports vision + web search |
| @mui/material | 5.x / 6.x | Autocomplete + Chip components | Most mature React UI library, built-in editable chip support |
| Next.js | 15.x | File upload via Server Actions | Server Actions eliminate need for API routes |
| react | 18.x / 19.x | UI framework | Required for Next.js and MUI |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| react-hook-form | 7.x | Form state management | Complex validation or multi-step forms |
| zod | 3.x | File validation schema | Type-safe validation with react-hook-form |
| sharp | 0.33.x | Server-side image optimization | If need to resize/compress on server |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| MUI Autocomplete | PrimeReact Chips | Less mature, simpler API but fewer features |
| Server Actions | API Routes (route.ts) | More boilerplate, no advantage for file upload |
| Claude Vision | Google Vision / Tesseract | Claude tied to existing auth, top-tier accuracy |
| Claude Web Search | Custom web scraping | $10/1000 searches, but reliable + cited sources |

**Installation:**
```bash
npm install @anthropic-ai/sdk @mui/material @mui/icons-material @emotion/react @emotion/styled
```

## Architecture Patterns

### Recommended Project Structure
```
src/
├── app/
│   ├── api/
│   │   └── extract/
│   │       └── route.ts           # Optional: API route if not using Server Actions
│   ├── artist-extraction/
│   │   ├── page.tsx               # Main page component
│   │   └── actions.ts             # Server Actions for file upload
│   └── components/
│       ├── FileUploadButton.tsx   # File picker + preview
│       ├── EditableChipList.tsx   # MUI Autocomplete chips
│       ├── ProgressIndicator.tsx  # Status during extraction
│       └── FestivalSearch.tsx     # Autocomplete for festival names
├── lib/
│   ├── claude.ts                  # Claude client initialization
│   ├── prompts/
│   │   ├── poster-extraction.txt  # Vision extraction prompt
│   │   └── festival-search.txt    # Web search prompt
│   └── utils/
│       ├── imageValidation.ts     # Size/type checks
│       └── imageOptimization.ts   # Client-side resize
└── types/
    └── extraction.ts              # Artist list types
```

### Pattern 1: File Upload with Server Actions
**What:** Client uploads image via FormData, Server Action processes with Claude Vision
**When to use:** Always—simpler than API routes for Next.js 15
**Example:**
```typescript
// app/artist-extraction/actions.ts
"use server";

import Anthropic from "@anthropic-ai/sdk";
import fs from "fs/promises";
import path from "path";

const client = new Anthropic({
  apiKey: process.env.ANTHROPIC_API_KEY,
});

export async function extractArtistsFromPoster(formData: FormData) {
  const file = formData.get("poster") as File;

  // Validate
  if (!file || file.size > 5 * 1024 * 1024) {
    return { error: "File must be under 5MB" };
  }

  // Convert to base64
  const bytes = await file.arrayBuffer();
  const buffer = Buffer.from(bytes);
  const base64Image = buffer.toString("base64");

  // Read prompt from file
  const promptPath = path.join(process.cwd(), "src/lib/prompts/poster-extraction.txt");
  const promptTemplate = await fs.readFile(promptPath, "utf-8");

  // Call Claude Vision
  const message = await client.messages.create({
    model: "claude-sonnet-4-5",
    max_tokens: 1024,
    messages: [
      {
        role: "user",
        content: [
          {
            type: "image",
            source: {
              type: "base64",
              media_type: file.type as "image/jpeg" | "image/png" | "image/webp",
              data: base64Image,
            },
          },
          {
            type: "text",
            text: promptTemplate,
          },
        ],
      },
    ],
  });

  // Parse response (assumes JSON format)
  const textContent = message.content.find((block) => block.type === "text");
  if (!textContent || textContent.type !== "text") {
    return { error: "No text response from Claude" };
  }

  const artists = JSON.parse(textContent.text);
  return { artists };
}
```

**Client component:**
```typescript
// app/artist-extraction/page.tsx
"use client";

import { useState } from "react";
import { extractArtistsFromPoster } from "./actions";

export default function ArtistExtractionPage() {
  const [artists, setArtists] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);
  const [preview, setPreview] = useState<string | null>(null);

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Show preview
    const reader = new FileReader();
    reader.onload = (event) => setPreview(event.target?.result as string);
    reader.readAsDataURL(file);

    // Extract artists
    setLoading(true);
    const formData = new FormData();
    formData.append("poster", file);

    const result = await extractArtistsFromPoster(formData);
    setLoading(false);

    if (result.error) {
      alert(result.error);
    } else {
      setArtists(result.artists);
    }
  };

  return (
    <div>
      <input type="file" accept="image/png,image/jpeg" onChange={handleFileChange} />
      {preview && <img src={preview} alt="Preview" style={{ maxWidth: 300 }} />}
      {loading && <p>Extracting artists...</p>}
      {/* EditableChipList component here */}
    </div>
  );
}
```

### Pattern 2: Editable Chip List with MUI Autocomplete
**What:** Chip-based artist list with add/remove/edit capabilities
**When to use:** Any editable tag/list interface
**Example:**
```typescript
// components/EditableChipList.tsx
"use client";

import { Autocomplete, TextField, Chip } from "@mui/material";
import { useState } from "react";

interface EditableChipListProps {
  initialArtists: string[];
  onChange: (artists: string[]) => void;
}

export default function EditableChipList({ initialArtists, onChange }: EditableChipListProps) {
  const [artists, setArtists] = useState<string[]>(initialArtists);

  const handleChange = (event: any, newValue: string[]) => {
    const filtered = newValue.filter((name) => name.trim() !== "");
    setArtists(filtered);
    onChange(filtered);
  };

  return (
    <Autocomplete
      multiple
      freeSolo
      options={[]} // No suggestions, pure input
      value={artists}
      onChange={handleChange}
      renderInput={(params) => (
        <TextField
          {...params}
          label="Artist Names"
          placeholder="Type name and press Enter"
          helperText="Click X to remove, type to add"
        />
      )}
      renderTags={(value, getTagProps) =>
        value.map((option, index) => (
          <Chip
            label={option}
            {...getTagProps({ index })}
            onDelete={() => {
              const newArtists = artists.filter((_, i) => i !== index);
              setArtists(newArtists);
              onChange(newArtists);
            }}
          />
        ))
      }
    />
  );
}
```

### Pattern 3: Claude Web Search for Festival Lineups
**What:** Use web_search tool (not separate API) to find festival lineups
**When to use:** Festival name search, no predefined database
**Example:**
```typescript
// app/artist-extraction/actions.ts
"use server";

import Anthropic from "@anthropic-ai/sdk";
import fs from "fs/promises";
import path from "path";

export async function searchFestivalLineup(festivalName: string, year?: number) {
  const client = new Anthropic({ apiKey: process.env.ANTHROPIC_API_KEY });

  // Read prompt from file
  const promptPath = path.join(process.cwd(), "src/lib/prompts/festival-search.txt");
  const promptTemplate = await fs.readFile(promptPath, "utf-8");
  const prompt = promptTemplate.replace("{FESTIVAL_NAME}", festivalName).replace("{YEAR}", year?.toString() || "2026");

  const response = await client.messages.create({
    model: "claude-sonnet-4-5",
    max_tokens: 2048,
    messages: [{ role: "user", content: prompt }],
    tools: [
      {
        type: "web_search_20250305",
        name: "web_search",
        max_uses: 5,
      },
    ],
  });

  // Extract artists from response
  // Claude will automatically search and cite sources
  const textContent = response.content.find((block) => block.type === "text");
  if (!textContent || textContent.type !== "text") {
    return { error: "No lineup found" };
  }

  // Parse artist names (prompt should request JSON format)
  try {
    const data = JSON.parse(textContent.text);
    return { artists: data.artists, sources: data.sources };
  } catch {
    return { error: "Could not parse lineup data" };
  }
}
```

### Pattern 4: Client-Side Image Optimization
**What:** Resize/validate images before upload to reduce latency
**When to use:** Always—reduces API cost and improves UX
**Example:**
```typescript
// lib/utils/imageOptimization.ts
export async function optimizeImage(file: File): Promise<File> {
  // Validate type
  if (!["image/jpeg", "image/png", "image/webp"].includes(file.type)) {
    throw new Error("Invalid file type. Use PNG, JPG, or WebP.");
  }

  // Check size
  const MAX_SIZE = 5 * 1024 * 1024; // 5MB
  if (file.size > MAX_SIZE) {
    throw new Error("File must be under 5MB");
  }

  // Resize if too large (client-side)
  return new Promise((resolve, reject) => {
    const img = new Image();
    const reader = new FileReader();

    reader.onload = (e) => {
      img.src = e.target?.result as string;
    };

    img.onload = () => {
      const canvas = document.createElement("canvas");
      let { width, height } = img;

      // Resize to max 1568px (Claude's optimal size)
      const MAX_DIMENSION = 1568;
      if (width > MAX_DIMENSION || height > MAX_DIMENSION) {
        if (width > height) {
          height = (height / width) * MAX_DIMENSION;
          width = MAX_DIMENSION;
        } else {
          width = (width / height) * MAX_DIMENSION;
          height = MAX_DIMENSION;
        }
      }

      canvas.width = width;
      canvas.height = height;
      const ctx = canvas.getContext("2d");
      ctx?.drawImage(img, 0, 0, width, height);

      canvas.toBlob(
        (blob) => {
          if (!blob) {
            reject(new Error("Failed to optimize image"));
            return;
          }
          const optimizedFile = new File([blob], file.name, { type: file.type });
          resolve(optimizedFile);
        },
        file.type,
        0.9 // Quality
      );
    };

    img.onerror = () => reject(new Error("Failed to load image"));
    reader.readAsDataURL(file);
  });
}
```

### Anti-Patterns to Avoid
- **Don't use API routes for file upload**: Server Actions are simpler and handle FormData natively
- **Don't hardcode prompts**: Always read from files for version control and testing
- **Don't skip client-side validation**: Server-side only validation wastes API calls on invalid files
- **Don't use separate web scraping**: Claude Web Search costs $10/1000 but includes citations and reliability
- **Don't forget rate limit headers**: Always check `retry-after` header on 429 errors

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Editable tag input UI | Custom chip component with inline editing | MUI Autocomplete with freeSolo | Handles keyboard nav, backspace deletion, input validation, accessibility |
| Image file validation | Manual MIME type checks | FileReader + actual content validation | Extension checks can be bypassed, need to validate actual image data |
| Festival lineup lookup | Custom web scraping | Claude Web Search Tool | $10/1000 searches includes citations, handles rate limits, no scraping fragility |
| Retry logic for rate limits | Fixed delays or manual exponential backoff | SDK with built-in retry + `retry-after` header | Claude API provides exact wait time, SDKs handle automatically |
| Image optimization | Custom canvas resizing | Browser Image API + Claude's size guidelines | Edge cases (aspect ratio, quality, format) already solved |
| Prompt management | String concatenation in code | Read from files with fs.readFile | Version control, A/B testing, user can update prompts without deploy |

**Key insight:** File upload in React looks simple (one `<input type="file">`) but validation, preview, optimization, and error handling require significant edge case handling. MUI provides battle-tested components. Claude APIs provide robust tools (Vision, Web Search) that eliminate need for custom scraping or OCR services.

## Common Pitfalls

### Pitfall 1: Ignoring Claude Vision Image Size Limits
**What goes wrong:** Sending large images (>5MB or >8000px) causes 413 errors or automatic resizing with added latency
**Why it happens:** User uploads high-res poster (10MP phone camera) without optimization
**How to avoid:** Always resize images client-side to max 1568px on longest edge (Claude's optimal size, ~1600 tokens, $4.80/1K images)
**Warning signs:** Slow "analyzing" phase, 413 errors, unexpectedly high token usage

### Pitfall 2: Not Handling Claude Vision's Spatial Reasoning Limits
**What goes wrong:** Extraction misses artists in complex layouts, rotated text, or artistic typography
**Why it happens:** Claude Vision has limited spatial reasoning—struggles with precise positioning, artistic fonts on posters
**How to avoid:**
- Prompt engineering: "List all artist names, even if uncertain. Mark uncertain names with ?"
- Always provide manual editing UI
- Show confidence indicators on chips
- User context mentions this is unvalidated—iteration expected
**Warning signs:** Consistently missing artists in corners, rotated text, or stylized fonts

### Pitfall 3: Forgetting to Enable Web Search in Console
**What goes wrong:** Web search tool calls fail with permission errors despite correct code
**Why it happens:** Web search requires organization admin to enable in Console Settings → Privacy
**How to avoid:** Document requirement, check during setup, provide clear error messages
**Warning signs:** 403 permission_error when web_search tool is used

### Pitfall 4: Treating Web Search as Separate API Endpoint
**What goes wrong:** Developer tries to call web search directly, gets confused about implementation
**Why it happens:** Misunderstanding—web search is a TOOL that Claude decides to use, not a direct endpoint
**How to avoid:**
- Pass tool definition in messages.create()
- Let Claude decide when to search based on prompt
- Web search executes automatically during message processing
**Warning signs:** Looking for `/search` endpoint, trying to trigger search manually

### Pitfall 5: Not Handling Rate Limits with Exponential Backoff
**What goes wrong:** Rapid retries after 429 errors burn through rate limit, cascade failures
**Why it happens:** Tier 1 starts at 50 RPM—easy to hit during testing with multiple poster uploads
**How to avoid:**
- Check `retry-after` header (tells exact wait time)
- Implement exponential backoff with jitter
- Use Tier 2 for development ($40 deposit → 1,000 RPM)
- Show user-friendly "Server busy, retrying..." messages
**Warning signs:** Multiple 429 errors in logs, user sees repeated failures

### Pitfall 6: Server Actions File Upload Limits
**What goes wrong:** Large images fail silently or timeout in Server Actions
**Why it happens:** Next.js has request size limits (varies by deployment platform)
**How to avoid:**
- Validate file size client-side BEFORE upload (<5MB)
- Optimize images client-side first
- Show file size in UI before upload
- Implement proper error boundaries
**Warning signs:** Timeouts on large files, no error message, request hangs

### Pitfall 7: Autocomplete freeSolo + Multiple Mode Edge Cases
**What goes wrong:** Users can't create new chips, or chips disappear when typing
**Why it happens:** Known MUI issue when combining `freeSolo` + `multiple` with object arrays
**How to avoid:**
- Use string arrays, not objects, for artist names
- Avoid `getOptionLabel` with freeSolo + multiple
- Test backspace behavior (should delete last chip)
- Provide clear placeholder text
**Warning signs:** GitHub issues #30123, #38022 describe same symptoms

### Pitfall 8: Hardcoded Prompts Instead of Files
**What goes wrong:** User has existing tested prompt but can't integrate it, or wants to iterate without redeploying
**Why it happens:** Convenience of inline strings during development
**How to avoid:**
- Read prompts with `fs.readFile` in Server Actions (server-side only)
- Store prompts in `/lib/prompts/` directory
- Document prompt file format and variables
- User requirement explicitly states "prompt in codebase file"
**Warning signs:** User feedback about "can't update prompt", failed prompt iteration testing

### Pitfall 9: Forgetting Token Costs for Images
**What goes wrong:** Unexpected API costs from processing many large posters
**Why it happens:** Each image uses ~1600 tokens ($4.80/1K images with Sonnet 4.5)
**How to avoid:**
- Estimate costs in UI: "This will use ~1600 tokens (~$0.005)"
- Optimize images to exactly 1568px max dimension
- Consider caching extracted results
- Batch process if multiple posters
**Warning signs:** High bills, user complaints about cost

### Pitfall 10: No Fallback When Extraction Fails
**What goes wrong:** User stuck with failed extraction, no way to proceed
**Why it happens:** Vision API can fail (timeout, 529 overload, bad image quality)
**How to avoid:**
- Always offer "Type festival name instead" option
- Show partial results if available
- Provide clear "Try different image" CTA
- Festival name search as alternative flow
**Warning signs:** User context mentions "suggest alternatives on failure"

## Code Examples

Verified patterns from official sources:

### Claude Vision with Base64 Image
```typescript
// Source: https://platform.claude.com/docs/en/build-with-claude/vision
import Anthropic from "@anthropic-ai/sdk";

const client = new Anthropic();

const message = await client.messages.create({
  model: "claude-sonnet-4-5",
  max_tokens: 1024,
  messages: [
    {
      role: "user",
      content: [
        {
          type: "image",
          source: {
            type: "base64",
            media_type: "image/jpeg",
            data: base64ImageData, // Base64-encoded string
          },
        },
        {
          type: "text",
          text: "Extract all artist names from this festival poster. Return as JSON array.",
        },
      ],
    },
  ],
});
```

### Claude Web Search Tool Definition
```typescript
// Source: https://platform.claude.com/docs/en/agents-and-tools/tool-use/web-search-tool
const response = await client.messages.create({
  model: "claude-sonnet-4-5",
  max_tokens: 1024,
  messages: [
    {
      role: "user",
      content: "What is the Coachella 2026 lineup?",
    },
  ],
  tools: [
    {
      type: "web_search_20250305",
      name: "web_search",
      max_uses: 5, // Limit searches per request
    },
  ],
});

// Web search executes automatically
// Response includes citations in content blocks
```

### MUI Autocomplete with Multiple + FreeSolo
```typescript
// Source: https://mui.com/material-ui/react-autocomplete/
import { Autocomplete, TextField } from "@mui/material";
import { useState } from "react";

export default function Tags() {
  const [value, setValue] = useState<string[]>([]);

  return (
    <Autocomplete
      multiple
      freeSolo
      options={[]} // Empty for pure input
      value={value}
      onChange={(event, newValue) => {
        setValue(newValue);
      }}
      renderInput={(params) => (
        <TextField
          {...params}
          variant="outlined"
          label="Artist Names"
          placeholder="Type and press Enter"
        />
      )}
    />
  );
}
```

### Reading Prompt from File (Server-Side)
```typescript
// Source: https://nodejsdesignpatterns.com/blog/reading-writing-files-nodejs/
import fs from "fs/promises";
import path from "path";

// Server Action or API Route
export async function loadPrompt(promptName: string): Promise<string> {
  const promptPath = path.join(process.cwd(), "src/lib/prompts", `${promptName}.txt`);
  const prompt = await fs.readFile(promptPath, "utf-8");
  return prompt;
}

// Usage in Server Action
const extractionPrompt = await loadPrompt("poster-extraction");
```

### File Size Validation with Error Handling
```typescript
// Source: https://learnersbucket.com/examples/react/react-validate-the-file-size-before-upload/
export function validateImageFile(file: File): { valid: boolean; error?: string } {
  // Type validation
  const validTypes = ["image/jpeg", "image/png", "image/webp"];
  if (!validTypes.includes(file.type)) {
    return { valid: false, error: "Invalid file type. Use PNG, JPG, or WebP." };
  }

  // Size validation
  const MAX_SIZE = 5 * 1024 * 1024; // 5MB
  if (file.size > MAX_SIZE) {
    return { valid: false, error: `File too large (${(file.size / 1024 / 1024).toFixed(1)}MB). Max 5MB.` };
  }

  return { valid: true };
}
```

### Rate Limit Error Handling with Retry-After
```typescript
// Source: https://platform.claude.com/docs/en/api/rate-limits
async function callClaudeWithRetry(requestFn: () => Promise<any>, maxRetries = 3): Promise<any> {
  for (let attempt = 0; attempt < maxRetries; attempt++) {
    try {
      return await requestFn();
    } catch (error: any) {
      if (error.status === 429) {
        // Check retry-after header (in seconds)
        const retryAfter = error.response?.headers?.["retry-after"];
        const waitTime = retryAfter ? parseInt(retryAfter) * 1000 : Math.pow(2, attempt) * 1000;

        console.log(`Rate limited. Waiting ${waitTime}ms before retry ${attempt + 1}/${maxRetries}`);
        await new Promise((resolve) => setTimeout(resolve, waitTime));
      } else {
        throw error; // Non-rate-limit error, propagate
      }
    }
  }
  throw new Error("Max retries exceeded");
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| API Routes for file upload | Next.js Server Actions | Next.js 13.4+ (2023) | Simpler code, built-in FormData handling |
| Custom web scraping | Claude Web Search Tool | Sep 2025 | Reliable, cited sources, $10/1K searches |
| Tesseract.js OCR | Claude Vision API | Claude 3 (Mar 2024) | Top-tier accuracy, multi-modal understanding |
| react-select for tags | MUI Autocomplete freeSolo | MUI v5+ (2021) | Better accessibility, built-in chip rendering |
| Fixed rate limit delays | retry-after header + SDK retry | Anthropic API early 2024 | Exact wait times, faster recovery |
| Prompt templates in code | File-based prompts | Best practice 2025+ | Version control, A/B testing, user-editable |

**Deprecated/outdated:**
- **API Routes for file upload**: Server Actions are now standard for Next.js App Router
- **Claude Sonnet 3.7**: Deprecated model, use Sonnet 4.5 (better vision accuracy)
- **Combined TPM limits**: Old API providers counted all tokens; Claude excludes cached tokens from ITPM (effective 10x throughput with caching)
- **Drag-and-drop zones**: User context explicitly chose click-only file picker (simpler UX)

## Open Questions

Things that couldn't be fully resolved:

1. **Claude Vision accuracy with artistic festival typography**
   - What we know: Claude Vision has top-tier OCR accuracy, but user noted "unvalidated" for festival posters
   - What's unclear: Specific accuracy rate for artistic/stylized fonts, rotated text
   - Recommendation: Implement confidence indicators (question mark icon on chips), always provide manual editing, plan for prompt iteration

2. **Optimal image size for festival posters**
   - What we know: Claude recommends max 1568px, ~1600 tokens, $4.80/1K images
   - What's unclear: Does poster aspect ratio (typically tall) affect extraction quality?
   - Recommendation: Test with real posters, may need to preserve aspect ratio vs. square crop

3. **Web search citation display requirements**
   - What we know: Web search returns citations with `cited_text`, `title`, `url`
   - What's unclear: Must citations be shown to end users? Legal requirement?
   - Recommendation: Display sources in UI (Anthropic docs say "must include citations when displaying to end users")

4. **MUI Autocomplete inline editing for chips**
   - What we know: User context says "click chip to edit inline"
   - What's unclear: MUI Autocomplete chips don't natively support inline editing, requires custom implementation
   - Recommendation: Use simpler UX: click X to delete, type new name to add (re-type if misspelled). Inline editing adds complexity.

5. **Next.js deployment file size limits**
   - What we know: Varies by platform (Vercel, Netlify, self-hosted)
   - What's unclear: Exact limits for Server Actions on each platform
   - Recommendation: Client-side 5MB validation should prevent most issues, document platform-specific limits

## Sources

### Primary (HIGH confidence)
- [Claude Vision API Documentation](https://platform.claude.com/docs/en/build-with-claude/vision) - Image upload methods, size limits, best practices
- [Claude Web Search Tool Documentation](https://platform.claude.com/docs/en/agents-and-tools/tool-use/web-search-tool) - Tool definition, pricing, usage patterns
- [Claude API Rate Limits](https://platform.claude.com/docs/en/api/rate-limits) - Tier requirements ($40 for Tier 2), rate limits (1000 RPM), retry-after headers
- [Claude API Errors](https://platform.claude.com/docs/en/api/errors) - Error types (429, 529, 413), timeout handling, request size limits
- [MUI Autocomplete Documentation](https://mui.com/material-ui/react-autocomplete/) - Multiple mode, freeSolo, chip rendering

### Secondary (MEDIUM confidence)
- [Next.js Server Actions Tutorial](https://strapi.io/blog/epic-next-js-15-tutorial-part-5-file-upload-using-server-actions) - File upload patterns (Jan 2026)
- [React File Upload Best Practices](https://rootstack.com/en/blog/file-uploads-reactjs-and-hooks-complete-guide) - Validation, FormData, error handling
- [Node.js File Reading Best Practices](https://nodejsdesignpatterns.com/blog/reading-writing-files-nodejs/) - fs.promises, async/await patterns
- [Next.js Environment Variables](https://nextjs.org/docs/pages/guides/environment-variables) - Configuration for file paths

### Tertiary (LOW confidence)
- [Claude Vision Festival Poster Accuracy](https://research.aimultiple.com/ocr-accuracy/) - OCR benchmark showing Claude top-tier, but not festival-specific
- [MUI Autocomplete GitHub Issues](https://github.com/mui/material-ui/issues/30123) - Known issues with freeSolo + multiple mode
- [Claude API 429 Error Handling Guide](https://www.aifreeapi.com/en/posts/fix-claude-api-429-rate-limit-error) - Community best practices, not official

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Official Anthropic and MUI docs confirm recommendations
- Architecture: HIGH - Next.js 15 Server Actions are current standard, verified with official tutorials
- Pitfalls: MEDIUM - Spatial reasoning limits and freeSolo edge cases are documented but festival-specific accuracy unvalidated
- Web Search implementation: HIGH - Official Anthropic docs detail tool usage and pricing
- Prompt file reading: HIGH - Standard Node.js pattern, verified with Next.js docs

**Research date:** 2026-01-26
**Valid until:** ~60 days (stable domain, but Claude API and Next.js evolve quickly)

**Critical notes for planner:**
- User has existing tested prompt file that needs fs.readFile integration
- Anthropic Tier 2 ($40 deposit) required early for 1,000 RPM during testing
- Claude Vision accuracy with artistic typography is unvalidated—expect prompt iteration
- User context explicitly defined click-only file picker (no drag-and-drop)
- Web search costs $10/1000 searches—estimate ~2-5 searches per festival name lookup
