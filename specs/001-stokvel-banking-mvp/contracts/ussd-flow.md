# USSD Flow Documentation: Digital Stokvel Banking MVP

**Shortcode**: `*120*78653#` (spells "STOKVEL")  
**Session Timeout**: 120 seconds  
**Max Menu Depth**: 3 levels  
**Languages**: English (en), isiZulu (zu), Sesotho (st), Xhosa (xh), Afrikaans (af)  
**Date**: 2026-03-24

---

## Table of Contents

1. [USSD Architecture](#ussd-architecture)
2. [Language Selection](#language-selection)
3. [Main Menu](#main-menu)
4. [Flow 1: Make Contribution](#flow-1-make-contribution)
5. [Flow 2: Check Balance](#flow-2-check-balance)
6. [Flow 3: View Transactions](#flow-3-view-transactions)
7. [Flow 4: Help & Support](#flow-4-help--support)
8. [Error Handling](#error-handling)
9. [Session Management](#session-management)

---

## USSD Architecture

**Session State Storage**: Redis (120-second TTL)  
**Authentication**: Bank PIN (validated once per session)  
**MNO Gateways**: Vodacom, MTN, Cell C, Telkom (HTTP-based APIs)  
**Response Format**: Plain text (max 180 characters per screen)

**Session State Object**:
```json
{
  "sessionId": "ussd_sess_a1b2c3d4e5f6",
  "phoneNumber": "+27821234567",
  "memberId": "550e8400-e29b-41d4-a716-446655440000",
  "language": "zu",
  "authenticated": true,
  "currentMenu": "main",
  "currentLevel": 1,
  "selectedGroupId": null,
  "inputHistory": [],
  "createdAt": "2026-03-24T10:00:00Z"
}
```

---

## Language Selection

**Trigger**: First-time user dials `*120*78653#`

**Level 1 - Language Selection** (English default):
```
Welcome to Digital Stokvel!
Select your language:
1. English
2. isiZulu
3. Sesotho
4. Xhosa
5. Afrikaans
```

**User Input**: `2` (selects isiZulu)

**Response** (Level 1 - Language Confirmed):
```
Ulimi lwakho: isiZulu ✓
Uyacelwa ukufaka iPIN yakho yebanki:
```

**Note**: Language preference is stored in member profile and reused in future sessions.

---

## Main Menu

**Level 1 - Main Menu** (isiZulu example):

**Trigger**: User dials `*120*78653#` and authenticates

```
Yebo! Khetha okufunayo:
1. Faka imali (Pay)
2. Bheka imali (Balance)
3. Umlando (History)
4. Usizo (Help)
```

**User Input**: `1`, `2`, `3`, or `4`

---

## Flow 1: Make Contribution

### Level 2 - Select Group

**User pressed** `1` (Faka imali / Pay Contribution)

**Response** (Level 2):
```
Khetha iqembu:
1. Ntombizodwa Stokvel
2. Ubuntu Savings
3. Thembisa Group
```

**User Input**: `1` (selects Ntombizodwa Stokvel)

---

### Level 3 - Confirm Payment

**Response** (Level 3):
```
Qinisekisa:
Iqembu: Ntombizodwa Stokvel
Imali: R500.00
Inombolo yakho: ****1234

1=Yebo (Yes)
2=Cha (No)
```

**User Input**: `1` (confirms)

---

### Level 3b - PIN Authentication

**Response** (Level 3b):
```
Faka iPIN yakho yebanki:
(4 digits)
```

**User Input**: `1234` (bank PIN)

**Backend Process**:
1. Validate PIN with bank authentication service
2. Create contribution transaction with idempotency key
3. Deduct funds from member's account
4. Update group wallet balance
5. Log transaction to immutable ledger

---

### Final Response (Terminal)

**Success**:
```
✓ Impumelelo! (Success!)
Imali: R500.00
Inombolo: STK-2026-03-24-001234
Iqembu: Ntombizodwa Stokvel
Isamba: R5,514.38

Ngiyabonga! (Thank you!)
```

**Failure - Insufficient Funds**:
```
✗ Ehlulekile
Imali yakho ayenele.
Oyidingayo: R500.00
Oyinayo: R350.00

Sicela uzame futhi.
(Press 1 to try again)
```

**Failure - Network Timeout**:
```
⏱ Kulindile...
Ukulinganya kwakho kusahlolwa.
Sicela ubheke ngemva kwemizuzu emi-5.
Inombolo: STK-PEND-20260324
```

---

## Flow 2: Check Balance

### Level 2 - Select Group

**User pressed** `2` (Bheka imali / Check Balance)

**Response** (Level 2):
```
Khetha iqembu:
1. Ntombizodwa Stokvel
2. Ubuntu Savings
3. Thembisa Group
```

**User Input**: `1` (selects Ntombizodwa Stokvel)

---

### Final Response (Terminal)

**Response** (Level 3 - Group Balance):
```
Iqembu: Ntombizodwa Stokvel
Isamba: R5,514.38
  - Inhloko: R5,000.00
  - Inzuzo: R514.38
Izinga lenzuzo: 4.5%

Okwakho: R500.00 (Paid ✓)
Okulandelayo: 2026-04-01
```

---

## Flow 3: View Transactions

### Level 2 - Recent Transactions

**User pressed** `3` (Umlando / History)

**Response** (Level 2 - Last 5 Transactions):
```
Umlando wakho:
1. R500 - 2026-03-24 ✓
2. R500 - 2026-02-24 ✓
3. R500 - 2026-01-24 ✓
4. R500 - 2025-12-24 ✓
5. R500 - 2025-11-24 ✓

Isamba: R2,500
(Press 0 for Main Menu)
```

**User Input**: `0` (returns to main menu)

---

## Flow 4: Help & Support

### Level 2 - Help Menu

**User pressed** `4` (Usizo / Help)

**Response** (Level 2):
```
Usizo:
1. Ungakulingana kanjani?
2. Uhlelo lokukhokha
3. Izindleko
4. Xhumana nathi

(How to pay, Schedule, Fees, Contact)
```

**User Input**: `4` (Contact us)

---

### Final Response (Terminal)

**Response** (Level 3 - Contact Information):
```
Xhumana nathi:
WhatsApp: 082 123 4567
Email: support@stokvel.bank
Mon-Fri: 8AM-5PM
Sat: 9AM-1PM

Ungashayela mina:
0860 STOKVEL
```

---

## Error Handling

### Invalid Input

**User Input**: `9` (invalid option)

**Response**:
```
✗ Khetha okufanele (Invalid)
Sicela ukhethe 1-4 kuphela.

<Previous menu repeated>
```

---

### Session Timeout

**Scenario**: User inactive for 120+ seconds

**Response** (Next input attempt):
```
⏱ Isikhathi siphelile
Sicela ushayele futhi:
*120*78653#
```

**Backend**: Session state cleared from Redis.

---

### Network Interruption

**Scenario**: User loses network mid-transaction

**User Action**: Redials within 120 seconds

**Response** (Session Resume):
```
Siyabuyela lapho wasishiya...
<Previous menu state restored>
```

**Backend**: Redis session state retrieved; user continues from last menu level.

---

### Bank PIN Error

**Scenario**: User enters incorrect PIN

**Response** (Attempt 1/3):
```
✗ iPIN ephuthile
Zilinge: 1 ze-3
Sicela uzame futhi:
```

**Response** (Attempt 3/3 - Account Locked):
```
✗ iPIN ephuthile
Zilinge: 3 ze-3
I-akhawunti yakho ivaliwe.
Xhumana no-0860 STOKVEL
```

**Backend**: Account locked for 1 hour; member must call support to unlock.

---

## Session Management

### Session Creation

**Trigger**: User dials `*120*78653#`

**Process**:
1. MNO gateway sends HTTP request: `POST /ussd/callback`
2. Backend creates session in Redis (120s TTL)
3. Return language selection or main menu (if language already set)

---

### Session State Update

**Trigger**: User selects menu option (e.g., presses `1`)

**Process**:
1. MNO gateway sends user input: `POST /ussd/callback`
2. Backend retrieves session from Redis by `sessionId`
3. Update `currentMenu`, `currentLevel`, `inputHistory`
4. Return next menu or execute action
5. Update session state in Redis (reset TTL to 120s)

---

### Session Termination

**Triggers**:
- User completes transaction (terminal response with "END" action)
- Session timeout (120s inactivity)
- User cancels (presses 0 repeatedly or closes USSD)

**Process**:
1. Delete session from Redis
2. Log session summary to audit log (duration, menus visited, outcome)

---

## Localization Examples

### Main Menu - All Languages

**English**:
```
Hi! Choose an option:
1. Pay Contribution
2. Check Balance
3. Transaction History
4. Help
```

**isiZulu**:
```
Yebo! Khetha okufunayo:
1. Faka imali
2. Bheka imali
3. Umlando
4. Usizo
```

**Sesotho**:
```
Lumela! Kgetha kgetho:
1. Lefa
2. Sheba tjhelete
3. Histori
4. Thuso
```

**Xhosa**:
```
Molo! Khetha ukhetho:
1. Hlawula
2. Jonga imali
3. Imbali
4. Uncedo
```

**Afrikaans**:
```
Hallo! Kies 'n opsie:
1. Betaal bydrae
2. Kontroleer balans
3. Transaksie geskiedenis
4. Hulp
```

---

## USSD Performance Metrics

**Success Criteria** (per FR-037, SC-009):
- Max 3-level menu depth: ✓ All flows meet this constraint
- 95% of transactions complete in <30 seconds
- No user should get lost navigating menus

**Monitoring**:
- Track completion rate per flow (Pay, Balance, History)
- Track average session duration
- Track error rates (invalid input, timeouts, PIN failures)
- Send alerts if completion rate drops below 90%

---

## USSD vs. Mobile App Feature Parity

| Feature | USSD | Mobile App |
|---------|------|------------|
| Make contribution | ✓ | ✓ |
| Check balance | ✓ | ✓ |
| View recent transactions | ✓ (last 5) | ✓ (full history with filters) |
| Payout notification | ✓ (SMS fallback) | ✓ (push notification) |
| Create group | ✗ | ✓ |
| Invite members | ✗ | ✓ |
| View full ledger | ✗ | ✓ |
| Voting on proposals | ✗ | ✓ |
| Dispute resolution | ✗ | ✓ |

**Rationale**: USSD focuses on core transactional flows (contribute, balance, history) per FR-037. Administrative functions (group management, governance) require richer UI and are mobile/web-only.

---

**End of USSD Flow Documentation**
