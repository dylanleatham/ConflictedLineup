# 03-05 Summary: Integration & Workflow

## Outcome: Complete (via iterative development)

**Note:** The original 03-05-PLAN.md was superseded by iterative development during the session. This summary documents what was actually built.

## What Was Built

### UI Flow (Search-First with Poster Fallback)

The extraction workflow uses a search-first approach rather than the originally planned tab-based UI:

1. **Primary:** User enters festival name + year → "Find Lineup" button
2. **On Success:** Shows results with editable artist list
3. **On Failure:** Reveals poster upload as fallback option
4. **On Results:** "Artists unexpected? Upload a poster instead" link

### Key Changes from Original Plan

| Original Plan | Actual Implementation |
|---------------|----------------------|
| Tab-based UI (Upload / Search) | Search-first, poster fallback |
| Dark theme | Light theme |
| Separate PosterUpload.tsx | Consolidated into UploadPage.tsx |
| Separate FestivalSearch.tsx | Consolidated into UploadPage.tsx |
| Web search via prompt instruction | Web search via SDK tool (`ServerTools.GetWebSearchTool`) |
| Read-only artist chips | Inline editing (click to edit name) |

### Files Modified

- `frontend/src/pages/UploadPage.tsx` - Complete unified workflow
- `frontend/src/pages/UploadPage.css` - Light theme styling
- `frontend/src/components/EditableArtistList.tsx` - Added inline editing
- `frontend/src/components/EditableArtistList.css` - Edit UI styling
- `backend/src/ConflictedLineup.Api/Services/ClaudeService.cs` - Added web search tool, multi-block response handling

### Files Removed

- `frontend/src/components/PosterUpload.tsx` - Consolidated into UploadPage
- `frontend/src/components/PosterUpload.css`
- `frontend/src/components/FestivalSearch.tsx` - Consolidated into UploadPage
- `frontend/src/components/FestivalSearch.css`

## Technical Decisions

1. **Web search as SDK tool:** Added `ServerTools.GetWebSearchTool(maxUses: 5)` to Claude API calls for reliable lineup lookups
2. **Multi-block response handling:** Claude web search returns multiple text blocks; combined them before JSON extraction
3. **Confidence simplified:** Removed uncertain/high logic; all artists marked as high confidence
4. **Inline editing:** Click artist chip to edit name, Enter to save, Escape to cancel

## Verification

Manual testing confirmed:
- Festival name search triggers web search and returns artist list
- Poster fallback appears when search fails
- Artist chips are editable (add, remove, edit names)
- "Continue to Track Selection" button proceeds with edited list
- "Artists unexpected?" link on results allows switching to poster upload

## Duration

~45 minutes (iterative development across multiple exchanges)

## Commits

- `a303296` - feat(03): add web search as primary extraction with poster fallback
- `0db1225` - chore: remove unused FestivalSearch and PosterUpload components
