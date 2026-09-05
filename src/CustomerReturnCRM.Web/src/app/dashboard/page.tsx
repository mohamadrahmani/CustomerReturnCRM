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

function avatarFor(name: string | null | undefined) {
  const value = name ?? "";
  const index = [...value].reduce((sum, char) => sum + char.charCodeAt(0), 0) % 3;
  return `/avatars/avatar-${index + 1}.svg`;
}

function Avatar({ name, size = "md" }: { name: string | null | undefined; size?: "sm" | "md" }) {
  return (
    <img
      src={avatarFor(name)}
      alt=""
      aria-hidden="true"
      className={size === "sm" ? "h-9 w-9 shrink-0 rounded-full object-cover ring-2 ring-white" : "h-11 w-11 shrink-0 rounded-full object-cover ring-2 ring-white"}
    />
  );
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
    <main className="-mx-4 -mt-5 min-h-[calc(100vh-65px)] bg-gradient-to-b from-rose-50 via-pink-50/60 to-violet-50/40 px-4 pb-10 pt-5 sm:-mx-8 sm:-mt-8 sm:px-8 sm:pt-7">
      <div className="mx-auto max-w-7xl">
        <header className="mb-5 flex flex-col gap-4 sm:mb-6 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <p className="text-xs font-bold text-pink-600">نمای کلی</p>
            <h1 className="mt-1 text-[26px] font-black tracking-tight text-slate-950 sm:text-3xl">داشبورد</h1>
            <p className="mt-1.5 text-sm leading-6 text-slate-600">وضعیت امروز {business?.name ?? "کسب‌وکار شما"} را در یک نگاه ببینید.</p>
          </div>
          <Link href="/appointments/new" className="crm-action self-start bg-gradient-to-l from-pink-500 to-rose-500 shadow-md shadow-pink-200 hover:from-pink-600 hover:to-rose-600 sm:self-auto">
            <span className="ml-2 text-base">＋</span>ثبت نوبت جدید
          </Link>
        </header>

        <section className="grid grid-cols-2 gap-3 lg:grid-cols-4 lg:gap-4">
          {stats.map(([label, value, action, href, icon, cardClass, iconClass]) => (
            <Link key={label} href={href} className={`group min-h-[112px] rounded-2xl border p-4 shadow-sm transition hover:-translate-y-0.5 hover:shadow-md sm:min-h-[122px] sm:p-5 ${cardClass}`}>
              <div className="flex items-center justify-between gap-2">
                <p className="text-xs font-bold text-slate-600 sm:text-sm">{label}</p>
                <span className={`flex h-9 w-9 items-center justify-center rounded-xl text-sm font-black ${iconClass}`} aria-hidden="true">
                  {icon === "users" ? "♙" : icon === "calendar" ? "▣" : icon === "clock" ? "◷" : "!"}
                </span>
              </div>
              <p className="mt-2 text-[29px] font-black leading-none tracking-tight text-slate-950 sm:text-3xl">{value.toLocaleString("fa-IR")}</p>
              <p className="mt-2 text-[11px] font-bold text-slate-600 transition group-hover:text-pink-600 sm:text-xs">{action} ←</p>
            </Link>
          ))}
        </section>

        <section className="mt-5 rounded-3xl border border-white/80 bg-white/90 p-4 shadow-sm backdrop-blur sm:mt-6 sm:p-6">
          <div className="flex items-start justify-between gap-4">
            <div><h2 className="text-[17px] font-black text-slate-950">مشتریان نیازمند اقدام</h2><p className="mt-1 text-xs leading-5 text-slate-500">اولویت‌بندی بر اساس چرخه بازگشت خدمت</p></div>
            <Link href="/return-analysis" className="shrink-0 text-xs font-black text-pink-600">مشاهده همه ←</Link>
          </div>
          {followUpCount === 0 ? <Empty text="فعلاً مشتری‌ای برای پیگیری نمایش داده نمی‌شود." /> : (
            <div className="mt-4 grid gap-2.5 md:grid-cols-2 xl:grid-cols-3">
              {[...data.overdue, ...data.atRisk, ...data.dueSoon].slice(0, 9).map((item, index) => (
                <Link href={`/customers/${item.customerId}`} key={`${item.customerId}-${item.serviceId ?? "service"}-${index}`} className="group rounded-2xl border border-slate-100 bg-slate-50/80 p-3.5 transition hover:border-pink-200 hover:bg-pink-50/50">
                  <div className="flex items-center gap-3">
                    <Avatar name={item.customerName} />
                    <div className="min-w-0 flex-1"><p className="truncate text-sm font-black text-slate-900">{item.customerName}</p><p className="mt-1 truncate text-xs text-slate-500">{item.serviceTitle ?? "خدمت ثبت‌شده"}</p></div>
                    <span className={`shrink-0 rounded-full px-2 py-1 text-[10px] font-black ${actionBadgeClass(item.smartListType)}`}>{actionLabel(item.smartListType)}</span>
                  </div>
                  <div className="mt-2.5 flex items-center justify-between border-t border-slate-200/70 pt-2.5"><p className="text-[11px] font-bold text-slate-500">{daysLabel(item)}</p><span className="text-slate-400 transition group-hover:-translate-x-1">←</span></div>
                </Link>
              ))}
            </div>
          )}
        </section>

        <section className="mt-5 grid gap-5 lg:mt-6 lg:grid-cols-2">
          <DashboardPanel title="نوبت‌های امروز" href="/appointments" action="مشاهده همه" accent="violet">
            {data.todayAppointments.length === 0 ? <Empty text="برای امروز نوبتی ثبت نشده است." /> : (
              <div className="divide-y divide-slate-100">
                {data.todayAppointments.slice(0, 5).map((item) => (
                  <div key={item.id} className="flex items-center gap-3 py-3 first:pt-0 last:pb-0">
                    <div className="w-14 shrink-0 text-center"><p className="text-sm font-black text-slate-950">{formatTime(item.startAt)}</p><p className="mt-1 text-[10px] font-medium text-slate-400">{appointmentStatus(item.status)}</p></div>
                    <div className="min-w-0 flex-1 border-r border-slate-100 pr-3"><p className="truncate text-sm font-black text-slate-900">{item.customerName}</p><p className="mt-1 truncate text-xs text-slate-500">{item.services.join("، ") || "بدون خدمت"}</p></div>
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
                  <div key={item.id} className="flex items-center gap-3 py-3 first:pt-0 last:pb-0">
                    <span className="h-2.5 w-2.5 shrink-0 rounded-full bg-pink-500 ring-4 ring-pink-50" />
                    <Avatar name={item.customerName} size="sm" />
                    <div className="min-w-0 flex-1"><p className="truncate text-sm font-black text-slate-800">{item.title}</p><p className="mt-1 truncate text-xs text-slate-500">{item.customerName}</p></div>
                    <p className="shrink-0 text-[11px] font-bold text-slate-500">{formatDate(item.dueAt)}</p>
                  </div>
                ))}
              </div>
            )}
          </DashboardPanel>
        </section>

        <section className="mt-5 rounded-3xl border border-white/80 bg-white/90 p-4 shadow-sm sm:mt-6 sm:p-6">
          <div className="flex items-center justify-between gap-4"><div><h2 className="text-[17px] font-black text-slate-950">آخرین مراجعه‌ها</h2><p className="mt-1 text-xs text-slate-500">آخرین فعالیت مشتریان فعال</p></div><Link href="/customers" className="shrink-0 text-xs font-black text-pink-600">مشاهده همه ←</Link></div>
          {data.recentVisits.length === 0 ? <Empty text="هنوز مراجعه‌ای ثبت نشده است." /> : (
            <div className="mt-4 divide-y divide-slate-100 sm:grid sm:grid-cols-2 sm:divide-y-0 sm:gap-3 lg:grid-cols-4">
              {data.recentVisits.slice(0, 4).map((item) => (
                <Link href={`/customers/${item.customerId}`} key={item.id} className="group flex items-center gap-3 py-3 first:pt-0 last:pb-0 sm:rounded-2xl sm:border sm:border-slate-100 sm:bg-slate-50/60 sm:p-3.5">
                  <Avatar name={item.customerName} size="sm" />
                  <div className="min-w-0 flex-1"><p className="truncate text-sm font-black text-slate-800">{item.customerName}</p><p className="mt-1 text-xs text-slate-500">{formatRelativeDate(item.visitAt)}</p></div>
                  <div className="shrink-0 text-left"><p className="text-[10px] text-slate-400">{formatDate(item.visitAt)}</p>{item.totalAmount != null && <p className="mt-1 text-xs font-black text-slate-900">{item.totalAmount.toLocaleString("fa-IR")} تومان</p>}</div>
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
  const iconClass = accent === "pink" ? "bg-pink-100 text-pink-600" : "bg-violet-100 text-violet-600";
  return <section className="rounded-3xl border border-white/80 bg-white/90 p-4 shadow-sm backdrop-blur sm:p-6"><div className="mb-4 flex items-center justify-between gap-4"><div className="flex items-center gap-2.5"><span className={`flex h-8 w-8 items-center justify-center rounded-xl text-xs font-black ${iconClass}`}>{accent === "pink" ? "◷" : "▣"}</span><h2 className="text-[16px] font-black text-slate-950">{title}</h2></div><Link href={href} className="shrink-0 text-xs font-black text-pink-600">{action} ←</Link></div>{children}</section>;
}

function Empty({ text }: { text: string }) {
  return <div className="rounded-2xl bg-slate-50 px-4 py-7 text-center text-sm text-slate-500">{text}</div>;
}

function DashboardLoading() {
  return <main className="-mx-4 -mt-5 min-h-screen bg-rose-50 px-4 pb-10 pt-5 sm:-mx-8 sm:-mt-8 sm:px-8"><div className="animate-pulse"><div className="h-8 w-40 rounded-lg bg-white/70"/><div className="mt-3 h-4 w-72 rounded bg-white/60"/><div className="mt-6 grid grid-cols-2 gap-3 lg:grid-cols-4"><div className="h-28 rounded-2xl bg-white/70"/><div className="h-28 rounded-2xl bg-white/70"/><div className="h-28 rounded-2xl bg-white/70"/><div className="h-28 rounded-2xl bg-white/70"/></div><div className="mt-5 h-72 rounded-3xl bg-white/70"/></div></main>;
}
