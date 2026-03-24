# Technical Research: Digital Stokvel Banking MVP

**Date**: 2026-03-24  
**Purpose**: Resolve technical unknowns and best practices for multi-platform fintech application

---

## 1. C# Banking Application Patterns

### Decision: Use Clean Architecture with Domain-Driven Design (DDD) for financial operations

**Rationale**:
- **ACID Transactions**: Entity Framework Core provides built-in transaction management with `DbContext.Database.BeginTransaction()` for multi-step financial operations (e.g., contribution + ledger entry + interest accrual)
- **Audit Logging**: Implement `IAuditableEntity` interface with `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy` fields; EF Core interceptors (`SaveChangesInterceptor`) automatically populate audit fields on all financial entities
- **Idempotency**: Use idempotency keys (GUID) on all customer-initiated financial transactions; store in `IdempotencyLog` table with transaction ID and status (Pending/Completed/Failed); API middleware checks for duplicate keys before processing
- **Money Handling**: Use `decimal` type (not `float`) for all currency amounts; create `Money` value object with currency code (ZAR) and amount; never perform floating-point arithmetic on money
- **Concurrency**: Implement optimistic concurrency with EF Core row versioning (`[Timestamp]` attribute) on Group and Account entities to prevent race conditions in contributions/payouts

**Pattern Structure**:
```csharp
// Domain Entity (DigitalStokvel.Core)
public class Contribution : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid MemberId { get; set; }
    public Money Amount { get; set; }
    public string IdempotencyKey { get; set; }
    public ContributionStatus Status { get; set; } // Pending, Completed, Failed
    public DateTime TransactionTimestamp { get; set; }
    public byte[] RowVersion { get; set; } // Concurrency token
}

// Service Layer (DigitalStokvel.Services)
public class ContributionService
{
    public async Task<ContributionResult> ProcessContributionAsync(
        Guid memberId, Guid groupId, decimal amount, string idempotencyKey)
    {
        // 1. Check idempotency
        if (await _idempotencyRepo.ExistsAsync(idempotencyKey))
            return await _idempotencyRepo.GetResultAsync(idempotencyKey);

        // 2. Begin transaction
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // 3. Deduct from member account
            var deduction = await _paymentGateway.DeductAsync(memberId, amount);
            
            // 4. Add to group wallet
            var group = await _groupRepo.GetByIdAsync(groupId);
            group.AddContribution(amount); // Domain logic
            
            // 5. Create ledger entry
            var contribution = new Contribution(groupId, memberId, amount, idempotencyKey);
            await _contributionRepo.AddAsync(contribution);
            
            // 6. Log idempotency
            await _idempotencyRepo.LogAsync(idempotencyKey, contribution.Id);
            
            await transaction.CommitAsync();
            return ContributionResult.Success(contribution);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ContributionResult.Failure(ex.Message);
        }
    }
}
```

**Alternatives Considered**:
- **CQRS with Event Sourcing**: Rejected due to MVP timeline constraints; event sourcing adds significant complexity for read model synchronization and historical replay
- **Microservices Architecture**: Rejected for MVP; monolithic Clean Architecture is sufficient for 500 groups/5K users; can decompose post-MVP if needed

**Implementation Notes**:
- Use Polly library for resilience (retry policies, circuit breakers) on external payment gateway calls
- Implement background job (Hangfire or Azure Functions) for failed transaction reconciliation
- Store transaction logs in append-only audit table (never UPDATE, only INSERT) to maintain immutable history per FR-017

---

## 2. PostgreSQL Schema Design for Financial Data

### Decision: Use PostgreSQL with jsonb for governance rules, proper indexing for financial queries, and row-level security for group isolation

**Rationale**:
- **ACID Compliance**: PostgreSQL provides industry-standard ACID guarantees essential for financial transactions
- **Decimal Precision**: Use `NUMERIC(19,4)` for money columns (supports up to R999,999,999,999,999.9999 with 4 decimal places for interest calculations)
- **JSONB for Flexibility**: Store group constitution rules as `jsonb` to allow dynamic schema (missed payment penalties, grace periods, voting thresholds vary per group)
- **Indexing Strategy**: Create composite indexes on `(group_id, member_id, transaction_timestamp)` for ledger queries; GIN index on `jsonb` governance rules for fast rule lookups
- **Row-Level Security (RLS)**: Enable PostgreSQL RLS policies to enforce group isolation at database level; ensures members can only query data for groups they belong to
- **Temporal Queries**: Use `tstzrange` (timestamp with time zone range) for contribution schedules and payout windows; supports efficient "what's due this week" queries
- **Audit Trail**: Implement trigger-based audit logging to `audit_log` table capturing INSERT/UPDATE/DELETE on financial tables with old/new values as `jsonb`

