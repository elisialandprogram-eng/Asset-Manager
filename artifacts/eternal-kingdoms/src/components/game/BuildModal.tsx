import { useState } from "react";
import { X, Hammer, Wheat, TreePine, Mountain, Coins, Shield, Eye, Lock, AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useQueryClient } from "@tanstack/react-query";
import {
  useGetConstructionOptions,
  useConstructBuilding,
  getGetKingdomStateQueryKey,
  getGetConstructionQueueQueryKey,
  getGetConstructionOptionsQueryKey,
  type ConstructionOption,
} from "@workspace/api-client-react";
import { motion, AnimatePresence } from "framer-motion";

interface BuildModalProps {
  kingdomId: number;
  open: boolean;
  onClose: () => void;
}

const BUILDING_ICONS: Record<string, React.ReactNode> = {
  farm: <Wheat className="w-5 h-5 text-amber-500" />,
  lumber_mill: <TreePine className="w-5 h-5 text-green-600" />,
  quarry: <Mountain className="w-5 h-5 text-slate-400" />,
  iron_mine: <Hammer className="w-5 h-5 text-zinc-300" />,
  gold_mine: <Coins className="w-5 h-5 text-yellow-400" />,
  barracks: <Shield className="w-5 h-5 text-blue-400" />,
  wall: <Shield className="w-5 h-5 text-slate-500" />,
  watch_tower: <Eye className="w-5 h-5 text-cyan-400" />,
};

const BUILDING_DESC: Record<string, string> = {
  farm: "Produces food each minute to feed your growing kingdom.",
  lumber_mill: "Cuts timber from surrounding forests.",
  quarry: "Extracts stone from the hillside.",
  iron_mine: "Mines iron ore for crafting weapons and tools.",
  gold_mine: "Extracts gold for trade and upkeep.",
  barracks: "Trains soldiers for your armies.",
  wall: "Fortifies your kingdom against attackers.",
  watch_tower: "Extends vision and warns of approaching enemies.",
};

function formatSeconds(s: number): string {
  if (s < 60) return `${s}s`;
  const m = Math.floor(s / 60);
  const r = s % 60;
  return r > 0 ? `${m}m ${r}s` : `${m}m`;
}

function ResourceCost({ cost, shortfall, canAfford }: { cost: Record<string, number>; shortfall: Record<string, number>; canAfford: boolean }) {
  const entries = Object.entries(cost).filter(([, v]) => v > 0);
  if (entries.length === 0) return <span className="text-xs text-muted-foreground">Free</span>;
  return (
    <div className="flex flex-wrap gap-2">
      {entries.map(([res, amount]) => {
        const missing = (shortfall[res] ?? 0) > 0;
        return (
          <span
            key={res}
            className={`text-xs font-mono px-1.5 py-0.5 rounded border ${
              missing
                ? "text-red-400 border-red-800/40 bg-red-950/20"
                : "text-green-400 border-green-800/40 bg-green-950/10"
            }`}
          >
            {res.charAt(0).toUpperCase() + res.slice(0, 2)} {amount.toLocaleString()}
          </span>
        );
      })}
    </div>
  );
}

