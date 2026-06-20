import { useEffect } from "react";
import { useLocation } from "wouter";
import { useAuth } from "@/hooks/use-auth";
import { ArrowLeft, Castle, Monitor } from "lucide-react";
import { motion } from "framer-motion";
import { Button } from "@/components/ui/button";

export default function KingdomPage() {
  const [, setLocation] = useLocation();
  const { user, isLoadingUser } = useAuth();

  useEffect(() => {
    if (!isLoadingUser && !user) setLocation("/");
  }, [user, isLoadingUser, setLocation]);

  if (isLoadingUser) {
    return (
      <div className="min-h-screen bg-black flex items-center justify-center">
        <div className="text-amber-400/60 text-sm animate-pulse font-serif">Loading realm…</div>
      </div>
    );
  }

  if (!user) return null;

  return (
    <div className="min-h-screen bg-background text-foreground flex flex-col">
      <header className="sticky top-0 z-40 border-b border-border/50 bg-background/92 backdrop-blur-md px-4 md:px-6 py-2.5">
        <div className="max-w-7xl mx-auto flex items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setLocation("/dashboard")}
              className="text-muted-foreground hover:text-foreground text-xs h-7 px-2"
            >
              <ArrowLeft className="w-3 h-3 mr-1.5" />
              Dashboard
            </Button>
            <div className="flex items-center gap-2">
              <Castle className="w-4 h-4 text-amber-400" />
              <span className="font-serif text-sm text-primary">Kingdom</span>
            </div>
          </div>
        </div>
      </header>

      <main className="flex-1 flex items-center justify-center p-6">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.4 }}
          className="max-w-lg w-full text-center space-y-6"
        >
          <div className="mx-auto w-20 h-20 rounded-2xl bg-card/60 border border-border/50 flex items-center justify-center shadow-xl shadow-black/50">
            <Castle className="w-10 h-10 text-amber-400/70" />
          </div>

          <div className="space-y-2">
            <h1 className="text-2xl font-serif text-primary">Kingdom Gameplay</h1>
            <p className="text-muted-foreground text-sm leading-relaxed">
              Kingdom gameplay is available in the Unity Client.
            </p>
          </div>

          <div className="bg-card/40 border border-border/50 rounded-xl p-5 text-left space-y-3">
            <h2 className="text-sm font-semibold text-foreground/80">Coming in the Unity Client</h2>
            <ul className="space-y-2 text-sm text-muted-foreground">
              <li className="flex items-start gap-2"><span className="text-amber-400 mt-0.5">▸</span> Full isometric 3D kingdom with animated buildings</li>
              <li className="flex items-start gap-2"><span className="text-amber-400 mt-0.5">▸</span> Palace, military ring, and resource ring layouts</li>
              <li className="flex items-start gap-2"><span className="text-amber-400 mt-0.5">▸</span> Real-time troop training and army management</li>
              <li className="flex items-start gap-2"><span className="text-amber-400 mt-0.5">▸</span> Live combat, sieges, and battle replays</li>
            </ul>
          </div>

          <Button
            size="lg"
            className="w-full bg-amber-700 hover:bg-amber-600 text-white border border-amber-600/50 font-semibold"
            disabled
          >
            <Monitor className="w-4 h-4 mr-2" />
            Launch Unity Client
            <span className="ml-2 text-xs opacity-60 font-normal">(coming soon)</span>
          </Button>
        </motion.div>
      </main>
    </div>
  );
}