**Schema Design**:
```sql
-- Groups table
CREATE TABLE groups (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    group_type VARCHAR(20) CHECK (group_type IN ('rotating_payout', 'savings_pot', 'investment_club')),
    contribution_amount NUMERIC(19,4) NOT NULL,
    contribution_frequency VARCHAR(10) CHECK (contribution_frequency IN ('weekly', 'monthly')),
    payout_schedule JSONB NOT NULL, -- e.g., {"type": "rotating", "cycle_days": 30, "current_recipient_order": 1}
    constitution JSONB NOT NULL, -- e.g., {"missed_payment_penalty": 50.00, "grace_period_days": 7, "quorum_threshold": 0.6}
    account_balance NUMERIC(19,4) DEFAULT 0.0000,
    interest_tier VARCHAR(20) DEFAULT 'tier_1', -- tier_1: 3.5%, tier_2: 4.5%, tier_3: 5.5%
    accrued_interest NUMERIC(19,4) DEFAULT 0.0000,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    modified_at TIMESTAMPTZ DEFAULT NOW(),
    row_version BIGINT DEFAULT 1, -- Optimistic concurrency
    CONSTRAINT positive_balance CHECK (account_balance >= 0)
);
CREATE INDEX idx_groups_type ON groups(group_type);
CREATE INDEX idx_groups_constitution ON groups USING GIN (constitution);

-- Members table
CREATE TABLE members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bank_customer_id VARCHAR(50) NOT NULL UNIQUE, -- Foreign key to bank's customer system
    phone_number VARCHAR(20) NOT NULL,
    preferred_language VARCHAR(5) CHECK (preferred_language IN ('en', 'zu', 'st', 'xh', 'af')),
    fica_verified BOOLEAN DEFAULT FALSE,
    fica_verification_date TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX idx_members_bank_customer ON members(bank_customer_id);

-- Group membership (join table with roles)
CREATE TABLE group_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    group_id UUID NOT NULL REFERENCES groups(id) ON DELETE CASCADE,
    member_id UUID NOT NULL REFERENCES members(id) ON DELETE CASCADE,
    role VARCHAR(20) CHECK (role IN ('chairperson', 'treasurer', 'secretary', 'member')),
    joined_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(group_id, member_id)
);
CREATE INDEX idx_group_members_group ON group_members(group_id);
CREATE INDEX idx_group_members_member ON group_members(member_id);

-- Contributions ledger
CREATE TABLE contributions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    group_id UUID NOT NULL REFERENCES groups(id),
    member_id UUID NOT NULL REFERENCES members(id),
    amount NUMERIC(19,4) NOT NULL,
    payment_method VARCHAR(20) CHECK (payment_method IN ('one_tap', 'debit_order', 'ussd')),
    status VARCHAR(20) CHECK (status IN ('pending', 'completed', 'failed')),
    idempotency_key UUID NOT NULL UNIQUE,
    transaction_timestamp TIMESTAMPTZ DEFAULT NOW(),
    confirmation_receipt_url TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    CONSTRAINT positive_amount CHECK (amount > 0)
);
CREATE INDEX idx_contributions_group_member ON contributions(group_id, member_id, transaction_timestamp DESC);
CREATE INDEX idx_contributions_idempotency ON contributions(idempotency_key);
CREATE INDEX idx_contributions_status ON contributions(status) WHERE status = 'pending';

-- Payouts table
CREATE TABLE payouts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    group_id UUID NOT NULL REFERENCES groups(id),
    payout_type VARCHAR(20) CHECK (payout_type IN ('rotating', 'year_end', 'partial')),
    total_amount NUMERIC(19,4) NOT NULL,
    initiated_by UUID NOT NULL REFERENCES members(id), -- Chairperson
    confirmed_by UUID REFERENCES members(id), -- Treasurer
    status VARCHAR(20) CHECK (status IN ('pending_confirmation', 'confirmed', 'executed', 'rejected')),
    execution_timestamp TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Payout recipients (can be multiple for year_end distribution)
CREATE TABLE payout_recipients (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    payout_id UUID NOT NULL REFERENCES payouts(id) ON DELETE CASCADE,
    member_id UUID NOT NULL REFERENCES members(id),
    amount NUMERIC(19,4) NOT NULL,
    eft_reference VARCHAR(50),
    disbursed_at TIMESTAMPTZ
);

-- Idempotency log (prevents duplicate transactions)
CREATE TABLE idempotency_log (
    idempotency_key UUID PRIMARY KEY,
    entity_type VARCHAR(50) NOT NULL, -- 'contribution', 'payout', etc.
    entity_id UUID NOT NULL,
    status VARCHAR(20) CHECK (status IN ('pending', 'completed', 'failed')),
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Audit log (immutable transaction history)
CREATE TABLE audit_log (
    id BIGSERIAL PRIMARY KEY,
    table_name VARCHAR(50) NOT NULL,
    record_id UUID NOT NULL,
    operation VARCHAR(10) CHECK (operation IN ('INSERT', 'UPDATE', 'DELETE')),
    old_values JSONB,
    new_values JSONB,
    changed_by VARCHAR(50), -- User/system identifier
    changed_at TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX idx_audit_log_table_record ON audit_log(table_name, record_id, changed_at DESC);

-- Row-Level Security policies (example for contributions)
ALTER TABLE contributions ENABLE ROW LEVEL SECURITY;
CREATE POLICY contributions_member_access ON contributions
    FOR SELECT
    USING (member_id IN (
        SELECT member_id FROM group_members WHERE group_id = contributions.group_id
    ));
```

**Alternatives Considered**:
- **SQL Server**: Rejected due to licensing costs; PostgreSQL is open-source and widely supported on Azure
- **MongoDB**: Rejected due to lack of ACID guarantees across documents; financial data requires relational integrity
- **Separate database per group (multi-tenancy)**: Rejected due to operational overhead; RLS provides sufficient isolation

**Implementation Notes**:
- Use EF Core migrations to version-control schema changes
- Implement database seeding scripts for initial interest tier configuration
- Set up PostgreSQL connection pooling (Npgsql) with `Min Pool Size=10, Max Pool Size=100` for Azure-hosted database
- Enable PostgreSQL query logging for slow queries (>500ms) to Application Insights for performance monitoring

---

## 3. USSD Gateway Integration Patterns for South African MNOs

### Decision: Use HTTP-based USSD gateway APIs with session state management in Redis and max 3-level menu depth

