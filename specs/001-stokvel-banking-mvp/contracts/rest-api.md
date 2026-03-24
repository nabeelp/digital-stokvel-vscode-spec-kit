# REST API Contract: Digital Stokvel Banking MVP

**Version**: 1.0.0  
**Base URL**: `https://api.stokvel.bank/v1`  
**Authentication**: Bearer JWT token (ASP.NET Core Identity)  
**Date**: 2026-03-24

---

## Table of Contents

1. [Authentication](#authentication)
2. [Groups API](#groups-api)
3. [Contributions API](#contributions-api)
4. [Payouts API](#payouts-api)
5. [Governance API](#governance-api)
6. [Members API](#members-api)
7. [Error Handling](#error-handling)
8. [Rate Limiting](#rate-limiting)

---

## Authentication

All endpoints require authentication via JWT Bearer token obtained from login.

### `POST /auth/login`

Authenticate user and obtain JWT token.

**Request**:
```json
{
  "bankCustomerId": "BC001234",
  "pin": "1234"
}
```

**Response** (200 OK):
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "rt_a1b2c3d4e5f6",
  "expiresIn": 3600,
  "member": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "bankCustomerId": "BC001234",
    "phoneNumber": "+27821234567",
    "preferredLanguage": "zu",
    "ficaVerified": true
  }
}
```

**Error Responses**:
- `401 Unauthorized`: Invalid credentials
- `403 Forbidden`: Account locked due to multiple failed attempts

---

## Groups API

### `POST /groups`

Create a new stokvel group.

**Authorization**: Authenticated member  
**Request**:
```json
{
  "name": "Ntombizodwa Stokvel",
  "description": "Community savings group for education funding",
  "groupType": "RotatingPayout",
  "contributionAmount": 500.00,
  "contributionFrequency": "Monthly",
  "payoutSchedule": {
    "type": "rotating",
    "cycleDays": 30,
    "startDate": "2026-04-01T00:00:00Z"
  },
  "constitution": {
    "missedPaymentPenalty": 50.00,
    "gracePeriodDays": 7,
    "quorumThreshold": 0.60,
    "memberRemovalCriteria": "3_consecutive_misses"
  }
}
```

**Response** (201 Created):
```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "name": "Ntombizodwa Stokvel",
  "groupType": "RotatingPayout",
  "accountBalance": 0.00,
  "interestTier": "Tier1",
  "accruedInterest": 0.00,
  "memberCount": 1,
  "createdAt": "2026-03-24T10:30:00Z",
  "inviteCode": "STK-NTM-2026"
}
```

**Error Responses**:
- `400 Bad Request`: Invalid request body (validation errors)
- `401 Unauthorized`: Member not authenticated
- `403 Forbidden`: Member FICA not verified

---

### `GET /groups`

List all groups the authenticated member belongs to.

**Authorization**: Authenticated member  
**Query Parameters**:
- `page` (int, default: 1): Page number
- `pageSize` (int, default: 10): Items per page
- `status` (enum: Active, Suspended, Closed): Filter by group status

**Response** (200 OK):
```json
{
  "groups": [
    {
      "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "name": "Ntombizodwa Stokvel",
      "groupType": "RotatingPayout",
      "accountBalance": 5014.38,
      "interestTier": "Tier1",
      "accruedInterest": 14.38,
      "memberCount": 10,
      "myRole": "Chairperson",
      "nextContributionDue": "2026-04-01T00:00:00Z",
      "contributionAmount": 500.00
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "totalPages": 1,
    "totalItems": 1
  }
}
```

---

### `GET /groups/{groupId}`

Get detailed information about a specific group.

**Authorization**: Authenticated member (must be member of the group)  
**Path Parameters**:
- `groupId` (UUID): Group identifier

**Response** (200 OK):
```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "name": "Ntombizodwa Stokvel",
  "description": "Community savings group for education funding",
  "groupType": "RotatingPayout",
  "contributionAmount": 500.00,
  "contributionFrequency": "Monthly",
  "accountBalance": 5014.38,
  "interestTier": "Tier1",
  "currentInterestRate": 0.035,
  "accruedInterest": 14.38,
  "payoutSchedule": {
    "type": "rotating",
    "cycleDays": 30,
    "currentRecipientOrder": 2,
    "nextPayoutDate": "2026-04-24T00:00:00Z"
  },
  "constitution": {
    "missedPaymentPenalty": 50.00,
    "gracePeriodDays": 7,
    "quorumThreshold": 0.60
  },
  "members": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Ntombizodwa M.",
      "role": "Chairperson",
      "joinedAt": "2026-03-24T10:30:00Z",
      "contributionStatus": "Current",
      "totalContributions": 500.00
    }
  ],
  "createdAt": "2026-03-24T10:30:00Z"
}
```

**Error Responses**:
- `404 Not Found`: Group does not exist or member not authorized to view
- `401 Unauthorized`: Member not authenticated

---

### `POST /groups/{groupId}/invite`

Invite members to join a group.

**Authorization**: Chairperson of the group  
**Path Parameters**:
- `groupId` (UUID): Group identifier

**Request**:
```json
{
  "phoneNumbers": [
    "+27821234568",
    "+27821234569"
  ],
  "message": "Join our education savings stokvel! 🎓"
}
```

**Response** (200 OK):
```json
{
  "invitationsSent": 2,
  "inviteLink": "https://stokvel.bank/join/STK-NTM-2026",
  "recipients": [
    {
      "phoneNumber": "+27821234568",
      "status": "Sent",
      "isExistingMember": true
    },
    {
      "phoneNumber": "+27821234569",
      "status": "Sent",
      "isExistingMember": false,
      "requiresOnboarding": true
    }
  ]
}
```

**Error Responses**:
- `403 Forbidden`: Member does not have Chairperson role
- `404 Not Found`: Group not found

---

### `POST /groups/{groupId}/join`

Join a group via invite code.

**Authorization**: Authenticated member  
**Path Parameters**:
- `groupId` (UUID): Group identifier

**Request**:
```json
{
  "inviteCode": "STK-NTM-2026"
}
```

**Response** (200 OK):
```json
{
  "memberId": "550e8400-e29b-41d4-a716-446655440001",
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "role": "Member",
  "joinedAt": "2026-03-24T11:00:00Z",
  "nextContributionDue": "2026-04-01T00:00:00Z"
}
```

**Error Responses**:
- `400 Bad Request`: Invalid invite code
- `409 Conflict`: Member already in group

---

### `GET /groups/{groupId}/ledger`

View group contribution ledger with POPIA-compliant data minimization.

**Authorization**: Member of the group  
**Path Parameters**:
- `groupId` (UUID): Group identifier

**Query Parameters**:
- `from` (ISO 8601 date): Start date filter
- `to` (ISO 8601 date): End date filter

**Response** (200 OK):
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "ledger": [
    {
      "contributionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "memberName": "Ntombizodwa M.",
      "amount": 500.00,
      "status": "Completed",
      "timestamp": "2026-03-24T12:00:00Z",
      "maskedAccountNumber": "****1234"
    },
    {
      "contributionId": "b2c3d4e5-f6g7-8901-bcde-f12345678901",
      "memberName": "Thandi N.",
      "amount": 500.00,
      "status": "Pending",
      "timestamp": "2026-03-24T13:00:00Z",
      "maskedAccountNumber": "****5678"
    }
  ],
  "summary": {
    "totalContributions": 10000.00,
    "completedCount": 18,
    "pendingCount": 2,
    "failedCount": 0
  }
}
```

**Note**: Full account numbers are NOT exposed per FR-046 (POPIA data minimization).

---

## Contributions API

### `POST /contributions`

Make a contribution to a group.

**Authorization**: Member of the group  
**Request**:
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "amount": 500.00,
  "paymentMethod": "OneTap",
  "idempotencyKey": "idem_550e8400e29b41d4a716446655440000"
}
```

**Response** (201 Created):
```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "memberId": "550e8400-e29b-41d4-a716-446655440000",
  "amount": 500.00,
  "status": "Completed",
  "transactionTimestamp": "2026-03-24T12:00:00Z",
  "confirmationCode": "STK-2026-03-24-001234",
  "receiptUrl": "https://stokvel.bank/receipts/a1b2c3d4.pdf"
}
```

**Error Responses**:
- `400 Bad Request`: Invalid amount (does not match group's contribution amount)
- `402 Payment Required`: Insufficient funds in member's bank account
- `409 Conflict`: Duplicate idempotency key (transaction already processed)
- `404 Not Found`: Group not found or member not in group

---

### `GET /contributions`

List contributions made by the authenticated member.

**Authorization**: Authenticated member  
**Query Parameters**:
- `groupId` (UUID, optional): Filter by group
- `status` (enum: Pending, Completed, Failed, optional): Filter by status
- `from` (ISO 8601 date): Start date filter
- `to` (ISO 8601 date): End date filter

**Response** (200 OK):
```json
{
  "contributions": [
    {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "groupName": "Ntombizodwa Stokvel",
      "amount": 500.00,
      "status": "Completed",
      "timestamp": "2026-03-24T12:00:00Z",
      "confirmationCode": "STK-2026-03-24-001234"
    }
  ],
  "summary": {
    "totalContributions": 5000.00,
    "completedCount": 10,
    "pendingCount": 0
  }
}
```

---

### `POST /contributions/debit-order`

Set up recurring debit order for automatic contributions.

**Authorization**: Member of the group  
**Request**:
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "dayOfMonth": 1,
  "accountNumber": "1234567890",
  "confirmationConsent": true
}
```

**Response** (201 Created):
```json
{
  "debitOrderId": "do_a1b2c3d4e5f6",
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "amount": 500.00,
  "frequency": "Monthly",
  "nextDebitDate": "2026-04-01T00:00:00Z",
  "status": "Active"
}
```

---

## Payouts API

### `POST /payouts`

Initiate a payout (Chairperson only).

**Authorization**: Chairperson of the group  
**Request**:
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "payoutType": "Rotating",
  "recipientMemberId": "550e8400-e29b-41d4-a716-446655440002"
}
```

**Response** (201 Created):
```json
{
  "id": "p1a2b3c4-d5e6-7890-abcd-ef1234567890",
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "payoutType": "Rotating",
  "totalAmount": 5000.00,
  "status": "PendingConfirmation",
  "initiatedBy": "Ntombizodwa M.",
  "pendingConfirmationFrom": "Thandi N. (Treasurer)",
  "createdAt": "2026-03-24T14:00:00Z"
}
```

**Error Responses**:
- `403 Forbidden`: Member does not have Chairperson role
- `400 Bad Request`: Insufficient group balance for payout
- `422 Unprocessable Entity`: Payout violates group constitution rules

---

### `POST /payouts/{payoutId}/confirm`

Confirm a payout (Treasurer only).

**Authorization**: Treasurer of the group  
**Path Parameters**:
- `payoutId` (UUID): Payout identifier

**Request**:
```json
{
  "confirmed": true,
  "notes": "Verified recipient and amount"
}
```

**Response** (200 OK):
```json
{
  "id": "p1a2b3c4-d5e6-7890-abcd-ef1234567890",
  "status": "Executed",
  "confirmedBy": "Thandi N.",
  "executionTimestamp": "2026-03-24T14:05:00Z",
  "eftReference": "EFT20260324140500",
  "notification": "All group members notified of successful payout"
}
```

**Error Responses**:
- `403 Forbidden`: Member does not have Treasurer role
- `404 Not Found`: Payout not found
- `409 Conflict`: Payout already confirmed or rejected

---

### `GET /payouts`

List payouts for a group.

**Authorization**: Member of the group  
**Query Parameters**:
- `groupId` (UUID, required): Filter by group
- `status` (enum: PendingConfirmation, Confirmed, Executed, Rejected, optional): Filter by status

**Response** (200 OK):
```json
{
  "payouts": [
    {
      "id": "p1a2b3c4-d5e6-7890-abcd-ef1234567890",
      "payoutType": "Rotating",
      "totalAmount": 5000.00,
      "status": "Executed",
      "recipientName": "Sipho K.",
      "executionTimestamp": "2026-03-24T14:05:00Z"
    }
  ]
}
```

---

## Governance API

### `POST /governance/votes`

Create a voting proposal for group decision.

**Authorization**: Chairperson of the group  
**Request**:
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "proposalType": "ChangeContributionAmount",
  "title": "Increase monthly contribution to R750",
  "description": "Proposal to increase contribution from R500 to R750 to reach savings goals faster",
  "newValue": 750.00,
  "votingDurationHours": 72
}
```

