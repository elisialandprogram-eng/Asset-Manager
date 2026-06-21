import { Switch, Route, Router as WouterRouter } from "wouter";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { Toaster } from "@/components/ui/toaster";
import { TooltipProvider } from "@/components/ui/tooltip";
import NotFound from "@/pages/not-found";
import Login from "@/pages/login";
import Register from "@/pages/register";
import UnityLauncher from "@/pages/UnityLauncher";

const queryClient = new QueryClient();

// Warm the browser cache for the Unity loader before the user hits /dashboard
const _unityPreload = document.createElement("link");
_unityPreload.rel = "prefetch";
_unityPreload.as = "script";
_unityPreload.href = "/unity/Build/EternalKingdoms.loader.js";
document.head.appendChild(_unityPreload);

function Router() {
  return (
    <Switch>
      <Route path="/" component={Login} />
      <Route path="/register" component={Register} />
      <Route path="/dashboard" component={UnityLauncher} />
      <Route path="/world" component={UnityLauncher} />
      <Route path="/kingdom" component={UnityLauncher} />
      <Route component={NotFound} />
    </Switch>
  );
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <TooltipProvider>
        <WouterRouter base={import.meta.env.BASE_URL.replace(/\/$/, "")}>
          <Router />
        </WouterRouter>
        <Toaster />
      </TooltipProvider>
    </QueryClientProvider>
  );
}

export default App;
