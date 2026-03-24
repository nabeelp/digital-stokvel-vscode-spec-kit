import { useState } from 'react';
import './Login.css';

interface LoginProps {
  onLogin: (token: string) => void;
}

export default function Login({ onLogin }: LoginProps) {
  const [phoneNumber, setPhoneNumber] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!phoneNumber) {
      setError('Phone number is required');
      return;
    }

    if (!/^27\d{9}$/.test(phoneNumber)) {
      setError('Phone number must be in format 27XXXXXXXXX (11 digits starting with 27)');
      return;
    }

    setLoading(true);

    // Simulate API call - in production, this would call Auth0 or backend authentication
    setTimeout(() => {
      const mockToken = `mock-jwt-token-${phoneNumber}-${Date.now()}`;
      onLogin(mockToken);
      setLoading(false);
    }, 1000);
  };

  return (
    <div className="login-container">
      <div className="login-card">
        <div className="login-header">
          <div className="login-logo">
            <span className="logo-icon">🏦</span>
            <h1>Digital Stokvel</h1>
          </div>
          <p className="login-subtitle">Community Banking Made Simple</p>
        </div>

        <form onSubmit={handleSubmit} className="login-form">
          {error && (
            <div className="alert alert-error">
              {error}
            </div>
          )}

          <div className="form-group">
            <label htmlFor="phoneNumber">Phone Number</label>
            <input
              type="tel"
              id="phoneNumber"
              value={phoneNumber}
              onChange={(e) => setPhoneNumber(e.target.value)}
              placeholder="27812345678"
              className="form-input"
              disabled={loading}
            />
            <small className="form-help">Enter your phone number in format: 27XXXXXXXXX</small>
          </div>

          <button type="submit" className="btn btn-primary btn-block" disabled={loading}>
            {loading ? 'Signing in...' : 'Sign In'}
          </button>
        </form>

        <div className="login-footer">
          <div className="security-badges">
            <span className="badge">🛡️ FSCA Protected</span>
            <span className="badge">🔒 Bank-Grade Security</span>
          </div>
          <p className="disclaimer">
            ℹ️ This is a demo environment. In production, authentication will use Auth0 with OTP verification.
          </p>
        </div>
      </div>
    </div>
  );
}
