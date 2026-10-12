"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useAuth } from "./auth-provider";
import { AppShell } from "./app-shell";

function hasValidLicense(business: { licenseIsActive: boolean; licenseStartDate: string | null; licenseEndDate: string | null } | undefined) {
  if (!business || !business.licenseIsActive) return false;
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  if (business.licenseStartDate && new Date(business.licenseStartDate).getTime() > today.getTime()) return false;
  if (business.licenseEndDate) {
    const end = new Date(business.licenseEndDate);
    end.setHours(23, 59, 59, 999);
    if (end.getTime() < Date.now()) return false;
  }
  return true;
}

export function RouteGuard({ children }: { children: React.ReactNode }) {
  const { auth, businessId, isReady } = useAuth();
  const pathname = usePathname();
  const router = useRouter();
  const isLicensePage = pathname === "/license-required";
  const isAdminRoute = pathname.startsWith("/admin");
  const isPublic = pathname === "/login" || pathname === "/register" || isLicensePage || pathname.startsWith("/b/") || pathname.startsWith("/a/");
  const isSetup = pathname === "/setup";
  const selectedBusiness = auth?.businesses.find((item) => item.id === businessId) ?? auth?.businesses[0];
  const licenseBlocked = !!auth && !auth.isCentralAdmin && !!selectedBusiness && !hasValidLicense(selectedBusiness);

  useEffect(() => {
    if (!isReady) return;
    if (!auth && !isPublic) {
      router.replace("/login");
      return;
    }
    if (auth && (pathname === "/login" || pathname === "/register")) {
      router.replace(auth.isCentralAdmin ? "/admin/businesses" : licenseBlocked ? "/license-required" : auth.businesses.length > 0 ? "/dashboard" : "/setup");
      return;
    }
    if (auth && licenseBlocked && !isAdminRoute && pathname !== "/license-required") {
      router.replace("/license-required");
      return;
    }
    if (auth && isAdminRoute && !auth.isCentralAdmin) {
      router.replace(licenseBlocked ? "/license-required" : "/dashboard");
      return;
    }
    if (auth && !auth.isCentralAdmin && auth.businesses.length === 0 && !isSetup && !isPublic) {
      router.replace("/setup");
      return;
    }
    if (auth && !auth.isCentralAdmin && auth.businesses.length > 0 && isSetup) router.replace("/dashboard");
  }, [auth, businessId, isPublic, isSetup, isReady, pathname, router, licenseBlocked, isAdminRoute]);

  if (
    !isReady ||
    (!auth && !isPublic) ||
    (auth && (pathname === "/login" || pathname === "/register")) ||
    (auth && licenseBlocked && !isAdminRoute && pathname !== "/license-required") ||
    (auth && isAdminRoute && !auth.isCentralAdmin) ||
    (auth && !auth.isCentralAdmin && auth.businesses.length === 0 && !isSetup && !isPublic) ||
    (auth && !auth.isCentralAdmin && auth.businesses.length > 0 && isSetup)
  ) {
    return <div className="flex min-h-screen items-center justify-center text-sm text-slate-500">در حال بارگذاری...</div>;
  }

  if (isPublic || isSetup || isAdminRoute) return <>{children}</>;
  return <AppShell>{children}</AppShell>;
}
