# Feature Specification: Digital Stokvel Banking MVP

**Feature Branch**: `001-stokvel-banking-mvp`  
**Created**: 2026-03-24  
**Status**: Draft  
**Input**: User description: "Digital Stokvel Banking platform that formalizes South Africa's R50B informal savings economy by providing group savings accounts, digital contributions, automated payouts, and multilingual access across Android, iOS, USSD, and Web"

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.
  
  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - Chairperson Creates and Manages Stokvel Group (Priority: P0)

A bank customer (Chairperson) creates a new stokvel group, invites members, sets contribution rules, and manages the group roster. This represents the foundational capability—without groups, no other features can function.

**Why this priority**: This is the entry point to the entire platform. No stokvel activity can occur without group creation. Delivers immediate value by providing a digital space for communities already organizing informally via WhatsApp.

**Independent Test**: Can be fully tested by a Chairperson creating a group with a name, type, contribution amount, and invited members (via phone number or share link), then viewing the group dashboard showing roster and contribution schedule.

**Acceptance Scenarios**:

1. **Given** a verified bank customer is logged in, **When** they navigate to "Create Stokvel Group", **Then** they can enter group name, description, type (rotating payout/savings pot/investment club), contribution amount, frequency (weekly/monthly), and payout schedule
2. **Given** a group is created, **When** the Chairperson invites members via phone number, **Then** invited members receive an SMS/push notification with a join link
3. **Given** a Chairperson has created a group, **When** they assign roles (Treasurer, Secretary), **Then** those members have role-specific permissions (e.g., Treasurer approves payouts)
4. **Given** a group has 5 members, **When** the Chairperson views the group dashboard, **Then** they see full member roster, contribution schedule, and upcoming payment dates
5. **Given** an invited member is not a bank customer, **When** they click the join link, **Then** they are guided through simplified onboarding (ID + selfie) before joining

---

### User Story 2 - Member Contributes to Group Savings (Priority: P0)

A group member makes monthly contributions to the group wallet via one-tap payment, debit order, or USSD dial-in. They receive confirmation receipts and can view their contribution history.

**Why this priority**: Contributions are the lifeblood of stokvels—this is the core value proposition that replaces cash payments. Without this, the group wallet remains empty and the product has no purpose.

**Independent Test**: Can be fully tested by a member logging in, selecting their group, making a R500 payment, receiving a confirmation receipt, and seeing the transaction logged in their personal history and the group ledger.

**Acceptance Scenarios**:

1. **Given** a member is logged into the app, **When** they navigate to their group and tap "Pay Contribution", **Then** they can pay the set amount (e.g., R500) from their linked bank account with one tap
2. **Given** a member has set up a debit order, **When** the contribution due date arrives, **Then** the system automatically deducts the amount and logs it to the group ledger
3. **Given** a member is using a feature phone, **When** they dial *120*STOKVEL# and follow the USSD menu, **Then** they can make a contribution using their bank PIN
4. **Given** a contribution is due in 3 days, **When** the system timer triggers, **Then** the member receives a payment reminder via push notification, SMS, or USSD
5. **Given** a member completes a payment, **When** the transaction succeeds, **Then** they receive a branded contribution receipt that can be shared in group meetings
6. **Given** a member views the group wallet, **When** they check the ledger, **Then** they see who paid, when, and how much (full transparency)

---

### User Story 3 - Group Receives Interest-Bearing Pooled Savings (Priority: P0)

All member contributions flow into a single group savings account held by the bank. The account earns tiered interest (3.5%–5.5% based on balance) with daily compounding and monthly capitalization visible to all members.

**Why this priority**: This is the core value proposition for members—earning interest on pooled funds that currently earn nothing when held as cash. This differentiates the bank product from informal stokvels.

**Independent Test**: Can be fully tested by a group accumulating R10,000 in contributions over time, then members viewing the group wallet balance showing principal + accrued interest, with interest breakdown visible.

**Acceptance Scenarios**:

