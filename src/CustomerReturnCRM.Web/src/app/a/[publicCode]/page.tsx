"use client";

import { use, useEffect, useState } from "react";

const API = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5108";
type Appointment = { businessName: string; address: string | null; city: string | null; customerName: string; serviceTitle: string; staffName: string; startAt: string; endAt: string; status: string };

export default function PublicAppointmentPage({ params }: { params: Promise<{ publicCode: string }> }) {
  const { publicCode } = use(params);
  const [data, setData] = useState<Appointment | null>(null);
  const [error, setError] = useState("");

  useEffect(() => {
    fetch(`${API}/api/public/appointments/${encodeURIComponent(publicCode)}`)
      .then(async r => { if (!r.ok) throw new Error("نوبت پیدا نشد."); return r.json(); })
      .then(setData)
      .catch(e => setError(e.message));
  }, [publicCode]);

  if (!data) return <main dir="rtl" className="min-h-screen bg-slate-50 p-6"><div className="mx-auto max-w-xl rounded-3xl bg-white p-8 shadow-sm">{error || "در حال بارگذاری..."}</div></main>;

  return <main dir="rtl" className="min-h-screen bg-slate-50 p-4 sm:p-8">
    <div className="mx-auto max-w-xl rounded-3xl bg-white p-8 shadow-sm">
      <h1 className="text-2xl font-bold">جزئیات نوبت</h1>
      <p className="mt-4 text-lg font-medium">{data.businessName}</p>
      <div className="mt-5 space-y-3 text-sm">
        <p><span className="text-slate-500">مشتری:</span> {data.customerName}</p>
        <p><span className="text-slate-500">خدمت:</span> {data.serviceTitle}</p>
        <p><span className="text-slate-500">متخصص:</span> {data.staffName}</p>
        <p><span className="text-slate-500">زمان:</span> {new Date(data.startAt).toLocaleString("fa-IR")}</p>
        <p><span className="text-slate-500">وضعیت:</span> {data.status}</p>
        {(data.city || data.address) && <p><span className="text-slate-500">آدرس:</span> {[data.city, data.address].filter(Boolean).join("، ")}</p>}
      </div>
    </div>
  </main>;
}
