import { Shield, Crown, Zap, TrendingUp } from "lucide-react";
import { motion } from "framer-motion";
import type { Kingdom, PalaceTierConfig } from "@workspace/api-client-react";

interface KingdomOverviewProps {
  kingdom: Kingdom;
  power: number;
  palaceTier: PalaceTierConfig;
  palaceLevel: number;
  username: string;
}

const TIER_LABELS: Record<number, { label: string; color: string }> = {
  1: { label: "Tier I — Hamlet", color: "text-slate-400" },
  2: { label: "Tier II — Village", color: "text-green-500" },
  3: { label: "Tier III — Town", color: "text-blue-400" },
  4: { label: "Tier IV — City", color: "text-purple-400" },
  5: { label: "Tier V — Capital", color: "text-yellow-400" },
};

function getTier(palaceLevel: number) {
  if (palaceLevel >= 20) return 5;
  if (palaceLevel >= 15) return 4;
  if (palaceLevel >= 10) return 3;
  if (palaceLevel >= 5) return 2;
  return 1;
}

export function KingdomOverview({ kingdom, power, palaceTier, palaceLevel, username }: KingdomOverviewProps) {
  const tier = getTier(palaceLevel);
  const tierInfo = TIER_LABELS[tier];

  const stats = [
    {
      icon: Crown,
      label: "Palace",
      value: `Level ${palaceLevel}`,
      color: "text-yellow-400",
    },
    {
      icon: Zap,
      label: "Power",
      value: power.toLocaleString(),
      color: "text-amber-400",
    },
    {
      icon: Shield,
      label: "Max Structures",
      value: palaceTier.maxResourceBuildings + (palaceTier.maxBarracks > 0 ? ` + ${palaceTier.maxBarracks} barracks` : ""),
      color: "text-slate-300",
    },
    {
      icon: TrendingUp,
      label: "Season",
      value: "Age of Embers",
      color: "text-orange-400",
    },
  ];

  return (
    <section className="bg-card/50 border border-border p-6 shadow-lg shadow-black/50">
      <h2 className="text-xl font-serif text-primary mb-1 border-b border-border/30 pb-2">Kingdom</h2>

      <div className="mt-4 space-y-1 mb-4">
        <div className="text-2xl font-serif font-bold text-foreground">{kingdom.name}</div>
        <div className="text-sm text-muted-foreground">Lord {username}</div>
        <div className={`text-sm font-semibold mt-1 ${tierInfo.color}`}>{tierInfo.label}</div>
      </div>

      <div className="space-y-3">
        {stats.map(({ icon: Icon, label, value, color }) => (
          <div key={label} className="flex items-center justify-between py-1 border-b border-border/20 last:border-0">
            <div className="flex items-center gap-2 text-muted-foreground text-sm">
              <Icon className={`w-3.5 h-3.5 ${color}`} />
              {label}
            </div>
            <span className={`text-sm font-semibold ${color}`}>{value}</span>
          </div>
        ))}
      </div>

      {palaceTier.unlocksFeatures.length > 0 && (
        <div className="mt-4 pt-3 border-t border-border/30">
          <div className="text-xs text-muted-foreground uppercase tracking-wider mb-2">Unlocked</div>
          <div className="flex flex-wrap gap-1">
            {palaceTier.unlocksFeatures.map((f: string) => (
              <span key={f} className="text-xs px-2 py-0.5 bg-primary/10 border border-primary/20 text-primary capitalize">
                {f.replace(/_/g, " ")}
              </span>
            ))}
          </div>
        </div>
      )}

      {palaceLevel < 20 && (
        <motion.div
          className="mt-4 pt-3 border-t border-border/30"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
        >
          <div className="text-xs text-muted-foreground">
            {palaceLevel < 5 && `Upgrade Palace to L5 to unlock Barracks`}
            {palaceLevel >= 5 && palaceLevel < 10 && `Upgrade Palace to L10 to unlock Militia`}
            {palaceLevel >= 10 && palaceLevel < 15 && `Upgrade Palace to L15 to unlock Spearmen`}
            {palaceLevel >= 15 && palaceLevel < 20 && `Upgrade Palace to L20 to unlock Scouts`}
          </div>
        </motion.div>
      )}
    </section>
  );
}
