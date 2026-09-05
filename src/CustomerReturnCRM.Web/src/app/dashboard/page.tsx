"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { getDashboard } from "@/lib/api";
import { useAuth } from "@/components/auth-provider";

function formatTime(value: string) {
  return new Intl.DateTimeFormat("fa-IR", { hour: "2-digit", minute: "2-digit" }).format(new Date(value));
}

function formatRelativeDate(value: string | null) {
  if (!value) return "—";
  const date = new Date(value);
  const diff = Math.max(0, Math.floor((Date.now() - date.getTime()) / 86_400_000));
  if (diff === 0) return "امروز";
  if (diff === 1) return "دیروز";
  return `${diff.toLocaleString("fa-IR")} روز پیش`;
}

function formatDate(value: string | null) {
  if (!value) return "—";
  return new Intl.DateTimeFormat("fa-IR", { month: "short", day: "numeric" }).format(new Date(value));
}

function appointmentStatus(status: number) {
  return ["در انتظار", "تأیید شده", "تکمیل شده", "لغو شده", "عدم حضور"][status] ?? "نامشخص";
}

function daysLabel(item: { daysFromExpectedReturn: number | null }) {
  if (item.daysFromExpectedReturn == null) return "بدون تاریخ بازگشت";
  if (item.daysFromExpectedReturn > 0) return `${item.daysFromExpectedReturn.toLocaleString("fa-IR")} روز گذشته`;
  if (item.daysFromExpectedReturn === 0) return "امروز";
  return `${Math.abs(item.daysFromExpectedReturn).toLocaleString("fa-IR")} روز مانده`;
}

function actionLabel(type: string) {
  if (type === "Overdue") return "عقب‌افتاده";
  if (type === "AtRisk") return "در معرض ریسک";
  return "نزدیک موعد";
}

function actionBadgeClass(type: string) {
  if (type === "Overdue") return "bg-rose-100 text-rose-700";
  if (type === "AtRisk") return "bg-amber-100 text-amber-700";
  return "bg-violet-100 text-violet-700";
}

function avatarInitial(name: string | null | undefined) {
  const value = (name ?? "").trim();
  if (!value) return "؟";
  const parts = value.split(/\s+/).filter(Boolean);
  return parts.length > 1 ? `${parts[0][0]}${parts[1][0]}` : value[0];
}

function avatarTone(name: string | null | undefined) {
  const value = name ?? "";
  const index = [...value].reduce((sum, char) => sum + char.charCodeAt(0), 0) % 4;
  return [
    "bg-rose-100 text-rose-700",
    "bg-violet-100 text-violet-700",
    "bg-amber-100 text-amber-700",
    "bg-emerald-100 text-emerald-700",
  ][index];
}

function Avatar({ name, size = "md" }: { name: string | null | undefined; size?: "sm" | "md" }) {
  return (
    <span
      className={`${size === "sm" ? "h-9 w-9 text-[11px]" : "h-11 w-11 text-xs"} ${avatarTone(name)} flex shrink-0 items-center justify-center rounded-full font-black ring-2 ring-white`}
      aria-hidden="true"
    >
      {avatarInitial(name)}
    </span>
  );
}

function Icon({ name, className = "h-5 w-5" }: { name: "users" | "calendar" | "clock" | "alert" | "calendarPlus"; className?: string }) {
  const common = { className, viewBox: "0 0 24 24", fill: "none", stroke: "currentColor", strokeWidth: 1.8, strokeLinecap: "round" as const, strokeLinejoin: "round" as const, "aria-hidden": true };
  if (name === "users") return <svg {...common}><path d="M16 20c0-2.8-2.2-5-5-5s-5 2.2-5 5"/><circle cx="11" cy="8" r="3"/><path d="M17 11a3 3 0 1 0-1.2-5.8M17 15c2.2.2 3.7 1.7 4 4"/></svg>;
  if (name === "calendar") return <svg {...common}><rect x="3" y="4.5" width="18" height="16" rx="2"/><path d="M7 2.5v4M17 2.5v4M3 9h18M8 13h3M8 17h3M14 13h2"/></svg>;
  if (name === "clock") return <svg {...common}><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></svg>;
  if (name === "calendarPlus") return <svg {...common}><rect x="3" y="4.5" width="18" height="16" rx="2"/><path d="M7 2.5v4M17 2.5v4M3 9h18M12 12v5M9.5 14.5h5"/></svg>;
  return <svg {...common}><path d="M12 8v5"/><path d="M12 17h.01"/><path d="M10.3 3.8 2.8 17a2 2 0 0 0 1.8 3h14.8a2 2 0 0 0 1.8-3l-7.5-13.2a2 2 0 0 0-3.4 0Z"/></svg>;
}

