"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { createBaleConnectInvite, getCustomers, type Customer } from "@/lib/api";
import { useAuth } from "@/components/auth-provider";

function customerName(customer: Customer) { return [customer.firstName, customer.lastName].filter(Boolean).join(" "); }

export default function BaleSettingsPage() {
  const { auth, businessId, isReady } = useAuth();
  const activeBusinessId = businessId ?? auth?.businesses[0]?.id ?? null;
  const [search, setSearch] = useState("");
  const [selected, setSelected] = useState<Customer | null>(null);
  const [inviteUrl, setInviteUrl] = useState("");
  const [expiresAt, setExpiresAt] = useState("");
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);
  const customers = useQuery({ queryKey: ["bale-customers", activeBusinessId, search], queryFn: () => getCustomers(activeBusinessId!, 1, 20, search, true), enabled: isReady && !!activeBusinessId });

  const connect = async (customer: Customer) => {
    if (!activeBusinessId) return;
    setSelected(customer); setInviteUrl(""); setError(""); setSaving(true);
    try {
      const result = await createBaleConnectInvite(activeBusinessId, customer.id);
      setInviteUrl(result.connectUrl); setExpiresAt(result.expiresAtUtc);
    } catch (e) { setError(e instanceof Error ? e.message : "ساخت لینک اتصال انجام نشد."); }
    finally { setSaving(false); }
  };

  const copy = async () => { if (inviteUrl) await navigator.clipboard.writeText(inviteUrl); };

  if (!isReady || !activeBusinessId) return <main className="p-4" />;

  return (
    <main dir="rtl" className="-mx-4 -mt-5 min-h-[calc(100vh-65px)] bg-gradient-to-b from-rose-50 via-pink-50/60 to-violet-50/40 px-4 pb-10 pt-5 sm:-mx-8 sm:-mt-8 sm:px-8 sm:pt-7">
      <div className="mx-auto max-w-5xl">
        <div className="mb-5 flex items-center gap-2 text-xs font-bold text-slate-500"><a href="/settings" className="hover:text-pink-600">تنظیمات</a><span>←</span><span className="text-slate-800">بله</span></div>
        <header className="mb-5"><p className="text-[11px] font-bold text-pink-600">کانال ارتباطی</p><h1 className="mt-1 text-2xl font-black tracking-tight text-slate-950">اتصال مشتریان به بله</h1><p className="mt-1 text-sm leading-6 text-slate-500">برای هر مشتری یک لینک یک‌بارمصرف بسازید تا با باز کردن آن در بله، حساب مشتری به BEMOONI متصل شود.</p></header>

        <section className="crm-card mb-4 p-4 sm:p-5">
          <div className="mb-4 flex items-start gap-3"><div className="crm-icon-box bg-violet-50 text-violet-600">✈</div><div><h2 className="crm-section-title">اتصال مشتری</h2><p className="crm-muted mt-1">لینک ایجادشده ۱۵ دقیقه اعتبار دارد و فقط یک‌بار قابل استفاده است.</p></div></div>
          <label className="relative block"><span className="sr-only">جستجوی مشتری</span><svg className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8"><circle cx="11" cy="11" r="7"/><path d="m20 20-4-4"/></svg><input value={search} onChange={e => setSearch(e.target.value)} placeholder="جستجو با نام یا شماره موبایل..." className="crm-input w-full !pr-10" /></label>
        </section>

        <div className="grid gap-4 lg:grid-cols-[1fr_360px]">
          <section className="crm-card overflow-hidden"><div className="border-b border-slate-100 px-4 py-3.5"><h2 className="text-sm font-black text-slate-900">مشتریان فعال</h2></div>{customers.isLoading ? <div className="space-y-2 p-4">{[1,2,3,4].map(x => <div key={x} className="h-16 animate-pulse rounded-2xl bg-slate-50" />)}</div> : !customers.data?.items.length ? <div className="p-8 text-center text-sm text-slate-500">مشتری‌ای پیدا نشد.</div> : <div className="divide-y divide-slate-100">{customers.data.items.map(customer => <div key={customer.id} className={`flex items-center gap-3 px-4 py-3.5 transition ${selected?.id === customer.id ? "bg-violet-50/60" : "hover:bg-pink-50/40"}`}><span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-pink-50 text-pink-500 font-black">{customer.firstName.slice(0,1)}</span><div className="min-w-0 flex-1"><p className="truncate text-xs font-black text-slate-900">{customerName(customer)}</p><p dir="ltr" className="mt-1 text-right text-[10px] text-slate-400">{customer.mobile}</p></div><button disabled={saving} onClick={() => connect(customer)} className="crm-secondary-action !min-h-9 !rounded-lg !px-3 !py-2 !text-[10px]">اتصال به بله</button></div>)}</div>}</section>

          <section className="crm-card h-fit overflow-hidden"><div className="border-b border-slate-100 px-4 py-3.5"><h2 className="text-sm font-black text-slate-900">لینک فعال‌سازی</h2></div><div className="p-4">{!selected ? <div className="rounded-2xl bg-slate-50 p-5 text-center"><div className="mx-auto flex h-12 w-12 items-center justify-center rounded-2xl bg-white text-xl shadow-sm">✈</div><p className="mt-3 text-xs font-bold text-slate-700">یک مشتری را انتخاب کنید</p><p className="mt-1 text-[11px] leading-5 text-slate-400">بعد از ایجاد لینک، می‌توانید آن را کپی کنید و برای مشتری ارسال کنید.</p></div> : <><div className="rounded-2xl bg-violet-50/70 p-3.5"><p className="text-[10px] font-bold text-violet-600">مشتری انتخاب‌شده</p><p className="mt-1 text-sm font-black text-slate-900">{customerName(selected)}</p><p dir="ltr" className="mt-1 text-right text-[11px] text-slate-500">{selected.mobile}</p></div>{error && <div className="mt-3 rounded-xl bg-rose-50 px-3 py-2 text-[11px] font-bold leading-5 text-rose-700">{error}</div>}{inviteUrl && <div className="mt-3"><p className="mb-1.5 text-[10px] font-bold text-slate-500">لینک یک‌بارمصرف</p><div className="break-all rounded-xl border border-slate-200 bg-slate-50 p-3 text-[10px] leading-5 text-slate-600" dir="ltr">{inviteUrl}</div><button onClick={copy} className="crm-action mt-2 w-full !min-h-10 !rounded-xl !text-xs">کپی لینک فعال‌سازی</button><p className="mt-2 text-center text-[10px] text-slate-400">اعتبار تا {new Intl.DateTimeFormat("fa-IR", { hour: "2-digit", minute: "2-digit", day: "numeric", month: "short" }).format(new Date(expiresAt))}</p></div>}{!inviteUrl && !saving && !error && <button onClick={() => connect(selected)} className="crm-action mt-3 w-full !min-h-10 !rounded-xl !text-xs">ساخت لینک فعال‌سازی</button>}{saving && <div className="mt-3 rounded-xl bg-slate-50 px-3 py-3 text-center text-[11px] font-bold text-slate-500">در حال ساخت لینک...</div>}</>}</div></section>
        </div>
      </div>
    </main>
  );
}
