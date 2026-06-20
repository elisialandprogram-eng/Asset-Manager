import { useEffect, useState } from "react";
import { Hammer, Clock, Wheat, TreePine, Mountain, Coins, Shield, Eye, Building2 } from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";
import type { ConstructionQueueItem } from "@workspace/api-client-react";

interface ConstructionQueueProps {
  queue: ConstructionQueueItem[];
}

const BUILDING_ICONS: Record<string, React.ReactNode> = {
  farm: <Wheat className="w-4 h-4 text-amber-500" />,
  lumber_mill: <TreePine className="w-4 h-4 text-green-600" />,
  quarry: <Mountain className="w-4 h-4 text-slate-400" />,
  iron_mine: <Hammer className="w-4 h-4 text-zinc-300" />,
  gold_mine: <Coins className="w-4 h-4 text-yellow-400" />,
  barracks: <Shield className="w-4 h-4 text-blue-400" />,
  wall: <Shield className="w-4 h-4 text-slate-500" />,
  watch_tower: <Eye className="w-4 h-4 text-cyan-400" />,
  palace: <Building2 className="w-4 h-4 text-yellow-400" />,
};

const BUILDING_LABELS: Record<string, string> = {
  farm: "Farm",
  lumber_mill: "Lumber Mill",
  quarry: "Quarry",
  iron_mine: "Iron Mine",
  gold_mine: "Gold Mine",
  barracks: "Barracks",
  wall: "Wall",
  watch_tower: "Watch Tower",
  palace: "Palace",
};

function Countdown({ endsAt }: { endsAt: string }) {
  const [remaining, setRemaining] = useState(() =>
    Math.max(0, Math.ceil((new Date(endsAt).getTime() - Date.now()) / 1000))
  );

  useEffect(() => {
    const id = setInterval(() => {
      setRemaining(Math.max(0, Math.ceil((new Date(endsAt).getTime() - Date.now()) / 1000)));
    }, 1000);
    return () => clearInterval(id);
  }, [endsAt]);

  if (remaining <= 0) {
    return <span className="text-green-400 font-semibold text-sm">Completing…</span>;
  }

  const mins = Math.floor(remaining / 60);
  const secs = remaining % 60;
  const display = mins > 0 ? `${mins}m ${secs.toString().padStart(2, "0")}s` : `${secs}s`;

  return (
    <div className="flex items-center gap-1.5 text-emerald-400 font-mono text-sm font-semibold">
      <Clock className="w-3.5 h-3.5" />
      {display}
    </div>
  );
}

function ConstructionItem({ item }: { item: ConstructionQueueItem }) {
  const icon = BUILDING_ICONS[item.buildingType];
  const label = BUILDING_LABELS[item.buildingType] ?? item.buildingType.replace(/_/g, " ");

  const totalMs = new Date(item.endsAt).getTime() - new Date(item.startsAt).getTime();
  const [elapsed, setElapsed] = useState(Date.now() - new Date(item.startsAt).getTime());

  useEffect(() => {
    const id = setInterval(() => {
      setElapsed(Date.now() - new Date(item.startsAt).getTime());
    }, 1000);
    return () => clearInterval(id);
  }, [item.startsAt]);

  const pct = Math.min(100, Math.max(0, (elapsed / totalMs) * 100));

  return (
    <motion.div
      layout
      initial={{ opacity: 0, y: -8 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: 8 }}
      className="p-4 border border-emerald-900/40 bg-emerald-950/10 space-y-2"
    >
      <div className="flex items-start justify-between gap-2">
        <div className="flex items-center gap-2">
          <div className="p-1.5 border border-emerald-900/40 bg-emerald-950/20 rounded-sm animate-pulse">
            {icon}
          </div>
          <div>
            <div className="text-sm font-semibold text-foreground">{label}</div>
            <div className="text-xs text-muted-foreground">Constructing — Level 1</div>
          </div>
        </div>
        <Countdown endsAt={item.endsAt} />
      </div>

      <div className="w-full h-1.5 bg-background rounded-full overflow-hidden border border-border/20">
        <motion.div
          className="h-full rounded-full bg-emerald-500/70"
          animate={{ width: `${pct}%` }}
          transition={{ duration: 0.8, ease: "linear" }}
        />
      </div>

      <div className="flex gap-3 text-xs text-muted-foreground">
        {(item.foodCost ?? 0) > 0 && <span>Food {item.foodCost}</span>}
        {(item.woodCost ?? 0) > 0 && <span>Wood {item.woodCost}</span>}
        {(item.stoneCost ?? 0) > 0 && <span>Stone {item.stoneCost}</span>}
        {(item.ironCost ?? 0) > 0 && <span>Iron {item.ironCost}</span>}
        {(item.goldCost ?? 0) > 0 && <span>Gold {item.goldCost}</span>}
      </div>
    </motion.div>
  );
}

export function ConstructionQueue({ queue }: ConstructionQueueProps) {
  if (queue.length === 0) return null;

  return (
    <section className="bg-card/50 border border-emerald-900/40 p-6 shadow-lg shadow-black/50">
      <div className="flex items-center gap-2 mb-4 border-b border-border/30 pb-2">
        <Hammer className="w-5 h-5 text-emerald-500" />
        <h2 className="text-xl font-serif text-primary">New Construction</h2>
        <span className="ml-auto text-xs bg-emerald-500/20 text-emerald-400 border border-emerald-500/30 px-2 py-0.5 font-semibold">
          {queue.length} building{queue.length > 1 ? "s" : ""}
        </span>
      </div>
      <div className="space-y-3">
        <AnimatePresence mode="popLayout">
          {queue.map((item) => (
            <ConstructionItem key={item.id} item={item} />
          ))}
        </AnimatePresence>
      </div>
    </section>
  );
}
