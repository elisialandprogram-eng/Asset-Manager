# Eternal Kingdoms — Blockchain Architecture Bible

> **SPECIFICATION FREEZE DOCUMENT — IMMUTABLE REFERENCE ARCHITECTURE**
> Version 1.0 — June 2026

---

## 1. Overview

Eternal Kingdoms uses a Polygon (PoS)-based blockchain layer for asset ownership, resource tokenization, and dividend distribution. The blockchain layer is **opt-in** — the full game is playable without a wallet. Blockchain features add a verifiable ownership layer on top of the existing game economy.

All on-chain operations are executed on **Polygon Mainnet** (or Polygon Amoy testnet during development). Polygon was selected for:
- Near-zero gas fees relative to Ethereum mainnet
- EVM compatibility (Solidity contracts)
- Mature tooling (Hardhat, OpenZeppelin, The Graph)
- Existing ecosystem of gaming projects

---

## 2. ERC-721 Land NFTs

### Contract: `EKLandRegistry.sol`

Land NFTs represent permanent ownership of a 32×32 tile plot on the world map. 4,096 Land NFTs exist — one per plot in the 64×64 plot grid.

### Token ID Formula

```
plot_x ∈ [0, 63]
plot_z ∈ [0, 63]
token_id = plot_z × 64 + plot_x + 1    (range: 1 to 4096)
```

Reverse:
```
plot_z = floor((token_id - 1) / 64)
plot_x = (token_id - 1) mod 64
```

### Tile Coverage Formula

```
tile_x_min = plot_x × 32
tile_z_min = plot_z × 32
tile_x_max = plot_x × 32 + 31
tile_z_max = plot_z × 32 + 31
```

### Land Plot Coordinate Slicing

The world's 2048×2048 tile grid is divided as:
```
2048 tiles / 32 tiles per plot = 64 plots per axis
Total: 64 × 64 = 4,096 plots
```

Central plots (Zone 0–1) are premium. Outer plots (Zone 5–7) are the most affordable. Initial auction prices reflect zone tier.

### Land NFT Metadata Schema

```json
{
  "token_id": 1024,
  "name": "Eternal Kingdoms Land #1024",
  "description": "A 32×32 tile land plot in the world of Aethoria.",
  "image": "https://assets.eternalkingdoms.com/land/1024.png",
  "attributes": [
    { "trait_type": "Plot X", "value": 15 },
    { "trait_type": "Plot Z", "value": 15 },
    { "trait_type": "Zone", "value": 2 },
    { "trait_type": "Tile Range X", "value": "480-511" },
    { "trait_type": "Tile Range Z", "value": "480-511" },
    { "trait_type": "Biome", "value": "Hills" },
    { "trait_type": "Land Level", "value": 1 }
  ]
}
```

### Land NFT Functions

```solidity
// EKLandRegistry.sol (interface)
function mint(address to, uint256 tokenId) external onlyMinter;
function ownerOf(uint256 tokenId) external view returns (address);
function plotToToken(uint8 plotX, uint8 plotZ) external pure returns (uint256 tokenId);
function tokenToPlot(uint256 tokenId) external pure returns (uint8 plotX, uint8 plotZ);
function landLevel(uint256 tokenId) external view returns (uint8);
function developmentPoints(uint256 tokenId) external view returns (uint256);
```

---

## 3. Land Development Points and Land Levels

### Development Points (DP)

Development Points (DP) are accumulated on a Land NFT through activity within the plot's tile boundaries:
- Resource nodes gathered within the plot: +1 DP per 1,000 resources gathered
- Kingdoms present within the plot: +10 DP per kingdom upgrade completed within the plot
- Battles won within the plot boundaries: +5 DP per victory

DP is stored on-chain in the `EKLandRegistry` contract:
```solidity
mapping(uint256 => uint256) public developmentPoints;
```

### Land Levels

Land Levels are a prestige tier for land plots, gated by DP thresholds:

| Land Level | DP Required | Bonus |
|-----------|------------|-------|
| 1 (base) | 0 | 5% resource siphon |
| 2 | 10,000 | 7% siphon + visual upgrade |
| 3 | 50,000 | 10% siphon + higher dividend share |
| 4 | 200,000 | 13% siphon + rarest node spawn probability |
| 5 | 1,000,000 | 15% siphon + season NFT auto-mint |

Level upgrades are executed on-chain via the `levelUp(tokenId)` function, which burns the required DP and mints the new land level attribute.

---

## 4. Resource Siphoning

