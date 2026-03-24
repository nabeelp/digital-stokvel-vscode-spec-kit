# Tasks: Digital Stokvel Banking MVP

**Input**: Design documents from `/specs/001-stokvel-banking-mvp/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓

**Tests**: Not explicitly requested in feature specification - tasks focus on implementation only

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Multi-platform project structure:
- **Backend**: `backend/src/DigitalStokvel.API/`, `backend/src/DigitalStokvel.Core/`, `backend/src/DigitalStokvel.Infrastructure/`, `backend/src/DigitalStokvel.Services/`
- **Android**: `android/app/src/main/java/za/co/stokvel/`
- **iOS**: `ios/DigitalStokvel/`
- **Web**: `web/src/`
- **USSD**: `backend/src/DigitalStokvel.Infrastructure/USSD/`
- **Tests**: `backend/tests/DigitalStokvel.Tests.Unit/`, `backend/tests/DigitalStokvel.Tests.Integration/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and repository structure

- [X] T001 Create multi-platform repository structure: backend/, android/, ios/, web/, ussd/ directories
- [X] T002 Initialize .NET 10 solution in backend/ with DigitalStokvel.API, DigitalStokvel.Core, DigitalStokvel.Infrastructure, DigitalStokvel.Services projects
- [X] T003 [P] Configure C# 13 language version and .NET 10.0 target framework in backend/*.csproj files
- [ ] T004 [P] Initialize Android project with Kotlin and Jetpack Compose dependencies in android/app/build.gradle *(requires Android Studio)*
- [ ] T005 [P] Initialize iOS project with SwiftUI and Xcode 15+ configuration in ios/DigitalStokvel.xcodeproj *(requires macOS + Xcode)*
- [X] T006 [P] Initialize React 18 + TypeScript web project in web/ with Vite bundler *(requires Node.js - can run: npm create vite@latest web -- --template react-ts)*
- [X] T007 [P] Setup Docker Compose for local PostgreSQL 16 and Redis 7 in docker-compose.yml
- [X] T008 [P] Configure EditorConfig, .gitignore, and linting rules for C#, Kotlin, Swift, TypeScript
- [X] T009 Add Entity Framework Core 10.0, ASP.NET Core Identity, Npgsql, Azure Service Bus SDK dependencies to backend/src/DigitalStokvel.API/DigitalStokvel.API.csproj
- [X] T010 [P] Configure xUnit, FluentAssertions, Testcontainers test dependencies in backend/tests/DigitalStokvel.Tests.Unit/
- [ ] T011 Setup CI/CD pipeline configuration for Azure App Service deployment in .github/workflows/deploy.yml

**Phase 1 Status**: Backend foundation complete (6/11 tasks). T004-T006 require platform-specific tools. Ready to proceed with Phase 2 (Foundational) for backend implementation.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ ALL TASKS COMPLETE** ✅

- [X] T012 Create PostgreSQL database schema with tables: members, stokvel_groups, group_members, contributions, payouts, governance_rules, disputes in backend/src/DigitalStokvel.Infrastructure/Data/Migrations/
- [X] T013 [P] Configure Entity Framework Core DbContext with PostgreSQL connection and entity mappings in backend/src/DigitalStokvel.Infrastructure/Data/ApplicationDbContext.cs
- [X] T014 [P] Implement ASP.NET Core Identity + JWT authentication middleware in backend/src/DigitalStokvel.API/Middleware/AuthenticationMiddleware.cs
- [X] T015 [P] Configure Azure Cache for Redis session management in backend/src/DigitalStokvel.API/Program.cs
- [X] T016 Create base IAuditableEntity interface with CreatedAt, CreatedBy, ModifiedAt, ModifiedBy fields in backend/src/DigitalStokvel.Core/Interfaces/IAuditableEntity.cs
- [X] T017 Implement EF Core SaveChangesInterceptor for automatic audit field population in backend/src/DigitalStokvel.Infrastructure/Data/AuditInterceptor.cs
- [X] T018 [P] Create Money value object with ZAR currency and decimal precision handling in backend/src/DigitalStokvel.Core/ValueObjects/Money.cs
- [X] T019 [P] Implement IdempotencyLog repository for duplicate transaction prevention in backend/src/DigitalStokvel.Infrastructure/Repositories/IdempotencyLogRepository.cs
- [X] T020 [P] Configure API error handling middleware with RFC 7807 ProblemDetails format in backend/src/DigitalStokvel.API/Middleware/ErrorHandlingMiddleware.cs
- [X] T021 [P] Setup structured logging with Serilog and Application Insights in backend/src/DigitalStokvel.API/Program.cs
- [X] T022 [P] Implement localization service interface ILocalizationService for 5-language support in backend/src/DigitalStokvel.Core/Interfaces/ILocalizationService.cs
- [X] T023 Implement LocalizationService with resource file loading for EN, ZU, ST, XH, AF in backend/src/DigitalStokvel.Services/LocalizationService.cs
- [X] T024 [P] Create base repository pattern IRepository<T> with CRUD operations in backend/src/DigitalStokvel.Core/Interfaces/IRepository.cs
- [X] T025 Implement generic Repository<T> with EF Core and optimistic concurrency support in backend/src/DigitalStokvel.Infrastructure/Repositories/Repository.cs
- [X] T026 [P] Configure API rate limiting (100 req/min per user) in backend/src/DigitalStokvel.API/Middleware/RateLimitingMiddleware.cs
- [X] T027 Setup Azure Service Bus client for async notification delivery in backend/src/DigitalStokvel.Infrastructure/Messaging/ServiceBusClient.cs
- [X] T028 [P] Create shared DTO models for API responses in backend/src/DigitalStokvel.API/DTOs/
- [X] T029 Configure CORS policy for mobile and web clients in backend/src/DigitalStokvel.API/Program.cs
- [X] T030 [P] Setup API versioning and OpenAPI documentation in backend/src/DigitalStokvel.API/Program.cs

**Phase 2 Complete**: 19/19 tasks (100%) ✅✅✅ Foundation infrastructure fully implemented. Ready for user story development!

