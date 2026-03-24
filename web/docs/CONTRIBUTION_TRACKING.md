# Web Frontend - Contribution Tracking Components

## Overview

This document describes the new components added to track and display contribution history and group financial transactions.

## Components

### 1. ContributionHistory (`/groups/:id/history`)

**Purpose**: Display personal contribution history for a logged-in member within a specific group.

**Features**:
- Lists all contributions made by the current user
- Shows transaction details: date, amount, payment method, status, transaction ID
- Provides downloadable text receipts for each contribution
- Displays summary statistics: total contributions count and total amount
- Empty state messaging when no contributions exist
- Responsive card-based layout for mobile devices

**Backend API**: `GET /api/v1/members/{memberPhone}/contributions?groupId={groupId}`

**Props**:
- `groupId` (string): The ID of the group to filter contributions
- `memberPhone` (string): The phone number of the logged-in member

**Usage**:
```tsx
<ContributionHistory 
  groupId="abc123" 
  memberPhone="27812345678" 
/>
```

**Key UI Elements**:
- **Stats Cards**: Total contributions count and amount
- **Contribution Cards**: Individual contribution details with status badges
- **Download Receipt Button**: Generates a simple text receipt file
- **Loading/Error States**: Proper feedback for async operations

**Status Badge Colors**:
- **Completed**: Green background (`#d1fae5`)
- **Pending**: Yellow background (`#fef3c7`)
- **Failed**: Red background (`#fee2e2`)

---

### 2. GroupLedger (`/groups/:id/ledger`)

**Purpose**: Display complete transaction history for a group (accessible to Chairperson and Treasurer).

**Features**:
- Lists all contributions made to the group by all members
- Shows member details, transaction dates, amounts, payment methods
- Provides filtering by transaction status (All, Completed, Pending, Failed)
- Displays summary statistics: total transactions and total collected
- Pagination support (20 transactions per page)
- POPIA compliance: Account numbers are masked for privacy
- Responsive table with horizontal scroll on mobile

**Backend API**: `GET /api/v1/contributions/group/{groupId}/ledger?page={page}&pageSize={pageSize}`

**Props**:
- `groupId` (string): The ID of the group
- `groupName` (string): The name of the group (for display)

**Usage**:
```tsx
<GroupLedger 
  groupId="abc123" 
  groupName="Ubuntu Savings Circle" 
/>
```

**Key UI Elements**:
- **Stats Cards**: Total transactions count and total collected amount
- **Filter Dropdown**: Filter by transaction status
- **POPIA Badge**: Privacy compliance indicator
- **Data Table**: Sortable columns with member info, amounts, status
- **Pagination Controls**: Previous/Next buttons with page indicator

**Access Control**:
- Currently no frontend enforcement (relies on backend authorization)
- Intended for Chairperson and Treasurer roles only
- Future enhancement: Add role-based UI restrictions

---

## Routing Changes

### New Routes Added to `App.tsx`:

```tsx
<Route path="/groups/:id/history" element={<ContributionHistoryWrapper />} />
<Route path="/groups/:id/ledger" element={<GroupLedgerWrapper />} />
```

### Route Wrappers:

**ContributionHistoryWrapper**:
- Extracts `groupId` from URL path
- Retrieves `memberPhone` from localStorage
- Passes both as props to ContributionHistory component

**GroupLedgerWrapper**:
- Extracts `groupId` from URL path
- Passes groupId and placeholder groupName to GroupLedger component
- **Note**: GroupName should ideally be fetched from API in future enhancement

---

## Integration with GroupDashboard

### New Navigation Buttons

Added two buttons to the GroupDashboard roster section:

```tsx
<button className="btn btn-secondary" 
  onClick={() => window.location.href = `/groups/${groupId}/history`}>
  📋 My Contributions
</button>

<button className="btn btn-secondary" 
  onClick={() => window.location.href = `/groups/${groupId}/ledger`}>
  📊 Group Ledger
</button>
```

**Button Placement**: Located in the roster-actions section, between the search input and "Invite Member" button.

**User Flow**:
1. User navigates to group dashboard
2. User clicks "My Contributions" to view personal payment history
3. User clicks "Group Ledger" to view all group transactions (role-dependent access)

---

## API Service Updates

### New Methods in `api.ts`:

