"use client";

import { FormEvent, useMemo, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { getCentralAdminBusinesses, updateBusinessLicense, type CentralAdminBusiness } from "@/lib/api";
import { useAuth } from "@/components/auth-provider";

const dateValue = (value: string | null) => value ? value.slice(0, 10) : "";
const dateLabel = (value: string | null) => value
  ? new Intl.DateTimeFormat("fa-IR", { year: "numeric", month: "short", day: "numeric" }).format(new Date(value))
  : "ثبت نشده";

function statusLabel(item: CentralAdminBusiness) {
  if (!item.licenseIsActive) return "غیرفعال";
  const today = new Date(); today.setHours(0, 0, 0, 0);
  if (item.licenseStartDate && new Date(item.licenseStartDate).getTime() > today.getTime()) return "شروع نشده";
  if (item.licenseEndDate) {
    const end = new Date(item.licenseEndDate); end.setHours(23, 59, 59, 999);
    if (end.getTime() < Date.now()) return "منقضی";
  }
  return "فعال";
}

export default function CentralAdminBusinessesPage() {
  const { auth, logout } = useAuth();
  const qc = useQueryClient();
  const query = useQuery({ queryKey: ["central-admin-businesses"], queryFn: getCentralAdminBusinesses });
  const [search, setSearch] = useState("");
  const [editing, setEditing] = useState<CentralAdminBusiness | null>(null);
  const [active, setActive] = useState(true);
  const [start, setStart] = useState("");
  const [end, setEnd] = useState("");
  const [note, setNote] = useState("");
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);

  const rows = useMemo(() => (query.data ?? []).filter(x =>
    `${x.name} ${x.mobile} ${x.ownerMobile ?? ""} ${x.city ?? ""}`.toLowerCase().includes(search.trim().toLowerCase())
  ), [query.data, search]);

  function edit(item: CentralAdminBusiness) {
    setEditing(item); setActive(item.licenseIsActive);
    setStart(dateValue(item.licenseStartDate)); setEnd(dateValue(item.licenseEndDate));
    setNote(item.licenseAdminNote ?? ""); setError("");
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!editing) return;
    if (start && end && start > end) { setError("تاریخ پایان نمی‌تواند قبل از تاریخ شروع باشد."); return; }
    setSaving(true); setError("");
    try {
      await updateBusinessLicense(editing.id, {
        licenseIsActive: active, licenseStartDate: start || null,
        licenseEndDate: end || null, licenseAdminNote: note.trim() || null
      });
      await qc.invalidateQueries({ queryKey: ["central-admin-businesses"] });
      setEditing(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : "ذخیره تغییرات انجام نشد.");
    } finally { setSaving(false); }
  }

  const businesses = query.data ?? [];
  return <main dir="rtl" className="min-h-screen bg-slate-50 px-4 py-6 sm:px-8">
    <div className="mx-auto max-w-7xl">
      <header className="mb-7 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div><p className="text-sm font-bold text-slate-500">SalonCRM / مدیریت مرکزی</p><h1 className="mt-1 text-3xl font-black text-slate-950">مدیریت کسب‌وکارها</h1><p className="mt-2 text-sm text-slate-600">فهرست خریداران محصول و کنترل اعتبار لایسنس.</p></div>
        <div className="flex items-center gap-3"><span className="text-sm text-slate-500">{auth?.mobile}</span><button onClick={() => { logout(); window.location.href = "/login"; }} className="rounded-xl border border-slate-300 bg-white px-4 py-2 text-sm font-bold">خروج</button></div>
      </header>
      <section className="mb-5 grid gap-3 sm:grid-cols-3">
        <div className="rounded-2xl border border-slate-200 bg-white p-5"><p className="text-sm text-slate-500">کل کسب‌وکارها</p><p className="mt-2 text-3xl font-black">{businesses.length.toLocaleString("fa-IR")}</p></div>
        <div className="rounded-2xl border border-slate-200 bg-white p-5"><p className="text-sm text-slate-500">لایسنس معتبر</p><p className="mt-2 text-3xl font-black text-emerald-700">{businesses.filter(x => x.licenseValid).length.toLocaleString("fa-IR")}</p></div>
        <div className="rounded-2xl border border-slate-200 bg-white p-5"><p className="text-sm text-slate-500">نیازمند بررسی</p><p className="mt-2 text-3xl font-black text-amber-700">{businesses.filter(x => !x.licenseValid).length.toLocaleString("fa-IR")}</p></div>
      </section>
      <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <div className="flex flex-col gap-3 border-b border-slate-100 p-4 sm:flex-row sm:items-center sm:justify-between"><h2 className="font-black">فهرست خریداران</h2><input value={search} onChange={e => setSearch(e.target.value)} placeholder="جست‌وجو بر اساس نام، موبایل یا شهر" className="w-full rounded-xl border border-slate-300 px-4 py-2.5 text-sm sm:max-w-sm" /></div>
        {query.isLoading ? <p className="p-8 text-center text-slate-500">در حال دریافت اطلاعات...</p> :
        query.isError ? <div className="p-8 text-center"><p className="text-rose-700">دریافت اطلاعات انجام نشد. مجوز ادمین مرکزی را بررسی کنید.</p><button onClick={() => query.refetch()} className="mt-3 rounded-xl border px-4 py-2">تلاش مجدد</button></div> :
        rows.length === 0 ? <p className="p-8 text-center text-slate-500">موردی پیدا نشد.</p> :
        <div className="overflow-x-auto"><table className="w-full min-w-[900px] text-right text-sm">
          <thead className="bg-slate-50 text-xs text-slate-500"><tr><th className="px-4 py-3">کسب‌وکار</th><th className="px-4 py-3">موبایل</th><th className="px-4 py-3">شروع اعتبار</th><th className="px-4 py-3">پایان اعتبار</th><th className="px-4 py-3">وضعیت لایسنس</th><th className="px-4 py-3">عملیات</th></tr></thead>
          <tbody className="divide-y divide-slate-100">{rows.map(item => <tr key={item.id} className="hover:bg-slate-50">
            <td className="px-4 py-4"><p className="font-bold">{item.name}</p><p className="mt-1 text-xs text-slate-500">{item.city || "شهر ثبت نشده"} · {item.businessType}</p><p className="mt-1 text-xs text-slate-500">مدیر: {item.ownerMobile || "ثبت نشده"}</p></td>
            <td className="px-4 py-4">{item.mobile}</td><td className="px-4 py-4">{dateLabel(item.licenseStartDate)}</td><td className="px-4 py-4">{dateLabel(item.licenseEndDate)}</td>
            <td className="px-4 py-4"><span className={`rounded-full px-3 py-1 text-xs font-bold ${item.licenseValid ? "bg-emerald-50 text-emerald-700" : "bg-amber-50 text-amber-800"}`}>{statusLabel(item)}</span></td>
            <td className="px-4 py-4"><button onClick={() => edit(item)} className="rounded-lg border border-slate-300 px-3 py-2 text-xs font-bold">مدیریت لایسنس</button></td>
          </tr>)}</tbody>
        </table></div>}
      </section>
      <p className="mt-4 text-xs leading-6 text-slate-500">غیرفعال‌سازی لایسنس اطلاعات کسب‌وکار را حذف نمی‌کند. مدیریت پیامک انبوه به فاز بعد موکول شده است.</p>
    </div>
    {editing && <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/40 p-4" role="dialog" aria-modal="true" aria-labelledby="license-title">
      <form onSubmit={save} className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-2xl bg-white p-6 shadow-xl">
        <div className="flex items-start justify-between"><div><h2 id="license-title" className="text-xl font-black">مدیریت لایسنس</h2><p className="mt-1 text-sm text-slate-500">{editing.name}</p></div><button type="button" onClick={() => setEditing(null)} aria-label="بستن">✕</button></div>
        <label className="mt-5 flex items-center justify-between rounded-xl border border-slate-200 p-4"><span><strong className="block">لایسنس فعال باشد</strong><span className="mt-1 block text-xs text-slate-500">در صورت غیرفعال بودن، APIهای پنل مشتری مسدود می‌شوند.</span></span><input type="checkbox" checked={active} onChange={e => setActive(e.target.checked)} className="h-5 w-5 accent-emerald-600" /></label>
        <div className="mt-4 grid gap-4 sm:grid-cols-2"><label className="text-sm font-bold">شروع اعتبار<input type="date" value={start} onChange={e => setStart(e.target.value)} className="mt-2 w-full rounded-xl border border-slate-300 px-3 py-2.5 font-normal" /></label><label className="text-sm font-bold">پایان اعتبار<input type="date" value={end} onChange={e => setEnd(e.target.value)} className="mt-2 w-full rounded-xl border border-slate-300 px-3 py-2.5 font-normal" /></label></div>
        <label className="mt-4 block text-sm font-bold">یادداشت داخلی<textarea value={note} onChange={e => setNote(e.target.value)} rows={3} maxLength={1000} className="mt-2 w-full rounded-xl border border-slate-300 px-3 py-2.5 font-normal" /></label>
        {error && <p role="alert" className="mt-4 rounded-xl bg-rose-50 p-3 text-sm text-rose-700">{error}</p>}
        <div className="mt-6 flex justify-end gap-3"><button type="button" onClick={() => setEditing(null)} className="rounded-xl border border-slate-300 px-4 py-2.5 text-sm font-bold">انصراف</button><button type="submit" disabled={saving} className="rounded-xl bg-slate-900 px-5 py-2.5 text-sm font-bold text-white disabled:opacity-50">{saving ? "در حال ذخیره..." : "ذخیره تغییرات"}</button></div>
      </form>
    </div>}
  </main>;
}
