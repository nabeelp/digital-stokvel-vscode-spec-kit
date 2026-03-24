# Implementation Plan: Digital Stokvel Banking MVP

**Branch**: `001-stokvel-banking-mvp` | **Date**: 2026-03-24 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-stokvel-banking-mvp/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

The Digital Stokvel Banking MVP formalizes South Africa's R50B informal savings economy by providing group savings accounts with tiered interest (3.5%–5.5%), digital contributions via Android/iOS/USSD/Web, automated payouts with dual-approval governance, and multilingual access (5 languages). Core technical approach: C# ASP.NET Core 10.0 backend with PostgreSQL for ACID-compliant financial transactions, Kotlin/Swift native mobile apps with offline-first architecture, React TypeScript web dashboard for Chairpersons, and USSD gateway integration for feature phone users. Azure cloud infrastructure in South Africa regions ensures SARB data residency compliance.

## Technical Context

**Language/Version**: C# 13 with ASP.NET Core 10.0 (backend API)  
**Primary Dependencies**: Entity Framework Core 10.0 (ORM), ASP.NET Core Identity + JWT (authentication), Azure Service Bus SDK (async messaging), Npgsql (PostgreSQL driver)  
**Storage**: PostgreSQL 16+ (primary database), Azure Cache for Redis (session management, caching)  
**Testing**: xUnit with FluentAssertions (unit tests), Testcontainers for .NET (integration tests with PostgreSQL), SpecFlow (BDD acceptance tests)  
**Target Platform**: Multi-platform — Backend: Azure App Service (Linux), Mobile: Android 8+ (Kotlin/Jetpack Compose), iOS 15+ (Swift/SwiftUI), Web: React 18 + TypeScript (desktop-first responsive), USSD: South African MNO gateways (Vodacom, MTN, Cell C, Telkom)  
**Project Type**: Multi-platform fintech application (mobile-first with web admin dashboard and USSD financial inclusion channel)  
**Performance Goals**: API <500ms p95 response time for banking operations, offline-first mobile apps with sync latency <2s when online, USSD session completion <30s for contribution flow  
**Constraints**: POPIA/FICA compliance (explicit consent, KYC verification), SARB data residency (Azure South Africa Central region mandatory), ACID transactions for all financial operations, AML monitoring (>R20K deposits, >R100K monthly inflows), max 3-level USSD menu depth, 5-language UI support (English, isiZulu, Sesotho, Xhosa, Afrikaans)  
**Scale/Scope**: MVP targets 500 active groups, 5,000 members, R5M pooled deposits within 3 months; system must scale to 10,000 groups (100K members) within 12 months

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**✓** I. Cultural-First Design: Feature uses communal language ('your group', 'your savings pot'), Chairperson retains authority (dual-approval for payouts), no direct member contact without consent (FR-052), branded shareable receipts for group meetings (FR-049)  
**✓** II. Transparency & Trust: Full group ledger visible to all members (FR-008), immutable audit trail (FR-017), contribution receipts shareable (FR-049), no unilateral withdrawals (FR-009), ledger export for AGM records (FR-050), encouraging error messages not alarming (FR-051)  
**✓** III. Financial Inclusion: USSD supports core flows (contribute, check balance, payout notification) with max 3-level menu depth (FR-037, FR-038), 120-second session persistence (FR-038), all 5 languages supported (FR-030-034), 30% of groups must originate via USSD (SC-004)  
**✓** IV. Security & Compliance: POPIA compliance with explicit consent for credit bureau/marketing (FR-040), FICA/KYC verification required (FR-041), AML thresholds monitored (FR-042), SA-domiciled infrastructure (FR-043), AES-256 encryption at rest + TLS 1.3 in transit (FR-044), 7-year log retention (FR-045), POPIA data minimization enforced (FR-046, FR-008)  
**✓** V. Community Governance: Group constitution builder for missed payment/removal rules (FR-025), in-app voting for major decisions (FR-026), automated escalation per group rules (FR-027), dispute flags with bank mediation path (FR-028, FR-029), dual-approval for payouts (FR-021), quorum required for partial withdrawals (FR-024)  
**✓** Platform: Multi-platform mandate met—Android full feature set (FR-035), iOS feature parity (FR-036), USSD core flows (FR-037), Chairperson web dashboard (FR-039)  
**✓** Data Privacy: Group owns contribution history/roster/ledger (constitution), member data export available, explicit consent for credit bureau (FR-040), SA-domiciled storage (FR-043)

*All constitutional checks pass. No exceptions required.*

**Phase 1 Design Re-check**:
- ✓ Data model includes `MemberConsent` entity for POPIA compliance
- ✓ Contribution ledger exposes only masked account numbers (`****1234`) via REST API
- ✓ USSD flows maintain max 3-level depth (see contracts/ussd-flow.md)
- ✓ REST API uses communal terminology ("group", "contribution", "wallet") not clinical banking terms
- ✓ Payout workflow enforces dual-approval (Chairperson initiates → Treasurer confirms → System executes)
- ✓ Governance API supports in-app voting with quorum thresholds
- ✓ All 5 languages supported in USSD flows and mobile/web UIs (localization service)

**Final Verdict**: All constitutional principles upheld in Phase 1 design. Ready for Phase 2 (task breakdown).

## Project Structure

### Documentation (this feature)

