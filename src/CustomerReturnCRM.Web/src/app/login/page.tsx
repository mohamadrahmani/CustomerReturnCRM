"use client";

import { FormEvent, useState } from "react";
import { login } from "@/lib/api";

export default function LoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setLoading(true);
    try {
      const result = await login(email, password);
      // Token persistence will be finalized with the authenticated app shell.
      sessionStorage.setItem("crm_auth", JSON.stringify(result));
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
        <h1 className="text-2xl font-bold">ورود به حساب</h1>
        <p className="mt-2 text-sm text-slate-500">برای ادامه اطلاعات حساب خود را وارد کنید.</p>

        <label className="mt-7 block text-sm font-medium">ایمیل</label>
        <input value={email} onChange={(e) => setEmail(e.target.value)} type="email" required className="mt-2 w-full rounded-xl border border-slate-300 px-4 py-3 outline-none focus:border-indigo-500" />

        <label className="mt-4 block text-sm font-medium">رمز عبور</label>
        <input value={password} onChange={(e) => setPassword(e.target.value)} type="password" required className="mt-2 w-full rounded-xl border border-slate-300 px-4 py-3 outline-none focus:border-indigo-500" />

        {error && <p className="mt-4 rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700">{error}</p>}

        <button disabled={loading} className="mt-6 w-full rounded-xl bg-indigo-600 px-4 py-3 font-semibold text-white disabled:opacity-50">
          {loading ? "در حال ورود..." : "ورود"}
        </button>
      </form>
    </main>
  );
}
