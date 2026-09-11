"use client";

import Link from "next/link";

export default function SmsSettingsPage() {
  return <main dir="rtl"><div className="mb-6"><Link href="/settings" className="text-sm text-slate-500">← تنظیمات</Link><h1 className="mt-3 text-2xl font-extrabold text-slate-900">تنظیمات پیامک</h1><p className="mt-1 text-sm text-slate-500">قالب‌ها و سابقه ارسال پیامک کسب‌وکار را مدیریت کنید.</p></div><div className="grid gap-4 sm:grid-cols-2"><Link href="/sms-templates" className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm hover:border-slate-300"><h2 className="font-bold">قالب‌های پیامک</h2><p className="mt-2 text-sm leading-6 text-slate-500">ساخت، ویرایش و فعال/غیرفعال کردن قالب‌های آماده.</p><span className="mt-4 inline-flex text-sm font-semibold text-indigo-600">مدیریت قالب‌ها ←</span></Link><Link href="/sms-campaigns" className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm hover:border-slate-300"><h2 className="font-bold">تاریخچه ارسال</h2><p className="mt-2 text-sm leading-6 text-slate-500">مشاهده کمپین‌ها، وضعیت ارسال و جزئیات گیرندگان.</p><span className="mt-4 inline-flex text-sm font-semibold text-indigo-600">مشاهده تاریخچه ←</span></Link></div></main>;
}
