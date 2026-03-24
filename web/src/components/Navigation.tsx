import LanguageSelector from './LanguageSelector';
import './Navigation.css';

interface NavigationProps {
  onLogout: () => void;
}

export default function Navigation({ onLogout }: NavigationProps) {
  return (
    <nav className="navigation">
      <div className="nav-container">
        <div className="nav-logo">
          <span className="logo-icon">🏦</span>
          <span className="logo-text">Digital Stokvel</span>
        </div>

        <div className="nav-links">
          <a href="/groups/create" className="nav-link">
            ➕ Create Group
          </a>
          <a href="/" className="nav-link">
            📊 My Groups
          </a>
        </div>

        <div className="nav-actions">
          <LanguageSelector />
          <button onClick={onLogout} className="btn-logout">
            🚪 Logout
          </button>
        </div>
      </div>
    </nav>
  );
}
