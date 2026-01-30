import { useState, useEffect } from 'react';

/**
 * CONFLICTED LINEUP — VISUAL REFERENCE
 * =====================================
 * This component demonstrates the "Portal to the Undercard" 
 * design system for the festival lineup to Spotify playlist app.
 * 
 * Use this as a visual reference when retrofitting the existing app.
 * Copy patterns, not the entire component.
 */

// Inline styles using design tokens
const tokens = {
  colors: {
    void: '#0a0a0a',
    abyss: '#141414',
    smoke: '#1a1a1a',
    ash: '#2a2a2a',
    ember: '#404040',
    neonCyan: '#00f5ff',
    neonMagenta: '#ff00aa',
    toxicGreen: '#39ff14',
    blazeOrange: '#ff6a00',
    voidPurple: '#8b00ff',
    textPrimary: '#f0f0f0',
    textSecondary: '#a0a0a0',
    textMuted: '#606060',
    spotify: '#1db954',
  },
  fonts: {
    display: "'Bebas Neue', 'Anton', sans-serif",
    accent: "'Permanent Marker', cursive",
    body: "'Space Grotesk', sans-serif",
    mono: "'JetBrains Mono', monospace",
  },
};

// Noise texture SVG as data URL
const noiseTexture = `url("data:image/svg+xml,%3Csvg viewBox='0 0 200 200' xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='noise'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.65' numOctaves='3' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23noise)'/%3E%3C/svg%3E")`;

// Sample artist data
const sampleArtists = [
  { name: 'EXCISION', image: '🦖', selected: true, tracks: 47 },
  { name: 'SUBTRONICS', image: '🔊', selected: true, tracks: 38 },
  { name: 'SVDDEN DEATH', image: '💀', selected: true, tracks: 29 },
  { name: 'WOOLI', image: '🐺', selected: false, tracks: 34 },
  { name: 'SULLIVAN KING', image: '🎸', selected: true, tracks: 41 },
  { name: 'BOOGIE T', image: '🎺', selected: false, tracks: 22 },
];