**Rationale**:
- **MNO Gateway Standards**: Major South African MNOs (Vodacom, MTN, Cell C, Telkom) support HTTP-based USSD gateways with XML or JSON payloads
- **Session Management**: USSD sessions timeout after 180 seconds of inactivity; use Redis to store session state (current menu level, user input history, authentication status) with 120-second TTL to handle network interruptions
- **Authentication**: Users authenticate once per session using existing bank PIN; store authenticated state in Redis session; subsequent menu actions don't require re-authentication within session
- **Menu Design**: Strict 3-level depth limit to prevent user drop-off; structure: Level 1 (Main Menu) → Level 2 (Action Selection) → Level 3 (Confirmation/Execution)
- **Language Support**: Detect user's phone language preference on session initiation; store in session state; render all menu text in selected language (5 languages supported)
- **Graceful Degradation**: If session state is lost (Redis eviction, network timeout), prompt user to redial with option to resume last action

**USSD Flow Example**:
```text
Level 1 (Main Menu):
*120*STOKVEL#
→ Response (isiZulu):
   Yebo! Khetha okufunayo:
   1. Faka imali (Pay Contribution)
   2. Bheka imali (Check Balance)
   3. Umlando (Transaction History)
   4. Usizo (Help)

Level 2 (Action: Pay Contribution):
User presses 1
→ Response:
   Qinisekisa:
   Iqembu: Ntombizodwa Stokvel
   Imali: R500.00
   1=Yebo (Yes), 2=Cha (No)

Level 3 (Confirmation):
User presses 1
→ Response:
   Faka iPIN yakho yebanki:
   [User enters PIN]
→ Final Response:
   Impumelelo! (Success!)
   Inombolo yokuqinisekisa: STK-2026-03-24-001234
   Imali yakho ye-R500 isithunyelwe kuNtombizodwa Stokvel.
   [END SESSION]
```

**Integration Pattern**:
```csharp
// USSD Gateway Integration (DigitalStokvel.Infrastructure/USSD)
public class UssdGatewayService : IUssdGatewayService
{
    private readonly IDistributedCache _redis; // Session state
    private readonly ILocalizationService _localization;
    private readonly IContributionService _contributionService;

    public async Task<UssdResponse> HandleRequestAsync(UssdRequest request)
    {
        // 1. Retrieve or create session
        var sessionKey = $"ussd:session:{request.SessionId}";
        var session = await _redis.GetAsync<UssdSession>(sessionKey) 
                      ?? new UssdSession(request.PhoneNumber, request.Language);

        // 2. Route to menu handler based on current level
        var handler = _menuFactory.GetHandler(session.CurrentMenu);
        var response = await handler.ProcessInputAsync(request.UserInput, session);

        // 3. Update session state
        session.CurrentMenu = response.NextMenu;
        session.InputHistory.Add(request.UserInput);
        await _redis.SetAsync(sessionKey, session, TimeSpan.FromSeconds(120));

        // 4. Return response to MNO gateway
        return new UssdResponse
        {
            Message = _localization.Translate(response.Message, session.Language),
            SessionAction = response.IsTerminal ? "END" : "CON", // CON = continue, END = terminate
            SessionId = request.SessionId
        };
    }
}

// Menu Handler (example: Contribution flow)
public class ContributionMenuHandler : IUssdMenuHandler
{
    public async Task<MenuResponse> ProcessInputAsync(string input, UssdSession session)
    {
        switch (session.CurrentLevel)
        {
            case 2: // Confirmation prompt
                if (input == "1") // User confirmed
                {
                    return new MenuResponse
                    {
                        Message = "ussd.enter_pin", // Localization key
                        NextMenu = UssdMenu.PinAuthentication,
                        IsTerminal = false
                    };
                }
                return MenuResponse.CancelAndReturnToMain();

            case 3: // PIN entered, execute contribution
                var pinValid = await _authService.ValidatePinAsync(session.PhoneNumber, input);
                if (!pinValid)
                    return MenuResponse.Error("ussd.invalid_pin");

                var result = await _contributionService.ProcessContributionAsync(
                    session.MemberId, session.SelectedGroupId, session.ContributionAmount,
                    idempotencyKey: Guid.NewGuid().ToString()
                );

                return new MenuResponse
                {
                    Message = result.Success 
                        ? $"ussd.contribution_success|{result.ConfirmationCode}"
                        : "ussd.contribution_failed",
                    IsTerminal = true
                };
        }
    }
}
```

