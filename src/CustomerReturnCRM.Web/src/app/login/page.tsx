"use client";

import Link from "next/link";
import { FormEvent, useState } from "react";
import { login } from "@/lib/api";
import { useAuth } from "@/components/auth-provider";

export default function LoginPage() {
  const { setAuth } = useAuth();
  const [mobile, setMobile] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setLoading(true);
    try {
      const result = await login(mobile.trim(), password);
      setAuth(result);
      window.location.href = "/dashboard";
    } catch (err) {
      setError(err instanceof Error ? err.message : "ورود انجام نشد.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center px-4 py-8">
      <form onSubmit={handleSubmit} className="w-full max-w-md rounded-3xl border border-slate-200 bg-white p-7 shadow-sm">
        <div className="mb-7">
          <p className="text-sm font-semibold text-indigo-600">Customer Return CRM</p>
          <h1 className="mt-2 text-2xl font-bold">ورود به حساب</h1>
          <p className="mt-2 text-sm leading-6 text-slate-500">برای ورود به پنل مدیریت، اطلاعات حساب خود را وارد کنید.</p>
        </div>

        <label className="block text-sm font-medium">شماره موبایل</label>
        <input value={mobile} onChange={(e) => setMobile(e.target.value)} type="tel" inputMode="tel" autoComplete="tel" required className="mt-2 w-full rounded-xl border border-slate-300 px-4 py-3 outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100" />

        <label className="mt-4 block text-sm font-medium">رمز عبور</label>
        <input value={password} onChange={(e) => setPassword(e.target.value)} type="password" autoComplete="current-password" required className="mt-2 w-full rounded-xl border border-slate-300 px-4 py-3 outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100" />

        {error && <p role="alert" className="mt-4 rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700">{error}</p>}

        <button type="submit" disabled={loading} className="mt-6 w-full rounded-xl bg-indigo-600 px-4 py-3 font-semibold text-white transition hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-50">
          {loading ? "در حال ورود..." : "ورود"}
        </button>

        <p className="mt-4 text-center text-sm"><Link href="/forgot-password" className="font-semibold text-indigo-600 hover:text-indigo-700">رمز عبور را فراموش کرده‌اید؟</Link></p>

        <p className="mt-5 text-center text-sm text-slate-500">
          حساب کاربری ندارید؟{" "}
          <Link href="/register" className="font-semibold text-indigo-600 hover:text-indigo-700">
            ثبت‌نام کنید
          </Link>
        </p>
      </form>
    </main>
  );
}
