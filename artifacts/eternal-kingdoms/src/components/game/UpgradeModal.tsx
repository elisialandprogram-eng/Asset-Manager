import { useState, useEffect } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import {
  useGetUpgradePreview,
  useUpgradeBuilding,
  getGetKingdomStateQueryKey,
  getGetKingdomQueueQueryKey,
  getGetUpgradePreviewQueryKey,
} from "@workspace/api-client-react";
import { useQueryClient } from "@tanstack/react-query";
import {
  Wheat, TreePine, Mountain, Hammer, Coins, Clock,
  AlertTriangle, Building2, Swords, CheckCircle2,
} from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";
import { ProgressRing } from "./ProgressRing";
import type { Building } from "@workspace/api-client-react";

interface UpgradeModalProps {
  building: Building;
  kingdomId: number;
  open: boolean;
  onClose: () => void;
}

const RESOURCE_ICONS: Record<string, React.ReactNode> = {
  food:  <Wheat    className="w-3.5 h-3.5 text-amber-500" />,
  wood:  <TreePine className="w-3.5 h-3.5 text-green-500" />,
  stone: <Mountain className="w-3.5 h-3.5 text-slate-400" />,
  iron:  <Hammer   className="w-3.5 h-3.5 text-zinc-300"  />,
  gold:  <Coins    className="w-3.5 h-3.5 text-yellow-400" />,
};

const BUILDING_META: Record<string, { label: string; icon: React.ReactNode; ringColor: string }> = {
  palace:      { label: "Palace",      icon: <Building2 className="w-6 h-6 text-yellow-400" />, ringColor: "#f59e0b" },
  farm:        { label: "Farm",        icon: <Wheat     className="w-6 h-6 text-amber-400"  />, ringColor: "#f59e0b" },
  lumber_mill: { label: "Lumber Mill", icon: <TreePine  className="w-6 h-6 text-emerald-400"/>, ringColor: "#34d399" },
  quarry:      { label: "Quarry",      icon: <Mountain  className="w-6 h-6 text-slate-300"  />, ringColor: "#94a3b8" },
  iron_mine:   { label: "Iron Mine",   icon: <Hammer    className="w-6 h-6 text-zinc-300"   />, ringColor: "#d4d4d8" },
  gold_mine:   { label: "Gold Mine",   icon: <Coins     className="w-6 h-6 text-yellow-400" />, ringColor: "#eab308" },
  barracks:    { label: "Barracks",    icon: <Swords    className="w-6 h-6 text-blue-400"   />, ringColor: "#60a5fa" },
  wall:        { label: "Wall",        icon: <Building2 className="w-6 h-6 text-slate-400"  />, ringColor: "#94a3b8" },
  watch_tower: { label: "Watch Tower", icon: <Building2 className="w-6 h-6 text-cyan-400"   />, ringColor: "#22d3ee" },
};

function formatDuration(seconds: number) {
  if (seconds < 60) return `${seconds}s`;
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  if (m < 60) return s > 0 ? `${m}m ${s}s` : `${m}m`;
  const h = Math.floor(m / 60);
  const rem = m % 60;
  return rem > 0 ? `${h}h ${rem}m` : `${h}h`;
}

function useTickingClock(open: boolean) {
  const [tick, setTick] = useState(0);
  useEffect(() => {
    if (!open) return;
    const id = setInterval(() => setTick((t) => t + 1), 1000);
    return () => clearInterval(id);
  }, [open]);
  return tick;
}

