# Data Model: Digital Stokvel Banking MVP

**Date**: 2026-03-24  
**Purpose**: Define domain entities, relationships, and validation rules

---

## Entity Relationship Diagram

```
┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐
│     Member      │       │  GroupMember    │       │  StokvelsGroup  │
│─────────────────│       │─────────────────│       │─────────────────│
│ Id (UUID)       │◄──────│ MemberId (FK)   │──────►│ Id (UUID)       │
│ BankCustomerId  │       │ GroupId (FK)    │       │ Name            │
│ PhoneNumber     │       │ Role            │       │ Description     │
│ PreferredLang   │       │ JoinedAt        │       │ Type            │
│ FicaVerified    │       └─────────────────┘       │ Constitution    │
│ CreatedAt       │                                  │ Balance         │
└─────────────────┘                                  │ InterestTier    │
        │                                            │ AccruedInterest │
        │                                            └─────────────────┘
        │                                                     │
        │       ┌─────────────────┐                         │
        └──────►│  Contribution   │◄────────────────────────┘
                │─────────────────│
                │ Id (UUID)       │
                │ GroupId (FK)    │
                │ MemberId (FK)   │
                │ Amount          │
                │ PaymentMethod   │
                │ Status          │
                │ IdempotencyKey  │
                │ Timestamp       │
                └─────────────────┘
                        │
                        │
        ┌───────────────┴───────────────┐
        │                               │
┌───────▼─────────┐           ┌────────▼────────┐
│     Payout      │           │  GovernanceRule │
│─────────────────│           │─────────────────│
│ Id (UUID)       │           │ Id (UUID)       │
│ GroupId (FK)    │           │ GroupId (FK)    │
│ PayoutType      │           │ RuleType        │
│ TotalAmount     │           │ RuleValue       │
│ InitiatedBy     │           │ CreatedAt       │
│ ConfirmedBy     │           └─────────────────┘
│ Status          │
│ ExecutionTime   │
└─────────────────┘
        │
        │
┌───────▼─────────┐
│ PayoutRecipient │
│─────────────────│
│ Id (UUID)       │
│ PayoutId (FK)   │
│ MemberId (FK)   │
│ Amount          │
│ EftReference    │
│ DisbursedAt     │
└─────────────────┘
```

---

## Core Entities

### 1. Member

Represents a bank customer participating in stokvel groups.

**Properties**:
| Property | Type | Required | Constraints | Description |
|----------|------|----------|-------------|-------------|
| `Id` | UUID | ✓ | Primary Key | Unique member identifier |
| `BankCustomerId` | string | ✓ | Unique, Max 50 chars | Foreign key to bank's customer system |
| `PhoneNumber` | string | ✓ | E.164 format, Unique | Member's phone number (e.g., +27821234567) |
| `PreferredLanguage` | enum | ✓ | {en, zu, st, xh, af} | UI language preference |
| `FicaVerified` | bool | ✓ | Default: false | Whether KYC/FICA verification is complete |
| `FicaVerificationDate` | DateTime? | ✗ | Nullable | Date when FICA verification was completed |
| `CreatedAt` | DateTime | ✓ | Auto-set | Timestamp of member registration |

**Validation Rules**:
- `PhoneNumber` must be valid South African mobile number (starts with +2781, +2782, +2783, +2784, etc.)
- `FicaVerified` must be `true` before joining any group (FR-041)
- `BankCustomerId` must exist in bank's customer database

**Relationships**:
- One-to-many with `GroupMember` (member can join multiple groups)
- One-to-many with `Contribution` (member makes multiple contributions)
- One-to-many with `MemberConsent` (tracks consent for credit bureau, marketing)

**Indexes**:
```sql
CREATE INDEX idx_members_bank_customer ON members(bank_customer_id);
CREATE INDEX idx_members_phone ON members(phone_number);
```

---

### 2. StokvelsGroup

Represents a savings community with contribution rules and governance.

