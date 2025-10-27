# Medieval Guild Economy — Requirements (DDD)

---

## Scope
- Focus: guild-run, mercantilist economy with tariffs, taxes, dues, fines, permits, and regulated stalls/markets.
- Out of scope for core: modern stock/insurance systems, speculative exchanges; escrow is avoided in favor of holds/liens and notarial bonds.
- Minimal period-appropriate credit: bills of exchange; optional letters of credit.

---

## Bounded Contexts (Domains)
- Treasury and Coinhouses: deposits, transfers, holds/liens, fees, settlement.
- Markets and Guild Trade: stalls, listings, guild permits, pricing rules, seizures, market fees.
- Warehousing and Collateral: storage lots, warehouse receipts, pledges, liquidation.
- Credit Instruments (minimal): bills of exchange; optional letters of credit.
- Organizations and Guilds: membership, dues, fines, sanctions, permits.
- Taxation and Customs: tariff schedules, duty assessment, remittance, exemptions.
- Notary and Contracts: commenda partnerships, bonds, arbitration, protests.
- Reputation and Permits: standing, compliance, tiered permit rights and credit lines.
- Courier/Clearing (minimal): confirmation-based inter-coinhouse settlement without a full clearinghouse.

---

## Core Aggregates and Entities (Conceptual)

### Treasury and Coinhouse
- **CoinHouse** (Aggregate Root): physical location bank; reserves, fee schedule, rules; ties to accounts and postings.
- **Treasury** (Aggregate Root): generalized holder for characters/orgs; available/held balance; policies and limits.
- **CoinHouseAccount** (Entity): owner (character/org), balance, available/held, authorized agents.
- **LedgerEntry** (Value): immutable double-entry posting, refs, timestamps, reason.
- **HoldLien** (Value): reason, authority, created/expiry, disposition (released/forfeit).
- **SettlementInstruction** (Entity): inter-coinhouse pending transfer with courier/notary reference.

### Markets and Guild Trade
- **PlayerStall** (Aggregate Root): listings, fee policy, permit checks, settlement to Treasury.
- **MarketListing** (Entity): item/qty/price, status, policy flags.
- **StallTransaction** (Entity): sale record, fees, tariffs, settlement references.
- **TradePermit** (Entity): issuing guild, scope (goods/region), validity, status.

### Organizations and Guilds
- **Guild** (Aggregate Root): membership, roles/privileges, rules; linked to treasury.
- **GuildTreasury** (Aggregate Root): specialization of Treasury; dues/fine policies, grants.
- **DuesAssessment** (Entity): periodic assessment rule instance and posting status.
- **Fine** (Entity): infraction, amount, escalation and settlement state.
- **Sanction** (Entity): restrictions (sell ban, permit revoke, seizure).

### Taxation and Customs
- **TariffSchedule** (Value): rule set keyed by good/origin/guild status/permit tier.
- **DutyAssessment** (Entity): duty computation result and remittance state to tax authority.
- **TaxAuthority** (Aggregate Root): receives duties, audits remittances.

### Warehousing and Collateral
- **Warehouse** (Aggregate Root): storage lots, custody policies, lien/seizure hooks.
- **WarehouseReceipt** (Value): negotiable proof of title to a storage lot or bundle.
- **Pledge** (Entity): collateralization of receipts to a Treasury/loan/credit line.

### Credit Instruments (Minimal)
- **BillOfExchange** (Aggregate Root): drawer, drawee, payee, amount, issue/maturity; endorsements; status (Issued, Presented, Redeemed, Refused, Protested).
- **Endorsement** (Entity/Value): transfer of title chain and dates.

### Notary and Contracts
- **NotarizedContract** (Aggregate Root): commenda terms, bonds/penalties, arbiters; execution events.
- **Bond** (Value): amount and forfeiture conditions.

### Reputation and Permits
- **CreditProfile** (Aggregate Root): credit limits, compliance history, defaults; ties to permits.
- **PermitTier** (Value): thresholds for privileges and limits.

---

## Invariants and Policies (High Level)
- Every monetary movement is recorded as balanced LedgerEntries within a CoinHouse.
- Holds/liens reduce available balance and cannot create negative available funds unless policy allows.
- Guild rules override market actions: no listing or settlement without required TradePermit in good standing.
- Duty and fee policies must be applied before final settlement.
- Warehouse receipts pledged as collateral are immobilized until pledge is released or liquidated.
- Bills of exchange settle only at or after maturity unless discounted under policy.

---

## Requirements (Given–When–Then)

