"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/components/auth-provider";
import { createBusiness, apiFetch } from "@/lib/api";

type ServiceTemplate = {
  id: string;
  businessType: string;
  title: string;
  defaultDurationMinutes: number;
  suggestedReturnDays: number | null;
};

const businessTypes = [
  { value: "General", label: "سایر کسب‌وکارهای خدماتی" },
  { value: "Salon", label: "سالن زیبایی" },
  { value: "Barbershop", label: "آرایشگاه" },
  { value: "Repair", label: "خدمات تعمیراتی" },
];

export default function SetupPage() {
  const router = useRouter();
  const { auth, isReady, setAuth } = useAuth();
  const [templates, setTemplates] = useState<ServiceTemplate[]>([]);
  const [loadingTemplates, setLoadingTemplates] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [form, setForm] = useState({
    name: "",
    businessType: "General",
    mobile: "",
    address: "",
    city: "",
    firstName: "",
    lastName: "",
    staffMobile: "",
    serviceTemplateId: "",
  });

  useEffect(() => {
    if (!isReady) return;
    if (!auth) {
      router.replace("/login");
      return;
    }
    if (auth.businesses.length > 0) {
      router.replace("/dashboard");
    }
  }, [auth, isReady, router]);

  useEffect(() => {
    if (!auth || auth.businesses.length > 0) return;
    let cancelled = false;
    setLoadingTemplates(true);
    apiFetch<ServiceTemplate[]>(`/api/service-templates?businessType=${encodeURIComponent(form.businessType)}`)
      .then((items) => {
        if (cancelled) return;
        setTemplates(items);
        setForm((current) => ({ ...current, serviceTemplateId: items[0]?.id ?? "" }));
      })
      .catch(() => {
        if (!cancelled) setTemplates([]);
      })
      .finally(() => {
        if (!cancelled) setLoadingTemplates(false);
      });
    return () => {
      cancelled = true;
    };
  }, [auth, form.businessType]);

  if (!isReady || !auth || auth.businesses.length > 0) {
    return <main className="flex min-h-screen items-center justify-center text-sm text-slate-500">در حال آماده‌سازی...</main>;
  }

  const update = (key: keyof typeof form, value: string) =>
    setForm((current) => ({ ...current, [key]: value }));

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setSubmitting(true);
    try {
      await createBusiness({
        ...form,
        serviceTemplateId: form.serviceTemplateId || undefined,
      });

      const refreshed = await apiFetch<{ businesses: typeof auth.businesses }>("/api/auth/login", {
        method: "POST",
        body: JSON.stringify({ email: auth.email, password: "" }),
      });
      setAuth({ ...auth, businesses: refreshed.businesses });
      router.replace("/dashboard");
    } catch (err) {
      setError(err instanceof Error ? err.message : "ایجاد کسب‌وکار انجام نشد.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main className="min-h-screen bg-slate-50 px-4 py-8 md:px-8">
      <div className="mx-auto max-w-3xl">
        <div className="mb-7">
          <p className="text-sm font-semibold text-indigo-600">Customer Return CRM</p>
          <h1 className="mt-2 text-2xl font-bold md:text-3xl">راه‌اندازی کسب‌وکار</h1>
          <p className="mt-2 text-sm leading-6 text-slate-500">اطلاعات اولیه را وارد کنید تا فضای کاری شما ساخته شود.</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6 rounded-3xl border border-slate-200 bg-white p-5 shadow-sm md:p-7">
          <section>
            <h2 className="font-bold">اطلاعات کسب‌وکار</h2>
            <div className="mt-4 grid gap-4 md:grid-cols-2">
              <Field label="نام کسب‌وکار" value={form.name} onChange={(value) => update("name", value)} required />
              <div>
                <label className="text-sm font-medium">نوع کسب‌وکار</label>
                <select value={form.businessType} onChange={(e) => update("businessType", e.target.value)} className="mt-2 w-full rounded-xl border border-slate-300 px-4 py-3 outline-none focus:border-indigo-500" required>
                  {businessTypes.map((type) => <option key={type.value} value={type.value}>{type.label}</option>)}
                </select>
              </div>
              <Field label="شماره تماس" value={form.mobile} onChange={(value) => update("mobile", value)} required type="tel" />
              <Field label="شهر" value={form.city} onChange={(value) => update("city", value)} />
              <div className="md:col-span-2"><Field label="آدرس" value={form.address} onChange={(value) => update("address", value)} /></div>
            </div>
          </section>

          <section className="border-t border-slate-100 pt-6">
            <h2 className="font-bold">اطلاعات مسئول</h2>
            <div className="mt-4 grid gap-4 md:grid-cols-2">
              <Field label="نام" value={form.firstName} onChange={(value) => update("firstName", value)} required />
              <Field label="نام خانوادگی" value={form.lastName} onChange={(value) => update("lastName", value)} required />
              <Field label="شماره تماس مسئول" value={form.staffMobile} onChange={(value) => update("staffMobile", value)} type="tel" />
            </div>
          </section>

          <section className="border-t border-slate-100 pt-6">
            <h2 className="font-bold">خدمت اولیه</h2>
            <p className="mt-1 text-sm text-slate-500">اختیاری؛ این مورد فقط به‌عنوان اولین خدمت کسب‌وکار ساخته می‌شود.</p>
            <select value={form.serviceTemplateId} onChange={(e) => update("serviceTemplateId", e.target.value)} disabled={loadingTemplates || templates.length === 0} className="mt-4 w-full rounded-xl border border-slate-300 px-4 py-3 outline-none focus:border-indigo-500 disabled:bg-slate-50">
              <option value="">بدون خدمت اولیه</option>
              {templates.map((template) => <option key={template.id} value={template.id}>{template.title} · {template.defaultDurationMinutes} دقیقه</option>)}
            </select>
          </section>

          {error && <p role="alert" className="rounded-xl bg-red-50 px-4 py-3 text-sm text-red-700">{error}</p>}

          <button type="submit" disabled={submitting} className="w-full rounded-xl bg-indigo-600 px-4 py-3 font-semibold text-white transition hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-50">
            {submitting ? "در حال ایجاد..." : "ایجاد و ورود به پنل"}
          </button>
        </form>
      </div>
    </main>
  );
}

function Field({ label, value, onChange, required = false, type = "text" }: { label: string; value: string; onChange: (value: string) => void; required?: boolean; type?: string }) {
  return (
    <div>
      <label className="text-sm font-medium">{label}</label>
      <input value={value} onChange={(e) => onChange(e.target.value)} type={type} required={required} className="mt-2 w-full rounded-xl border border-slate-300 px-4 py-3 outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100" />
    </div>
  );
}