**Response** (201 Created):
```json
{
  "voteId": "v1a2b3c4-d5e6-7890-abcd-ef1234567890",
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "proposalType": "ChangeContributionAmount",
  "status": "Open",
  "votesFor": 0,
  "votesAgainst": 0,
  "quorumRequired": 6,
  "expiresAt": "2026-03-27T14:00:00Z"
}
```

---

### `POST /governance/votes/{voteId}/cast`

Cast a vote on a proposal.

**Authorization**: Member of the group  
**Path Parameters**:
- `voteId` (UUID): Vote identifier

**Request**:
```json
{
  "vote": "For"
}
```

**Response** (200 OK):
```json
{
  "voteId": "v1a2b3c4-d5e6-7890-abcd-ef1234567890",
  "memberVote": "For",
  "currentTally": {
    "votesFor": 7,
    "votesAgainst": 2,
    "quorumReached": true,
    "status": "Passed"
  }
}
```

---

### `POST /governance/disputes`

Raise a dispute.

**Authorization**: Member of the group  
**Request**:
```json
{
  "groupId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "disputeType": "MissedPaymentClaim",
  "description": "My payment was marked as missed but I have proof of payment on 2026-03-20"
}
```

**Response** (201 Created):
```json
{
  "disputeId": "d1a2b3c4-d5e6-7890-abcd-ef1234567890",
  "status": "Open",
  "createdAt": "2026-03-24T15:00:00Z",
  "notification": "Dispute raised. Chairperson has been notified."
}
```

