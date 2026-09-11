"use client";

import Link from "next/link";

const sections = [
  { href: "/settings/working-hours", title: "ساعات کاری", description: "تعیین روزهای کاری و ساعت شروع و پایان فعالیت کسب‌وکار برای محاسبه زمان‌های رزرو.", icon: "◷" },
  { href: "/settings/sms", title: "پیامک", description: "مدیریت قالب‌ها و مشاهده تاریخچه ارسال پیامک.", icon: "✉" },
];

export default function SettingsPage() {
  return <main dir="rtl"><div className="mb-6"><h1 className="text-2xl font-extrabold text-slate-900">تنظیمات</h1><p className="mt-1 text-sm text-slate-500">تنظیمات و امکانات مدیریتی کسب‌وکار را از اینجا مدیریت کنید.</p></div><div className="grid gap-4 sm:grid-cols-2">{sections.map(section => <Link key={section.href} href={section.href} className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm transition hover:-translate-y-0.5 hover:border-slate-300 hover:shadow"><div className="flex h-11 w-11 items-center justify-center rounded-xl bg-slate-100 text-lg">{section.icon}</div><h2 className="mt-4 font-bold text-slate-900">{section.title}</h2><p className="mt-2 text-sm leading-6 text-slate-500">{section.description}</p><span className="mt-4 inline-flex text-sm font-semibold text-indigo-600">ورود به تنظیمات ←</span></Link>)}</div></main>;
}
