"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useAuth } from "./auth-provider";
import { AppShell } from "./app-shell";

export function RouteGuard({ children }: { children: React.ReactNode }) {
  const { auth, isReady } = useAuth();
  const pathname = usePathname();
  const router = useRouter();
  const isLogin = pathname === "/login";
  const isSetup = pathname === "/setup";

  useEffect(() => {
    if (!isReady) return;
    if (!auth && !isLogin) {
      router.replace("/login");
      return;
    }
    if (auth && isLogin) {
      router.replace(auth.businesses.length > 0 ? "/dashboard" : "/setup");
      return;
    }
    if (auth && auth.businesses.length === 0 && !isSetup) {
      router.replace("/setup");
    }
    if (auth && auth.businesses.length > 0 && isSetup) {
      router.replace("/dashboard");
    }
  }, [auth, isLogin, isSetup, isReady, router]);

  if (!isReady || (!auth && !isLogin) || (auth && isLogin) || (auth && auth.businesses.length === 0 && !isSetup) || (auth && auth.businesses.length > 0 && isSetup)) {
    return <div className="flex min-h-screen items-center justify-center text-sm text-slate-500">در حال بارگذاری...</div>;
  }

  if (isLogin || isSetup) return <>{children}</>;
  return <AppShell>{children}</AppShell>;
}
