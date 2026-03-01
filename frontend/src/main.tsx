import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import './styles/tokens.css'
import './App.css'
import App from './App.tsx'
import { AuthProvider } from './auth/AuthProvider'

const basePath = import.meta.env.BASE_URL;
// BrowserRouter basename must not have a trailing slash, but Vite's BASE_URL always includes one
const routerBasename = basePath.replace(/\/+$/, '') || '/';

// Clear stale tokens if session expired to prevent refresh loop
if (window.location.search.includes('session_expired=true')) {
  localStorage.removeItem('ROCP_token');
  localStorage.removeItem('ROCP_refreshToken');
  localStorage.removeItem('ROCP_idToken');
  // Clean up URL
  window.history.replaceState({}, '', basePath);
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter basename={routerBasename}>
      <AuthProvider>
        <App />
      </AuthProvider>
    </BrowserRouter>
  </StrictMode>,
)
