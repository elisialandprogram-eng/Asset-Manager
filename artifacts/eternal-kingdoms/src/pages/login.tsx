import { useState } from "react";
import { useAuth } from "@/hooks/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Link } from "wouter";
import { motion } from "framer-motion";

export default function Login() {
  const { login, isLoggingIn } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    login({ data: { email, password } });
  };

  return (
    <div className="min-h-screen w-full flex items-center justify-center relative overflow-hidden bg-background">
      <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_center,_var(--tw-gradient-stops))] from-primary/10 via-background to-background pointer-events-none" />
      
      <motion.div 
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.8 }}
        className="relative z-10 w-full max-w-md p-8 bg-card/80 backdrop-blur-sm border border-border shadow-2xl shadow-primary/5"
      >
        <div className="text-center mb-10">
          <h1 className="text-4xl font-serif font-bold text-primary mb-2">Eternal Kingdoms</h1>
          <p className="text-muted-foreground">Enter your realm</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          <div className="space-y-2">
            <Label htmlFor="email" className="text-primary/80 uppercase tracking-widest text-xs">Email</Label>
            <Input 
              id="email" 
              type="email" 
              required 
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="bg-background/50 border-primary/20 focus-visible:ring-primary/50 text-foreground"
            />
          </div>
          
          <div className="space-y-2">
            <Label htmlFor="password" className="text-primary/80 uppercase tracking-widest text-xs">Password</Label>
            <Input 
              id="password" 
              type="password" 
              required 
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="bg-background/50 border-primary/20 focus-visible:ring-primary/50 text-foreground"
            />
          </div>

          <Button 
            type="submit" 
            className="w-full h-12 bg-primary hover:bg-primary/90 text-primary-foreground font-serif text-lg tracking-wide uppercase transition-all"
            disabled={isLoggingIn}
          >
            {isLoggingIn ? "Entering..." : "Enter Your Kingdom"}
          </Button>
        </form>

        <div className="mt-8 text-center border-t border-border/50 pt-6">
          <p className="text-sm text-muted-foreground">
            A new ruler?{" "}
            <Link href="/register" className="text-primary hover:text-primary/80 transition-colors uppercase tracking-wider font-semibold">
              Forge Your Kingdom
            </Link>
          </p>
        </div>
      </motion.div>
    </div>
  );
}