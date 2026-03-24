import axios from 'axios';
import type { AxiosInstance } from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api/v1';

class ApiService {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      baseURL: API_BASE_URL,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Add JWT token to requests if available
    this.client.interceptors.request.use((config) => {
      const token = localStorage.getItem('authToken');
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    });

    // Handle response errors globally
    this.client.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          // Token expired or invalid
          localStorage.removeItem('authToken');
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
  }

  // Group endpoints
  async createGroup(data: CreateGroupRequest): Promise<ApiResponse<CreateGroupResponse>> {
    const response = await this.client.post('/groups', data);
    return response.data;
  }

  async getGroup(id: string): Promise<ApiResponse<GroupResponse>> {
    const response = await this.client.get(`/groups/${id}`);
    return response.data;
  }

  async inviteMember(groupId: string, phoneNumber: string): Promise<ApiResponse<unknown>> {
    const response = await this.client.put(`/groups/${groupId}/members`, { phoneNumber });
    return response.data;
  }

  async assignRole(groupId: string, memberId: string, role: string): Promise<ApiResponse<unknown>> {
    const response = await this.client.put(`/groups/${groupId}/roles`, { memberId, role });
    return response.data;
  }

  async getGroupWallet(groupId: string): Promise<ApiResponse<GroupWalletResponse>> {
    const response = await this.client.get(`/groups/${groupId}/wallet`);
    return response.data;
  }

  async getInterestDetails(groupId: string, fromDate?: string, toDate?: string): Promise<ApiResponse<InterestDetailsResponse>> {
    const params = new URLSearchParams();
    if (fromDate) params.append('fromDate', fromDate);
    if (toDate) params.append('toDate', toDate);
    const response = await this.client.get(`/groups/${groupId}/interest-details?${params}`);
    return response.data;
  }

  // Contribution endpoints
  async makeContribution(data: MakeContributionRequest, idempotencyKey: string): Promise<ApiResponse<ContributionResponse>> {
    const response = await this.client.post('/contributions', data, {
      headers: {
        'X-Idempotency-Key': idempotencyKey,
      },
    });
    return response.data;
  }

  async getGroupLedger(groupId: string, page = 1, pageSize = 20): Promise<ApiResponse<LedgerEntryResponse[]>> {
    const response = await this.client.get(`/contributions/group/${groupId}/ledger`, {
      params: { page, pageSize },
    });
    return response.data;
  }

  async getMemberHistory(groupId: string, memberPhone: string): Promise<ApiResponse<LedgerEntryResponse[]>> {
    const response = await this.client.get(`/members/${memberPhone}/contributions`, {
      params: { groupId },
    });
    return response.data;
  }

  // Get user's groups
  async getUserGroups(memberPhone: string): Promise<ApiResponse<GroupResponse[]>> {
    const response = await this.client.get(`/groups/member/${memberPhone}`);
    return response.data;
  }
}

// Types
export interface ApiResponse<T> {
  message?: string;
  data: T;
}

export interface PagedResponse<T> {
  message?: string;
  data: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface CreateGroupRequest {
  name: string;
  description?: string;
  groupType: string;
  contributionAmount: number;
  contributionFrequency: string;
  constitutionRules?: Record<string, unknown>;
}

export interface CreateGroupResponse {
  groupId: string;
  groupName: string;
  role: string;
  groupSavingsAccountNumber: string;
  createdAt: string;
}

export interface GroupResponse {
  id: string;
  name: string;
  description?: string;
  groupType: string;
  contributionAmount: number;
  contributionFrequency: string;
  balance: number;
  groupSavingsAccountNumber?: string;
  maxMembers: number;
  currentMemberCount: number;
  isActive: boolean;
  createdAt: string;
  members?: GroupMemberResponse[];
  constitution?: Record<string, unknown>;
}

export interface GroupMemberResponse {
  memberId: string;
  phoneNumber: string;
  role: string;
  joinedDate: string;
  isActive: boolean;
}

export interface GroupWalletResponse {
  groupId: string;
  groupName: string;
  balance: number;
  accruedInterest: number;
  totalValue: number;
  interestTier: string;
  interestRate: number;
  interestRateDisplay: string;
  lastUpdated: string;
}

export interface InterestDetailsResponse {
  groupId: string;
  groupName: string;
  currentBalance: number;
  interestTier: string;
  annualInterestRate: number;
  yearToDateEarnings: number;
  projectedMonthlyEarnings: number;
  dailyCalculations: DailyCalculationDto[];
  calculationPeriod?: {
    from: string;
    to: string;
  };
}

export interface DailyCalculationDto {
  date: string;
  principalAmount: number;
  interestRate: number;
  accruedAmount: number;
  interestTier: string;
}

export interface MakeContributionRequest {
  groupId: string;
  amount: number;
  paymentMethod: string;
}

export interface ContributionResponse {
  id: string;
  groupId: string;
  groupName: string;
  memberId: string;
  memberPhone: string;
  amount: number;
  currency: string;
  paymentMethod: string;
  status: string;
  timestamp: string;
  paymentReference?: string;
  receipt?: string;
}

export interface LedgerEntryResponse {
  ledgerEntryId: string;
  groupId: string;
  groupName?: string;
  contributionId: string;
  transactionId: string;
  memberPhone: string;
  memberName?: string;
  amount: number;
  currency: string;
  paymentMethod: string;
  status: string;
  timestamp: string;
  description?: string;
  maskedAccountNumber?: string;
}

export const apiService = new ApiService();
