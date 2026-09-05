"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useState } from "react";
import { useAuth } from "./auth-provider";

const navigation = [
  { href: "/dashboard", label: "داشبورد", icon: "home" },
  { href: "/appointments", label: "نوبت‌ها", icon: "calendar" },
  { href: "/customers", label: "مشتریان", icon: "users" },
  { href: "/services", label: "خدمات", icon: "service" },
  { href: "/staff", label: "کارکنان", icon: "staff" },
  { href: "/follow-ups", label: "پیگیری‌ها", icon: "check" },
  { href: "/return-analysis", label: "تحلیل بازگشت", icon: "trend" },
];

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const { auth, businessId, setBusinessId, logout } = useAuth();
  const [mobileOpen, setMobileOpen] = useState(false);
  const activeBusiness = auth?.businesses.find((business) => business.id === businessId) ?? auth?.businesses[0];

  function handleLogout() { logout(); router.replace("/login"); }
  function closeMobile() { setMobileOpen(false); }

  return (
    <div className="min-h-screen bg-slate-50 md:flex">
      <aside className="hidden w-64 shrink-0 bg-slate-950 px-4 py-6 text-white md:flex md:flex-col">
        <Brand />
        <nav className="mt-8 space-y-1.5">{navigation.map((item) => <NavItem key={item.href} item={item} active={pathname === item.href} />)}</nav>
        <div className="mt-auto border-t border-white/10 pt-4"><button onClick={handleLogout} className="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-right text-sm text-slate-400 transition hover:bg-white/5 hover:text-white"><Icon name="logout" className="h-4 w-4" /><span>خروج از حساب</span></button></div>
      </aside>

      {mobileOpen && <button aria-label="بستن منو" onClick={closeMobile} className="fixed inset-0 z-30 bg-slate-950/40 md:hidden" />}
      <aside className={`fixed inset-y-0 right-0 z-40 w-72 bg-slate-950 px-4 py-5 text-white shadow-2xl transition-transform md:hidden ${mobileOpen ? "translate-x-0" : "translate-x-full"}`}>
        <div className="flex items-start justify-between"><Brand /><button onClick={closeMobile} className="rounded-lg px-2 py-1 text-slate-400 hover:bg-white/10" aria-label="بستن">×</button></div>
        <nav className="mt-8 space-y-1">{navigation.map((item) => <div key={item.href} onClick={closeMobile}><NavItem item={item} active={pathname === item.href} /></div>)}</nav>
        <button onClick={handleLogout} className="mt-8 flex w-full items-center gap-3 rounded-xl px-3 py-3 text-right text-sm text-slate-400 hover:bg-white/5 hover:text-white"><Icon name="logout" className="h-4 w-4" />خروج از حساب</button>
      </aside>

      <div className="min-w-0 flex-1">
        <header className="sticky top-0 z-20 border-b border-slate-200/90 bg-white/95 px-4 py-3 backdrop-blur md:px-8">
          <div className="mx-auto flex max-w-7xl items-center justify-between gap-4">
            <div className="flex min-w-0 items-center gap-3">
              <button onClick={() => setMobileOpen(true)} className="order-2 flex h-10 w-10 items-center justify-center rounded-xl border border-slate-200 text-slate-600 transition hover:bg-slate-50 md:hidden" aria-label="باز کردن منو"><Icon name="menu" className="h-5 w-5" /></button>
              <div className="min-w-0"><p className="text-[10px] font-medium text-slate-400">کسب‌وکار فعال</p>{auth?.businesses.length ? <select aria-label="انتخاب کسب‌وکار" value={businessId ?? auth.businesses[0].id} onChange={(event) => setBusinessId(event.target.value)} className="mt-0.5 max-w-[220px] bg-transparent text-sm font-extrabold text-slate-800 outline-none">{auth.businesses.map((business) => <option key={business.id} value={business.id}>{business.name}</option>)}</select> : <p className="text-sm font-semibold">بدون کسب‌وکار</p>}</div>
            </div>
            <div className="flex items-center gap-3"><span className="hidden rounded-full bg-slate-100 px-3 py-1.5 text-xs font-medium text-slate-600 sm:inline-flex">{activeBusiness?.role ?? ""}</span><button onClick={handleLogout} className="hidden rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-600 transition hover:bg-slate-50 md:inline-flex">خروج</button></div>
          </div>
        </header>

        <div className="mx-auto max-w-7xl px-4 py-5 pb-24 md:px-8 md:py-8 md:pb-8">{children}</div>

        <nav className="fixed inset-x-0 bottom-0 z-20 grid grid-cols-4 border-t border-slate-200 bg-white/95 pb-safe shadow-[0_-4px_20px_rgba(15,23,42,0.04)] backdrop-blur md:hidden">
          {navigation.slice(0, 3).map((item) => <BottomNavItem key={item.href} item={item} active={pathname === item.href} />)}
          <button onClick={() => setMobileOpen(true)} className="flex min-h-[58px] flex-col items-center justify-center gap-1 text-[11px] font-medium text-slate-500"><Icon name="menu" className="h-5 w-5" /><span>بیشتر</span></button>
        </nav>
      </div>
    </div>
  );
}

