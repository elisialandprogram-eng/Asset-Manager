import { useState, useEffect } from "react";
import { ArrowUp, Wheat, TreePine, Mountain, Hammer, Coins, Shield, Eye, Building2, Zap } from "lucide-react";
import { Button } from "@/components/ui/button";
import { motion } from "framer-motion";
import { UpgradeModal } from "./UpgradeModal";
import { ProgressRing } from "./ProgressRing";
import type { Building, UpgradeQueueItem, ResourceAmounts } from "@workspace/api-client-react";

interface BuildingCardProps {
  building: Building;
  kingdomId: number;
  activeQueue: UpgradeQueueItem[];
  productionRates: ResourceAmounts;
}

const BUILDING_META: Record<string, {
  label: string;
  description: string;
  icon: React.ReactNode;
  accentColor: string;
  ringColor: string;
  produces?: { resource: string; icon: React.ReactNode };
}> = {
  palace: {
    label: "Palace",
    description: "Seat of your power. Higher levels unlock new structures and tiers.",
    icon: <Building2 className="w-5 h-5 text-yellow-400" />,
    accentColor: "amber",
    ringColor: "#f59e0b",
  },
  farm: {
    label: "Farm",
    description: "Produces food each minute to feed your growing kingdom.",
    icon: <Wheat className="w-5 h-5 text-amber-400" />,
    accentColor: "amber",
    ringColor: "#f59e0b",
    produces: { resource: "food", icon: <Wheat className="w-3 h-3 text-amber-400" /> },
  },
  lumber_mill: {
    label: "Lumber Mill",
    description: "Cuts timber from surrounding forests.",
    icon: <TreePine className="w-5 h-5 text-emerald-400" />,
    accentColor: "emerald",
    ringColor: "#34d399",
    produces: { resource: "wood", icon: <TreePine className="w-3 h-3 text-emerald-400" /> },
  },
  quarry: {
    label: "Quarry",
    description: "Extracts stone from the hillside.",
    icon: <Mountain className="w-5 h-5 text-slate-300" />,
    accentColor: "slate",
    ringColor: "#94a3b8",
    produces: { resource: "stone", icon: <Mountain className="w-3 h-3 text-slate-300" /> },
  },
  iron_mine: {
    label: "Iron Mine",
    description: "Mines iron ore for crafting weapons and tools.",
    icon: <Hammer className="w-5 h-5 text-zinc-300" />,
    accentColor: "zinc",
    ringColor: "#d4d4d8",
    produces: { resource: "iron", icon: <Hammer className="w-3 h-3 text-zinc-300" /> },
  },
  gold_mine: {
    label: "Gold Mine",
    description: "Extracts gold for trade and upkeep.",
    icon: <Coins className="w-5 h-5 text-yellow-400" />,
    accentColor: "yellow",
    ringColor: "#eab308",
    produces: { resource: "gold", icon: <Coins className="w-3 h-3 text-yellow-400" /> },
  },
  barracks: {
    label: "Barracks",
    description: "Trains soldiers for your armies.",
    icon: <Shield className="w-5 h-5 text-blue-400" />,
    accentColor: "blue",
    ringColor: "#60a5fa",
  },
  wall: {
    label: "Wall",
    description: "Fortifies your kingdom against attackers.",
    icon: <Shield className="w-5 h-5 text-slate-400" />,
    accentColor: "slate",
    ringColor: "#94a3b8",
  },
  watch_tower: {
    label: "Watch Tower",
    description: "Extends vision and warns of approaching enemies.",
    icon: <Eye className="w-5 h-5 text-cyan-400" />,
    accentColor: "cyan",
    ringColor: "#22d3ee",
  },
};

const RESOURCE_RATE_MAP: Record<string, keyof ResourceAmounts> = {
  farm: "food", lumber_mill: "wood", quarry: "stone", iron_mine: "iron", gold_mine: "gold",
};

function useLiveCountdown(endsAt: string, startsAt?: string) {
  const [now, setNow] = useState(Date.now());

  useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, []);

  const endMs = new Date(endsAt).getTime();
  const remaining = Math.max(0, Math.ceil((endMs - now) / 1000));
  const mins = Math.floor(remaining / 60);
  const secs = remaining % 60;
  const timeString = mins > 0 ? `${mins}m ${secs.toString().padStart(2, "0")}s` : `${secs}s`;

  let progress = 0;
  if (startsAt) {
    const startMs = new Date(startsAt).getTime();
    const total = endMs - startMs;
    if (total > 0) progress = Math.min(1, (now - startMs) / total);
  }

  return { timeString, remaining, progress };
}

