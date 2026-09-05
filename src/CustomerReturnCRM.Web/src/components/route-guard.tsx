"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useAuth } from "./auth-provider";

export function RouteGuard({ children }: { children: React.ReactNode }) {
  const { auth, isReady } = useAuth();
  const pathname = usePathname();
  const router = useRouter();
  const isLogin = pathname === "/login";

  useEffect(() => {
    if (!isReady) return;
    if (!auth && !isLogin) router.replace("/login");
    if (auth && isLogin) router.replace("/dashboard");
  }, [auth, isLogin, isReady, router]);

  if (!isReady || (!auth && !isLogin) || (auth && isLogin)) {
    return <div className="flex min-h-screen items-center justify-center text-sm text-slate-500">در حال بارگذاری...</div>;
  }

  return <>{children}</>;
}