**Properties**:
| Property | Type | Required | Constraints | Description |
|----------|------|----------|-------------|-------------|
| `Id` | UUID | ✓ | Primary Key | Unique group identifier |
| `Name` | string | ✓ | Max 100 chars | Group display name |
| `Description` | string | ✗ | Max 500 chars | Optional group description |
| `GroupType` | enum | ✓ | {RotatingPayout, SavingsPot, InvestmentClub} | Stokvel type determines payout logic |
| `ContributionAmount` | decimal | ✓ | > 0, Precision(19,4) | Fixed contribution amount per cycle (e.g., R500.0000) |
| `ContributionFrequency` | enum | ✓ | {Weekly, Monthly} | Contribution cadence |
| `PayoutSchedule` | jsonb | ✓ | Valid JSON | Payout configuration (see structure below) |
| `Constitution` | jsonb | ✓ | Valid JSON | Group governance rules (see structure below) |
| `AccountBalance` | decimal | ✓ | >= 0, Precision(19,4) | Current group wallet balance (principal + capitalized interest) |
| `InterestTier` | enum | ✓ | {Tier1, Tier2, Tier3} | Interest rate tier based on balance (Tier1: 3.5%, Tier2: 4.5%, Tier3: 5.5%) |
| `AccruedInterest` | decimal | ✓ | >= 0, Precision(19,4) | Interest accrued since last capitalization |
| `CreatedAt` | DateTime | ✓ | Auto-set | Group creation timestamp |
| `ModifiedAt` | DateTime | ✓ | Auto-updated | Last modification timestamp |
| `RowVersion` | long | ✓ | Optimistic concurrency token | Prevents concurrent update conflicts |

**PayoutSchedule JSON Structure**:
```json
{
  "type": "rotating",  // or "year_end", "on_demand"
  "cycleDays": 30,     // Payout every 30 days (monthly)
  "currentRecipientOrder": 1,  // Next member in rotation
  "nextPayoutDate": "2026-04-24T00:00:00Z"
}
```

**Constitution JSON Structure**:
```json
{
  "missedPaymentPenalty": 50.00,      // Late fee in ZAR
  "gracePeriodDays": 7,                // Days before penalty applied
  "quorumThreshold": 0.60,             // 60% of members required for votes
  "memberRemovalCriteria": "3_consecutive_misses",
  "partialWithdrawalAllowed": false   // Requires quorum vote if true
}
```

**Validation Rules**:
- `ContributionAmount` must be between R50 and R100,000 per cycle
- `AccountBalance` cannot go negative (enforced by CHECK constraint)
- `InterestTier` automatically updates when balance crosses thresholds:
  - Tier1: R0 - R9,999.99 → 3.5%
  - Tier2: R10,000 - R49,999.99 → 4.5%
  - Tier3: R50,000+ → 5.5%
- `PayoutSchedule.type` must match `GroupType` (e.g., RotatingPayout groups must have "rotating" schedule)

**State Transitions**:
```
[Draft] → [Active] → [Suspended] → [Closed]
         ↓            ↓
      [Archived] ← [Archived]

Draft: Group created but not yet accepting contributions
Active: Actively collecting contributions and processing payouts
Suspended: Temporarily paused (e.g., governance dispute)
Closed: All funds disbursed, group dissolved
Archived: Historical record, no longer operational
```

**Relationships**:
- One-to-many with `GroupMember` (group has multiple members)
- One-to-many with `Contribution` (group receives multiple contributions)
- One-to-many with `Payout` (group makes multiple payouts)
- One-to-many with `GovernanceRule` (group has multiple custom rules)

**Indexes**:
```sql
CREATE INDEX idx_groups_type ON groups(group_type);
CREATE INDEX idx_groups_tier ON groups(interest_tier);
CREATE INDEX idx_groups_constitution ON groups USING GIN (constitution);
CREATE INDEX idx_groups_payout_schedule ON groups USING GIN (payout_schedule);
```

---