export default function DashboardPage() {
  const { auth, businessId, isReady } = useAuth();
  const business = auth?.businesses.find((item) => item.id === businessId) ?? auth?.businesses[0];
  const activeBusinessId = businessId ?? business?.id ?? null;
  const dashboard = useQuery({ queryKey: ["dashboard", activeBusinessId], queryFn: () => getDashboard(activeBusinessId!), enabled: isReady && !!activeBusinessId, staleTime: 30_000 });

  if (!isReady) return <DashboardLoading />;
  if (!activeBusinessId) return <main><section className="crm-card p-8 text-center"><h1 className="text-xl font-bold text-slate-900">کسب‌وکاری انتخاب نشده است</h1><p className="mt-2 crm-muted">ابتدا اطلاعات کسب‌وکار را تکمیل کنید.</p><Link href="/setup" className="crm-action mt-5">تکمیل راه‌اندازی</Link></section></main>;
  if (dashboard.isLoading) return <DashboardLoading />;
  if (dashboard.isError) return <main><section className="crm-card border-rose-200 p-8 text-center"><h1 className="text-xl font-bold text-slate-900">بارگذاری داشبورد انجام نشد</h1><p className="mt-2 crm-muted">اتصال به اطلاعات کسب‌وکار با خطا مواجه شد.</p><button onClick={() => dashboard.refetch()} className="crm-secondary-action mt-5">تلاش مجدد</button></section></main>;

  const data = dashboard.data!;
  const followUpCount = data.dueSoon.length + data.overdue.length + data.atRisk.length;
  const stats = [
    ["مشتریان فعال", data.activeCustomerCount, "مشاهده مشتریان", "/customers", "users", "bg-rose-50 border-rose-100", "bg-rose-100 text-rose-600"],
    ["نوبت‌های امروز", data.todayAppointments.length, "مشاهده نوبت‌ها", "/appointments", "calendar", "bg-violet-50 border-violet-100", "bg-violet-100 text-violet-600"],
    ["پیگیری‌های باز", data.pendingReminders.length, "مشاهده پیگیری‌ها", "/follow-ups", "clock", "bg-emerald-50 border-emerald-100", "bg-emerald-100 text-emerald-600"],
    ["نیازمند اقدام", followUpCount, "مشاهده موارد", "/return-analysis", "alert", "bg-amber-50 border-amber-100", "bg-amber-100 text-amber-600"],
  ] as const;

  return (
    <main className="-mx-4 -mt-5 min-h-[calc(100vh-65px)] bg-gradient-to-b from-rose-50 via-pink-50/60 to-violet-50/40 px-4 pb-10 pt-4 sm:-mx-8 sm:-mt-8 sm:px-8 sm:pt-6">
      <div className="mx-auto max-w-7xl">
        <header className="mb-4 flex flex-col gap-3 sm:mb-5 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <p className="text-[11px] font-bold text-pink-600">نمای کلی</p>
            <h1 className="mt-0.5 text-[25px] font-black tracking-tight text-slate-950 sm:text-3xl">داشبورد</h1>
            <p className="mt-1 text-xs leading-5 text-slate-600 sm:text-sm">وضعیت امروز {business?.name ?? "کسب‌وکار شما"} را در یک نگاه ببینید.</p>
          </div>
          <Link href="/appointments/new" className="crm-action self-start !min-h-9 !rounded-xl !px-3.5 !py-2 !text-xs bg-gradient-to-l from-pink-500 to-rose-500 shadow-md shadow-pink-200 hover:from-pink-600 hover:to-rose-600 sm:self-auto sm:!min-h-10 sm:!px-4 sm:!text-sm">
            <Icon name="calendarPlus" className="ml-1.5 h-4 w-4" />ثبت نوبت جدید
          </Link>
        </header>

        <section className="grid grid-cols-2 gap-2.5 lg:grid-cols-4 lg:gap-4">
          {stats.map(([label, value, action, href, icon, cardClass, iconClass]) => (
            <Link key={label} href={href} className={`group min-h-[102px] rounded-2xl border p-3.5 shadow-sm transition hover:-translate-y-0.5 hover:shadow-md sm:min-h-[116px] sm:p-5 ${cardClass}`}>
              <div className="flex items-center justify-between gap-2">
                <p className="text-[11px] font-bold text-slate-700 sm:text-sm">{label}</p>
                <span className={`flex h-8 w-8 items-center justify-center rounded-xl ${iconClass}`} aria-hidden="true">
                  <Icon name={icon} className="h-[17px] w-[17px]" />
                </span>
              </div>
              <p className="mt-1.5 text-[27px] font-black leading-none tracking-tight text-slate-950 sm:text-3xl">{value.toLocaleString("fa-IR")}</p>
              <p className="mt-1.5 text-[10px] font-bold text-slate-600 transition group-hover:text-pink-600 sm:text-xs">{action} ←</p>
            </Link>
          ))}
        </section>

        <section className="mt-4 rounded-3xl border border-white/80 bg-white/95 p-3.5 shadow-sm backdrop-blur sm:mt-5 sm:p-6">
          <div className="flex items-start justify-between gap-3">
            <div><h2 className="text-[16px] font-black text-slate-950 sm:text-[17px]">مشتریان نیازمند اقدام</h2><p className="mt-0.5 text-[11px] leading-5 text-slate-500 sm:text-xs">اولویت‌بندی بر اساس چرخه بازگشت خدمت</p></div>
            <Link href="/return-analysis" className="shrink-0 pt-0.5 text-[11px] font-black text-pink-600 sm:text-xs">مشاهده همه ←</Link>
          </div>
          {followUpCount === 0 ? <Empty text="فعلاً مشتری‌ای برای پیگیری نمایش داده نمی‌شود." /> : (
            <div className="mt-3 grid gap-2 sm:mt-4 md:grid-cols-2 xl:grid-cols-3">
              {[...data.overdue, ...data.atRisk, ...data.dueSoon].slice(0, 9).map((item, index) => (
                <Link href={`/customers/${item.customerId}`} key={`${item.customerId}-${item.serviceId ?? "service"}-${index}`} className="group rounded-2xl border border-slate-100 bg-slate-50/75 px-3 py-2.5 transition hover:border-pink-200 hover:bg-pink-50/50 sm:p-3.5">
                  <div className="flex items-center gap-2.5">
                    <Avatar name={item.customerName} />
                    <div className="min-w-0 flex-1"><p className="truncate text-sm font-black text-slate-900">{item.customerName}</p><p className="mt-0.5 truncate text-[11px] text-slate-500">{item.serviceTitle ?? "خدمت ثبت‌شده"}</p></div>
                    <span className={`shrink-0 rounded-full px-2 py-1 text-[9px] font-black ${actionBadgeClass(item.smartListType)}`}>{actionLabel(item.smartListType)}</span>
                  </div>
                  <div className="mt-2 flex items-center justify-between border-t border-slate-200/70 pt-2"><p className="text-[10px] font-bold text-slate-500">{daysLabel(item)}</p><span className="text-slate-400 transition group-hover:-translate-x-1">←</span></div>
                </Link>
              ))}
            </div>
          )}
        </section>

        <section className="mt-4 grid gap-4 lg:mt-5 lg:grid-cols-2">
          <DashboardPanel title="نوبت‌های امروز" href="/appointments" action="مشاهده همه" accent="violet">
            {data.todayAppointments.length === 0 ? <Empty text="برای امروز نوبتی ثبت نشده است." /> : (
              <div className="divide-y divide-slate-100">
                {data.todayAppointments.slice(0, 5).map((item) => (
                  <div key={item.id} className="flex items-center gap-2.5 py-2.5 first:pt-0 last:pb-0">
                    <div className="w-14 shrink-0 text-center"><p className="text-sm font-black text-slate-950">{formatTime(item.startAt)}</p><p className="mt-0.5 text-[9px] font-medium text-slate-400">{appointmentStatus(item.status)}</p></div>
                    <div className="min-w-0 flex-1 border-r border-slate-100 pr-2.5"><p className="truncate text-sm font-black text-slate-900">{item.customerName}</p><p className="mt-0.5 truncate text-[11px] text-slate-500">{item.services.join("، ") || "بدون خدمت"}</p></div>
                    <Avatar name={item.customerName} size="sm" />
                  </div>
                ))}
              </div>
            )}
          </DashboardPanel>

          <DashboardPanel title="پیگیری‌های باز" href="/follow-ups" action="مشاهده همه" accent="pink">
            {data.pendingReminders.length === 0 ? <Empty text="پیگیری بازی برای نمایش وجود ندارد." /> : (
              <div className="divide-y divide-slate-100">
                {data.pendingReminders.slice(0, 5).map((item) => (
                  <div key={item.id} className="flex items-center gap-2.5 py-2.5 first:pt-0 last:pb-0">
                    <span className="h-2 w-2 shrink-0 rounded-full bg-pink-500 ring-4 ring-pink-50" />
                    <Avatar name={item.customerName} size="sm" />
                    <div className="min-w-0 flex-1"><p className="truncate text-sm font-black text-slate-800">{item.title}</p><p className="mt-0.5 truncate text-[11px] text-slate-500">{item.customerName}</p></div>
                    <p className="shrink-0 text-[10px] font-bold text-slate-500">{formatDate(item.dueAt)}</p>
                  </div>
                ))}
              </div>
            )}
          </DashboardPanel>
        </section>

        <section className="mt-4 rounded-3xl border border-white/80 bg-white/95 p-3.5 shadow-sm sm:mt-5 sm:p-6">
          <div className="flex items-center justify-between gap-4"><div><h2 className="text-[16px] font-black text-slate-950 sm:text-[17px]">آخرین مراجعه‌ها</h2><p className="mt-0.5 text-[11px] text-slate-500 sm:text-xs">آخرین فعالیت مشتریان فعال</p></div><Link href="/customers" className="shrink-0 text-[11px] font-black text-pink-600 sm:text-xs">مشاهده همه ←</Link></div>
          {data.recentVisits.length === 0 ? <Empty text="هنوز مراجعه‌ای ثبت نشده است." /> : (
            <div className="mt-3 divide-y divide-slate-100 sm:mt-4 sm:grid sm:grid-cols-2 sm:divide-y-0 sm:gap-3 lg:grid-cols-4">
              {data.recentVisits.slice(0, 4).map((item) => (
                <Link href={`/customers/${item.customerId}`} key={item.id} className="group flex items-center gap-2.5 py-2.5 first:pt-0 last:pb-0 sm:rounded-2xl sm:border sm:border-slate-100 sm:bg-slate-50/60 sm:p-3.5">
                  <Avatar name={item.customerName} size="sm" />
                  <div className="min-w-0 flex-1"><p className="truncate text-sm font-black text-slate-800">{item.customerName}</p><p className="mt-0.5 text-[11px] text-slate-500">{formatRelativeDate(item.visitAt)}</p></div>
                  <div className="shrink-0 text-left"><p className="text-[9px] text-slate-400">{formatDate(item.visitAt)}</p>{item.totalAmount != null && <p className="mt-0.5 text-[10px] font-black text-slate-900">{item.totalAmount.toLocaleString("fa-IR")} تومان</p>}</div>
                </Link>
              ))}
            </div>
          )}
        </section>
      </div>
    </main>
  );
}