### Siphon Mechanism

When a gather march collects resources from a node within an NFT Land plot, a percentage of the collected resources is directed to the land owner:

```
siphon_rate = base_siphon_rate(land_level) = 5% to 15%
siphon_amount = resources_collected × siphon_rate
player_receives = resources_collected × (1 - siphon_rate)
land_owner_credit = siphon_amount  (credited to land owner's in-game treasury)
```

Siphoned resources are credited in-game. The land owner may:
1. Keep them as in-game resources (spend on construction/training).
2. Request a tokenized withdrawal (mint ERC-1155 resource tokens — see Section 7).

### Siphon Oracle

The backend API reports gather completion events to the Oracle contract:
```solidity
// EKSiphonOracle.sol
function reportGather(
    uint256 landTokenId,
    address landOwner,
    uint256 resourceType,
    uint256 amount
) external onlyRelayer;
```

The Oracle emits `GatherSiphon` events indexed by `landTokenId`. The land owner's wallet can claim accumulated siphon through the `EKTreasury` contract.

---

## 5. Crystal Reward Routing

Crystal is the game's premium resource and the primary blockchain-native token.

### Crystal Flow

```
Player gathers crystal from node
→ In-game crystal credited (+100%)
→ Siphon deducted to land owner (-siphon_rate%)
→ 2% of ALL crystal gathered globally goes to Dividend Engine
```

The 2% global crystal tax funds the dividend pool distributed to Land NFT holders.

### Crystal Tokenization

Players may mint their in-game crystal balance into ERC-1155 Crystal tokens:
```
1 in-game crystal = 1 ERC-1155 Crystal token (token ID 6)
Minimum mint: 100 crystal
Minting fee: 0.5% burned (deflationary pressure)
```

Crystal tokens can be:
- Traded on the open marketplace
- Burned back to in-game crystal (no fee)
- Staked in the dividend pool for additional yield

---

## 6. Dividend Engine

### Architecture

The Dividend Engine distributes the global crystal tax to Land NFT holders proportionally.

```
dividend_pool_per_season = sum(all_crystal_gathered × 0.02)
each_plot_share = (plot_land_level_weight / total_world_weight) × dividend_pool

land_level_weight:
  Level 1: 1.0
  Level 2: 1.4
  Level 3: 2.0
  Level 4: 3.0
  Level 5: 5.0
```

### Dividend Contract: `EKDividend.sol`

```solidity
function deposit(uint256 crystalAmount) external onlyOracle;
function claimDividend(uint256 tokenId) external;
function pendingDividend(uint256 tokenId) external view returns (uint256);
function seasonSnapshot() external onlyAdmin;  // locks balances for season-end distribution
```

Dividends are claimable per-season. Unclaimed dividends roll over to the next season (maximum 3 seasons accumulation).

---

## 7. ERC-1155 Resource Tokens

### Contract: `EKResources.sol`

All tokenizable in-game resources are represented as ERC-1155 tokens.

### Token IDs

| Token ID | Resource | Symbol |
|---------|----------|--------|
| 1 | Food | FOOD |
| 2 | Wood | WOOD |
| 3 | Stone | STONE |
| 4 | Iron | IRON |
| 5 | Gold | GOLD |
| 6 | Crystal | CRYSTAL |

### Minting (In-Game → On-Chain)

```solidity
// EKResources.sol
function mint(address to, uint256 id, uint256 amount, bytes calldata data) external onlyMinter;
function mintBatch(address to, uint256[] calldata ids, uint256[] calldata amounts) external onlyMinter;
```

Players initiate a withdrawal from the in-game portal. The server:
1. Deducts the requested amount from the player's in-game balance.
2. Calls the minter relay, which calls `mint()` on-chain.
3. Player's wallet receives the ERC-1155 tokens.

### Packing (Multi-Resource Bundles)

Resource packs can be bundled into a single ERC-1155 token for efficient marketplace trading:
```solidity
// EKResourcePack.sol — bundles multiple resource tokens into one transferable NFT
function pack(uint256[] calldata ids, uint256[] calldata amounts) external returns (uint256 packId);
function unpack(uint256 packId) external;  // burns pack, returns constituent resources
```

### Escrow

When a player deposits on-chain resources back into the game, they are escrowed:
```solidity
// EKEscrow.sol
function deposit(uint256[] calldata ids, uint256[] calldata amounts) external;
// Player's ERC-1155 tokens are transferred to escrow contract
// Oracle credits in-game balance
function withdraw(uint256[] calldata ids, uint256[] calldata amounts) external;
// Oracle deducts in-game balance
// ERC-1155 tokens released from escrow to player wallet
```

