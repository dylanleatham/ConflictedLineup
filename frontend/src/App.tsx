import './App.css';
import { useAuth } from './auth';
import { Header } from './components/Header';
import { LoginPage } from './components/LoginPage';
import { UploadPage } from './pages/UploadPage';

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

  // Show Header and UploadPage when authenticated
  return (
    <div className="app-container">
      <Header />
      <UploadPage />
    </div>
  );
}

export default App;
