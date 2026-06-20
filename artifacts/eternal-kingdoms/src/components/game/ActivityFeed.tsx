import { motion, AnimatePresence } from "framer-motion";
import { ArrowUp, Hammer, CheckCircle, Clock } from "lucide-react";
import type { UpgradeQueueItem, ConstructionQueueItem } from "@workspace/api-client-react";

interface ActivityFeedProps {
  upgradeQueue: UpgradeQueueItem[];
  constructionQueue: ConstructionQueueItem[];
}

const BUILDING_LABELS: Record<string, string> = {
  palace: "Palace", farm: "Farm", lumber_mill: "Lumber Mill",
  quarry: "Quarry", iron_mine: "Iron Mine", gold_mine: "Gold Mine",
  barracks: "Barracks", wall: "Wall", watch_tower: "Watch Tower",
};

function useCountdown(endsAt: string) {
  const remaining = Math.max(0, Math.ceil((new Date(endsAt).getTime() - Date.now()) / 1000));
  const mins = Math.floor(remaining / 60);
  const secs = remaining % 60;
  return mins > 0 ? `${mins}m ${secs}s` : `${secs}s`;
}

function UpgradeEntry({ item }: { item: UpgradeQueueItem }) {
  const timeLeft = useCountdown(item.endsAt);
  const label = "Building";

  return (
    <motion.div
      initial={{ opacity: 0, x: -8 }}
      animate={{ opacity: 1, x: 0 }}
      exit={{ opacity: 0, x: -8 }}
      className="flex items-center gap-2.5 py-2 border-b border-border/15 last:border-0"
    >
      <div className="p-1.5 rounded bg-amber-950/50 border border-amber-700/30 shrink-0">
        <ArrowUp className="w-3 h-3 text-amber-400" />
      </div>
      <div className="flex-1 min-w-0">
        <div className="text-xs font-medium text-foreground leading-tight truncate">
          {label} <span className="text-amber-400">→ L{item.toLevel}</span>
        </div>
        <div className="flex items-center gap-1 mt-0.5">
          <Clock className="w-2.5 h-2.5 text-amber-500/70" />
          <span className="text-[10px] text-amber-400/80 font-mono">{timeLeft}</span>
        </div>
      </div>
      <div className="shrink-0 text-[10px] text-amber-500/60 uppercase tracking-wider">Upgrading</div>
    </motion.div>
  );
}

function ConstructEntry({ item }: { item: ConstructionQueueItem }) {
  const timeLeft = useCountdown(item.endsAt);
  const label = BUILDING_LABELS[item.buildingType ?? ""] ?? "Building";

  return (
    <motion.div
      initial={{ opacity: 0, x: -8 }}
      animate={{ opacity: 1, x: 0 }}
      exit={{ opacity: 0, x: -8 }}
      className="flex items-center gap-2.5 py-2 border-b border-border/15 last:border-0"
    >
      <div className="p-1.5 rounded bg-emerald-950/50 border border-emerald-700/30 shrink-0">
        <Hammer className="w-3 h-3 text-emerald-400" />
      </div>
      <div className="flex-1 min-w-0">
        <div className="text-xs font-medium text-foreground leading-tight truncate">
          <span className="text-emerald-400">New</span> {label}
        </div>
        <div className="flex items-center gap-1 mt-0.5">
          <Clock className="w-2.5 h-2.5 text-emerald-500/70" />
          <span className="text-[10px] text-emerald-400/80 font-mono">{timeLeft}</span>
        </div>
      </div>
      <div className="shrink-0 text-[10px] text-emerald-500/60 uppercase tracking-wider">Building</div>
    </motion.div>
  );
}

export function ActivityFeed({ upgradeQueue, constructionQueue }: ActivityFeedProps) {
  const total = upgradeQueue.length + constructionQueue.length;

  return (
    <section className="bg-card/40 border border-border/60 rounded-lg shadow-lg shadow-black/40 p-4">
      <div className="flex items-center justify-between mb-3">
        <div className="flex items-center gap-2">
          <CheckCircle className="w-3.5 h-3.5 text-primary/70" />
          <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Active Work</h3>
        </div>
        {total > 0 && (
          <span className="text-[10px] px-1.5 py-0.5 bg-primary/10 border border-primary/20 text-primary rounded font-medium">
            {total} task{total !== 1 ? "s" : ""}
          </span>
        )}
      </div>

      {total === 0 ? (
        <div className="text-center py-3 text-muted-foreground/40 text-xs italic">
          No active tasks — your builders rest
        </div>
      ) : (
        <AnimatePresence mode="popLayout">
          {constructionQueue.map((item) => (
            <ConstructEntry key={`c-${item.id}`} item={item} />
          ))}
          {upgradeQueue.map((item) => (
            <UpgradeEntry key={`u-${item.id}`} item={item} />
          ))}
        </AnimatePresence>
      )}
    </section>
  );
}
