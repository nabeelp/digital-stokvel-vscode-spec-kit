import { useEffect, useState } from 'react';
import { apiService } from '../services/api';
import type { GroupResponse } from '../services/api';
import InviteMember from './InviteMember';
import AssignRole from './AssignRole';
import './GroupDashboard.css';

interface GroupDashboardProps {
  groupId: string;
}

export default function GroupDashboard({ groupId }: GroupDashboardProps) {
  const [group, setGroup] = useState<GroupResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [searchQuery, setSearchQuery] = useState('');
  const [showInviteModal, setShowInviteModal] = useState(false);
  const [showRoleModal, setShowRoleModal] = useState(false);
  const [selectedMember, setSelectedMember] = useState<{ phone: string; role: string } | null>(null);

  useEffect(() => {
    loadGroup();
  }, [groupId]);

  const loadGroup = async () => {
    try {
      setLoading(true);
      const response = await apiService.getGroup(groupId);
      setGroup(response.data);
      setError('');
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load group details');
    } finally {
      setLoading(false);
    }
  };

  const filteredMembers = group?.members?.filter((member) => {
    if (!searchQuery) return true;
    const query = searchQuery.toLowerCase();
    return (
      member.phoneNumber.toLowerCase().includes(query) ||
      member.role.toLowerCase().includes(query)
    );
  }) || [];

  const getRoleBadgeClass = (role: string) => {
    switch (role) {
      case 'Chairperson':
        return 'badge-chairperson';
      case 'Treasurer':
        return 'badge-treasurer';
      case 'Secretary':
        return 'badge-secretary';
      default:
        return 'badge-member';
    }
  };

  if (loading) {
    return (
      <div className="group-dashboard loading">
        <div className="spinner"></div>
        <p>Loading group details...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="group-dashboard error">
        <div className="alert alert-error">{error}</div>
        <button onClick={loadGroup} className="btn btn-secondary">
          Try Again
        </button>
      </div>
    );
  }

  if (!group) {
    return <div className="group-dashboard">Group not found</div>;
  }

  const isFull = group.currentMemberCount >= group.maxMembers;
  const showWarning = group.currentMemberCount > 50;

  return (
    <div className="group-dashboard">
      <div className="dashboard-header">
        <div className="group-info">
          <h1>{group.name}</h1>
          <p className="group-description">{group.description}</p>
          <div className="group-meta">
            <span className="meta-item">
              📊 {group.groupType}
            </span>
            <span className="meta-item">
              💰 R{group.contributionAmount.toLocaleString()} {group.contributionFrequency}
            </span>
            <span className="meta-item">
              👥 {group.currentMemberCount} members
            </span>
          </div>
        </div>

        <div className="group-stats">
          <div className="stat-card">
            <div className="stat-label">Group Balance</div>
            <div className="stat-value">R{group.balance.toLocaleString('en-ZA', { minimumFractionDigits: 2 })}</div>
          </div>
          <div className="stat-card">
            <div className="stat-label">Account Number</div>
            <div className="stat-value">{group.groupSavingsAccountNumber || 'Pending'}</div>
          </div>
        </div>
      </div>

      {showWarning && (
        <div className="alert alert-warning">
          ⚠️ Larger groups may experience performance considerations. Current members: {group.currentMemberCount}
        </div>
      )}

      {isFull && (
        <div className="alert alert-info">
          ℹ️ This group has reached its maximum capacity of {group.maxMembers} members.
        </div>
      )}

      <div className="roster-section">
        <div className="roster-header">
          <h2>Group Roster</h2>
          <div className="roster-actions">
            <input
              type="text"
              placeholder="Search members..."
              className="search-input"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
            <button 
              className="btn btn-secondary" 
              onClick={() => window.location.href = `/groups/${groupId}/history`}
            >
              📋 My Contributions
            </button>
            <button 
              className="btn btn-secondary" 
              onClick={() => window.location.href = `/groups/${groupId}/ledger`}
            >
              📊 Group Ledger
            </button>
            <button 
              className="btn btn-primary" 
              disabled={isFull}
              onClick={() => setShowInviteModal(true)}
            >
              + Invite Member
            </button>
          </div>
        </div>

        <div className="member-table-container">
          <table className="member-table">
            <thead>
              <tr>
                <th>Phone Number</th>
                <th>Role</th>
                <th>Joined</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredMembers.length === 0 ? (
                <tr>
                  <td colSpan={5} className="no-data">
                    {searchQuery ? 'No members match your search' : 'No members yet'}
                  </td>
                </tr>
              ) : (
                filteredMembers.map((member) => (
                  <tr key={member.memberId}>
                    <td>{member.phoneNumber}</td>
                    <td>
                      <span className={`badge ${getRoleBadgeClass(member.role)}`}>
                        {member.role}
                      </span>
                    </td>
                    <td>{new Date(member.joinedDate).toLocaleDateString()}</td>
                    <td>
                      <span className={`status-indicator ${member.isActive ? 'active' : 'inactive'}`}>
                        {member.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td>
                      <button className="btn-icon" title="View member details">
                        👁️
                      </button>
                      <button 
                        className="btn-icon" 
                        title="Edit role"
                        onClick={() => {
                          setSelectedMember({ phone: member.phoneNumber, role: member.role });
                          setShowRoleModal(true);
                        }}
                      >
                        ✏️
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {filteredMembers.length > 20 && (
          <div className="pagination-info">
            Showing {filteredMembers.length} of {group.currentMemberCount} members
          </div>
        )}
      </div>

      <div className="constitution-section">
        <h2>Group Constitution</h2>
        {group.constitution && Object.keys(group.constitution).length > 0 ? (
          <div className="constitution-content">
            <pre>{JSON.stringify(group.constitution, null, 2)}</pre>
          </div>
        ) : (
          <p className="no-data">No constitution rules defined yet</p>
        )}
      </div>

      {showInviteModal && (
        <InviteMember
          groupId={groupId}
          groupName={group.name}
          onClose={() => setShowInviteModal(false)}
          onSuccess={() => {
            loadGroup();
            setShowInviteModal(false);
          }}
        />
      )}

      {showRoleModal && selectedMember && (
        <AssignRole
          groupId={groupId}
          groupName={group.name}
          memberPhone={selectedMember.phone}
          currentRole={selectedMember.role}
          onClose={() => {
            setShowRoleModal(false);
            setSelectedMember(null);
          }}
          onSuccess={() => {
            loadGroup();
            setShowRoleModal(false);
            setSelectedMember(null);
          }}
        />
      )}
    </div>
  );
}