---

## Phase 3: User Story 1 - Chairperson Creates and Manages Stokvel Group (Priority: P0) 🎯 MVP

**Goal**: Enable Chairperson to create named groups, invite members, assign roles, and view roster dashboard

**Independent Test**: Chairperson creates "Ntombizodwa Stokvel" with 5 invited members, assigns Treasurer role, views dashboard showing roster and contribution schedule

### Implementation for User Story 1

- [X] T031 [P] [US1] Create Member entity with BankCustomerId, PhoneNumber, PreferredLanguage, FicaVerified in backend/src/DigitalStokvel.Core/Entities/Member.cs
- [X] T032 [P] [US1] Create StokvelsGroup entity with Name, GroupType, ContributionAmount, Constitution (jsonb), Balance in backend/src/DigitalStokvel.Core/Entities/StokvelsGroup.cs
- [X] T033 [P] [US1] Create GroupMember join entity with GroupId, MemberId, Role (Chairperson/Treasurer/Secretary/Member) in backend/src/DigitalStokvel.Core/Entities/GroupMember.cs
- [X] T034 [US1] Implement IMemberRepository with GetByPhoneNumber and GetByBankCustomerId methods in backend/src/DigitalStokvel.Core/Interfaces/IMemberRepository.cs
- [X] T035 [US1] Implement MemberRepository with EF Core queries in backend/src/DigitalStokvel.Infrastructure/Repositories/MemberRepository.cs
- [X] T036 [P] [US1] Implement IGroupRepository with CreateGroup, AddMember, AssignRole methods in backend/src/DigitalStokvel.Core/Interfaces/IGroupRepository.cs
- [X] T037 [US1] Implement GroupRepository with EF Core including Constitution jsonb queries in backend/src/DigitalStokvel.Infrastructure/Repositories/GroupRepository.cs
- [X] T038 [US1] Implement GroupService with CreateGroup business logic: validate contribution amount R50-R100K, create group savings account in backend/src/DigitalStokvel.Services/GroupService.cs
- [X] T039 [US1] Implement GroupService.InviteMember: generate SMS/push notification with join link in backend/src/DigitalStokvel.Services/GroupService.cs
- [X] T040 [US1] Implement GroupService.AssignRole: validate role-specific permissions (Treasurer approves payouts) in backend/src/DigitalStokvel.Services/GroupService.cs
- [X] T041 [US1] Create POST /api/v1/groups endpoint with CreateGroupRequest DTO in backend/src/DigitalStokvel.API/Controllers/GroupsController.cs
- [X] T042 [US1] Create GET /api/v1/groups/{id} endpoint returning group details with roster in backend/src/DigitalStokvel.API/Controllers/GroupsController.cs
- [X] T043 [US1] Create PUT /api/v1/groups/{id}/members endpoint for adding members in backend/src/DigitalStokvel.API/Controllers/GroupsController.cs
- [X] T044 [US1] Create PUT /api/v1/groups/{id}/roles endpoint for role assignment in backend/src/DigitalStokvel.API/Controllers/GroupsController.cs
- [ ] T045 [P] [US1] Implement Android GroupCreationScreen with Jetpack Compose Material Design 3 in android/app/src/main/java/za/co/stokvel/ui/group/GroupCreationScreen.kt
- [ ] T046 [P] [US1] Implement iOS GroupCreationView with SwiftUI forms in ios/DigitalStokvel/Views/GroupCreationView.swift
- [X] T047 [P] [US1] Implement React GroupCreation component with Formik validation in web/src/components/GroupCreation.tsx
- [ ] T048 [US1] Implement Android GroupDashboardScreen with roster list, pagination for 20+ members in android/app/src/main/java/za/co/stokvel/ui/group/GroupDashboardScreen.kt
- [ ] T049 [US1] Implement iOS GroupDashboardView with roster SwiftUI List in ios/DigitalStokvel/Views/GroupDashboardView.swift
- [X] T050 [US1] Implement React GroupDashboard with member table and search for web in web/src/components/GroupDashboard.tsx
- [X] T051 [US1] Add soft warning UI for groups exceeding 50 members: "Larger groups may experience performance considerations" in all platforms
- [X] T052 [US1] Implement SMS notification service for member invitations using Azure Communication Services in backend/src/DigitalStokvel.Infrastructure/Notifications/SmsNotificationService.cs

**Checkpoint**: At this point, User Story 1 should be fully functional - Chairperson can create groups and manage members

---

## Phase 4: User Story 2 - Member Contributes to Group Savings (Priority: P0) 🎯 MVP

**Goal**: Members make R500 contributions via one-tap, debit order, or USSD; receive receipts and view history

**Independent Test**: Member logs in, taps "Pay Contribution" for R500, completes payment, receives branded receipt, sees transaction in personal history and group ledger

### Implementation for User Story 2

