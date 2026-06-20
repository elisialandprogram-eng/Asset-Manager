# Eternal Kingdoms — Monetization Bible

> **SPECIFICATION FREEZE DOCUMENT — IMMUTABLE REFERENCE ARCHITECTURE**
> Version 1.0 — June 2026

---

## 1. Free-to-Play Philosophy

Eternal Kingdoms is a free-to-play game with genuine depth and competitive viability for non-paying players. Monetization is structured around three principles:

### Principle 1: Time, Not Power

Premium purchases accelerate progression along the same path every player walks. They cannot purchase stats, attack bonuses, troop attack multipliers, or any numerical advantage inaccessible to free players. A free player who invests 90 days reaches the same ceiling as a paying player — the paying player arrives faster.

**The line:** Speedups, cosmetics, and convenience are monetizable. Combat power bonuses, exclusive high-tier units, and pay-walled endgame content are not.

### Principle 2: Cosmetics Are Genuinely Optional

Skins, emblems, visual effects, and castle appearances provide zero gameplay advantage. They are sold as standalone cosmetic items or bundled into season battle passes. No player ever feels mechanically inferior for not buying cosmetics.

### Principle 3: Blockchain Ownership Is an Opt-In Premium Layer

NFT Land plots, Hero NFTs, and Dragoon NFTs represent real asset ownership and real economic value. Their economic benefits (resource siphoning, dividends) are significant but bounded — a land-owning player does not gain a combat advantage over a non-landowning player. The NFT economy is a parallel wealth track, not a power track.

---

## 2. Premium Currency

### Eternal Gems (EG)

Eternal Gems are the premium currency of Eternal Kingdoms. They are purchased with real money and spent on in-game items.

### Purchase Tiers

| Package | Gems | Price (USD) | Bonus |
|---------|------|-------------|-------|
| Starter Pack | 500 | $4.99 | None |
| Adventurer Pack | 1,200 | $9.99 | +200 bonus (17% more) |
| Baron Pack | 2,800 | $19.99 | +600 bonus (21% more) |
| Duke Pack | 6,500 | $39.99 | +1,500 bonus (23% more) |
| King Pack | 15,000 | $79.99 | +4,500 bonus (30% more) |
| Emperor Pack | 35,000 | $149.99 | +12,500 bonus (36% more) |

Pricing is subject to regional parity adjustments. Gems are non-refundable and non-transferable between accounts.

### First Purchase Bonus

Every account receives a first-purchase bonus: +100% gems on their first Eternal Gems transaction (any tier). This is a one-time offer visible prominently in the shop.

---

## 3. Speedups

Speedups are the primary gem expenditure category. They reduce construction, research, training, and healing timers.

### Speedup Pricing

| Speedup Type | Gem Cost |
|-------------|---------|
| 1-minute Universal Speedup | 2 EG |
| 1-hour Universal Speedup | 80 EG |
| 3-hour Universal Speedup | 200 EG |
| 8-hour Universal Speedup | 480 EG |
| 24-hour Universal Speedup | 1,200 EG |
| 8-hour Training Speedup | 400 EG |
| 8-hour Medical Speedup | 320 EG |

**Bundle discount:** 10× 1-hour speedups sold for 720 EG (10% savings vs. individual).

### Weekly Speedup Cap

No player may apply more than 72 hours of speedups per category (construction, research, training) per 7-day rolling window. This is enforced server-side. This cap prevents unlimited gem spending from trivializing progression.

### Speedup Acquisition (Free)

Speedups are obtainable without spending:
- Daily login (1-minute to 30-minute speedups)
- Quest completion
- PvE monster kills (drop table)
- Alliance help (timer reductions)
- Season milestone rewards

The free speedup supply is sufficient for a free player to maintain active queues, but insufficient to match the progression speed of a consistent spender.

---

## 4. Cosmetics

Cosmetics provide zero gameplay advantage. They are permanent unlocks on the purchasing account.

### Castle Skins

Castle skins alter the visual appearance of the player's kingdom in the Unity client.

| Tier | Description | Price |
|------|------------|-------|
| Standard | Recolor / minor visual change | 200 EG |
| Premium | Full architecture replacement | 800 EG |
| Legendary | Animated effects, unique architecture | 2,500 EG |
| Season Exclusive | Only available during the season's battle pass | Included in Battle Pass |

Castle skins are registered in the `asset_registry` table with a unique `assetId` and tied to the player account — not tradeable between accounts in the base game. NFT version (tradeable) available at Legendary tier.

### Kingdom Name Cosmetics

