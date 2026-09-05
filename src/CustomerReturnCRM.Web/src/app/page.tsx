"use client";

import Link from "next/link";

const modules = [
  ["مشتریان", "مدیریت مشتریان و مشاهده سوابق"],
  ["نوبت‌ها", "تقویم و مدیریت قرارها"],
  ["خدمات", "تعریف خدمات و دوره بازگشت"],
  ["پیگیری‌ها", "مشتریان نیازمند پیگیری"],
];

export default function HomePage() {
  return (
    <main className="min-h-screen px-4 py-8 md:px-8">
      <div className="mx-auto max-w-6xl">
        <header className="mb-8 flex items-center justify-between">
          <div>
            <p className="mb-1 text-sm font-medium text-indigo-600">Customer Return CRM</p>
            <h1 className="text-2xl font-bold md:text-3xl">مدیریت مشتری و بازگشت</h1>
          </div>
          <Link href="/login" className="rounded-xl bg-indigo-600 px-5 py-2.5 text-sm font-semibold text-white hover:bg-indigo-700">
            ورود
          </Link>
        </header>

        <section className="rounded-3xl bg-slate-900 p-7 text-white shadow-sm md:p-10">
          <p className="mb-3 text-sm text-slate-300">نسخه اولیه</p>
          <h2 className="max-w-2xl text-3xl font-bold leading-tight md:text-4xl">از ثبت مشتری تا پیگیری بازگشت، در یک جریان ساده.</h2>
          <p className="mt-4 max-w-2xl leading-7 text-slate-300">هسته CRM برای کسب‌وکارهای خدماتی با تمرکز روی مشتری، مراجعه، خدمات و اقدام بعدی.</p>
        </section>

        <section className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {modules.map(([title, description]) => (
            <div key={title} className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
              <h3 className="font-bold">{title}</h3>
              <p className="mt-2 text-sm leading-6 text-slate-500">{description}</p>
            </div>
          ))}
        </section>
      </div>
    </main>
  );
}
