"use client";

import { useEffect, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { getCustomers, type Customer } from "@/lib/api";
import { useAuth } from "@/components/auth-provider";
import { SmsComposer } from "@/components/sms-composer";

function nameOf(customer: Customer) {
  return [customer.firstName, customer.lastName].filter(Boolean).join(" ");
}

function Avatar() {
  return <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-violet-50 text-violet-600"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" className="h-6 w-6" aria-hidden="true"><circle cx="12" cy="8" r="3.2"/><path d="M5.5 20c.9-3.6 3.1-5.4 6.5-5.4s5.6 1.8 6.5 5.4"/></svg></span>;
}

export default function BulkSmsPage() {
  const { auth, businessId, isReady } = useAuth();
  const activeBusinessId = businessId ?? auth?.businesses[0]?.id ?? null;
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState<Record<string, Customer>>({});
  const [composerOpen, setComposerOpen] = useState(false);

  useEffect(() => {
    const timer = window.setTimeout(() => { setSearch(searchInput.trim()); setPage(1); }, 300);
    return () => window.clearTimeout(timer);
  }, [searchInput]);

  const customers = useQuery({
    queryKey: ["bulk-sms-customers", activeBusinessId, page, search],
    queryFn: () => getCustomers(activeBusinessId!, page, 20, search, true),
    enabled: isReady && !!activeBusinessId,
    staleTime: 10_000,
  });

  const selectedCustomers = useMemo(() => Object.values(selected), [selected]);
  const pageItems = customers.data?.items ?? [];
  const selectedOnPage = pageItems.filter(customer => !!selected[customer.id]).length;
  const allPageSelected = pageItems.length > 0 && selectedOnPage === pageItems.length;

  const toggle = (customer: Customer) => {
    setSelected(current => {
      const next = { ...current };
      if (next[customer.id]) delete next[customer.id];
      else next[customer.id] = customer;
      return next;
    });
  };

  const togglePage = () => {
    setSelected(current => {
      const next = { ...current };
      if (allPageSelected) pageItems.forEach(customer => delete next[customer.id]);
      else pageItems.forEach(customer => { next[customer.id] = customer; });
      return next;
    });
  };

  if (!isReady || !activeBusinessId) return <main className="p-4"><div className="mx-auto max-w-5xl space-y-3">{[1,2,3,4].map(x => <div key={x} className="h-20 animate-pulse rounded-2xl bg-white/70" />)}</div></main>;

  return <main dir="rtl" className="-mx-4 -mt-5 min-h-[calc(100vh-65px)] bg-gradient-to-b from-violet-50 via-white to-slate-50 px-4 pb-10 pt-5 sm:-mx-8 sm:-mt-8 sm:px-8 sm:pt-7">
    <div className="mx-auto max-w-5xl">
      <header className="mb-5 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div><p className="text-[11px] font-bold text-violet-600">پیامک</p><h1 className="mt-1 text-2xl font-black tracking-tight text-slate-950 sm:text-3xl">ارسال گروهی SMS</h1><p className="mt-1 text-xs leading-5 text-slate-500 sm:text-sm">مشتریان را جستجو و انتخاب کنید، سپس پیام را برای آن‌ها ارسال یا زمان‌بندی کنید.</p></div>
        <button type="button" disabled={!selectedCustomers.length || selectedCustomers.length > 10000} onClick={() => setComposerOpen(true)} className="crm-action self-start !min-h-10 !rounded-xl !px-4 !py-2.5 !text-xs disabled:opacity-50 sm:self-auto sm:!text-sm">ارسال SMS به {selectedCustomers.length.toLocaleString("fa-IR")} مشتری</button>
      </header>

      <section className="crm-card mb-4 p-3.5 sm:p-4">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
          <label className="relative min-w-0 flex-1"><span className="sr-only">جستجوی مشتری</span><svg className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden="true"><circle cx="11" cy="11" r="7"/><path d="m20 20-4-4"/></svg><input value={searchInput} onChange={e => setSearchInput(e.target.value)} placeholder="جستجو با نام، نام خانوادگی یا شماره موبایل..." className="crm-input w-full !pr-10" /></label>
          <button type="button" disabled={!pageItems.length} onClick={togglePage} className="crm-secondary-action !min-h-10 !rounded-xl !px-4 !text-xs">{allPageSelected ? "لغو انتخاب این صفحه" : "انتخاب این صفحه"}</button>
          {selectedCustomers.length > 0 && <button type="button" onClick={() => setSelected({})} className="text-xs font-black text-rose-600">پاک کردن انتخاب‌ها</button>}
        </div>
        {selectedCustomers.length > 0 && <div className="mt-3 flex flex-wrap items-center gap-2 border-t border-slate-100 pt-3"><span className="rounded-full bg-violet-50 px-3 py-1.5 text-[11px] font-black text-violet-700">{selectedCustomers.length.toLocaleString("fa-IR")} مشتری انتخاب شده</span><span className="text-[10px] text-slate-400">انتخاب‌ها با تغییر جستجو و صفحه حفظ می‌شوند.</span></div>}
      </section>

      {customers.isLoading ? <div className="space-y-2">{[1,2,3,4,5].map(x => <div key={x} className="h-[72px] animate-pulse rounded-2xl bg-white/80" />)}</div> : customers.isError ? <section className="crm-card p-8 text-center"><h2 className="font-black text-slate-900">بارگذاری مشتریان انجام نشد</h2><button onClick={() => customers.refetch()} className="crm-secondary-action mt-4">تلاش مجدد</button></section> : !pageItems.length ? <section className="crm-card p-10 text-center text-sm text-slate-500">مشتری فعالی با این مشخصات پیدا نشد.</section> : <>
        <section className="hidden overflow-hidden rounded-3xl border border-white/80 bg-white/95 shadow-sm md:block">
          <div className="grid grid-cols-[44px_minmax(220px,1.4fr)_180px_110px] items-center gap-4 border-b border-slate-100 px-5 py-3 text-[11px] font-black text-slate-400"><span></span><span>مشتری</span><span>موبایل</span><span>انتخاب</span></div>
          {pageItems.map(customer => <button type="button" key={customer.id} onClick={() => toggle(customer)} className={`grid w-full grid-cols-[44px_minmax(220px,1.4fr)_180px_110px] items-center gap-4 border-b border-slate-100 px-5 py-3.5 text-right transition last:border-0 hover:bg-violet-50/40 ${selected[customer.id] ? "bg-violet-50/50" : ""}`}><span className={`flex h-6 w-6 items-center justify-center rounded-lg border text-sm font-black ${selected[customer.id] ? "border-violet-500 bg-violet-500 text-white" : "border-slate-300 bg-white text-transparent"}`}>✓</span><span className="flex min-w-0 items-center gap-3"><Avatar /><span className="truncate text-sm font-black text-slate-900">{nameOf(customer)}</span></span><span dir="ltr" className="text-xs text-slate-600">{customer.mobile}</span><span className="text-xs font-bold text-violet-700">{selected[customer.id] ? "انتخاب شده" : "انتخاب"}</span></button>)}
        </section>
        <section className="space-y-2 md:hidden">{pageItems.map(customer => <button type="button" key={customer.id} onClick={() => toggle(customer)} className={`flex w-full items-center gap-3 rounded-2xl border border-white/80 bg-white/95 p-3.5 text-right shadow-sm ${selected[customer.id] ? "ring-2 ring-violet-200" : ""}`}><span className={`flex h-6 w-6 shrink-0 items-center justify-center rounded-lg border text-sm font-black ${selected[customer.id] ? "border-violet-500 bg-violet-500 text-white" : "border-slate-300 bg-white text-transparent"}`}>✓</span><Avatar /><span className="min-w-0 flex-1"><span className="block truncate text-sm font-black text-slate-900">{nameOf(customer)}</span><span dir="ltr" className="mt-1 block text-right text-[11px] text-slate-500">{customer.mobile}</span></span></button>)}</section>
        <div className="mt-4 flex items-center justify-between rounded-2xl border border-white/80 bg-white/80 px-3 py-2.5 text-[11px] font-bold text-slate-500 sm:px-4"><span>{customers.data!.totalCount.toLocaleString("fa-IR")} مشتری</span><div className="flex items-center gap-1.5"><button disabled={customers.data!.page <= 1} onClick={() => setPage(p => p - 1)} className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 disabled:opacity-40">قبلی</button><span className="px-2">صفحه {customers.data!.page.toLocaleString("fa-IR")} از {customers.data!.totalPages.toLocaleString("fa-IR")}</span><button disabled={customers.data!.page >= customers.data!.totalPages} onClick={() => setPage(p => p + 1)} className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 disabled:opacity-40">بعدی</button></div></div>
      </>}
    </div>

    {composerOpen && <SmsComposer businessId={activeBusinessId} customers={selectedCustomers} title="ارسال گروهی SMS" onClose={() => setComposerOpen(false)} onCreated={() => {}} />}
  </main>;
}