### Burning

Crystal tokens can be burned for special in-game rewards (rare items, dragoon evolution materials):
```solidity
function burn(address from, uint256 id, uint256 amount) external onlyBurner;
```

Burning is a deflationary mechanism. All burned tokens reduce total supply permanently.

---

## 8. Oracle Architecture

### Purpose

The Oracle bridges the authoritative game server with the on-chain contracts. The server is the single source of truth for all game state — the blockchain records ownership and crystallized value, not live game state.

### Oracle Components

1. **API Server** — Game server (Express 5). Generates signed game events.
2. **Relayer Service** — Off-chain relay that batches game events into on-chain transactions.
3. **Oracle Contracts** — On-chain receivers that validate relay signatures and update state.

### Trust Model

```
Game Server (authoritative) → signs event with SERVER_SIGNING_KEY
Relayer reads signed events from queue
Relayer submits to OracleReceiver.sol
OracleReceiver verifies signature against AUTHORIZED_SIGNER
On-chain state updated
```

The `SERVER_SIGNING_KEY` is held exclusively by the API server. No player can forge a signed event. All on-chain state changes require a valid server signature.

### Oracle Event Types

| Event | On-Chain Action |
|-------|---------------|
| `gather_complete` | Credit land owner siphon; contribute to dividend pool |
| `crystal_mint_request` | Mint ERC-1155 crystal tokens |
| `resource_mint_request` | Mint ERC-1155 resource tokens |
| `land_dp_grant` | Increment developmentPoints for land token |
| `hero_nft_mint` | Mint ERC-721 hero token |
| `dragoon_breed` | Mint new ERC-721 dragoon token |
| `season_snapshot` | Lock dividend pool for distribution |

### Gas Batching

The relayer batches multiple events into single transactions using Multicall3:
```
Batch size: 50 events per transaction (maximum)
Batch interval: every 60 seconds
Priority queue: crystal_mint_request and land_dp_grant are priority-batched every 10 seconds
```

---

## 9. Hero NFTs

### Contract: `EKHeroes.sol` (ERC-721)

Heroes at tier 4 and 5 can be minted as NFTs. A hero NFT represents the hero's permanent stats and level.

### Hero NFT Token ID

```
token_id = hero_database_uuid (converted to uint256)
```

### Hero NFT Metadata Schema

```json
{
  "token_id": "...",
  "name": "Aethelred the Bold",
  "description": "A Tier 5 Legendary Hero of Eternal Kingdoms.",
  "image": "https://assets.eternalkingdoms.com/heroes/aethelred_bold.png",
  "attributes": [
    { "trait_type": "Tier", "value": 5 },
    { "trait_type": "Command", "value": 1200 },
    { "trait_type": "Attack", "value": 950 },
    { "trait_type": "Defense", "value": 800 },
    { "trait_type": "Speed", "value": 1100 },
    { "trait_type": "Gathering", "value": 600 },
    { "trait_type": "Level", "value": 60 },
    { "trait_type": "Skill", "value": "Iron Will" }
  ]
}
```

### Hero NFT Minting

Hero NFTs are minted at the Celestial Forge (Palace Tier VII). Minting costs:
- 500 Crystal
- A "Hero Inscription" (rare item from PvE/season rewards)
- Hero must be at level 40+

Once minted, the hero's stats are frozen on-chain. Further in-game leveling above the mint snapshot does not update the NFT (a re-mint option exists at additional cost).

### Hero NFT Transfer Rules

- Transferred heroes arrive in the new owner's kingdom at level 1.
- On-chain stats are inherited; the new owner must re-level in-game.
- Skill sets are inherited permanently.

---

## 10. Dragoon NFTs

### Contract: `EKDragoons.sol` (ERC-721)

All dragoons are NFTs from creation. There are no non-NFT dragoons.

### Dragoon Minting (First Generation)

First-generation dragoons are minted from "Dragoon Eggs":
- Eggs are distributed through season rewards, events, and limited auctions.
- An egg is burned (on-chain) to mint a generation-0 dragoon.
- Generation-0 dragoons have randomized base stats (seeded by egg token ID + block hash).

### Dragoon Stat System