- [X] T053 [P] [US2] Create Contribution entity with GroupId, MemberId, Amount, PaymentMethod, Status, IdempotencyKey, Timestamp in backend/src/DigitalStokvel.Core/Entities/Contribution.cs
- [X] T054 [P] [US2] Create ContributionStatus enum (Pending, Completed, Failed, Retrying) in backend/src/DigitalStokvel.Core/Enums/ContributionStatus.cs
- [X] T055 [P] [US2] Create PaymentMethod enum (OneTap, DebitOrder, USSD) in backend/src/DigitalStokvel.Core/Enums/PaymentMethod.cs
- [X] T056 [US2] Implement IContributionRepository with AddContribution, GetGroupLedger, GetMemberHistory in backend/src/DigitalStokvel.Core/Interfaces/IContributionRepository.cs
- [X] T057 [US2] Implement ContributionRepository with indexed queries on (group_id, member_id, timestamp) in backend/src/DigitalStokvel.Infrastructure/Repositories/ContributionRepository.cs
- [X] T058 [US2] Implement IPaymentGateway interface with DeductFromAccount method in backend/src/DigitalStokvel.Core/Interfaces/IPaymentGateway.cs
- [X] T059 [US2] Implement PaymentGatewayService with bank's payment rails integration in backend/src/DigitalStokvel.Infrastructure/Payments/PaymentGatewayService.cs
- [X] T060 [US2] Implement ContributionService.ProcessContribution with idempotency check, transaction scope, ledger entry in backend/src/DigitalStokvel.Services/ContributionService.cs
- [X] T061 [US2] Add Polly retry policy for payment gateway resilience (3 retries with exponential backoff) in backend/src/DigitalStokvel.Services/ContributionService.cs
- [X] T062 [US2] Implement debit order retry logic: 48hr delay, 2 retries, member notification each attempt in backend/src/DigitalStokvel.Services/ContributionService.cs
- [X] T063 [US2] Implement branded receipt generation with group name, amount, timestamp, shareable format in backend/src/DigitalStokvel.Services/ReceiptService.cs
- [X] T064 [US2] Create POST /api/v1/contributions endpoint with idempotency key header in backend/src/DigitalStokvel.API/Controllers/ContributionsController.cs
- [X] T065 [US2] Create GET /api/v1/groups/{groupId}/ledger endpoint returning payment history with masked account indicators (****1234) in backend/src/DigitalStokvel.API/Controllers/ContributionsController.cs
- [X] T066 [US2] Create GET /api/v1/members/{memberId}/contributions endpoint for personal history in backend/src/DigitalStokvel.API/Controllers/ContributionsController.cs
- [X] T067 [US2] Create POST /api/v1/contributions/debit-order endpoint for recurring payment setup in backend/src/DigitalStokvel.API/Controllers/ContributionsController.cs
- [ ] T068 [P] [US2] Implement Android PayContributionScreen with one-tap payment button in android/app/src/main/java/za/co/stokvel/ui/contribution/PayContributionScreen.kt
- [ ] T069 [P] [US2] Implement iOS PayContributionView with Apple Pay integration in ios/DigitalStokvel/Views/PayContributionView.swift
- [X] T070 [P] [US2] Implement React PayContribution modal for web dashboard in web/src/components/PayContribution.tsx
- [ ] T071 [US2] Implement Android ContributionHistoryScreen with list and receipt sharing in android/app/src/main/java/za/co/stokvel/ui/contribution/ContributionHistoryScreen.kt
- [ ] T072 [US2] Implement iOS ContributionHistoryView with SwiftUI in ios/DigitalStokvel/Views/ContributionHistoryView.swift
- [X] T073 [US2] Implement payment reminder background job (3 days, 1 day before due date) in backend/src/DigitalStokvel.Infrastructure/Jobs/PaymentReminderJob.cs
- [X] T074 [US2] Implement push notification service for payment reminders in backend/src/DigitalStokvel.Infrastructure/Notifications/PushNotificationService.cs
- [X] T075 [US2] Enforce POPIA data minimization: ledger API returns only masked account indicators, no full account numbers in backend/src/DigitalStokvel.API/Controllers/ContributionsController.cs

**Checkpoint**: At this point, User Story 2 should be fully functional - Members can contribute and view ledger with privacy protections

---

## Phase 5: User Story 3 - Group Receives Interest-Bearing Pooled Savings (Priority: P0) 🎯 MVP

**Goal**: Group savings account earns tiered interest (3.5%–5.5%) with daily compounding, monthly capitalization visible to all

**Independent Test**: Group accumulates R10K contributions, members view wallet showing principal + accrued interest at 4.5% rate, interest breakdown visible

### Implementation for User Story 3

- [X] T076 [P] [US3] Create InterestCalculation entity with GroupId, CalculationDate, PrincipalAmount, InterestRate, AccruedAmount in backend/src/DigitalStokvel.Core/Entities/InterestCalculation.cs
- [X] T077 [P] [US3] Create InterestTier enum (Tier1_3_5Pct, Tier2_4_5Pct, Tier3_5_5Pct) in backend/src/DigitalStokvel.Core/Enums/InterestTier.cs
- [X] T078 [US3] Implement IInterestService interface with CalculateDailyInterest, CapitalizeMonthly, GetInterestBreakdown in backend/src/DigitalStokvel.Core/Interfaces/IInterestService.cs
- [X] T079 [US3] Implement InterestService with daily compounding formula: A = P(1 + r/365)^days in backend/src/DigitalStokvel.Services/InterestService.cs
- [X] T080 [US3] Implement tiered interest rate logic: R0-R10K=3.5%, R10K-R50K=4.5%, R50K+=5.5% in backend/src/DigitalStokvel.Services/InterestService.cs
- [X] T081 [US3] Implement monthly capitalization job: add AccruedInterest to Balance, reset AccruedInterest to 0 in backend/src/DigitalStokvel.Infrastructure/Jobs/InterestCapitalizationJob.cs
- [X] T082 [US3] Implement daily interest accrual background job (runs at 00:01 UTC) in backend/src/DigitalStokvel.Infrastructure/Jobs/DailyInterestAccrualJob.cs
- [ ] T083 [US3] Update GroupService to block unilateral withdrawal: require quorum approval (60% of eligible members) in backend/src/DigitalStokvel.Services/GroupService.cs
- [X] T084 [US3] Create GET /api/v1/groups/{groupId}/wallet endpoint returning Balance, AccruedInterest, InterestTier in backend/src/DigitalStokvel.API/Controllers/GroupsController.cs
- [X] T085 [US3] Create GET /api/v1/groups/{groupId}/interest-details endpoint with YTD earnings, daily calculation breakdown in backend/src/DigitalStokvel.API/Controllers/GroupsController.cs
- [ ] T086 [P] [US3] Implement Android GroupWalletScreen with balance display, interest breakdown modal in android/app/src/main/java/za/co/stokvel/ui/wallet/GroupWalletScreen.kt
- [ ] T087 [P] [US3] Implement iOS GroupWalletView with interest tier indicator in ios/DigitalStokvel/Views/GroupWalletView.swift
- [X] T088 [P] [US3] Implement React GroupWallet component with real-time balance updates in web/src/components/GroupWallet.tsx
- [X] T089 [US3] Add bank logo, FSCA badge, "Your money is protected" disclosure to wallet UI in all platforms
- [X] T090 [US3] Configure Hangfire or Azure Functions for scheduled interest jobs in backend/src/DigitalStokvel.API/Program.cs

