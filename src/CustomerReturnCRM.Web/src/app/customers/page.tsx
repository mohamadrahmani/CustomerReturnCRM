"use client";

import Link from "next/link";
import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { createCustomer, getCustomers } from "@/lib/api";
import { useAuth } from "@/components/auth-provider";

function formatDate(value: string | null) {
  if (!value) return "بدون مراجعه";
  return new Intl.DateTimeFormat("fa-IR", { year: "numeric", month: "short", day: "numeric" }).format(new Date(value));
}

function CustomerAvatar() {
  return (
    <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-pink-50 text-pink-500 ring-2 ring-white">
      <svg viewBox="0 0 48 48" fill="none" className="h-9 w-9" aria-hidden="true">
        <path d="M8 39c2.2-7.2 7.4-10.8 15.8-10.8S37.4 31.8 40 39" fill="currentColor" opacity=".72" />
        <path d="M15 22.5C12.8 13.8 17.2 7 25.1 7c7.6 0 12.5 6.3 9.7 15.8-1.8-2.9-4.2-4.7-7.2-5.6-2.4 2.2-5.7 3.2-9.9 2.7-.8.8-1.6 1.7-2.7 2.6Z" fill="currentColor" />
        <path d="M17.5 24.5c1.1 3 3.5 4.7 6.7 4.7 3.5 0 5.9-1.9 6.9-4.7" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" opacity=".45" />
      </svg>
    </span>
  );
}

function StatusBadge({ active }: { active: boolean }) {
  return active ? (
    <span className="rounded-full bg-emerald-50 px-2.5 py-1 text-[10px] font-black text-emerald-700">فعال</span>
  ) : (
    <span className="rounded-full bg-slate-100 px-2.5 py-1 text-[10px] font-black text-slate-500">غیرفعال</span>
  );
}

function LoadingRows() {
  return <div className="space-y-2">{Array.from({ length: 6 }).map((_, i) => <div key={i} className="h-[72px] animate-pulse rounded-2xl bg-slate-100" />)}</div>;
}