| Stat | Range | Description |
|------|-------|------------|
| Strength | 100–1000 | Combat power multiplier |
| Agility | 100–1000 | March speed bonus |
| Endurance | 100–1000 | Duration of dragoon ability in battle |
| Element | Fire/Frost/Storm/Earth/Void | Determines dragoon type (immutable) |
| Generation | 0–10 | Breeding generation (higher = possible degradation) |
| Rarity | Common/Uncommon/Rare/Epic/Legendary | Determines visual tier |

### Dragoon Breeding System

Two dragoons (both owned by the requesting wallet) can be bred at the Dragon Roost (level 15+):

**Breeding rules:**
1. Both parent dragoons must be level 10+.
2. Each dragoon can breed a maximum of 7 times (stored on-chain as `breedCount`).
3. Breeding costs: 200 Crystal + breeding_fee(generation_A + generation_B).
4. Offspring generation = max(parent_A.generation, parent_B.generation) + 1.

**Stat inheritance formula:**
```
offspring_stat = 0.5 × (parent_A_stat + parent_B_stat) + mutation_roll

mutation_roll = random(-50, +100) × rarity_multiplier
Where:
  Legendary rarity: 1.2× mutation range
  Common rarity: 0.8× mutation range
  random() seeded by: keccak256(parent_A_id, parent_B_id, block.timestamp)
```

**Element inheritance:**
- 40% chance of parent A element
- 40% chance of parent B element
- 20% chance of mutation (random element from all 5 types)

**Mutation Events:**
Mutation is a rare outcome (2% chance per breed) where an offspring receives a stat 3 standard deviations above the parent average — creating "legendary line" dragoons highly valued on the marketplace.

---

## 11. Marketplace Architecture

### Contract: `EKMarketplace.sol`

The Eternal Kingdoms marketplace supports fixed-price listings and auction-style listings for all ERC-721 and ERC-1155 game assets.

### Supported Asset Types

| Asset | Contract | Standard |
|-------|----------|----------|
| Land Plots | EKLandRegistry | ERC-721 |
| Heroes | EKHeroes | ERC-721 |
| Dragoons | EKDragoons | ERC-721 |
| Resources | EKResources | ERC-1155 |
| Resource Packs | EKResourcePack | ERC-1155 |
| Dragoon Eggs | EKEggs | ERC-1155 |
| Seasonal NFTs | EKSeasonBadges | ERC-721 |

### Listing Types

**Fixed Price:**
```solidity
function list(address nftContract, uint256 tokenId, uint256 price, address paymentToken) external;
function buy(uint256 listingId) external;
function cancelListing(uint256 listingId) external;
```

**Auction:**
```solidity
function createAuction(address nftContract, uint256 tokenId, uint256 startPrice, uint256 duration) external;
function bid(uint256 auctionId, uint256 amount) external;
function settleAuction(uint256 auctionId) external;
```

### Payment Tokens

Accepted payment tokens:
- Wrapped MATIC (WMATIC) — primary settlement token
- USDC (Polygon) — stable pricing option
- EK Crystal Token (ERC-20 equivalent of the Crystal resource — future phase)

### Marketplace Fees

```
Seller fee: 5% of sale price (retained by protocol treasury)
Creator royalty: 2.5% (for ERC-2981 compliant assets — Heroes and Dragoons)
Net to seller: 92.5% of sale price
```

Fees flow to the `EKProtocolTreasury.sol` multisig wallet.

---

## 12. Wallet Abstraction

### Design Goal

Players should not need to understand or manage a wallet to play Eternal Kingdoms. Blockchain features are available to those who want them, but never required.

### Account Abstraction (EIP-4337)

Eternal Kingdoms uses ERC-4337 account abstraction via a Paymaster architecture:
1. Players create an embedded wallet (generated by the game client, custodial by default).
2. The Paymaster covers gas fees for all in-game operations (mint, transfer, marketplace).
3. Players can optionally export their private key or link an external wallet (MetaMask, WalletConnect).

### Embedded Wallet Lifecycle

```
Registration:
  Player registers with email/password
  Game generates a 12-word BIP-39 seed phrase
  Seed phrase is encrypted with the player's password (AES-256)
  Encrypted seed stored on Supabase (not server-accessible without password)

First NFT Mint:
  Player triggers a mint action in-game
  Game decrypts the seed using the player's session-derived key
  Signs the transaction locally
  Submits via Paymaster (gas-free for player)

Export:
  Player can view/export their seed phrase from account settings
  External wallet connection: EIP-1193 provider injection (MetaMask, etc.)
```

### Security Boundaries

