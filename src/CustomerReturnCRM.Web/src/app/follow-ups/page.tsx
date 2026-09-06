"use client";

import { FormEvent, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { cancelReminder, completeReminder, createReminder, getCustomers, getReminders, getServices } from "../../lib/api";
import { useAuth } from "../../components/auth-provider";

function formatDate(value: string) {
  return new Intl.DateTimeFormat("fa-IR", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

function statusLabel(status: number) {
  if (status === 1) return "انجام‌شده";
  if (status === 2) return "لغوشده";
  return "باز";
}

export default function FollowUpsPage() {
  const { businessId: activeBusinessId } = useAuth();
  const queryClient = useQueryClient();
  const [status, setStatus] = useState<number | null>(0);
  const [page, setPage] = useState(1);
  const [openCreate, setOpenCreate] = useState(false);
  const [customerId, setCustomerId] = useState("");
  const [serviceId, setServiceId] = useState("");
  const [title, setTitle] = useState("");
  const [dueAt, setDueAt] = useState("");
  const [note, setNote] = useState("");

  const reminders = useQuery({
    queryKey: ["reminders", activeBusinessId, status, page],
    queryFn: () => getReminders(activeBusinessId!, status, page, 20),
    enabled: !!activeBusinessId,
  });
  const customers = useQuery({
    queryKey: ["reminder-customers", activeBusinessId],
    queryFn: () => getCustomers(activeBusinessId!, 1, 100, "", true),
    enabled: !!activeBusinessId && openCreate,
  });
  const services = useQuery({
    queryKey: ["reminder-services", activeBusinessId],
    queryFn: () => getServices(activeBusinessId!, 1, 100, "", true),
    enabled: !!activeBusinessId && openCreate,
  });

  const customerMap = useMemo(() => new Map((customers.data?.items ?? []).map(x => [x.id, `${x.firstName} ${x.lastName ?? ""}`.trim()])), [customers.data]);
  const serviceMap = useMemo(() => new Map((services.data?.items ?? []).map(x => [x.id, x.title])), [services.data]);

  const create = useMutation({
    mutationFn: () => createReminder(activeBusinessId!, { customerId, serviceId: serviceId || undefined, title, dueAt: new Date(dueAt).toISOString(), note: note || undefined }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["reminders", activeBusinessId] });
      setOpenCreate(false); setCustomerId(""); setServiceId(""); setTitle(""); setDueAt(""); setNote("");
    },
  });
  const complete = useMutation({ mutationFn: (id: string) => completeReminder(activeBusinessId!, id), onSuccess: () => queryClient.invalidateQueries({ queryKey: ["reminders", activeBusinessId] }) });
  const cancel = useMutation({ mutationFn: (id: string) => cancelReminder(activeBusinessId!, id), onSuccess: () => queryClient.invalidateQueries({ queryKey: ["reminders", activeBusinessId] }) });

  function submit(event: FormEvent) {
    event.preventDefault();
    if (!customerId || !title.trim() || !dueAt) return;
    create.mutate();
  }

  return (
    <main className="space-y-6">
      <header className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div><h1 className="text-2xl font-bold text-slate-900">پیگیری‌ها</h1><p className="mt-1 text-sm text-slate-500">اقدام‌های دستی برای پیگیری مشتریان</p></div>
        <button onClick={() => setOpenCreate(true)} className="rounded-xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white">+ پیگیری جدید</button>
      </header>

      <div className="flex gap-2 overflow-x-auto rounded-2xl border border-slate-200 bg-white p-2">
        {[[null, "همه"], [0, "باز"], [1, "انجام‌شده"], [2, "لغوشده"]].map(([value, label]) => (
          <button key={String(value)} onClick={() => { setStatus(value as number | null); setPage(1); }} className={`whitespace-nowrap rounded-xl px-4 py-2 text-sm font-medium ${status === value ? "bg-slate-900 text-white" : "text-slate-600 hover:bg-slate-100"}`}>{label}</button>
        ))}
      </div>

      {reminders.isLoading && <div className="rounded-2xl border border-slate-200 bg-white p-10 text-center text-slate-500">در حال دریافت پیگیری‌ها...</div>}
      {reminders.isError && <div className="rounded-2xl border border-red-200 bg-red-50 p-5 text-sm text-red-700">دریافت پیگیری‌ها با خطا مواجه شد.</div>}
      {reminders.data && reminders.data.items.length === 0 && <div className="rounded-2xl border border-dashed border-slate-300 bg-white p-12 text-center"><p className="font-semibold text-slate-700">پیگیری‌ای برای نمایش وجود ندارد.</p><p className="mt-1 text-sm text-slate-500">از Smart List یا دکمه بالا یک پیگیری ثبت کنید.</p></div>}

      {reminders.data && reminders.data.items.length > 0 && (
        <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white">
          <div className="hidden overflow-x-auto md:block"><table className="w-full text-right text-sm"><thead className="bg-slate-50 text-slate-500"><tr><th className="px-5 py-3">مشتری</th><th className="px-5 py-3">عنوان</th><th className="px-5 py-3">موعد</th><th className="px-5 py-3">خدمت</th><th className="px-5 py-3">وضعیت</th><th className="px-5 py-3">عملیات</th></tr></thead><tbody className="divide-y divide-slate-100">
            {reminders.data.items.map(item => <tr key={item.id}><td className="px-5 py-4 font-medium text-slate-800">{customerMap.get(item.customerId) || "مشتری"}</td><td className="px-5 py-4 text-slate-700">{item.title}</td><td className="px-5 py-4 text-slate-600">{formatDate(item.dueAt)}</td><td className="px-5 py-4 text-slate-600">{item.serviceId ? serviceMap.get(item.serviceId) || "خدمت" : "—"}</td><td className="px-5 py-4"><span className="rounded-full bg-slate-100 px-2.5 py-1 text-xs">{statusLabel(item.status)}</span></td><td className="px-5 py-4">{item.status === 0 && <div className="flex gap-2"><button onClick={() => complete.mutate(item.id)} className="rounded-lg bg-emerald-50 px-3 py-1.5 text-xs font-semibold text-emerald-700">انجام شد</button><button onClick={() => cancel.mutate(item.id)} className="rounded-lg bg-red-50 px-3 py-1.5 text-xs font-semibold text-red-700">لغو</button></div>}</td></tr>)}
          </tbody></table></div>
          <div className="divide-y divide-slate-100 md:hidden">{reminders.data.items.map(item => <article key={item.id} className="p-4"><div className="flex items-start justify-between gap-3"><div><h3 className="font-semibold text-slate-800">{item.title}</h3><p className="mt-1 text-sm text-slate-500">{customerMap.get(item.customerId) || "مشتری"}</p></div><span className="rounded-full bg-slate-100 px-2.5 py-1 text-xs">{statusLabel(item.status)}</span></div><p className="mt-3 text-sm text-slate-600">موعد: {formatDate(item.dueAt)}</p>{item.serviceId && <p className="mt-1 text-sm text-slate-500">خدمت: {serviceMap.get(item.serviceId) || "خدمت"}</p>}{item.status === 0 && <div className="mt-4 flex gap-2"><button onClick={() => complete.mutate(item.id)} className="flex-1 rounded-lg bg-emerald-50 px-3 py-2 text-xs font-semibold text-emerald-700">انجام شد</button><button onClick={() => cancel.mutate(item.id)} className="flex-1 rounded-lg bg-red-50 px-3 py-2 text-xs font-semibold text-red-700">لغو</button></div>}</article>)}</div>
          {reminders.data.totalPages > 1 && <div className="flex items-center justify-between border-t border-slate-100 p-4 text-sm"><button disabled={page <= 1} onClick={() => setPage(x => x - 1)} className="rounded-lg border px-3 py-2 disabled:opacity-40">قبلی</button><span>صفحه {page} از {reminders.data.totalPages}</span><button disabled={page >= reminders.data.totalPages} onClick={() => setPage(x => x + 1)} className="rounded-lg border px-3 py-2 disabled:opacity-40">بعدی</button></div>}
        </section>
      )}

      {openCreate && <div className="fixed inset-0 z-50 flex items-end justify-center bg-slate-900/40 p-0 sm:items-center sm:p-4"><form onSubmit={submit} className="w-full max-w-lg space-y-4 rounded-t-3xl bg-white p-5 shadow-xl sm:rounded-3xl"><div className="flex items-center justify-between"><h2 className="text-lg font-bold">پیگیری جدید</h2><button type="button" onClick={() => setOpenCreate(false)} className="text-slate-400">✕</button></div><label className="block text-sm"><span className="mb-1 block font-medium">مشتری</span><select value={customerId} onChange={e => setCustomerId(e.target.value)} className="w-full rounded-xl border px-3 py-2.5" required><option value="">انتخاب مشتری</option>{customers.data?.items.map(x => <option key={x.id} value={x.id}>{x.firstName} {x.lastName ?? ""} — {x.mobile}</option>)}</select></label><label className="block text-sm"><span className="mb-1 block font-medium">خدمت مرتبط</span><select value={serviceId} onChange={e => setServiceId(e.target.value)} className="w-full rounded-xl border px-3 py-2.5"><option value="">بدون خدمت مشخص</option>{services.data?.items.map(x => <option key={x.id} value={x.id}>{x.title}</option>)}</select></label><label className="block text-sm"><span className="mb-1 block font-medium">عنوان پیگیری</span><input value={title} onChange={e => setTitle(e.target.value)} className="w-full rounded-xl border px-3 py-2.5" placeholder="مثلاً تماس برای یادآوری مراجعه" required /></label><label className="block text-sm"><span className="mb-1 block font-medium">موعد پیگیری</span><input type="datetime-local" value={dueAt} onChange={e => setDueAt(e.target.value)} className="w-full rounded-xl border px-3 py-2.5" required /></label><label className="block text-sm"><span className="mb-1 block font-medium">یادداشت</span><textarea value={note} onChange={e => setNote(e.target.value)} rows={3} className="w-full rounded-xl border px-3 py-2.5" /></label>{create.isError && <p className="text-sm text-red-600">ثبت پیگیری انجام نشد.</p>}<div className="flex gap-2 pt-2"><button type="button" onClick={() => setOpenCreate(false)} className="flex-1 rounded-xl border px-4 py-3 font-semibold">انصراف</button><button disabled={create.isPending} className="flex-1 rounded-xl bg-slate-900 px-4 py-3 font-semibold text-white disabled:opacity-50">{create.isPending ? "در حال ثبت..." : "ثبت پیگیری"}</button></div></form></div>}
    </main>
  );
}
