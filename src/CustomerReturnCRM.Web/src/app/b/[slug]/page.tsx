"use client";

import { use, useEffect, useMemo, useState } from "react";

const API = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5108";

type Service = { id: string; title: string; description: string | null; price: number; durationMinutes: number };
type Staff = { id: string; firstName: string; lastName: string };
type Profile = { businessId: string; name: string; mobile: string; address: string | null; city: string | null; description: string | null; services: Service[]; staff: Staff[] };
type Slot = { staffId: string; startAt: string; endAt: string };
type Booking = { publicDetailsUrl: string; startAt: string; endAt: string; serviceTitle: string; staffName: string };

function formatMoney(value: number) {
  return new Intl.NumberFormat("fa-IR").format(value);
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat("fa-IR", { dateStyle: "full", timeStyle: "short" }).format(new Date(value));
}

export default function PublicBookingPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = use(params);
  const [profile, setProfile] = useState<Profile | null>(null);
  const [serviceId, setServiceId] = useState("");
  const [staffId, setStaffId] = useState("");
  const [date, setDate] = useState("");
  const [slot, setSlot] = useState<Slot | null>(null);
  const [slots, setSlots] = useState<Slot[]>([]);
  const [form, setForm] = useState({ firstName: "", lastName: "", mobile: "", note: "" });
  const [loading, setLoading] = useState(true);
  const [availabilityLoading, setAvailabilityLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [booking, setBooking] = useState<Booking | null>(null);
  const [error, setError] = useState("");
  const [availabilityError, setAvailabilityError] = useState("");

  useEffect(() => {
    setLoading(true);
    setError("");
    fetch(`${API}/api/public/businesses/${encodeURIComponent(slug)}/profile`)
      .then(async r => { if (!r.ok) throw new Error("این صفحه رزرو در دسترس نیست."); return r.json(); })
      .then((data: Profile) => { setProfile(data); setServiceId(data.services[0]?.id ?? ""); })
      .catch(e => setError(e instanceof Error ? e.message : "دریافت اطلاعات کسب‌وکار ناموفق بود."))
      .finally(() => setLoading(false));
  }, [slug]);

  useEffect(() => {
    setSlots([]); setSlot(null); setAvailabilityError("");
    if (!profile || !serviceId || !date) return;
    setAvailabilityLoading(true);
    const qs = new URLSearchParams({ serviceId, date, slotIntervalMinutes: "30" });
    if (staffId) qs.set("staffId", staffId);
    fetch(`${API}/api/public/businesses/${profile.businessId}/availability?${qs}`)
      .then(async r => { if (!r.ok) throw new Error("دریافت زمان‌های آزاد ناموفق بود."); return r.json(); })
      .then(setSlots)
      .catch(e => setAvailabilityError(e instanceof Error ? e.message : "دریافت زمان‌های آزاد ناموفق بود."))
      .finally(() => setAvailabilityLoading(false));
  }, [profile, serviceId, staffId, date]);

  const selectedService = useMemo(() => profile?.services.find(x => x.id === serviceId), [profile, serviceId]);
  const selectedStaff = useMemo(() => profile?.staff.find(x => x.id === staffId), [profile, staffId]);

  async function submit() {
    if (!profile || !selectedService || !slot || submitting) return;
    setError("");
    setSubmitting(true);
    try {
      const response = await fetch(`${API}/api/public/businesses/${encodeURIComponent(slug)}/bookings`, {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ serviceId, staffId: slot.staffId, startAt: slot.startAt, ...form })
      });
      const data = await response.json();
      if (!response.ok) throw new Error(data.error ?? "ثبت نوبت ناموفق بود.");
      setBooking(data); setSlot(null);
    } catch (e) { setError(e instanceof Error ? e.message : "ثبت نوبت ناموفق بود."); }
    finally { setSubmitting(false); }
  }

  if (loading) return <main dir="rtl" className="min-h-screen bg-slate-50 p-4 sm:p-8"><div className="mx-auto max-w-3xl animate-pulse space-y-5"><div className="h-40 rounded-3xl bg-white" /><div className="h-[520px] rounded-3xl bg-white" /></div></main>;
  if (!profile) return <main dir="rtl" className="min-h-screen bg-slate-50 p-4 sm:p-8"><div className="mx-auto max-w-xl rounded-3xl border border-red-100 bg-white p-8 text-center shadow-sm"><div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-red-50 text-red-600">!</div><h1 className="mt-4 text-xl font-bold">صفحه رزرو در دسترس نیست</h1><p className="mt-2 text-sm text-slate-500">{error || "کسب‌وکار پیدا نشد."}</p></div></main>;
  if (!profile.services.length) return <main dir="rtl" className="min-h-screen bg-slate-50 p-4 sm:p-8"><div className="mx-auto max-w-xl rounded-3xl bg-white p-8 text-center shadow-sm"><h1 className="text-2xl font-bold">{profile.name}</h1><p className="mt-3 text-slate-500">در حال حاضر خدمتی برای رزرو آنلاین فعال نیست.</p></div></main>;
  if (booking) return <main dir="rtl" className="min-h-screen bg-slate-50 p-4 sm:p-8"><div className="mx-auto max-w-xl pt-6 sm:pt-12"><section className="rounded-3xl bg-white p-7 text-center shadow-sm sm:p-10"><div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-emerald-50 text-2xl text-emerald-600">✓</div><h1 className="mt-5 text-2xl font-bold text-slate-900">نوبت با موفقیت ثبت شد</h1><p className="mt-2 text-sm text-slate-500">جزئیات نوبت شما ثبت شده است.</p><div className="mt-6 rounded-2xl bg-slate-50 p-5 text-right text-sm"><p className="font-semibold">{booking.serviceTitle}</p><p className="mt-2 text-slate-600">{booking.staffName}</p><p className="mt-2 text-slate-600">{formatDateTime(booking.startAt)}</p></div><a className="mt-6 inline-flex w-full items-center justify-center rounded-xl bg-slate-900 px-5 py-3 font-medium text-white transition hover:bg-slate-800" href={booking.publicDetailsUrl}>مشاهده جزئیات نوبت</a></section></div></main>;

  return <main dir="rtl" className="min-h-screen bg-slate-50 p-4 sm:p-8">
    <div className="mx-auto max-w-4xl space-y-5">
      <section className="overflow-hidden rounded-3xl bg-slate-900 p-6 text-white shadow-sm sm:p-8">
        <div className="max-w-2xl"><p className="text-sm font-medium text-slate-300">رزرو آنلاین</p><h1 className="mt-2 text-3xl font-extrabold tracking-tight sm:text-4xl">{profile.name}</h1>{profile.description && <p className="mt-4 leading-7 text-slate-300">{profile.description}</p>}<div className="mt-5 flex flex-wrap gap-2 text-sm text-slate-300">{profile.city && <span className="rounded-full bg-white/10 px-3 py-1.5">{profile.city}</span>}{profile.address && <span className="rounded-full bg-white/10 px-3 py-1.5">{profile.address}</span>}{profile.mobile && <span className="rounded-full bg-white/10 px-3 py-1.5">{profile.mobile}</span>}</div></div>
      </section>

      <section className="rounded-3xl bg-white p-5 shadow-sm sm:p-7">
        <div className="mb-6"><h2 className="text-xl font-bold">انتخاب نوبت</h2><p className="mt-1 text-sm text-slate-500">خدمت، متخصص و زمان مناسب خود را انتخاب کنید.</p></div>
        <div className="grid gap-5 lg:grid-cols-3">
          <label className="block"><span className="mb-2 block text-sm font-medium">خدمت</span><select className="w-full rounded-xl border border-slate-200 bg-white p-3 outline-none transition focus:border-slate-900" value={serviceId} onChange={e => { setServiceId(e.target.value); setSlot(null); }}><option value="">انتخاب خدمت</option>{profile.services.map(x => <option key={x.id} value={x.id}>{x.title} — {x.durationMinutes} دقیقه{x.price > 0 ? ` — ${formatMoney(x.price)}` : ""}</option>)}</select></label>
          <label className="block"><span className="mb-2 block text-sm font-medium">متخصص <span className="font-normal text-slate-400">(اختیاری)</span></span><select className="w-full rounded-xl border border-slate-200 bg-white p-3 outline-none transition focus:border-slate-900" value={staffId} onChange={e => setStaffId(e.target.value)}><option value="">فرقی ندارد</option>{profile.staff.map(x => <option key={x.id} value={x.id}>{x.firstName} {x.lastName}</option>)}</select></label>
          <label className="block"><span className="mb-2 block text-sm font-medium">تاریخ</span><input className="w-full rounded-xl border border-slate-200 bg-white p-3 outline-none transition focus:border-slate-900" type="date" value={date} min={new Date().toISOString().slice(0, 10)} onChange={e => setDate(e.target.value)} /></label>
        </div>

        <div className="mt-7 border-t border-slate-100 pt-6"><div className="mb-3 flex items-center justify-between"><p className="text-sm font-semibold">زمان‌های آزاد</p>{date && <span className="text-xs text-slate-400">{new Date(`${date}T00:00:00`).toLocaleDateString("fa-IR", { dateStyle: "full" })}</span>}</div>{availabilityLoading ? <div className="grid grid-cols-3 gap-2 sm:grid-cols-5">{Array.from({ length: 10 }).map((_, i) => <div key={i} className="h-10 animate-pulse rounded-xl bg-slate-100" />)}</div> : availabilityError ? <div className="rounded-2xl bg-red-50 p-4 text-sm text-red-700">{availabilityError}</div> : !date ? <div className="rounded-2xl bg-slate-50 p-5 text-center text-sm text-slate-500">ابتدا تاریخ را انتخاب کنید.</div> : slots.length === 0 ? <div className="rounded-2xl bg-slate-50 p-5 text-center text-sm text-slate-500">برای این تاریخ زمان آزادی وجود ندارد.</div> : <div className="grid grid-cols-3 gap-2 sm:grid-cols-5">{slots.map(x => <button key={`${x.staffId}-${x.startAt}`} type="button" onClick={() => setSlot(x)} className={`min-h-11 rounded-xl border p-2 text-sm font-medium transition ${slot?.startAt === x.startAt && slot?.staffId === x.staffId ? "border-slate-900 bg-slate-900 text-white" : "border-slate-200 bg-white hover:border-slate-400"}`}>{new Date(x.startAt).toLocaleTimeString("fa-IR", { hour: "2-digit", minute: "2-digit" })}</button>)}</div>}</div>

        {slot && <div className="mt-5 rounded-2xl border border-slate-200 bg-slate-50 p-4 text-sm"><p className="font-semibold">زمان انتخاب‌شده</p><p className="mt-1 text-slate-600">{formatDateTime(slot.startAt)}{selectedStaff ? ` — ${selectedStaff.firstName} ${selectedStaff.lastName}` : ""}</p></div>}

        <div className="mt-7 border-t border-slate-100 pt-6"><h3 className="text-base font-bold">اطلاعات شما</h3><div className="mt-4 grid gap-3 sm:grid-cols-2"><label><span className="mb-1 block text-xs text-slate-500">نام *</span><input className="w-full rounded-xl border border-slate-200 p-3 outline-none focus:border-slate-900" placeholder="نام" value={form.firstName} onChange={e => setForm({ ...form, firstName: e.target.value })} /></label><label><span className="mb-1 block text-xs text-slate-500">نام خانوادگی</span><input className="w-full rounded-xl border border-slate-200 p-3 outline-none focus:border-slate-900" placeholder="نام خانوادگی" value={form.lastName} onChange={e => setForm({ ...form, lastName: e.target.value })} /></label><label><span className="mb-1 block text-xs text-slate-500">شماره موبایل *</span><input className="w-full rounded-xl border border-slate-200 p-3 outline-none focus:border-slate-900" placeholder="مثلاً 09121234567" inputMode="tel" value={form.mobile} onChange={e => setForm({ ...form, mobile: e.target.value })} /></label><label><span className="mb-1 block text-xs text-slate-500">توضیحات</span><input className="w-full rounded-xl border border-slate-200 p-3 outline-none focus:border-slate-900" placeholder="اختیاری" value={form.note} onChange={e => setForm({ ...form, note: e.target.value })} /></label></div></div>
        {error && <div className="mt-5 rounded-2xl bg-red-50 p-4 text-sm text-red-700" role="alert">{error}</div>}
        <button type="button" disabled={!slot || !form.firstName.trim() || !form.mobile.trim() || submitting} onClick={submit} className="mt-6 w-full rounded-xl bg-slate-900 p-3.5 font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-40">{submitting ? "در حال ثبت نوبت..." : "ثبت نوبت"}</button>
      </section>
    </div>
  </main>;
}
