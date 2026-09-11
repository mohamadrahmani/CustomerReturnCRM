"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useAuth } from "@/components/auth-provider";
import { apiFetch } from "@/lib/api";

type WorkingHour = { id: string; dayOfWeek: number; startTime: string; endTime: string };
type DayConfig = { dayOfWeek: number; label: string; enabled: boolean; startTime: string; endTime: string };

const days: { dayOfWeek: number; label: string }[] = [
  { dayOfWeek: 6, label: "شنبه" },
  { dayOfWeek: 0, label: "یکشنبه" },
  { dayOfWeek: 1, label: "دوشنبه" },
  { dayOfWeek: 2, label: "سه‌شنبه" },
  { dayOfWeek: 3, label: "چهارشنبه" },
  { dayOfWeek: 4, label: "پنجشنبه" },
  { dayOfWeek: 5, label: "جمعه" },
];

const defaults = (hours: WorkingHour[]): DayConfig[] => days.map(day => {
  const item = hours.find(x => x.dayOfWeek === day.dayOfWeek);
  return { dayOfWeek: day.dayOfWeek, label: day.label, enabled: Boolean(item), startTime: item?.startTime?.slice(0, 5) ?? "09:00", endTime: item?.endTime?.slice(0, 5) ?? "18:00" };
});

export default function WorkingHoursPage() {
  const { auth, businessId, isReady } = useAuth();
  const activeBusinessId = businessId ?? auth?.businesses[0]?.id ?? null;
  const [items, setItems] = useState<DayConfig[]>(defaults([]));
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  useEffect(() => {
    if (!isReady || !activeBusinessId) return;
    setLoading(true);
    apiFetch<WorkingHour[]>(`/api/businesses/${activeBusinessId}/availability/working-hours`)
      .then(result => setItems(defaults(result)))
      .catch(e => setError(e instanceof Error ? e.message : "دریافت ساعات کاری انجام نشد."))
      .finally(() => setLoading(false));
  }, [isReady, activeBusinessId]);

  function update(dayOfWeek: number, patch: Partial<DayConfig>) {
    setItems(current => current.map(item => item.dayOfWeek === dayOfWeek ? { ...item, ...patch } : item));
    setSuccess("");
  }

  async function save() {
    if (!activeBusinessId) return;
    setSaving(true); setError(""); setSuccess("");
    try {
      const invalid = items.find(x => x.enabled && x.endTime <= x.startTime);
      if (invalid) throw new Error(`ساعت پایان ${invalid.label} باید بعد از ساعت شروع باشد.`);
      await apiFetch<WorkingHour[]>(`/api/businesses/${activeBusinessId}/availability/working-hours`, {
        method: "PUT",
        body: JSON.stringify(items.filter(x => x.enabled).map(x => ({ dayOfWeek: x.dayOfWeek, startTime: `${x.startTime}:00`, endTime: `${x.endTime}:00` }))),
      });
      setSuccess("ساعات کاری با موفقیت ذخیره شد.");
    } catch (e) {
      setError(e instanceof Error ? e.message : "ذخیره ساعات کاری انجام نشد.");
    } finally { setSaving(false); }
  }

  if (!isReady || !activeBusinessId) return <main className="p-4"><div className="h-40 animate-pulse rounded-3xl bg-white" /></main>;

  return <main dir="rtl">
    <div className="mb-6">
      <Link href="/settings" className="text-sm text-slate-500">← تنظیمات</Link>
      <h1 className="mt-3 text-2xl font-extrabold text-slate-900">ساعات کاری</h1>
      <p className="mt-1 text-sm leading-6 text-slate-500">روزها و بازه زمانی فعالیت کسب‌وکار را مشخص کنید. رزرو آنلاین بر اساس این برنامه زمان‌های آزاد را محاسبه می‌کند.</p>
    </div>

    {loading ? <div className="space-y-3">{days.map(day => <div key={day.dayOfWeek} className="h-20 animate-pulse rounded-2xl bg-white" />)}</div> : <section className="crm-card overflow-hidden">
      <div className="divide-y divide-slate-100">
        {items.map(item => <div key={item.dayOfWeek} className="flex flex-col gap-4 p-4 sm:flex-row sm:items-center sm:justify-between sm:p-5">
          <label className="flex items-center gap-3 sm:w-40">
            <input type="checkbox" checked={item.enabled} onChange={e => update(item.dayOfWeek, { enabled: e.target.checked })} className="h-5 w-5 rounded border-slate-300" />
            <span className="font-bold text-slate-800">{item.label}</span>
          </label>
          <div className={`grid flex-1 gap-3 sm:grid-cols-2 ${item.enabled ? "" : "opacity-40"}`}>
            <label className="text-xs text-slate-500">شروع<input type="time" disabled={!item.enabled} value={item.startTime} onChange={e => update(item.dayOfWeek, { startTime: e.target.value })} className="mt-1 w-full rounded-xl border border-slate-200 bg-white p-3 text-sm text-slate-800 outline-none focus:border-slate-900" /></label>
            <label className="text-xs text-slate-500">پایان<input type="time" disabled={!item.enabled} value={item.endTime} onChange={e => update(item.dayOfWeek, { endTime: e.target.value })} className="mt-1 w-full rounded-xl border border-slate-200 bg-white p-3 text-sm text-slate-800 outline-none focus:border-slate-900" /></label>
          </div>
        </div>)}
      </div>
      <div className="border-t border-slate-100 bg-slate-50 p-4 text-xs leading-6 text-slate-500">اگر برای یک کارمند ساعت کاری اختصاصی ثبت نشود، برنامه همین کسب‌وکار برای او اعمال می‌شود.</div>
    </section>}

    {error && <div role="alert" className="mt-4 rounded-2xl bg-rose-50 p-4 text-sm font-medium text-rose-700">{error}</div>}
    {success && <div role="status" className="mt-4 rounded-2xl bg-emerald-50 p-4 text-sm font-medium text-emerald-700">{success}</div>}
    <div className="mt-5 flex justify-end"><button type="button" onClick={() => void save()} disabled={loading || saving} className="rounded-xl bg-slate-900 px-6 py-3 text-sm font-bold text-white disabled:cursor-not-allowed disabled:opacity-40">{saving ? "در حال ذخیره..." : "ذخیره ساعات کاری"}</button></div>
  </main>;
}