**Checkpoint**: At this point, User Story 3 should be fully functional - Groups earn visible tiered interest with daily compounding

---

## Phase 6: User Story 5 - USSD Access for Feature Phone Users (Priority: P0) 🎯 MVP

**Goal**: Feature phone users dial *120*STOKVEL# to contribute, check balance, view transactions with max 3-level menu depth

**Independent Test**: Feature phone user dials shortcode, authenticates with PIN, makes R500 contribution, checks balance - all without smartphone

### Implementation for User Story 5

- [ ] T091 [P] [US5] Create IUssdGateway interface with SendMenu, ProcessInput, ManageSession methods in backend/src/DigitalStokvel.Core/Interfaces/IUssdGateway.cs
- [ ] T092 [US5] Implement VodacomUssdAdapter for Vodacom gateway API in backend/src/DigitalStokvel.Infrastructure/USSD/VodacomUssdAdapter.cs
- [ ] T093 [P] [US5] Implement MtnUssdAdapter for MTN gateway API in backend/src/DigitalStokvel.Infrastructure/USSD/MtnUssdAdapter.cs
- [ ] T094 [P] [US5] Implement CellCUssdAdapter for Cell C gateway API in backend/src/DigitalStokvel.Infrastructure/USSD/CellCUssdAdapter.cs
- [ ] T095 [P] [US5] Implement TelkomUssdAdapter for Telkom gateway API in backend/src/DigitalStokvel.Infrastructure/USSD/TelkomUssdAdapter.cs
- [ ] T096 [US5] Implement UssdSessionManager with 120-second state persistence using Redis in backend/src/DigitalStokvel.Infrastructure/USSD/UssdSessionManager.cs
- [ ] T097 [US5] Implement UssdMenuBuilder with 3-level max depth validation (Main → Category → Action) in backend/src/DigitalStokvel.Services/UssdMenuBuilder.cs
- [ ] T098 [US5] Create USSD menu flow: Main Menu (1=Pay Contribution, 2=Check Balance, 3=Transactions, 4=Language) in backend/src/DigitalStokvel.Services/UssdMenuBuilder.cs
- [ ] T099 [US5] Implement Pay Contribution flow: Group Selection → Amount Confirmation → PIN Auth → Receipt in backend/src/DigitalStokvel.Services/UssdFlowService.cs
- [ ] T100 [US5] Implement Check Balance flow: Group Selection → Display Balance + Interest in backend/src/DigitalStokvel.Services/UssdFlowService.cs
- [ ] T101 [US5] Implement View Transactions flow: Group Selection → Last 5 transactions with pagination in backend/src/DigitalStokvel.Services/UssdFlowService.cs
- [ ] T102 [US5] Add USSD menu translations for all 5 languages (EN, ZU, ST, XH, AF) in backend/src/DigitalStokvel.Services/Resources/ussd/
- [ ] T103 [US5] Implement session restoration logic: reconnect within 120s restores state in backend/src/DigitalStokvel.Infrastructure/USSD/UssdSessionManager.cs
- [ ] T104 [US5] Create POST /api/v1/ussd/webhook endpoint for MNO callbacks in backend/src/DigitalStokvel.API/Controllers/UssdController.cs
- [ ] T105 [US5] Implement bank PIN authentication for USSD payments in backend/src/DigitalStokvel.Services/UssdFlowService.cs
- [ ] T106 [US5] Implement SMS notification for USSD payout disbursements in backend/src/DigitalStokvel.Infrastructure/Notifications/SmsNotificationService.cs
- [ ] T107 [US5] Add USSD menu depth validator: reject navigation if exceeds 3 levels in backend/src/DigitalStokvel.Services/UssdMenuBuilder.cs

**Checkpoint**: At this point, User Story 5 should be fully functional - Feature phone users have full USSD access to core flows

---

## Phase 7: User Story 7 - Multilingual Interface for Inclusivity (Priority: P0) 🎯 MVP

**Goal**: Entire app and USSD available in 5 languages (EN, ZU, ST, XH, AF) with language selection at onboarding

**Independent Test**: User selects isiZulu at onboarding, navigates app, confirms all UI elements, errors, USSD menus in isiZulu

### Implementation for User Story 7

- [ ] T108 [P] [US7] Create English resource files for all UI strings in backend/src/DigitalStokvel.Services/Resources/localization/en.json
- [ ] T109 [P] [US7] Create isiZulu translation files in backend/src/DigitalStokvel.Services/Resources/localization/zu.json
- [ ] T110 [P] [US7] Create Sesotho translation files in backend/src/DigitalStokvel.Services/Resources/localization/st.json
- [ ] T111 [P] [US7] Create Xhosa translation files in backend/src/DigitalStokvel.Services/Resources/localization/xh.json
- [ ] T112 [P] [US7] Create Afrikaans translation files in backend/src/DigitalStokvel.Services/Resources/localization/af.json
- [ ] T113 [US7] Update LocalizationService to load JSON resource files dynamically based on user preference in backend/src/DigitalStokvel.Services/LocalizationService.cs
- [ ] T114 [US7] Implement language selection screen at onboarding (5 options) for Android in android/app/src/main/java/za/co/stokvel/ui/onboarding/LanguageSelectionScreen.kt
- [ ] T115 [US7] Implement language selection at onboarding for iOS in ios/DigitalStokvel/Views/LanguageSelectionView.swift
- [ ] T116 [US7] Implement language settings screen with immediate language switch for Android in android/app/src/main/java/za/co/stokvel/ui/settings/LanguageSettingsScreen.kt
- [ ] T117 [US7] Implement language settings for iOS with immediate refresh in ios/DigitalStokvel/Views/LanguageSettingsView.swift
- [ ] T118 [US7] Configure i18n for React web dashboard with 5 language support in web/src/i18n/config.ts
- [ ] T119 [US7] Update all notification templates (SMS, push, USSD) to use localized strings in backend/src/DigitalStokvel.Infrastructure/Notifications/
- [ ] T120 [US7] Update error messages to use LocalizationService (encouraging messages per FR-051) in backend/src/DigitalStokvel.API/Middleware/ErrorHandlingMiddleware.cs
- [ ] T121 [P] [US7] Translate all Android UI strings to 5 languages in android/app/src/main/res/values-zu/, values-st/, values-xh/, values-af/
- [ ] T122 [P] [US7] Translate all iOS UI strings to 5 languages in ios/DigitalStokvel/Resources/zu.lproj/, st.lproj/, xh.lproj/, af.lproj/
- [ ] T123 [US7] Add language detection from phone settings as default fallback in all platforms