- Custom Kingdom Name Change: 500 EG (once per 30 days)
- Custom Title (displays before username): 300 EG
- Kingdom Flag Design: 200 EG

### UI Themes

- Chat themes and color schemes: 100 EG
- Battle report visual style: 150 EG
- World map marker style (personal kingdom icon): 100 EG

### Alliance Cosmetics (Purchased by R5)

- Custom Alliance Emblem (from premium library): 1,000 EG
- Alliance War Banner (displayed at fortresses): 800 EG
- Alliance Announcement Sound (unique fanfare): 500 EG

---

## 5. Skins

Skins are cosmetic overlays for specific game entities. They are distinct from castle skins in that they apply to individual units or buildings.

### Building Skins

Applied to specific building types (e.g., Barracks, Academy). Visual appearance changes without altering function.

| Building Tier | Price |
|-------------|-------|
| Standard | 150 EG |
| Premium | 500 EG |

### Troop Skins (Unity)

Troop skins change the visual appearance of troops in march animations and kingdom views.

| Troop Tier | Price |
|-----------|-------|
| Infantry Skin | 300 EG per tier |
| Cavalry Skin | 300 EG per tier |
| Ranged Skin | 300 EG per tier |

Troop skins apply to all troops of that type — not per-troop. Purchasing a Cavalry Skin means all cavalry trained by that player use the skin.

### Dragon Skins

Dragoon visual skins (separate from dragoon NFT stats):
- Standard Reskin: 600 EG
- Legendary Reskin (animated): 2,000 EG
- Special Event Skin: 3,000 EG (limited run)

---

## 6. NFTs

NFTs are the blockchain-native premium asset layer. See Blockchain Architecture Bible for contract specifications.

### NFT Categories

| NFT Type | Standard | Price Range | Notes |
|---------|----------|-------------|-------|
| Land Plot (32×32 tile) | ERC-721 | Auction-based | Fixed 4,096 supply |
| Dragoon | ERC-721 | Variable | All dragoons are NFTs |
| Hero (Tier 4+) | ERC-721 | 500 Crystal mint fee | Minted at Celestial Forge |
| Seasonal Badge | ERC-721 | Earned, not purchased | Season ranking reward |
| Resource Pack | ERC-1155 | Market price | Tokenized resources |
| Dragoon Egg | ERC-1155 | Auction/event | Breeds first-gen dragoons |

### Land Plot Initial Sale

The 4,096 Land plots are sold via a phased auction:
- **Phase 1 (First 512 plots, Zone 0–1):** English auction. Starting bid: 0.1 ETH equivalent in MATIC. High-value central plots expected to reach significant premiums.
- **Phase 2 (Next 1,024 plots, Zone 2–3):** Dutch auction. Starting price: 0.05 ETH equivalent, declining 2% per hour until sold.
- **Phase 3 (Remaining 2,560 plots, Zone 4–7):** Fixed-price sale at 0.01 ETH equivalent in MATIC.

All proceeds flow to the Protocol Treasury.

### NFT Royalties

Secondary market royalties (ERC-2981):
- Land Plots: 3% to Protocol Treasury
- Dragons/Dragoons: 5% to Protocol Treasury (2.5% standard + 2.5% breeding incentive)
- Heroes: 2.5% to Protocol Treasury

---

## 7. Marketplace Fees

The Eternal Kingdoms in-game marketplace charges the following fees:

| Operation | Fee | Recipient |
|-----------|-----|----------|
| Listing (fixed price) | 0% (no fee) | N/A |
| Sale (fixed price) | 5% of sale price | Protocol Treasury |
| Auction listing | 0% | N/A |
| Auction settlement | 5% of final bid | Protocol Treasury |
| Resource pack creation | 0.5% burn | Deflationary burn |
| Crystal withdrawal (tokenize) | 0.5% burn | Deflationary burn |

Protocol Treasury funds:
- Game development team (70%)
- Paymaster gas funding (15%)
- Dividend pool top-up (10%)
- Community grants (5%)

---

## 8. Tax Structures

### Crystal Economy Taxes

| Event | Tax Rate | Recipient |
|-------|---------|----------|
| Crystal gathered | 2% | Dividend pool |
| Crystal token mint | 0.5% | Burned |
| Crystal marketplace sale | 5% | Protocol Treasury |
| Crystal breeding cost | 100% | Burned |

### NFT Economy Taxes

