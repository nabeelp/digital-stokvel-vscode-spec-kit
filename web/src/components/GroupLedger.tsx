import { useEffect, useState } from 'react';
import { apiService } from '../services/api';
import type { LedgerEntryResponse } from '../services/api';
import './GroupLedger.css';

interface GroupLedgerProps {
  groupId: string;
  groupName: string;
}

export default function GroupLedger({ groupId, groupName }: GroupLedgerProps) {
  const [ledger, setLedger] = useState<LedgerEntryResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [hasMore, setHasMore] = useState(true);
  const [filterStatus, setFilterStatus] = useState<string>('all');

  useEffect(() => {
    loadLedger();
  }, [groupId, page]);

  const loadLedger = async () => {
    try {
      setLoading(true);
      const response = await apiService.getGroupLedger(groupId, page, pageSize);
      setLedger(response.data);
      setHasMore(response.data.length === pageSize);
      setError('');
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load group ledger');
    } finally {
      setLoading(false);
    }
  };

  const filteredLedger = ledger.filter((entry) => {
    if (filterStatus === 'all') return true;
    return entry.status.toLowerCase() === filterStatus.toLowerCase();
  });

  const getTotalContributions = () => {
    return ledger
      .filter(e => e.status.toLowerCase() === 'completed')
      .reduce((sum, e) => sum + e.amount, 0);
  };

  if (loading && page === 1) {
    return (
      <div className="group-ledger loading">
        <div className="spinner"></div>
        <p>Loading group ledger...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="group-ledger error">
        <div className="alert alert-error">{error}</div>
        <button onClick={loadLedger} className="btn btn-secondary">
          Try Again
        </button>
      </div>
    );
  }

  return (
    <div className="group-ledger">
      <div className="ledger-header">
        <div>
          <h2>{groupName} - Transaction Ledger</h2>
          <p className="ledger-subtitle">Complete history of all group contributions</p>
        </div>
        <div className="ledger-stats">
          <div className="stat-card">
            <div className="stat-label">Total Transactions</div>
            <div className="stat-value">{ledger.length}</div>
          </div>
          <div className="stat-card">
            <div className="stat-label">Total Collected</div>
            <div className="stat-value">
              R{getTotalContributions().toLocaleString('en-ZA', { minimumFractionDigits: 2 })}
            </div>
          </div>
        </div>
      </div>

      <div className="ledger-controls">
        <div className="filter-group">
          <label htmlFor="statusFilter">Filter by Status:</label>
          <select
            id="statusFilter"
            value={filterStatus}
            onChange={(e) => setFilterStatus(e.target.value)}
            className="filter-select"
          >
            <option value="all">All Transactions</option>
            <option value="completed">Completed</option>
            <option value="pending">Pending</option>
            <option value="failed">Failed</option>
          </select>
        </div>

        <div className="info-badge">
          🔒 <strong>POPIA Compliant:</strong> Account numbers are masked for privacy
        </div>
      </div>

      {filteredLedger.length === 0 ? (
        <div className="empty-state">
          <div className="empty-icon">📊</div>
          <h3>No transactions found</h3>
          <p>
            {filterStatus === 'all'
              ? 'No contributions have been recorded yet.'
              : `No ${filterStatus} transactions found.`}
          </p>
        </div>
      ) : (
        <>
          <div className="ledger-table-container">
            <table className="ledger-table">
              <thead>
                <tr>
                  <th>Date & Time</th>
                  <th>Member</th>
                  <th>Amount</th>
                  <th>Payment Method</th>
                  <th>Transaction ID</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {filteredLedger.map((entry) => (
                  <tr key={entry.ledgerEntryId}>
                    <td>
                      {new Date(entry.timestamp).toLocaleDateString('en-ZA', {
                        month: 'short',
                        day: 'numeric',
                        year: 'numeric'
                      })}<br/>
                      <span className="time-text">
                        {new Date(entry.timestamp).toLocaleTimeString('en-ZA', {
                          hour: '2-digit',
                          minute: '2-digit'
                        })}
                      </span>
                    </td>
                    <td>
                      <div className="member-cell">
                        <span className="member-phone">{entry.memberPhone || 'N/A'}</span>
                        {entry.memberName && (
                          <span className="member-name">{entry.memberName}</span>
                        )}
                      </div>
                    </td>
                    <td className="amount-cell">
                      R{entry.amount.toLocaleString('en-ZA', { minimumFractionDigits: 2 })}
                    </td>
                    <td>{entry.paymentMethod}</td>
                    <td>
                      <code className="transaction-id">{entry.transactionId}</code>
                    </td>
                    <td>
                      <span className={`status-badge status-${entry.status.toLowerCase()}`}>
                        {entry.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="ledger-pagination">
            <button
              className="btn btn-secondary"
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={page === 1 || loading}
            >
              ← Previous
            </button>
            <span className="page-info">
              Page {page} {hasMore && '(more available)'}
            </span>
            <button
              className="btn btn-secondary"
              onClick={() => setPage(p => p + 1)}
              disabled={!hasMore || loading}
            >
              Next →
            </button>
          </div>
        </>
      )}
    </div>
  );
}
