# Digital Stokvel Web Frontend

## Overview
React 18 + TypeScript web application with Vite bundler for the Digital Stokvel Banking MVP.

## Tech Stack
- **Framework**: React 18.3.1
- **Language**: TypeScript 5.7+
- **Build Tool**: Vite 8.0.2
- **Routing**: React Router DOM 7+
- **Form Management**: Formik
- **HTTP Client**: Axios
- **Node.js**: v22.19.0

## Quick Start

### Install Dependencies
```bash
npm install
```

### Start Dev Server
```bash
npm run dev
# Runs on http://localhost:5173/
```

### Build Production
```bash
npm run build
# Output: dist/
```

## Components Implemented

### ✅ MyGroups (Landing Page)
Grid view of all groups user belongs to with search, status badges, and quick actions.

### ✅ GroupCreation (T047)
Form for creating new stokvel groups with validation (R50-R100K contribution limits).

### ✅ GroupDashboard (T050)
Group details with member roster, role badges, search functionality, and constitution display. Integrated with InviteMember and AssignRole modals.

### ✅ PayContribution (T070)
Payment modal with idempotency keys, receipt preview, and payment method selection.

### ✅ GroupWallet (T088)
Wallet display with tiered interest calculations (3.5%/4.5%/5.5%), real-time updates, and FSCA badges.

### ✅ InviteMember Modal
Modal form to invite new members by phone number with validation and SMS notification flow.

### ✅ AssignRole Modal
Modal to change member roles (Chairperson, Treasurer, Secretary, Member) with descriptive information and warnings.

## API Integration

All backend endpoints integrated via centralized `ApiService` class in `src/services/api.ts`:
- JWT authentication with auto-inject interceptor
- 401 auto-redirect to login
- 11 TypeScript interfaces for type safety
- Environment-configurable base URL

## Environment Configuration

Create `.env` file:
```bash
VITE_API_BASE_URL=http://localhost:5000/api/v1
```

## Routing

- `/` or `/groups` - My Groups landing page (grid view of all groups)
- `/login` - Authentication (mock implementation)
- `/groups/create` - Create new group
- `/groups/:id` - Group dashboard
- `/groups/:id/wallet` - Wallet & interest details

## Testing

Requires backend API running on `http://localhost:5000`:
1. View My Groups page (empty state shown if no groups)
3. Create group with contribution amount between R50-R100K
4. View dashboard and wallet
5. Invite members via modal form
6. Assign roles to members via modal form

## Known Limitations

- **Auth**: Mock JWT tokens (Auth0 integration pending)
- **Mobile apps**: iOS/Android not yet implemented
- **API Endpoint**: MyGroups page uses mock data (GET /api/v1/groups/my-groups endpoint not yet implemented
- **Additional UIs**: Invite member, assign role, ledger view (pending)

## Contributing

- TypeScript strict mode enabled
- Use `import type` for type-only imports
- One CSS file per component
- PascalCase for components, camelCase for functions
75% - landing page + modals added
---

**Status**: Phase 3-5 Web UI complete (50%)
**Last Updated**: 2025-05-15