export default function CustomersPage() {
  const { auth, businessId, isReady } = useAuth();
  const activeBusinessId = businessId ?? auth?.businesses[0]?.id ?? null;
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<"active" | "inactive" | "all">("active");
  const [page, setPage] = useState(1);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [form, setForm] = useState({ firstName: "", lastName: "", mobile: "" });
  const [formError, setFormError] = useState("");
  const [isSaving, setIsSaving] = useState(false);

  const isActive = status === "all" ? null : status === "active";
  const customers = useQuery({
    queryKey: ["customers", activeBusinessId, page, search, status],
    queryFn: () => getCustomers(activeBusinessId!, page, 12, search, isActive),
    enabled: isReady && !!activeBusinessId,
    staleTime: 15_000,
  });

  if (!isReady || !activeBusinessId) return <main className="p-4"><LoadingRows /></main>;

  const data = customers.data;
  const resetSearch = () => { setSearch(""); setPage(1); };

  async function handleCreate(event: React.FormEvent) {
    event.preventDefault();
    setFormError("");
    if (!form.firstName.trim() || !form.mobile.trim()) { setFormError("نام و شماره موبایل الزامی است."); return; }
    setIsSaving(true);
    try {
      await createCustomer(activeBusinessId!, { firstName: form.firstName.trim(), lastName: form.lastName.trim() || undefined, mobile: form.mobile.trim() });
      setForm({ firstName: "", lastName: "", mobile: "" });
      setIsCreateOpen(false);
      await queryClient.invalidateQueries({ queryKey: ["customers", activeBusinessId] });
    } catch (error) {
      setFormError(error instanceof Error ? error.message : "ثبت مشتری انجام نشد.");
    } finally { setIsSaving(false); }
  }

  return (
    <main className="-mx-4 -mt-5 min-h-[calc(100vh-65px)] bg-gradient-to-b from-rose-50 via-pink-50/60 to-violet-50/40 px-4 pb-10 pt-5 sm:-mx-8 sm:-mt-8 sm:px-8 sm:pt-7">
      <div className="mx-auto max-w-7xl">
        <header className="mb-5 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <p className="text-[11px] font-bold text-pink-600">مدیریت ارتباط</p>
            <h1 className="mt-1 text-2xl font-black tracking-tight text-slate-950 sm:text-3xl">مشتریان</h1>
            <p className="mt-1 text-xs leading-5 text-slate-500 sm:text-sm">مشتریان، سوابق مراجعه و وضعیت بازگشت آن‌ها را مدیریت کنید.</p>
          </div>
          <button type="button" onClick={() => { setFormError(""); setIsCreateOpen(true); }} className="crm-action self-start !min-h-10 !rounded-xl !px-4 !py-2.5 !text-xs bg-gradient-to-l from-pink-500 to-rose-500 shadow-md shadow-pink-200 sm:self-auto sm:!text-sm">
            <span className="ml-1 text-base">＋</span> مشتری جدید
          </button>
        </header>

        <section className="crm-card mb-4 p-3 sm:p-4">
          <div className="flex flex-col gap-3 lg:flex-row lg:items-center">
            <label className="relative min-w-0 flex-1">
              <span className="sr-only">جستجوی مشتری</span>
              <svg className="absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden="true"><circle cx="11" cy="11" r="7"/><path d="m20 20-4-4"/></svg>
              <input value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} placeholder="جستجو با نام یا شماره موبایل..." className="crm-input w-full !pr-9" />
            </label>
            <div className="grid grid-cols-3 gap-1 rounded-xl bg-slate-100 p-1 lg:w-[300px]">
              {([['active', 'فعال'], ['inactive', 'غیرفعال'], ['all', 'همه']] as const).map(([value, label]) => (
                <button key={value} type="button" onClick={() => { setStatus(value); setPage(1); }} className={`rounded-lg px-2 py-2 text-[11px] font-bold transition ${status === value ? "bg-white text-pink-600 shadow-sm" : "text-slate-500"}`}>{label}</button>
              ))}
            </div>
          </div>
        </section>

        {customers.isLoading ? <LoadingRows /> : customers.isError ? (
          <section className="crm-card p-8 text-center"><h2 className="font-black text-slate-900">بارگذاری مشتریان انجام نشد</h2><p className="mt-2 text-sm text-slate-500">اتصال به اطلاعات مشتریان با خطا مواجه شد.</p><button onClick={() => customers.refetch()} className="crm-secondary-action mt-4">تلاش مجدد</button></section>
        ) : !data?.items.length ? (
          <section className="crm-card p-10 text-center"><div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-pink-50 text-pink-500"><span className="text-2xl">⌕</span></div><h2 className="mt-4 text-lg font-black text-slate-900">مشتری‌ای پیدا نشد</h2><p className="mt-1 text-sm text-slate-500">فیلترها را تغییر دهید یا مشتری جدیدی ثبت کنید.</p>{search && <button onClick={resetSearch} className="mt-4 text-xs font-black text-pink-600">پاک کردن جستجو</button>}</section>
        ) : (
          <>
            <section className="hidden overflow-hidden rounded-3xl border border-white/80 bg-white/95 shadow-sm md:block">
              <div className="grid grid-cols-[minmax(260px,1.5fr)_160px_140px_120px_100px] items-center gap-4 border-b border-slate-100 px-6 py-3 text-[11px] font-black text-slate-400">
                <span>مشتری</span><span>موبایل</span><span>آخرین مراجعه</span><span>تعداد مراجعه</span><span>وضعیت</span>
              </div>
              {data.items.map((customer) => <Link href={`/customers/${customer.id}`} key={customer.id} className="grid grid-cols-[minmax(260px,1.5fr)_160px_140px_120px_100px] items-center gap-4 border-b border-slate-100 px-6 py-3.5 transition last:border-0 hover:bg-pink-50/40">
                <div className="flex min-w-0 items-center gap-3"><CustomerAvatar /><div className="min-w-0"><p className="truncate text-sm font-black text-slate-900">{customer.firstName} {customer.lastName}</p><p className="mt-0.5 text-[11px] text-slate-400">مشتری ثبت‌شده</p></div></div>
                <span dir="ltr" className="text-xs font-medium text-slate-600">{customer.mobile}</span><span className="text-xs text-slate-500">{formatDate(customer.lastVisitDate)}</span><span className="text-xs font-bold text-slate-700">{customer.totalVisits.toLocaleString("fa-IR")}</span><StatusBadge active={customer.isActive} />
              </Link>)}
            </section>

            <section className="space-y-2 md:hidden">
              {data.items.map((customer) => <Link href={`/customers/${customer.id}`} key={customer.id} className="group block rounded-2xl border border-white/80 bg-white/95 p-3.5 shadow-sm transition hover:border-pink-200">
                <div className="flex items-center gap-3"><CustomerAvatar /><div className="min-w-0 flex-1"><div className="flex items-center gap-2"><p className="truncate text-sm font-black text-slate-900">{customer.firstName} {customer.lastName}</p><StatusBadge active={customer.isActive} /></div><p dir="ltr" className="mt-1 text-right text-[11px] text-slate-500">{customer.mobile}</p></div><span className="text-lg text-slate-300 transition group-hover:-translate-x-1">←</span></div>
                <div className="mt-3 grid grid-cols-2 gap-2 border-t border-slate-100 pt-2.5"><div><p className="text-[10px] text-slate-400">آخرین مراجعه</p><p className="mt-0.5 text-[11px] font-bold text-slate-700">{formatDate(customer.lastVisitDate)}</p></div><div><p className="text-[10px] text-slate-400">تعداد مراجعه</p><p className="mt-0.5 text-[11px] font-bold text-slate-700">{customer.totalVisits.toLocaleString("fa-IR")}</p></div></div>
              </Link>)}
            </section>

            <div className="mt-4 flex items-center justify-between rounded-2xl border border-white/80 bg-white/80 px-3 py-2.5 text-[11px] font-bold text-slate-500 sm:px-4">
              <span>{data.totalCount.toLocaleString("fa-IR")} مشتری</span>
              <div className="flex items-center gap-1.5"><button disabled={data.page <= 1} onClick={() => setPage((p) => p - 1)} className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 disabled:opacity-40">قبلی</button><span className="px-2">صفحه {data.page.toLocaleString("fa-IR")} از {data.totalPages.toLocaleString("fa-IR")}</span><button disabled={data.page >= data.totalPages} onClick={() => setPage((p) => p + 1)} className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 disabled:opacity-40">بعدی</button></div>
            </div>
          </>
        )}
      </div>

      {isCreateOpen && <div className="fixed inset-0 z-50 flex items-end justify-center bg-slate-950/30 p-0 backdrop-blur-[2px] sm:items-center sm:p-4" onMouseDown={() => setIsCreateOpen(false)}>
        <div role="dialog" aria-modal="true" aria-labelledby="create-customer-title" className="w-full max-w-md rounded-t-3xl bg-white p-5 shadow-2xl sm:rounded-3xl" onMouseDown={(e) => e.stopPropagation()}>
          <div className="mb-5 flex items-center justify-between"><div><p className="text-[11px] font-bold text-pink-600">مشتری جدید</p><h2 id="create-customer-title" className="mt-1 text-xl font-black text-slate-950">ثبت مشتری</h2></div><button onClick={() => setIsCreateOpen(false)} className="flex h-9 w-9 items-center justify-center rounded-xl bg-slate-100 text-slate-500">×</button></div>
          <form onSubmit={handleCreate} className="space-y-3">
            <input autoFocus value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} placeholder="نام *" className="crm-input w-full" />
            <input value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} placeholder="نام خانوادگی" className="crm-input w-full" />
            <input value={form.mobile} onChange={(e) => setForm({ ...form, mobile: e.target.value })} placeholder="شماره موبایل *" inputMode="tel" className="crm-input w-full" />
            {formError && <p className="rounded-xl bg-rose-50 px-3 py-2 text-xs font-bold text-rose-700">{formError}</p>}
            <div className="flex gap-2 pt-2"><button type="button" onClick={() => setIsCreateOpen(false)} className="crm-secondary-action flex-1">انصراف</button><button disabled={isSaving} type="submit" className="crm-action flex-1 disabled:opacity-60">{isSaving ? "در حال ثبت..." : "ثبت مشتری"}</button></div>
          </form>
        </div>
      </div>}
    </main>
  );
}
