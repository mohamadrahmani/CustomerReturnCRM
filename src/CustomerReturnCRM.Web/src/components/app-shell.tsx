"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useAuth } from "./auth-provider";

const navigation = [
  { href: "/dashboard", label: "داشبورد" },
  { href: "/appointments", label: "نوبت‌ها" },
  { href: "/customers", label: "مشتریان" },
  { href: "/services", label: "خدمات" },
  { href: "/staff", label: "کارکنان" },
  { href: "/follow-ups", label: "پیگیری‌ها" },
  { href: "/return-analysis", label: "تحلیل بازگشت" },
];

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const { auth, businessId, setBusinessId, logout } = useAuth();
  const activeBusiness = auth?.businesses.find((business) => business.id === businessId);

  function handleLogout() {
    logout();
    router.replace("/login");
  }

  return (
    <div className="min-h-screen bg-slate-50 md:flex">
      <aside className="hidden w-64 shrink-0 bg-slate-950 p-5 text-white md:flex md:flex-col">
        <div className="mb-8">
          <p className="text-sm font-semibold text-indigo-300">Customer Return CRM</p>
          <p className="mt-1 text-xs text-slate-400">مدیریت مشتری و بازگشت</p>
        </div>
        <nav className="space-y-1">
          {navigation.map((item) => (
            <Link key={item.href} href={item.href} className={`block rounded-xl px-4 py-3 text-sm transition ${pathname === item.href ? "bg-white/10 font-semibold text-white" : "text-slate-300 hover:bg-white/5 hover:text-white"}`}>
              {item.label}
            </Link>
          ))}
        </nav>
        <div className="mt-auto border-t border-white/10 pt-4">
          <button onClick={handleLogout} className="w-full rounded-xl px-4 py-3 text-right text-sm text-slate-300 hover:bg-white/5 hover:text-white">خروج</button>
        </div>
      </aside>

      <div className="min-w-0 flex-1">
        <header className="sticky top-0 z-10 border-b border-slate-200 bg-white/95 px-4 py-3 backdrop-blur md:px-8">
          <div className="mx-auto flex max-w-7xl items-center justify-between gap-4">
            <div className="min-w-0">
              <p className="truncate text-xs text-slate-500">کسب‌وکار فعال</p>
              {auth?.businesses.length ? (
                <select value={businessId ?? ""} onChange={(event) => setBusinessId(event.target.value)} className="mt-1 max-w-[220px] bg-transparent text-sm font-semibold outline-none">
                  {auth.businesses.map((business) => <option key={business.id} value={business.id}>{business.name}</option>)}
                </select>
              ) : <p className="mt-1 text-sm font-semibold">بدون کسب‌وکار</p>}
            </div>
            <button onClick={handleLogout} className="rounded-xl border border-slate-200 px-3 py-2 text-sm text-slate-600 md:hidden">خروج</button>
            <span className="hidden text-sm text-slate-500 md:block">{activeBusiness?.role ?? ""}</span>
          </div>
        </header>

        <div className="mx-auto max-w-7xl px-4 py-5 pb-24 md:px-8 md:py-8 md:pb-8">{children}</div>

        <nav className="fixed inset-x-0 bottom-0 z-20 grid grid-cols-4 border-t border-slate-200 bg-white md:hidden">
          {navigation.slice(0, 3).map((item) => (
            <Link key={item.href} href={item.href} className={`px-2 py-3 text-center text-xs ${pathname === item.href ? "font-semibold text-indigo-600" : "text-slate-500"}`}>{item.label}</Link>
          ))}
          <Link href="/return-analysis" className={`px-2 py-3 text-center text-xs ${pathname === "/return-analysis" ? "font-semibold text-indigo-600" : "text-slate-500"}`}>بیشتر</Link>
        </nav>
      </div>
    </div>
  );
}
