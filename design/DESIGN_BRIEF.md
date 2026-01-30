# Conflicted Lineup — Design Brief

## Project Overview
Retrofit an existing React application that converts festival lineups into Spotify playlists. The app searches for festival lineups via web search (with poster upload as a fallback option). The current implementation is functional but visually generic. This design system will transform it into something that feels like it belongs at Thunderdome, Lost Lands, Bass Canyon, Beyond Wonderland, or Apocalypse.

---

## Aesthetic Direction: "PORTAL TO THE UNDERCARD"

### Core Concept
The app should feel like a **digital artifact from the festival itself** — as if it were printed on a black t-shirt, projected onto a stage, or handed out as contraband at 3am in the campgrounds. It's not a utility; it's an **experience**.

### Tone Keywords
- **Aggressive but inviting** — Heavy visual impact without being hostile to use
- **Nocturnal** — This app lives at night, under neon and strobes
- **Textured & tactile** — Screen-printed, distressed, layered, weathered
- **Dramatic** — Every element earns its place through visual weight
- **Tribal/ritualistic** — Shared experience, community artifact, sacred lineup

### What We're NOT
- Clean minimalism (no sterile whites, no airy spacing)
- Corporate SaaS aesthetic (no rounded-everything, no soft shadows)
- Generic "dark mode" (just inverting colors isn't enough)
- AI-generated slop (no purple gradients, no Inter font, no predictable layouts)

---

## Visual Language

### Color System

#### Primary Palette
| Token | Hex | Usage |
|-------|-----|-------|
| `--void` | `#0a0a0a` | Primary background, the darkness |
| `--abyss` | `#141414` | Card backgrounds, elevated surfaces |
| `--smoke` | `#1a1a1a` | Subtle depth layers |
| `--ash` | `#2a2a2a` | Borders, dividers, inactive states |
| `--ember` | `#404040` | Secondary text, muted elements |

#### Neon Accents (The Stage Lights)
| Token | Hex | Vibe |
|-------|-----|------|
| `--neon-cyan` | `#00f5ff` | Primary action, Spotify connection, "electric" |
| `--neon-magenta` | `#ff00aa` | Highlights, hover states, "hot" |
| `--toxic-green` | `#39ff14` | Success states, "go", energy |
| `--blaze-orange` | `#ff6a00` | Warnings, featured items, "fire" |
| `--void-purple` | `#8b00ff` | Accent, premium feel, "cosmic" |

#### Text Colors
| Token | Hex | Usage |
|-------|-----|-------|
| `--text-primary` | `#f0f0f0` | Headlines, primary content |
| `--text-secondary` | `#a0a0a0` | Body text, descriptions |
| `--text-muted` | `#606060` | Captions, timestamps, tertiary |

### Glow Effects
Neon colors should **glow**. Use layered box-shadows:
```css
/* Cyan glow example */
box-shadow: 
  0 0 10px var(--neon-cyan),
  0 0 20px rgba(0, 245, 255, 0.5),
  0 0 40px rgba(0, 245, 255, 0.25);
```

---

## Typography

### Display Font: **Bebas Neue** or **Anton**
- All caps for headers
- Tight letter-spacing (-0.02em to -0.05em)
- Used for: Page titles, feature names, big moments

### Accent Font: **Permanent Marker** or **Rock Salt** (sparingly)
- Hand-drawn energy for special callouts
- Used for: "LIVE", badges, emphatic labels

### Body Font: **Space Grotesk** or **JetBrains Mono**
- Technical, readable, but with character
- Used for: Body text, artist names, UI labels

### Type Scale
```
--text-xs: 0.75rem    /* 12px — captions */
--text-sm: 0.875rem   /* 14px — secondary */
--text-base: 1rem     /* 16px — body */
--text-lg: 1.25rem    /* 20px — emphasis */
--text-xl: 1.5rem     /* 24px — section headers */
--text-2xl: 2rem      /* 32px — page titles */
--text-3xl: 3rem      /* 48px — hero moments */
--text-4xl: 4rem      /* 64px — festival name display */
```

---

## Texture & Effects

### Noise Overlay
Every major surface should have subtle noise grain — like printed material or a projector screen.
```css
.textured {
  position: relative;
}
.textured::after {
  content: '';
  position: absolute;
  inset: 0;
  background-image: url("data:image/svg+xml,..."); /* noise pattern */
  opacity: 0.03;
  pointer-events: none;
}
```

### Distressed Edges
Important containers can have irregular, torn-paper-style borders using clip-path or SVG masks.

### Gradient Mesh Backgrounds
For hero sections, use dramatic multi-stop radial gradients that feel like stage lighting:
```css
background: 
  radial-gradient(ellipse at 20% 80%, rgba(255, 0, 170, 0.15) 0%, transparent 50%),
  radial-gradient(ellipse at 80% 20%, rgba(0, 245, 255, 0.1) 0%, transparent 50%),
  var(--void);
```

---

## Component Patterns

### Cards (Artist Cards, Playlist Cards)
- **Background**: `--abyss` with subtle border of `--ash`
- **Border style**: 2px solid, sharp corners (no border-radius or very minimal 2-4px)
- **Hover**: Lift with transform + accent glow
- **Image treatment**: Slight desaturation at rest, full color on hover

### Buttons

#### Primary Action (e.g., "Generate Playlist")
- Background: Gradient from `--neon-cyan` to `--void-purple`
- Text: `--void` (dark on bright)
- Border: None
- All caps, bold, letter-spacing
- Glow effect on hover
- Transform: scale(1.02) on hover

#### Secondary Action
- Background: transparent
- Border: 2px solid `--neon-magenta`
- Text: `--neon-magenta`
- Glow border on hover

#### Ghost/Tertiary
- Background: `--smoke`
- Border: 1px solid `--ash`
- Text: `--text-secondary`

### Input Fields
- Background: `--smoke`
- Border: 2px solid `--ash`
- Focus: Border transitions to `--neon-cyan` with glow
- Text: `--text-primary`
- Placeholder: `--text-muted`

### Progress/Loading States
- Use animated gradient bars
- Colors: sweep from `--neon-cyan` through `--neon-magenta` to `--toxic-green`
- Add shimmer effect for loading skeletons

---

## Layout Principles

### Spacing Scale
```
--space-1: 0.25rem   /* 4px */
--space-2: 0.5rem    /* 8px */
--space-3: 0.75rem   /* 12px */
--space-4: 1rem      /* 16px */
--space-5: 1.5rem    /* 24px */
--space-6: 2rem      /* 32px */
--space-8: 3rem      /* 48px */
--space-10: 4rem     /* 64px */
--space-12: 6rem     /* 96px */
```

### Grid Philosophy
- **Asymmetry is welcome** — not everything needs to be perfectly balanced
- **Generous negative space** contrasted with **dense information clusters**
- **Overlapping elements** where appropriate (images bleeding into headers, etc.)

### Z-Index Layers
```
--z-base: 0
--z-elevated: 10
--z-dropdown: 100
--z-modal: 1000
--z-toast: 2000
```

---

## Animation Guidelines

### Principles
1. **Purposeful motion** — animations should reinforce the action, not decorate
2. **Snappy, not floaty** — use cubic-bezier curves that feel punchy
3. **Stagger reveals** — lists and grids should animate in sequence

### Recommended Easings
```css
--ease-out-expo: cubic-bezier(0.16, 1, 0.3, 1);
--ease-out-back: cubic-bezier(0.34, 1.56, 0.64, 1);
--ease-in-out: cubic-bezier(0.65, 0, 0.35, 1);
```

### Key Animations

#### Page Load / Hero Reveal
- Title slides up and fades in (staggered letters optional)
- Background gradient animates in
- Duration: 600-800ms

#### Card Entrance (Artist grid)
- Stagger delay: 50ms between cards
- Animation: fade up + scale from 0.95
- Duration: 400ms each

#### Button Press
- Transform: scale(0.98)
- Duration: 100ms
- Ease: ease-out

#### Success State (Playlist Created)
- Confetti burst or particle effect
- Success icon scales in with bounce
- Duration: 500ms

---

## Iconography

### Style
- Solid fills preferred over strokes
- Chunky line weights if using stroke (2-3px)
- Consider custom icons for key actions (not generic UI kit)

### Key Icons Needed
- Upload/Drop image
- Spotify logo integration
- Play/Preview
- Add to library
- Artist/Performer
- Calendar/Date
- Location/Venue
- Loading/Processing
- Success/Check
- Error/Warning

---

## Page-by-Page Direction

### 1. Landing / Home
**The Portal**
- Hero section with dramatic gradient mesh background
- Bold headline: "FIND YOUR LINEUP" (massive type)
- Subhead: "Search any festival. Get a playlist. Enter the portal."
- **Primary CTA: Search input** — large, prominent text field with search button
- Quick-pick festival cards below (Lost Lands, Bass Canyon, Beyond, Thunderdome, etc.)
- **Fallback: Poster upload** — smaller, secondary option below examples
  - "Can't find your lineup? Upload a poster instead."
  - Compact drag & drop zone with magenta accent (not cyan — differentiate from primary)

### 2. Search Results / Lineup Preview
**The Discovery**
- Show festival name prominently
- Display extracted lineup with source attribution
- Allow user to confirm or refine the artist list
- Option to "Search again" or "Upload poster instead" if results aren't right

### 3. Artist Selection / Review
**The Lineup**
- Grid of artist cards with images
- Toggle selection (selected = full brightness + glow, unselected = dimmed)
- Drag to reorder priority
- Search/filter functionality
- "Select All" / "Deselect All" actions

### 4. Playlist Generation
**The Drop**
- Real-time track additions visualized
- Show album art thumbnails streaming in
- Counter: "147 TRACKS ADDED"
- Progress toward completion

### 5. Result / Success
**The Afterparty**
- Playlist preview with track list
- Large "OPEN IN SPOTIFY" CTA
- Share functionality
- Stats: "42 artists • 147 tracks • 9h 23m"
- Option to save/export

---

## Responsive Considerations

### Breakpoints
```
--bp-sm: 640px
--bp-md: 768px
--bp-lg: 1024px
--bp-xl: 1280px
```

### Mobile-First Approach
**This app is expected to be heavily used on mobile.** Design for thumb-reachable zones and touch interactions first.

#### Touch Targets
- **Minimum 48x48px** for all interactive elements (Apple/Google guidelines)
- Artist cards: min-height 72px
- Checkboxes: 32x32px on mobile (larger than desktop 28px)
- Buttons: min-height 48px with generous padding

#### Search Input (Mobile)
- **Stack vertically** on screens < 768px
- Full-width input + full-width button below
- Input font-size: 16px minimum (prevents iOS zoom on focus)

#### Festival Quick-Picks
- **2-column grid** on mobile (not 4)
- Cards should be chunky and easy to tap
- Consider making these the primary action on mobile (easier than typing)

#### Artist Selection Grid
- **Single column** on mobile for easier scanning and selection
- Full-width cards with large touch targets
- Checkbox on right side (thumb-friendly for right-handed users)

#### Thumb Zone Optimization
```
┌─────────────────┐
│   Hard to reach │  ← Nav, logo (view-only OK)
│                 │
│   Comfortable   │  ← Content, scrolling
│                 │
│   Easy reach    │  ← Primary actions, generate button
└─────────────────┘
```

Consider a **sticky bottom CTA** for the "Generate Playlist" button on artist selection screen.

#### Touch vs Hover
- Replace hover effects with `:active` states on touch devices
- Use `@media (hover: none) and (pointer: coarse)` to detect touch
- Scale transforms on `:active` (e.g., `scale(0.98)`) give tactile feedback
- Ensure `-webkit-tap-highlight-color` is visible but on-brand

#### Mobile-Specific Patterns

**Bottom Sheet for Results**
When showing search results or playlist preview, consider a bottom sheet that slides up — feels native on mobile.

**Swipe to Deselect**
Artist cards could support swipe-left to deselect (optional enhancement).

**Haptic Feedback**
If building with React Native or PWA with haptics API, trigger light haptic on:
- Artist selection toggle
- Playlist generation complete
- Successful Spotify connection

### Desktop Enhancements
- Multi-column artist grid (2-3 columns)
- Hover states with glow effects
- Side-by-side search input + button
- Keyboard shortcuts (Enter to search, Space to toggle artist)

### Shared Principles
- **Generous spacing** — fat fingers need room
- **High contrast** — outdoor festival use, bright sunlight
- **Fast perceived performance** — skeleton loaders, optimistic UI
- **Offline consideration** — cache the lineup once loaded

## Accessibility Notes

- Maintain WCAG AA contrast ratios (4.5:1 for text)
- Neon colors on dark backgrounds generally pass, but test
- Provide focus states with high visibility (glow effects help)
- Respect `prefers-reduced-motion`
- All interactive elements keyboard accessible

---

## File Structure Recommendation

```
src/
├── styles/
│   ├── tokens.css          # CSS custom properties
│   ├── reset.css           # Base reset
│   ├── typography.css      # Font imports + type styles
│   ├── utilities.css       # Utility classes
│   └── animations.css      # Keyframes + animation utilities
├── components/
│   ├── Button/
│   ├── Card/
│   ├── Input/
│   ├── ProgressBar/
│   └── ...
└── ...
```

---

## Reference Mood Board

### Visual Inspirations
- **Thunderdome**: Industrial metal, dystopian posters, bold red/orange/black
- **Lost Lands**: Prehistoric drama, volcanic imagery, dinosaur illustrations
- **Bass Canyon**: Canyon walls, dramatic landscapes, Pacific Northwest intensity
- **Beyond Wonderland**: Dark Alice, twisted fairytale, Cheshire grin, playing cards, mushrooms
- **Apocalypse**: End-times imagery, post-apocalyptic wasteland, survival aesthetic

### Common Elements Across All
- Black/near-black backgrounds
- Bold, impactful typography (often ALL CAPS)
- Illustrated characters/creatures
- Neon or bright accents against darkness
- Texture and grain (screen-printed look)
- Layered compositions with depth
- Fantasy/otherworldly themes

---

*"The lineup isn't just a list. It's a sacred text. Treat it accordingly."*