#### getMemberHistory
```typescript
async getMemberHistory(groupId: string, memberPhone: string): Promise<ApiResponse<LedgerEntryResponse[]>>
```
- **Endpoint**: `GET /api/v1/members/{memberPhone}/contributions?groupId={groupId}`
- **Purpose**: Fetch contribution history for a specific member in a group
- **Returns**: Array of LedgerEntryResponse objects

#### getGroupLedger
```typescript
async getGroupLedger(groupId: string, page = 1, pageSize = 20): Promise<ApiResponse<LedgerEntryResponse[]>>
```
- **Endpoint**: `GET /api/v1/contributions/group/{groupId}/ledger?page={page}&pageSize={pageSize}`
- **Purpose**: Fetch paginated transaction ledger for a group
- **Returns**: Array of LedgerEntryResponse objects

#### getUserGroups
```typescript
async getUserGroups(memberPhone: string): Promise<ApiResponse<GroupResponse[]>>
```
- **Endpoint**: `GET /api/v1/groups/member/{memberPhone}`
- **Purpose**: Fetch all groups a member belongs to
- **Returns**: Array of GroupResponse objects

### Updated Interface: LedgerEntryResponse

```typescript
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
```

**Key Fields**:
- `ledgerEntryId`: Unique identifier for the ledger entry
- `transactionId`: External payment reference
- `status`: One of "Completed", "Pending", "Failed"
- `maskedAccountNumber`: POPIA-compliant masked account number (e.g., "****1234")

---

## Authentication Updates

### Login Component Changes

Updated `Login.tsx` to store user's phone number in localStorage:

```typescript
localStorage.setItem('userPhone', phoneNumber);
```

**Purpose**: Enables ContributionHistory component to retrieve current user's phone number without prop drilling.

**Storage Key**: `userPhone`

**Flow**:
1. User enters phone number in login form
2. On successful authentication, phone number is stored in localStorage
3. Components retrieve phone number using `localStorage.getItem('userPhone')`

---

## Styling Conventions

### Shared CSS Patterns

All components follow consistent styling:

**Colors**:
- Primary text: `#1a202c` and `#2d3748`
- Secondary text: `#718096`
- Success: `#10b981` (green)
- Warning: `#f59e0b` (amber)
- Error: `#ef4444` (red)

**Component Structure**:
- White backgrounds with `border-radius: 0.75rem`
- Box shadows: `0 1px 3px rgba(0, 0, 0, 0.1)`
- Hover effects: `translateY(-2px)` with increased shadow

**Responsive Breakpoints**:
- Desktop: 1024px and above
- Tablet: 768px - 1023px
- Mobile: Below 768px

---

## Future Enhancements

### Planned Improvements:

1. **Export to CSV**: Add export functionality to GroupLedger
   - Button: "📥 Export to CSV"
   - Client-side CSV generation using papaparse library
   - Filename format: `{GroupName}_Ledger_{Date}.csv`

2. **Receipt Modal**: Create dedicated ReceiptView component
   - Printable receipt design with Digital Stokvel branding
   - Print button using `window.print()`
   - Download as PDF option (html-to-pdf library)
   - Share button (native share API)

3. **Advanced Filtering**: Add date range picker to both components
   - Libraries: react-datepicker or date-fns
   - Filter by: last 7 days, last 30 days, custom range
   - Apply filters to API requests

4. **Real-time Updates**: Add WebSocket support for live transaction updates
   - Display toast notification when new contribution is received
   - Auto-refresh ledger when new data available
   - Use React Query for optimistic updates

5. **Role-Based UI**: Enforce access control in frontend
   - Hide "Group Ledger" button for regular members
   - Show permission denied message for unauthorized access
   - Use JWT claims to determine user role

6. **Search and Sort**: Enhanced table functionality
   - Search by member phone number
   - Sort by date, amount, status
   - Multi-column sorting

7. **Charts and Analytics**: Visual representation of contributions
   - Line chart: Contributions over time
   - Pie chart: Contribution distribution by member
   - Bar chart: Monthly contribution totals
   - Libraries: Chart.js or Recharts

---

## Testing Recommendations

### Manual Testing Checklist:

- [ ] ContributionHistory displays empty state when no contributions exist
- [ ] ContributionHistory shows all personal contributions correctly
- [ ] Receipt download generates valid text file
- [ ] Status badges display correct colors for each status type
- [ ] GroupLedger displays all group transactions
- [ ] Filtering by status works correctly
- [ ] Pagination controls navigate between pages correctly
- [ ] Both components handle loading states properly
- [ ] Error messages display when API requests fail
- [ ] Mobile responsive layout works on small screens
- [ ] Navigation buttons in GroupDashboard work correctly
- [ ] Login stores phone number in localStorage
- [ ] Components retrieve phone number from localStorage

