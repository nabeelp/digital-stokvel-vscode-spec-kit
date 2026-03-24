<!--
SYNC IMPACT REPORT
===================
Version: 0.0.0 → 1.0.0 (INITIAL CONSTITUTION)
Date: 2026-03-24
Rationale: Initial constitution creation based on Digital Stokvel Banking PRD

Modified Principles:
  - NEW: I. Cultural-First Design
  - NEW: II. Transparency & Trust
  - NEW: III. Financial Inclusion (NON-NEGOTIABLE)
  - NEW: IV. Security & Compliance (NON-NEGOTIABLE)
  - NEW: V. Community Governance

Added Sections:
  - Platform Requirements (multi-platform mandate)
  - Data Privacy Standards (POPIA/FICA compliance)

Templates Requiring Updates:
  ✅ plan-template.md - Constitution Check section ready for validation
  ✅ spec-template.md - User scenarios align with cultural-first approach
  ✅ tasks-template.md - Task structure supports independent user story implementation

Follow-up TODOs:
  - None - all placeholders filled based on PRD content
-->

# Digital Stokvel Banking Constitution

## Core Principles

### I. Cultural-First Design
The bank is infrastructure, not the leader. Stokvel traditions and cultural practices MUST be respected and reflected in every feature.

**Rules**:
- Group Chairperson retains authority; the bank provides infrastructure only
- Language and terminology MUST use cultural terms ('stokvel', 'umseki', 'ilungu') over clinical banking terms ('account', 'product', 'client')
- User interface tone MUST be warm and communal, never corporate or transactional
- Group governance mirrors traditional stokvel practices (quorum requirements, role assignments, dispute resolution)
- The bank MUST NOT contact group members directly about products without Chairperson consent

**Rationale**: Stokvels represent R50B+ in annual flows and 11M+ participants operating on trust and community bonds. Any product perceived as disrupting these traditions will fail. Cultural resonance is the foundation of trust and adoption.

### II. Transparency & Trust
Every member can see the full group ledger at all times. All transactions are immutable and auditable.

**Rules**:
- Full contribution history visible to all members (who paid, when, how much)
- Chairperson can view but MUST NOT unilaterally withdraw without quorum approval
- Group ledger can be exported as PDF for annual AGM records
- Contribution receipts are branded, shareable, and verifiable
- Bank logo, FSCA badge, and 'Your money is protected' disclosure MUST be visible on wallet screen
- NO hidden fees, NO surprise charges, NO opacity in interest calculations

**Rationale**: Stokvels are built on trust between members. Any perception of hidden activity or unilateral action by the bank destroys that trust. Transparency is not a feature—it is the foundation.

### III. Financial Inclusion (NON-NEGOTIABLE)
USSD is a first-class platform with feature parity for core flows. No smartphone means no exclusion.

**Rules**:
- USSD MUST support: contribute, check balance, receive payout notification, view recent transactions
- USSD menu depth MUST NOT exceed 3 levels (deep nesting causes drop-offs)
- All USSD transactions MUST be PIN-authenticated using existing bank PIN
- USSD session state MUST persist for 120 seconds to handle network interruptions
- All error messages and confirmations MUST be available in 5 languages (English, isiZulu, Sesotho, Xhosa, Afrikaans)
- 30% of MVP groups MUST originate from USSD (tracked as a success metric)

**Rationale**: 19% of South African users rely on feature phones. 33% of adults are financially excluded or underbanked. USSD is not a secondary channel—it is essential to reach the core target market. A smartphone-only product abandons the most vulnerable users.

### IV. Security & Compliance (NON-NEGOTIABLE)
POPIA, FICA, and AML compliance are mandatory. Data protection is non-negotiable.

**Rules**:
- All personal data handling MUST comply with the Protection of Personal Information Act (POPIA)
- All group members MUST be verified bank customers (FICA/KYC completed)
- Non-customers MUST complete simplified onboarding (ID + selfie) before joining; USSD users complete FICA via branch or ATM
- Group accounts MUST be subject to standard AML transaction monitoring (flag deposits >R20K or monthly inflows >R100K)
- Credit bureau reporting MUST be opt-in only with explicit, informed consent
- All data MUST be stored on South Africa-domiciled infrastructure (SARB requirement)
- Group data (contribution history, roster, ledger) is owned by the group; bank is custodian only

