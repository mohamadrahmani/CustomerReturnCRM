"use client";

import { createContext, useContext, useEffect, useMemo, useState } from "react";
import type { AuthenticationResult } from "@/lib/api";
import { clearAuth, readAuth, readBusinessId, writeAuth } from "@/lib/auth";

type AuthContextValue = {
  auth: AuthenticationResult | null;
  businessId: string | null;
  isReady: boolean;
  setAuth: (value: AuthenticationResult) => void;
  setBusinessId: (id: string) => void;
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [auth, setAuthState] = useState<AuthenticationResult | null>(null);
  const [businessId, setBusinessIdState] = useState<string | null>(null);
  const [isReady, setIsReady] = useState(false);

  useEffect(() => {
    const currentAuth = readAuth();
    setAuthState(currentAuth);
    setBusinessIdState(readBusinessId(currentAuth));
    setIsReady(true);
  }, []);

  const value = useMemo<AuthContextValue>(() => ({
    auth,
    businessId,
    isReady,
    setAuth: (next) => {
      writeAuth(next);
      setAuthState(next);
      setBusinessIdState(readBusinessId(next));
    },
    setBusinessId: (id) => {
      if (!auth?.businesses.some((business) => business.id === id)) return;
      sessionStorage.setItem("crm_business_id", id);
      setBusinessIdState(id);
    },
    logout: () => {
      clearAuth();
      setAuthState(null);
      setBusinessIdState(null);
    },
  }), [auth, businessId, isReady]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used inside AuthProvider");
  return context;
}
