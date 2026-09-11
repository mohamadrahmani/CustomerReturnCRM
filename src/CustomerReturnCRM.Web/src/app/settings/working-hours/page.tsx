"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useAuth } from "@/components/auth-provider";
import { apiFetch, type PagedResult, type Staff } from "@/lib/api";

type WorkingHour = { id: string; dayOfWeek: number; startTime: string; endTime: string };
type DayConfig = { dayOfWeek: number; label: string; enabled: boolean; startTime: string; endTime: string };

const days: { dayOfWeek: number; label: string }[] = [
  { dayOfWeek: 6, label: "شنبه" },
  { dayOfWeek: 0, label: "یکشنبه" },
  { dayOfWeek: 1, label: "دوشنبه" },
  { dayOfWeek: 2, label: "سه‌شنبه" },
  { dayOfWeek: 3, label: "چهارشنبه" },
  { dayOfWeek: 4, label: "پنجشنبه" },
  { dayOfWeek: 5, label: "جمعه" },
];

const defaultSchedule = (hours: WorkingHour[] = [], fallbackEnabled = false): DayConfig[] => days.map(day => {
  const item = hours.find(x => x.dayOfWeek === day.dayOfWeek);
  return {
    dayOfWeek: day.dayOfWeek,
    label: day.label,
    enabled: item ? true : fallbackEnabled,
    startTime: item?.startTime?.slice(0, 5) ?? "07:00",
    endTime: item?.endTime?.slice(0, 5) ?? "18:00",
  };
});

function ScheduleEditor({
  items,
  disabled,
  onChange,
}: {
  items: DayConfig[];
  disabled?: boolean;
  onChange: (dayOfWeek: number, patch: Partial<DayConfig>) => void;
}) {
  return <div className="divide-y divide-slate-100">
    {items.map(item => <div key={item.dayOfWeek} className="flex flex-col gap-4 p-4 sm:flex-row sm:items-center sm:justify-between sm:p-5">
      <label className="flex items-center gap-3 sm:w-40">
        <input type="checkbox" disabled={disabled} checked={item.enabled} onChange={e => onChange(item.dayOfWeek, { enabled: e.target.checked })} className="h-5 w-5 rounded border-slate-300" />
        <span className={`font-bold ${item.enabled ? "text-slate-800" : "text-slate-400"}`}>{item.label}</span>
      </label>
      <div className={`grid flex-1 gap-3 sm:grid-cols-2 ${item.enabled && !disabled ? "" : "opacity-50"}`}>
        <label className="text-xs text-slate-500">شروع<input type="time" disabled={disabled || !item.enabled} value={item.startTime} onChange={e => onChange(item.dayOfWeek, { startTime: e.target.value })} className="mt-1 w-full rounded-xl border border-slate-200 bg-white p-3 text-sm text-slate-800 outline-none focus:border-slate-900" /></label>
        <label className="text-xs text-slate-500">پایان<input type="time" disabled={disabled || !item.enabled} value={item.endTime} onChange={e => onChange(item.dayOfWeek, { endTime: e.target.value })} className="mt-1 w-full rounded-xl border border-slate-200 bg-white p-3 text-sm text-slate-800 outline-none focus:border-slate-900" /></label>
      </div>
    </div>)}
  </div>;
}

function fullName(staff: Staff) { return `${staff.firstName} ${staff.lastName}`.trim(); }