function DashboardPanel({ title, href, action, accent, children }: { title: string; href: string; action: string; accent: "violet" | "pink"; children: React.ReactNode }) {
  const isPink = accent === "pink";
  return (
    <section className="rounded-3xl border border-white/80 bg-white/95 p-3.5 shadow-sm backdrop-blur sm:p-6">
      <div className="mb-3.5 flex items-center justify-between gap-4 sm:mb-4">
        <div className="flex items-center gap-2.5">
          <span className={`flex h-8 w-8 items-center justify-center rounded-xl ${isPink ? "bg-pink-100 text-pink-600" : "bg-violet-100 text-violet-600"}`}>
            <Icon name={isPink ? "clock" : "calendar"} className="h-[16px] w-[16px]" />
          </span>
          <h2 className="text-[15px] font-black text-slate-950 sm:text-[16px]">{title}</h2>
        </div>
        <Link href={href} className="shrink-0 text-[11px] font-black text-pink-600 sm:text-xs">{action} ←</Link>
      </div>
      {children}
    </section>
  );
}

function Empty({ text }: { text: string }) {
  return <div className="rounded-2xl bg-slate-50 px-4 py-6 text-center text-xs text-slate-500 sm:text-sm">{text}</div>;
}

function DashboardLoading() {
  return <main className="-mx-4 -mt-5 min-h-screen bg-rose-50 px-4 pb-10 pt-4 sm:-mx-8 sm:-mt-8 sm:px-8"><div className="animate-pulse"><div className="h-7 w-36 rounded-lg bg-white/70"/><div className="mt-2 h-4 w-64 rounded bg-white/60"/><div className="mt-5 grid grid-cols-2 gap-2.5 lg:grid-cols-4"><div className="h-24 rounded-2xl bg-white/70"/><div className="h-24 rounded-2xl bg-white/70"/><div className="h-24 rounded-2xl bg-white/70"/><div className="h-24 rounded-2xl bg-white/70"/></div><div className="mt-4 h-64 rounded-3xl bg-white/70"/></div></main>;
}