| Event | Tax Rate | Recipient |
|-------|---------|----------|
| NFT primary sale | 100% to protocol | Protocol Treasury |
| NFT secondary sale | 5% royalty | Protocol Treasury (2.5%) + Creator (2.5%) |
| Land siphon | 5–15% of gathered resources | Land owner (in-game) |
| Dividend pool contribution | 2% of all crystal | Land NFT holders |

### Governance of Tax Rates

Crystal tax rates and dividend allocation percentages are governance-controlled parameters. Future DAO governance (via Crystal token staking) will allow community voting on tax rates within defined bounds:
- Minimum crystal gather tax: 1%
- Maximum crystal gather tax: 5%
- Minimum dividend pool allocation: 50% of tax
- Maximum marketplace fee: 7.5%

---

## 9. Dragons (Premium Dragoon System)

### Premium Dragoon Acquisition

First-generation dragoons are obtained through Dragoon Eggs. Eggs are distributed via:
1. **Season battle pass:** 1 Common egg per pass tier
2. **Season ranking reward:** 1 Rare egg (top 100), 1 Epic egg (top 10)
3. **Event completion:** 1 Common egg per major event completion
4. **Premium egg purchase:** See pricing below

### Dragoon Egg Pricing

| Egg Tier | Gem Price | MATIC Price | Expected Rarity |
|----------|-----------|------------|-----------------|
| Common Egg | 5,000 EG | N/A | Common or Uncommon dragoon |
| Rare Egg | 15,000 EG | 50 MATIC | Uncommon or Rare dragoon |
| Epic Egg | 40,000 EG | 150 MATIC | Rare or Epic dragoon |
| Legendary Egg | Not sold — earned only | N/A | Guaranteed Legendary |

Legendary eggs are not purchasable. They are season ranking rewards (top 10 worldwide), Congress victory prizes, and Ancient Dragon hunt rewards. This ensures Legendary dragoons remain genuinely earned.

### Dragoon Breeding Revenue

Breeding two dragoons costs 200 Crystal + a breeding fee:
```
breeding_fee_crystal = 50 × (parent_A_breedCount + parent_B_breedCount + 2)
```

This fee is burned (deflationary). High-generation dragoons become increasingly expensive to breed.

---

## 10. Battle Passes

### Season Battle Pass

The Season Battle Pass is a tiered reward track running for the duration of the season (~90 days).

**Pass Types:**

| Pass | Price | Description |
|------|-------|------------|
| Free Track | Free | Available to all players. Basic rewards. |
| Noble Pass | 1,200 EG | Premium track. Better rewards. |
| Royal Pass | 2,800 EG | All Noble rewards + exclusive skins + dragoon egg. |

### Pass Tier System

The pass has 50 tiers. Players advance tiers through:
- Daily login (+1 tier per day)
- Quest completion (+0.5 tier per quest)
- Event participation (+1–5 tiers per major event)

A player who logs in daily and completes standard quests will complete all 50 tiers before the season ends.

### Sample Pass Rewards by Track

| Tier | Free Track | Noble Pass | Royal Pass |
|------|-----------|-----------|-----------|
| 1 | 5× 1-min speedup | 10× 1-min speedup + 500 EG | Same as Noble |
| 10 | 2× 1-hour speedup | 5× 1-hour speedup + building skin | Same + exclusive skin |
| 25 | 1× 8-hour speedup | 3× 8-hour speedup + 1,000 EG | Same + 1 Rare Hero shard |
| 50 | Seasonal badge | Seasonal badge + 5× 24-hour speedup + Common Egg | Same + 1 Rare Egg + Exclusive Emblem |

### Battle Pass VIP Upgrades

Players who purchase the Royal Pass also unlock VIP services for the season:
- Priority construction queue notification
- Detailed battle analytics dashboard
- Extended battle report history (90 days vs. 30 days)
- 1 kingdom rename per season

---

## 11. Events

Events are limited-time in-game experiences that provide premium rewards and drive engagement.

### Event Types

| Event | Duration | Rewards | Monetization |
|-------|----------|---------|-------------|
| Monster Hunt | 3 days | Speedups, Crystal, Dragoon Egg fragments | Gem-purchasable AP potions |
| Construction Sprint | 2 days | Building materials, speedups | Speedup purchase |
| Alliance Warfare Event | 5 days | Alliance crystals, rare skins | Gem-purchasable reinforcement speedups |
| Ancient Dragon Hunt | 7 days | Legendary rewards, Rare Eggs | AP potions (high volume) |
| Season Opener | 14 days | Starter packs, hero shards | Starter pack purchase |
| Cross-World War | 7 days | Cross-world exclusive NFTs | Strategic item purchase |

