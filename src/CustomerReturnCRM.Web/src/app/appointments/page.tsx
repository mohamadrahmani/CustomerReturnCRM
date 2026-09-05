"use client";

import { useMemo, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import { useAuth } from "@/components/auth-provider";

type Customer = { id: string; firstName: string; lastName: string | null; mobile: string };
type Service = { id: string; title: string; defaultPrice: number; defaultDurationMinutes: number; isActive: boolean };
type Staff = { id: string; firstName: string; lastName: string; isActive: boolean };
type AppointmentService = { id: string; serviceId: string; staffId: string; serviceTitle: string; price: number; durationMinutes: number };
type Appointment = { id: string; customerId: string; startAt: string; endAt: string; status: number; note: string | null; services: AppointmentService[] };
type Page<T> = { items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number };

type Form = { customerId: string; serviceId: string; staffId: string; date: string; start: string; end: string; status: number; note: string };
const emptyForm = (date: string, start = "10:00"): Form => ({ customerId: "", serviceId: "", staffId: "", date, start, end: addMinutes(start, 60), status: 0, note: "" });

function addMinutes(time: string, minutes: number) { const [h, m] = time.split(":").map(Number); const total = h * 60 + m + minutes; const hh = Math.floor(total / 60) % 24; const mm = total % 60; return `${String(hh).padStart(2, "0")}:${String(mm).padStart(2, "0")}`; }
function localDateTime(date: string, time: string) { return `${date}T${time}:00`; }
function dateKey(value: Date) { return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, "0")}-${String(value.getDate()).padStart(2, "0")}`; }
function parseDate(value: string) { const [y, m, d] = value.split("-").map(Number); return new Date(y, m - 1, d); }
function shiftDate(value: string, days: number) { const d = parseDate(value); d.setDate(d.getDate() + days); return dateKey(d); }
function startOfWeek(value: string) { const d = parseDate(value); const offset = (d.getDay() + 1) % 7; d.setDate(d.getDate() - offset); return dateKey(d); }
function formatDate(value: string) { return new Intl.DateTimeFormat("fa-IR-u-ca-persian", { weekday: "short", day: "numeric", month: "short" }).format(parseDate(value)); }
function formatDateLong(value: string) { return new Intl.DateTimeFormat("fa-IR-u-ca-persian", { weekday: "long", day: "numeric", month: "long", year: "numeric" }).format(parseDate(value)); }
function timeOf(value: string) { return new Date(value).toLocaleTimeString("fa-IR", { hour: "2-digit", minute: "2-digit" }); }
function customerName(c?: Customer) { return c ? [c.firstName, c.lastName].filter(Boolean).join(" ") : "مشتری"; }
function staffName(s?: Staff) { return s ? [s.firstName, s.lastName].filter(Boolean).join(" ") : "کارکنان"; }
function statusLabel(status: number) { return status === 0 ? "در انتظار" : status === 1 ? "تأیید شده" : status === 2 ? "تکمیل شده" : status === 3 ? "لغو شده" : "عدم مراجعه"; }
function statusClass(status: number) { return status === 1 ? "border-indigo-200 bg-indigo-50 text-indigo-700" : status === 2 ? "border-emerald-200 bg-emerald-50 text-emerald-700" : status === 3 ? "border-slate-200 bg-slate-100 text-slate-500" : status === 4 ? "border-rose-200 bg-rose-50 text-rose-700" : "border-amber-200 bg-amber-50 text-amber-700"; }

function Field({ label, children }: { label: string; children: React.ReactNode }) { return <label className="block"><span className="mb-1.5 block text-xs font-bold text-slate-700">{label}</span>{children}</label>; }
function Loading() { return <div className="space-y-3">{Array.from({ length: 6 }).map((_, i) => <div key={i} className="h-16 animate-pulse rounded-2xl bg-white/70" />)}</div>; }

export default function AppointmentsPage() {
  const { auth, businessId, isReady } = useAuth();
  const activeBusinessId = businessId ?? auth?.businesses[0]?.id ?? null;
  const queryClient = useQueryClient();
  const today = dateKey(new Date());
  const [selectedDate, setSelectedDate] = useState(today);
  const [view, setView] = useState<"day" | "week">("week");
  const [staffFilter, setStaffFilter] = useState("all");
  const [modal, setModal] = useState<"create" | "detail" | "complete" | null>(null);
  const [selected, setSelected] = useState<Appointment | null>(null);
  const [form, setForm] = useState<Form>(emptyForm(today));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const rangeStart = view === "week" ? startOfWeek(selectedDate) : selectedDate;
  const rangeEnd = view === "week" ? shiftDate(rangeStart, 7) : shiftDate(selectedDate, 1);
  const dates = useMemo(() => Array.from({ length: view === "week" ? 7 : 1 }, (_, i) => shiftDate(rangeStart, i)), [rangeStart, view]);

  const appointments = useQuery({
    queryKey: ["appointments", activeBusinessId, rangeStart, rangeEnd],
    queryFn: () => apiFetch<Page<Appointment>>(`/api/businesses/${activeBusinessId}/appointments?from=${encodeURIComponent(localDateTime(rangeStart, "00:00"))}&to=${encodeURIComponent(localDateTime(rangeEnd, "00:00"))}&page=1&pageSize=200`),
    enabled: isReady && !!activeBusinessId,
    staleTime: 10_000,
  });
  const customers = useQuery({ queryKey: ["appointment-customers", activeBusinessId], queryFn: () => apiFetch<Page<Customer>>(`/api/businesses/${activeBusinessId}/customers?page=1&pageSize=100&isActive=true`), enabled: isReady && !!activeBusinessId });
  const services = useQuery({ queryKey: ["appointment-services", activeBusinessId], queryFn: () => apiFetch<Page<Service>>(`/api/businesses/${activeBusinessId}/services?page=1&pageSize=100&isActive=true`), enabled: isReady && !!activeBusinessId });
  const staff = useQuery({ queryKey: ["appointment-staff", activeBusinessId], queryFn: () => apiFetch<Page<Staff>>(`/api/businesses/${activeBusinessId}/staff?page=1&pageSize=100&isActive=true`), enabled: isReady && !!activeBusinessId });

  const customerMap = useMemo(() => new Map((customers.data?.items ?? []).map(x => [x.id, x])), [customers.data]);
  const serviceMap = useMemo(() => new Map((services.data?.items ?? []).map(x => [x.id, x])), [services.data]);
  const staffMap = useMemo(() => new Map((staff.data?.items ?? []).map(x => [x.id, x])), [staff.data]);
  const visibleAppointments = (appointments.data?.items ?? []).filter(a => staffFilter === "all" || a.services.some(s => s.staffId === staffFilter));

  if (!isReady || !activeBusinessId) return <main className="p-4"><Loading /></main>;

  function openCreate(date = selectedDate, start = "10:00") { setError(""); setSelected(null); setForm(emptyForm(date, start)); setModal("create"); }
  function openDetail(a: Appointment) { setError(""); setSelected(a); setModal("detail"); }
  function updateDate(delta: number) { setSelectedDate(view === "week" ? shiftDate(selectedDate, delta * 7) : shiftDate(selectedDate, delta)); }
  async function create() {
    if (!form.customerId || !form.serviceId || !form.staffId) { setError("مشتری، خدمت و کارکنان را انتخاب کنید."); return; }
    if (form.end <= form.start) { setError("زمان پایان باید بعد از زمان شروع باشد."); return; }
    setSaving(true); setError("");
    try {
      await apiFetch(`/api/businesses/${activeBusinessId}/appointments`, { method: "POST", body: JSON.stringify({ customerId: form.customerId, startAt: localDateTime(form.date, form.start), endAt: localDateTime(form.date, form.end), status: form.status, note: form.note.trim() || undefined, services: [{ serviceId: form.serviceId, staffId: form.staffId }] }) });
      setModal(null); await queryClient.invalidateQueries({ queryKey: ["appointments", activeBusinessId] });
    } catch (e) { setError(e instanceof Error ? e.message : "ثبت نوبت انجام نشد."); } finally { setSaving(false); }
  }
  async function cancel() {
    if (!selected) return; setSaving(true); setError("");
    try { await apiFetch(`/api/businesses/${activeBusinessId}/appointments/${selected.id}/cancel`, { method: "POST" }); setModal(null); await queryClient.invalidateQueries({ queryKey: ["appointments", activeBusinessId] }); }
    catch (e) { setError(e instanceof Error ? e.message : "لغو نوبت انجام نشد."); } finally { setSaving(false); }
  }
  async function confirmAppointment() {
    if (!selected) return; setSaving(true); setError("");
    try { await apiFetch(`/api/businesses/${activeBusinessId}/appointments/${selected.id}`, { method: "PUT", body: JSON.stringify({ customerId: selected.customerId, startAt: selected.startAt, endAt: selected.endAt, status: 1, note: selected.note, services: selected.services.map(s => ({ serviceId: s.serviceId, staffId: s.staffId })) }) }); setModal(null); await queryClient.invalidateQueries({ queryKey: ["appointments", activeBusinessId] }); }
    catch (e) { setError(e instanceof Error ? e.message : "تأیید نوبت انجام نشد."); } finally { setSaving(false); }
  }
  async function complete(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!selected) return; const data = new FormData(event.currentTarget); setSaving(true); setError("");
    try { await apiFetch(`/api/businesses/${activeBusinessId}/appointments/${selected.id}/complete`, { method: "POST", body: JSON.stringify({ visitAt: data.get("visitAt") ? new Date(String(data.get("visitAt"))).toISOString() : undefined, totalAmount: data.get("totalAmount") ? Number(data.get("totalAmount")) : undefined, note: String(data.get("note") || "").trim() || undefined }) }); setModal(null); await queryClient.invalidateQueries({ queryKey: ["appointments", activeBusinessId] }); queryClient.invalidateQueries({ queryKey: ["dashboard", activeBusinessId] }); }
    catch (e) { setError(e instanceof Error ? e.message : "ثبت مراجعه انجام نشد."); } finally { setSaving(false); }
  }

  return <main className="-mx-4 -mt-5 min-h-[calc(100vh-65px)] bg-gradient-to-b from-rose-50 via-pink-50/60 to-violet-50/40 px-4 pb-10 pt-5 sm:-mx-8 sm:-mt-8 sm:px-8 sm:pt-7">
    <div className="mx-auto max-w-7xl">
      <header className="mb-5 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between"><div><p className="text-[11px] font-bold text-pink-600">مدیریت زمان</p><h1 className="mt-1 text-2xl font-black tracking-tight text-slate-950 sm:text-3xl">نوبت‌ها</h1><p className="mt-1 text-xs leading-5 text-slate-500 sm:text-sm">تقویم روزانه و هفتگی نوبت‌ها را مدیریت کنید و نوبت‌های انجام‌شده را به مراجعه تبدیل کنید.</p></div><button onClick={() => openCreate()} className="crm-action self-start !min-h-10 !rounded-xl !px-4 !py-2.5 !text-xs bg-gradient-to-l from-pink-500 to-rose-500 shadow-md shadow-pink-200 sm:self-auto sm:!text-sm">＋ نوبت جدید</button></header>
      <section className="crm-card mb-4 p-3.5 sm:p-4"><div className="flex flex-col gap-3 xl:flex-row xl:items-center xl:justify-between"><div className="flex items-center gap-2"><button onClick={() => updateDate(-1)} className="crm-secondary-action !min-h-9 !px-3">‹</button><button onClick={() => setSelectedDate(today)} className="crm-secondary-action !min-h-9 !px-3 !text-xs">امروز</button><button onClick={() => updateDate(1)} className="crm-secondary-action !min-h-9 !px-3">›</button><div className="mr-2 min-w-0"><p className="truncate text-sm font-black text-slate-900">{view === "week" ? `${formatDate(rangeStart)} تا ${formatDate(shiftDate(rangeStart, 6))}` : formatDateLong(selectedDate)}</p></div></div><div className="flex flex-col gap-2 sm:flex-row"><select value={staffFilter} onChange={e => setStaffFilter(e.target.value)} className="crm-input !min-h-9 text-xs"><option value="all">همه کارکنان</option>{(staff.data?.items ?? []).map(s => <option key={s.id} value={s.id}>{staffName(s)}</option>)}</select><div className="grid grid-cols-2 rounded-xl bg-slate-100 p-1 sm:w-52"><button onClick={() => setView("day")} className={`rounded-lg py-2 text-xs font-bold ${view === "day" ? "bg-white text-pink-600 shadow-sm" : "text-slate-500"}`}>روزانه</button><button onClick={() => setView("week")} className={`rounded-lg py-2 text-xs font-bold ${view === "week" ? "bg-white text-pink-600 shadow-sm" : "text-slate-500"}`}>هفتگی</button></div></div></div></section>
      {appointments.isLoading || customers.isLoading || services.isLoading || staff.isLoading ? <Loading /> : appointments.isError ? <section className="crm-card p-8 text-center"><h2 className="font-black">بارگذاری نوبت‌ها انجام نشد</h2><button onClick={() => appointments.refetch()} className="crm-secondary-action mt-4">تلاش مجدد</button></section> : <section className="overflow-hidden rounded-3xl border border-white/80 bg-white/95 shadow-sm"><div className="overflow-x-auto"><div className="min-w-[760px]"><div className="grid grid-cols-[70px_repeat(7,minmax(110px,1fr))] border-b border-slate-100">{view === "week" ? <><div className="bg-slate-50/70" />{dates.map(d => <button key={d} onClick={() => { setSelectedDate(d); setView("day"); }} className={`border-r border-slate-100 px-2 py-3 text-center ${d === today ? "bg-pink-50" : "bg-slate-50/70"}`}><p className="text-[10px] font-bold text-slate-400">{formatDate(d).split("،")[0]}</p><p className={`mt-1 text-sm font-black ${d === today ? "text-pink-600" : "text-slate-800"}`}>{formatDate(d).replace(/^[^۰-۹]*،?\s*/, "")}</p></button>)} </> : <><div className="bg-slate-50/70" /><div className="col-span-7 bg-pink-50 px-4 py-3 text-center text-sm font-black text-pink-700">{formatDateLong(selectedDate)}</div></>}</div><div className="relative grid grid-cols-[70px_repeat(7,minmax(110px,1fr))]">{Array.from({ length: 25 }, (_, i) => { const minutes = 8 * 60 + i * 30; return <div key={i} className="contents"><div className="h-16 border-b border-l border-slate-100 bg-slate-50/40 px-2 pt-1 text-[10px] text-slate-400">{`${String(Math.floor(minutes / 60)).padStart(2, "0")}:${String(minutes % 60).padStart(2, "0")}`}</div>{dates.map(d => <button key={`${d}-${i}`} onClick={() => openCreate(d, `${String(Math.floor(minutes / 60)).padStart(2, "0")}:${String(minutes % 60).padStart(2, "0")}`)} className="h-16 border-b border-l border-slate-100 bg-white text-right transition hover:bg-pink-50/40" />)}</div>; })}{visibleAppointments.map(a => { const d = dateKey(new Date(a.startAt)); const dayIndex = dates.indexOf(d); if (dayIndex < 0) return null; const start = new Date(a.startAt); const end = new Date(a.endAt); const top = ((start.getHours() * 60 + start.getMinutes() - 8 * 60) / 30) * 64; const height = Math.max(48, ((end.getTime() - start.getTime()) / 60000 / 30) * 64); if (top < 0 || top > 1536) return null; const col = dayIndex + 1; return <button key={a.id} onClick={(e) => { e.stopPropagation(); openDetail(a); }} className={`absolute z-10 overflow-hidden rounded-xl border p-2 text-right shadow-sm transition hover:shadow-md ${statusClass(a.status)}`} style={{ top, height, right: `calc(${((7 - col) / 7) * 100}% + 70px)`, width: `calc(${100 / (view === "week" ? 7 : 1)}% - 70px)` }}><p className="truncate text-[10px] font-black">{customerName(customerMap.get(a.customerId))}</p><p className="mt-0.5 truncate text-[9px] font-medium opacity-80">{a.services.map(s => s.serviceTitle).join("، ")}</p><p className="mt-1 text-[9px] font-bold">{timeOf(a.startAt)}–{timeOf(a.endAt)}</p></button>; })}</div></div></div></section>}
      <p className="mt-3 text-[11px] text-slate-400">برای ثبت سریع نوبت، روی بازه زمانی موردنظر در تقویم کلیک کنید.</p>
    </div>

    {modal && <div className="fixed inset-0 z-50 flex items-end justify-center bg-slate-950/40 p-0 backdrop-blur-[3px] sm:items-center sm:p-4" onMouseDown={() => !saving && setModal(null)}><div role="dialog" aria-modal="true" className="max-h-[92vh] w-full max-w-lg overflow-y-auto rounded-t-[28px] bg-white shadow-2xl sm:rounded-[28px]" onMouseDown={e => e.stopPropagation()}>
      {modal === "create" && <><div className="border-b border-slate-100 bg-gradient-to-l from-pink-50 to-white px-5 py-4"><div className="flex items-start justify-between"><div><p className="text-[11px] font-bold text-pink-600">نوبت جدید</p><h2 className="mt-1 text-xl font-black">ثبت نوبت</h2><p className="mt-1 text-xs text-slate-500">اطلاعات زمان و خدمت را ثبت کنید.</p></div><button onClick={() => setModal(null)} className="h-10 w-10 rounded-xl border border-slate-200 text-slate-500">×</button></div></div><form onSubmit={e => { e.preventDefault(); create(); }} className="space-y-4 px-5 py-5"><div className="grid gap-4 sm:grid-cols-2"><Field label="مشتری"><select value={form.customerId} onChange={e => setForm({ ...form, customerId: e.target.value })} className="crm-input w-full"><option value="">انتخاب مشتری</option>{customers.data?.items.map(c => <option key={c.id} value={c.id}>{customerName(c)}</option>)}</select></Field><Field label="خدمت"><select value={form.serviceId} onChange={e => { const id = e.target.value; const s = serviceMap.get(id); setForm({ ...form, serviceId: id, end: s ? addMinutes(form.start, s.defaultDurationMinutes) : form.end }); }} className="crm-input w-full"><option value="">انتخاب خدمت</option>{services.data?.items.map(s => <option key={s.id} value={s.id}>{s.title} — {s.defaultDurationMinutes} دقیقه</option>)}</select></Field></div><Field label="کارکنان"><select value={form.staffId} onChange={e => setForm({ ...form, staffId: e.target.value })} className="crm-input w-full"><option value="">انتخاب کارکنان</option>{staff.data?.items.map(s => <option key={s.id} value={s.id}>{staffName(s)}</option>)}</select></Field><div className="grid gap-4 sm:grid-cols-3"><Field label="تاریخ"><input type="date" value={form.date} onChange={e => setForm({ ...form, date: e.target.value })} className="crm-input w-full" /></Field><Field label="شروع"><input type="time" value={form.start} onChange={e => { const s = serviceMap.get(form.serviceId); setForm({ ...form, start: e.target.value, end: s ? addMinutes(e.target.value, s.defaultDurationMinutes) : form.end }); }} className="crm-input w-full" /></Field><Field label="پایان"><input type="time" value={form.end} onChange={e => setForm({ ...form, end: e.target.value })} className="crm-input w-full" /></Field></div><Field label="وضعیت"><select value={form.status} onChange={e => setForm({ ...form, status: Number(e.target.value) })} className="crm-input w-full"><option value={0}>در انتظار</option><option value={1}>تأیید شده</option></select></Field><Field label="یادداشت"><textarea value={form.note} onChange={e => setForm({ ...form, note: e.target.value })} className="crm-input min-h-24 w-full resize-y" placeholder="یادداشت اختیاری..." /></Field>{error && <p className="rounded-xl bg-rose-50 px-3 py-2 text-xs font-bold text-rose-700">{error}</p>}<button disabled={saving} className="crm-action w-full !min-h-11">{saving ? "در حال ذخیره..." : "ثبت نوبت"}</button></form></>}
      {modal === "detail" && selected && <><div className="border-b border-slate-100 bg-gradient-to-l from-pink-50 to-white px-5 py-4"><div className="flex items-start justify-between"><div><p className="text-[11px] font-bold text-pink-600">جزئیات نوبت</p><h2 className="mt-1 text-xl font-black">{customerName(customerMap.get(selected.customerId))}</h2><p className="mt-1 text-xs text-slate-500">{formatDateLong(dateKey(new Date(selected.startAt)))} · {timeOf(selected.startAt)} تا {timeOf(selected.endAt)}</p></div><button onClick={() => setModal(null)} className="h-10 w-10 rounded-xl border border-slate-200 text-slate-500">×</button></div></div><div className="space-y-4 px-5 py-5"><div className="flex items-center justify-between"><span className={`rounded-full border px-3 py-1.5 text-[10px] font-black ${statusClass(selected.status)}`}>{statusLabel(selected.status)}</span><span className="text-xs text-slate-500">{selected.services.map(s => s.serviceTitle).join("، ")}</span></div><div className="rounded-2xl bg-slate-50 p-3 text-xs leading-6"><p><span className="text-slate-400">کارکنان:</span> {selected.services.map(s => staffName(staffMap.get(s.staffId))).join("، ")}</p>{selected.note && <p><span className="text-slate-400">یادداشت:</span> {selected.note}</p>}</div>{error && <p className="rounded-xl bg-rose-50 px-3 py-2 text-xs font-bold text-rose-700">{error}</p>}<div className="grid gap-2 sm:grid-cols-2">{selected.status === 0 && <button disabled={saving} onClick={confirmAppointment} className="crm-action">تأیید نوبت</button>}{selected.status !== 2 && selected.status !== 3 && <button disabled={saving} onClick={() => setModal("complete")} className="crm-action bg-emerald-600 hover:bg-emerald-700">ثبت مراجعه</button>}{selected.status !== 2 && selected.status !== 3 && <button disabled={saving} onClick={cancel} className="crm-secondary-action !text-rose-600">لغو نوبت</button>}<button onClick={() => setModal(null)} className="crm-secondary-action">بستن</button></div></div></>}
      {modal === "complete" && selected && <><div className="border-b border-slate-100 bg-gradient-to-l from-emerald-50 to-white px-5 py-4"><p className="text-[11px] font-bold text-emerald-600">تکمیل نوبت</p><h2 className="mt-1 text-xl font-black">ثبت مراجعه</h2><p className="mt-1 text-xs text-slate-500">مراجعه واقعی مشتری را ثبت کنید.</p></div><form onSubmit={complete} className="space-y-4 px-5 py-5"><Field label="زمان مراجعه"><input name="visitAt" type="datetime-local" defaultValue={selected.startAt.slice(0, 16)} className="crm-input w-full" /></Field><Field label="مبلغ نهایی"><input name="totalAmount" inputMode="decimal" className="crm-input w-full" placeholder="اختیاری" /></Field><Field label="یادداشت"><textarea name="note" className="crm-input min-h-24 w-full" placeholder="یادداشت مراجعه..." /></Field>{error && <p className="rounded-xl bg-rose-50 px-3 py-2 text-xs font-bold text-rose-700">{error}</p>}<div className="flex gap-2"><button disabled={saving} className="crm-action flex-1">{saving ? "در حال ثبت..." : "ثبت مراجعه"}</button><button type="button" disabled={saving} onClick={() => setModal("detail")} className="crm-secondary-action flex-1">انصراف</button></div></form></>}
    </div></div>}
  </main>;
}
