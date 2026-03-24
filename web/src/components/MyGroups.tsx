import { useEffect, useState } from 'react';
import type { GroupResponse } from '../services/api';
import './MyGroups.css';

export default function MyGroups() {
  const [groups, setGroups] = useState<GroupResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>('');

  useEffect(() => {
    loadGroups();
  }, []);

  const loadGroups = async () => {
    try {
      setLoading(true);
      // In production, this would call GET /api/v1/groups/my-groups
      // For now, we'll simulate with mock data
      setTimeout(() => {
        setGroups([]);
        setLoading(false);
      }, 500);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load groups');
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="my-groups loading">
        <div className="spinner"></div>
        <p>Loading your groups...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="my-groups error">
        <div className="alert alert-error">{error}</div>
        <button onClick={loadGroups} className="btn btn-secondary">
          Try Again
        </button>
      </div>
    );
  }

  return (
    <div className="my-groups">
      <div className="my-groups-header">
        <h1>My Groups</h1>
        <a href="/groups/create" className="btn btn-primary">
          ➕ Create New Group
        </a>
      </div>

      {groups.length === 0 ? (
        <div className="empty-state">
          <div className="empty-icon">📋</div>
          <h2>No groups yet</h2>
          <p>You haven't created or joined any stokvel groups yet.</p>
          <p>Create your first group or wait for an invitation from a Chairperson.</p>
          <a href="/groups/create" className="btn btn-primary">
            Create Your First Group
          </a>
        </div>
      ) : (
        <div className="groups-grid">
          {groups.map((group) => (
            <div key={group.id} className="group-card">
              <div className="group-card-header">
                <h3>{group.name}</h3>
                <span className={`badge badge-${group.groupType.toLowerCase()}`}>
                  {group.groupType}
                </span>
              </div>

              <p className="group-description">{group.description || 'No description provided'}</p>

              <div className="group-meta">
                <div className="meta-item">
                  <span className="meta-label">Contribution</span>
                  <span className="meta-value">
                    R{group.contributionAmount.toLocaleString('en-ZA', { minimumFractionDigits: 2 })}
                  </span>
                </div>
                <div className="meta-item">
                  <span className="meta-label">Frequency</span>
                  <span className="meta-value">{group.contributionFrequency}</span>
                </div>
                <div className="meta-item">
                  <span className="meta-label">Members</span>
                  <span className="meta-value">{group.currentMemberCount}/{group.maxMembers}</span>
                </div>
                <div className="meta-item">
                  <span className="meta-label">Status</span>
                  <span className={`status-badge status-${group.isActive ? 'active' : 'inactive'}`}>
                    {group.isActive ? 'Active' : 'Inactive'}
                  </span>
                </div>
              </div>

              <div className="group-actions">
                <a href={`/groups/${group.id}`} className="btn btn-secondary">
                  View Dashboard
                </a>
                <a href={`/groups/${group.id}/wallet`} className="btn btn-primary">
                  View Wallet
                </a>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