export function BuildingCard({ building, kingdomId, activeQueue, productionRates }: BuildingCardProps) {
  const [modalOpen, setModalOpen] = useState(false);

  const meta = BUILDING_META[building.buildingType] ?? {
    label: building.buildingType.replace(/_/g, " ").replace(/\b\w/g, (c: string) => c.toUpperCase()),
    description: "A structure in your kingdom.",
    icon: <Building2 className="w-5 h-5 text-muted-foreground" />,
    accentColor: "slate",
    ringColor: "#64748b",
  };

  const isUpgrading = activeQueue.some((q) => q.buildingId === building.id && q.status === "in_progress");
  const upgradeItem = activeQueue.find((q) => q.buildingId === building.id && q.status === "in_progress");
  const isMaxLevel = building.level >= 20;

  const rateKey = RESOURCE_RATE_MAP[building.buildingType];
  const productionRate = rateKey ? productionRates[rateKey] : 0;

  const { timeString, progress } = useLiveCountdown(
    upgradeItem?.endsAt ?? new Date(Date.now() + 9999999).toISOString(),
    upgradeItem?.startsAt,
  );

  return (
    <>
      <motion.div
        whileHover={{ scale: 1.015, y: -2 }}
        transition={{ duration: 0.18 }}
        className={`group relative flex flex-col gap-3 p-5 border rounded-lg transition-all cursor-default shadow-md overflow-hidden ${
          isUpgrading
            ? "border-amber-700/50 bg-amber-950/15 shadow-amber-900/30"
            : isMaxLevel
            ? "border-yellow-700/30 bg-yellow-950/10"
            : "border-border/40 bg-card/40 hover:border-border/80 hover:bg-card/70 hover:shadow-lg hover:shadow-black/40"
        }`}
      >
        {/* Upgrade shimmer sweep */}
        {isUpgrading && (
          <motion.div
            className="absolute inset-0 pointer-events-none"
            initial={{ x: "-100%" }}
            animate={{ x: "200%" }}
            transition={{ repeat: Infinity, duration: 2.4, ease: "linear", repeatDelay: 1.2 }}
            style={{
              background: "linear-gradient(105deg, transparent 40%, rgba(251,191,36,0.07) 50%, transparent 60%)",
            }}
          />
        )}

        {isMaxLevel && (
          <div className="absolute top-2 right-2">
            <span className="text-[9px] px-1.5 py-0.5 bg-yellow-900/40 border border-yellow-700/40 text-yellow-400 rounded uppercase tracking-wider font-bold">Max</span>
          </div>
        )}

        <div className="flex items-start gap-3">
          {/* Icon — wrapped in progress ring when upgrading */}
          {isUpgrading && upgradeItem ? (
            <ProgressRing
              progress={progress}
              size={46}
              strokeWidth={3}
              color={meta.ringColor}
              animateDuration={0.8}
            >
              <div className="p-2 rounded-lg border border-amber-700/40 bg-amber-900/20">
                {meta.icon}
              </div>
            </ProgressRing>
          ) : (
            <motion.div
              whileHover={{ rotate: [0, -5, 5, 0] }}
              transition={{ duration: 0.4 }}
              className="p-2.5 rounded-lg border shrink-0 border-border/30 bg-background/60"
            >
              {meta.icon}
            </motion.div>
          )}

          <div className="flex-1 min-w-0">
            <div className="flex items-baseline gap-2">
              <span className="font-semibold text-sm text-foreground">{meta.label}</span>
              <span className="text-xs font-serif font-bold text-primary/80">Lv.{building.level}</span>
              {isUpgrading && upgradeItem && (
                <motion.span
                  initial={{ opacity: 0, x: -4 }}
                  animate={{ opacity: 1, x: 0 }}
                  className="text-xs font-serif font-bold text-amber-400"
                >
                  → Lv.{upgradeItem.toLevel}
                </motion.span>
              )}
            </div>
            <div className="text-[11px] text-muted-foreground mt-0.5 leading-snug">{meta.description}</div>
          </div>
        </div>

        {productionRate > 0 && (
          <div className="flex items-center gap-1.5 text-xs text-emerald-400 bg-emerald-950/30 border border-emerald-800/20 rounded px-2.5 py-1">
            <Zap className="w-3 h-3" />
            {meta.produces?.icon}
            <span>+{productionRate} {meta.produces?.resource}/min</span>
          </div>
        )}

        {/* Progress bar strip when upgrading */}
        {isUpgrading && (
          <div className="w-full h-1 bg-amber-950/40 rounded-full overflow-hidden">
            <motion.div
              className="h-full rounded-full"
              style={{ background: `linear-gradient(90deg, ${meta.ringColor}80, ${meta.ringColor})` }}
              initial={{ width: "0%" }}
              animate={{ width: `${progress * 100}%` }}
              transition={{ duration: 0.8, ease: "easeOut" }}
            />
          </div>
        )}

        <div className="flex items-center justify-between mt-auto pt-2.5 border-t border-border/15">
          {isUpgrading && upgradeItem ? (
            <div className="flex items-center gap-2 text-amber-400 text-xs">
              <motion.div
                animate={{ scale: [1, 1.15, 1] }}
                transition={{ repeat: Infinity, duration: 1.4, ease: "easeInOut" }}
                className="w-1.5 h-1.5 rounded-full bg-amber-400"
              />
              <span className="font-mono font-semibold">{timeString}</span>
              <span className="text-amber-600 text-[10px]">remaining</span>
            </div>
          ) : (
            <div className="text-[11px] text-muted-foreground/50">
              {isMaxLevel ? "⚜ Fully upgraded" : "Idle"}
            </div>
          )}

          <Button
            size="sm"
            variant="outline"
            disabled={isUpgrading || isMaxLevel}
            onClick={() => setModalOpen(true)}
            className={`text-xs h-7 px-3 transition-all ${
              isUpgrading || isMaxLevel
                ? "opacity-30 cursor-not-allowed border-border/30 text-muted-foreground"
                : "border-primary/30 text-primary hover:bg-primary/10 hover:border-primary/60 hover:shadow-sm hover:shadow-primary/20"
            }`}
          >
            <ArrowUp className="w-3 h-3 mr-1" />
            Upgrade
          </Button>
        </div>
      </motion.div>

      <UpgradeModal
        building={building}
        kingdomId={kingdomId}
        open={modalOpen}
        onClose={() => setModalOpen(false)}
      />
    </>
  );
}
