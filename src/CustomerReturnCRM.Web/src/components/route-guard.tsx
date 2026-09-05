"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useAuth } from "./auth-provider";
import { AppShell } from "./app-shell";

export function RouteGuard({ children }: { children: React.ReactNode }) {
  const { auth, isReady } = useAuth();
  const pathname = usePathname();
  const router = useRouter();
  const isPublic = pathname === "/login" || pathname === "/register";
  const isSetup = pathname === "/setup";

  useEffect(() => {
    if (!isReady) return;

    if (!auth && !isPublic) {
      router.replace("/login");
      return;
    }

    if (auth && isPublic) {
      router.replace(auth.businesses.length > 0 ? "/dashboard" : "/setup");
      return;
    }

    if (auth && auth.businesses.length === 0 && !isSetup) {
      router.replace("/setup");
      return;
    }

    if (auth && auth.businesses.length > 0 && isSetup) {
      router.replace("/dashboard");
    }
  }, [auth, isPublic, isSetup, isReady, router]);

  if (
    !isReady ||
    (!auth && !isPublic) ||
    (auth && isPublic) ||
    (auth && auth.businesses.length === 0 && !isSetup) ||
    (auth && auth.businesses.length > 0 && isSetup)
  ) {
    return <div className="flex min-h-screen items-center justify-center text-sm text-slate-500">در حال بارگذاری...</div>;
  }

  if (isPublic || isSetup) return <>{children}</>;
  return <AppShell>{children}</AppShell>;
}