---

## Members API

### `GET /members/me`

Get authenticated member's profile.

**Authorization**: Authenticated member  
**Response** (200 OK):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "bankCustomerId": "BC001234",
  "phoneNumber": "+27821234567",
  "preferredLanguage": "zu",
  "ficaVerified": true,
  "ficaVerificationDate": "2024-01-15T00:00:00Z",
  "groupsCount": 3,
  "totalContributions": 15000.00,
  "createdAt": "2024-01-10T00:00:00Z"
}
```

---

### `PATCH /members/me`

Update member preferences.

**Authorization**: Authenticated member  
**Request**:
```json
{
  "preferredLanguage": "en"
}
```

**Response** (200 OK):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "preferredLanguage": "en",
  "updatedAt": "2026-03-24T16:00:00Z"
}
```

---

### `GET /members/me/consents`

View consent status.

**Authorization**: Authenticated member  
**Response** (200 OK):
```json
{
  "consents": [
    {
      "consentType": "CreditBureauReporting",
      "isGranted": false,
      "grantedAt": null,
      "revokedAt": null
    },
    {
      "consentType": "Marketing",
      "isGranted": true,
      "grantedAt": "2024-01-10T10:00:00Z",
      "revokedAt": null
    }
  ]
}
```

---

### `POST /members/me/consents`

