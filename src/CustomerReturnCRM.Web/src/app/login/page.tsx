"use client";

import Link from "next/link";
import { FormEvent, useState } from "react";
import { login, requestPasswordReset, resetPassword } from "@/lib/api";
import { useAuth } from "@/components/auth-provider";

export default function LoginPage() {
  const { setAuth } = useAuth();
  const [forgot, setForgot] = useState(false);
  const [mobile, setMobile] = useState("");
  const [password, setPassword] = useState("");
  const [otp, setOtp] = useState("");
  const [error, setError] = useState("");
  const [step, setStep] = useState<"mobile" | "reset">("mobile");
  const [loading, setLoading] = useState(false);

  async function handleLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setError(""); setLoading(true);
    try { const result = await login(mobile.trim(), password); setAuth(result); window.location.href = "/dashboard"; }
    catch (err) { setError(err instanceof Error ? err.message : "ورود انجام نشد."); }
    finally { setLoading(false); }
  }

  async function handleReset(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setError(""); setLoading(true);
    try {
      if (step === "mobile") { await requestPasswordReset(mobile.trim()); setStep("reset"); }
      else { await resetPassword(mobile.trim(), otp.trim(), password); setForgot(false); setStep("mobile"); setPassword(""); setOtp(""); setError(""); }
    } catch (err) { setError(err instanceof Error ? err.message : "عملیات انجام نشد."); }
    finally { setLoading(false); }
  }

  return <main className="flex min-h-screen items-center justify-center px-4 py-8">
    <form onSubmit={forgot ? handleReset : handleLogin} className="w-full max-w-md rounded-3xl border border-slate-200 bg-white p-7 shadow-sm">
      <div className="mb-7"><p className="text-sm font-semibold text-indigo-600">Customer Return CRM</p><h1 className="mt-2 text-2xl font-bold">{forgot ? "بازیابی رمز عبور" : "ورود به حساب"}</h1><p className="mt-2 text-sm leading-6 text-slate-500">{forgot ? "کد تأیید از طریق SMS برای شما ارسال می‌شود." : "برای ورود به پنل مدیریت، اطلاعات حساب خود را وارد کنید."}</p></div>
      <label className="block text-sm font-medium">شماره موبایل</label>
      <input value={mobile} onChange={e => setMobile(e.target.value)} type="tel" inputMode="tel" autoComplete="tel" required disabled={forgot && step === "reset"} className="mt-2 w-full rounded-xl border border-slate-300 px-4 py-3" />
      {!forgot && <><label className="mt-4 block text-sm font-medium">رمز عبور</label><input value={password} onChange={e => setPassword(e.target.value)} type="password" autoComplete="current-password" required minLength={8} className="mt-2 w-full rounded-xl border border-slate-300 px-4 py-3" /></>}
      {forgot && step === "reset" && <><label className="mt-4 block text-sm font-medium">کد تأیید</label><input value={otp} onChange={e => setOtp(e.target.value)} inputMode="numeric" autoComplete="one-time-code" maxLength={6} required className="mt-2 w-full rounded-xl border border-slate-300 px-4 py-3" /><label className="mt-4 block text-sm font-medium">رمز عبور جدید</label><input value={password} onChange={e => setPassword(e.target.value)} type="password" autoComplete="new-password" required minLength={8} className="mt-2 w-full rounded-xl border border-slate-300 px-4 py-3" /><p className="mt-2 text-xs text-slate-500">حداقل ۸ کاراکتر؛ فقط حروف انگلیسی و اعداد.</p></>}
      {error && <p role="alert" className="mt-4 rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700">{error}</p>}
      <button type="submit" disabled={loading} className="mt-6 w-full rounded-xl bg-indigo-600 px-4 py-3 font-semibold text-white disabled:opacity-50">{loading ? "در حال انجام..." : forgot ? (step === "mobile" ? "دریافت کد" : "تغییر رمز عبور") : "ورود"}</button>
      {!forgot ? <><button type="button" onClick={() => { setForgot(true); setError(""); }} className="mt-4 w-full text-sm font-semibold text-indigo-600">رمز عبور را فراموش کرده‌اید؟</button><p className="mt-5 text-center text-sm text-slate-500">حساب کاربری ندارید؟ <Link href="/register" className="font-semibold text-indigo-600">ثبت‌نام کنید</Link></p></> : <button type="button" onClick={() => { setForgot(false); setStep("mobile"); setError(""); }} className="mt-5 w-full text-sm font-semibold text-slate-500">بازگشت به ورود</button>}
    </form>
  </main>;
}
