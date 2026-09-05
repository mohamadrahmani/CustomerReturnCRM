"use client";

import Link from "next/link";
import { useState } from "react";
import { useQuery, useQueryClient, useMutation } from "@tanstack/react-query";
import { apiFetch, createReminder, type PagedResult } from "@/lib/api";
import { useAuth } from "@/components/auth-provider";

type SmartListItem = { customerId: string; customerName: string; mobile: string; serviceId: string | null; serviceTitle: string | null; lastVisitAt: string; expectedReturnDate: string | null; daysFromExpectedReturn: number | null; smartListType: string };
type ListKey = "due-soon" | "overdue" | "at-risk" | "no-recent-visit";

const tabs: { key: ListKey; label: string; tone: string }[] = [
  { key: "due-soon", label: "نزدیک بازگشت", tone: "amber" },
  { key: "overdue", label: "موعد گذشته", tone: "orange" },
  { key: "at-risk", label: "در معرض ریزش", tone: "rose" },
  { key: "no-recent-visit", label: "بدون مراجعه اخیر", tone: "slate" },
];

const fmtDate = (v: string | null) => v ? new Intl.DateTimeFormat("fa-IR", { year: "numeric", month: "short", day: "numeric" }).format(new Date(v)) : "—";
const name = (x: SmartListItem) => x.customerName || "مشتری بدون نام";

function toneClass(type: string) {
  if (type === "DueSoon") return "bg-amber-50 text-amber-700 border-amber-200";
  if (type === "Overdue") return "bg-orange-50 text-orange-700 border-orange-200";
  if (type === "AtRisk") return "bg-rose-50 text-rose-700 border-rose-200";
  return "bg-slate-100 text-slate-600 border-slate-200";
}

