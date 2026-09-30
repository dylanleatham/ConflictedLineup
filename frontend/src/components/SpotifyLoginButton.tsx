import { SpotifyIcon } from './SpotifyIcon';

interface SpotifyLoginButtonProps {
  onClick: () => void;
  disabled?: boolean;
  isLoading?: boolean;
  label?: string;
}

export function SpotifyLoginButton({ onClick, disabled = false, isLoading = false, label = 'Log in with Spotify' }: SpotifyLoginButtonProps) {
  const buttonText = isLoading ? 'Connecting...' : label;
  const isDisabled = disabled || isLoading;

  return (
    <button
      className="spotify-login-button"
      onClick={onClick}
      disabled={isDisabled}
      style={{
        backgroundColor: 'var(--color-spotify)',
        color: '#ffffff',
        border: 'none',
        borderRadius: 'var(--radius-full)',
        padding: '14px 32px',
        fontSize: '16px',
        fontWeight: 600,
        fontFamily: 'var(--font-body)',
        display: 'inline-flex',
        alignItems: 'center',
        gap: '12px',
        cursor: isDisabled ? 'not-allowed' : 'pointer',
        opacity: isDisabled ? 0.6 : 1,
        transition: 'var(--transition-fast)',
        boxShadow: isDisabled ? 'none' : '0 0 20px rgba(29, 185, 84, 0.4)',
        minHeight: '52px',
      }}
      onMouseEnter={(e) => {
        if (!isDisabled) {
          e.currentTarget.style.transform = 'scale(1.02)';
          e.currentTarget.style.boxShadow = '0 0 30px rgba(29, 185, 84, 0.5)';
        }
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.transform = 'scale(1)';
        e.currentTarget.style.boxShadow = '0 0 20px rgba(29, 185, 84, 0.4)';
      }}
    >
      <SpotifyIcon size={24} />
      {buttonText}
    </button>
  );
}