**MNO Integration Requirements**:
- **Vodacom USSD Gateway**: Register shortcode via Vodacom developer portal; use their HTTP API with JSON payloads
- **MTN USSD API**: Similar HTTP-based API; requires business account and approval process (6-8 weeks)
- **Cell C & Telkom**: Use aggregators (e.g.,Clickatell, Africa's Talking) for faster onboarding

**Alternatives Considered**:
- **SMS-based flows**: Rejected due to higher latency (store-and-forward), higher cost per transaction, and inferior user experience
- **USSD without session state**: Rejected; users would need to restart flow on any interruption, causing frustration

**Implementation Notes**:
- Build USSD simulator for local testing (no need to dial shortcode during development)
- Implement comprehensive logging of USSD sessions for debugging (session ID, menu path, user inputs, errors)
- Use feature flags to enable/disable USSD per MNO gateway for staged rollout
- Monitor USSD success rates (<30s completion) vs. drop-off rates (session timeout, user cancellation) in Application Insights

---

## 4. Mobile App Architecture for Offline-First Stokvel Apps

### Decision: Use MVVM architecture with Repository pattern, Room/CoreData for local persistence, and WorkManager/Background Tasks for sync with conflict resolution

**Rationale**:
- **Offline-First Requirement**: Target market has low connectivity (36% of South African users experience intermittent mobile data); app must function when offline and sync when online
- **MVVM (Model-View-ViewModel)**: Separates UI (Jetpack Compose/SwiftUI) from business logic (ViewModel) and data (Repository); facilitates unit testing and reactive UI updates
- **Local Database**: Android uses Room (SQLite abstraction); iOS uses CoreData (Apple's ORM); stores groups, members, contributions, ledger locally for instant read access
- **Sync Strategy**: Use WorkManager (Android) / BackgroundTasks (iOS) for background sync when connectivity available; implement conflict resolution for concurrent contributions (last-write-wins with server timestamp as authority)
- **Optimistic UI**: Show contribution as "Pending" immediately in local DB with sync icon; sync to backend asynchronously; update to "Completed" or "Failed" after server confirmation
- **Data Consistency**: Use server as source of truth; on sync, fetch latest group balance and ledger from API, merge with local changes, resolve conflicts, update local DB

**Android Architecture (Kotlin + Jetpack Compose)**:
```kotlin
// Data Layer: Room database
@Entity(tableName = "contributions")
data class ContributionEntity(
    @PrimaryKey val id: String = UUID.randomUUID().toString(),
    val groupId: String,
    val memberId: String,
    val amount: BigDecimal,
    val status: String, // "pending_sync", "synced", "failed"
    val syncedAt: Long? = null,
    val createdAt: Long = System.currentTimeMillis()
)

@Dao
interface ContributionDao {
    @Query("SELECT * FROM contributions WHERE groupId = :groupId ORDER BY createdAt DESC")
    fun getContributionsForGroup(groupId: String): Flow<List<ContributionEntity>>

    @Query("SELECT * FROM contributions WHERE status = 'pending_sync'")
    suspend fun getPendingContributions(): List<ContributionEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insertContribution(contribution: ContributionEntity)

    @Update
    suspend fun updateContribution(contribution: ContributionEntity)
}

// Repository: Manages local + remote data
class ContributionRepository(
    private val contributionDao: ContributionDao,
    private val apiService: StokvelsApiService
) {
    fun getContributionsForGroup(groupId: String): Flow<List<Contribution>> {
        return contributionDao.getContributionsForGroup(groupId)
            .map { entities -> entities.map { it.toDomainModel() } }
    }

    suspend fun makeContribution(
        groupId: String, memberId: String, amount: BigDecimal
    ): Result<Contribution> {
        val contribution = ContributionEntity(
            groupId = groupId,
            memberId = memberId,
            amount = amount,
            status = "pending_sync"
        )
        
        // 1. Save locally immediately (optimistic UI)
        contributionDao.insertContribution(contribution)
        
        // 2. Attempt sync if online
        return try {
            val response = apiService.postContribution(contribution.toDto())
            val synced = contribution.copy(
                id = response.id,
                status = "synced",
                syncedAt = System.currentTimeMillis()
            )
            contributionDao.updateContribution(synced)
            Result.success(synced.toDomainModel())
        } catch (e: Exception) {
            // If offline, contribution stays in "pending_sync" state
            Result.success(contribution.toDomainModel())
        }
    }

    suspend fun syncPendingContributions() {
        val pending = contributionDao.getPendingContributions()
        pending.forEach { contribution ->
            try {
                val response = apiService.postContribution(contribution.toDto())
                contributionDao.updateContribution(
                    contribution.copy(
                        id = response.id,
                        status = "synced",
                        syncedAt = System.currentTimeMillis()
                    )
                )
            } catch (e: Exception) {
                // If sync fails, leave as pending; WorkManager will retry
                Log.e("Sync", "Failed to sync contribution ${contribution.id}", e)
            }
        }
    }
}

// ViewModel: Exposes data to UI
@HiltViewModel
class GroupWalletViewModel @Inject constructor(
    private val contributionRepository: ContributionRepository,
    savedStateHandle: SavedStateHandle
) : ViewModel() {
    private val groupId: String = checkNotNull(savedStateHandle["groupId"])

    val contributions: StateFlow<List<Contribution>> = contributionRepository
        .getContributionsForGroup(groupId)
        .stateIn(viewModelScope, SharingStarted.Lazily, emptyList())

    fun makeContribution(amount: BigDecimal) {
        viewModelScope.launch {
            val result = contributionRepository.makeContribution(groupId, currentMemberId, amount)
            // UI reacts automatically via StateFlow
        }
    }
}

// Background Sync: WorkManager
class ContributionSyncWorker(
    context: Context,
    params: WorkerParameters,
    private val repository: ContributionRepository
) : CoroutineWorker(context, params) {
    override suspend fun doWork(): Result {
        return try {
            repository.syncPendingContributions()
            Result.success()
        } catch (e: Exception) {
            if (runAttemptCount < 3) Result.retry() else Result.failure()
        }
    }
}

// Enqueue sync worker on app start and when connectivity restored
val syncRequest = PeriodicWorkRequestBuilder<ContributionSyncWorker>(
    repeatInterval = 15, repeatIntervalTimeUnit = TimeUnit.MINUTES
).setConstraints(
    Constraints.Builder()
        .setRequiredNetworkType(NetworkType.CONNECTED)
        .build()
).build()
WorkManager.getInstance(context).enqueue(syncRequest)
```

**iOS Architecture (Swift + SwiftUI)**:
```swift
// Data Layer: CoreData entity
@objc(ContributionEntity)
class ContributionEntity: NSManagedObject {
    @NSManaged var id: UUID
    @NSManaged var groupId: UUID
    @NSManaged var amount: NSDecimalNumber
    @NSManaged var status: String // "pending_sync", "synced", "failed"
    @NSManaged var createdAt: Date
}

// Repository: Manages local + remote data
class ContributionRepository {
    private let persistentContainer: NSPersistentContainer
    private let apiService: StokvelsAPIService
    
    func makeContribution(groupId: UUID, amount: Decimal) async throws -> Contribution {
        let context = persistentContainer.viewContext
        let entity = ContributionEntity(context: context)
        entity.id = UUID()
        entity.groupId = groupId
        entity.amount = NSDecimalNumber(decimal: amount)
        entity.status = "pending_sync"
        entity.createdAt = Date()
        
        try context.save() // Save locally immediately
        
        // Attempt sync if online
        Task {
            do {
                let response = try await apiService.postContribution(entity.toDTO())
                entity.id = response.id
                entity.status = "synced"
                try context.save()
            } catch {
                // If offline, stays in pending_sync state
                print("Sync failed: \(error)")
            }
        }
        
        return entity.toDomainModel()
    }
    
    func syncPendingContributions() async {
        let fetchRequest: NSFetchRequest<ContributionEntity> = ContributionEntity.fetchRequest()
        fetchRequest.predicate = NSPredicate(format: "status == %@", "pending_sync")
        
        let context = persistentContainer.newBackgroundContext()
        guard let pending = try? context.fetch(fetchRequest) else { return }
        
        for entity in pending {
            do {
                let response = try await apiService.postContribution(entity.toDTO())
                entity.id = response.id
                entity.status = "synced"
            } catch {
                print("Sync failed for contribution \(entity.id): \(error)")
            }
        }
        try? context.save()
    }
}

// ViewModel: SwiftUI ObservableObject
@MainActor
class GroupWalletViewModel: ObservableObject {
    @Published var contributions: [Contribution] = []
    private let repository: ContributionRepository
    
    func makeContribution(amount: Decimal) {
        Task {
            do {
                let contribution = try await repository.makeContribution(
                    groupId: groupId, amount: amount
                )
                // UI updates automatically via @Published
            } catch {
                // Handle error
            }
        }
    }
}

// Background Sync: BackgroundTasks framework
BGTaskScheduler.shared.register(
    forTaskWithIdentifier: "com.stokvel.sync",
    using: nil
) { task in
    Task {
        await contributionRepository.syncPendingContributions()
        task.setTaskCompleted(success: true)
    }
}
```

**Alternatives Considered**:
- **Online-only architecture**: Rejected due to poor user experience in low-connectivity areas; 36% of target market has intermittent data
- **Full offline mode with manual sync**: Rejected due to complexity of conflict resolution and risk of data loss

**Implementation Notes**:
- Implement conflict resolution strategy: server timestamp wins for balance conflicts; show user notification if local contribution failed due to insufficient funds detected on server
- Display sync status icon in UI (synced checkmark, pending spinner, failed exclamation with retry button)
- Store last successful sync timestamp; show "Last updated X minutes ago" in wallet view
- Preload group data (roster, ledger last 3 months) on Wi-Fi to minimize mobile data usage

---

## 5. POPIA Compliance Implementation in C# Applications

### Decision: Implement POPIA compliance with explicit consent management, data minimization, audit logging, and right-to-erasure workflows in ASP.NET Core

**Rationale**:
- **POPIA Requirements**: Protection of Personal Information Act (2013) mandates explicit consent, data minimization, purpose limitation, accuracy, security, and accountability
- **Explicit Consent**: Use granular consent checkboxes during onboarding (separate for credit bureau reporting, marketing, group data sharing); store consent records with timestamp and version
- **Data Minimization**: FR-046 requires ledger to show only masked account numbers ("****1234"), not full account details; implement column-level access control in repositories
- **Purpose Limitation**: Personal data collected for stokvel participation MUST NOT be used for cross-selling without additional consent
- **Right to Erasure**: Members can request data deletion; implement soft-delete with anonymization (replace PII with "[Deleted Member]", retain transaction history with anonymized identifiers for audit compliance)
- **Security**: AES-256 encryption at rest (Azure Storage encryption), TLS 1.3 in transit, role-based access control (RBAC) in API

**Consent Management Implementation**:
```csharp
// Domain Entity (DigitalStokvel.Core/Entities/Consent.cs)
public class MemberConsent
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public ConsentType Type { get; set; } // CreditBureauReporting, Marketing, GroupDataSharing
    public bool IsGranted { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string ConsentVersion { get; set; } // e.g., "1.0" (tracks terms version)
    public string IpAddress { get; set; } // Audit: where consent was given
}

public enum ConsentType
{
    CreditBureauReporting,
    Marketing,
    GroupDataSharing
}

// Service Layer (DigitalStokvel.Services/ConsentService.cs)
public class ConsentService
{
    private readonly IConsentRepository _consentRepo;
    private readonly IAuditLogger _auditLogger;

    public async Task<bool> HasConsentAsync(Guid memberId, ConsentType type)
    {
        var consent = await _consentRepo.GetLatestConsentAsync(memberId, type);
        return consent != null && consent.IsGranted && consent.RevokedAt == null;
    }

    public async Task GrantConsentAsync(Guid memberId, ConsentType type, string ipAddress)
    {
        var consent = new MemberConsent
        {
            MemberId = memberId,
            Type = type,
            IsGranted = true,
            GrantedAt = DateTime.UtcNow,
            ConsentVersion = "1.0", // Current terms version
            IpAddress = ipAddress
        };
        await _consentRepo.AddAsync(consent);
        await _auditLogger.LogConsentGrantedAsync(memberId, type);
    }

    public async Task RevokeConsentAsync(Guid memberId, ConsentType type)
    {
        var consent = await _consentRepo.GetLatestConsentAsync(memberId, type);
        if (consent != null && consent.IsGranted && consent.RevokedAt == null)
        {
            consent.RevokedAt = DateTime.UtcNow;
            await _consentRepo.UpdateAsync(consent);
            await _auditLogger.LogConsentRevokedAsync(memberId, type);
        }
    }
}

// API Middleware (DigitalStokvel.API/Middleware/ConsentCheckMiddleware.cs)
public class ConsentCheckMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context, IConsentService consentService)
    {
        // Example: Block credit bureau reporting API if consent not granted
        if (context.Request.Path.StartsWithSegments("/api/credit-bureau"))
        {
            var memberId = context.User.GetMemberId(); // Extract from JWT
            var hasConsent = await consentService.HasConsentAsync(
                memberId, ConsentType.CreditBureauReporting
            );
            if (!hasConsent)
            {
                context.Response.StatusCode = 403; // Forbidden
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Credit bureau reporting requires explicit consent. " +
                            "Update your consent preferences in Settings."
                });
                return;
            }
        }
        await _next(context);
    }
}
```

**Data Minimization Implementation**:
```csharp
// Repository Layer: Ledger view with masked account numbers
public class ContributionRepository : IContributionRepository
{
    public async Task<List<ContributionLedgerDto>> GetGroupLedgerAsync(Guid groupId)
    {
        return await _dbContext.Contributions
            .Where(c => c.GroupId == groupId)
            .Include(c => c.Member)
            .Select(c => new ContributionLedgerDto
            {
                MemberName = c.Member.FullName,
                Amount = c.Amount,
                Status = c.Status,
                Timestamp = c.TransactionTimestamp,
                // POPIA Data Minimization: Show only last 4 digits of account
                MaskedAccountNumber = "****" + c.Member.AccountNumber.Substring(
                    c.Member.AccountNumber.Length - 4
                )
                // Full account number NOT included in DTO
            })
            .OrderByDescending(c => c.Timestamp)
            .ToListAsync();
    }
}
```

**Right to Erasure Implementation**:
```csharp
// Service Layer (DigitalStokvel.Services/MemberService.cs)
public class MemberService
{
    public async Task ProcessDataDeletionRequestAsync(Guid memberId)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var member = await _memberRepo.GetByIdAsync(memberId);
            
            // 1. Check if member has active group participation
            var activeGroups = await _groupRepo.GetActiveGroupsForMemberAsync(memberId);
            if (activeGroups.Any())
            {
                throw new InvalidOperationException(
                    "Cannot delete data while member is active in groups. " +
                    "Please exit all groups first."
                );
            }
            
            // 2. Anonymize personal data (retain transaction history for audit)
            member.FullName = "[Deleted Member]";
            member.PhoneNumber = "N/A";
            member.EmailAddress = "N/A";
            member.IdentityNumber = "REDACTED"; // Hash with SHA-256 for future reference
            member.IsDeleted = true; // Soft delete flag
            member.DeletedAt = DateTime.UtcNow;
            
            // 3. Retain contribution records (financial audit requirement) but with anonymized member reference
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            
            await _auditLogger.LogDataDeletionAsync(memberId, "Member requested data deletion");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

**Alternatives Considered**:
- **Hard delete all member data**: Rejected due to financial audit requirements (FICA mandates 7-year transaction retention)
- **Complete anonymization with no trace**: Rejected due to fraud investigation requirements

**Implementation Notes**:
- Display POPIA notice and consent checkboxes during onboarding before any data collection
- Implement "Data & Privacy" settings page where members can view/revoke consents at any time
- Log all consent changes to audit table with IP address, timestamp, and user agent
- Conduct annual POPIA compliance review to ensure data processing aligns with stated purposes
- Use Azure Key Vault for managing encryption keys (rotate keys annually)

---

## 6. Interest Calculation and Compounding Strategies

### Decision: Use daily compounding with monthly capitalization on group wallet balances; for rotating payouts, only principal is distributed, interest remains in wallet

**Rationale**:
- **Competitive Rates**: Tiered interest rates (3.5%/4.5%/5.5%) align with savings account products offered by South African banks; daily compounding maximizes returns for members
- **Compounding Formula**: Use standard compound interest formula `A = P(1 + r/n)^(nt)` where daily compounding means `n = 365`
- **Monthly Capitalization**: Accrued interest is added to principal monthly (not daily) to simplify accounting; daily compounding calculation runs nightly but balance update happens once per month
- **Rotating Payout Logic**: When a payout occurs in a rotating group, only the principal contributions for that cycle are paid out; all accrued interest remains in the group wallet and continues compounding for future recipients
- **Year-End Distribution**: For savings pot groups, both principal + all accrued interest are distributed proportionally on year-end
- **Precision**: Use `decimal` type in C# (28-29 significant digits) to avoid floating-point errors; store interest in database as `NUMERIC(19,4)` (4 decimal places sufficient for ZAR amounts)

**Interest Calculation Implementation**:
```csharp
// Service Layer (DigitalStokvel.Services/InterestService.cs)
public class InterestService
{
    private readonly IGroupRepository _groupRepo;
    private readonly IInterestRateConfig _rateConfig;
    private readonly IAuditLogger _auditLogger;

    // Nightly background job: Calculate daily interest for all groups
    public async Task CalculateDailyInterestForAllGroupsAsync()
    {
        var groups = await _groupRepo.GetAllActiveGroupsAsync();
        
        foreach (var group in groups)
        {
            var dailyInterest = CalculateDailyCompoundInterest(
                principal: group.AccountBalance,
                annualRate: GetInterestRateForTier(group.InterestTier),
                days: 1
            );
            
            group.AccruedInterest += dailyInterest;
            await _groupRepo.UpdateAsync(group);
            
            await _auditLogger.LogInterestAccrualAsync(
                group.Id, dailyInterest, group.AccruedInterest
            );
        }
    }

    // Monthly job: Capitalize accrued interest to account balance
    public async Task CapitalizeMonthlyInterestAsync(Guid groupId)
    {
        var group = await _groupRepo.GetByIdAsync(groupId);
        
        group.AccountBalance += group.AccruedInterest;
        var capitalizedAmount = group.AccruedInterest;
        group.AccruedInterest = 0; // Reset for next month
        
        await _groupRepo.UpdateAsync(group);
        await _auditLogger.LogInterestCapitalizationAsync(groupId, capitalizedAmount);
    }

    // Helper: Calculate daily compound interest
    private decimal CalculateDailyCompoundInterest(
        decimal principal, decimal annualRate, int days)
    {
        // Formula: A = P(1 + r/n)^(nt) - P
        // Where: n = 365 (daily compounding), t = days/365
        var dailyRate = annualRate / 365m;
        var compoundFactor = (decimal)Math.Pow(
            (double)(1 + dailyRate), days
        );
        return (principal * compoundFactor) - principal;
    }

    // Helper: Get interest rate based on balance tier
    private decimal GetInterestRateForTier(string tier)
    {
        return tier switch
        {
            "tier_1" => 0.035m, // 3.5% for R0-R10K
            "tier_2" => 0.045m, // 4.5% for R10K-R50K
            "tier_3" => 0.055m, // 5.5% for R50K+
            _ => 0.035m
        };
    }

    // Update tier when balance crosses threshold
    public async Task UpdateInterestTierAsync(Guid groupId)
    {
        var group = await _groupRepo.GetByIdAsync(groupId);
        var newTier = group.AccountBalance switch
        {
            >= 50000m => "tier_3",
            >= 10000m => "tier_2",
            _ => "tier_1"
        };

        if (group.InterestTier != newTier)
        {
            group.InterestTier = newTier;
            await _groupRepo.UpdateAsync(group);
            await _auditLogger.LogTierChangeAsync(groupId, newTier);
        }
    }
}

// Payout Service: Handle rotating payout logic (principal only, interest stays)
public class PayoutService
{
    public async Task ExecuteRotatingPayoutAsync(Guid payoutId)
    {
        var payout = await _payoutRepo.GetByIdAsync(payoutId);
        var group = await _groupRepo.GetByIdAsync(payout.GroupId);

        // Calculate total contributions in current cycle (principal only)
        var cycleStartDate = GetCycleStartDate(group);
        var cyclePrincipal = await _contributionRepo.GetTotalContributionsAsync(
            group.Id, cycleStartDate, DateTime.UtcNow
        );

        // Payout ONLY principal; interest remains in wallet
        var payoutAmount = cyclePrincipal;
        group.AccountBalance -= payoutAmount;
        // Note: AccruedInterest and capitalized interest stay in AccountBalance

        await _eftService.DisburseAsync(payout.RecipientMemberId, payoutAmount);
        payout.Status = "executed";
        await _payoutRepo.UpdateAsync(payout);
        await _groupRepo.UpdateAsync(group);

        await _notificationService.NotifyGroupAsync(
            group.Id, 
            $"Payout of {payoutAmount:C} sent to {payout.RecipientName}. " +
            $"Remaining balance: {group.AccountBalance:C} (includes accrued interest)"
        );
    }
}
```

**Interest Calculation Example**:
```
Scenario: Rotating payout group with 10 members contributing R500/month

Month 1:
- Contributions: 10 members * R500 = R5,000
- Balance: R5,000 (tier_1: 3.5% annual rate)
- Daily interest: R5,000 * (1 + 0.035/365)^1 - R5,000 = R0.479
- 30 days accrued: R0.479 * 30 = R14.38
- Month-end capitalization: R5,000 + R14.38 = R5,014.38
- Payout to Member 1: R5,000 (principal only)
- Remaining balance after payout: R5,014.38 - R5,000 = R14.38 (interest retained)

Month 2:
- Contributions: 10 members * R500 = R5,000
- Balance before new contributions: R14.38 (interest from Month 1)
- Balance after contributions: R14.38 + R5,000 = R5,014.38
- Daily interest: R5,014.38 * (1 + 0.035/365)^1 - R5,014.38 = R0.480
- 30 days accrued: R0.480 * 30 = R14.41
- Month-end capitalization: R5,014.38 + R14.41 = R5,028.79
- Payout to Member 2: R5,000 (principal only)
- Remaining balance after payout: R5,028.79 - R5,000 = R28.79 (cumulative interest)

...Month 10:
- By end of cycle, cumulative interest retained ≈ R150-R200
- Year-end: Interest distributed proportionally to all 10 members
```

**Alternatives Considered**:
- **Simple interest (no compounding)**: Rejected; daily compounding maximizes member returns and is industry standard
- **Annual compounding**: Rejected; daily compounding provides better returns and competitive advantage
- **Distribute interest in rotating payouts**: Rejected; retaining interest ensures all members benefit equally from pooled savings

**Implementation Notes**:
- Schedule nightly Azure Function to calculate daily interest for all groups (runs at 2 AM SAST)
- Schedule monthly Azure Function on 1st of each month to capitalize accrued interest
- Display interest breakdown in wallet view: "Principal: R5,000 | Accrued Interest: R14.38 | Total: R5,014.38"
- For rotating payout groups, show interest retention message: "Payout of R5,000 sent. Interest of R28.79 stays in group wallet for everyone's benefit."

---

## 7. Multi-Tenancy Patterns for Group Isolation

### Decision: Use shared database with PostgreSQL Row-Level Security (RLS) for group isolation; each group is a tenant with strict data access policies

**Rationale**:
- **Cost Efficiency**: Shared database (single PostgreSQL instance) is more cost-effective than separate databases per group; with 500-10,000 groups, managing thousands of databases is operationally infeasible
- **Data Isolation**: PostgreSQL RLS enforces access control at database level; even if application logic fails, members cannot access other groups' data
- **Performance**: Single database simplifies querying across groups for admin dashboards and compliance reporting; proper indexing on `group_id` ensures query performance
- **Scalability**: Shared database with RLS scales to 10,000+ groups; can partition database by region/shard if needed post-MVP
- **POPIA Compliance**: RLS ensures strict data segregation per group; members only see data for groups they belong to

**RLS Implementation**:
```sql
-- Enable RLS on financial tables
ALTER TABLE contributions ENABLE ROW LEVEL SECURITY;
ALTER TABLE payouts ENABLE ROW LEVEL SECURITY;
ALTER TABLE group_members ENABLE ROW LEVEL SECURITY;

-- Policy: Members can only see contributions for groups they belong to
CREATE POLICY contributions_member_read ON contributions
    FOR SELECT
    USING (
        group_id IN (
            SELECT group_id 
            FROM group_members 
            WHERE member_id = current_setting('app.current_member_id')::uuid
        )
    );

-- Policy: Members can only insert contributions to their own groups
CREATE POLICY contributions_member_insert ON contributions
    FOR INSERT
    WITH CHECK (
        member_id = current_setting('app.current_member_id')::uuid
        AND group_id IN (
            SELECT group_id 
            FROM group_members 
            WHERE member_id = current_setting('app.current_member_id')::uuid
        )
    );

-- Policy: Only Chairperson/Treasurer can view payouts
CREATE POLICY payouts_admin_read ON payouts
    FOR SELECT
    USING (
        group_id IN (
            SELECT group_id 
            FROM group_members 
            WHERE member_id = current_setting('app.current_member_id')::uuid
              AND role IN ('chairperson', 'treasurer')
        )
    );

-- Policy: Only Chairperson can initiate payouts
CREATE POLICY payouts_chairperson_insert ON payouts
    FOR INSERT
    WITH CHECK (
        initiated_by = current_setting('app.current_member_id')::uuid
        AND group_id IN (
            SELECT group_id 
            FROM group_members 
            WHERE member_id = current_setting('app.current_member_id')::uuid
              AND role = 'chairperson'
        )
    );
```

**Application Layer Implementation**:
```csharp
// Middleware: Set PostgreSQL session variable with authenticated member ID
public class PostgresRlsMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        var memberId = context.User.GetMemberId(); // Extract from JWT
        if (memberId != Guid.Empty)
        {
            // Set PostgreSQL session variable for RLS policies
            await dbContext.Database.ExecuteSqlRawAsync(
                "SET app.current_member_id = {0}", memberId
            );
        }
        await _next(context);
    }
}