```text
specs/001-stokvel-banking-mvp/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   ├── rest-api.md      # REST API contract for mobile/web clients
│   └── ussd-flow.md     # USSD menu flow documentation
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── DigitalStokvel.API/          # ASP.NET Core Web API project
│   │   ├── Controllers/             # REST endpoints (Groups, Contributions, Payouts)
│   │   ├── Middleware/              # Authentication, logging, error handling
│   │   └── Program.cs               # App configuration, dependency injection
│   ├── DigitalStokvel.Core/         # Domain models and interfaces
│   │   ├── Entities/                # Group, Member, Contribution, Payout, GovernanceRule, Dispute
│   │   ├── Interfaces/              # Repository and service contracts
│   │   └── ValueObjects/            # Money, InterestRate, ContributionSchedule
│   ├── DigitalStokvel.Infrastructure/ # Data access and external integrations
│   │   ├── Data/                    # EF Core DbContext, migrations, repositories
│   │   ├── Identity/                # ASP.NET Core Identity configuration
│   │   ├── Messaging/               # Azure Service Bus integration
│   │   ├── USSD/                    # MNO gateway integration (Vodacom, MTN, Cell C, Telkom)
│   │   └── Compliance/              # AML monitoring, POPIA audit logging
│   └── DigitalStokvel.Services/     # Business logic layer
│       ├── GroupService.cs          # Group creation, member management, constitution
│       ├── ContributionService.cs   # Payment processing, debit orders, confirmations
│       ├── PayoutService.cs         # Payout execution, dual-approval workflow
│       ├── InterestService.cs       # Daily compounding, monthly capitalization
│       ├── GovernanceService.cs     # Voting, dispute handling, rule enforcement
│       └── LocalizationService.cs   # 5-language translation (EN, ZU, ST, XH, AF)
└── tests/
    ├── DigitalStokvel.Tests.Unit/       # xUnit unit tests with FluentAssertions
    ├── DigitalStokvel.Tests.Integration/ # Testcontainers with PostgreSQL
    └── DigitalStokvel.Tests.Acceptance/  # SpecFlow BDD scenarios from spec.md

android/
├── app/
│   ├── src/
│   │   ├── main/
│   │   │   ├── java/za/co/stokvel/  # Kotlin source
│   │   │   │   ├── ui/              # Jetpack Compose screens (Groups, Contributions, Wallet)
│   │   │   │   ├── viewmodels/      # MVVM ViewModels
│   │   │   │   ├── data/            # Repository pattern, Room DB for offline
│   │   │   │   ├── network/         # Retrofit API client
│   │   │   │   └── sync/            # WorkManager for offline sync
│   │   │   └── res/                 # Material Design 3 resources, 5 language strings
│   │   └── androidTest/             # Espresso UI tests
│   └── build.gradle.kts             # Gradle Kotlin DSL build script
└── README.md                        # Android app setup guide

ios/
├── DigitalStokvel/
│   ├── Features/
│   │   ├── Groups/                  # SwiftUI views for group management
│   │   ├── Contributions/           # SwiftUI views for payment flows
│   │   ├── Wallet/                  # SwiftUI views for group wallet/ledger
│   │   └── Governance/              # SwiftUI views for voting/disputes
│   ├── Services/
│   │   ├── NetworkService.swift     # URLSession API client
│   │   ├── StorageService.swift     # CoreData for offline persistence
│   │   └── SyncService.swift        # Background sync coordination
│   ├── Models/                      # Swift data models (Group, Member, Contribution)
│   └── Resources/                   # Localization strings (5 languages)
├── DigitalStokvelfTests/            # XCTest unit tests
└── DigitalStokvelfUITests/          # XCUITest acceptance tests

web/
├── src/
│   ├── components/                  # React functional components
│   │   ├── Dashboard/               # Chairperson dashboard widgets
│   │   ├── Members/                 # Member roster management
│   │   ├── Contributions/           # Contribution tracking UI
│   │   └── Payouts/                 # Payout approval interface
│   ├── pages/                       # Next.js pages (if using Next) or React Router routes
│   ├── services/                    # API client (Axios), state management (React Query)
│   ├── hooks/                       # Custom React hooks
│   └── locales/                     # i18next translation files (5 languages)
└── tests/
    ├── unit/                        # Jest + React Testing Library
    └── e2e/                         # Playwright end-to-end tests

infrastructure/
├── bicep/                           # Azure infrastructure as code
│   ├── main.bicep                   # Root deployment (App Service, PostgreSQL, Redis, Service Bus)
│   ├── modules/                     # Reusable modules (networking, identity, monitoring)
│   └── parameters.json              # Environment-specific config (South Africa Central region)
└── scripts/                         # Deployment automation (CI/CD helpers)
```

**Structure Decision**: Multi-platform fintech architecture with backend API, native mobile apps (Android/iOS), web dashboard (Chairperson-only), and USSD gateway integration. Backend follows Clean Architecture (Core → Services → Infrastructure → API) for maintainability and testability. Mobile apps use MVVM with offline-first architecture (Room/CoreData) to handle low-connectivity scenarios common in target market. Web app is React TypeScript for component-based UI with strong typing.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

*No constitutional violations detected. All complexity is justified by domain requirements (multi-platform fintech, ACID transactions, regulatory compliance, offline-first mobile, USSD inclusion).*