export default function WorkingHoursPage() {
  const { auth, businessId, isReady } = useAuth();
  const activeBusinessId = businessId ?? auth?.businesses[0]?.id ?? null;
  const [businessItems, setBusinessItems] = useState<DayConfig[]>(defaultSchedule());
  const [staff, setStaff] = useState<Staff[]>([]);
  const [selectedStaffId, setSelectedStaffId] = useState("");
  const [staffItems, setStaffItems] = useState<DayConfig[]>(defaultSchedule());
  const [staffHasCustomSchedule, setStaffHasCustomSchedule] = useState(false);
  const [loading, setLoading] = useState(true);
  const [staffLoading, setStaffLoading] = useState(false);
  const [savingBusiness, setSavingBusiness] = useState(false);
  const [savingStaff, setSavingStaff] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const selectedStaff = useMemo(() => staff.find(x => x.id === selectedStaffId) ?? null, [staff, selectedStaffId]);

  useEffect(() => {
    if (!isReady || !activeBusinessId) return;
    setLoading(true); setError("");
    Promise.all([
      apiFetch<WorkingHour[]>(`/api/businesses/${activeBusinessId}/availability/working-hours`),
      apiFetch<PagedResult<Staff>>(`/api/businesses/${activeBusinessId}/staff?page=1&pageSize=100&isActive=true`),
    ])
      .then(([hours, staffResult]) => {
        setBusinessItems(defaultSchedule(hours));
        setStaff(staffResult.items);
        setSelectedStaffId(current => current || staffResult.items[0]?.id || "");
      })
      .catch(e => setError(e instanceof Error ? e.message : "دریافت اطلاعات ساعات کاری انجام نشد."))
      .finally(() => setLoading(false));
  }, [isReady, activeBusinessId]);

  useEffect(() => {
    if (!isReady || !activeBusinessId || !selectedStaffId) return;
    setStaffLoading(true); setError(""); setSuccess("");
    apiFetch<WorkingHour[]>(`/api/businesses/${activeBusinessId}/availability/staff/${selectedStaffId}/working-hours`)
      .then(hours => {
        const custom = hours.length > 0;
        setStaffHasCustomSchedule(custom);
        setStaffItems(defaultSchedule(hours, false));
      })
      .catch(e => setError(e instanceof Error ? e.message : "دریافت برنامه کاری کارمند انجام نشد."))
      .finally(() => setStaffLoading(false));
  }, [isReady, activeBusinessId, selectedStaffId]);

  function updateBusiness(dayOfWeek: number, patch: Partial<DayConfig>) {
    setBusinessItems(current => current.map(item => item.dayOfWeek === dayOfWeek ? { ...item, ...patch } : item));
    setSuccess("");
  }

  function updateStaff(dayOfWeek: number, patch: Partial<DayConfig>) {
    setStaffItems(current => current.map(item => item.dayOfWeek === dayOfWeek ? { ...item, ...patch } : item));
    setSuccess("");
  }

  function enableCustomSchedule(enabled: boolean) {
    setStaffHasCustomSchedule(enabled);
    if (enabled) {
      setStaffItems(defaultSchedule([], true));
    }
    setSuccess("");
  }

  async function saveBusiness() {
    if (!activeBusinessId) return;
    setSavingBusiness(true); setError(""); setSuccess("");
    try {
      const invalid = businessItems.find(x => x.enabled && x.endTime <= x.startTime);
      if (invalid) throw new Error(`ساعت پایان ${invalid.label} باید بعد از ساعت شروع باشد.`);
      await apiFetch<WorkingHour[]>(`/api/businesses/${activeBusinessId}/availability/working-hours`, {
        method: "PUT",
        body: JSON.stringify(businessItems.filter(x => x.enabled).map(x => ({ dayOfWeek: x.dayOfWeek, startTime: `${x.startTime}:00`, endTime: `${x.endTime}:00` }))),
      });
      setSuccess("ساعات کاری مجموعه با موفقیت ذخیره شد.");
    } catch (e) {
      setError(e instanceof Error ? e.message : "ذخیره ساعات کاری مجموعه انجام نشد.");
    } finally { setSavingBusiness(false); }
  }

  async function saveStaff() {
    if (!activeBusinessId || !selectedStaffId) return;
    setSavingStaff(true); setError(""); setSuccess("");
    try {
      const invalid = staffItems.find(x => x.enabled && x.endTime <= x.startTime);
      if (invalid) throw new Error(`ساعت پایان ${invalid.label} باید بعد از ساعت شروع باشد.`);
      const payload = staffHasCustomSchedule
        ? staffItems.filter(x => x.enabled).map(x => ({ dayOfWeek: x.dayOfWeek, startTime: `${x.startTime}:00`, endTime: `${x.endTime}:00` }))
        : [];
      await apiFetch<WorkingHour[]>(`/api/businesses/${activeBusinessId}/availability/staff/${selectedStaffId}/working-hours`, {
        method: "PUT",
        body: JSON.stringify(payload),
      });
      setSuccess(staffHasCustomSchedule ? `برنامه کاری ${selectedStaff ? fullName(selectedStaff) : "کارمند"} ذخیره شد.` : `برنامه اختصاصی ${selectedStaff ? fullName(selectedStaff) : "کارمند"} حذف شد و از ساعات کاری مجموعه استفاده می‌کند.`);
      if (!staffHasCustomSchedule) setStaffItems(defaultSchedule([]));
    } catch (e) {
      setError(e instanceof Error ? e.message : "ذخیره برنامه کاری کارمند انجام نشد.");
    } finally { setSavingStaff(false); }
  }

  if (!isReady || !activeBusinessId) return <main className="p-4"><div className="h-40 animate-pulse rounded-3xl bg-white" /></main>;

  return <main dir="rtl">
    <div className="mb-6">
      <Link href="/settings" className="text-sm text-slate-500">← تنظیمات</Link>
      <h1 className="mt-3 text-2xl font-extrabold text-slate-900">ساعات کاری</h1>
      <p className="mt-1 text-sm leading-6 text-slate-500">ساعات کاری مجموعه و برنامه اختصاصی هر کارمند را تعیین کنید. رزرو آنلاین فقط زمان‌هایی را نشان می‌دهد که با این برنامه و نوبت‌های موجود سازگار باشند.</p>
    </div>

    {error && <div role="alert" className="mb-4 rounded-2xl bg-rose-50 p-4 text-sm font-medium text-rose-700">{error}</div>}
    {success && <div role="status" className="mb-4 rounded-2xl bg-emerald-50 p-4 text-sm font-medium text-emerald-700">{success}</div>}

    {loading ? <div className="space-y-3"><div className="h-64 animate-pulse rounded-2xl bg-white" /><div className="h-64 animate-pulse rounded-2xl bg-white" /></div> : <div className="space-y-5">
      <section className="crm-card overflow-hidden">
        <header className="border-b border-slate-100 bg-slate-50 p-5">
          <h2 className="font-black text-slate-900">ساعات کاری مجموعه</h2>
          <p className="mt-1 text-xs leading-6 text-slate-500">برنامه پیش‌فرض کسب‌وکار است. اگر کارمند برنامه اختصاصی نداشته باشد، همین ساعات برای او ملاک رزرو خواهد بود.</p>
        </header>
        <ScheduleEditor items={businessItems} onChange={updateBusiness} />
        <div className="flex justify-end border-t border-slate-100 p-4">
          <button type="button" onClick={() => void saveBusiness()} disabled={savingBusiness} className="rounded-xl bg-slate-900 px-6 py-3 text-sm font-bold text-white disabled:cursor-not-allowed disabled:opacity-40">{savingBusiness ? "در حال ذخیره..." : "ذخیره ساعات مجموعه"}</button>
        </div>
      </section>

      <section className="crm-card overflow-hidden">
        <header className="border-b border-slate-100 bg-slate-50 p-5">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <h2 className="font-black text-slate-900">برنامه کاری کارکنان</h2>
              <p className="mt-1 text-xs leading-6 text-slate-500">برای هر کارمند می‌توانید برنامه متفاوتی تعریف کنید؛ در غیر این صورت ساعات کاری مجموعه استفاده می‌شود.</p>
            </div>
            <div className="sm:w-72">
              <label className="text-xs font-bold text-slate-500">کارمند</label>
              <select value={selectedStaffId} onChange={e => setSelectedStaffId(e.target.value)} className="mt-1 w-full rounded-xl border border-slate-200 bg-white p-3 text-sm font-bold text-slate-800 outline-none focus:border-slate-900">
                {staff.length === 0 ? <option value="">کارمند فعالی وجود ندارد</option> : staff.map(item => <option key={item.id} value={item.id}>{fullName(item)}</option>)}
              </select>
            </div>
          </div>
        </header>

        {!selectedStaffId ? <div className="p-8 text-center text-sm text-slate-500">ابتدا یک کارمند فعال تعریف کنید.</div> : staffLoading ? <div className="space-y-3 p-5">{days.map(day => <div key={day.dayOfWeek} className="h-16 animate-pulse rounded-2xl bg-slate-50" />)}</div> : <>
          <div className="border-b border-slate-100 p-5">
            <label className="flex cursor-pointer items-start gap-3 rounded-2xl border border-slate-200 bg-white p-4">
              <input type="checkbox" checked={staffHasCustomSchedule} onChange={e => enableCustomSchedule(e.target.checked)} className="mt-1 h-5 w-5 rounded border-slate-300" />
              <span>
                <span className="block text-sm font-black text-slate-800">برنامه اختصاصی برای {selectedStaff ? fullName(selectedStaff) : "این کارمند"}</span>
                <span className="mt-1 block text-xs leading-6 text-slate-500">{staffHasCustomSchedule ? "فعال است؛ ساعات زیر برای این کارمند اعمال می‌شود." : "غیرفعال است؛ این کارمند از ساعات کاری مجموعه استفاده می‌کند."}</span>
              </span>
            </label>
          </div>
          <ScheduleEditor items={staffItems} disabled={!staffHasCustomSchedule} onChange={updateStaff} />
          {staffHasCustomSchedule && <div className="border-t border-slate-100 bg-slate-50 p-4 text-xs leading-6 text-slate-500">پیش‌فرض برنامه اختصاصی: شنبه تا پنجشنبه از ۰۷:۰۰ تا ۱۸:۰۰. می‌توانید هر روز و ساعت را تغییر دهید.</div>}
          <div className="flex justify-end border-t border-slate-100 p-4">
            <button type="button" onClick={() => void saveStaff()} disabled={savingStaff} className="rounded-xl bg-slate-900 px-6 py-3 text-sm font-bold text-white disabled:cursor-not-allowed disabled:opacity-40">{savingStaff ? "در حال ذخیره..." : "ذخیره برنامه کارمند"}</button>
          </div>
        </>}
      </section>
    </div>}
  </main>;
}