// Repository: Queries automatically filtered by RLS
public class ContributionRepository : IContributionRepository
{
    private readonly AppDbContext _dbContext;

    // This query is automatically filtered by RLS policy
    // Member can only see contributions for groups they belong to
    public async Task<List<Contribution>> GetMyContributionsAsync(Guid groupId)
    {
        return await _dbContext.Contributions
            .Where(c => c.GroupId == groupId)
            .OrderByDescending(c => c.TransactionTimestamp)
            .ToListAsync();
        // RLS ensures only contributions for member's groups are returned
    }
}
```

**Alternative Patterns Considered**:
| Pattern | Pros | Cons | Decision |
|---------|------|------|----------|
| **Separate DB per group** | Strongest isolation | 10,000+ databases = operational nightmare, high cost | ❌ Rejected |
| **Schema per group** | Better isolation than shared | Still 10,000+ schemas, complex migrations | ❌ Rejected |
| **Shared DB + RLS** | Cost-efficient, scalable, DB-enforced isolation | Requires careful RLS policy design | ✅ **Selected** |
| **App-level filtering only** | Simple to implement | Vulnerable to code bugs leaking data | ❌ Rejected |

**Alternatives Considered**:
- **Separate database per group**: Rejected due to operational overhead (10,000+ databases) and cost
- **Schema per group**: Rejected due to complexity of managing 10,000+ schemas and migration challenges
- **Application-level filtering only**: Rejected as vulnerable to code bugs; RLS provides defense-in-depth

**Implementation Notes**:
- Test RLS policies thoroughly in development with multiple member accounts
- Monitor RLS policy performance with `EXPLAIN ANALYZE` on complex queries
- Implement admin bypass role for support staff (requires explicit audit logging)
- Document RLS policies in `/docs/security/rls-policies.md` for team reference
- Use connection pooling carefully; ensure `app.current_member_id` is set per request, not per connection

---

## Summary & Next Steps

All Phase 0 research is complete. Key decisions made:

1. **Backend Architecture**: Clean Architecture with DDD, EF Core transactions, idempotency, optimistic concurrency
2. **Database**: PostgreSQL with `NUMERIC(19,4)` for money, jsonb for governance rules, RLS for multi-tenancy
3. **USSD Integration**: HTTP-based MNO gateways, Redis session management, 3-level menu depth, 5-language support
4. **Mobile Offline-First**: MVVM + Repository, Room/CoreData, WorkManager/BackgroundTasks, optimistic UI
5. **POPIA Compliance**: Explicit consent management, data minimization (masked accounts), soft-delete with anonymization
6. **Interest Calculation**: Daily compounding, monthly capitalization, principal-only rotating payouts, interest retained for all
7. **Multi-Tenancy**: Shared DB with PostgreSQL RLS for group isolation

**Proceed to Phase 1**: Generate data-model.md, contracts/rest-api.md, contracts/ussd-flow.md, and quickstart.md.