1. **Given** a group has R10,000 in total contributions, **When** a member views the group wallet, **Then** they see the balance earning 4.5% annual interest (tiered rate for R10K–R50K)
2. **Given** contributions are made throughout the month, **When** month-end arrives, **Then** accrued interest is capitalized to the group wallet and visible to all members
3. **Given** a group balance reaches R50,001, **When** members check the interest rate, **Then** they see the rate has increased to 5.5% (tiered interest schedule)
4. **Given** a member is viewing the group wallet, **When** they tap "Interest Details", **Then** they see daily compounding calculation, current balance, and year-to-date interest earned
5. **Given** the Chairperson views the wallet, **When** they attempt to withdraw funds, **Then** the system blocks unilateral withdrawal and requires quorum approval (governance rule)

---

### User Story 4 - Automated Payouts to Members (Priority: P1)

The group defines payout rules (rotating payout per cycle or year-end pot distribution). Chairperson initiates payout, Treasurer confirms, and the system automatically disburses funds to member bank accounts with full transparency.

**Why this priority**: Payouts are the culmination of the savings cycle—the moment members receive value. Automating this eliminates cash risk, fraud, and the logistical burden of manual distribution.

**Independent Test**: Can be fully tested by a group reaching payout time, Chairperson initiating payout, Treasurer confirming, and designated member(s) receiving instant EFT with all members notified.

**Acceptance Scenarios**:

1. **Given** a rotating payout group reaches the end of a cycle, **When** the Chairperson initiates payout to the designated member (e.g., Member 3), **Then** the Treasurer receives a confirmation request
2. **Given** the Treasurer reviews the payout request, **When** they confirm, **Then** the system instantly transfers funds to Member 3's bank account via EFT
3. **Given** a year-end pot group reaches December 31, **When** the Chairperson initiates full distribution, **Then** funds are disbursed proportionally to all members based on their contribution totals
4. **Given** a payout is executed, **When** the transaction completes, **Then** all group members receive a notification: "Payout of R6,400 sent to Ntombizodwa"
5. **Given** a group wants to distribute only partial funds, **When** the Chairperson attempts payout, **Then** the system requires quorum approval (e.g., 60% of members must vote to approve)

---

### User Story 5 - USSD Access for Feature Phone Users (Priority: P0)

