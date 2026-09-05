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

function actionLabel(item: { smartListType: string }) {
  if (item.smartListType === "Overdue") return "عقب‌افتاده";
  if (item.smartListType === "AtRisk") return "در معرض ریسک";
  return "نزدیک موعد";
}

function actionBadgeClass(item: { smartListType: string }) {
  if (item.smartListType === "Overdue") return "bg-rose-50 text-rose-700";
  if (item.smartListType === "AtRisk") return "bg-amber-50 text-amber-700";
  return "bg-indigo-50 text-indigo-700";
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
    ["مشتریان فعال", data.activeCustomerCount, "مشاهده مشتریان", "/customers", "users"],
    ["نوبت‌های امروز", data.todayAppointments.length, "مشاهده نوبت‌ها", "/appointments", "calendar"],
    ["پیگیری‌های باز", data.pendingReminders.length, "مشاهده پیگیری‌ها", "/follow-ups", "check"],
    ["نیازمند اقدام", followUpCount, "مشاهده موارد", "/return-analysis", "alert"],
  ] as const;

  return <main>
    <header className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div>
        <p className="text-xs font-bold text-indigo-600">نمای کلی</p>
        <h1 className="mt-1 text-2xl font-extrabold tracking-tight text-slate-900 sm:text-3xl">داشبورد</h1>
        <p className="mt-1 text-sm text-slate-500">وضعیت امروز {business?.name ?? "کسب‌وکار شما"} را در یک نگاه ببینید.</p>
      </div>
      <Link href="/appointments/new" className="crm-action self-start sm:self-auto"><span className="ml-2 text-base">＋</span>ثبت نوبت</Link>
    </header>

    <section className="grid grid-cols-2 gap-3 lg:grid-cols-4 lg:gap-4">
      {stats.map(([label, value, action, href, icon]) => (
        <Link key={label} href={href} className="group crm-card min-h-[108px] p-4 transition hover:-translate-y-0.5 hover:border-indigo-200 hover:shadow-md sm:min-h-[118px] sm:p-5">
          <div className="flex items-center justify-between gap-2">
            <p className="text-xs font-semibold text-slate-500 sm:text-sm">{label}</p>
            <span className="crm-icon-box" aria-hidden="true">
              {icon === "users" ? "◉" : icon === "calendar" ? "▣" : icon === "check" ? "✓" : "!"}
            </span>
          </div>
          <p className="mt-2 text-2xl font-extrabold tracking-tight text-slate-900 sm:text-3xl">{value.toLocaleString("fa-IR")}</p>
          <p className="mt-1 text-[11px] font-bold text-indigo-600 sm:text-xs">{action} ←</p>
        </Link>
      ))}
    </section>

    <section className="crm-card mt-5 p-4 sm:mt-6 sm:p-6">
      <div className="flex items-center justify-between gap-4">
        <div><h2 className="crm-section-title">مشتریان نیازمند اقدام</h2><p className="mt-1 text-xs text-slate-500">اولویت‌بندی بر اساس چرخه بازگشت خدمت</p></div>
        <Link href="/return-analysis" className="shrink-0 text-xs font-bold text-indigo-600">مشاهده همه ←</Link>
      </div>
      {followUpCount === 0 ? <Empty text="فعلاً مشتری‌ای برای پیگیری نمایش داده نمی‌شود." /> : (
        <div className="mt-4 grid gap-2.5 md:grid-cols-2 xl:grid-cols-3">
          {[...data.overdue, ...data.atRisk, ...data.dueSoon].slice(0, 9).map((item, index) => (
            <Link href={`/customers/${item.customerId}`} key={`${item.customerId}-${item.serviceId ?? "service"}-${index}`} className="group rounded-xl border border-slate-100 bg-slate-50/70 p-3.5 transition hover:border-indigo-200 hover:bg-indigo-50/30">
              <div className="flex items-start gap-3">
                <span className={`mt-0.5 shrink-0 rounded-full px-2 py-1 text-[10px] font-bold ${actionBadgeClass(item)}`}>{actionLabel(item)}</span>
                <div className="min-w-0 flex-1"><p className="truncate text-sm font-bold text-slate-800">{item.customerName}</p><p className="mt-1 truncate text-xs text-slate-500">{item.serviceTitle ?? "خدمت ثبت‌شده"}</p></div>
                <span className="text-slate-400 transition group-hover:-translate-x-0.5">←</span>
              </div>
              <p className="mt-2 text-xs font-bold text-slate-700">{daysLabel(item)}</p>
            </Link>
          ))}
        </div>
      )}
    </section>

    <section className="mt-5 grid gap-5 lg:grid-cols-2 lg:mt-6">
      <DashboardPanel title="نوبت‌های امروز" href="/appointments" action="مشاهده همه">
        {data.todayAppointments.length === 0 ? <Empty text="برای امروز نوبتی ثبت نشده است." /> : (
          <div className="divide-y divide-slate-100">
            {data.todayAppointments.slice(0, 5).map((item) => (
              <div key={item.id} className="flex items-center gap-3 py-3 first:pt-0 last:pb-0">
                <div className="w-14 shrink-0 text-center"><p className="text-sm font-extrabold text-slate-900">{formatTime(item.startAt)}</p><p className="mt-1 text-[10px] text-slate-400">{appointmentStatus(item.status)}</p></div>
                <div className="min-w-0 flex-1 border-r border-slate-100 pr-3"><p className="truncate text-sm font-bold text-slate-800">{item.customerName}</p><p className="mt-1 truncate text-xs text-slate-500">{item.services.join("، ") || "بدون خدمت"}</p></div>
              </div>
            ))}
          </div>
        )}
      </DashboardPanel>

      <DashboardPanel title="پیگیری‌های باز" href="/follow-ups" action="مشاهده همه">
        {data.pendingReminders.length === 0 ? <Empty text="پیگیری بازی برای نمایش وجود ندارد." /> : (
          <div className="divide-y divide-slate-100">
            {data.pendingReminders.slice(0, 5).map((item) => (
              <div key={item.id} className="flex items-center gap-3 py-3 first:pt-0 last:pb-0">
                <span className="h-2 w-2 shrink-0 rounded-full bg-indigo-500" />
                <div className="min-w-0 flex-1"><p className="truncate text-sm font-bold text-slate-800">{item.title}</p><p className="mt-1 truncate text-xs text-slate-500">{item.customerName}</p></div>
                <p className="shrink-0 text-[11px] font-medium text-slate-500">{formatDate(item.dueAt)}</p>
              </div>
            ))}
          </div>
        )}
      </DashboardPanel>
    </section>

    <section className="crm-card mt-5 p-4 sm:mt-6 sm:p-6">
      <div className="flex items-center justify-between gap-4"><div><h2 className="crm-section-title">آخرین مراجعه‌ها</h2><p className="mt-1 text-xs text-slate-500">آخرین فعالیت مشتریان فعال</p></div><Link href="/customers" className="shrink-0 text-xs font-bold text-indigo-600">مشاهده همه ←</Link></div>
      {data.recentVisits.length === 0 ? <Empty text="هنوز مراجعه‌ای ثبت نشده است." /> : (
        <div className="mt-4 divide-y divide-slate-100 sm:grid sm:grid-cols-2 sm:divide-y-0 sm:gap-3 lg:grid-cols-4">
          {data.recentVisits.slice(0, 4).map((item) => (
            <Link href={`/customers/${item.customerId}`} key={item.id} className="group flex items-center justify-between gap-3 py-3 first:pt-0 last:pb-0 sm:rounded-xl sm:border sm:border-slate-100 sm:p-3.5">
              <div className="min-w-0"><p className="truncate text-sm font-bold text-slate-800">{item.customerName}</p><p className="mt-1 text-xs text-slate-500">{formatRelativeDate(item.visitAt)}</p></div>
              <div className="shrink-0 text-left"><p className="text-[11px] text-slate-400">{formatDate(item.visitAt)}</p>{item.totalAmount != null && <p className="mt-1 text-xs font-extrabold text-slate-900">{item.totalAmount.toLocaleString("fa-IR")} تومان</p>}</div>
            </Link>
          ))}
        </div>
      )}
    </section>
  </main>;
}

function DashboardPanel({ title, href, action, children }: { title: string; href: string; action: string; children: React.ReactNode }) {
  return <section className="crm-card p-4 sm:p-6"><div className="mb-4 flex items-center justify-between gap-4"><h2 className="crm-section-title">{title}</h2><Link href={href} className="shrink-0 text-xs font-bold text-indigo-600">{action} ←</Link></div>{children}</section>;
}

function Empty({ text }: { text: string }) {
  return <div className="rounded-xl bg-slate-50 px-4 py-7 text-center text-sm text-slate-500">{text}</div>;
}

function DashboardLoading() {
  return <main><div className="animate-pulse"><div className="h-8 w-40 rounded-lg bg-slate-200"/><div className="mt-3 h-4 w-72 rounded bg-slate-100"/><div className="mt-6 grid grid-cols-2 gap-3 lg:grid-cols-4"><div className="h-28 rounded-2xl bg-slate-100"/><div className="h-28 rounded-2xl bg-slate-100"/><div className="h-28 rounded-2xl bg-slate-100"/><div className="h-28 rounded-2xl bg-slate-100"/></div><div className="mt-5 h-64 rounded-2xl bg-slate-100"/></div></main>;
}