function Brand() { return <div><p className="text-sm font-bold tracking-tight text-white">Customer Return CRM</p><p className="mt-1 text-xs text-slate-500">مدیریت مشتری و بازگشت</p></div>; }
function NavItem({ item, active }: { item: typeof navigation[number]; active: boolean }) { return <Link href={item.href} className={`flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm transition ${active ? "bg-indigo-500/15 font-semibold text-white ring-1 ring-indigo-400/10" : "text-slate-400 hover:bg-white/5 hover:text-slate-100"}`}><span className={`flex h-8 w-8 items-center justify-center rounded-lg ${active ? "bg-indigo-500/20 text-indigo-300" : "text-slate-500"}`}><Icon name={item.icon} className="h-4 w-4" /></span>{item.label}</Link>; }
function BottomNavItem({ item, active }: { item: typeof navigation[number]; active: boolean }) { return <Link href={item.href} className={`flex min-h-[58px] flex-col items-center justify-center gap-1 text-[11px] ${active ? "font-bold text-indigo-600" : "font-medium text-slate-500"}`}><span className={`flex h-7 w-9 items-center justify-center rounded-full ${active ? "bg-indigo-50" : "bg-transparent"}`}><Icon name={item.icon} className="h-4 w-4" /></span><span>{item.label}</span></Link>; }

function Icon({ name, className = "h-5 w-5" }: { name: string; className?: string }) {
  const common = { className, fill: "none", stroke: "currentColor", strokeWidth: 1.8, strokeLinecap: "round" as const, strokeLinejoin: "round" as const, viewBox: "0 0 24 24", "aria-hidden": true };
  if (name === "home") return <svg {...common}><path d="m3 10 9-7 9 7"/><path d="M5 9.5V21h14V9.5"/><path d="M9 21v-6h6v6"/></svg>;
  if (name === "calendar") return <svg {...common}><rect x="3" y="4.5" width="18" height="16" rx="2"/><path d="M7 2.5v4M17 2.5v4M3 9h18"/><path d="M8 13h3M8 17h3M14 13h2"/></svg>;
  if (name === "users") return <svg {...common}><circle cx="9" cy="8" r="3"/><path d="M3.5 20c.4-3.2 2.3-5 5.5-5s5.1 1.8 5.5 5"/><path d="M15 5.5a3 3 0 0 1 0 5.8M16 15c2.5.2 4 1.8 4.5 4"/></svg>;
  if (name === "service") return <svg {...common}><path d="m14.7 6.3 3-3 3 3-3 3"/><path d="m13.5 7.5-8 8a2.8 2.8 0 1 0 4 4l8-8"/><path d="m6.5 17.5 2 2"/></svg>;
  if (name === "staff") return <svg {...common}><circle cx="12" cy="7" r="3"/><path d="M5 21c.6-4.1 2.9-6.2 7-6.2s6.4 2.1 7 6.2"/><path d="M4 10h3M17 10h3"/></svg>;
  if (name === "check") return <svg {...common}><circle cx="12" cy="12" r="9"/><path d="m8 12 2.5 2.5L16 9"/></svg>;
  if (name === "trend") return <svg {...common}><path d="M4 17 10 11l4 4 6-7"/><path d="M15 8h5v5"/></svg>;
  if (name === "logout") return <svg {...common}><path d="M10 17l5-5-5-5M15 12H3"/><path d="M14 4h5v16h-5"/></svg>;
  return <svg {...common}><path d="M4 7h16M4 12h16M4 17h16"/></svg>;
}
