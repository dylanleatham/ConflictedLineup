import { SpotifyIcon } from './SpotifyIcon';

interface SpotifyLoginButtonProps {
  onClick: () => void;
  disabled?: boolean;
  isLoading?: boolean;
}

export function SpotifyLoginButton({ onClick, disabled = false, isLoading = false }: SpotifyLoginButtonProps) {
  const buttonText = isLoading ? 'Connecting...' : 'Log in with Spotify';
  const isDisabled = disabled || isLoading;

  return (
    <button
      className="spotify-login-button"
      onClick={onClick}
      disabled={isDisabled}
      style={{
        backgroundColor: '#1ED760',
        color: '#000000',
        border: 'none',
        borderRadius: '500px',
        padding: '14px 32px',
        fontSize: '16px',
        fontWeight: 700,
        display: 'inline-flex',
        alignItems: 'center',
        gap: '8px',
        cursor: isDisabled ? 'not-allowed' : 'pointer',
        opacity: isDisabled ? 0.6 : 1,
        transition: 'all 0.2s ease',
      }}
      onMouseEnter={(e) => {
        if (!isDisabled) {
          e.currentTarget.style.backgroundColor = '#1DB954';
        }
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.backgroundColor = '#1ED760';
      }}
    >
      <SpotifyIcon size={24} />
      {buttonText}
    </button>
  );
}
