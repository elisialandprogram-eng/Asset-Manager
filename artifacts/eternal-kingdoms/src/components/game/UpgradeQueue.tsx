import { useEffect, useState } from "react";
import { Clock, Hammer } from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";
import type { UpgradeQueueItem } from "@workspace/api-client-react";

interface UpgradeQueueProps {
  queue: UpgradeQueueItem[];
}

function formatBuildingType(t: string) {
  return t.replace(/_/g, " ").replace(/\b\w/g, (c) => c.toUpperCase());
}

function Countdown({ endsAt }: { endsAt: string }) {
  const [remaining, setRemaining] = useState(() => {
    const diff = Math.max(0, new Date(endsAt).getTime() - Date.now());
    return Math.ceil(diff / 1000);
  });

  useEffect(() => {
    const id = setInterval(() => {
      const diff = Math.max(0, new Date(endsAt).getTime() - Date.now());
      setRemaining(Math.ceil(diff / 1000));
    }, 1000);
    return () => clearInterval(id);
  }, [endsAt]);

  if (remaining <= 0) {
    return <span className="text-green-400 font-semibold text-sm">Completing…</span>;
  }

  const mins = Math.floor(remaining / 60);
  const secs = remaining % 60;
  const display = mins > 0
    ? `${mins}m ${secs.toString().padStart(2, "0")}s`
    : `${secs}s`;

  const totalSeconds = Math.max(1, (new Date(endsAt).getTime() - new Date(/* startsAt */ 0).getTime()) / 1000);
  const pct = Math.max(0, Math.min(100, 100 - (remaining / Math.max(1, remaining + 1)) * 100));

  return (
    <div className="flex flex-col gap-1 items-end">
      <div className="flex items-center gap-1.5 text-amber-400 font-mono text-sm font-semibold">
        <Clock className="w-3.5 h-3.5" />
        {display}
      </div>
    </div>
  );
}

function QueueItem({ item, buildingNames }: { item: UpgradeQueueItem; buildingNames: Record<number, string> }) {
  const endsAt = new Date(item.endsAt);
  const now = Date.now();
  const totalMs = endsAt.getTime() - new Date(item.startsAt).getTime();
  const [elapsed, setElapsed] = useState(now - new Date(item.startsAt).getTime());

  useEffect(() => {
    const id = setInterval(() => {
      setElapsed(Date.now() - new Date(item.startsAt).getTime());
    }, 1000);
    return () => clearInterval(id);
  }, [item.startsAt]);

  const pct = Math.min(100, Math.max(0, (elapsed / totalMs) * 100));
  const name = buildingNames[item.buildingId] ?? `Building #${item.buildingId}`;

  return (
    <motion.div
      layout
      initial={{ opacity: 0, y: -8 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: 8 }}
      className="p-4 border border-amber-900/40 bg-amber-950/20 space-y-2"
    >
      <div className="flex items-start justify-between gap-2">
        <div className="flex items-center gap-2">
          <Hammer className="w-4 h-4 text-amber-500 shrink-0 animate-pulse" />
          <div>
            <div className="text-sm font-semibold text-foreground">
              {name}
            </div>
            <div className="text-xs text-muted-foreground">
              Level {item.fromLevel} → Level {item.toLevel}
            </div>
          </div>
        </div>
        <Countdown endsAt={item.endsAt} />
      </div>

      <div className="w-full h-1.5 bg-background rounded-full overflow-hidden border border-border/20">
        <motion.div
          className="h-full rounded-full bg-amber-500/70"
          animate={{ width: `${pct}%` }}
          transition={{ duration: 0.8, ease: "linear" }}
        />
      </div>
    </motion.div>
  );
}

export function UpgradeQueue({ queue }: UpgradeQueueProps) {
  if (queue.length === 0) return null;

  const buildingNames: Record<number, string> = {};
  queue.forEach((item) => {
    buildingNames[item.buildingId] = `Building #${item.buildingId}`;
  });

  return (
    <section className="bg-card/50 border border-amber-900/40 p-6 shadow-lg shadow-black/50">
      <div className="flex items-center gap-2 mb-4 border-b border-border/30 pb-2">
        <Hammer className="w-5 h-5 text-amber-500" />
        <h2 className="text-xl font-serif text-primary">Under Construction</h2>
        <span className="ml-auto text-xs bg-amber-500/20 text-amber-400 border border-amber-500/30 px-2 py-0.5 font-semibold">
          {queue.length} active
        </span>
      </div>
      <div className="space-y-3">
        <AnimatePresence mode="popLayout">
          {queue.map((item) => (
            <QueueItem key={item.id} item={item} buildingNames={buildingNames} />
          ))}
        </AnimatePresence>
      </div>
    </section>
  );
}
