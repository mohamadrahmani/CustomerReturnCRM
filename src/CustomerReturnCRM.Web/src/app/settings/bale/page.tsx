"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { getCustomers, type Customer } from "@/lib/api";
import { readAuth } from "@/lib/auth";
import { useAuth } from "@/components/auth-provider";

const API = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5108";

function customerName(customer: Customer) {
  return [customer.firstName, customer.lastName].filter(Boolean).join(" ");
}

async function shareBusinessCard(businessId: string, customerId: string, channel: "Sms" | "Bale") {
  const auth = readAuth();
  const response = await fetch(`${API}/api/business-card/businesses/${businessId}/customers/${customerId}/share?channel=${channel}`, {
    method: "POST",
    headers: auth?.token ? { Authorization: `Bearer ${auth.token}` } : undefined,
  });
  const body = await response.json().catch(() => null);
  if (!response.ok) throw new Error(body?.detail || body?.error || "ارسال کارت ویزیت انجام نشد.");
  return body as { publicUrl: string; sent: boolean; error: string | null };
}

export default function BaleSettingsPage() {
  const { auth, businessId, isReady } = useAuth();
  const activeBusinessId = businessId ?? auth?.businesses[0]?.id ?? null;
  const [search, setSearch] = useState("");
  const [selected, setSelected] = useState<Customer | null>(null);
  const [inviteUrl, setInviteUrl] = useState("");
  const [expiresAt, setExpiresAt] = useState("");
  const [cardUrl, setCardUrl] = useState("");
  const [error, setError] = useState("");
  const [smsError, setSmsError] = useState("");
  const [cardError, setCardError] = useState("");
  const [saving, setSaving] = useState(false);
  const [cardSending, setCardSending] = useState<"Sms" | "Bale" | null>(null);
  const [copied, setCopied] = useState(false);
  const customers = useQuery({
    queryKey: ["bale-customers", activeBusinessId, search],
    queryFn: () => getCustomers(activeBusinessId!, 1, 20, search, true),
    enabled: isReady && !!activeBusinessId,
  });

  const connectAndSendSms = async (customer: Customer) => {
    if (!activeBusinessId) return;
    setSelected(customer); setInviteUrl(""); setExpiresAt(""); setError(""); setSmsError(""); setCopied(false); setSaving(true);
    try {
      const auth = readAuth();
      const response = await fetch(`${API}/api/bale/businesses/${activeBusinessId}/customers/${customer.id}/connect-and-sms`, {
        method: "POST",
        headers: auth?.token ? { Authorization: `Bearer ${auth.token}` } : undefined,
      });
      const result = await response.json();
      if (!response.ok) throw new Error(result?.detail || result?.error || "ساخت لینک و ارسال پیامک انجام نشد.");
      setInviteUrl(result.invite.connectUrl); setExpiresAt(result.invite.expiresAtUtc);
      if (!result.smsSent) setSmsError(result.smsError || "لینک ساخته شد، اما ارسال پیامک انجام نشد.");
    } catch (e) { setError(e instanceof Error ? e.message : "ساخت لینک و ارسال پیامک انجام نشد."); }
    finally { setSaving(false); }
  };

  const sendCard = async (channel: "Sms" | "Bale") => {
    if (!activeBusinessId || !selected) return;
    setCardSending(channel); setCardError("");
    try {
      const result = await shareBusinessCard(activeBusinessId, selected.id, channel);
      setCardUrl(result.publicUrl);
      if (!result.sent) setCardError(result.error || "ارسال کارت ویزیت انجام نشد.");
    } catch (e) { setCardError(e instanceof Error ? e.message : "ارسال کارت ویزیت انجام نشد."); }
    finally { setCardSending(null); }
  };

  const copy = async (value: string) => {
    await navigator.clipboard.writeText(value);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1800);
  };

  if (!isReady || !activeBusinessId) return <main className="p-4" />;

  return (
    <main dir="rtl" className="-mx-4 -mt-5 min-h-[calc(100vh-65px)] bg-gradient-to-b from-rose-50 via-pink-50/60 to-violet-50/40 px-4 pb-10 pt-5 sm:-mx-8 sm:-mt-8 sm:px-8 sm:pt-7">
      <div className="mx-auto max-w-5xl">
        <div className="mb-5 flex items-center gap-2 text-xs font-bold text-slate-500"><a href="/settings" className="hover:text-pink-600">تنظیمات</a><span>←</span><span className="text-slate-800">ارتباط و اشتراک‌گذاری</span></div>
        <header className="mb-5"><p className="text-[11px] font-bold text-pink-600">کانال ارتباطی</p><h1 className="mt-1 text-2xl font-black tracking-tight text-slate-950">بله و کارت ویزیت</h1><p className="mt-1 text-sm leading-6 text-slate-500">از همین مسیر مشتری را به بله متصل کنید یا لینک کارت ویزیت و صفحه رزرو کسب‌وکار را برای او بفرستید.</p></header>

        <section className="crm-card mb-4 p-4 sm:p-5">
          <div className="mb-4 flex items-start gap-3"><div className="crm-icon-box bg-violet-50 text-violet-600">✈</div><div><h2 className="crm-section-title">انتخاب مشتری</h2><p className="crm-muted mt-1">مشتری را جستجو کنید و سپس یکی از عملیات اتصال بله یا ارسال کارت ویزیت را انجام دهید.</p></div></div>
          <label className="relative block"><span className="sr-only">جستجوی مشتری</span><svg className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8"><circle cx="11" cy="11" r="7"/><path d="m20 20-4-4"/></svg><input value={search} onChange={e => setSearch(e.target.value)} placeholder="جستجو با نام یا شماره موبایل..." className="crm-input w-full !pr-10" /></label>
        </section>

        <div className="grid gap-4 lg:grid-cols-[1fr_380px]">
          <section className="crm-card overflow-hidden">
            <div className="border-b border-slate-100 px-4 py-3.5"><h2 className="text-sm font-black text-slate-900">مشتریان فعال</h2></div>
            {customers.isLoading ? <div className="space-y-2 p-4">{[1,2,3,4].map(x => <div key={x} className="h-16 animate-pulse rounded-2xl bg-slate-50" />)}</div> : !customers.data?.items.length ? <div className="p-8 text-center text-sm text-slate-500">مشتری‌ای پیدا نشد.</div> : <div className="divide-y divide-slate-100">{customers.data.items.map(customer => <div key={customer.id} onClick={() => { setSelected(customer); setError(""); setSmsError(""); setCardError(""); }} className={`cursor-pointer px-4 py-3.5 transition ${selected?.id === customer.id ? "bg-violet-50/60" : "hover:bg-pink-50/40"}`}><div className="flex items-center gap-3"><span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-pink-50 font-black text-pink-500">{customer.firstName.slice(0,1)}</span><div className="min-w-0 flex-1"><p className="truncate text-xs font-black text-slate-900">{customerName(customer)}</p><p dir="ltr" className="mt-1 text-right text-[10px] text-slate-400">{customer.mobile}</p></div><span className="text-[10px] font-bold text-slate-400">انتخاب</span></div></div>)}</div>}
          </section>

          <section className="space-y-4">
            <section className="crm-card h-fit overflow-hidden"><div className="border-b border-slate-100 px-4 py-3.5"><h2 className="text-sm font-black text-slate-900">اتصال به بله</h2></div><div className="p-4">{!selected ? <div className="rounded-2xl bg-slate-50 p-5 text-center"><div className="mx-auto flex h-12 w-12 items-center justify-center rounded-2xl bg-white text-xl shadow-sm">✈</div><p className="mt-3 text-xs font-bold text-slate-700">ابتدا مشتری را انتخاب کنید</p></div> : <><div className="rounded-2xl bg-violet-50/70 p-3.5"><p className="text-[10px] font-bold text-violet-600">مشتری انتخاب‌شده</p><p className="mt-1 text-sm font-black text-slate-900">{customerName(selected)}</p><p dir="ltr" className="mt-1 text-right text-[11px] text-slate-500">{selected.mobile}</p></div>{error && <div className="mt-3 rounded-xl bg-rose-50 px-3 py-2 text-[11px] font-bold leading-5 text-rose-700">{error}</div>}{smsError && <div className="mt-3 rounded-xl bg-amber-50 px-3 py-2 text-[11px] font-bold leading-5 text-amber-700">{smsError}</div>}{inviteUrl ? <div className="mt-3"><div className="break-all rounded-xl border border-slate-200 bg-slate-50 p-3 text-[10px] leading-5 text-slate-600" dir="ltr">{inviteUrl}</div><button onClick={() => copy(inviteUrl)} className="crm-action mt-2 w-full !min-h-10 !rounded-xl !text-xs">{copied ? "کپی شد ✓" : "کپی لینک فعال‌سازی"}</button><p className="mt-2 text-center text-[10px] text-slate-400">اعتبار تا {new Intl.DateTimeFormat("fa-IR", { hour: "2-digit", minute: "2-digit", day: "numeric", month: "short" }).format(new Date(expiresAt))}</p></div> : <button disabled={saving || !selected.mobile} onClick={() => connectAndSendSms(selected)} className="crm-action mt-3 w-full !min-h-10 !rounded-xl !text-xs disabled:cursor-not-allowed disabled:opacity-50">{saving ? "در حال ساخت لینک و ارسال پیامک..." : "اتصال بله و ارسال لینک با پیامک"}</button>}</>}</div></section>

            <section className="crm-card h-fit overflow-hidden"><div className="border-b border-slate-100 px-4 py-3.5"><h2 className="text-sm font-black text-slate-900">ارسال کارت ویزیت</h2></div><div className="p-4">{!selected ? <div className="rounded-2xl bg-slate-50 p-5 text-center text-xs text-slate-500">یک مشتری را انتخاب کنید.</div> : <><p className="text-[11px] leading-5 text-slate-500">لینک عمومی کسب‌وکار همراه با اطلاعات ثبت‌شده در تنظیمات و امکان رزرو آنلاین برای مشتری ارسال می‌شود.</p><div className="mt-3 grid grid-cols-2 gap-2"><button disabled={cardSending !== null || !selected.mobile} onClick={() => sendCard("Sms")} className="crm-action !min-h-10 !rounded-xl !text-xs disabled:cursor-not-allowed disabled:opacity-50">{cardSending === "Sms" ? "در حال ارسال..." : "ارسال با پیامک"}</button><button disabled={cardSending !== null} onClick={() => sendCard("Bale")} className="crm-secondary-action !min-h-10 !rounded-xl !text-xs disabled:cursor-not-allowed disabled:opacity-50">{cardSending === "Bale" ? "در حال ارسال..." : "ارسال در بله"}</button></div>{cardError && <div className="mt-3 rounded-xl bg-amber-50 px-3 py-2 text-[11px] font-bold leading-5 text-amber-700">{cardError}</div>}{cardUrl && <div className="mt-3"><div className="break-all rounded-xl border border-slate-200 bg-slate-50 p-3 text-[10px] leading-5 text-slate-600" dir="ltr">{cardUrl}</div><button onClick={() => copy(cardUrl)} className="crm-secondary-action mt-2 w-full !min-h-10 !rounded-xl !text-xs">{copied ? "کپی شد ✓" : "کپی لینک کارت ویزیت"}</button></div>}</>}</div></section>
          </section>
        </div>
      </div>
    </main>
  );
}