export default function FestivalPlaylistReference() {
  const [activeTab, setActiveTab] = useState('upload');
  const [artists, setArtists] = useState(sampleArtists);
  const [progress, setProgress] = useState(0);
  const [isHovering, setIsHovering] = useState(false);

  // Simulate progress animation
  useEffect(() => {
    if (activeTab === 'generating') {
      const interval = setInterval(() => {
        setProgress(p => {
          if (p >= 100) {
            clearInterval(interval);
            return 100;
          }
          return p + 2;
        });
      }, 50);
      return () => clearInterval(interval);
    }
  }, [activeTab]);

  const toggleArtist = (index) => {
    setArtists(prev => prev.map((a, i) => 
      i === index ? { ...a, selected: !a.selected } : a
    ));
  };

  return (
    <div style={styles.container}>
      {/* Google Fonts */}
      <link href="https://fonts.googleapis.com/css2?family=Anton&family=Bebas+Neue&family=JetBrains+Mono:wght@400;500;600;700&family=Permanent+Marker&family=Space+Grotesk:wght@400;500;600;700&display=swap" rel="stylesheet" />
      
      {/* Noise overlay */}
      <div style={styles.noiseOverlay} />
      
      {/* Gradient mesh background */}
      <div style={styles.gradientMesh} />
      
      {/* Header */}
      <header style={styles.header}>
        <div style={styles.logo}>
          <span style={styles.logoIcon}>⚡</span>
          <span style={styles.logoText}>CONFLICTED LINEUP</span>
        </div>
        <nav style={styles.nav}>
          <button 
            style={{
              ...styles.navButton,
              ...(activeTab === 'upload' ? styles.navButtonActive : {})
            }}
            onClick={() => setActiveTab('upload')}
          >
            SEARCH
          </button>
          <button 
            style={{
              ...styles.navButton,
              ...(activeTab === 'artists' ? styles.navButtonActive : {})
            }}
            onClick={() => setActiveTab('artists')}
          >
            ARTISTS
          </button>
          <button 
            style={{
              ...styles.navButton,
              ...(activeTab === 'generating' ? styles.navButtonActive : {})
            }}
            onClick={() => { setActiveTab('generating'); setProgress(0); }}
          >
            GENERATE
          </button>
        </nav>
      </header>

      <main style={styles.main}>
        {/* === UPLOAD VIEW === */}
        {activeTab === 'upload' && (
          <section style={styles.section}>
            <h1 style={styles.heroTitle}>
              FIND YOUR<br />
              <span style={styles.heroTitleAccent}>LINEUP</span>
            </h1>
            <p style={styles.heroSubtitle}>
              Search any festival. Get a playlist. Enter the portal.
            </p>
            
            {/* Search Input (Primary Action) */}
            <div style={styles.searchContainer}>
              <input 
                type="text" 
                placeholder="Enter festival name..." 
                style={styles.searchInput}
              />
              <button style={styles.buttonSearch}>SEARCH</button>
            </div>

            {/* Example Festivals */}
            <div style={styles.examplesSection}>
              <h3 style={styles.sectionTitle}>OR PICK A FESTIVAL</h3>
              <div style={styles.examplesGrid}>
                {['LOST LANDS', 'BASS CANYON', 'BEYOND', 'THUNDERDOME'].map((fest, i) => (
                  <button key={fest} style={{
                    ...styles.exampleCard,
                    animationDelay: `${i * 100}ms`
                  }}>
                    <span style={styles.exampleEmoji}>
                      {['🦖', '🏔️', '🐰', '⚡'][i]}
                    </span>
                    <span style={styles.exampleName}>{fest}</span>
                  </button>
                ))}
              </div>
            </div>
            
            {/* Upload Fallback */}
            <div style={styles.uploadFallback}>
              <p style={styles.uploadFallbackText}>Can't find your lineup? Upload a poster instead.</p>
              <div 
                style={{
                  ...styles.uploadZone,
                  ...(isHovering ? styles.uploadZoneHover : {})
                }}
                onMouseEnter={() => setIsHovering(true)}
                onMouseLeave={() => setIsHovering(false)}
              >
                <div style={styles.uploadIcon}>📸</div>
                <p style={styles.uploadText}>DROP POSTER HERE</p>
                <p style={styles.uploadSubtext}>PNG, JPG, WEBP</p>
              </div>
            </div>
          </section>
        )}

        {/* === ARTISTS VIEW === */}
        {activeTab === 'artists' && (
          <section style={styles.section}>
            <div style={styles.artistsHeader}>
              <h2 style={styles.pageTitle}>THE LINEUP</h2>
              <div style={styles.artistsActions}>
                <button style={styles.buttonGhost}>SELECT ALL</button>
                <button style={styles.buttonGhost}>DESELECT ALL</button>
              </div>
            </div>
            
            <p style={styles.artistsSubtitle}>
              {artists.filter(a => a.selected).length} of {artists.length} artists selected
            </p>
            
            {/* Artists Grid */}
            <div style={styles.artistsGrid}>
              {artists.map((artist, index) => (
                <div 
                  key={artist.name}
                  style={{
                    ...styles.artistCard,
                    ...(artist.selected ? styles.artistCardSelected : styles.artistCardUnselected),
                  }}
                  onClick={() => toggleArtist(index)}
                >
                  <div style={styles.artistImage}>
                    <span style={{ fontSize: '3rem' }}>{artist.image}</span>
                  </div>
                  <div style={styles.artistInfo}>
                    <h4 style={styles.artistName}>{artist.name}</h4>
                    <p style={styles.artistTracks}>{artist.tracks} tracks available</p>
                  </div>
                  <div style={{
                    ...styles.artistCheck,
                    ...(artist.selected ? styles.artistCheckSelected : {})
                  }}>
                    {artist.selected ? '✓' : ''}
                  </div>
                </div>
              ))}
            </div>

            {/* Generate Button */}
            <div style={styles.generateSection}>
              <button 
                style={styles.buttonPrimary}
                onClick={() => { setActiveTab('generating'); setProgress(0); }}
              >
                GENERATE PLAYLIST →
              </button>
            </div>
          </section>
        )}

        {/* === GENERATING VIEW === */}
        {activeTab === 'generating' && (
          <section style={styles.section}>
            <h2 style={styles.pageTitle}>
              {progress < 100 ? 'BUILDING YOUR SET' : 'PLAYLIST READY'}
            </h2>
            
            {/* Progress Section */}
            <div style={styles.progressSection}>
              <div style={styles.progressBar}>
                <div style={{
                  ...styles.progressFill,
                  width: `${progress}%`
                }} />
                <div style={styles.progressGlow} />
              </div>
              
              <div style={styles.progressStats}>
                <div style={styles.statBox}>
                  <span style={styles.statValue}>{Math.floor(progress * 1.47)}</span>
                  <span style={styles.statLabel}>TRACKS</span>
                </div>
                <div style={styles.statBox}>
                  <span style={styles.statValue}>{artists.filter(a => a.selected).length}</span>
                  <span style={styles.statLabel}>ARTISTS</span>
                </div>
                <div style={styles.statBox}>
                  <span style={styles.statValue}>{Math.floor(progress * 0.09)}h {Math.floor((progress * 5.3) % 60)}m</span>
                  <span style={styles.statLabel}>DURATION</span>
                </div>
              </div>
            </div>

            {progress >= 100 && (
              <div style={styles.successSection}>
                <div style={styles.successIcon}>🎉</div>
                <button style={styles.buttonSpotify}>
                  <span style={styles.spotifyIcon}>●</span>
                  OPEN IN SPOTIFY
                </button>
                <div style={styles.secondaryActions}>
                  <button style={styles.buttonSecondary}>COPY LINK</button>
                  <button style={styles.buttonSecondary}>SHARE</button>
                </div>
              </div>
            )}
          </section>
        )}
      </main>

      {/* Component Library Section */}
      <section style={styles.componentLibrary}>
        <h2 style={styles.libraryTitle}>COMPONENT REFERENCE</h2>
        
        {/* Buttons */}
        <div style={styles.componentGroup}>
          <h3 style={styles.componentGroupTitle}>BUTTONS</h3>
          <div style={styles.componentRow}>
            <button style={styles.buttonPrimary}>PRIMARY ACTION</button>
            <button style={styles.buttonSecondary}>SECONDARY</button>
            <button style={styles.buttonGhost}>GHOST</button>
            <button style={styles.buttonSpotify}>
              <span style={styles.spotifyIcon}>●</span>
              SPOTIFY
            </button>
          </div>
        </div>

        {/* Colors */}
        <div style={styles.componentGroup}>
          <h3 style={styles.componentGroupTitle}>NEON PALETTE</h3>
          <div style={styles.colorRow}>
            {[
              { name: 'CYAN', color: tokens.colors.neonCyan },
              { name: 'MAGENTA', color: tokens.colors.neonMagenta },
              { name: 'GREEN', color: tokens.colors.toxicGreen },
              { name: 'ORANGE', color: tokens.colors.blazeOrange },
              { name: 'PURPLE', color: tokens.colors.voidPurple },
            ].map(({ name, color }) => (
              <div key={name} style={styles.colorSwatch}>
                <div style={{
                  ...styles.colorCircle,
                  backgroundColor: color,
                  boxShadow: `0 0 20px ${color}80`
                }} />
                <span style={styles.colorName}>{name}</span>
                <span style={styles.colorHex}>{color}</span>
              </div>
            ))}
          </div>
        </div>

        {/* Typography */}
        <div style={styles.componentGroup}>
          <h3 style={styles.componentGroupTitle}>TYPOGRAPHY</h3>
          <div style={styles.typeScale}>
            <p style={{ ...styles.typeDemo, fontFamily: tokens.fonts.display, fontSize: '3rem' }}>
              BEBAS NEUE — DISPLAY
            </p>
            <p style={{ ...styles.typeDemo, fontFamily: tokens.fonts.accent, fontSize: '1.5rem' }}>
              Permanent Marker — Accent
            </p>
            <p style={{ ...styles.typeDemo, fontFamily: tokens.fonts.body, fontSize: '1rem' }}>
              Space Grotesk — Body text for reading
            </p>
            <p style={{ ...styles.typeDemo, fontFamily: tokens.fonts.mono, fontSize: '0.875rem' }}>
              JetBrains Mono — Technical/code
            </p>
          </div>
        </div>

        {/* Input */}
        <div style={styles.componentGroup}>
          <h3 style={styles.componentGroupTitle}>INPUTS</h3>
          <div style={styles.inputDemo}>
            <input 
              type="text" 
              placeholder="Search artists..." 
              style={styles.inputField}
            />
            <input 
              type="text" 
              placeholder="Focused state" 
              style={{...styles.inputField, ...styles.inputFieldFocus}}
            />
          </div>
        </div>
      </section>
    </div>
  );
}