export default function ReturnAnalysisPage() {
  const { auth, businessId, isReady } = useAuth();
  const activeBusinessId = businessId ?? auth?.businesses[0]?.id ?? null;
  const qc = useQueryClient();
  const [tab, setTab] = useState<ListKey>("due-soon");
  const [page, setPage] = useState(1);
  const [working, setWorking] = useState<string | null>(null);
  const [error, setError] = useState("");
  const [reminderItem, setReminderItem] = useState<SmartListItem | null>(null);
  const [reminderTitle, setReminderTitle] = useState("");
  const [reminderDueAt, setReminderDueAt] = useState("");

  const query = useQuery({
    queryKey: ["smart-list", activeBusinessId, tab, page],
    queryFn: () => apiFetch<PagedResult<SmartListItem>>(`/api/businesses/${activeBusinessId}/smart-lists/${tab}?page=${page}&pageSize=15`),
    enabled: isReady && !!activeBusinessId,
    staleTime: 15_000,
  });

  const reminderMutation = useMutation({
    mutationFn: () => createReminder(activeBusinessId!, {
      customerId: reminderItem!.customerId,
      serviceId: reminderItem!.serviceId ?? undefined,
      title: reminderTitle.trim(),
      dueAt: new Date(reminderDueAt).toISOString(),
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["reminders", activeBusinessId] });
      setReminderItem(null); setReminderTitle(""); setReminderDueAt("");
    },
  });

  if (!isReady || !activeBusinessId) return <main className="p-4"><div className="h-80 animate-pulse rounded-3xl bg-white/70" /></main>;
  const data = query.data;

  async function dismiss(item: SmartListItem) {
    const key = `${item.customerId}:${item.serviceId ?? "none"}`;
    setWorking(key); setError("");
    try {
      await apiFetch(`/api/businesses/${activeBusinessId}/smart-lists/dismiss`, { method: "POST", body: JSON.stringify({ smartListType: item.smartListType, customerId: item.customerId, serviceId: item.serviceId }) });
      await qc.invalidateQueries({ queryKey: ["smart-list", activeBusinessId] });
    } catch (e) { setError(e instanceof Error ? e.message : "خروج از فهرست انجام نشد."); } finally { setWorking(null); }
  }

  return <main className="-mx-4 -mt-5 min-h-[calc(100vh-65px)] bg-gradient-to-b from-rose-50 via-pink-50/60 to-violet-50/40 px-4 pb-10 pt-5 sm:-mx-8 sm:-mt-8 sm:px-8 sm:pt-7">
    <div className="mx-auto max-w-7xl">
      <header className="mb-5"><p className="text-[11px] font-bold text-pink-600">اقدام بر اساس رفتار مشتری</p><h1 className="mt-1 text-2xl font-black tracking-tight text-slate-950 sm:text-3xl">تحلیل بازگشت</h1><p className="mt-1 max-w-2xl text-xs leading-5 text-slate-500 sm:text-sm">مشتریانی را ببینید که بر اساس سابقه واقعی مراجعه، اکنون زمان مناسبی برای پیگیری آن‌هاست.</p></header>
      <section className="crm-card mb-4 p-2 sm:p-3"><div className="grid grid-cols-2 gap-1 sm:grid-cols-4">{tabs.map(t => <button key={t.key} type="button" onClick={() => { setTab(t.key); setPage(1); setError(""); }} className={`rounded-xl px-3 py-3 text-xs font-black transition ${tab === t.key ? "bg-slate-900 text-white shadow-sm" : "text-slate-500 hover:bg-slate-50"}`}>{t.label}</button>)}</div></section>
      {error && <div role="alert" className="mb-4 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-xs font-bold text-rose-700">{error}</div>}
      {query.isLoading ? <div className="space-y-2">{[1,2,3,4,5].map(x => <div key={x} className="h-28 animate-pulse rounded-2xl bg-white/70" />)}</div> : query.isError ? <section className="crm-card p-8 text-center"><h2 className="font-black">بارگذاری تحلیل بازگشت انجام نشد</h2><button onClick={() => query.refetch()} className="crm-secondary-action mt-4">تلاش مجدد</button></section> : !data?.items.length ? <section className="crm-card p-10 text-center"><div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-emerald-50 text-emerald-600">✓</div><h2 className="mt-4 text-lg font-black text-slate-900">موردی در این فهرست نیست</h2><p className="mt-1 text-sm text-slate-500">در حال حاضر مشتری‌ای مطابق این وضعیت پیدا نشد.</p></section> : <>
        <div className="mb-3 flex items-center justify-between text-[11px] font-bold text-slate-500"><span>{data.totalCount.toLocaleString("fa-IR")} مورد</span><span>بر اساس آخرین مراجعه و چرخه بازگشت</span></div>
        <section className="space-y-2 md:space-y-3">{data.items.map(item => { const key = `${item.customerId}:${item.serviceId ?? "none"}`; return <article key={key} className="rounded-2xl border border-white/80 bg-white/95 p-4 shadow-sm sm:p-5"><div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between"><div className="min-w-0"><div className="flex flex-wrap items-center gap-2"><Link href={`/customers/${item.customerId}`} className="truncate text-sm font-black text-slate-900 hover:text-pink-600">{name(item)}</Link><span className={`rounded-full border px-2 py-1 text-[10px] font-black ${toneClass(item.smartListType)}`}>{tabs.find(x => ({ DueSoon: "due-soon", Overdue: "overdue", AtRisk: "at-risk", NoRecentVisit: "no-recent-visit" } as Record<string,string>)[item.smartListType] === x.key)?.label ?? item.smartListType}</span></div><p className="mt-1 text-xs text-slate-500">{item.mobile}</p><div className="mt-3 grid grid-cols-2 gap-x-6 gap-y-2 text-[11px] sm:grid-cols-3"><div><span className="text-slate-400">خدمت</span><p className="mt-0.5 font-bold text-slate-700">{item.serviceTitle || "همه خدمات"}</p></div><div><span className="text-slate-400">آخرین مراجعه</span><p className="mt-0.5 font-bold text-slate-700">{fmtDate(item.lastVisitAt)}</p></div><div><span className="text-slate-400">موعد بازگشت</span><p className={`mt-0.5 font-bold ${item.expectedReturnDate ? "text-slate-700" : "text-slate-400"}`}>{fmtDate(item.expectedReturnDate)}</p></div></div></div><div className="flex shrink-0 flex-wrap gap-2 border-t border-slate-100 pt-3 lg:border-0 lg:pt-0"><Link href={`/customers/${item.customerId}`} className="crm-secondary-action !min-h-10 !px-3 !text-xs">مشاهده مشتری</Link><button type="button" disabled={working === key} onClick={() => { setReminderItem(item); setReminderTitle(item.serviceTitle ? `پیگیری ${item.serviceTitle}` : "پیگیری مشتری"); }} className="rounded-xl bg-emerald-50 px-3 py-2 text-xs font-bold text-emerald-700 disabled:opacity-50">ایجاد پیگیری</button><button type="button" disabled={working === key} onClick={() => dismiss(item)} className="crm-action !min-h-10 !rounded-xl !px-3 !py-2 !text-xs disabled:opacity-50">{working === key ? "در حال ثبت..." : "فعلاً پیگیری نمی‌کنم"}</button></div></div></article>; })}</section>
        <div className="mt-4 flex items-center justify-between rounded-2xl border border-white/80 bg-white/80 px-3 py-2.5 text-[11px] font-bold text-slate-500"><span>صفحه {data.page.toLocaleString("fa-IR")} از {data.totalPages.toLocaleString("fa-IR")}</span><div className="flex gap-1.5"><button disabled={data.page <= 1} onClick={() => setPage(p => p - 1)} className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 disabled:opacity-40">قبلی</button><button disabled={data.page >= data.totalPages} onClick={() => setPage(p => p + 1)} className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 disabled:opacity-40">بعدی</button></div></div>
      </>}
    </div>
    {reminderItem && <div className="fixed inset-0 z-50 flex items-end justify-center bg-slate-950/40 p-0 sm:items-center sm:p-4"><form onSubmit={e => { e.preventDefault(); if (reminderTitle.trim() && reminderDueAt) reminderMutation.mutate(); }} className="w-full max-w-md space-y-4 rounded-t-3xl bg-white p-5 shadow-xl sm:rounded-3xl"><div><h2 className="text-lg font-black text-slate-900">ایجاد پیگیری</h2><p className="mt-1 text-xs text-slate-500">{name(reminderItem)}{reminderItem.serviceTitle ? ` — ${reminderItem.serviceTitle}` : ""}</p></div><label className="block text-sm"><span className="mb-1 block font-medium">عنوان</span><input value={reminderTitle} onChange={e => setReminderTitle(e.target.value)} className="w-full rounded-xl border px-3 py-2.5" required /></label><label className="block text-sm"><span className="mb-1 block font-medium">موعد پیگیری</span><input type="datetime-local" value={reminderDueAt} onChange={e => setReminderDueAt(e.target.value)} className="w-full rounded-xl border px-3 py-2.5" required /></label>{reminderMutation.isError && <p className="text-xs text-red-600">ثبت پیگیری انجام نشد.</p>}<div className="flex gap-2"><button type="button" onClick={() => setReminderItem(null)} className="flex-1 rounded-xl border px-4 py-3 font-bold">انصراف</button><button disabled={reminderMutation.isPending} className="flex-1 rounded-xl bg-slate-900 px-4 py-3 font-bold text-white disabled:opacity-50">{reminderMutation.isPending ? "در حال ثبت..." : "ثبت پیگیری"}</button></div></form></div>}
  </main>;
}