### 3. GroupMember (Join Table)

Represents membership relationship between Member and Group with role assignment.

**Properties**:
| Property | Type | Required | Constraints | Description |
|----------|------|----------|-------------|-------------|
| `Id` | UUID | ✓ | Primary Key | Unique membership identifier |
| `GroupId` | UUID | ✓ | Foreign Key (StokvelsGroup) | Reference to group |
| `MemberId` | UUID | ✓ | Foreign Key (Member) | Reference to member |
| `Role` | enum | ✓ | {Chairperson, Treasurer, Secretary, Member} | Member's role in group |
| `JoinedAt` | DateTime | ✓ | Auto-set | Timestamp of joining |

**Validation Rules**:
- A member can only be in a group once (unique constraint on `GroupId + MemberId`)
- A group must have exactly one `Chairperson`
- A group can have at most one `Treasurer` and one `Secretary`
- Role changes require Chairperson approval

**Relationships**:
- Many-to-one with `StokvelsGroup`
- Many-to-one with `Member`

**Indexes**:
```sql
CREATE UNIQUE INDEX idx_group_members_unique ON group_members(group_id, member_id);
CREATE INDEX idx_group_members_role ON group_members(group_id, role);
```

---

### 4. Contribution

Represents a payment transaction from member to group wallet.

**Properties**:
| Property | Type | Required | Constraints | Description |
|----------|------|----------|-------------|-------------|
| `Id` | UUID | ✓ | Primary Key | Unique contribution identifier |
| `GroupId` | UUID | ✓ | Foreign Key (StokvelsGroup) | Reference to group receiving funds |
| `MemberId` | UUID | ✓ | Foreign Key (Member) | Reference to member making payment |
| `Amount` | decimal | ✓ | > 0, Precision(19,4) | Contribution amount (e.g., R500.0000) |
| `PaymentMethod` | enum | ✓ | {OneTap, DebitOrder, Ussd} | Payment channel used |
| `Status` | enum | ✓ | {Pending, Completed, Failed} | Transaction status |
| `IdempotencyKey` | UUID | ✓ | Unique | Prevents duplicate transactions |
| `TransactionTimestamp` | DateTime | ✓ | Auto-set | When transaction occurred |
| `ConfirmationReceiptUrl` | string | ✗ | Max 500 chars | URL to downloadable receipt (PDF) |
| `CreatedAt` | DateTime | ✓ | Auto-set | Record creation timestamp |

**Validation Rules**:
- `Amount` must match group's `ContributionAmount` (or allow overpayment with flag)
- `IdempotencyKey` must be unique across all contributions (prevents duplicate charges)
- `Status` cannot transition from `Completed` to any other state (immutable)
- Member must be an active participant in the group

**State Transitions**:
```
[Pending] → [Completed]
         → [Failed]

Pending: Payment initiated but not yet confirmed
Completed: Payment successful, ledger updated, receipt issued
Failed: Payment failed (insufficient funds, network error, etc.)
```

**Relationships**:
- Many-to-one with `StokvelsGroup`
- Many-to-one with `Member`

**Indexes**:
```sql
CREATE INDEX idx_contributions_group_member ON contributions(group_id, member_id, transaction_timestamp DESC);
CREATE UNIQUE INDEX idx_contributions_idempotency ON contributions(idempotency_key);
CREATE INDEX idx_contributions_status ON contributions(status) WHERE status = 'Pending';
```

---

### 5. Payout

Represents a disbursement transaction from group wallet to one or more members.