- The server **never** has access to the player's unencrypted seed phrase.
- All signing occurs client-side (Unity or browser).
- Encrypted seeds are stored encrypted-at-rest in Supabase.
- Hardware wallet (Ledger/Trezor) support via WalletConnect v2.

---

## 13. Gas Abstraction

### Paymaster Contract: `EKPaymaster.sol`

The protocol sponsors gas fees for all standard game operations.

**Sponsored operations:**
- NFT mints (hero, dragoon, land claim)
- Marketplace listings (first 3 listings per day free)
- Resource token mints (up to 5 per day free)
- Dividend claims

**Non-sponsored operations:**
- Marketplace buys (buyer pays gas)
- Large bulk transfers (>100 tokens)
- Auction settlements

### Paymaster Funding

The Paymaster pool is funded from:
- 1% of all marketplace transaction fees
- Protocol treasury allocation (governance-controlled)
- Seasonal top-up from protocol reserves

### Gas Estimation

All client-side transaction previews show:
- Gas cost in MATIC (raw)
- Gas cost in USD equivalent
- Whether the operation is sponsored (shows "Free" if Paymaster covers it)

---

## 14. Enterprise Relay Architecture

### Purpose

High-frequency game events (gather completions, construction events, battle outcomes) cannot be submitted one-by-one to the blockchain. The Enterprise Relay batches and compresses these events.

### Relay Stack

```
[Game Server] → [Event Queue (Redis)] → [Relay Worker] → [Multicall3 Batch] → [Polygon Chain]
```

### Relay Worker Specification

```
Language: Node.js (TypeScript)
Queue: Redis Stream (XADD / XREAD consumer groups)
Batch size: 50 events maximum per transaction
Commit interval: 60 seconds (or when batch reaches 50)
Priority: crystal/mint events bypass standard queue (10s commit)
Retry: 3 attempts with exponential backoff on revert
Dead letter: failed events logged to dead_letter_stream for manual review
Signature: each event signed with SERVER_SIGNING_KEY (secp256k1)
```

### Relay Worker Failure Handling

If the relay worker fails or Polygon is congested:
- In-game state is unaffected (server is authoritative)
- Events accumulate in Redis queue (max 24h retention)
- On relay recovery, events are processed in order
- If a player tries to claim an NFT that hasn't been minted yet, the UI shows "Pending — blockchain sync in progress"

---

## 15. Polygon Integration

### Network Configuration

| Parameter | Mainnet | Testnet (Amoy) |
|-----------|---------|---------------|
| Chain ID | 137 | 80002 |
| RPC URL | `https://polygon-rpc.com` | `https://rpc-amoy.polygon.technology` |
| Block Explorer | polygonscan.com | amoy.polygonscan.com |
| Native Token | MATIC | Test MATIC |
| Avg Block Time | ~2 seconds | ~5 seconds |

### Contract Deployment Order

1. `EKLandRegistry.sol` (ERC-721 Land NFTs)
2. `EKResources.sol` (ERC-1155 resources)
3. `EKResourcePack.sol` (resource bundles)
4. `EKEggs.sol` (Dragoon Eggs ERC-1155)
5. `EKHeroes.sol` (ERC-721 Heroes)
6. `EKDragoons.sol` (ERC-721 Dragoons)
7. `EKSeasonBadges.sol` (ERC-721 seasonal badges)
8. `EKTreasury.sol` (land owner treasury claims)
9. `EKDividend.sol` (dividend pool distribution)
10. `EKSiphonOracle.sol` (relayer-facing oracle)
11. `EKMarketplace.sol` (listings + auctions)
12. `EKPaymaster.sol` (EIP-4337 gas sponsorship)
13. `EKProtocolTreasury.sol` (fee accumulation multisig)

### Contract Upgrade Strategy

All contracts use the **UUPS Proxy Pattern** (EIP-1822) for upgradability:
- Logic contracts are upgradeable by the protocol admin multisig (5-of-9 signers).
- Proxy addresses are permanent — wallets and integrations never need to change.
- Upgrade timelock: 48 hours delay (announced on-chain before execution).

### Subgraph (The Graph)

A Graph Protocol subgraph indexes all Eternal Kingdoms events for fast querying:
- Land plot ownership history
- Dragoon lineage graphs
- Marketplace trade history
- Dividend distribution records

Subgraph endpoint: `https://api.thegraph.com/subgraphs/name/eternalkingdoms/ek-main`

---

*This Blockchain Architecture Bible defines the authoritative smart contract and on-chain integration specification for Eternal Kingdoms. All contract implementations, relay systems, and wallet integrations must conform to this document.*