// Comprehensive styles object
const styles = {
  container: {
    position: 'relative',
    minHeight: '100vh',
    backgroundColor: tokens.colors.void,
    color: tokens.colors.textPrimary,
    fontFamily: tokens.fonts.body,
    overflow: 'hidden',
  },
  
  noiseOverlay: {
    position: 'fixed',
    inset: 0,
    backgroundImage: noiseTexture,
    opacity: 0.03,
    pointerEvents: 'none',
    zIndex: 100,
  },
  
  gradientMesh: {
    position: 'fixed',
    inset: 0,
    background: `
      radial-gradient(ellipse at 20% 80%, rgba(255, 0, 170, 0.12) 0%, transparent 50%),
      radial-gradient(ellipse at 80% 20%, rgba(0, 245, 255, 0.08) 0%, transparent 50%),
      radial-gradient(ellipse at 50% 50%, rgba(139, 0, 255, 0.06) 0%, transparent 60%)
    `,
    pointerEvents: 'none',
    zIndex: 0,
  },
  
  // Header
  header: {
    position: 'relative',
    zIndex: 10,
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: '1.5rem 2rem',
    borderBottom: `1px solid ${tokens.colors.ash}`,
  },
  
  logo: {
    display: 'flex',
    alignItems: 'center',
    gap: '0.75rem',
  },
  
  logoIcon: {
    fontSize: '1.5rem',
  },
  
  logoText: {
    fontFamily: tokens.fonts.display,
    fontSize: '1.5rem',
    letterSpacing: '-0.02em',
    color: tokens.colors.neonCyan,
  },
  
  nav: {
    display: 'flex',
    gap: '0.5rem',
  },
  
  navButton: {
    background: 'transparent',
    border: 'none',
    padding: '0.5rem 1rem',
    fontFamily: tokens.fonts.body,
    fontSize: '0.875rem',
    fontWeight: 500,
    color: tokens.colors.textSecondary,
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    borderRadius: '4px',
  },
  
  navButtonActive: {
    color: tokens.colors.neonCyan,
    backgroundColor: `${tokens.colors.neonCyan}15`,
  },
  
  // Main content
  main: {
    position: 'relative',
    zIndex: 1,
    maxWidth: '1200px',
    margin: '0 auto',
    padding: '3rem 2rem',
  },
  
  section: {
    animation: 'fadeUp 0.6s ease-out forwards',
  },
  
  // Hero/Upload view
  heroTitle: {
    fontFamily: tokens.fonts.display,
    fontSize: 'clamp(3rem, 10vw, 6rem)',
    lineHeight: 1,
    letterSpacing: '-0.02em',
    margin: '0 0 1rem 0',
    textAlign: 'center',
  },
  
  heroTitleAccent: {
    color: tokens.colors.neonCyan,
    textShadow: `0 0 40px ${tokens.colors.neonCyan}60`,
  },
  
  heroSubtitle: {
    fontSize: '1.25rem',
    color: tokens.colors.textSecondary,
    textAlign: 'center',
    marginBottom: '2rem',
  },
  
  // Search (Primary Action)
  searchContainer: {
    display: 'flex',
    gap: '0.5rem',
    maxWidth: '600px',
    margin: '0 auto 2rem',
  },
  
  searchInput: {
    flex: 1,
    padding: '1rem 1.5rem',
    background: tokens.colors.smoke,
    border: `2px solid ${tokens.colors.ash}`,
    borderRadius: '4px',
    fontFamily: tokens.fonts.body,
    fontSize: '1.125rem',
    color: tokens.colors.textPrimary,
    outline: 'none',
    transition: 'all 0.2s ease',
  },
  
  buttonSearch: {
    padding: '1rem 2rem',
    background: `linear-gradient(135deg, ${tokens.colors.neonCyan}, ${tokens.colors.voidPurple})`,
    border: 'none',
    borderRadius: '4px',
    fontFamily: tokens.fonts.display,
    fontSize: '1.125rem',
    letterSpacing: '0.02em',
    color: tokens.colors.void,
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    whiteSpace: 'nowrap',
  },
  
  // Upload Fallback
  uploadFallback: {
    marginTop: '3rem',
    paddingTop: '2rem',
    borderTop: `1px solid ${tokens.colors.ash}`,
    textAlign: 'center',
  },
  
  uploadFallbackText: {
    color: tokens.colors.textMuted,
    fontSize: '0.875rem',
    marginBottom: '1rem',
  },
  
  uploadZone: {
    position: 'relative',
    border: `2px dashed ${tokens.colors.ash}`,
    borderRadius: '8px',
    padding: '2rem 1.5rem',
    textAlign: 'center',
    cursor: 'pointer',
    transition: 'all 0.3s ease',
    backgroundColor: `${tokens.colors.smoke}50`,
    overflow: 'hidden',
    maxWidth: '400px',
    margin: '0 auto',
  },
  
  uploadZoneHover: {
    borderColor: tokens.colors.neonMagenta,
    backgroundColor: `${tokens.colors.neonMagenta}08`,
    boxShadow: `0 0 20px ${tokens.colors.neonMagenta}20`,
  },
  
  uploadIcon: {
    fontSize: '2rem',
    marginBottom: '0.5rem',
    filter: 'grayscale(50%)',
  },
  
  uploadText: {
    fontFamily: tokens.fonts.display,
    fontSize: '1rem',
    letterSpacing: '0.05em',
    marginBottom: '0.25rem',
    color: tokens.colors.textSecondary,
  },
  
  uploadSubtext: {
    color: tokens.colors.textMuted,
    fontSize: '0.75rem',
  },
  
  // Examples section
  examplesSection: {
    marginTop: '4rem',
  },
  
  sectionTitle: {
    fontFamily: tokens.fonts.display,
    fontSize: '1rem',
    letterSpacing: '0.1em',
    color: tokens.colors.textMuted,
    textAlign: 'center',
    marginBottom: '1.5rem',
  },
  
  examplesGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))',
    gap: '1rem',
    maxWidth: '600px',
    margin: '0 auto',
  },
  
  exampleCard: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: '0.5rem',
    padding: '1.5rem 1rem',
    background: tokens.colors.abyss,
    border: `2px solid ${tokens.colors.ash}`,
    borderRadius: '4px',
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    fontFamily: tokens.fonts.body,
  },
  
  exampleEmoji: {
    fontSize: '2rem',
  },
  
  exampleName: {
    fontFamily: tokens.fonts.display,
    fontSize: '0.875rem',
    letterSpacing: '0.05em',
    color: tokens.colors.textSecondary,
  },
  
  // Artists view
  artistsHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: '0.5rem',
  },
  
  pageTitle: {
    fontFamily: tokens.fonts.display,
    fontSize: '2.5rem',
    letterSpacing: '-0.02em',
    margin: 0,
  },
  
  artistsActions: {
    display: 'flex',
    gap: '0.5rem',
  },
  
  artistsSubtitle: {
    color: tokens.colors.textMuted,
    marginBottom: '2rem',
  },
  
  artistsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
    gap: '1rem',
    marginBottom: '2rem',
  },
  
  artistCard: {
    display: 'flex',
    alignItems: 'center',
    gap: '1rem',
    padding: '1rem',
    background: tokens.colors.abyss,
    border: `2px solid ${tokens.colors.ash}`,
    borderRadius: '4px',
    cursor: 'pointer',
    transition: 'all 0.2s ease',
  },
  
  artistCardSelected: {
    borderColor: tokens.colors.neonCyan,
    boxShadow: `0 0 20px ${tokens.colors.neonCyan}30`,
  },
  
  artistCardUnselected: {
    opacity: 0.6,
    filter: 'grayscale(50%)',
  },
  
  artistImage: {
    width: '60px',
    height: '60px',
    background: tokens.colors.smoke,
    borderRadius: '4px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  
  artistInfo: {
    flex: 1,
  },
  
  artistName: {
    fontFamily: tokens.fonts.display,
    fontSize: '1.25rem',
    margin: '0 0 0.25rem 0',
  },
  
  artistTracks: {
    margin: 0,
    fontSize: '0.875rem',
    color: tokens.colors.textMuted,
  },
  
  artistCheck: {
    width: '28px',
    height: '28px',
    border: `2px solid ${tokens.colors.ash}`,
    borderRadius: '4px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    fontSize: '1rem',
    fontWeight: 'bold',
    transition: 'all 0.2s ease',
  },
  
  artistCheckSelected: {
    background: tokens.colors.neonCyan,
    borderColor: tokens.colors.neonCyan,
    color: tokens.colors.void,
  },
  
  generateSection: {
    display: 'flex',
    justifyContent: 'center',
    marginTop: '2rem',
  },
  
  // Progress/Generating view
  progressSection: {
    maxWidth: '600px',
    margin: '3rem auto',
  },
  
  progressBar: {
    position: 'relative',
    height: '8px',
    background: tokens.colors.ash,
    borderRadius: '4px',
    overflow: 'hidden',
    marginBottom: '2rem',
  },
  
  progressFill: {
    position: 'absolute',
    top: 0,
    left: 0,
    height: '100%',
    background: `linear-gradient(90deg, ${tokens.colors.neonCyan}, ${tokens.colors.voidPurple})`,
    borderRadius: '4px',
    transition: 'width 0.1s linear',
  },
  
  progressGlow: {
    position: 'absolute',
    top: '-10px',
    left: 0,
    right: 0,
    bottom: '-10px',
    background: `linear-gradient(90deg, ${tokens.colors.neonCyan}40, ${tokens.colors.voidPurple}40)`,
    filter: 'blur(10px)',
    opacity: 0.5,
  },
  
  progressStats: {
    display: 'flex',
    justifyContent: 'center',
    gap: '3rem',
  },
  
  statBox: {
    textAlign: 'center',
  },
  
  statValue: {
    display: 'block',
    fontFamily: tokens.fonts.mono,
    fontSize: '2rem',
    fontWeight: 'bold',
    color: tokens.colors.neonCyan,
  },
  
  statLabel: {
    fontSize: '0.75rem',
    letterSpacing: '0.1em',
    color: tokens.colors.textMuted,
  },
  
  successSection: {
    textAlign: 'center',
    marginTop: '3rem',
    animation: 'scaleIn 0.5s ease-out forwards',
  },
  
  successIcon: {
    fontSize: '4rem',
    marginBottom: '1.5rem',
  },
  
  secondaryActions: {
    display: 'flex',
    justifyContent: 'center',
    gap: '1rem',
    marginTop: '1rem',
  },
  
  // Buttons
  buttonPrimary: {
    display: 'inline-flex',
    alignItems: 'center',
    gap: '0.5rem',
    padding: '1rem 2rem',
    background: `linear-gradient(135deg, ${tokens.colors.neonCyan}, ${tokens.colors.voidPurple})`,
    border: 'none',
    borderRadius: '4px',
    fontFamily: tokens.fonts.display,
    fontSize: '1.25rem',
    letterSpacing: '0.02em',
    color: tokens.colors.void,
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    boxShadow: `0 0 30px ${tokens.colors.neonCyan}40`,
  },
  
  buttonSecondary: {
    display: 'inline-flex',
    alignItems: 'center',
    gap: '0.5rem',
    padding: '0.75rem 1.5rem',
    background: 'transparent',
    border: `2px solid ${tokens.colors.neonMagenta}`,
    borderRadius: '4px',
    fontFamily: tokens.fonts.body,
    fontSize: '0.875rem',
    fontWeight: 600,
    letterSpacing: '0.02em',
    color: tokens.colors.neonMagenta,
    cursor: 'pointer',
    transition: 'all 0.2s ease',
  },
  
  buttonGhost: {
    display: 'inline-flex',
    alignItems: 'center',
    gap: '0.5rem',
    padding: '0.5rem 1rem',
    background: tokens.colors.smoke,
    border: `1px solid ${tokens.colors.ash}`,
    borderRadius: '4px',
    fontFamily: tokens.fonts.body,
    fontSize: '0.75rem',
    fontWeight: 500,
    letterSpacing: '0.05em',
    color: tokens.colors.textSecondary,
    cursor: 'pointer',
    transition: 'all 0.2s ease',
  },
  
  buttonSpotify: {
    display: 'inline-flex',
    alignItems: 'center',
    gap: '0.75rem',
    padding: '1rem 2rem',
    background: tokens.colors.spotify,
    border: 'none',
    borderRadius: '9999px',
    fontFamily: tokens.fonts.body,
    fontSize: '1rem',
    fontWeight: 600,
    color: '#fff',
    cursor: 'pointer',
    transition: 'all 0.2s ease',
  },
  
  spotifyIcon: {
    fontSize: '1.5rem',
  },
  
  // Component Library section
  componentLibrary: {
    position: 'relative',
    zIndex: 1,
    maxWidth: '1200px',
    margin: '4rem auto 0',
    padding: '3rem 2rem',
    borderTop: `1px solid ${tokens.colors.ash}`,
  },
  
  libraryTitle: {
    fontFamily: tokens.fonts.display,
    fontSize: '1.5rem',
    letterSpacing: '0.1em',
    color: tokens.colors.textMuted,
    marginBottom: '2rem',
  },
  
  componentGroup: {
    marginBottom: '2.5rem',
  },
  
  componentGroupTitle: {
    fontFamily: tokens.fonts.mono,
    fontSize: '0.75rem',
    letterSpacing: '0.1em',
    color: tokens.colors.textMuted,
    marginBottom: '1rem',
  },
  
  componentRow: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: '1rem',
    alignItems: 'center',
  },
  
  colorRow: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: '2rem',
  },
  
  colorSwatch: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: '0.5rem',
  },
  
  colorCircle: {
    width: '60px',
    height: '60px',
    borderRadius: '50%',
  },
  
  colorName: {
    fontFamily: tokens.fonts.display,
    fontSize: '0.75rem',
    letterSpacing: '0.1em',
  },
  
  colorHex: {
    fontFamily: tokens.fonts.mono,
    fontSize: '0.625rem',
    color: tokens.colors.textMuted,
  },
  
  typeScale: {
    display: 'flex',
    flexDirection: 'column',
    gap: '1rem',
  },
  
  typeDemo: {
    margin: 0,
    color: tokens.colors.textPrimary,
  },
  
  inputDemo: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: '1rem',
  },
  
  inputField: {
    padding: '0.75rem 1rem',
    background: tokens.colors.smoke,
    border: `2px solid ${tokens.colors.ash}`,
    borderRadius: '4px',
    fontFamily: tokens.fonts.body,
    fontSize: '1rem',
    color: tokens.colors.textPrimary,
    outline: 'none',
    transition: 'all 0.2s ease',
    width: '250px',
  },
  
  inputFieldFocus: {
    borderColor: tokens.colors.neonCyan,
    boxShadow: `0 0 20px ${tokens.colors.neonCyan}30`,
  },
};

// Add keyframes via style tag
const styleSheet = document.createElement('style');
styleSheet.textContent = `
  @keyframes fadeUp {
    from {
      opacity: 0;
      transform: translateY(20px);
    }
    to {
      opacity: 1;
      transform: translateY(0);
    }
  }
  
  @keyframes scaleIn {
    from {
      opacity: 0;
      transform: scale(0.9);
    }
    to {
      opacity: 1;
      transform: scale(1);
    }
  }
  
  @keyframes spin {
    from { transform: rotate(0deg); }
    to { transform: rotate(360deg); }
  }
`;
document.head.appendChild(styleSheet);
