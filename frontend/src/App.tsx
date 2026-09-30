import { useEffect } from 'react';
import { Routes, Route, Navigate, useLocation } from 'react-router-dom';
import './App.css';
import { useAuth } from './auth';
import { Header } from './components/Header';
import { LoginPage } from './components/LoginPage';
import { UploadPage } from './pages/UploadPage';
import { TrackSelectionPage } from './pages/TrackSelectionPage';
import { PlaylistResultsPage } from './pages/PlaylistResultsPage';

/**
 * Client-side navigation keeps the window's scroll position, so moving from a long track list to the
 * results page would land mid-page, below the "Playlist Created!" banner.
 */
function ScrollToTopOnNavigate() {
  const { pathname } = useLocation();
  useEffect(() => {
    window.scrollTo(0, 0);
  }, [pathname]);
  return null;
}

function App() {
  const { isAuthenticated, isLoading } = useAuth();

  // Show loading state during auth initialization
  if (isLoading) {
    return (
      <div className="app-container">
        <div className="main-content" style={{ display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
          <p>Loading...</p>
        </div>
      </div>
    );
  }

  // Show LoginPage when not authenticated
  if (!isAuthenticated) {
    return <LoginPage />;
  }

  // Show Header and routed pages when authenticated
  return (
    <div className="app-container">
      <ScrollToTopOnNavigate />
      <Header />
      <Routes>
        <Route path="/" element={<UploadPage />} />
        <Route path="/track-selection" element={<TrackSelectionPage />} />
        <Route path="/results" element={<PlaylistResultsPage />} />
        <Route path="/callback" element={<Navigate to="/" replace />} />
      </Routes>
    </div>
  );
}

export default App;
