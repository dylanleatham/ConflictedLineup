import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import './styles/tokens.css';
import './App.css';
import App from './App.tsx';
import { AuthProvider } from './auth/AuthProvider';
import { clearStoredTokens } from './auth/SpotifyAuthConfig';
import { AppConfigContext } from './AppConfigContext';
import { loadAppConfig } from './config';

// Clear stale tokens if the session expired, to prevent a refresh loop
if (new URLSearchParams(window.location.search).has('session_expired')) {
  clearStoredTokens();
  window.history.replaceState({}, '', '/');
}

loadAppConfig().then((config) => {
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <AppConfigContext.Provider value={config}>
        <BrowserRouter>
          <AuthProvider demoMode={config.demoMode}>
            <App />
          </AuthProvider>
        </BrowserRouter>
      </AppConfigContext.Provider>
    </StrictMode>,
  );
});