export function UpgradeModal({ building, kingdomId, open, onClose }: UpgradeModalProps) {
  const queryClient = useQueryClient();
  const tick = useTickingClock(open);
  const [launched, setLaunched] = useState(false);

  useEffect(() => {
    if (!open) { setLaunched(false); }
  }, [open]);

  const meta = BUILDING_META[building.buildingType] ?? {
    label: building.buildingType.replace(/_/g, " ").replace(/\b\w/g, (c: string) => c.toUpperCase()),
    icon: <Building2 className="w-6 h-6 text-muted-foreground" />,
    ringColor: "#b45309",
  };

  const { data: preview, isLoading: isLoadingPreview } = useGetUpgradePreview(building.id, {
    query: { queryKey: getGetUpgradePreviewQueryKey(building.id), enabled: open },
  });

  const { mutate: startUpgrade, isPending, isError } = useUpgradeBuilding({
    mutation: {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: getGetKingdomStateQueryKey(kingdomId) });
        queryClient.invalidateQueries({ queryKey: getGetKingdomQueueQueryKey(kingdomId) });
        setLaunched(true);
        setTimeout(onClose, 900);
      },
    },
  });

  const costs = preview
    ? ([
        ["food",  preview.cost.food],
        ["wood",  preview.cost.wood],
        ["stone", preview.cost.stone],
        ["iron",  preview.cost.iron],
        ["gold",  preview.cost.gold],
      ] as const).filter(([, v]) => v > 0)
    : [];

  const shortfalls = preview?.shortfall
    ? ([
        ["food",  preview.shortfall.food],
        ["wood",  preview.shortfall.wood],
        ["stone", preview.shortfall.stone],
        ["iron",  preview.shortfall.iron],
        ["gold",  preview.shortfall.gold],
      ] as const).filter(([, v]) => (v ?? 0) > 0)
    : [];

  const ringProgress = open ? (isLoadingPreview ? 0.15 : preview?.canAfford ? 0.72 : 0.35) : 0;

  return (
    <Dialog open={open} onOpenChange={(v) => { if (!v) onClose(); }}>
      <DialogContent className="bg-[#0f0d0a] border border-amber-900/30 text-foreground max-w-sm p-0 overflow-hidden">

        {/* Header band */}
        <div className="relative flex flex-col items-center gap-2 px-6 pt-7 pb-5 bg-gradient-to-b from-amber-950/30 to-transparent border-b border-amber-900/20">
          {/* Decorative corner glyphs */}
          <div className="absolute top-3 left-3 text-amber-800/30 text-xs font-serif select-none">⌖</div>
          <div className="absolute top-3 right-3 text-amber-800/30 text-xs font-serif select-none">⌖</div>

          <ProgressRing
            progress={launched ? 1 : ringProgress}
            size={88}
            strokeWidth={4}
            color={meta.ringColor}
            trackColor="rgba(180,83,9,0.12)"
            animateDuration={launched ? 0.5 : 1.0}
          >
            <AnimatePresence mode="wait">
              {launched ? (
                <motion.div
                  key="check"
                  initial={{ scale: 0, opacity: 0 }}
                  animate={{ scale: 1, opacity: 1 }}
                  exit={{ scale: 0, opacity: 0 }}
                  transition={{ type: "spring", stiffness: 400, damping: 20 }}
                >
                  <CheckCircle2 className="w-8 h-8 text-emerald-400" />
                </motion.div>
              ) : (
                <motion.div
                  key="icon"
                  initial={{ scale: 0.8, opacity: 0 }}
                  animate={{ scale: 1, opacity: 1 }}
                  transition={{ delay: 0.15 }}
                  className="p-3 rounded-xl bg-amber-950/40 border border-amber-800/30"
                >
                  {meta.icon}
                </motion.div>
              )}
            </AnimatePresence>
          </ProgressRing>

          <DialogHeader className="text-center space-y-0.5">
            <DialogTitle className="font-serif text-xl text-amber-200 leading-tight">
              {meta.label}
            </DialogTitle>
            <DialogDescription className="text-muted-foreground text-sm font-mono">
              Level <span className="text-foreground font-bold">{building.level}</span>
              <span className="text-amber-600 mx-1.5">→</span>
              Level <span className="text-amber-400 font-bold">{building.level + 1}</span>
            </DialogDescription>
          </DialogHeader>
        </div>

        {/* Body */}
        <div className="px-6 py-5 space-y-4">
          {isLoadingPreview ? (
            <div className="py-6 flex flex-col items-center gap-3 text-muted-foreground text-sm">
              <motion.div
                animate={{ rotate: 360 }}
                transition={{ repeat: Infinity, duration: 1.2, ease: "linear" }}
                className="w-5 h-5 border-2 border-amber-700/40 border-t-amber-500 rounded-full"
              />
              <span className="text-xs">Consulting the architects…</span>
            </div>
          ) : preview ? (
            <motion.div
              initial={{ opacity: 0, y: 6 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.25 }}
              className="space-y-4"
            >
              {/* Resource Costs */}
              {costs.length > 0 && (
                <div>
                  <div className="text-[10px] uppercase tracking-widest text-amber-800/70 mb-2 font-semibold">
                    Resources Required
                  </div>
                  <div className="grid grid-cols-2 gap-1.5">
                    {costs.map(([key, val]) => {
                      const isShort = shortfalls.some(([k]) => k === key);
                      return (
                        <div
                          key={key}
                          className={`flex items-center gap-2 px-3 py-2 rounded border ${
                            isShort
                              ? "border-red-800/50 bg-red-950/30"
                              : "border-amber-900/20 bg-amber-950/10"
                          }`}
                        >
                          {RESOURCE_ICONS[key]}
                          <span className="text-[11px] text-muted-foreground capitalize">{key}</span>
                          <span className={`ml-auto text-sm font-bold font-mono ${isShort ? "text-red-400" : "text-foreground"}`}>
                            {val.toLocaleString()}
                          </span>
                        </div>
                      );
                    })}
                  </div>
                </div>
              )}

              {/* Duration */}
              <div className="flex items-center justify-between px-3 py-2.5 rounded border border-amber-900/20 bg-amber-950/10">
                <div className="flex items-center gap-2 text-muted-foreground text-xs">
                  <motion.div
                    key={tick}
                    initial={{ scale: 1.2 }}
                    animate={{ scale: 1 }}
                    transition={{ duration: 0.3 }}
                  >
                    <Clock className="w-3.5 h-3.5 text-amber-600" />
                  </motion.div>
                  Construction Time
                </div>
                <span className="font-mono text-sm font-bold text-amber-400">
                  {formatDuration(preview.durationSeconds)}
                </span>
              </div>

              {/* Shortfall Warning */}
              {!preview.canAfford && shortfalls.length > 0 && (
                <motion.div
                  initial={{ opacity: 0, y: 4 }}
                  animate={{ opacity: 1, y: 0 }}
                  className="flex items-start gap-2.5 px-3 py-2.5 rounded border border-red-800/50 bg-red-950/20 text-red-400 text-xs"
                >
                  <AlertTriangle className="w-3.5 h-3.5 shrink-0 mt-0.5" />
                  <div>
                    <div className="font-semibold mb-0.5">Insufficient Resources</div>
                    <div className="space-y-0.5 text-red-500/80">
                      {shortfalls.map(([key, val]) => (
                        <div key={key} className="capitalize">
                          {key}: need {(val ?? 0).toLocaleString()} more
                        </div>
                      ))}
                    </div>
                  </div>
                </motion.div>
              )}

              {!preview.canUpgrade && preview.reason && (
                <div className="flex items-start gap-2.5 px-3 py-2.5 rounded border border-yellow-800/50 bg-yellow-950/20 text-yellow-400 text-xs">
                  <AlertTriangle className="w-3.5 h-3.5 shrink-0 mt-0.5" />
                  {preview.reason}
                </div>
              )}

              {isError && (
                <div className="text-xs text-red-400 px-3 py-2 rounded border border-red-800/50 bg-red-950/20">
                  Upgrade failed — please try again.
                </div>
              )}
            </motion.div>
          ) : (
            <div className="py-6 text-center text-muted-foreground text-xs italic">
              Could not load upgrade details.
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="flex items-center gap-2 px-6 pb-6">
          <Button
            variant="outline"
            onClick={onClose}
            disabled={isPending}
            className="flex-1 border-amber-900/30 text-muted-foreground hover:bg-amber-950/20 hover:text-foreground h-9 text-sm"
          >
            Cancel
          </Button>
          <Button
            onClick={() => startUpgrade({ buildingId: building.id })}
            disabled={isPending || isLoadingPreview || !preview?.canUpgrade || !preview?.canAfford || launched}
            className="flex-1 h-9 text-sm font-semibold bg-amber-700 hover:bg-amber-600 text-amber-50 border-0 disabled:opacity-40"
          >
            <AnimatePresence mode="wait">
              {isPending || launched ? (
                <motion.span
                  key="pending"
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
                  className="flex items-center gap-1.5"
                >
                  <motion.div
                    animate={{ rotate: 360 }}
                    transition={{ repeat: Infinity, duration: 0.8, ease: "linear" }}
                    className="w-3.5 h-3.5 border-2 border-amber-200/30 border-t-amber-100 rounded-full"
                  />
                  Commencing…
                </motion.span>
              ) : (
                <motion.span
                  key="idle"
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
                >
                  Begin Upgrade
                </motion.span>
              )}
            </AnimatePresence>
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
