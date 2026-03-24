import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { useState, useEffect } from 'react';
import Navigation from './components/Navigation';
import MyGroups from './components/MyGroups';
import GroupCreation from './components/GroupCreation';
import GroupDashboard from './components/GroupDashboard';
import GroupWallet from './components/GroupWallet';
import ContributionHistory from './components/ContributionHistory';
import GroupLedger from './components/GroupLedger';
import Login from './components/Login';
import './i18n/config'; // Initialize i18n
import './App.css';

function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  useEffect(() => {
    const token = localStorage.getItem('token');
    setIsAuthenticated(!!token);
  }, []);

  const handleLogin = (token: string) => {
    localStorage.setItem('token', token);
    setIsAuthenticated(true);
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    setIsAuthenticated(false);
  };

  if (!isAuthenticated) {
    return (
      <Router>
        <div className="app">
          <Routes>
            <Route path="/login" element={<Login onLogin={handleLogin} />} />
            <Route path="*" element={<Navigate to="/login" replace />} />
          </Routes>
        </div>
      </Router>
    );
  }

  return (
    <Router>
      <div className="app">
        <Navigation onLogout={handleLogout} />
        <main className="main-content">
          <Routes>
            <Route path="/" element={<MyGroups />} />
            <Route path="/groups" element={<MyGroups />} />
            <Route path="/groups/create" element={<GroupCreation />} />
            <Route path="/groups/:id" element={<GroupDashboardWrapper />} />
            <Route path="/groups/:id/wallet" element={<GroupWalletWrapper />} />
            <Route path="/groups/:id/history" element={<ContributionHistoryWrapper />} />
            <Route path="/groups/:id/ledger" element={<GroupLedgerWrapper />} />
            <Route path="/login" element={<Navigate to="/" replace />} />
            <Route path="*" element={<NotFound />} />
          </Routes>
        </main>
      </div>
    </Router>
  );
}

function GroupDashboardWrapper() {
  const groupId = window.location.pathname.split('/')[2];
  return <GroupDashboard groupId={groupId} />;
}

function GroupWalletWrapper() {
  const groupId = window.location.pathname.split('/')[2];
  return <GroupWallet groupId={groupId} />;
}

function ContributionHistoryWrapper() {
  const groupId = window.location.pathname.split('/')[2];
  const memberPhone = localStorage.getItem('userPhone') || '';
  return <ContributionHistory groupId={groupId} memberPhone={memberPhone} />;
}

function GroupLedgerWrapper() {
  const groupId = window.location.pathname.split('/')[2];
  // We'll need to fetch the group name from the API
  return <GroupLedger groupId={groupId} groupName="Group" />;
}

function NotFound() {
  return (
    <div className="not-found">
      <h1>404 - Page Not Found</h1>
      <p>The page you're looking for doesn't exist.</p>
      <a href="/">Go to Home</a>
    </div>
  );
}

export default App;