### Guild Dues, Fines, Sanctions
1) Given a member in good standing, when periodic dues are assessed, then the member’s Treasury is debited and the GuildTreasury is credited with a LedgerEntry pair and an assessment record.
2) Given unpaid dues past grace, when escalation runs, then a Fine is created and a Sanction may suspend the member’s TradePermit until payment.
3) Given an infraction is adjudicated (e.g., unpermitted selling), when the decision posts, then a Fine is recorded and a HoldLien is placed on the violator’s Treasury.

### Market Stalls Under Guild Regulation
4) Given a seller attempts to list goods at a PlayerStall, when the seller lacks a valid TradePermit for those goods/region, then the listing is rejected with reason `PermitRequired`.
5) Given a listing violates guild price policy, when publish is requested, then it is blocked and a warning is recorded referencing the policy.
6) Given a sale completes at a PlayerStall, when settlement is posted, then the seller’s Treasury is credited minus guild market fees and any applicable duties; LedgerEntries reference the StallTransaction.
7) Given a seller operates while under sanction, when a stall action is attempted, then the action is denied and the event is logged for guild review.

### Tariffs and Duties
8) Given a sale involves dutiable goods per TariffSchedule, when DutyAssessment is computed, then duties are debited from the configured party (buyer or seller) and credited to the TaxAuthority’s Treasury.
9) Given a cargo manifest is declared at port, when duties are unpaid, then a HoldLien is created on the importer’s Treasury or on the stored goods, preventing release.

### Treasury Transfers and Holds
10) Given two Treasuries at the same CoinHouse, when an internal transfer executes, then balanced LedgerEntries post and no external SettlementInstruction is created.
11) Given Treasuries at different CoinHouses, when a transfer executes, then a SettlementInstruction is created and both sides record pending status until courier/notary confirmation closes it.
12) Given a HoldLien exists for investigation, when release is authorized, then held funds move to available or are forfeited to the specified authority and the lien is archived.

### Warehousing and Collateral
13) Given a WarehouseReceipt is pledged to a Treasury, when a credit line is opened, then the Treasury’s available credit increases and the receipt is immobilized until release or liquidation.
14) Given a default past its cure period, when liquidation runs, then pledged goods are auctioned and proceeds settle the outstanding balance, fees, and penalties in priority order.

### Bills of Exchange (Credit Instrument)
15) Given a BillOfExchange reaches maturity, when presented by the lawful holder, then the drawee’s Treasury is debited and the holder’s Treasury is credited; the bill becomes `Redeemed`.
16) Given the drawee refuses for cause, when a protest is recorded by a notary, then the drawer’s CreditProfile is updated and recourse applies per policy.

### Notary and Commenda
17) Given a notarized commenda with profit split, when voyage profit is posted, then profits are distributed per terms to investor and managing merchant Treasuries; bonds forfeit on breach.
18) Given a contract specifies penalties for non-performance, when breach is confirmed, then a Fine posts or a Bond is forfeited and LedgerEntries are recorded.

### Reputation and Permits
19) Given repeated on-time payments and compliance, when CreditProfile recalculates, then permit tier or credit limits increase within defined caps.
20) Given active sanctions or defaults, when a permit renewal is requested, then renewal is denied or downgraded until issues are resolved.

### Courier/Clearing (Minimal)
21) Given multiple outstanding SettlementInstructions between CoinHouses, when the settlement window closes, then instructions are confirmed by courier/notary and balances are posted; failures are flagged for guild oversight.

---

## Optional Nice‑to‑Haves (Deferred)
- Marine insurance and risk pooling (policies, claims, salvage).
- Letters of credit beyond simple bills of exchange.
- Formal clearinghouse/netting between CoinHouses.
- Advanced auctions/market mechanisms beyond guild stalls.
- Voyage risk classes and caravan escorts influencing fees.

---

## Implementation Alignment (Repo)
- Place services and aggregates under Features/WorldEngine/Economy (Banks, Markets, Taxation, Instruments, Organizations, Warehousing).
- Persist under Database/Entities/Economy (Treasuries, Shops, Taxation, Storage) reusing existing CoinHouse/CoinHouseAccount/Transaction entities.
- Enforce TradePermit checks at PlayerStall publication and settlement.
- Apply TariffSchedule and market fees before settlement postings.
- Use HoldLien and SettlementInstruction instead of escrow; prefer notarial bonds for conditional obligations.