**Rationale**: Regulatory non-compliance risks license revocation. POPIA violations can result in fines up to R10M. More critically, any data breach or misuse destroys trust in a market where trust is the only currency that matters.

### V. Community Governance
Group rules are enforced by the group. The bank executes decisions but does not dictate them.

**Rules**:
- Group constitution builder MUST allow groups to define: missed payment penalties, late fees, member removal criteria
- Payout execution MUST require dual approval: Chairperson initiates, Treasurer confirms, system executes
- In-app voting MUST be available for major decisions (change contribution amount, remove member, adjust payout schedule)
- Missed payments trigger automated notice, grace period, then Chairperson notification—NO automatic penalties without group rules
- Dispute flag allows members to raise issues; bank mediates ONLY if group escalates

**Rationale**: Stokvels are self-governing communities. Imposing bank-defined rules violates the principle of member autonomy and will be rejected by users. The bank must be a neutral executor, not a decision-maker.

## Platform Requirements

All features MUST support multi-platform access with appropriate fidelity for each channel.

**Mandatory Platform Support**:
- **Android App**: Full feature set (F-01 to F-06 in MVP). Material Design 3, offline-tolerant architecture for low-connectivity areas.
- **iOS App**: Full feature set with feature parity. Maintain consistency with Android UX patterns where culturally appropriate.
- **USSD (*120*STOKVEL#)**: Core flows only—contribute, check balance, payout notification. Max 3-level menu depth, 120-second session timeout, fallback SMS confirmation.
- **Web Browser**: Chairperson admin dashboard (member management, contribution tracking, payout approvals). Desktop-first, responsive for tablet.

**Design Standards**:
- Multilingual UI MUST be available in 5 languages (English, isiZulu, Sesotho, Xhosa, Afrikaans)
- Language selection at onboarding, changeable in settings
- All amounts MUST be confirmed before execution (e.g., "Confirm: Pay R500 to Ntombizodwa Stokvel? Press 1 for Yes, 2 for No")
- Error messages MUST be encouraging, not alarming (e.g., "Your payment didn't go through this time—let's try again" NOT "Transaction Failed")

## Data Privacy Standards

Data ownership, consent, and portability are fundamental rights.

**Data Ownership**:
- Contribution history, member roster, and group ledger are owned by the group
- Bank is a custodian, not an owner
- Members can export their data at any time in machine-readable format (CSV, JSON)

**Consent & Control**:
- Explicit consent required for credit bureau reporting (opt-in only)
- Explicit consent required for marketing contact
- Clear explanation of what data is reported and how it affects credit profile
- Members can revoke consent at any time; data reporting ceases within 48 hours

**Data Residency & Security**:
- All data stored on South Africa-domiciled infrastructure (SARB requirement)
- Data at rest MUST be encrypted (AES-256 or equivalent)
- Data in transit MUST use TLS 1.3 or higher
- Access logs MUST be retained for 7 years (FICA requirement)

## Governance

This constitution supersedes all other practices. Amendments require documentation, stakeholder approval, and a migration plan.

**Amendment Process**:
- Proposed amendments MUST be documented in a formal proposal with rationale and impact analysis
- Amendments affecting core principles (I-V) require executive approval
- Amendments affecting platform or compliance MUST include legal/regulatory review
- All amendments MUST include a migration plan for existing features and groups
- Version MUST increment according to semantic versioning:
  - **MAJOR**: Backward-incompatible governance changes or principle removals/redefinitions
  - **MINOR**: New principle/section added or materially expanded guidance
  - **PATCH**: Clarifications, wording fixes, non-semantic refinements

**Compliance Verification**:
- All feature specifications MUST include a Constitution Check section validating alignment with principles I-V
- All implementation plans MUST document any exceptions or deviations with explicit justification
- All pull requests MUST include a statement confirming constitutional compliance or documenting approved exceptions
- Quarterly constitution review to assess if principles remain aligned with product evolution and market needs

**Complexity Justification**:
- Any feature introducing complexity (e.g., multi-step workflows, conditional logic, background processing) MUST be justified against the principles
- Simplicity is preferred unless complexity directly serves cultural design, transparency, inclusion, security, or governance

---

**Version**: 1.0.0 | **Ratified**: 2026-03-24 | **Last Amended**: 2026-03-24