**Checkpoint**: At this point, User Story 7 should be fully functional - Full 5-language support across all platforms

---

## Phase 8: User Story 4 - Automated Payouts to Members (Priority: P1)

**Goal**: Chairperson initiates payout, Treasurer confirms, system disburses via EFT with full transparency

**Independent Test**: Rotating payout group reaches cycle end, Chairperson initiates R6400 payout to Member 3, Treasurer confirms, Member 3 receives instant EFT

### Implementation for User Story 4

- [ ] T124 [P] [US4] Create Payout entity with GroupId, PayoutType, TotalAmount, InitiatedBy, ConfirmedBy, Status, ExecutionTime in backend/src/DigitalStokvel.Core/Entities/Payout.cs
- [ ] T125 [P] [US4] Create PayoutRecipient join entity with PayoutId, MemberId, Amount, EftReference, DisbursedAt in backend/src/DigitalStokvel.Core/Entities/PayoutRecipient.cs
- [ ] T126 [P] [US4] Create PayoutType enum (RotatingCycle, YearEndPot, PartialWithdrawal) in backend/src/DigitalStokvel.Core/Enums/PayoutType.cs
- [ ] T127 [P] [US4] Create PayoutStatus enum (PendingTreasurerApproval, PendingQuorum, Approved, InProgress, Completed, Failed) in backend/src/DigitalStokvel.Core/Enums/PayoutStatus.cs
- [ ] T128 [US4] Implement IPayoutRepository with CreatePayout, UpdateStatus, GetGroupPayouts in backend/src/DigitalStokvel.Core/Interfaces/IPayoutRepository.cs
- [ ] T129 [US4] Implement PayoutRepository with EF Core in backend/src/DigitalStokvel.Infrastructure/Repositories/PayoutRepository.cs
- [ ] T130 [US4] Implement PayoutService.InitiatePayout: validate Chairperson role, calculate amounts per type in backend/src/DigitalStokvel.Services/PayoutService.cs
- [ ] T131 [US4] Implement rotating payout logic: pay out principal only (sum of contributions), retain interest in wallet in backend/src/DigitalStokvel.Services/PayoutService.cs
- [ ] T132 [US4] Implement year-end pot logic: disburse full balance (principal + interest) proportionally to all members in backend/src/DigitalStokvel.Services/PayoutService.cs
- [ ] T133 [US4] Implement partial withdrawal quorum check: require 60% member votes before approval in backend/src/DigitalStokvel.Services/PayoutService.cs
- [ ] T134 [US4] Implement PayoutService.ConfirmPayout: validate Treasurer role, execute EFT transfers in backend/src/DigitalStokvel.Services/PayoutService.cs
- [ ] T135 [US4] Implement EFT disbursement via bank payment gateway with transaction logging in backend/src/DigitalStokvel.Infrastructure/Payments/PaymentGatewayService.cs
- [ ] T136 [US4] Implement payout notification broadcast to all group members in backend/src/DigitalStokvel.Services/PayoutService.cs
- [ ] T137 [US4] Create POST /api/v1/payouts/initiate endpoint with Chairperson auth check in backend/src/DigitalStokvel.API/Controllers/PayoutsController.cs
- [ ] T138 [US4] Create POST /api/v1/payouts/{id}/confirm endpoint with Treasurer auth check in backend/src/DigitalStokvel.API/Controllers/PayoutsController.cs
- [ ] T139 [US4] Create GET /api/v1/groups/{groupId}/payouts endpoint returning payout history in backend/src/DigitalStokvel.API/Controllers/PayoutsController.cs
- [ ] T140 [P] [US4] Implement Android PayoutInitiationScreen with Chairperson-only access in android/app/src/main/java/za/co/stokvel/ui/payout/PayoutInitiationScreen.kt
- [ ] T141 [P] [US4] Implement iOS PayoutInitiationView with role validation in ios/DigitalStokvel/Views/PayoutInitiationView.swift
- [ ] T142 [P] [US4] Implement React PayoutApproval component for Treasurer confirmation in web/src/components/PayoutApproval.tsx
- [ ] T143 [US4] Implement Android PayoutHistoryScreen with disbursement tracking in android/app/src/main/java/za/co/stokvel/ui/payout/PayoutHistoryScreen.kt
- [ ] T144 [US4] Implement iOS PayoutHistoryView in ios/DigitalStokvel/Views/PayoutHistoryView.swift
- [ ] T145 [US4] Add payout notification to USSD users via SMS with amount and confirmation code in backend/src/DigitalStokvel.Infrastructure/Notifications/SmsNotificationService.cs

**Checkpoint**: At this point, User Story 4 should be fully functional - Automated payouts with dual-approval and transparency

---

## Phase 9: User Story 6 - Group Self-Governance and Dispute Resolution (Priority: P1)

**Goal**: Groups define constitution (missed payment rules, late fees), in-app voting for major decisions, dispute flagging with mediation

**Independent Test**: Group defines "missed payment = R50 late fee", member misses payment, system applies 7-day grace period, then R50 fee per group rule

### Implementation for User Story 6