### Non‑Functional Considerations
- Auditable postings (immutable LedgerEntry, correlation IDs).
- Idempotent settlement operations and retries.
- Config-driven policy tables for tariffs, fees, and permit tiers.
- Clear error codes for player-facing feedback (`PermitRequired`, `Sanctioned`, `FundsOnHold`, `DutyUnpaid`).

---

## Subsystem Boundaries and Placement

### Economy/Banks (existing)
- **Owns:** CoinHouse, Treasury, CoinHouseAccount, LedgerEntry, HoldLien, SettlementInstruction, FeePolicy, TransferService.
- **Code:** Features/WorldEngine/Economy/Banks
- **Data:** Database/Entities/Economy/Treasuries/*

### Economy/Markets (existing)
- **Owns:** PlayerStall, MarketListing, StallTransaction, MarketFeePolicy.
- **Depends on:** Organizations/Permits (TradePermit), Economy/Taxation (DutyAssessment), Organizations/Sanctions.
- **Code:** Features/WorldEngine/Economy/Markets
- **Data:** Database/Entities/Economy/Shops/*

### Economy/Taxation (new)
- **Owns:** TariffSchedule, DutyAssessment, TaxAuthority, DutyComputationService.
- **Code:** Features/WorldEngine/Economy/Taxation
- **Data:** Database/Entities/Economy/Taxation (add folder)

### Organizations (existing)
- **Owns:** Guild, Membership, Roles, DuesAssessment, Fine, Sanction, GuildTreasury (domain policy), TradePermit, InstructorCertification, PermitTier.
- **Code:** Features/WorldEngine/Organizations
- **Data:** Database/Entities/Organization.* and Database/Entities/Economy/Treasuries/* for GuildTreasury

### KnowledgeSubsystem (existing)
- **Owns:** KnowledgePointWallet, KnowledgeExposure, TrainingSession, StudyTask, Enrollment, Curriculum (definition), KnowledgeGrantService.
- **Code:** Features/WorldEngine/KnowledgeSubsystem
- **Data:** Database/Entities/Economy/PersistentCharacterKnowledge.cs plus new training/enrollment tables when implemented.

### Warehousing (new)
- **Owns:** Warehouse, WarehouseReceipt, Pledge, WarehouseService, LiquidationPolicy.
- **Code:** Features/WorldEngine/Warehousing
- **Data:** Database/Entities/Storage.*, plus pledge relations.

### Economy/Instruments (new)
- **Owns:** BillOfExchange, Endorsement; optional LettersOfCredit.
- **Code:** Features/WorldEngine/Economy/Instruments
- **Data:** Database/Entities/Economy/Instruments (add folder)

### Notary (new)
- **Owns:** NotarizedContract, Bond, Protest, ArbitrationService.
- **Code:** Features/WorldEngine/Notary
- **Data:** Database/Entities/Legal/* (add folder)

### Reputation and Credit (existing + new)
- **Owns:** CreditProfile (Economy/Credit), ties into CharacterReputation and OrganizationReputation.
- **Code:** Features/WorldEngine/Economy/Credit (add), Features/WorldEngine/Organizations/Reputation
- **Data:** Database/Entities/Economy/Credit (add), Database/Entities/CharacterReputation.cs, OrganizationReputation.cs

### Settlement/Courier (minimal; under Banks)
- **Owns:** SettlementWindow, CourierConfirmation events, minimal netting.
- **Code:** Features/WorldEngine/Economy/Banks/Settlement
- **Data:** within Treasury/CoinHouse tables and a light SettlementInstruction table.

### Cross‑Cutting Concerns
- **Policy configuration:** Features/WorldEngine/Economy/Policies (fees, tariffs) with JSON/YAML loaders via EconomyLoaderService.
- **Event contracts:** Features/WorldEngine/SharedKernel/Events for Enrollment/KnowledgeAwarded/Settlement events.
- **Access control:** Organizations govern permits and sanctions used by Markets and KnowledgeSubsystem.

---

## Guild Knowledge Exchange

### Overview
- Knowledge points are a spendable capacity; knowledge is only awarded when a character is presented the topic and has points to consume at award time.
- Presentation sources: certified instructors (NPC/PC), manuscripts (rank-gated), and live demonstrations (synchronous).
- Sessions can be asynchronous (self-paced study with NPC trainer or manuscript) or synchronous (scheduled demonstration/training).

### Core Concepts (Conceptual)
- **KnowledgePointWallet:** unspent points available to consume.
- **KnowledgeExposure:** proof that a character has been presented a specific topic/tier with a source and timestamp; used to gate spending.
- **TrainingSession (Sync):** scheduled event with roster, instructor, venue, fee, and attendance telemetry.
- **StudyTask (Async):** initiated study with an NPC trainer or a manuscript; time-on-task recorded.
- **InstructorCertification:** guild-issued authorization to teach specified topics/tiers; revocable.
- **Curriculum:** ordered set of topics/tiers with prerequisites and optional evaluation hooks.
- **Manuscript:** itemized source of presentation; requires minimum profession rank to study.
- **Enrollment:** a character’s participation record for a session or study task.

### Invariants and Policies
- No knowledge award without both: a valid KnowledgeExposure and sufficient KnowledgePointWallet balance at award time.
- Points are consumed atomically with award posting; no partial awards.
- Fees/stipends settle before award is finalized.
- Uncertified teaching or sanctioned instructors cannot create exposure or sessions.
- Prerequisites and guild membership/permit tier gates are enforced before enrollment.
- Asynchronous study must meet minimum time-on-task; synchronous sessions must meet attendance thresholds.

### Session Types
- **Asynchronous (NPC Trainer):** player initiates a study task with a certified NPC; time gate and fee apply.
- **Asynchronous (Manuscript):** player studies a manuscript if rank allows; may consume durability/charges.
- **Synchronous (Demonstration/Class):** scheduled, capacity-limited; attendance is required for award eligibility.

### Economy Hooks
- **TrainingFee:** debited from attendee and credited to guild or instructor split before award.
- **Stipend/Subsidy:** optional payout from GuildTreasury on completion when policy grants sponsorship.
- **Fine/Sanction:** posted for cheating, botting, or unlicensed teaching; blocks further sessions until cleared.

### Requirements (Given–When–Then)

#### 1) Presentation Gating and Spend
- Given a character has unspent knowledge points but no exposure for Topic T, when they attempt to learn T, then the award is rejected with reason `NotPresented`.
- Given a character has a valid exposure for Topic T and sufficient points, when completion is recorded, then points are consumed and knowledge for T is awarded.

#### 2) Asynchronous Trainer Study
- Given a certified NPC instructor for Topic T, when a player initiates an async study task and pays the fee, then a StudyTask and Exposure are created.
- Given the player completes required study time, when completion is posted and points are sufficient, then knowledge is awarded and the fee ledger is confirmed.

#### 3) Manuscript Study
- Given a manuscript for Topic T requires rank R, when a player below R attempts to study, then the attempt is rejected with reason `RankInsufficient`.
- Given rank R or higher and a valid manuscript, when study completes and points are sufficient, then knowledge is awarded and the manuscript’s usage is decremented per policy.

#### 4) Synchronous Demonstration
- Given a scheduled class with capacity and fee, when a player enrolls and pays, then enrollment is confirmed and attendance is tracked.
- Given attendance meets thresholds and points are sufficient, when the session ends, then knowledge is awarded; otherwise the enrollment is marked `AttendanceInsufficient`.

#### 5) Certification and Sanctions
- Given a player lacks InstructorCertification for Topic T, when they attempt to host a session for T, then creation is rejected with reason `InstructorNotCertified`.
- Given an instructor is sanctioned, when any session action is attempted, then it is denied and logged.

#### 6) Fees, Stipends, and Award Ordering
- Given a fee is due for a session, when completion is evaluated, then the fee must be posted successfully before the award occurs.
- Given a guild sponsors Topic T, when the trainee completes and is awarded, then a stipend is paid from GuildTreasury to the instructor per split policy.

#### 7) Prerequisites and Curriculum
- Given Topic T2 requires T1, when completion for T2 is attempted without T1, then award is rejected with reason `PrerequisiteMissing`.
- Given a curriculum step allows acceleration, when a player spends points and passes evaluation, then required time is reduced per policy and award proceeds.

### Events and Processing
- EnrollmentRequested → EnrollmentConfirmed/Rejected
- StudyStarted/AttendanceRecorded → StudyCompleted
- TrainingFeePosted/TrainingStipendPosted
- KnowledgePresented (exposure created)
- KnowledgeAwarded (points debited, knowledge persisted)
- CertificationIssued/Revoked
- SanctionApplied/Cleared

### Data Alignment (Repo)
- Persist awarded knowledge in `Database/Entities/Economy/PersistentCharacterKnowledge.cs`.
- Track membership/permit gates with `Database/Entities/Organization.cs` and related membership data.
- Post fees/stipends via Treasury entities under `Database/Entities/Economy/Treasuries/*`.
- Manuscripts integrate with Items once defined; store exposure/study/enrollment records under `Database/Entities/Economy/*` when implemented.
