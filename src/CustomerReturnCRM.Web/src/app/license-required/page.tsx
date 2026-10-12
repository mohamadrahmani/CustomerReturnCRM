"use client";

import Link from "next/link";
import { useAuth } from "@/components/auth-provider";

export default function LicenseRequiredPage() {
  const { auth, logout } = useAuth();
  const business = auth?.businesses[0];
  return <main dir="rtl" className="flex min-h-screen items-center justify-center bg-slate-50 px-4 py-10">
    <section className="w-full max-w-lg rounded-3xl border border-slate-200 bg-white p-8 text-center shadow-sm">
      <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-amber-50 text-amber-700" aria-hidden="true"><svg viewBox="0 0 24 24" className="h-7 w-7" fill="none" stroke="currentColor" strokeWidth="1.8"><circle cx="12" cy="12" r="9"/><path d="M12 7v5m0 4h.01"/></svg></div>
      <h1 className="mt-5 text-2xl font-black text-slate-900">اعتبار لایسنس فعال نیست</h1>
      <p className="mt-3 leading-7 text-slate-600">دسترسی به امکانات پنل این کسب‌وکار موقتاً مسدود شده است. اطلاعات شما حذف نشده‌اند. برای تمدید یا فعال‌سازی لایسنس با پشتیبانی SalonCRM تماس بگیرید.</p>
      {business && <p className="mt-4 rounded-xl bg-slate-50 p-3 text-sm text-slate-600">کسب‌وکار: <strong>{business.name}</strong></p>}
      <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-center"><button onClick={() => { logout(); window.location.href = "/login"; }} className="rounded-xl border border-slate-300 px-5 py-3 text-sm font-bold">خروج از حساب</button><Link href="/login" className="rounded-xl bg-slate-900 px-5 py-3 text-sm font-bold text-white">بازگشت به ورود</Link></div>
    </section>
  </main>;
}
