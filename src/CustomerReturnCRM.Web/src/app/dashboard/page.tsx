"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { getDashboard } from "@/lib/api";
import { useAuth } from "@/components/auth-provider";

function formatTime(value: string) {
  return new Intl.DateTimeFormat("fa-IR", { hour: "2-digit", minute: "2-digit" }).format(new Date(value));
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
  if (item.daysFromExpectedReturn > 0) return `${item.daysFromExpectedReturn} روز گذشته`;
  if (item.daysFromExpectedReturn === 0) return "امروز";
  return `${Math.abs(item.daysFromExpectedReturn)} روز مانده`;
}

export default function DashboardPage() {
  const { auth, businessId, isReady } = useAuth();
  const business = auth?.businesses.find((item) => item.id === businessId) ?? auth?.businesses[0];
  const activeBusinessId = businessId ?? business?.id ?? null;

  const dashboard = useQuery({
    queryKey: ["dashboard", activeBusinessId],
    queryFn: () => getDashboard(activeBusinessId!),
    enabled: isReady && !!activeBusinessId,
    staleTime: 30_000,
  });

  if (!isReady) return <DashboardLoading />;

  if (!activeBusinessId) {
    return (
      <main className="mx-auto max-w-7xl p-4 md:p-8">
        <section className="rounded-2xl border border-slate-200 bg-white p-8 text-center shadow-sm">
          <h1 className="text-xl font-bold text-slate-900">کسب‌وکاری انتخاب نشده است</h1>
          <p className="mt-2 text-sm text-slate-500">ابتدا اطلاعات کسب‌وکار را تکمیل کنید.</p>
          <Link href="/setup" className="mt-5 inline-flex rounded-xl bg-indigo-600 px-5 py-2.5 text-sm font-semibold text-white">تکمیل راه‌اندازی</Link>
        </section>
      </main>
    );
  }

  if (dashboard.isLoading) return <DashboardLoading />;

  if (dashboard.isError) {
    return (
      <main className="mx-auto max-w-7xl p-4 md:p-8">
        <section className="rounded-2xl border border-rose-200 bg-white p-8 text-center shadow-sm">
          <h1 className="text-xl font-bold text-slate-900">بارگذاری داشبورد انجام نشد</h1>
          <p className="mt-2 text-sm text-slate-500">اتصال به اطلاعات کسب‌وکار با خطا مواجه شد.</p>
          <button onClick={() => dashboard.refetch()} className="mt-5 rounded-xl border border-slate-200 px-5 py-2.5 text-sm font-semibold">تلاش مجدد</button>
        </section>
      </main>
    );
  }

  const data = dashboard.data!;
  const followUpCount = data.dueSoon.length + data.overdue.length + data.atRisk.length;
  const stats = [
    ["مشتریان فعال", data.activeCustomerCount, "مدیریت مشتریان", "/customers"],
    ["نوبت‌های امروز", data.todayAppointments.length, "مشاهده تقویم", "/appointments"],
    ["پیگیری‌های باز", data.pendingReminders.length, "مشاهده پیگیری‌ها", "/follow-ups"],
    ["نیازمند اقدام", followUpCount, "تحلیل بازگشت", "/return-analysis"],
  ] as const;

  return (
    <main className="mx-auto max-w-7xl p-4 md:p-8">
      <header className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-sm font-medium text-indigo-600">داشبورد</p>
          <h1 className="mt-1 text-2xl font-bold tracking-tight text-slate-900">سلام، {auth?.email ?? "خوش آمدید"}</h1>
          <p className="mt-1 text-sm text-slate-500">نمای کلی امروز در {business?.name ?? "کسب‌وکار شما"}</p>
        </div>
        <Link href="/appointments/new" className="inline-flex w-fit rounded-xl bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm">+ ثبت نوبت</Link>
      </header>

      <section className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {stats.map(([label, value, action, href]) => (
          <Link key={label} href={href} className="group rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:-translate-y-0.5 hover:shadow-md">
            <p className="text-sm text-slate-500">{label}</p>
            <p className="mt-3 text-3xl font-bold text-slate-900">{value.toLocaleString("fa-IR")}</p>
            <p className="mt-3 text-xs font-medium text-indigo-600 group-hover:underline">{action} ←</p>
          </Link>
        ))}
      </section>

      <section className="mt-6 grid gap-6 lg:grid-cols-2">
        <DashboardPanel title="نوبت‌های امروز" href="/appointments" action="مشاهده همه">
          {data.todayAppointments.length === 0 ? <Empty text="برای امروز نوبتی ثبت نشده است." /> : (
            <div className="divide-y divide-slate-100">
              {data.todayAppointments.slice(0, 6).map((item) => (
                <div key={item.id} className="flex items-center justify-between gap-4 py-3 first:pt-0 last:pb-0">
                  <div className="min-w-0"><p className="truncate font-semibold text-slate-800">{item.customerName}</p><p className="mt-1 truncate text-xs text-slate-500">{item.services.join("، ") || "بدون خدمت"}</p></div>
                  <div className="shrink-0 text-left"><p className="font-semibold text-slate-900">{formatTime(item.startAt)}</p><p className="mt-1 text-xs text-slate-500">{appointmentStatus(item.status)}</p></div>
                </div>
              ))}
            </div>
          )}
        </DashboardPanel>

        <DashboardPanel title="پیگیری‌های باز" href="/follow-ups" action="مشاهده همه">
          {data.pendingReminders.length === 0 ? <Empty text="پیگیری بازی برای نمایش وجود ندارد." /> : (
            <div className="divide-y divide-slate-100">
              {data.pendingReminders.slice(0, 6).map((item) => (
                <div key={item.id} className="flex items-center justify-between gap-4 py-3 first:pt-0 last:pb-0">
                  <div className="min-w-0"><p className="truncate font-semibold text-slate-800">{item.title}</p><p className="mt-1 text-xs text-slate-500">{item.customerName}</p></div>
                  <p className="shrink-0 text-xs font-medium text-slate-600">{formatDate(item.dueAt)}</p>
                </div>
              ))}
            </div>
          )}
        </DashboardPanel>
      </section>

      <section className="mt-6 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <div className="flex items-center justify-between"><div><h2 className="font-bold text-slate-900">مشتریان نیازمند اقدام</h2><p className="mt-1 text-xs text-slate-500">بر اساس چرخه بازگشت خدمت</p></div><Link href="/return-analysis" className="text-xs font-semibold text-indigo-600">مشاهده تحلیل ←</Link></div>
        {followUpCount === 0 ? <Empty text="فعلاً مشتری‌ای برای پیگیری نمایش داده نمی‌شود." /> : (
          <div className="mt-4 grid gap-3 md:grid-cols-3">
            {[...data.overdue, ...data.atRisk, ...data.dueSoon].slice(0, 9).map((item, index) => (
              <Link href={`/customers/${item.customerId}`} key={`${item.customerId}-${item.serviceId ?? "service"}-${index}`} className="rounded-xl border border-slate-100 bg-slate-50 p-4 hover:border-indigo-200">
                <div className="flex items-start justify-between gap-3"><p className="font-semibold text-slate-800">{item.customerName}</p><span className="rounded-full bg-white px-2 py-1 text-[11px] text-slate-600">{item.smartListType === "Overdue" ? "عقب‌افتاده" : item.smartListType === "AtRisk" ? "در معرض ریسک" : "نزدیک موعد"}</span></div>
                <p className="mt-2 text-xs text-slate-500">{item.serviceTitle ?? "خدمت ثبت‌شده"}</p><p className="mt-2 text-xs font-medium text-slate-700">{daysLabel(item)}</p>
              </Link>
            ))}
          </div>
        )}
      </section>

      <section className="mt-6 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <div className="flex items-center justify-between"><h2 className="font-bold text-slate-900">آخرین مراجعه‌ها</h2><Link href="/customers" className="text-xs font-semibold text-indigo-600">مشتریان ←</Link></div>
        {data.recentVisits.length === 0 ? <Empty text="هنوز مراجعه‌ای ثبت نشده است." /> : <div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">{data.recentVisits.slice(0, 8).map((item) => <div key={item.id} className="rounded-xl border border-slate-100 p-4"><p className="font-semibold text-slate-800">{item.customerName}</p><p className="mt-1 text-xs text-slate-500">{formatDate(item.visitAt)}</p>{item.totalAmount != null && <p className="mt-2 text-sm font-bold text-slate-900">{item.totalAmount.toLocaleString("fa-IR")} تومان</p>}</div>)}</div>}
      </section>
    </main>
  );
}

function DashboardPanel({ title, href, action, children }: { title: string; href: string; action: string; children: React.ReactNode }) {
  return <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"><div className="mb-4 flex items-center justify-between"><h2 className="font-bold text-slate-900">{title}</h2><Link href={href} className="text-xs font-semibold text-indigo-600">{action} ←</Link></div>{children}</section>;
}

function Empty({ text }: { text: string }) { return <div className="rounded-xl bg-slate-50 px-4 py-8 text-center text-sm text-slate-500">{text}</div>; }

function DashboardLoading() { return <main className="mx-auto max-w-7xl p-4 md:p-8"><div className="animate-pulse"><div className="h-8 w-56 rounded bg-slate-200"/><div className="mt-2 h-4 w-72 rounded bg-slate-100"/><div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">{[1,2,3,4].map((item) => <div key={item} className="h-36 rounded-2xl bg-slate-100"/>)}</div><div className="mt-6 grid gap-6 lg:grid-cols-2"><div className="h-72 rounded-2xl bg-slate-100"/><div className="h-72 rounded-2xl bg-slate-100"/></div></div></main>; }