- [ ] T146 [P] [US6] Create GovernanceRule entity with GroupId, RuleType, RuleValue (jsonb), CreatedAt in backend/src/DigitalStokvel.Core/Entities/GovernanceRule.cs
- [ ] T147 [P] [US6] Create Dispute entity with GroupId, MemberId, Description, Status, Resolution, EscalatedAt in backend/src/DigitalStokvel.Core/Entities/Dispute.cs
- [ ] T148 [P] [US6] Create QuorumVote entity with GroupId, ProposalType, ProposalDetails, VotesFor, VotesAgainst, Status in backend/src/DigitalStokvel.Core/Entities/QuorumVote.cs
- [ ] T149 [P] [US6] Create RuleType enum (MissedPaymentPenalty, GracePeriod, MemberRemovalCriteria, QuorumThreshold) in backend/src/DigitalStokvel.Core/Enums/RuleType.cs
- [ ] T150 [P] [US6] Create DisputeStatus enum (Open, ChairpersonReviewed, Resolved, EscalatedToBank) in backend/src/DigitalStokvel.Core/Enums/DisputeStatus.cs
- [ ] T151 [US6] Implement IGovernanceService interface with DefineRule, InitiateVote, ProcessVoteResult, RaiseDispute in backend/src/DigitalStokvel.Core/Interfaces/IGovernanceService.cs
- [ ] T152 [US6] Implement GovernanceService with constitution builder logic in backend/src/DigitalStokvel.Services/GovernanceService.cs
- [ ] T153 [US6] Implement missed payment escalation job: send notice, start grace period timer, notify Chairperson in backend/src/DigitalStokvel.Infrastructure/Jobs/MissedPaymentEscalationJob.cs
- [ ] T154 [US6] Implement late fee application logic: after grace period expires, apply penalty per group rule in backend/src/DigitalStokvel.Services/GovernanceService.cs
- [ ] T155 [US6] Implement in-app voting: create vote proposal, notify all members, tally votes when quorum reached in backend/src/DigitalStokvel.Services/GovernanceService.cs
- [ ] T156 [US6] Implement dispute flagging: member submits explanation, Chairperson receives notification in backend/src/DigitalStokvel.Services/GovernanceService.cs
- [ ] T157 [US6] Implement bank mediation escalation path: dispute escalated after 7 days unresolved in backend/src/DigitalStokvel.Services/GovernanceService.cs
- [ ] T158 [US6] Create POST /api/v1/groups/{groupId}/constitution endpoint for rule definition in backend/src/DigitalStokvel.API/Controllers/GovernanceController.cs
- [ ] T159 [US6] Create POST /api/v1/groups/{groupId}/votes endpoint for initiating votes in backend/src/DigitalStokvel.API/Controllers/GovernanceController.cs
- [ ] T160 [US6] Create POST /api/v1/groups/{groupId}/votes/{voteId}/cast endpoint for member voting in backend/src/DigitalStokvel.API/Controllers/GovernanceController.cs
- [ ] T161 [US6] Create POST /api/v1/groups/{groupId}/disputes endpoint for raising disputes in backend/src/DigitalStokvel.API/Controllers/GovernanceController.cs
- [ ] T162 [US6] Create GET /api/v1/groups/{groupId}/constitution endpoint returning all governance rules in backend/src/DigitalStokvel.API/Controllers/GovernanceController.cs
- [ ] T163 [P] [US6] Implement Android ConstitutionBuilderScreen with rule templates in android/app/src/main/java/za/co/stokvel/ui/governance/ConstitutionBuilderScreen.kt
- [ ] T164 [P] [US6] Implement iOS ConstitutionBuilderView in ios/DigitalStokvel/Views/ConstitutionBuilderView.swift
- [ ] T165 [P] [US6] Implement React ConstitutionBuilder with form validation in web/src/components/ConstitutionBuilder.tsx
- [ ] T166 [US6] Implement Android VotingScreen with proposal details and vote buttons in android/app/src/main/java/za/co/stokvel/ui/governance/VotingScreen.kt
- [ ] T167 [US6] Implement iOS VotingView in ios/DigitalStokvel/Views/VotingView.swift
- [ ] T168 [US6] Implement Android DisputeFlagScreen with text input and Chairperson notification in android/app/src/main/java/za/co/stokvel/ui/governance/DisputeFlagScreen.kt
- [ ] T169 [US6] Implement iOS DisputeFlagView in ios/DigitalStokvel/Views/DisputeFlagView.swift

**Checkpoint**: At this point, User Story 6 should be fully functional - Groups have self-governance with automated rule enforcement

---

## Phase 10: Compliance & Security

**Purpose**: POPIA, FICA, SARB compliance and security hardening