**Properties**:
| Property | Type | Required | Constraints | Description |
|----------|------|----------|-------------|-------------|
| `Id` | UUID | ✓ | Primary Key | Unique payout identifier |
| `GroupId` | UUID | ✓ | Foreign Key (StokvelsGroup) | Reference to group disbursing funds |
| `PayoutType` | enum | ✓ | {Rotating, YearEnd, Partial} | Type of payout |
| `TotalAmount` | decimal | ✓ | > 0, Precision(19,4) | Total amount being paid out |
| `InitiatedBy` | UUID | ✓ | Foreign Key (Member) | Chairperson who initiated payout |
| `ConfirmedBy` | UUID | ✗ | Foreign Key (Member) | Treasurer who confirmed payout (nullable until confirmed) |
| `Status` | enum | ✓ | {PendingConfirmation, Confirmed, Executed, Rejected} | Payout workflow status |
| `ExecutionTimestamp` | DateTime | ✗ | Nullable | When payout was executed (null until executed) |
| `CreatedAt` | DateTime | ✓ | Auto-set | Payout request timestamp |

**Validation Rules**:
- `InitiatedBy` must be a member with `Chairperson` role in the group
- `ConfirmedBy` must be a member with `Treasurer` role (or null if not yet confirmed)
- For `Rotating` payouts, `TotalAmount` should equal principal contributions only (interest stays in wallet)
- For `YearEnd` payouts, `TotalAmount` should include principal + all accrued interest
- `TotalAmount` cannot exceed group's `AccountBalance`

**State Transitions**:
```
[PendingConfirmation] → [Confirmed] → [Executed]
                      → [Rejected]

PendingConfirmation: Chairperson initiated, waiting for Treasurer confirmation
Confirmed: Treasurer confirmed, ready to execute EFT
Executed: Funds disbursed to member(s), transaction complete
Rejected: Treasurer rejected payout request
```

**Relationships**:
- Many-to-one with `StokvelsGroup`
- Many-to-one with `Member` (InitiatedBy)
- Many-to-one with `Member` (ConfirmedBy)
- One-to-many with `PayoutRecipient` (can have multiple recipients for YearEnd payouts)

**Indexes**:
```sql
CREATE INDEX idx_payouts_group ON payouts(group_id, created_at DESC);
CREATE INDEX idx_payouts_status ON payouts(status) WHERE status IN ('PendingConfirmation', 'Confirmed');
```

---

### 6. PayoutRecipient

Represents individual recipient(s) of a payout transaction.

**Properties**:
| Property | Type | Required | Constraints | Description |
|----------|------|----------|-------------|-------------|
| `Id` | UUID | ✓ | Primary Key | Unique recipient record identifier |
| `PayoutId` | UUID | ✓ | Foreign Key (Payout) | Reference to parent payout |
| `MemberId` | UUID | ✓ | Foreign Key (Member) | Reference to recipient member |
| `Amount` | decimal | ✓ | > 0, Precision(19,4) | Amount disbursed to this recipient |
| `EftReference` | string | ✗ | Max 50 chars | Bank EFT reference number |
| `DisbursedAt` | DateTime | ✗ | Nullable | When funds were transferred (null until executed) |

**Validation Rules**:
- Sum of all `PayoutRecipient.Amount` for a `PayoutId` must equal `Payout.TotalAmount`
- For `Rotating` payouts, only one recipient per payout
- For `YearEnd` payouts, multiple recipients with amounts proportional to contributions

**Relationships**:
- Many-to-one with `Payout`
- Many-to-one with `Member`

**Indexes**:
```sql
CREATE INDEX idx_payout_recipients_payout ON payout_recipients(payout_id);
CREATE INDEX idx_payout_recipients_member ON payout_recipients(member_id, disbursed_at DESC);
```

---

### 7. GovernanceRule

Represents custom rules defined by a group for missed payments, voting, and member removal.

**Properties**:
| Property | Type | Required | Constraints | Description |
|----------|------|----------|-------------|-------------|
| `Id` | UUID | ✓ | Primary Key | Unique rule identifier |
| `GroupId` | UUID | ✓ | Foreign Key (StokvelsGroup) | Reference to group owning this rule |
| `RuleType` | enum | ✓ | {MissedPayment, LateFee, MemberRemoval, QuorumVoting} | Category of rule |
| `RuleValue` | jsonb | ✓ | Valid JSON | Rule configuration (see examples below) |
| `CreatedAt` | DateTime | ✓ | Auto-set | Rule creation timestamp |

