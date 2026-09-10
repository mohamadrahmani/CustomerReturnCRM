"use client";

import { useEffect, useMemo, useState } from "react";

const API = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5108";

type Service = { id: string; title: string; description: string | null; price: number; durationMinutes: number };
type Staff = { id: string; firstName: string; lastName: string };
type Profile = { businessId: string; name: string; mobile: string; address: string | null; city: string | null; description: string | null; services: Service[]; staff: Staff[] };
type Slot = { staffId: string; startAt: string; endAt: string };
type Booking = { publicDetailsUrl: string; startAt: string; endAt: string; serviceTitle: string; staffName: string };

export default function PublicBookingPage({ params }: { params: { slug: string } }) {
  const [profile, setProfile] = useState<Profile | null>(null);
  const [serviceId, setServiceId] = useState("");
  const [staffId, setStaffId] = useState("");
  const [date, setDate] = useState("");
  const [slot, setSlot] = useState<Slot | null>(null);
  const [slots, setSlots] = useState<Slot[]>([]);
  const [form, setForm] = useState({ firstName: "", lastName: "", mobile: "", note: "" });
  const [loading, setLoading] = useState(true);
  const [booking, setBooking] = useState<Booking | null>(null);
  const [error, setError] = useState("");

  useEffect(() => {
    fetch(`${API}/api/public/businesses/${encodeURIComponent(params.slug)}/profile`)
      .then(async r => { if (!r.ok) throw new Error("این صفحه رزرو در دسترس نیست."); return r.json(); })
      .then((data: Profile) => { setProfile(data); setServiceId(data.services[0]?.id ?? ""); })
      .catch(e => setError(e.message))
      .finally(() => setLoading(false));
  }, [params.slug]);

  useEffect(() => {
    setSlots([]); setSlot(null);
    if (!profile || !serviceId || !date) return;
    const qs = new URLSearchParams({ serviceId, date, slotIntervalMinutes: "30" });
    if (staffId) qs.set("staffId", staffId);
    fetch(`${API}/api/public/businesses/${profile.businessId}/availability?${qs}`)
      .then(async r => { if (!r.ok) throw new Error("دریافت زمان‌های آزاد ناموفق بود."); return r.json(); })
      .then(setSlots)
      .catch(e => setError(e.message));
  }, [profile, serviceId, staffId, date]);

  const selectedService = useMemo(() => profile?.services.find(x => x.id === serviceId), [profile, serviceId]);
  const selectedStaff = useMemo(() => profile?.staff.find(x => x.id === staffId), [profile, staffId]);

  async function submit() {
    if (!profile || !selectedService || !slot) return;
    setError("");
    try {
      const response = await fetch(`${API}/api/public/businesses/${encodeURIComponent(params.slug)}/bookings`, {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ serviceId, staffId: slot.staffId, startAt: slot.startAt, ...form })
      });
      const data = await response.json();
      if (!response.ok) throw new Error(data.error ?? "ثبت نوبت ناموفق بود.");
      setBooking(data); setSlot(null);
    } catch (e) { setError(e instanceof Error ? e.message : "ثبت نوبت ناموفق بود."); }
  }

  if (loading) return <main dir="rtl" className="min-h-screen p-6">در حال بارگذاری...</main>;
  if (!profile) return <main dir="rtl" className="min-h-screen p-6"><div className="mx-auto max-w-xl rounded-2xl border p-6">{error || "کسب‌وکار پیدا نشد."}</div></main>;
  if (booking) return <main dir="rtl" className="min-h-screen bg-slate-50 p-6"><div className="mx-auto max-w-xl rounded-3xl bg-white p-8 shadow-sm"><h1 className="text-2xl font-bold">نوبت با موفقیت ثبت شد</h1><p className="mt-3">{booking.serviceTitle} — {booking.staffName}</p><p className="mt-1">{new Date(booking.startAt).toLocaleString("fa-IR")}</p><a className="mt-6 inline-block rounded-xl bg-slate-900 px-5 py-3 text-white" href={booking.publicDetailsUrl}>مشاهده جزئیات نوبت</a></div></main>;

  return <main dir="rtl" className="min-h-screen bg-slate-50 p-4 sm:p-8">
    <div className="mx-auto max-w-3xl space-y-5">
      <section className="rounded-3xl bg-white p-6 shadow-sm">
        <h1 className="text-3xl font-bold">{profile.name}</h1>
        {(profile.city || profile.address) && <p className="mt-2 text-slate-600">{[profile.city, profile.address].filter(Boolean).join("، ")}</p>}
        {profile.description && <p className="mt-3 text-slate-600">{profile.description}</p>}
        <p className="mt-2 text-sm text-slate-500">{profile.mobile}</p>
      </section>

      <section className="rounded-3xl bg-white p-6 shadow-sm space-y-5">
        <h2 className="text-xl font-bold">رزرو نوبت</h2>
        <label className="block"><span className="mb-1 block text-sm">خدمت</span><select className="w-full rounded-xl border p-3" value={serviceId} onChange={e => setServiceId(e.target.value)}>{profile.services.map(x => <option key={x.id} value={x.id}>{x.title} — {x.durationMinutes} دقیقه</option>)}</select></label>
        <label className="block"><span className="mb-1 block text-sm">متخصص (اختیاری)</span><select className="w-full rounded-xl border p-3" value={staffId} onChange={e => setStaffId(e.target.value)}><option value="">فرقی ندارد</option>{profile.staff.map(x => <option key={x.id} value={x.id}>{x.firstName} {x.lastName}</option>)}</select></label>
        <label className="block"><span className="mb-1 block text-sm">تاریخ</span><input className="w-full rounded-xl border p-3" type="date" value={date} onChange={e => setDate(e.target.value)} /></label>

        {date && <div><p className="mb-2 text-sm font-medium">زمان‌های آزاد</p><div className="grid grid-cols-3 gap-2 sm:grid-cols-5">{slots.map(x => <button key={`${x.staffId}-${x.startAt}`} type="button" onClick={() => setSlot(x)} className={`rounded-xl border p-2 text-sm ${slot?.startAt === x.startAt && slot?.staffId === x.staffId ? "border-slate-900 bg-slate-900 text-white" : "bg-white"}`}>{new Date(x.startAt).toLocaleTimeString("fa-IR", { hour: "2-digit", minute: "2-digit" })}</button>)}</div>{slots.length === 0 && <p className="text-sm text-slate-500">زمان آزادی پیدا نشد.</p>}</div>}

        {slot && <div className="rounded-2xl bg-slate-50 p-4 text-sm">زمان انتخاب‌شده: {new Date(slot.startAt).toLocaleString("fa-IR")} {selectedStaff ? ` — ${selectedStaff.firstName} ${selectedStaff.lastName}` : ""}</div>}

        <div className="grid gap-3 sm:grid-cols-2">
          <input className="rounded-xl border p-3" placeholder="نام" value={form.firstName} onChange={e => setForm({ ...form, firstName: e.target.value })} />
          <input className="rounded-xl border p-3" placeholder="نام خانوادگی" value={form.lastName} onChange={e => setForm({ ...form, lastName: e.target.value })} />
          <input className="rounded-xl border p-3" placeholder="شماره موبایل" inputMode="tel" value={form.mobile} onChange={e => setForm({ ...form, mobile: e.target.value })} />
          <input className="rounded-xl border p-3" placeholder="توضیحات (اختیاری)" value={form.note} onChange={e => setForm({ ...form, note: e.target.value })} />
        </div>
        {error && <p className="text-sm text-red-600">{error}</p>}
        <button type="button" disabled={!slot || !form.firstName || !form.mobile} onClick={submit} className="w-full rounded-xl bg-slate-900 p-3 font-medium text-white disabled:cursor-not-allowed disabled:opacity-40">ثبت نوبت</button>
      </section>
    </div>
  </main>;
}
