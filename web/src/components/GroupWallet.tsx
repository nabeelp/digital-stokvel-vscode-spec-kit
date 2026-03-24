import { useEffect, useState } from 'react';
import { apiService } from '../services/api';
import type { GroupWalletResponse, InterestDetailsResponse } from '../services/api';
import './GroupWallet.css';

interface GroupWalletProps {
  groupId: string;
}

export default function GroupWallet({ groupId }: GroupWalletProps) {
  const [wallet, setWallet] = useState<GroupWalletResponse | null>(null);
  const [interestDetails, setInterestDetails] = useState<InterestDetailsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [showDetails, setShowDetails] = useState(false);

  useEffect(() => {
    loadWallet();
    // Refresh wallet every 30 seconds for real-time updates
    const interval = setInterval(loadWallet, 30000);
    return () => clearInterval(interval);
  }, [groupId]);

  const loadWallet = async () => {
    try {
      setLoading(true);
      const response = await apiService.getGroupWallet(groupId);
      setWallet(response.data);
      setError('');
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load wallet');
    } finally {
      setLoading(false);
    }
  };

  const loadInterestDetails = async () => {
    if (interestDetails) {
      setShowDetails(!showDetails);
      return;
    }

    try {
      const response = await apiService.getInterestDetails(groupId);
      setInterestDetails(response.data);
      setShowDetails(true);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to load interest details');
    }
  };

  const getTierColor = (tier: string) => {
    if (tier.includes('Tier3')) return '#10b981';
    if (tier.includes('Tier2')) return '#3b82f6';
    return '#6366f1';
  };

  const getTierInfo = (tier: string) => {
    if (tier.includes('Tier3')) return { name: 'Tier 3', rate: '5.5%', range: 'R50,000+' };
    if (tier.includes('Tier2')) return { name: 'Tier 2', rate: '4.5%', range: 'R10,000 - R50,000' };
    return { name: 'Tier 1', rate: '3.5%', range: 'R0 - R10,000' };
  };

  if (loading && !wallet) {
    return (
      <div className="group-wallet loading">
        <div className="spinner"></div>
        <p>Loading wallet...</p>
      </div>
    );
  }

  if (error && !wallet) {
    return (
      <div className="group-wallet error">
        <div className="alert alert-error">{error}</div>
        <button onClick={loadWallet} className="btn btn-secondary">
          Retry
        </button>
      </div>
    );
  }

  if (!wallet) {
    return <div className="group-wallet">Wallet not found</div>;
  }

  const tierInfo = getTierInfo(wallet.interestTier);
  const tierColor = getTierColor(wallet.interestTier);

  return (
    <div className="group-wallet">
      <div className="wallet-header">
        <div className="wallet-title">
          <h2>{wallet.groupName}</h2>
          <p>Your Group Savings Wallet</p>
        </div>
        <div className="bank-badges">
          <div className="badge-fsca">🛡️ FSCA Protected</div>
          <div className="badge-bank">🏦 Bank Partner</div>
        </div>
      </div>

      <div className="wallet-balance-card">
        <div className="balance-main">
          <div className="balance-label">Total Value</div>
          <div className="balance-amount">
            R{wallet.totalValue.toLocaleString('en-ZA', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
          </div>
          <div className="balance-breakdown">
            <span>Principal: R{wallet.balance.toLocaleString('en-ZA', { minimumFractionDigits: 2 })}</span>
            <span>+</span>
            <span>Interest: R{wallet.accruedInterest.toLocaleString('en-ZA', { minimumFractionDigits: 2 })}</span>
          </div>
        </div>

        <div className="interest-tier-badge" style={{ borderColor: tierColor }}>
          <div className="tier-indicator" style={{ backgroundColor: tierColor }}></div>
          <div className="tier-info">
            <div className="tier-name">{tierInfo.name}</div>
            <div className="tier-rate">{wallet.interestRateDisplay}</div>
            <div className="tier-range">{tierInfo.range}</div>
          </div>
        </div>
      </div>

      <div className="wallet-actions">
        <button className="btn btn-primary" onClick={loadInterestDetails}>
          {showDetails ? '📊 Hide Details' : '📊 View Interest Details'}
        </button>
        <button className="btn btn-secondary">
          💰 Make Contribution
        </button>
      </div>

      {showDetails && interestDetails && (
        <div className="interest-details-panel">
          <h3>Interest Calculation Details</h3>

          <div className="details-grid">
            <div className="detail-card">
              <div className="detail-label">Year-to-Date Earnings</div>
              <div className="detail-value">
                R{interestDetails.yearToDateEarnings.toLocaleString('en-ZA', { minimumFractionDigits: 2 })}
              </div>
            </div>

            <div className="detail-card">
              <div className="detail-label">Projected Monthly</div>
              <div className="detail-value">
                R{interestDetails.projectedMonthlyEarnings.toLocaleString('en-ZA', { minimumFractionDigits: 2 })}
              </div>
            </div>

            <div className="detail-card">
              <div className="detail-label">Annual Rate</div>
              <div className="detail-value">
                {interestDetails.annualInterestRate.toFixed(2)}%
              </div>
            </div>

            <div className="detail-card">
              <div className="detail-label">Current Balance</div>
              <div className="detail-value">
                R{interestDetails.currentBalance.toLocaleString('en-ZA', { minimumFractionDigits: 2 })}
              </div>
            </div>
          </div>

          {interestDetails.dailyCalculations && interestDetails.dailyCalculations.length > 0 && (
            <div className="daily-calculations">
              <h4>Recent Daily Calculations</h4>
              <div className="calculations-table">
                <table>
                  <thead>
                    <tr>
                      <th>Date</th>
                      <th>Principal</th>
                      <th>Rate</th>
                      <th>Accrued</th>
                    </tr>
                  </thead>
                  <tbody>
                    {interestDetails.dailyCalculations.slice(0, 10).map((calc, index) => (
                      <tr key={index}>
                        <td>{new Date(calc.date).toLocaleDateString()}</td>
                        <td>R{calc.principalAmount.toLocaleString('en-ZA', { minimumFractionDigits: 2 })}</td>
                        <td>{calc.interestRate.toFixed(2)}%</td>
                        <td className="accrued-amount">+R{calc.accruedAmount.toLocaleString('en-ZA', { minimumFractionDigits: 4 })}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>
      )}

      <div className="wallet-footer">
        <div className="disclosure">
          ℹ️ <strong>Your money is protected</strong> - All deposits are covered by bank deposit insurance
        </div>
        <div className="last-updated">
          Last updated: {new Date(wallet.lastUpdated).toLocaleString()}
        </div>
      </div>
    </div>
  );
}