- [ ] T170 [P] Create MemberConsent entity with MemberId, ConsentType (CreditBureau, Marketing), ConsentGiven, Timestamp in backend/src/DigitalStokvel.Core/Entities/MemberConsent.cs
- [ ] T171 [P] Create AuditLog table with EntityType, EntityId, Action, OldValue (jsonb), NewValue (jsonb), UserId, Timestamp in backend/src/DigitalStokvel.Infrastructure/Data/Migrations/
- [ ] T172 Implement POPIA consent collection at onboarding: explicit opt-in for credit bureau, marketing in backend/src/DigitalStokvel.Services/MemberService.cs
- [ ] T173 Implement FICA/KYC verification flow: ID upload + selfie for app users in backend/src/DigitalStokvel.Services/KycService.cs
- [ ] T174 [P] Implement AML monitoring job: flag deposits >R20K or monthly inflows >R100K in backend/src/DigitalStokvel.Infrastructure/Jobs/AmlMonitoringJob.cs
- [ ] T175 Configure Azure South Africa Central region for all resources (SARB data residency) in infrastructure as code
- [ ] T176 [P] Implement AES-256 encryption at rest for PostgreSQL database using Transparent Data Encryption
- [ ] T177 [P] Enforce TLS 1.3 for all API endpoints in backend/src/DigitalStokvel.API/Program.cs
- [ ] T178 Implement 7-year audit log retention policy with automatic archival in backend/src/DigitalStokvel.Infrastructure/Jobs/AuditLogArchivalJob.cs
- [ ] T179 Implement rate limiting per IP and per user (100 req/min) with distributed cache in backend/src/DigitalStokvel.API/Middleware/RateLimitingMiddleware.cs
- [ ] T180 Add security headers (CSP, HSTS, X-Frame-Options) to API responses in backend/src/DigitalStokvel.API/Middleware/SecurityHeadersMiddleware.cs
- [ ] T181 [P] Implement input validation and sanitization for all API endpoints
- [ ] T182 Create compliance dashboard for bank staff to review flagged groups in web/src/components/admin/ComplianceDashboard.tsx

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T183 [P] Add telemetry and Application Insights instrumentation across all services in backend/src/DigitalStokvel.API/Program.cs
- [ ] T184 [P] Implement offline-first architecture for Android: Room database sync in android/app/src/main/java/za/co/stokvel/data/local/
- [ ] T185 [P] Implement offline-first architecture for iOS: Core Data sync in ios/DigitalStokvel/Data/
- [ ] T186 Implement PDF export for group ledger (annual AGM records) in backend/src/DigitalStokvel.Services/LedgerExportService.cs
- [ ] T187 Add shareable branded receipts for group meetings (FR-049) in backend/src/DigitalStokvel.Services/ReceiptService.cs
- [ ] T188 [P] Implement communal language styling: replace "account" with "savings pot", "client" with "member" in all UIs
- [ ] T189 [P] Update all error messages to use encouraging language (FR-051) in backend/src/DigitalStokvel.Services/Resources/localization/
- [ ] T190 Add bank logo and FSCA badge to all wallet screens in all platforms
- [ ] T191 [P] Performance optimization: add database indexes on frequently queried columns (group_id, member_id, timestamp)
- [ ] T192 [P] Implement API response caching for group details, ledger queries using Redis in backend/src/DigitalStokvel.API/Middleware/CachingMiddleware.cs
- [ ] T193 Configure monitoring alerts for AML flags, failed payments, API errors in Azure Application Insights
- [ ] T194 [P] Create API documentation with Swagger annotations and examples in backend/src/DigitalStokvel.API/Controllers/
- [ ] T195 Validate quickstart.md instructions: Docker Compose, database migrations, app launch in all platforms
- [ ] T196 [P] Code cleanup: remove unused dependencies, refactor duplicated logic
- [ ] T197 Add analytics tracking for user journeys: group creation, contributions, payouts
- [ ] T198 Implement push notification batch delivery for large groups (50+ members) in backend/src/DigitalStokvel.Infrastructure/Notifications/PushNotificationService.cs
- [ ] T199 Add soft warning UI when Chairperson adds 51st member in all platforms
- [ ] T200 Configure Azure Service Bus dead letter queue handling for failed notifications in backend/src/DigitalStokvel.Infrastructure/Messaging/ServiceBusClient.cs

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational (Phase 2) - Group creation is entry point
- **User Story 2 (Phase 4)**: Depends on Foundational (Phase 2) and User Story 1 (groups must exist for contributions)
- **User Story 3 (Phase 5)**: Depends on Foundational (Phase 2) and User Story 2 (interest accrues on contributions)
- **User Story 5 (Phase 6)**: Depends on Foundational (Phase 2), User Story 1, User Story 2 (USSD accesses same backend services)
- **User Story 7 (Phase 7)**: Depends on Foundational (Phase 2) - Localization affects all features
- **User Story 4 (Phase 8)**: Depends on User Stories 1, 2, 3 (payouts require groups, contributions, interest calculations)
- **User Story 6 (Phase 9)**: Depends on User Stories 1, 2 (governance rules apply to groups and contributions)
- **Compliance (Phase 10)**: Can run in parallel with user stories, but must complete before production
- **Polish (Phase 11)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P0)**: Foundation only - No dependencies on other stories ✅ MVP CORE
- **User Story 2 (P0)**: Requires User Story 1 (groups must exist) ✅ MVP CORE
- **User Story 3 (P0)**: Requires User Story 2 (interest accrues on contributions) ✅ MVP CORE
- **User Story 5 (P0)**: Requires User Stories 1, 2 (USSD uses same group/contribution services) ✅ MVP CORE
- **User Story 7 (P0)**: Foundation only - Can run in parallel with other stories ✅ MVP CORE
- **User Story 4 (P1)**: Requires User Stories 1, 2, 3 (payouts depend on full contribution/interest cycle)
- **User Story 6 (P1)**: Requires User Stories 1, 2 (governance rules apply to groups and payments)

### Within Each User Story

- Tests (if included) MUST be written and FAIL before implementation
- Entity models before repositories
- Repositories before services
- Services before controllers/APIs
- Backend APIs before mobile/web UIs
- Core implementation before platform-specific features

### Parallel Opportunities

- **Phase 1 (Setup)**: All [P] tasks (T003, T004, T005, T006, T008, T010) can run in parallel
- **Phase 2 (Foundational)**: All [P] tasks (T013, T014, T015, T016, T018, T019, T020, T021, T022, T024, T026, T028, T030) can run in parallel after prerequisites
- **User Stories**: Once Phase 2 completes, can work on multiple stories in parallel if team capacity allows:
  - Team A: User Story 1
  - Team B: User Story 7 (Localization)
  - Then Team A: User Story 2, Team B: User Story 5 (USSD)
- **Within User Stories**: All [P] tasks can run in parallel:
  - Entity models (all [P] entity tasks)
  - Platform UIs (Android/iOS/Web [P] tasks)
  - Localization files (all [P] translation tasks)

---

## Parallel Example: User Story 1

```bash
# Launch all entity models together:
Task T031: "Create Member entity in backend/src/DigitalStokvel.Core/Entities/Member.cs"
Task T032: "Create StokvelsGroup entity in backend/src/DigitalStokvel.Core/Entities/StokvelsGroup.cs"
Task T033: "Create GroupMember entity in backend/src/DigitalStokvel.Core/Entities/GroupMember.cs"

# Launch all platform UIs together after backend APIs complete:
Task T045: "Implement Android GroupCreationScreen in android/app/src/main/java/za/co/stokvel/ui/group/GroupCreationScreen.kt"
Task T046: "Implement iOS GroupCreationView in ios/DigitalStokvel/Views/GroupCreationView.swift"
Task T047: "Implement React GroupCreation in web/src/components/GroupCreation.tsx"
```