### Unit Testing (Future):

```typescript
// Example test structure
describe('ContributionHistory', () => {
  it('should render empty state when no contributions', () => {});
  it('should display contribution cards with correct data', () => {});
  it('should download receipt when button clicked', () => {});
  it('should handle API errors gracefully', () => {});
});

describe('GroupLedger', () => {
  it('should render transaction table with all entries', () => {});
  it('should filter transactions by status', () => {});
  it('should paginate correctly', () => {});
  it('should display POPIA compliance badge', () => {});
});
```

---

## Backend Dependencies

### Required Backend Endpoints:

These endpoints must be implemented in the backend API for the components to function:

1. **GET /api/v1/members/{memberPhone}/contributions**
   - **Implemented in**: `T066` (Phase 4: User Story 2)
   - **Controller**: `ContributionsController.GetMemberContributions()`
   - **Query Parameters**: 
     - `groupId` (optional): Filter by specific group
   - **Response**: Array of contribution objects

2. **GET /api/v1/contributions/group/{groupId}/ledger**
   - **Implemented in**: `T065` (Phase 4: User Story 2)
   - **Controller**: `ContributionsController.GetGroupLedger()`
   - **Query Parameters**:
     - `page` (default: 1)
     - `pageSize` (default: 20)
   - **Response**: Paginated array of ledger entries

3. **GET /api/v1/groups/member/{memberPhone}**
   - **Purpose**: Fetch all groups a member belongs to
   - **Status**: May need implementation
   - **Response**: Array of group objects

---

## POPIA Compliance

### Privacy Considerations:

1. **Account Number Masking**:
   - Display format: `****1234` (last 4 digits visible)
   - Backend should mask before sending to frontend
   - Never log or store unmasked account numbers in frontend

2. **Phone Number Display**:
   - Currently displayed in full (e.g., `27812345678`)
   - Consider masking for non-admin users: `2781***5678`
   - Configurable based on user role and settings

3. **Data Retention**:
   - Frontend only displays data, does not store long-term
   - Temporary caching in state only
   - localStorage only stores current user's phone for session

4. **Audit Trail**:
   - All ledger views should be logged in backend
   - Track who accessed transaction details and when
   - Particularly important for Treasurer access

---

## Performance Considerations

### Optimization Strategies:

1. **Pagination**: Both components support pagination to limit data fetching
   - Default page size: 20 transactions
   - Prevents loading thousands of records at once

2. **Virtual Scrolling** (Future):
   - For very large transaction lists
   - Libraries: react-window or react-virtualized
   - Renders only visible rows

3. **Memoization**:
   - Use `useMemo` for filtered/sorted data
   - Prevent unnecessary re-renders
   - Example: `const filteredLedger = useMemo(() => ledger.filter(...), [ledger, filterStatus])`

4. **Debounced Search** (Future):
   - Delay API calls while user types
   - Use lodash.debounce or custom hook
   - Wait 300-500ms after last keystroke

5. **Caching**:
   - Use React Query for server state management
   - Cache ledger data with 5-minute stale time
   - Invalidate cache on new contributions

---

## Security Notes

### Authorization:

- **Frontend**: Currently no role-based UI restrictions
- **Backend**: Must enforce authorization at API level
- **Best Practice**: Frontend should respect backend authorization errors

### Token Management:

- JWT token stored in localStorage (key: `token`)
- Auto-injected in all API requests via axios interceptor
- Automatic redirect to login on 401 Unauthorized

### Input Validation:

- Phone numbers validated in login: `27XXXXXXXXX` format (11 digits)
- No user input in ContributionHistory/GroupLedger (read-only display)
- Future filtering inputs should validate date ranges

---

## Conclusion

The ContributionHistory and GroupLedger components complete the web frontend for User Story 2 (Contributions). Members can now:
- View their personal payment history
- Download receipts for their contributions
- Access group financial transparency (role-dependent)
- Track group transaction details with POPIA compliance

These components integrate seamlessly with the existing GroupDashboard and leverage already-implemented backend APIs from Phase 4.

**Next Steps**:
- Add visual charts for contribution analytics
- Implement CSV export functionality
- Create printable receipt modal
- Add real-time updates via WebSocket
- Enhance filtering with date range picker
- Add comprehensive unit and integration tests
