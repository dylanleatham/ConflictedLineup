import { Routes, Route, Navigate } from 'react-router-dom';
import './App.css';
import { useAuth } from './auth';
import { Header } from './components/Header';
import { LoginPage } from './components/LoginPage';
import { UploadPage } from './pages/UploadPage';
import { TrackSelectionPage } from './pages/TrackSelectionPage';
import { PlaylistResultsPage } from './pages/PlaylistResultsPage';

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
