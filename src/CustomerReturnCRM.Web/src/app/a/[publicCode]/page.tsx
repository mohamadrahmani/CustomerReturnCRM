"use client";

import { use, useEffect, useState } from "react";

const API = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5108";
type Appointment = { businessName: string; address: string | null; city: string | null; customerName: string; serviceTitle: string; staffName: string; startAt: string; endAt: string; status: string };

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat("fa-IR", { dateStyle: "full", timeStyle: "short" }).format(new Date(value));
}

function statusLabel(status: string) {
  const labels: Record<string, string> = { Pending: "در انتظار تأیید", Confirmed: "تأیید شده", Completed: "انجام شده", Cancelled: "لغو شده", NoShow: "عدم مراجعه" };
  return labels[status] ?? status;
}

export default function PublicAppointmentPage({ params }: { params: Promise<{ publicCode: string }> }) {
  const { publicCode } = use(params);
  const [data, setData] = useState<Appointment | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    setLoading(true); setError("");
    fetch(`${API}/api/public/appointments/${encodeURIComponent(publicCode)}`)
      .then(async r => { if (!r.ok) throw new Error("این نوبت پیدا نشد یا لینک آن معتبر نیست."); return r.json(); })
      .then(setData)
      .catch(e => setError(e instanceof Error ? e.message : "دریافت جزئیات نوبت ناموفق بود."))
      .finally(() => setLoading(false));
  }, [publicCode]);

  if (loading) return <main dir="rtl" className="min-h-screen bg-slate-50 p-4 sm:p-8"><div className="mx-auto max-w-xl animate-pulse rounded-3xl bg-white p-8 shadow-sm"><div className="h-7 w-40 rounded bg-slate-100"/><div className="mt-6 space-y-3">{Array.from({ length: 6 }).map((_, i) => <div key={i} className="h-5 rounded bg-slate-100"/>)}</div></div></main>;
  if (!data) return <main dir="rtl" className="min-h-screen bg-slate-50 p-4 sm:p-8"><div className="mx-auto max-w-xl rounded-3xl border border-red-100 bg-white p-8 text-center shadow-sm"><div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-red-50 text-red-600">!</div><h1 className="mt-4 text-xl font-bold">جزئیات نوبت در دسترس نیست</h1><p className="mt-2 text-sm text-slate-500">{error || "لینک نوبت معتبر نیست."}</p><a href="/" className="mt-6 inline-flex rounded-xl bg-slate-900 px-5 py-3 text-sm font-medium text-white">بازگشت</a></div></main>;

  return <main dir="rtl" className="min-h-screen bg-slate-50 p-4 sm:p-8">
    <div className="mx-auto max-w-xl pt-4 sm:pt-10">
      <section className="rounded-3xl bg-white p-7 shadow-sm sm:p-9">
        <div className="flex items-center gap-3"><div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-slate-100 text-slate-700">✓</div><div><p className="text-xs text-slate-500">نوبت آنلاین</p><h1 className="text-xl font-extrabold">{data.businessName}</h1></div></div>
        <div className="mt-7 rounded-2xl bg-slate-50 p-5"><span className="rounded-full bg-white px-3 py-1 text-xs font-semibold text-slate-700">{statusLabel(data.status)}</span><p className="mt-4 text-lg font-bold">{data.serviceTitle}</p><p className="mt-1 text-sm text-slate-600">{data.staffName}</p><p className="mt-4 text-sm leading-6 text-slate-600">{formatDateTime(data.startAt)}</p></div>
        <div className="mt-6 space-y-4 text-sm"><div className="flex justify-between gap-4 border-b border-slate-100 pb-3"><span className="text-slate-500">مشتری</span><span className="font-medium">{data.customerName}</span></div><div className="flex justify-between gap-4 border-b border-slate-100 pb-3"><span className="text-slate-500">شروع</span><span className="font-medium">{formatDateTime(data.startAt)}</span></div><div className="flex justify-between gap-4 border-b border-slate-100 pb-3"><span className="text-slate-500">پایان</span><span className="font-medium">{new Intl.DateTimeFormat("fa-IR", { dateStyle: "medium", timeStyle: "short" }).format(new Date(data.endAt))}</span></div>{(data.city || data.address) && <div className="flex justify-between gap-4"><span className="text-slate-500">آدرس</span><span className="text-left font-medium">{[data.city, data.address].filter(Boolean).join("، ")}</span></div>}</div>
        <p className="mt-7 text-center text-xs leading-6 text-slate-400">این صفحه برای مشاهده جزئیات نوبت شما ایجاد شده است.</p>
      </section>
    </div>
  </main>;
}