function OptionCard({
  option,
  selected,
  onClick,
}: {
  option: ConstructionOption;
  selected: boolean;
  onClick: () => void;
}) {
  const icon = BUILDING_ICONS[option.buildingType];
  const desc = BUILDING_DESC[option.buildingType] ?? "";
  const blocked = !option.slotAvailable;
  const unaffordable = !option.canAfford && option.slotAvailable;

  return (
    <button
      onClick={!blocked ? onClick : undefined}
      disabled={blocked}
      className={`w-full text-left p-4 border transition-colors ${
        selected
          ? "border-primary/70 bg-primary/10"
          : blocked
          ? "border-border/30 bg-background/20 opacity-50 cursor-not-allowed"
          : "border-border/40 bg-background/40 hover:border-border hover:bg-background/70 cursor-pointer"
      }`}
    >
      <div className="flex items-start gap-3">
        <div className={`p-2 rounded-sm border shrink-0 ${selected ? "border-primary/40 bg-primary/10" : "border-border/30 bg-background"}`}>
          {blocked ? <Lock className="w-5 h-5 text-muted-foreground/40" /> : icon}
        </div>
        <div className="flex-1 min-w-0">
          <div className="flex items-center justify-between gap-2">
            <span className="font-semibold text-sm text-foreground">{option.label}</span>
            <span className="text-xs text-muted-foreground font-mono shrink-0">{formatSeconds(option.durationSeconds)}</span>
          </div>
          <p className="text-xs text-muted-foreground mt-0.5 line-clamp-2">{blocked ? option.slotReason : desc}</p>
          {!blocked && (
            <div className="mt-2">
              <ResourceCost cost={option.cost as unknown as Record<string, number>} shortfall={(option.shortfall ?? {}) as unknown as Record<string, number>} canAfford={option.canAfford} />
              {unaffordable && (
                <div className="flex items-center gap-1 mt-1.5 text-xs text-red-400">
                  <AlertCircle className="w-3 h-3" />
                  Insufficient resources
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </button>
  );
}

export function BuildModal({ kingdomId, open, onClose }: BuildModalProps) {
  const queryClient = useQueryClient();
  const [selected, setSelected] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const { data: optionsData, isLoading } = useGetConstructionOptions(kingdomId, {
    query: {
      queryKey: getGetConstructionOptionsQueryKey(kingdomId),
      enabled: open && !!kingdomId,
    },
  });

  const { mutate: construct, isPending } = useConstructBuilding({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: getGetKingdomStateQueryKey(kingdomId) });
        queryClient.invalidateQueries({ queryKey: getGetConstructionQueueQueryKey(kingdomId) });
        queryClient.invalidateQueries({ queryKey: getGetConstructionOptionsQueryKey(kingdomId) });
        onClose();
        setSelected(null);
        setError(null);
      },
      onError: (err: unknown) => {
        const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error ?? "Construction failed";
        setError(msg);
      },
    },
  });

  function handleBuild() {
    if (!selected) return;
    setError(null);
    construct({ id: kingdomId, data: { buildingType: selected as never } });
  }

  function handleClose() {
    setSelected(null);
    setError(null);
    onClose();
  }

  if (!open) return null;

  const selectedOption = optionsData?.options.find((o: ConstructionOption) => o.buildingType === selected);
  const canBuild = selectedOption && selectedOption.slotAvailable && selectedOption.canAfford;

  return (
    <AnimatePresence>
      {open && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="absolute inset-0 bg-black/70 backdrop-blur-sm"
            onClick={handleClose}
          />
          <motion.div
            initial={{ opacity: 0, scale: 0.95, y: 16 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95, y: 16 }}
            transition={{ duration: 0.2 }}
            className="relative bg-card border border-border shadow-2xl shadow-black/80 w-full max-w-lg max-h-[80vh] flex flex-col"
          >
            {/* Header */}
            <div className="flex items-center justify-between px-6 py-4 border-b border-border/50">
              <div className="flex items-center gap-2">
                <Hammer className="w-5 h-5 text-amber-500" />
                <h2 className="text-xl font-serif text-primary">Construct Building</h2>
              </div>
              <button onClick={handleClose} className="text-muted-foreground hover:text-foreground transition-colors">
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Palace tier info */}
            {optionsData && (
              <div className="px-6 py-2 border-b border-border/30 text-xs text-muted-foreground bg-background/30">
                Palace Level {optionsData.palaceLevel} — Tier {
                  optionsData.palaceLevel >= 20 ? 5 :
                  optionsData.palaceLevel >= 15 ? 4 :
                  optionsData.palaceLevel >= 10 ? 3 :
                  optionsData.palaceLevel >= 5 ? 2 : 1
                } &bull; Up to {optionsData.palaceTier.maxResourceBuildings} resource buildings
              </div>
            )}

            {/* Options list */}
            <div className="flex-1 overflow-y-auto p-4 space-y-2">
              {isLoading && (
                <div className="text-center py-8 text-muted-foreground text-sm animate-pulse">
                  Loading available structures…
                </div>
              )}
              {optionsData?.options.map((option: ConstructionOption) => (
                <OptionCard
                  key={option.buildingType}
                  option={option}
                  selected={selected === option.buildingType}
                  onClick={() => { setSelected(option.buildingType); setError(null); }}
                />
              ))}
            </div>

            {/* Error */}
            {error && (
              <div className="px-6 py-2 bg-red-950/30 border-t border-red-900/40 text-red-400 text-sm flex items-center gap-2">
                <AlertCircle className="w-4 h-4 shrink-0" />
                {error}
              </div>
            )}

            {/* Footer */}
            <div className="px-6 py-4 border-t border-border/50 flex items-center justify-between gap-3">
              <Button variant="outline" size="sm" onClick={handleClose} className="border-border text-muted-foreground hover:text-foreground">
                Cancel
              </Button>
              <Button
                size="sm"
                disabled={!canBuild || isPending}
                onClick={handleBuild}
                className={`font-semibold ${canBuild ? "bg-primary text-primary-foreground hover:bg-primary/90" : "opacity-40 cursor-not-allowed"}`}
              >
                <Hammer className="w-3.5 h-3.5 mr-1.5" />
                {isPending ? "Starting…" : selected ? `Build ${selectedOption?.label ?? ""}` : "Select a building"}
              </Button>
            </div>
          </motion.div>
        </div>
      )}
    </AnimatePresence>
  );
}
