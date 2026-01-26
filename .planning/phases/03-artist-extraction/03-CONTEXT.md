# Phase 3: Artist Extraction - Context

**Gathered:** 2026-01-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Users can upload festival poster images or type festival names to extract artist lists with editing capability. The phase delivers input → extraction → review workflow. Creating playlists and selecting tracks are separate phases.

</domain>

<decisions>
## Implementation Decisions

### Upload Experience
- Click-only file picker (no drag-and-drop zone)
- Show thumbnail preview of uploaded poster while extracting
- Progress bar with status text during AI extraction
- On extraction failure: show error + suggest alternatives (try different image, use festival search)

### Artist List Editing
- Tag/chip style display (compact chips with X to remove)
- Text input field above chips to add missed artists (type name, press Enter)
- Click chip to edit inline for misspelled names
- Auto-proceed option available (but explicit 'Continue' button as primary)

### Festival Name Search
- Autocomplete dropdown showing suggestions as user types
- Multiple matches: default to most recent year, but show alternatives
- No results found: offer to upload poster instead
- Data source: Claude web search tool with user's existing prompt

### Extraction Feedback
- Show uncertain names with question mark icon on chip
- Update status messages as extraction progresses ("Analyzing..." → "Processing complex poster..." → "Almost done...")
- Partial results: display what was found + warning message "Some artists may be missing. You can add them manually."

### Claude's Discretion
- Exact chip styling and colors
- Progress bar animation details
- Autocomplete debounce timing
- Threshold for "uncertain" confidence marking

</decisions>

<specifics>
## Specific Ideas

- User has existing prompt file for AI extraction that should be read from codebase
- Claude web search tool for festival lineup lookups (not a predefined database)

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 03-artist-extraction*
*Context gathered: 2026-01-26*
