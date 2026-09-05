"use client";

import { useEffect, useState } from "react";
import { AuthenticationResult } from "@/lib/api";

export default function DashboardPage() {
  const [auth, setAuth] = useState<AuthenticationResult | null>(null);

  useEffect(() => {
    const raw = sessionStorage.getItem("crm_auth");
    if (raw) setAuth(JSON.parse(raw) as AuthenticationResult);
  }, []);

  const business = auth?.businesses?.[0];

  return (
    <main className="min-h-screen p-4 md:p-8">
      <div className="mx-auto max-w-7xl">
        <header className="mb-8 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <p className="text-sm text-slate-500">داشبورد</p>
          <h1 className="mt-1 text-2xl font-bold">خوش آمدید{auth?.email ? `، ${auth.email}` : ""}</h1>
          {business && <p className="mt-2 text-sm text-indigo-600">کسب‌وکار فعال: {business.name}</p>}
        </header>

        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {["مشتریان فعال", "نوبت‌های امروز", "پیگیری‌های باز", "مشتریان نیازمند اقدام"].map((label) => (
            <div key={label} className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
              <p className="text-sm text-slate-500">{label}</p>
              <p className="mt-3 text-3xl font-bold">—</p>
            </div>
          ))}
        </div>

        <section className="mt-6 rounded-2xl border border-dashed border-slate-300 bg-white p-8 text-center text-slate-500">
          ماژول‌های عملیاتی در مرحله بعد به API واقعی متصل می‌شوند.
        </section>
      </div>
    </main>
  );
}