**RuleValue JSON Examples**:
```json
// MissedPayment rule
{
  "gracePeriodDays": 7,
  "notificationSchedule": ["day_3_before", "day_1_before", "day_of_due"],
  "escalationAction": "notify_chairperson"
}

// LateFee rule
{
  "penaltyAmount": 50.00,
  "applyAfterDays": 7,
  "maxPenalty": 200.00
}

// MemberRemoval rule
{
  "criteria": "3_consecutive_misses",
  "votingRequired": true,
  "quorumThreshold": 0.60
}

// QuorumVoting rule
{
  "defaultQuorum": 0.60,
  "votingDurationHours": 72,
  "tieBreaker": "chairperson_decides"
}
```

**Validation Rules**:
- A group can have multiple rules of different `RuleType` but only one rule per `RuleType`
- `RuleValue` schema must be valid for the specified `RuleType`

**Relationships**:
- Many-to-one with `StokvelsGroup`

**Indexes**:
```sql
CREATE INDEX idx_governance_rules_group_type ON governance_rules(group_id, rule_type);
```

---

### 8. Dispute

Represents a flagged issue raised by a member requiring resolution.

**Properties**:
| Property | Type | Required | Constraints | Description |
|----------|------|----------|-------------|-------------|
| `Id` | UUID | ✓ | Primary Key | Unique dispute identifier |
| `GroupId` | UUID | ✓ | Foreign Key (StokvelsGroup) | Reference to group where dispute occurred |
| `RaisedBy` | UUID | ✓ | Foreign Key (Member) | Member who raised the dispute |
| `DisputeType` | enum | ✓ | {MissedPaymentClaim, PayoutDispute, MemberRemoval, Other} | Category of dispute |
| `Description` | string | ✓ | Max 1000 chars | Member's explanation of the issue |
| `Status` | enum | ✓ | {Open, InReview, Resolved, Escalated} | Current dispute status |
| `ChairpersonResponse` | string | ✗ | Max 1000 chars | Chairperson's response (null until responded) |
| `ResolutionNotes` | string | ✗ | Max 1000 chars | Final resolution details |
| `CreatedAt` | DateTime | ✓ | Auto-set | Dispute creation timestamp |
| `ResolvedAt` | DateTime | ✗ | Nullable | When dispute was resolved (null until resolved) |

**Validation Rules**:
- `RaisedBy` must be an active member of the group
- `Status` cannot transition from `Resolved` to any other state (final)

**State Transitions**:
```
[Open] → [InReview] → [Resolved]
                    → [Escalated] → [Resolved]

Open: Dispute flagged, Chairperson notified
InReview: Chairperson reviewing and responding
Resolved: Issue resolved within group
Escalated: Escalated to bank mediation team
```

**Relationships**:
- Many-to-one with `StokvelsGroup`
- Many-to-one with `Member` (RaisedBy)

**Indexes**:
```sql
CREATE INDEX idx_disputes_group_status ON disputes(group_id, status);
CREATE INDEX idx_disputes_raised_by ON disputes(raised_by, created_at DESC);
```

---

### 9. MemberConsent

Represents explicit consent records for POPIA compliance.

**Properties**:
| Property | Type | Required | Constraints | Description |
|----------|------|----------|-------------|-------------|
| `Id` | UUID | ✓ | Primary Key | Unique consent record identifier |
| `MemberId` | UUID | ✓ | Foreign Key (Member) | Reference to member giving consent |
| `ConsentType` | enum | ✓ | {CreditBureauReporting, Marketing, GroupDataSharing} | Type of consent |
| `IsGranted` | bool | ✓ | Default: false | Whether consent is currently granted |
| `GrantedAt` | DateTime | ✗ | Nullable | When consent was granted (null if never granted) |
| `RevokedAt` | DateTime | ✗ | Nullable | When consent was revoked (null if still granted) |
| `ConsentVersion` | string | ✓ | Max 10 chars | Version of terms user consented to (e.g., "1.0") |
| `IpAddress` | string | ✓ | Max 45 chars | IP address where consent was given (audit trail) |

