import { useState, useEffect } from "react";
import { useLocation } from "wouter";
import { useAuth } from "@/hooks/use-auth";
import {
  useGetMyKingdom,
  useGetKingdomState,
  useGetKingdomQueue,
  useGetConstructionQueue,
  getGetMyKingdomQueryKey,
  getGetKingdomStateQueryKey,
  getGetKingdomQueueQueryKey,
  getGetConstructionQueueQueryKey,
  type Building,
} from "@workspace/api-client-react";
import { LogOut, Swords, Plus, Map, RefreshCw, Monitor, Castle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { motion } from "framer-motion";
import { ResourceHud } from "@/components/game/ResourceHud";
import { KingdomOverview } from "@/components/game/KingdomOverview";
import { BuildModal } from "@/components/game/BuildModal";
import { UpgradeModal } from "@/components/game/UpgradeModal";
import { ActivityFeed } from "@/components/game/ActivityFeed";

const POLL_MS = 8_000;

export default function Dashboard() {
  const [, setLocation] = useLocation();
  const { user, isLoadingUser, logout } = useAuth();
  const [buildModalOpen, setBuildModalOpen] = useState(false);
  const [selectedBuilding, setSelectedBuilding] = useState<Building | null>(null);

  const { data: myKingdom, isLoading: isLoadingKingdom } = useGetMyKingdom({
    query: { queryKey: getGetMyKingdomQueryKey(), enabled: !!user },
  });

  const kingdomId = myKingdom?.id ?? 0;

  const {
    data: state,
    isLoading: isLoadingState,
    isFetching,
  } = useGetKingdomState(kingdomId, {
    query: {
      queryKey: getGetKingdomStateQueryKey(kingdomId),
      enabled: !!kingdomId,
      refetchInterval: POLL_MS,
      staleTime: POLL_MS / 2,
    },
  });

  const { data: queue = [] } = useGetKingdomQueue(kingdomId, {
    query: {
      queryKey: getGetKingdomQueueQueryKey(kingdomId),
      enabled: !!kingdomId,
      refetchInterval: POLL_MS,
    },
  });

  const { data: constructionQueue = [] } = useGetConstructionQueue(kingdomId, {
    query: {
      queryKey: getGetConstructionQueueQueryKey(kingdomId),
      enabled: !!kingdomId,
      refetchInterval: POLL_MS,
    },
  });

  useEffect(() => {
    if (!isLoadingUser && !user) setLocation("/");
  }, [user, isLoadingUser, setLocation]);

  const isBooting = isLoadingUser || isLoadingKingdom || isLoadingState;

  if (isBooting || !user || !myKingdom || !state) {
    return (
      <div className="min-h-screen flex flex-col items-center justify-center bg-background text-primary gap-4">
        <motion.div
          animate={{ opacity: [0.4, 1, 0.4] }}
          transition={{ repeat: Infinity, duration: 2 }}
          className="font-serif text-3xl"
        >
          Loading Realm…
        </motion.div>
        <div className="text-sm text-muted-foreground">Gathering your kingdom&apos;s intelligence</div>
      </div>
    );
  }

  const palaceBuilding = state.buildings.find((b: Building) => b.buildingType === "palace");
  const palaceLevel = palaceBuilding?.level ?? 1;
  const existingBuildings = state.buildings.filter(
    (b: Building) => b.buildingType !== "palace" && b.level > 0
  );
  const maxResourceBuildings = state.palaceTier.maxResourceBuildings;
  const slotDisplay = `${existingBuildings.length} / ${maxResourceBuildings}`;

  return (
    <div className="min-h-screen w-full bg-background text-foreground">
      {/* Header */}
      <header className="sticky top-0 z-40 border-b border-border/50 bg-background/92 backdrop-blur-md px-4 md:px-6 py-2.5">
        <div className="max-w-7xl mx-auto flex items-center justify-between gap-4">
          <motion.div
            initial={{ opacity: 0, x: -16 }}
            animate={{ opacity: 1, x: 0 }}
            className="flex items-center gap-3"
          >
            <div className="p-1.5 rounded bg-primary/10 border border-primary/20">
              <Swords className="w-4 h-4 text-primary" />
            </div>
            <div>
              <h1 className="text-lg font-serif font-bold text-primary leading-tight">
                {myKingdom.name}
              </h1>
              <p className="text-[11px] text-muted-foreground">
                Lord <span className="text-foreground/80">{user.username}</span>
                &ensp;·&ensp;Power{" "}
                <span className="text-primary font-bold">{state.power.toLocaleString()}</span>
              </p>
            </div>
          </motion.div>

          <div className="flex items-center gap-2">
            <div className="hidden sm:flex items-center gap-1 text-[10px] text-muted-foreground/60">
              <RefreshCw className={`w-2.5 h-2.5 ${isFetching ? "animate-spin text-primary/60" : ""}`} />
              <span>{isFetching ? "Syncing…" : "Live"}</span>
            </div>
            <Button
              variant="outline"
              size="sm"
              className="border-border/60 hover:bg-border/40 text-foreground text-xs h-7 px-3"
              onClick={() => setLocation("/world")}
            >
              <Map className="w-3 h-3 mr-1.5" />
              World Map
            </Button>
            <Button
              variant="outline"
              size="sm"
              className="border-border/60 hover:bg-border/40 text-muted-foreground text-xs h-7 px-3"
              onClick={logout}
            >
              <LogOut className="w-3 h-3 mr-1.5" />
              Depart
            </Button>
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto p-4 md:p-6 space-y-5">
        {/* Resource HUD */}
        <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.04 }}>
          <ResourceHud
            resources={state.resources}
            productionRates={state.productionRates}
            resourceCaps={state.resourceCaps}
          />
        </motion.div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
          {/* Main column */}
          <div className="lg:col-span-2 space-y-5">

            {/* Kingdom — Unity Client placeholder */}
            <motion.section
              initial={{ opacity: 0, y: 14 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.08 }}
              className="bg-card/40 border border-border/50 rounded-xl overflow-hidden shadow-xl shadow-black/50"
            >
              <div className="flex items-center justify-between px-5 py-3 border-b border-border/40 bg-black/30">
                <h2 className="text-base font-serif text-primary flex items-center gap-2">
                  <span className="text-yellow-400">⚜</span>
                  Kingdom — {myKingdom.name}
                </h2>
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => setBuildModalOpen(true)}
                  className="text-xs h-7 px-3 border-emerald-700/50 text-emerald-400 hover:bg-emerald-950/30 hover:border-emerald-600/70"
                >
                  <Plus className="w-3 h-3 mr-1" />
                  Construct
                </Button>
              </div>

              {/* Unity Client placeholder viewport */}
              <div className="flex flex-col items-center justify-center gap-5 py-16 px-8 bg-gradient-to-b from-black/40 to-black/10" style={{ minHeight: "320px" }}>
                <div className="w-16 h-16 rounded-2xl bg-card/60 border border-border/50 flex items-center justify-center shadow-xl shadow-black/50">
                  <Castle className="w-8 h-8 text-amber-400/60" />
                </div>
                <div className="text-center space-y-1.5">
                  <p className="text-base font-serif text-foreground/70">Kingdom gameplay is available in the Unity Client.</p>
                  <p className="text-xs text-muted-foreground/50">Full isometric 3D kingdom · buildings · armies · battles</p>
                </div>
                <Button
                  size="sm"
                  className="bg-amber-700/60 hover:bg-amber-600/70 text-white border border-amber-600/40 text-xs"
                  onClick={() => setLocation("/kingdom")}
                >
                  <Monitor className="w-3.5 h-3.5 mr-1.5" />
                  Launch Unity Client
                </Button>
              </div>

              <div className="px-5 py-2 border-t border-border/30 bg-black/20 flex items-center gap-3 text-[11px] text-muted-foreground">
                <span className="font-mono bg-muted/20 border border-border/30 px-1.5 py-0.5 rounded">
                  {slotDisplay} structure slots
                </span>
                <span className="opacity-50">Use Construct to queue new buildings · manage upgrades in Activity</span>
              </div>
            </motion.section>

          </div>

          {/* Sidebar */}
          <div className="space-y-4">
            <motion.div initial={{ opacity: 0, x: 14 }} animate={{ opacity: 1, x: 0 }} transition={{ delay: 0.2 }}>
              <KingdomOverview
                kingdom={state.kingdom}
                power={state.power}
                palaceTier={state.palaceTier}
                palaceLevel={palaceLevel}
                username={user.username}
              />
            </motion.div>

            <motion.div initial={{ opacity: 0, x: 14 }} animate={{ opacity: 1, x: 0 }} transition={{ delay: 0.25 }}>
              <ActivityFeed upgradeQueue={queue} constructionQueue={constructionQueue} />
            </motion.div>

            {/* Military placeholder */}
            <motion.section
              initial={{ opacity: 0, x: 14 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{ delay: 0.3 }}
              className="bg-card/40 border border-border/50 rounded-xl p-5 shadow-lg shadow-black/40"
            >
              <h2 className="text-base font-serif text-primary mb-3 flex items-center gap-2">
                <Swords className="w-4 h-4 text-primary/60" />
                Military Forces
              </h2>
              <div className="text-center py-4 text-muted-foreground/40 text-sm italic flex flex-col items-center gap-2">
                <Swords className="w-7 h-7 opacity-20" />
                Armies unlock in the Unity Client
              </div>
            </motion.section>
          </div>
        </div>
      </main>

      {/* Modals */}
      <BuildModal
        kingdomId={kingdomId}
        open={buildModalOpen}
        onClose={() => setBuildModalOpen(false)}
      />

      {selectedBuilding && (
        <UpgradeModal
          building={selectedBuilding}
          kingdomId={kingdomId}
          open={!!selectedBuilding}
          onClose={() => setSelectedBuilding(null)}
        />
      )}
    </div>
  );
}
