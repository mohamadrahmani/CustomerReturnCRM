"use client";

import { useEffect, useMemo, useState } from "react";
import { createSmsCampaign, getSmsTemplates, type Customer, type SmsTemplate } from "@/lib/api";
import { SMS_CAMPAIGN_MAX_RECIPIENTS, SMS_MESSAGE_MAX_LENGTH, SMS_VARIABLES } from "@/lib/sms";
import { PersianDateTimePicker } from "@/components/persian-date-picker";

function customerName(customer: Customer) {
  return [customer.firstName, customer.lastName].filter(Boolean).join(" ");
}

function renderPreview(message: string, customer: Customer, businessName: string) {
  const fullName = customerName(customer);
  return message
    .replaceAll("[نام]", customer.firstName)
    .replaceAll("[نام خانوادگی]", customer.lastName ?? "")
    .replaceAll("[نام کامل]", fullName)
    .replaceAll("[نام کسب‌وکار]", businessName);
}

export function SmsComposer({ businessId, customers, onClose, onCreated, title = "ارسال SMS" }: { businessId: string; customers: Customer[]; onClose: () => void; onCreated?: () => void; title?: string }) {
  const [templates, setTemplates] = useState<SmsTemplate[]>([]);
  const [templateId, setTemplateId] = useState("");
  const [message, setMessage] = useState("");
  const [name, setName] = useState("");
  const [mode, setMode] = useState<"now" | "schedule">("now");
  const [scheduledAt, setScheduledAt] = useState("");
  const [previewCustomerId, setPreviewCustomerId] = useState(customers[0]?.id ?? "");
  const [error, setError] = useState("");
  const [loadingTemplates, setLoadingTemplates] = useState(true);
  const [saving, setSaving] = useState(false);
  const [success, setSuccess] = useState(false);

  const selectedPreviewCustomer = customers.find(x => x.id === previewCustomerId) ?? customers[0];
  const preview = selectedPreviewCustomer ? renderPreview(message, selectedPreviewCustomer, "نام کسب‌وکار") : message;
  const hasInvalidMobile = customers.some(x => !x.mobile?.trim());

  useEffect(() => {
    let active = true;
    getSmsTemplates(businessId, true)
      .then(items => { if (active) setTemplates(items); })
      .catch(() => { if (active) setError("بارگذاری قالب‌های پیامکی انجام نشد."); })
      .finally(() => { if (active) setLoadingTemplates(false); });
    return () => { active = false; };
  }, [businessId]);

  const selectedTemplate = useMemo(() => templates.find(x => x.id === templateId), [templates, templateId]);
  const insertVariable = (token: string) => setMessage(current => current.length + token.length <= SMS_MESSAGE_MAX_LENGTH ? `${current}${token}` : current);

  const submit = async () => {
    setError("");
    if (!customers.length) return setError("حداقل یک مشتری برای ارسال انتخاب کنید.");
    if (customers.length > SMS_CAMPAIGN_MAX_RECIPIENTS) return setError("امکان ارسال به بیش از ۱۰٬۰۰۰ مشتری در یک نوبت وجود ندارد.");
    if (hasInvalidMobile) return setError("یکی از مشتریان شماره موبایل معتبر ندارد. لطفاً آن مشتری را از گیرندگان حذف کنید.");
    if (!message.trim()) return setError("متن پیام الزامی است.");
    if (message.length > SMS_MESSAGE_MAX_LENGTH) return setError("متن پیام نمی‌تواند بیشتر از ۲۰۰۰ کاراکتر باشد.");
    if (mode === "schedule" && !scheduledAt) return setError("تاریخ و ساعت ارسال را انتخاب کنید.");
    if (mode === "schedule" && new Date(scheduledAt).getTime() <= Date.now()) return setError("زمان‌بندی باید در آینده باشد.");

    setSaving(true);
    try {
      await createSmsCampaign(businessId, {
        templateId: selectedTemplate?.id,
        name: name.trim() || undefined,
        message: message.trim(),
        scheduledAt: mode === "schedule" ? new Date(scheduledAt).toISOString() : undefined,
        customerIds: customers.map(x => x.id),
      });
      setSuccess(true);
      onCreated?.();
    } catch (e) {
      setError(e instanceof Error ? e.message : "درخواست ارسال ثبت نشد.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-[60] flex items-end justify-center bg-slate-950/40 p-0 backdrop-blur-[2px] sm:items-center sm:p-4" onMouseDown={onClose}>
      <div role="dialog" aria-modal="true" aria-labelledby="sms-composer-title" onMouseDown={e => e.stopPropagation()} className="flex max-h-[95vh] w-full max-w-2xl flex-col overflow-hidden rounded-t-[28px] bg-white shadow-2xl sm:rounded-[28px]">
        <header className="flex shrink-0 items-start justify-between border-b border-slate-100 bg-gradient-to-l from-violet-50 via-white to-white px-5 py-4 sm:px-6">
          <div><p className="text-[11px] font-bold text-violet-600">پیامک</p><h2 id="sms-composer-title" className="mt-1 text-xl font-black text-slate-950">{title}</h2><p className="mt-1 text-xs text-slate-500">ارسال برای {customers.length.toLocaleString("fa-IR")} مشتری</p></div>
          <button type="button" onClick={onClose} className="flex h-10 w-10 items-center justify-center rounded-xl border border-slate-200 text-lg text-slate-500" aria-label="بستن">×</button>
        </header>

        {success ? <div className="flex-1 overflow-y-auto p-6 text-center sm:p-10"><div className="mx-auto flex h-16 w-16 items-center justify-center rounded-2xl bg-emerald-50 text-emerald-600">✓</div><h3 className="mt-4 text-lg font-black text-slate-900">درخواست ارسال ثبت شد.</h3><p className="mt-2 text-sm leading-6 text-slate-500">پیام‌ها برای پردازش به صف ارسال سپرده شدند. این پیام به معنی تحویل قطعی نیست.</p><button type="button" onClick={onClose} className="crm-action mt-6 min-h-11 w-full sm:w-auto sm:px-10">بستن</button></div> : <>
          <div className="flex-1 space-y-5 overflow-y-auto p-5 sm:p-6">
            <section className="rounded-2xl border border-slate-100 bg-slate-50/70 p-4"><div className="flex items-center justify-between gap-3"><div><p className="text-xs font-black text-slate-800">گیرندگان</p><p className="mt-1 text-[11px] text-slate-500">{customers.length.toLocaleString("fa-IR")} مشتری انتخاب شده</p></div><span className="rounded-full bg-white px-2.5 py-1 text-[10px] font-black text-slate-500">حداکثر ۱۰٬۰۰۰</span></div><div className="mt-3 flex max-h-28 flex-wrap gap-1.5 overflow-y-auto">{customers.slice(0, 20).map(customer => <span key={customer.id} className="rounded-lg bg-white px-2.5 py-1.5 text-[10px] font-bold text-slate-600 ring-1 ring-slate-200">{customerName(customer)} · {customer.mobile}</span>)}{customers.length > 20 && <span className="rounded-lg bg-white px-2.5 py-1.5 text-[10px] font-bold text-slate-400">+{(customers.length - 20).toLocaleString("fa-IR")} نفر دیگر</span>}</div></section>

            <div className="grid gap-4 sm:grid-cols-2"><label className="block"><span className="mb-1.5 block text-xs font-bold text-slate-700">قالب پیام</span><select value={templateId} onChange={e => { setTemplateId(e.target.value); const item = templates.find(x => x.id === e.target.value); if (item) setMessage(item.content); }} disabled={loadingTemplates} className="crm-input w-full"><option value="">بدون قالب</option>{templates.map(template => <option key={template.id} value={template.id}>{template.name}</option>)}</select></label><label className="block"><span className="mb-1.5 block text-xs font-bold text-slate-700">عنوان ارسال <span className="font-normal text-slate-400">(اختیاری)</span></span><input value={name} onChange={e => setName(e.target.value)} className="crm-input w-full" placeholder="مثلاً یادآوری مراجعه" /></label></div>

            <section><div className="flex items-center justify-between"><label htmlFor="sms-message" className="text-xs font-bold text-slate-700">متن پیام</label><span className={`text-[10px] font-bold ${message.length > SMS_MESSAGE_MAX_LENGTH ? "text-rose-600" : "text-slate-400"}`}>{message.length.toLocaleString("fa-IR")} / ۲۰۰۰</span></div><textarea id="sms-message" value={message} onChange={e => setMessage(e.target.value)} dir="rtl" maxLength={SMS_MESSAGE_MAX_LENGTH} rows={6} className="crm-input mt-1.5 min-h-32 w-full resize-y leading-7" placeholder="متن پیام را بنویسید..." /><div className="mt-2 flex flex-wrap gap-1.5">{SMS_VARIABLES.map(variable => <button type="button" key={variable.token} onClick={() => insertVariable(variable.token)} className="rounded-lg bg-violet-50 px-2.5 py-1.5 text-[10px] font-bold text-violet-700 hover:bg-violet-100">+ {variable.label}</button>)}</div></section>

            <section className="rounded-2xl border border-slate-100 p-4"><div className="flex flex-wrap items-center gap-2"><span className="text-xs font-black text-slate-800">پیش‌نمایش</span>{customers.length > 1 && <select value={selectedPreviewCustomer?.id ?? ""} onChange={e => setPreviewCustomerId(e.target.value)} className="crm-input mr-auto !min-h-9 !w-auto !py-1.5 text-xs">{customers.slice(0, 3).map(customer => <option key={customer.id} value={customer.id}>{customerName(customer)}</option>)}</select>}</div><div className="mt-3 rounded-2xl bg-slate-50 p-4 text-sm leading-7 text-slate-700">{preview || "متن پیام در اینجا نمایش داده می‌شود."}</div><p className="mt-2 text-[10px] text-slate-400">پیش‌نمایش بر اساس اطلاعات مشتری انتخاب‌شده است.</p></section>

            <section><p className="mb-2 text-xs font-bold text-slate-700">زمان ارسال</p><div className="grid grid-cols-2 gap-2 rounded-xl bg-slate-100 p-1"><button type="button" onClick={() => setMode("now")} className={`rounded-lg py-2.5 text-xs font-bold ${mode === "now" ? "bg-white text-violet-700 shadow-sm" : "text-slate-500"}`}>ارسال فوری</button><button type="button" onClick={() => setMode("schedule")} className={`rounded-lg py-2.5 text-xs font-bold ${mode === "schedule" ? "bg-white text-violet-700 shadow-sm" : "text-slate-500"}`}>زمان‌بندی ارسال</button></div>{mode === "schedule" && <div className="mt-3"><PersianDateTimePicker value={scheduledAt} onChange={setScheduledAt} /></div>}</section>

            {error && <div role="alert" className="rounded-xl bg-rose-50 px-3 py-2.5 text-xs font-bold leading-6 text-rose-700">{error}</div>}
          </div>
          <footer className="sticky bottom-0 flex shrink-0 gap-2 border-t border-slate-100 bg-white p-4"><button type="button" disabled={saving} onClick={onClose} className="crm-secondary-action min-h-11 flex-1">انصراف</button><button type="button" disabled={saving || !customers.length} onClick={submit} className="crm-action min-h-11 flex-1">{saving ? "در حال ثبت..." : mode === "schedule" ? "ثبت زمان‌بندی" : "ثبت درخواست ارسال"}</button></footer>
        </>}
      </div>
    </div>
  );
}