**Validation Rules**:
- A member can have only one active consent record per `ConsentType` (unique constraint on `MemberId + ConsentType` where `RevokedAt IS NULL`)
- `RevokedAt` must be after `GrantedAt` if both are set

**Relationships**:
- Many-to-one with `Member`

**Indexes**:
```sql
CREATE UNIQUE INDEX idx_member_consent_active ON member_consents(member_id, consent_type) WHERE revoked_at IS NULL;
```

---

### 10. IdempotencyLog

Prevents duplicate financial transactions.

**Properties**:
| Property | Type | Required | Constraints | Description |
|----------|------|----------|-------------|-------------|
| `IdempotencyKey` | UUID | ✓ | Primary Key | Unique idempotency key from client |
| `EntityType` | string | ✓ | Max 50 chars | Type of entity (e.g., "Contribution", "Payout") |
| `EntityId` | UUID | ✓ | - | ID of the created entity |
| `Status` | enum | ✓ | {Pending, Completed, Failed} | Transaction status |
| `CreatedAt` | DateTime | ✓ | Auto-set | Log entry timestamp |

**Validation Rules**:
- `IdempotencyKey` must be unique (primary key)
- API middleware checks this table before processing financial transactions

**Indexes**:
```sql
CREATE INDEX idx_idempotency_status ON idempotency_log(status, created_at) WHERE status = 'Pending';
```

---

### 11. AuditLog

Immutable audit trail for all financial operations.

**Properties**:
| Property | Type | Required | Constraints | Description |
|----------|------|----------|-------------|-------------|
| `Id` | long | ✓ | Primary Key (auto-increment) | Unique log entry ID |
| `TableName` | string | ✓ | Max 50 chars | Table being audited |
| `RecordId` | UUID | ✓ | - | ID of the affected record |
| `Operation` | enum | ✓ | {Insert, Update, Delete} | Type of operation |
| `OldValues` | jsonb | ✗ | Valid JSON | Previous values (null for INSERT) |
| `NewValues` | jsonb | ✓ | Valid JSON | New values |
| `ChangedBy` | string | ✓ | Max 50 chars | User/system identifier |
| `ChangedAt` | DateTime | ✓ | Auto-set | Timestamp of change |

**Validation Rules**:
- Records are append-only (no UPDATE or DELETE allowed on this table)
- Retained for 7 years per FICA compliance (FR-045)

**Indexes**:
```sql
CREATE INDEX idx_audit_log_table_record ON audit_log(table_name, record_id, changed_at DESC);
CREATE INDEX idx_audit_log_changed_at ON audit_log(changed_at) WHERE changed_at > NOW() - INTERVAL '7 days';
```

---

## Summary

**Total Entities**: 11 core entities + supporting tables (IdempotencyLog, AuditLog)

**Key Design Decisions**:
1. **Money Type**: All currency amounts use `decimal` (Precision 19,4) to avoid floating-point errors
2. **JSONB for Flexibility**: Constitution and PayoutSchedule stored as jsonb to allow group-specific customization without schema changes
3. **Idempotency**: Every financial transaction requires a unique `IdempotencyKey` to prevent duplicate charges
4. **Audit Trail**: All financial operations logged to immutable `AuditLog` table for 7-year retention
5. **Optimistic Concurrency**: `RowVersion` on Group entity prevents race conditions during concurrent contributions/payouts
6. **POPIA Compliance**: `MemberConsent` entity tracks explicit consent for credit bureau, marketing, and data sharing
7. **Multi-Tenancy**: PostgreSQL RLS enforces group isolation at database level (not shown in entity model but implemented in repository layer)

**Next Steps**: Define REST API contracts and USSD flows based on these entities.
