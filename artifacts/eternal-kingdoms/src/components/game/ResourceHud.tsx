import { Wheat, TreePine, Mountain, Hammer, Coins, TrendingUp } from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";
import type { ResourceAmounts, Resources } from "@workspace/api-client-react";

const RESOURCES = [
  {
    key: "food" as const, label: "Food", icon: Wheat,
    color: "text-amber-400", barColor: "bg-amber-400", glowColor: "shadow-amber-500/30",
    bgColor: "bg-amber-500/10", borderColor: "border-amber-700/30",
  },
  {
    key: "wood" as const, label: "Wood", icon: TreePine,
    color: "text-emerald-400", barColor: "bg-emerald-400", glowColor: "shadow-emerald-500/30",
    bgColor: "bg-emerald-500/10", borderColor: "border-emerald-700/30",
  },
  {
    key: "stone" as const, label: "Stone", icon: Mountain,
    color: "text-slate-300", barColor: "bg-slate-300", glowColor: "shadow-slate-400/30",
    bgColor: "bg-slate-500/10", borderColor: "border-slate-600/30",
  },
  {
    key: "iron" as const, label: "Iron", icon: Hammer,
    color: "text-zinc-300", barColor: "bg-zinc-300", glowColor: "shadow-zinc-400/30",
    bgColor: "bg-zinc-500/10", borderColor: "border-zinc-600/30",
  },
  {
    key: "gold" as const, label: "Gold", icon: Coins,
    color: "text-yellow-400", barColor: "bg-yellow-400", glowColor: "shadow-yellow-500/30",
    bgColor: "bg-yellow-500/10", borderColor: "border-yellow-700/30",
  },
] as const;

interface ResourceHudProps {
  resources: Resources;
  productionRates: ResourceAmounts;
  resourceCaps: ResourceAmounts;
}

function fmt(n: number) {
  if (n >= 1_000_000) return (n / 1_000_000).toFixed(1) + "M";
  if (n >= 10_000) return (n / 1_000).toFixed(1) + "k";
  return Math.floor(n).toLocaleString();
}

export function ResourceHud({ resources, productionRates, resourceCaps }: ResourceHudProps) {
  return (
    <div className="grid grid-cols-5 gap-2">
      {RESOURCES.map(({ key, label, icon: Icon, color, barColor, glowColor, bgColor, borderColor }) => {
        const current = resources[key] ?? 0;
        const rate = productionRates[key] ?? 0;
        const cap = resourceCaps[key] ?? 0;
        const pct = cap > 0 ? Math.min(100, (current / cap) * 100) : 0;
        const isFull = pct >= 99;

        return (
          <motion.div
            key={key}
            whileHover={{ scale: 1.03, y: -1 }}
            transition={{ duration: 0.15 }}
            className={`relative flex flex-col gap-1.5 px-3 py-2.5 border rounded-lg cursor-default shadow-lg ${bgColor} ${borderColor} ${glowColor} ${isFull ? "animate-pulse" : ""}`}
          >
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-1.5">
                <Icon className={`w-3.5 h-3.5 ${color} shrink-0`} />
                <span className={`text-[10px] font-semibold uppercase tracking-wider ${color}`}>{label}</span>
              </div>
              {isFull && (
                <motion.span
                  initial={{ opacity: 0 }}
                  animate={{ opacity: [0.7, 1, 0.7] }}
                  transition={{ repeat: Infinity, duration: 1.5 }}
                  className="text-[9px] font-bold text-yellow-400 uppercase tracking-widest"
                >
                  FULL
                </motion.span>
              )}
            </div>

            <AnimatePresence mode="wait">
              <motion.div
                key={Math.floor(current / 10)}
                initial={{ opacity: 0.5, y: -3 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.25 }}
                className={`font-serif text-xl font-bold leading-none ${isFull ? "text-yellow-400" : "text-foreground"}`}
              >
                {fmt(current)}
              </motion.div>
            </AnimatePresence>

            <div className="flex items-center justify-between text-[10px]">
              <div className="flex items-center gap-0.5 text-emerald-400">
                {rate > 0 ? (
                  <>
                    <TrendingUp className="w-2.5 h-2.5" />
                    <span className="font-medium">+{rate}/m</span>
                  </>
                ) : (
                  <span className="text-muted-foreground/50">—</span>
                )}
              </div>
              <span className="text-muted-foreground/60">{fmt(cap)}</span>
            </div>

            <div className="w-full h-1 bg-black/40 rounded-full overflow-hidden">
              <motion.div
                className={`h-full rounded-full ${barColor}`}
                initial={false}
                animate={{ width: `${pct}%` }}
                transition={{ duration: 0.6, ease: "easeOut" }}
              />
            </div>
          </motion.div>
        );
      })}
    </div>
  );
}