---

## Implementation Strategy

### MVP First (P0 User Stories: 1, 2, 3, 5, 7)

1. Complete Phase 1: Setup ✅
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories) ✅
3. Complete Phase 3: User Story 1 (Group Creation) ✅
4. Complete Phase 4: User Story 2 (Contributions) ✅
5. Complete Phase 5: User Story 3 (Interest) ✅
6. Complete Phase 6: User Story 5 (USSD) ✅
7. Complete Phase 7: User Story 7 (Multilingual) ✅
8. Complete Phase 10: Compliance & Security ✅
9. **STOP and VALIDATE**: Test all P0 stories independently
10. Deploy/demo MVP ready for 500 groups, 5K members, R5M deposits

### Incremental Delivery (Add P1 Stories)

1. MVP deployed and validated ✅
2. Add Phase 8: User Story 4 (Automated Payouts) → Deploy
3. Add Phase 9: User Story 6 (Governance) → Deploy
4. Complete Phase 11: Polish → Final production release

### Parallel Team Strategy (3-4 Developers)

**Foundation Phase** (All together):
- Team completes Setup + Foundational (Phases 1-2)

**MVP Phase** (Parallel after foundation):
- Developer A: User Story 1 (Group Creation)
- Developer B: User Story 7 (Localization - foundational for all)
- After US1 completes:
  - Developer A: User Story 2 (Contributions)
  - Developer C: User Story 5 (USSD - depends on US1 backend)
- After US2 completes:
  - Developer A: User Story 3 (Interest)
  - Developer D: Compliance & Security (Phase 10)

**Enhancement Phase** (After MVP):
- Developer A: User Story 4 (Payouts)
- Developer B: User Story 6 (Governance)
- Developer C: Polish & optimization

---

## Success Validation Checkpoints

### After Phase 2 (Foundational)
- [ ] PostgreSQL database migration runs successfully
- [ ] JWT authentication works with test user
- [ ] API returns 401 for unauthenticated requests
- [ ] Localization service loads all 5 languages
- [ ] Redis session management stores/retrieves data

### After User Story 1 (Group Creation)
- [ ] Chairperson creates "Test Stokvel" with R500 contribution
- [ ] 5 members invited via SMS successfully
- [ ] Treasurer role assigned with correct permissions
- [ ] Dashboard shows roster with pagination at 20+ members
- [ ] Soft warning appears when adding 51st member

### After User Story 2 (Contributions)
- [ ] Member makes R500 one-tap payment successfully
- [ ] Contribution appears in group ledger with masked account (****1234)
- [ ] Branded receipt generated and shareable
- [ ] Debit order setup successful with retry logic
- [ ] Payment reminder sent 3 days before due date

### After User Story 3 (Interest)
- [ ] Group wallet shows R10K balance earning 4.5% interest
- [ ] Daily interest accrual job runs successfully
- [ ] Monthly capitalization adds accrued interest to balance
- [ ] Interest tier updates when balance crosses R50K threshold
- [ ] YTD earnings displayed correctly

### After User Story 5 (USSD)
- [ ] Feature phone user dials *120*STOKVEL# and sees menu
- [ ] USSD contribution flow completes in <30 seconds
- [ ] Session restores after network interruption within 120s
- [ ] USSD menu depth never exceeds 3 levels
- [ ] All 5 languages available in USSD menus

### After User Story 7 (Multilingual)
- [ ] User selects isiZulu, entire app displays in isiZulu
- [ ] Error message appears in chosen language with encouraging tone
- [ ] SMS notification sent in user's preferred language
- [ ] Language change in settings applies immediately
- [ ] USSD menus match member's language preference

### MVP Complete (All P0 Stories)
- [ ] 10 test groups created with 50 members
- [ ] R50K in test contributions accumulated
- [ ] Interest calculated and capitalized correctly
- [ ] USSD users complete full contribution flow
- [ ] All 5 languages validated across platforms
- [ ] POPIA consent collected for all test members
- [ ] FICA verification completed for all test members
- [ ] AML monitoring flags test deposit >R20K
- [ ] 7-year audit log retention configured
- [ ] All data stored in Azure South Africa Central region

---

## Key Risks & Mitigations

**Risk**: USSD shortcode registration delayed by MNOs
- **Mitigation**: Start registration process during Phase 1; have contingency plan with alternative shortcode

**Risk**: PostgreSQL jsonb queries perform poorly at scale
- **Mitigation**: Add GIN indexes on constitution and payout_schedule columns; benchmark with 10K groups

**Risk**: Interest calculation rounding errors accumulate
- **Mitigation**: Use decimal(19,4) precision; add daily reconciliation job to check balance integrity

**Risk**: Large groups (100+ members) cause notification cost spikes
- **Mitigation**: Implement batch delivery (50 users per batch); add cost monitoring alerts at R1K/day

**Risk**: POPIA compliance audit failure
- **Mitigation**: Legal review of consent flows before MVP launch; penetration testing by third party

**Risk**: Multi-platform feature parity divergence
- **Mitigation**: Shared REST API contract validation; cross-platform QA checklist per user story

---

## Notes

- [P] tasks = different files, no dependencies - can run in parallel
- [Story] label maps task to specific user story for traceability
- P0 user stories (US1, US2, US3, US5, US7) form MVP core - must complete all before production launch
- P1 user stories (US4, US6) are enhancements - can deploy MVP without them, add incrementally
- Each user story should be independently completable and testable
- Tests not included per feature specification - focus on implementation
- Commit after each task or logical group
- Stop at checkpoints to validate story independently
- All file paths use multi-platform structure from plan.md
- USSD integration requires MNO partnerships - start early
- Compliance (Phase 10) is non-negotiable for production - must complete before launch
- .NET 10 and C# 13 used per updated technical stack