Grant or revoke consent.

**Authorization**: Authenticated member  
**Request**:
```json
{
  "consentType": "CreditBureauReporting",
  "grant": true
}
```

**Response** (200 OK):
```json
{
  "consentType": "CreditBureauReporting",
  "isGranted": true,
  "grantedAt": "2026-03-24T16:30:00Z",
  "consentVersion": "1.0"
}
```

---

## Error Handling

All error responses follow this format:

```json
{
  "error": {
    "code": "INSUFFICIENT_FUNDS",
    "message": "Insufficient funds in your bank account to complete this contribution",
    "details": {
      "requiredAmount": 500.00,
      "availableBalance": 350.00
    },
    "timestamp": "2026-03-24T17:00:00Z",
    "requestId": "req_a1b2c3d4e5f6"
  }
}
```

**Common Error Codes**:
- `UNAUTHORIZED`: Missing or invalid authentication token
- `FORBIDDEN`: Member lacks required role (Chairperson/Treasurer)
- `NOT_FOUND`: Resource not found
- `VALIDATION_ERROR`: Request body validation failed
- `INSUFFICIENT_FUNDS`: Insufficient bank account balance
- `DUPLICATE_TRANSACTION`: Idempotency key already used
- `FICA_NOT_VERIFIED`: Member must complete KYC verification
- `QUORUM_NOT_MET`: Voting decision requires more member votes
- `BALANCE_TOO_LOW`: Group balance insufficient for payout

---

## Rate Limiting

**Limits**:
- **Authentication**: 5 failed login attempts per hour per account (then locked for 1 hour)
- **Contributions**: 10 contributions per hour per member
- **API calls**: 1000 requests per hour per member

**Rate Limit Headers**:
```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 987
X-RateLimit-Reset: 1679663400
```

**Response** (429 Too Many Requests):
```json
{
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Too many requests. Please try again in 15 minutes.",
    "retryAfter": 900
  }
}
```

---

## Versioning

API versioning is handled via URL path (`/v1/`, `/v2/`). Current version: **v1**.

**Deprecation Policy**: API versions are supported for 12 months after a new version is released.

---

## Pagination

Paginated endpoints use the following query parameters:
- `page` (int, default: 1): Page number (1-indexed)
- `pageSize` (int, default: 10, max: 100): Items per page

**Pagination Response Structure**:
```json
{
  "data": [...],
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "totalPages": 5,
    "totalItems": 47
  }
}
```

---

## Webhooks (Future)

Webhook support for real-time notifications (e.g., contribution received, payout executed) will be added in v2.

---

**End of REST API Contract**
