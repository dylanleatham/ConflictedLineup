import { SpotifyProfile } from '../auth/types';

interface UserProfileProps {
  profile: SpotifyProfile | null;
  onClick: () => void;
}

export function UserProfile({ profile, onClick }: UserProfileProps) {
  const displayName = profile?.display_name || 'User';
  const avatarUrl = profile?.images?.[0]?.url;

  return (
    <div
      className="user-profile"
      onClick={onClick}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => e.key === 'Enter' && onClick()}
    >
      <div className="user-avatar">
        {avatarUrl ? (
          <img src={avatarUrl} alt={displayName} />
        ) : (
          <div className="avatar-placeholder">
            {displayName.charAt(0).toUpperCase()}
          </div>
        )}
      </div>
      <span className="user-name">{displayName}</span>
      <svg
        className="chevron-icon"
        width="16"
        height="16"
        viewBox="0 0 16 16"
        fill="none"
      >
        <path
          d="M4 6L8 10L12 6"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
    </div>
  );
}
