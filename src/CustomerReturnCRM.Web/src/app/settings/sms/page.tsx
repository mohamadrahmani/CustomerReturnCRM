"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useAuth } from "@/components/auth-provider";
import { apiFetch } from "@/lib/api";

type SmsDebugConfig = {
  provider: string;
  apiKey: string;
  lineNumber: string;
};

export default function SmsSettingsPage() {
  const { businessId, isReady } = useAuth();
  const [config, setConfig] = useState<SmsDebugConfig | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!isReady || !businessId) return;
    apiFetch<SmsDebugConfig>(`/api/businesses/${businessId}/sms/debug-config`)
      .then(setConfig)
      .catch((exception) => setError(exception instanceof Error ? exception.message : "خطا در دریافت تنظیمات پیامک"));
  }, [isReady, businessId]);

  return <main dir="rtl">
    <div className="mb-6">
      <Link href="/settings" className="text-sm text-slate-500">← تنظیمات</Link>
      <h1 className="mt-3 text-2xl font-extrabold text-slate-900">تنظیمات پیامک</h1>
      <p className="mt-1 text-sm text-slate-500">قالب‌ها و سابقه ارسال پیامک کسب‌وکار را مدیریت کنید.</p>
    </div>

    <div className="mb-4 rounded-2xl border border-amber-200 bg-amber-50 p-5">
      <h2 className="font-bold text-amber-900">بررسی تنظیمات SMS.ir</h2>
      <p className="mt-2 text-sm leading-6 text-amber-800">مقادیر زیر فقط برای عیب‌یابی نمایش داده می‌شوند و بخش میانی Token و شماره خط مخفی است.</p>
      {config ? <div className="mt-4 grid gap-3 sm:grid-cols-3">
        <div className="rounded-xl bg-white p-3"><div className="text-xs text-slate-500">Provider</div><div className="mt-1 font-mono text-sm text-slate-900">{config.provider || "(empty)"}</div></div>
        <div className="rounded-xl bg-white p-3"><div className="text-xs text-slate-500">API Token</div><div className="mt-1 break-all font-mono text-sm text-slate-900">{config.apiKey}</div></div>
        <div className="rounded-xl bg-white p-3"><div className="text-xs text-slate-500">Line Number</div><div className="mt-1 break-all font-mono text-sm text-slate-900">{config.lineNumber}</div></div>
      </div> : error ? <div className="mt-3 text-sm text-red-700">{error}</div> : <div className="mt-3 text-sm text-slate-500">در حال دریافت...</div>}
    </div>

    <div className="grid gap-4 sm:grid-cols-2">
      <Link href="/sms-templates" className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm hover:border-slate-300"><h2 className="font-bold">قالب‌های پیامک</h2><p className="mt-2 text-sm leading-6 text-slate-500">ساخت، ویرایش و فعال/غیرفعال کردن قالب‌های آماده.</p><span className="mt-4 inline-flex text-sm font-semibold text-indigo-600">مدیریت قالب‌ها ←</span></Link>
      <Link href="/sms-campaigns" className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm hover:border-slate-300"><h2 className="font-bold">تاریخچه ارسال</h2><p className="mt-2 text-sm leading-6 text-slate-500">مشاهده کمپین‌ها، وضعیت ارسال و جزئیات گیرندگان.</p><span className="mt-4 inline-flex text-sm font-semibold text-indigo-600">مشاهده تاریخچه ←</span></Link>
    </div>
  </main>;
}
