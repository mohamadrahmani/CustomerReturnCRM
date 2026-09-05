"use client";

import { useEffect, useMemo, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";
import { useAuth } from "@/components/auth-provider";
import { gregorianToJalali, jalaliMonthDays, jalaliMonthNames, jalaliToIso, shiftJalaliMonth, todayJalali, type JalaliDate } from "@/lib/jalali";

type Customer = { id: string; firstName: string; lastName: string | null; mobile: string };
type Service = { id: string; title: string; defaultPrice: number; defaultDurationMinutes: number; isActive: boolean };
type Staff = { id: string; firstName: string; lastName: string; isActive: boolean };
type AppointmentService = { id: string; serviceId: string; staffId: string; serviceTitle: string; price: number; durationMinutes: number };
type Appointment = { id: string; customerId: string; startAt: string; endAt: string; status: number; note: string | null; services: AppointmentService[] };
type Page<T> = { items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number };
type Form = { customerId: string; serviceId: string; staffId: string; date: string; start: string; end: string; note: string };

function addMinutes(time: string, minutes: number) { const [h, m] = time.split(":").map(Number); const total = h * 60 + m + minutes; return `${String(Math.floor(total / 60) % 24).padStart(2, "0")}:${String(total % 60).padStart(2, "0")}`; }
function isoDate(d: Date) { return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`; }
function parseIso(value: string) { const [y, m, d] = value.split("-").map(Number); return new Date(y, m - 1, d); }
function shiftDate(value: string, days: number) { const d = parseIso(value); d.setDate(d.getDate() + days); return isoDate(d); }
function localDateTime(date: string, time: string) { return `${date}T${time}:00`; }
function customerName(c?: Customer) { return c ? [c.firstName, c.lastName].filter(Boolean).join(" ") : "مشتری"; }
function staffName(s?: Staff) { return s ? [s.firstName, s.lastName].filter(Boolean).join(" ") : "کارکنان"; }
function statusLabel(status: number) { return status === 0 ? "در انتظار" : status === 1 ? "تأیید شده" : status === 2 ? "تکمیل شده" : status === 3 ? "لغو شده" : "عدم مراجعه"; }
function statusClass(status: number) { return status === 1 ? "border-indigo-200 bg-indigo-50 text-indigo-700" : status === 2 ? "border-emerald-200 bg-emerald-50 text-emerald-700" : status === 3 ? "border-slate-200 bg-slate-100 text-slate-500" : status === 4 ? "border-rose-200 bg-rose-50 text-rose-700" : "border-amber-200 bg-amber-50 text-amber-700"; }
function formatPersianDate(iso: string) { return new Intl.DateTimeFormat("fa-IR-u-ca-persian", { weekday: "long", day: "numeric", month: "long", year: "numeric" }).format(parseIso(iso)); }
function timeOf(value: string) { return new Date(value).toLocaleTimeString("fa-IR", { hour: "2-digit", minute: "2-digit" }); }
function Field({ label, children }: { label: string; children: React.ReactNode }) { return <label className="block"><span className="mb-1.5 block text-xs font-bold text-slate-700">{label}</span>{children}</label>; }
function Loading() { return <div className="space-y-3">{Array.from({ length: 6 }).map((_, i) => <div key={i} className="h-16 animate-pulse rounded-2xl bg-white/70" />)}</div>; }

function JalaliDatePicker({ value, onChange }: { value: string; onChange: (value: string) => void }) {
  const [open, setOpen] = useState(false);
  const current = parseIso(value);
  const selected = gregorianToJalali(current.getFullYear(), current.getMonth() + 1, current.getDate());
  const [month, setMonth] = useState<JalaliDate>(selected);
  useEffect(() => setMonth(selected), [value]);
  const firstIso = jalaliToIso({ year: month.year, month: month.month, day: 1 });
  const firstDay = parseIso(firstIso).getDay();
  const days = jalaliMonthDays(month.year, month.month);
  const cells = Array.from({ length: firstDay + days }, (_, i) => i < firstDay ? null : i - firstDay + 1);
  const today = todayJalali();
  return <div className="relative">
    <button type="button" onClick={() => setOpen(x => !x)} className="crm-input flex w-full items-center justify-between text-right"><span className="font-semibold">{new Intl.DateTimeFormat("fa-IR-u-ca-persian", { day: "numeric", month: "long", year: "numeric" }).format(current)}</span><span className="text-slate-400">▦</span></button>
    {open && <><button aria-label="بستن تقویم" className="fixed inset-0 z-30 cursor-default" onClick={() => setOpen(false)} /><div className="absolute right-0 top-full z-40 mt-2 w-[320px] rounded-2xl border border-slate-200 bg-white p-3 shadow-2xl">
      <div className="mb-3 flex items-center justify-between"><button type="button" onClick={() => setMonth(m => shiftJalaliMonth(m, -1))} className="rounded-lg px-3 py-1.5 hover:bg-slate-100">‹</button><div className="text-sm font-black">{jalaliMonthNames[month.month - 1]} {new Intl.NumberFormat("fa-IR").format(month.year)}</div><button type="button" onClick={() => setMonth(m => shiftJalaliMonth(m, 1))} className="rounded-lg px-3 py-1.5 hover:bg-slate-100">›</button></div>
      <div className="mb-1 grid grid-cols-7 text-center text-[10px] font-bold text-slate-400">{["ی", "د", "س", "چ", "پ", "ج", "ش"].map(x => <span key={x}>{x}</span>)}</div>
      <div className="grid grid-cols-7 gap-1">{cells.map((day, i) => day === null ? <span key={`empty-${i}`} /> : <button type="button" key={day} onClick={() => { onChange(jalaliToIso({ year: month.year, month: month.month, day })); setOpen(false); }} className={`h-9 rounded-lg text-xs font-bold ${day === selected.day && month.month === selected.month && month.year === selected.year ? "bg-pink-500 text-white" : day === today.day && month.month === today.month && month.year === today.year ? "bg-pink-50 text-pink-600" : "text-slate-700 hover:bg-slate-100"}`}>{new Intl.NumberFormat("fa-IR").format(day)}</button>)}</div>
      <button type="button" onClick={() => { const t = todayJalali(); onChange(jalaliToIso(t)); setOpen(false); }} className="mt-3 w-full rounded-xl bg-slate-50 py-2 text-xs font-bold text-pink-600 hover:bg-pink-50">امروز</button>
    </div></>}
  </div>;
}

function CustomerPicker({ customers, value, onChange, search, setSearch }: { customers: Customer[]; value: string; onChange: (id: string) => void; search: string; setSearch: (value: string) => void }) {
  const [open, setOpen] = useState(false);
  const selected = customers.find(c => c.id === value);
  const normalized = search.trim().toLowerCase();
  const filtered = customers.filter(c => `${c.firstName} ${c.lastName ?? ""} ${c.mobile}`.toLowerCase().includes(normalized));
  return <div className="relative">
    <button type="button" onClick={() => setOpen(x => !x)} className="crm-input flex w-full items-center justify-between text-right"><span className={selected ? "text-slate-900" : "text-slate-400"}>{selected ? customerName(selected) : "انتخاب مشتری"}</span><span className="text-slate-400">⌄</span></button>
    {open && <><button className="fixed inset-0 z-30 cursor-default" aria-label="بستن انتخاب مشتری" onClick={() => setOpen(false)} /><div className="absolute right-0 top-full z-40 mt-2 w-full min-w-[320px] overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-2xl">
      <div className="border-b border-slate-100 p-2"><input autoFocus value={search} onChange={e => setSearch(e.target.value)} placeholder="جستجوی نام، نام خانوادگی یا موبایل..." className="crm-input w-full" /></div>
      <div className="max-h-64 overflow-y-auto p-1">{filtered.length === 0 ? <div className="p-5 text-center text-xs text-slate-400">مشتری‌ای پیدا نشد.</div> : filtered.map(c => <button type="button" key={c.id} onClick={() => { onChange(c.id); setSearch(""); setOpen(false); }} className={`flex w-full items-center justify-between rounded-xl px-3 py-2.5 text-right hover:bg-pink-50 ${c.id === value ? "bg-pink-50" : ""}`}><span><span className="block text-sm font-bold text-slate-800">{customerName(c)}</span><span className="mt-0.5 block text-[11px] text-slate-400" dir="ltr">{c.mobile}</span></span><span className="text-[10px] text-slate-300">›</span></button>)}</div>
    </div></>}
  </div>;
}

export default function AppointmentsPage() {
  const { auth, businessId, isReady } = useAuth();
  const activeBusinessId = businessId ?? auth?.businesses[0]?.id ?? null;
  const queryClient = useQueryClient();
  const today = isoDate(new Date());
  const [selectedDate, setSelectedDate] = useState(today);
  const [view, setView] = useState<"day" | "week">("week");
  const [staffFilter, setStaffFilter] = useState("all");
  const [modal, setModal] = useState<"create" | "detail" | null>(null);
  const [selected, setSelected] = useState<Appointment | null>(null);
  const [form, setForm] = useState<Form>({ customerId: "", serviceId: "", staffId: "", date: today, start: "10:00", end: "11:00", note: "" });
  const [customerSearch, setCustomerSearch] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const selectedDay = parseIso(selectedDate);
  const weekStart = new Date(selectedDay);
  weekStart.setDate(weekStart.getDate() - weekStart.getDay());
  const rangeStart = view === "week" ? isoDate(weekStart) : selectedDate;
  const rangeEnd = shiftDate(rangeStart, view === "week" ? 7 : 1);
  const dates = Array.from({ length: view === "week" ? 7 : 1 }, (_, i) => shiftDate(rangeStart, i));
  const appointments = useQuery({ queryKey: ["appointments", activeBusinessId, rangeStart, rangeEnd], queryFn: () => apiFetch<Page<Appointment>>(`/api/businesses/${activeBusinessId}/appointments?from=${encodeURIComponent(localDateTime(rangeStart, "00:00"))}&to=${encodeURIComponent(localDateTime(rangeEnd, "00:00"))}&page=1&pageSize=200`), enabled: isReady && !!activeBusinessId, staleTime: 10000 });
  const customers = useQuery({ queryKey: ["appointment-customers", activeBusinessId], queryFn: () => apiFetch<Page<Customer>>(`/api/businesses/${activeBusinessId}/customers?page=1&pageSize=100&isActive=true`), enabled: isReady && !!activeBusinessId });
  const services = useQuery({ queryKey: ["appointment-services", activeBusinessId], queryFn: () => apiFetch<Page<Service>>(`/api/businesses/${activeBusinessId}/services?page=1&pageSize=100&isActive=true`), enabled: isReady && !!activeBusinessId });
  const staff = useQuery({ queryKey: ["appointment-staff", activeBusinessId], queryFn: () => apiFetch<Page<Staff>>(`/api/businesses/${activeBusinessId}/staff?page=1&pageSize=100&isActive=true`), enabled: isReady && !!activeBusinessId });
  const customerMap = useMemo(() => new Map((customers.data?.items ?? []).map(x => [x.id, x])), [customers.data]);
  const staffMap = useMemo(() => new Map((staff.data?.items ?? []).map(x => [x.id, x])), [staff.data]);
  const visibleAppointments = (appointments.data?.items ?? []).filter(a => staffFilter === "all" || a.services.some(s => s.staffId === staffFilter));

  if (!isReady || !activeBusinessId) return <main className="p-4"><Loading /></main>;

  function openCreate(date = selectedDate, start = "10:00") { setError(""); setSelected(null); setCustomerSearch(""); setForm({ customerId: "", serviceId: "", staffId: "", date, start, end: addMinutes(start, 60), note: "" }); setModal("create"); }
  function openDetail(a: Appointment) { setError(""); setSelected(a); setModal("detail"); }
  function updateDate(delta: number) { setSelectedDate(shiftDate(selectedDate, view === "week" ? delta * 7 : delta)); }
  async function create() {
    const service = services.data?.items.find(x => x.id === form.serviceId);
    if (!form.customerId || !form.serviceId || !form.staffId) { setError("مشتری، خدمت و کارکنان را انتخاب کنید."); return; }
    if (form.end <= form.start) { setError("زمان پایان باید بعد از زمان شروع باشد."); return; }
    if (!service) { setError("خدمت انتخاب‌شده معتبر نیست."); return; }
    setSaving(true); setError("");
    try { await apiFetch(`/api/businesses/${activeBusinessId}/appointments`, { method: "POST", body: JSON.stringify({ customerId: form.customerId, startAt: localDateTime(form.date, form.start), endAt: localDateTime(form.date, form.end), note: form.note.trim() || undefined, services: [{ serviceId: service.id, staffId: form.staffId, price: service.defaultPrice, durationMinutes: service.defaultDurationMinutes }] }) }); setModal(null); await queryClient.invalidateQueries({ queryKey: ["appointments", activeBusinessId] }); }
    catch (e) { setError(e instanceof Error ? e.message : "ثبت نوبت انجام نشد."); } finally { setSaving(false); }
  }
  async function cancel() { if (!selected) return; setSaving(true); setError(""); try { await apiFetch(`/api/businesses/${activeBusinessId}/appointments/${selected.id}/cancel`, { method: "POST" }); setModal(null); await queryClient.invalidateQueries({ queryKey: ["appointments", activeBusinessId] }); } catch (e) { setError(e instanceof Error ? e.message : "لغو نوبت انجام نشد."); } finally { setSaving(false); } }
  async function confirmAppointment() { if (!selected) return; setSaving(true); setError(""); try { await apiFetch(`/api/businesses/${activeBusinessId}/appointments/${selected.id}/confirm`, { method: "POST" }); setModal(null); await queryClient.invalidateQueries({ queryKey: ["appointments", activeBusinessId] }); } catch (e) { setError(e instanceof Error ? e.message : "تأیید نوبت انجام نشد."); } finally { setSaving(false); } }

  return <main className="-mx-4 -mt-5 min-h-[calc(100vh-65px)] bg-gradient-to-b from-rose-50 via-pink-50/60 to-violet-50/40 px-4 pb-10 pt-5 sm:-mx-8 sm:-mt-8 sm:px-8 sm:pt-7">
    <div className="mx-auto max-w-7xl">
      <header className="mb-5 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between"><div><p className="text-[11px] font-bold text-pink-600">مدیریت زمان</p><h1 className="mt-1 text-2xl font-black text-slate-950 sm:text-3xl">نوبت‌ها</h1><p className="mt-1 text-xs text-slate-500 sm:text-sm">تقویم روزانه و هفتگی نوبت‌ها را مدیریت کنید.</p></div><button onClick={() => openCreate()} className="crm-action self-start !min-h-10 !rounded-xl !px-4 !py-2.5 !text-xs bg-gradient-to-l from-pink-500 to-rose-500 shadow-md shadow-pink-200 sm:self-auto sm:!text-sm">＋ نوبت جدید</button></header>
      <section className="crm-card mb-4 p-3.5 sm:p-4"><div className="flex flex-col gap-3 xl:flex-row xl:items-center xl:justify-between"><div className="flex items-center gap-2"><button onClick={() => updateDate(-1)} className="crm-secondary-action !min-h-9 !px-3">‹</button><button onClick={() => setSelectedDate(today)} className="crm-secondary-action !min-h-9 !px-3 !text-xs">امروز</button><button onClick={() => updateDate(1)} className="crm-secondary-action !min-h-9 !px-3">›</button><div className="mr-2 text-sm font-black text-slate-900">{formatPersianDate(selectedDate)}</div></div><div className="flex flex-col gap-2 sm:flex-row"><select value={staffFilter} onChange={e => setStaffFilter(e.target.value)} className="crm-input !min-h-9 text-xs"><option value="all">همه کارکنان</option>{(staff.data?.items ?? []).map(s => <option key={s.id} value={s.id}>{staffName(s)}</option>)}</select><div className="grid grid-cols-2 rounded-xl bg-slate-100 p-1 sm:w-52"><button onClick={() => setView("day")} className={`rounded-lg py-2 text-xs font-bold ${view === "day" ? "bg-white text-pink-600 shadow-sm" : "text-slate-500"}`}>روزانه</button><button onClick={() => setView("week")} className={`rounded-lg py-2 text-xs font-bold ${view === "week" ? "bg-white text-pink-600 shadow-sm" : "text-slate-500"}`}>هفتگی</button></div></div></div></section>
      {appointments.isLoading || customers.isLoading || services.isLoading || staff.isLoading ? <Loading /> : <section className="overflow-hidden rounded-3xl border border-white/80 bg-white/95 shadow-sm"><div className="overflow-x-auto"><div className="min-w-[760px]"><div className={`grid ${view === "week" ? "grid-cols-[70px_repeat(7,minmax(110px,1fr))]" : "grid-cols-[70px_minmax(500px,1fr)]"} border-b border-slate-100`}><div className="bg-slate-50/70" />{dates.map(d => <button key={d} onClick={() => { setSelectedDate(d); setView("day"); }} className={`border-r border-slate-100 px-2 py-3 text-center ${d === today ? "bg-pink-50" : "bg-slate-50/70"}`}><p className="text-[10px] font-bold text-slate-400">{new Intl.DateTimeFormat("fa-IR-u-ca-persian", { weekday: "short" }).format(parseIso(d))}</p><p className={`mt-1 text-sm font-black ${d === today ? "text-pink-600" : "text-slate-800"}`}>{new Intl.DateTimeFormat("fa-IR-u-ca-persian", { day: "numeric", month: "short" }).format(parseIso(d))}</p></button>)}</div><div className="grid grid-cols-[70px_1fr]"><div className="border-l border-slate-100 bg-slate-50/40">{[9,10,11,12,13,14,15,16,17,18,19,20].map(h => <div key={h} className="h-20 border-b border-slate-100 px-2 pt-2 text-[10px] font-bold text-slate-400">{new Intl.NumberFormat("fa-IR").format(h)}:۰۰</div>)}</div><div className={`grid ${view === "week" ? "grid-cols-7" : "grid-cols-1"}`}>{dates.map(d => <div key={d} className="relative border-r border-slate-100">{[9,10,11,12,13,14,15,16,17,18,19,20].map(h => <button key={h} onClick={() => openCreate(d, `${String(h).padStart(2, "0")}:00`)} className="block h-20 w-full border-b border-slate-100 text-right hover:bg-pink-50/50" />)}{visibleAppointments.filter(a => isoDate(new Date(a.startAt)) === d).map(a => { const start = new Date(a.startAt); const end = new Date(a.endAt); const top = Math.max(0, (start.getHours() - 9) * 80 + (start.getMinutes() / 60) * 80); const height = Math.max(52, ((end.getTime() - start.getTime()) / 3600000) * 80); return <button key={a.id} onClick={() => openDetail(a)} style={{ top, height }} className={`absolute right-1 left-1 overflow-hidden rounded-xl border p-2 text-right shadow-sm ${statusClass(a.status)}`}><p className="text-[10px] font-black">{timeOf(a.startAt)} · {customerName(customerMap.get(a.customerId))}</p><p className="mt-1 truncate text-[10px]">{a.services.map(s => s.serviceTitle).join("، ")}</p></button>; })}</div>)}</div></div></div></section>}
    </div>
    {modal === "create" && <div className="fixed inset-0 z-50 flex items-end justify-center bg-slate-950/40 p-0 sm:items-center sm:p-5"><div className="max-h-[92vh] w-full max-w-xl overflow-y-auto rounded-t-3xl bg-white p-5 shadow-2xl sm:rounded-3xl sm:p-6"><div className="mb-5 flex items-center justify-between"><div><h2 className="text-lg font-black text-slate-950">ثبت نوبت جدید</h2><p className="mt-1 text-xs text-slate-400">مشتری را با نام یا موبایل پیدا کنید و تاریخ را شمسی انتخاب کنید.</p></div><button onClick={() => setModal(null)} className="rounded-xl bg-slate-100 px-3 py-2 text-slate-500">×</button></div><div className="space-y-4"><Field label="مشتری"><CustomerPicker customers={customers.data?.items ?? []} value={form.customerId} onChange={id => setForm(f => ({ ...f, customerId: id }))} search={customerSearch} setSearch={setCustomerSearch} /></Field><div className="grid gap-4 sm:grid-cols-2"><Field label="تاریخ"><JalaliDatePicker value={form.date} onChange={date => setForm(f => ({ ...f, date }))} /></Field><Field label="خدمت"><select value={form.serviceId} onChange={e => { const s = services.data?.items.find(x => x.id === e.target.value); setForm(f => ({ ...f, serviceId: e.target.value, end: s ? addMinutes(f.start, s.defaultDurationMinutes) : f.end })); }} className="crm-input w-full"><option value="">انتخاب خدمت</option>{(services.data?.items ?? []).map(s => <option key={s.id} value={s.id}>{s.title} · {s.defaultDurationMinutes} دقیقه</option>)}</select></Field><Field label="کارکنان"><select value={form.staffId} onChange={e => setForm(f => ({ ...f, staffId: e.target.value }))} className="crm-input w-full"><option value="">انتخاب کارکنان</option>{(staff.data?.items ?? []).map(s => <option key={s.id} value={s.id}>{staffName(s)}</option>)}</select></Field><Field label="ساعت شروع"><input type="time" value={form.start} onChange={e => setForm(f => ({ ...f, start: e.target.value, end: f.serviceId ? addMinutes(e.target.value, services.data?.items.find(s => s.id === f.serviceId)?.defaultDurationMinutes ?? 60) : f.end }))} className="crm-input w-full" dir="ltr" /></Field><Field label="ساعت پایان"><input type="time" value={form.end} onChange={e => setForm(f => ({ ...f, end: e.target.value }))} className="crm-input w-full" dir="ltr" /></Field></div><Field label="توضیحات"><textarea value={form.note} onChange={e => setForm(f => ({ ...f, note: e.target.value }))} rows={3} className="crm-input w-full resize-none" placeholder="یادداشت اختیاری..." /></Field>{error && <div className="rounded-xl border border-rose-200 bg-rose-50 p-3 text-xs font-bold text-rose-700">{error}</div>}<div className="flex gap-2 pt-2"><button disabled={saving} onClick={create} className="crm-action flex-1 !min-h-11">{saving ? "در حال ثبت..." : "ثبت نوبت"}</button><button disabled={saving} onClick={() => setModal(null)} className="crm-secondary-action !min-h-11">انصراف</button></div></div></div></div>}
    {modal === "detail" && selected && <div className="fixed inset-0 z-50 flex items-end justify-center bg-slate-950/40 p-0 sm:items-center sm:p-5"><div className="w-full max-w-md rounded-t-3xl bg-white p-6 shadow-2xl sm:rounded-3xl"><div className="flex items-start justify-between"><div><p className="text-xs text-slate-400">جزئیات نوبت</p><h2 className="mt-1 text-xl font-black">{customerName(customerMap.get(selected.customerId))}</h2></div><button onClick={() => setModal(null)} className="rounded-xl bg-slate-100 px-3 py-2">×</button></div><div className="mt-5 space-y-3 text-sm"><div className="flex justify-between"><span className="text-slate-400">تاریخ</span><b>{formatPersianDate(isoDate(new Date(selected.startAt)))}</b></div><div className="flex justify-between"><span className="text-slate-400">زمان</span><b dir="ltr">{timeOf(selected.startAt)} - {timeOf(selected.endAt)}</b></div><div className="flex justify-between"><span className="text-slate-400">خدمت</span><b>{selected.services.map(s => s.serviceTitle).join("، ")}</b></div><div className="flex justify-between"><span className="text-slate-400">کارکنان</span><b>{selected.services.map(s => staffName(staffMap.get(s.staffId))).join("، ")}</b></div><div><span className={`inline-flex rounded-full border px-2.5 py-1 text-[11px] font-bold ${statusClass(selected.status)}`}>{statusLabel(selected.status)}</span></div></div>{error && <div className="mt-4 rounded-xl bg-rose-50 p-3 text-xs font-bold text-rose-700">{error}</div>}<div className="mt-6 grid grid-cols-2 gap-2">{selected.status === 0 && <button disabled={saving} onClick={confirmAppointment} className="crm-action">تأیید نوبت</button>}{selected.status !== 3 && selected.status !== 2 && <button disabled={saving} onClick={cancel} className="crm-secondary-action !border-rose-200 !text-rose-600">لغو نوبت</button>}</div></div></div>}
  </main>;
}
