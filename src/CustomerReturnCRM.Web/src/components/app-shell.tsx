"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useState } from "react";
import { useAuth } from "./auth-provider";

const navigation = [
  { href: "/dashboard", label: "داشبورد", icon: "⌂" },
  { href: "/appointments", label: "نوبت‌ها", icon: "◷" },
  { href: "/customers", label: "مشتریان", icon: "♙" },
  { href: "/services", label: "خدمات", icon: "◇" },
  { href: "/staff", label: "کارکنان", icon: "♧" },
  { href: "/follow-ups", label: "پیگیری‌ها", icon: "✓" },
  { href: "/return-analysis", label: "تحلیل بازگشت", icon: "↗" },
];

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const { auth, businessId, setBusinessId, logout } = useAuth();
  const [mobileOpen, setMobileOpen] = useState(false);
  const activeBusiness = auth?.businesses.find((business) => business.id === businessId);

  function handleLogout() {
    logout();
    router.replace("/login");
  }

  function closeMobile() { setMobileOpen(false); }

  return (
    <div className="min-h-screen bg-slate-50 md:flex">
      <aside className="hidden w-64 shrink-0 bg-slate-950 px-4 py-5 text-white md:flex md:flex-col">
        <Brand />
        <nav className="mt-8 space-y-1">
          {navigation.map((item) => <NavItem key={item.href} item={item} active={pathname === item.href} />)}
        </nav>
        <div className="mt-auto border-t border-white/10 pt-4">
          <button onClick={handleLogout} className="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-right text-sm text-slate-400 transition hover:bg-white/5 hover:text-white">
            <span className="text-base">↪</span><span>خروج از حساب</span>
          </button>
        </div>
      </aside>

      {mobileOpen && <button aria-label="بستن منو" onClick={closeMobile} className="fixed inset-0 z-30 bg-slate-950/40 md:hidden" />}
      <aside className={`fixed inset-y-0 right-0 z-40 w-72 bg-slate-950 px-4 py-5 text-white shadow-2xl transition-transform md:hidden ${mobileOpen ? "translate-x-0" : "translate-x-full"}`}>
        <div className="flex items-start justify-between"><Brand /><button onClick={closeMobile} className="rounded-lg px-2 py-1 text-slate-400 hover:bg-white/10" aria-label="بستن">×</button></div>
        <nav className="mt-8 space-y-1">{navigation.map((item) => <div key={item.href} onClick={closeMobile}><NavItem item={item} active={pathname === item.href} /></div>)}</nav>
        <button onClick={handleLogout} className="mt-8 flex w-full items-center gap-3 rounded-xl px-3 py-3 text-right text-sm text-slate-400 hover:bg-white/5 hover:text-white">↪ خروج از حساب</button>
      </aside>

      <div className="min-w-0 flex-1">
        <header className="sticky top-0 z-20 border-b border-slate-200/90 bg-white/95 px-4 py-3 backdrop-blur md:px-8">
          <div className="mx-auto flex max-w-7xl items-center justify-between gap-4">
            <div className="flex min-w-0 items-center gap-3">
              <button onClick={() => setMobileOpen(true)} className="rounded-xl border border-slate-200 p-2 text-slate-600 md:hidden" aria-label="باز کردن منو">☰</button>
              <div className="min-w-0">
                <p className="text-[11px] font-medium text-slate-400">کسب‌وکار فعال</p>
                {auth?.businesses.length ? <select value={businessId ?? auth.businesses[0].id} onChange={(event) => setBusinessId(event.target.value)} className="mt-0.5 max-w-[220px] bg-transparent text-sm font-bold text-slate-800 outline-none">{auth.businesses.map((business) => <option key={business.id} value={business.id}>{business.name}</option>)}</select> : <p className="text-sm font-semibold">بدون کسب‌وکار</p>}
              </div>
            </div>
            <div className="flex items-center gap-3"><span className="hidden rounded-full bg-slate-100 px-3 py-1.5 text-xs font-medium text-slate-600 sm:inline-flex">{activeBusiness?.role ?? ""}</span><button onClick={handleLogout} className="hidden rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-600 transition hover:bg-slate-50 md:inline-flex">خروج</button></div>
          </div>
        </header>

        <div className="mx-auto max-w-7xl px-4 py-5 pb-24 md:px-8 md:py-8 md:pb-8">{children}</div>

        <nav className="fixed inset-x-0 bottom-0 z-20 grid grid-cols-4 border-t border-slate-200 bg-white/95 pb-safe backdrop-blur md:hidden">
          {navigation.slice(0, 3).map((item) => <BottomNavItem key={item.href} item={item} active={pathname === item.href} />)}
          <button onClick={() => setMobileOpen(true)} className="flex flex-col items-center gap-1 px-2 py-2 text-[11px] text-slate-500"><span className="text-base">☰</span><span>بیشتر</span></button>
        </nav>
      </div>
    </div>
  );
}

function Brand() { return <div><p className="text-sm font-bold tracking-tight text-white">Customer Return CRM</p><p className="mt-1 text-xs text-slate-500">مدیریت مشتری و بازگشت</p></div>; }

function NavItem({ item, active }: { item: typeof navigation[number]; active: boolean }) { return <Link href={item.href} className={`flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm transition ${active ? "bg-indigo-500/15 font-semibold text-white ring-1 ring-indigo-400/10" : "text-slate-400 hover:bg-white/5 hover:text-slate-100"}`}><span className={`flex h-7 w-7 items-center justify-center rounded-lg text-sm ${active ? "bg-indigo-500/20 text-indigo-300" : "text-slate-500"}`}>{item.icon}</span>{item.label}</Link>; }

function BottomNavItem({ item, active }: { item: typeof navigation[number]; active: boolean }) { return <Link href={item.href} className={`flex flex-col items-center gap-1 px-2 py-2 text-[11px] ${active ? "font-bold text-indigo-600" : "text-slate-500"}`}><span className="text-base">{item.icon}</span><span>{item.label}</span></Link>; }