Members without smartphones can participate fully via USSD (*120*STOKVEL#) by contributing, checking balance, viewing recent transactions, and receiving payout notifications—all with max 3-level menu depth and 120-second session persistence.

**Why this priority**: 19% of South Africans use feature phones, and 33% are financially excluded. USSD access is non-negotiable for reaching the core target market and ensuring no one is left behind.

**Independent Test**: Can be fully tested by a feature phone user dialing *120*STOKVEL#, authenticating with bank PIN, making a contribution, and checking their group balance—all without a smartphone.

**Acceptance Scenarios**:

1. **Given** a member has a feature phone, **When** they dial *120*STOKVEL#, **Then** they see a menu in their chosen language (English/isiZulu/Sesotho/Xhosa/Afrikaans)
2. **Given** a USSD session is active, **When** the user selects "Pay Contribution", **Then** they see their group name, amount due, and confirmation prompt: "Confirm: Pay R500 to Ntombizodwa Stokvel? 1=Yes, 2=No"
3. **Given** a user confirms payment, **When** they press 1, **Then** they authenticate with their bank PIN and the system processes the contribution
4. **Given** a user selects "Check Balance", **When** the system responds, **Then** they see group balance and their total contributions to date
5. **Given** a network interruption occurs mid-session, **When** the user redials within 120 seconds, **Then** the session state is restored and they can continue
6. **Given** a payout is made to a USSD user, **When** the disbursement completes, **Then** they receive an SMS notification with the amount and confirmation code

---

### User Story 6 - Group Self-Governance and Dispute Resolution (Priority: P1)

Groups define their own constitution (rules for missed payments, late fees, member removal). In-app voting handles major decisions. Members can flag disputes, and the bank mediates only if escalated.

**Why this priority**: Stokvels are self-governing communities. The bank must enable autonomy while providing structure. This builds trust and respects cultural tradition.

**Independent Test**: Can be fully tested by a group defining a rule "missed payment = R50 late fee", then a member missing a payment triggers automated notice, grace period, and fee application per group rule.

**Acceptance Scenarios**:

1. **Given** a Chairperson is setting up a new group, **When** they use the constitution builder, **Then** they can define: missed payment penalty (e.g., R50), grace period (e.g., 7 days), and member removal criteria (e.g., 3 consecutive misses)
2. **Given** a member misses a payment, **When** the due date passes, **Then** the system sends automated notice, starts grace period timer, and notifies the Chairperson
3. **Given** a Chairperson proposes changing the contribution amount from R500 to R750, **When** they initiate in-app voting, **Then** all members receive a voting prompt and results are binding once quorum reached (e.g., 60% participation)
4. **Given** a member disputes a missed payment claim, **When** they raise a dispute flag in the app, **Then** the Chairperson receives the dispute with member's explanation, and bank escalation path is available if unresolved
5. **Given** a group votes to remove a member, **When** the vote passes, **Then** the member's access is revoked, their contribution history is preserved, and they receive their proportional share

---

### User Story 7 - Multilingual Interface for Inclusivity (Priority: P0)

The entire app and USSD experience is available in 5 languages (English, isiZulu, Sesotho, Xhosa, Afrikaans) with language selection at onboarding and changeable in settings.

**Why this priority**: Language is a trust signal. For a product targeting communities where English may not be the first language, local language support is essential for trust and adoption.

**Independent Test**: Can be fully tested by a user selecting isiZulu at onboarding, navigating the app, and confirming all UI elements, error messages, and USSD menus are in isiZulu.

**Acceptance Scenarios**:

1. **Given** a new user opens the app, **When** they reach the onboarding screen, **Then** they are prompted to select their preferred language from 5 options
2. **Given** a user has selected isiZulu, **When** they navigate the app, **Then** all buttons, labels, error messages, and notifications appear in isiZulu
3. **Given** a USSD user dials *120*STOKVEL#, **When** the menu loads, **Then** it appears in their previously selected language (or defaults to phone language)
4. **Given** a user receives a payment reminder, **When** the SMS/push notification arrives, **Then** it is in their chosen language: "Ikhontribushini yakho ye-R500 izofika emini yakusasa" (isiZulu)
5. **Given** a user wants to change language, **When** they navigate to Settings > Language, **Then** they can switch to any of the 5 languages and the change applies immediately

---

### Edge Cases

- What happens when a member tries to contribute more than the set amount?
  - Allow overpayment and log as "extra contribution" visible in ledger, or block and show "Amount exceeds R500 per cycle — contact Chairperson to adjust"
- How does the system handle a Chairperson leaving or becoming inactive?
  - Group constitution must define succession rule; backup role (Treasurer) can escalate to bank support for emergency role transfer
- What happens when a group accumulates large balance (>R100K) triggering AML review?
  - System flags for AML monitoring per compliance rules; bank reviews and may contact Chairperson for verification
- How does the system handle network failure during a contribution payment?
  - Transaction uses idempotency key; if timeout occurs, member receives "Payment pending — check status in 5 minutes" and system auto-reconciles within 2 hours
- What happens when a member disputes their contribution amount after payment?
  - Dispute flag allows member to explain; bank reviews transaction log (immutable ledger) and mediates with Chairperson if necessary
- How does the system handle a group wanting to close/dissolve early?
  - Quorum vote required (e.g., 75% agreement); upon approval, remaining balance distributed proportionally with final interest calculation

## Requirements *(mandatory)*

### Functional Requirements

**Group Creation & Management (F-01)**
- **FR-001**: System MUST allow verified bank customers to create a named stokvel group with description, type (rotating payout, savings pot, investment club), contribution amount, frequency (weekly/monthly), and payout schedule
- **FR-002**: System MUST allow Chairperson to invite members via phone number, share link, or QR code
- **FR-003**: System MUST support role assignment (Chairperson, Treasurer, Secretary) with role-specific permissions
- **FR-004**: System MUST require all members to have or open a bank account before joining (embedded onboarding for non-customers: ID + selfie for app users, branch/ATM verification for USSD users)
- **FR-005**: System MUST display full group roster, contribution schedule, and upcoming payment dates to Chairperson

**Digital Group Wallet (F-02)**
- **FR-006**: System MUST create a dedicated group savings account for each stokvel group earning tiered interest: R0–R10K at 3.5%, R10K–R50K at 4.5%, R50K+ at 5.5%
- **FR-007**: System MUST display real-time balance visible to all members with role-based permissions (Chairperson sees full controls, members see view-only)
- **FR-008**: System MUST maintain full contribution history (who paid, when, how much) accessible to all members
- **FR-009**: System MUST block unilateral withdrawal by Chairperson without quorum approval
- **FR-010**: System MUST calculate interest with daily compounding and monthly capitalization to group wallet
- **FR-011**: System MUST display interest breakdown showing principal, accrued interest, and year-to-date earnings

**Contribution Collection (F-03)**
- **FR-012**: System MUST support one-tap payment from member's linked bank account
- **FR-013**: System MUST support debit order/recurring payment setup for automatic contributions
- **FR-014**: System MUST support USSD dial-in payment (*120*STOKVEL#) for feature phone users authenticated with bank PIN
- **FR-015**: System MUST send payment reminders 3 days and 1 day before due date via push notification, SMS, or USSD
- **FR-016**: System MUST issue contribution confirmation receipt to member and log transaction on group ledger
- **FR-017**: System MUST ensure all transactions are immutable and auditable

**Payout Engine (F-04)**
- **FR-018**: System MUST support rotating payout with automated disbursement to designated member each cycle
- **FR-019**: System MUST support year-end pot with full balance disbursed proportionally to all members
- **FR-020**: System MUST require Chairperson to initiate payout and Treasurer to confirm before execution
- **FR-021**: System MUST execute payouts via instant EFT to member bank accounts
- **FR-022**: System MUST send payout notifications to all group members for transparency
- **FR-023**: System MUST require quorum approval for partial withdrawals not defined in group rules

**Group Governance & Dispute Resolution (F-05)**
- **FR-024**: System MUST provide constitution builder allowing groups to define rules for missed payments, late fees, and member removal
- **FR-025**: System MUST support in-app voting for major decisions (change contribution amount, remove member, adjust payout schedule)
- **FR-026**: System MUST automate missed payment escalation: notice, grace period, Chairperson notification per group rules
- **FR-027**: System MUST allow members to raise dispute flags with explanation
- **FR-028**: System MUST provide bank mediation escalation path if disputes are unresolved by group

**Multilingual Interface (F-06)**
- **FR-029**: System MUST support full UI in 5 languages: English, isiZulu, Sesotho, Xhosa, Afrikaans
- **FR-030**: System MUST support USSD menus in all 5 languages
- **FR-031**: System MUST prompt language selection at onboarding
- **FR-032**: System MUST allow language change in Settings with immediate application
- **FR-033**: System MUST deliver all notifications (push, SMS, USSD) in user's chosen language

**Platform Requirements**
- **FR-034**: Android app MUST support full feature set (F-01 to F-06) with Material Design 3 and offline-tolerant architecture
- **FR-035**: iOS app MUST support full feature set with feature parity to Android
- **FR-036**: USSD MUST support core flows (contribute, check balance, payout notification) with max 3-level menu depth
- **FR-037**: USSD sessions MUST persist state for 120 seconds to handle network interruptions
- **FR-038**: Web browser MUST provide Chairperson admin dashboard (member management, contribution tracking, payout approvals) with desktop-first responsive design

**Security & Compliance**
- **FR-039**: System MUST comply with Protection of Personal Information Act (POPIA) requiring explicit consent for credit bureau reporting and marketing
- **FR-040**: System MUST verify all group members per Financial Intelligence Centre Act (FICA/KYC) before allowing participation
- **FR-041**: System MUST monitor group accounts for AML thresholds: flag deposits >R20K or monthly inflows >R100K
- **FR-042**: System MUST store all data on South Africa-domiciled infrastructure per SARB requirements
- **FR-043**: System MUST encrypt data at rest (AES-256) and in transit (TLS 1.3+)
- **FR-044**: System MUST retain access logs for 7 years per FICA requirements

**Cultural & Trust Design**
- **FR-045**: System MUST use communal language ('your group', 'your savings pot', 'your contribution') not clinical banking terms ('account', 'product', 'client')
- **FR-046**: System MUST display bank logo, FSCA badge, and 'Your money is protected' disclosure on group wallet screen
- **FR-047**: System MUST provide branded, shareable contribution receipts for group meeting verification
- **FR-048**: System MUST allow group ledger export as PDF for annual AGM records
- **FR-049**: System MUST use encouraging error messages (e.g., "Your payment didn't go through this time—let's try again") not alarming messages (e.g., "Transaction Failed")
- **FR-050**: System MUST NOT contact group members directly about products without Chairperson consent

### Key Entities

- **Stokvel Group**: Represents a savings community with name, description, type (rotating/pot/investment), contribution rules (amount, frequency), payout schedule, constitution (governance rules), member roles, and linked group savings account
- **Member**: A bank customer participating in one or more groups with role (Chairperson/Treasurer/Secretary/Member), contribution history, language preference, notification settings, and linked bank account
- **Group Savings Account**: Bank-held account in group name with balance, interest tier (3.5%/4.5%/5.5%), accrued interest, transaction ledger, and withdrawal governance rules
- **Contribution**: A payment transaction with member ID, group ID, amount, timestamp, payment method (one-tap/debit order/USSD), confirmation receipt, and ledger entry
- **Payout**: A disbursement transaction with group ID, payout type (rotating/year-end/partial), recipient member(s), amount(s), approval chain (Chairperson initiated, Treasurer confirmed), and notification records
- **Governance Rule**: Group-defined policies for missed payments (penalty amount, grace period), member removal (criteria, voting threshold), and major decisions (quorum requirements)
- **Dispute**: A flagged issue raised by member with description, timestamp, status (open/resolved/escalated), Chairperson response, and bank mediation path

## Success Criteria *(mandatory)*

### Measurable Outcomes

**MVP Launch (3-Month Target)**
- **SC-001**: 500 active stokvel groups created within 3 months of launch
- **SC-002**: 5,000 members onboarded and participating in groups
- **SC-003**: R5 million in pooled deposits under management
- **SC-004**: 30% of groups originated via USSD (demonstrating financial inclusion success)
- **SC-005**: Net Promoter Score (NPS) among stokvel users exceeds 60

**User Experience & Performance**
- **SC-006**: Chairperson can create a group and invite 10 members in under 5 minutes
- **SC-007**: Members can complete a contribution payment in under 30 seconds (one-tap or USSD)
- **SC-008**: 95% of contribution payments complete successfully on first attempt
- **SC-009**: USSD sessions complete within max 3-level menu depth with no user getting lost
- **SC-010**: Users can navigate the app entirely in their chosen language with 100% UI coverage

**Financial & Business Metrics**
- **SC-011**: R25,000 in interest revenue generated from pooled deposits within 3 months
- **SC-012**: Average group balance grows from R1,000 to R10,000 within 12 months
- **SC-013**: 90% of groups make at least one payout within 12 months
- **SC-014**: Churn rate among stokvel users is <10% (vs. bank average of 15%+)

**Governance & Trust**
- **SC-015**: 80% of groups define their own constitution rules during setup
- **SC-016**: Disputes raised are resolved within group 90% of the time (only 10% escalate to bank)
- **SC-017**: Zero unilateral withdrawals by Chairpersons without required approvals
- **SC-018**: 100% of contribution transactions logged to immutable ledger with full member visibility

**Compliance & Security**
- **SC-019**: 100% of group members complete FICA/KYC verification before first contribution
- **SC-020**: Zero POPIA violations or data breaches during MVP period
- **SC-021**: AML monitoring flags <1% of groups for review (indicating appropriate thresholds)
- **SC-022**: 100% of data stored on SA-domiciled infrastructure with required encryption

**12-Month Targets (Post-MVP Scale)**
- **SC-023**: 10,000 active groups with 100,000 members participating
- **SC-024**: R250 million in pooled deposits under management
- **SC-025**: 40% of groups originated via USSD
- **SC-026**: R3 million+ in interest revenue generated
- **SC-027**: NPS score exceeds 70

## Assumptions

- Members already have or are willing to open bank accounts to participate
- Groups will self-organize member recruitment; bank does not need to form groups
- Average stokvel group size is 10–15 members contributing R500–R2,000/month each
- USSD shortcode (*120*STOKVEL#) will be approved and registered with mobile network operators
- Treasury will approve tiered interest rates (3.5%/4.5%/5.5%) for group savings accounts
- Credit bureau integration (F-07) is deferred to Phase 2 (post-MVP)
- Financial wellness nudges (F-08), investment bridge (F-09), and insurance (F-10) are deferred to Phase 2+
- Bank has FSCA regulatory approval to operate group savings accounts under existing banking license
- Chairperson acquisition campaign will be led by Marketing & Distribution teams
- Non-bank members will complete simplified onboarding (app: ID + selfie; USSD: branch/ATM verification)
- Groups will follow cultural norms around quorum (typically 50-75% participation for major decisions)

## Out of Scope (Explicitly Not in MVP)

- **Credit Profile Builder (F-07)**: Opt-in credit bureau reporting based on contribution history—requires bureau partner integration (Phase 2)
- **Financial Wellness Nudges (F-08)**: Contextual education, milestone celebrations, annual summaries—requires behavioral analytics layer (Phase 2)
- **Stokvel-to-Investment Bridge (F-09)**: Group voting to move savings into unit trusts/fixed deposits—requires investment product integration (Phase 3)
- **Insurance Integration (F-10)**: Group funeral/burial society cover—requires insurance partner integration (Phase 3)
- **Premium Group Tier**: Advanced features (custom governance, investment access) for R25/group/month (Phase 2)
- **B2B API Licensing**: White-label stokvel infrastructure for credit unions, churches, employers (Phase 3)
- **Web Dashboard for Members**: MVP web interface is Chairperson-only; member web access deferred to Phase 2
- **Cross-border Remittances**: Support for diaspora contributions from outside South Africa (future consideration)
- **AI-powered Fraud Detection**: Advanced anomaly detection beyond standard AML monitoring (future enhancement)

## Dependencies

- **Core Banking System**: New account class "Group Savings Account" must be configured with tiered interest rates
- **USSD Gateway**: Shortcode registration (*120*STOKVEL#) with Vodacom, MTN, Cell C, Telkom—requires MNO partnerships
- **FICA/KYC Infrastructure**: Simplified onboarding flow for non-customers (ID verification + selfie for app; branch/ATM for USSD)
- **Payment Rails**: Integration with bank's existing EFT/instant payment systems for contributions and payouts
- **AML Monitoring**: Group account thresholds (>R20K single deposit, >R100K monthly inflows) configured in compliance system
- **Language Localization**: Translation of all UI strings, error messages, USSD menus into isiZulu, Sesotho, Xhosa, Afrikaans
- **POPIA Compliance**: Legal review of consent flows for credit bureau opt-in and marketing contact
- **FSCA Regulatory Approval**: Confirmation that group savings accounts operate within banking license scope
- **Design System**: Material Design 3 for Android, iOS Human Interface Guidelines compliance, WCAG 2.1 AA accessibility standards
- **Data Residency**: SA-domiciled cloud infrastructure (Azure South Africa Central or equivalent) for SARB compliance
