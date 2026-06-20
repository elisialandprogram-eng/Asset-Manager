import { useLogin, useRegister, useGetMe, getGetMeQueryKey } from "@workspace/api-client-react";
import { useLocation } from "wouter";
import { useToast } from "@/hooks/use-toast";
import { useEffect } from "react";

const TOKEN_KEY = "ek_token";

export function useAuth() {
  const [, setLocation] = useLocation();
  const { toast } = useToast();
  
  const loginMutation = useLogin();
  const registerMutation = useRegister();
  
  const { data: user, isLoading: isLoadingUser, error: userError } = useGetMe({
    query: {
      queryKey: getGetMeQueryKey(),
      retry: false,
      enabled: !!localStorage.getItem(TOKEN_KEY)
    }
  });

  const login = async (data: Parameters<typeof loginMutation.mutateAsync>[0]) => {
    try {
      const res = await loginMutation.mutateAsync(data);
      localStorage.setItem(TOKEN_KEY, res.token);
      setLocation("/dashboard");
    } catch (err: any) {
      toast({
        title: "Login failed",
        description: err.message || "Invalid credentials",
        variant: "destructive"
      });
    }
  };

  const register = async (data: Parameters<typeof registerMutation.mutateAsync>[0]) => {
    try {
      const res = await registerMutation.mutateAsync(data);
      localStorage.setItem(TOKEN_KEY, res.token);
      setLocation("/dashboard");
    } catch (err: any) {
      toast({
        title: "Registration failed",
        description: err.message || "Could not create kingdom",
        variant: "destructive"
      });
    }
  };

  const logout = () => {
    localStorage.removeItem(TOKEN_KEY);
    setLocation("/");
  };

  // If user error is 401, clear token
  useEffect(() => {
    if (userError) {
      localStorage.removeItem(TOKEN_KEY);
    }
  }, [userError]);

  return {
    user,
    isLoadingUser,
    login,
    register,
    logout,
    isLoggingIn: loginMutation.isPending,
    isRegistering: registerMutation.isPending
  };
}