### Event Pass (Supplementary)

Major events (Monster Hunt, Alliance Warfare, Ancient Dragon Hunt) offer an Event Pass:
- Price: 500 EG per event
- Provides a separate event-specific tier track
- Rewards are unique to each event (non-repetitive with main Season Pass)

### Anti-FOMO Design

All event rewards that are gameplay-impactful (speedups, crystals, resources) are re-introduced in future events or through other acquisition paths. Only cosmetic and NFT rewards may be permanently limited-edition.

---

## 12. Anti-Pay-to-Win Policies

### Prohibited Monetization

The following are permanently prohibited from the premium shop:

| Prohibited Item | Reason |
|----------------|--------|
| Direct troop attack/defense bonus buffs | Creates unbridgeable stat gap |
| Instant max-level buildings | Eliminates time gate entirely |
| T5 troop direct purchase | Bypasses training and resource cost entirely |
| Exclusive high-tier units (purchasable only) | Pay-walled power ceiling |
| Combat ability buffs (purchasable mid-battle) | Real-time pay-to-win |
| Permanent resource production multipliers | Compounds into insurmountable economic advantage |

### Enforcement Mechanism

The game server validates that no purchased item grants a numerical stat bonus not achievable through gameplay. If a premium item is ever found to grant an exclusively purchasable combat advantage, it is immediately retracted and refunded (protocol enforced by governance).

---

## 13. Whale Controls

High-spending players (whales) are valuable but must not be able to unilaterally dominate the server.

### Spending Cap Mechanics

**Speedup weekly cap (per category):** 72 hours — prevents infinite construction/research acceleration (see Speedups section).

**Troop count cap:** Maximum troop count is gated by palace level + barracks/stable/archery levels — not by spending. No amount of gems produces more training queue slots beyond the structural caps.

**March speed cap:** Maximum march speed is +300% above base (regardless of buff stacking). This ensures even the fastest-spending player cannot close distances instantaneously.

**Resource purchase limit:** Resources cannot be purchased directly with gems (only speedups for production and gather). This prevents pure money → resources → troops conversion loops.

### Alliance Composition Controls

A single player's spending cannot carry an entire alliance to victory:
- Rally capacity scales with alliance research (requires many member donations — one whale cannot fund it alone efficiently due to diminishing returns above 3× speed).
- Congress VP includes activity from all members — a one-player alliance cannot win Congress.
- Shrine control requires active garrison — spending cannot automatically hold a shrine.

### Transparency

All premium item effects are documented in-game in an accessible item description that includes:
- What the item does
- The in-game equivalent (how long to earn through gameplay)
- The weekly cap (if applicable)

Players can always see how a premium purchase compares to gameplay alternatives.

---

## 14. Long-Term Retention Loops

### Daily Engagement

- **Daily Login Reward** — Escalating streak rewards (day 1–30 cycle, resets to day 1 after 30).
- **Daily Quests** — 5 daily quests refreshed at 00:00 UTC. Completing all 5 grants a bonus chest.
- **Alliance Daily Help** — Social obligation that reinforces daily logins.

### Weekly Engagement

- **Weekly Challenge** — A structured quest chain requiring diverse activities (gather, construct, fight). Completion rewards a 24-hour speedup pack.
- **Alliance Contribution** — Weekly ALP leaderboard within the alliance (top 3 donors receive recognition and small bonuses).

### Monthly Engagement

- **Season Battle Pass Tier Progression** — Monthly milestone unlocks visual upgrades.
- **Monthly Event Rotation** — Ensures fresh content each month.

### Long-Term Retention (Seasonal)

- **Seasonal Ranking** — Competitive motivation for sustained engagement.
- **Carry-Forward NFTs** — Season badges, hero shards, and dragoon lineage persist across season resets. Players who invest across multiple seasons accumulate a progressively richer NFT portfolio.
- **Land NFT Dividend** — Land owners have a financial incentive (crystal dividends) to remain engaged each season.

### Social Retention

Alliance dynamics are the single strongest long-term retention mechanism:
- Friends, rivalries, and politics keep players engaged beyond any individual content loop.
- Alliance leadership responsibility (R4/R5) creates a commitment anchor.
- Congress competition creates a server-wide narrative that sustains engagement through entire seasons.

---

*This Monetization Bible defines the authoritative philosophy and structure for all revenue mechanisms in Eternal Kingdoms. No monetization feature may be implemented that violates the anti-pay-to-win principles outlined in Section 12. Pricing adjustments require governance approval; prohibition violations require Board-level decision.*
