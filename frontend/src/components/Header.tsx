import { useState } from 'react';
import { useAuth } from '../auth';
import { UserProfile } from './UserProfile';
import { ProfileDropdown } from './ProfileDropdown';

export function Header() {
  const { isAuthenticated, profile, logout } = useAuth();
  const [isDropdownOpen, setIsDropdownOpen] = useState(false);

  if (!isAuthenticated) {
    return null;
  }

  const handleLogout = () => {
    setIsDropdownOpen(false);
    logout();
  };

  return (
    <header className="app-header">
      <div className="header-content">
        <h1 className="header-logo">Conflicted Lineup</h1>
        <div className="header-right">
          <div className="profile-container">
            <UserProfile
              profile={profile}
              onClick={() => setIsDropdownOpen(!isDropdownOpen)}
            />
            <ProfileDropdown
              isOpen={isDropdownOpen}
              onClose={() => setIsDropdownOpen(false)}
              onLogout={handleLogout}
            />
          </div>
        </div>
      </div>
    </header>
  );
}
