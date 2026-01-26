import './App.css';
import { useAuth } from './auth/useAuth';
import { LoginPage } from './components/LoginPage';

function App() {
  const { isAuthenticated, isLoading } = useAuth();

  // Show loading state during auth initialization
  if (isLoading) {
    return (
      <div className="app-container">
        <div style={{ textAlign: 'center' }}>
          <p>Loading...</p>
        </div>
      </div>
    );
  }

  // Show LoginPage when not authenticated
  if (!isAuthenticated) {
    return <LoginPage />;
  }

  // Show placeholder for authenticated state (upload page coming in Plan 03)
  return (
    <div className="app-container">
      <div className="main-content">
        <h1>Authenticated! Upload page coming soon.</h1>
      </div>
    </div>
  );
}

export default App;
