import { useEffect, useState } from 'react';
import { apiService } from '../services/api';
import type { LedgerEntryResponse } from '../services/api';
import './ContributionHistory.css';

interface ContributionHistoryProps {
  groupId: string;
  memberPhone: string;
}

export default function ContributionHistory({ groupId, memberPhone }: ContributionHistoryProps) {
  const [contributions, setContributions] = useState<LedgerEntryResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>('');

  useEffect(() => {
    loadHistory();
  }, [groupId, memberPhone]);

  const loadHistory = async () => {
    try {
      setLoading(true);
      const response = await apiService.getMemberHistory(groupId, memberPhone);
      setContributions(response.data);
      setError('');
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load contribution history');
    } finally {
      setLoading(false);
    }
  };

  const getTotalContributed = () => {
    return contributions.reduce((sum, c) => sum + c.amount, 0);
  };

  const downloadReceipt = (contribution: LedgerEntryResponse) => {
    // Create a simple text receipt
    const receipt = `
DIGITAL STOKVEL BANKING
CONTRIBUTION RECEIPT

Transaction ID: ${contribution.transactionId}
Date: ${new Date(contribution.timestamp).toLocaleString()}

Amount: R${contribution.amount.toLocaleString('en-ZA', { minimumFractionDigits: 2 })}
Payment Method: ${contribution.paymentMethod}
Status: ${contribution.status}

Group: ${contribution.groupName || 'N/A'}
Member: ${memberPhone}

Thank you for your contribution!
---
Digital Stokvel Banking
Support: support@digitalstokvel.co.za
    `.trim();

    // Create blob and download
    const blob = new Blob([receipt], { type: 'text/plain' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `receipt-${contribution.transactionId}.txt`;
    a.click();
    window.URL.revokeObjectURL(url);
  };

  if (loading) {
    return (
      <div className="contribution-history loading">
        <div className="spinner"></div>
        <p>Loading your contribution history...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="contribution-history error">
        <div className="alert alert-error">{error}</div>
        <button onClick={loadHistory} className="btn btn-secondary">
          Try Again
        </button>
      </div>
    );
  }

  return (
    <div className="contribution-history">
      <div className="history-header">
        <h2>My Contribution History</h2>
        <div className="history-stats">
          <div className="stat-card">
            <div className="stat-label">Total Contributions</div>
            <div className="stat-value">{contributions.length}</div>
          </div>
          <div className="stat-card">
            <div className="stat-label">Total Contributed</div>
            <div className="stat-value">
              R{getTotalContributed().toLocaleString('en-ZA', { minimumFractionDigits: 2 })}
            </div>
          </div>
        </div>
      </div>

      {contributions.length === 0 ? (
        <div className="empty-state">
          <div className="empty-icon">💰</div>
          <h3>No contributions yet</h3>
          <p>You haven't made any contributions to this group.</p>
          <p>Make your first contribution to see your history here.</p>
        </div>
      ) : (
        <div className="contributions-list">
          {contributions.map((contribution) => (
            <div key={contribution.ledgerEntryId} className="contribution-card">
              <div className="contribution-header">
                <div className="contribution-info">
                  <div className="contribution-amount">
                    R{contribution.amount.toLocaleString('en-ZA', { minimumFractionDigits: 2 })}
                  </div>
                  <div className="contribution-date">
                    {new Date(contribution.timestamp).toLocaleDateString('en-ZA', {
                      year: 'numeric',
                      month: 'long',
                      day: 'numeric',
                      hour: '2-digit',
                      minute: '2-digit'
                    })}
                  </div>
                </div>
                <div className="contribution-status">
                  <span className={`status-badge status-${contribution.status.toLowerCase()}`}>
                    {contribution.status}
                  </span>
                </div>
              </div>

              <div className="contribution-details">
                <div className="detail-row">
                  <span className="detail-label">Transaction ID:</span>
                  <span className="detail-value">{contribution.transactionId}</span>
                </div>
                <div className="detail-row">
                  <span className="detail-label">Payment Method:</span>
                  <span className="detail-value">{contribution.paymentMethod}</span>
                </div>
                {contribution.description && (
                  <div className="detail-row">
                    <span className="detail-label">Description:</span>
                    <span className="detail-value">{contribution.description}</span>
                  </div>
                )}
              </div>

              <div className="contribution-actions">
                <button
                  className="btn btn-secondary btn-sm"
                  onClick={() => downloadReceipt(contribution)}
                >
                  📄 Download Receipt
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
