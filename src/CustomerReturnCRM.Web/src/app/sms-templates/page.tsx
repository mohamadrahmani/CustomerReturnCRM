"use client";

import { useEffect, useState } from "react";
import { useAuth } from "@/components/auth-provider";
import { createSmsTemplate, getSmsTemplates, updateSmsTemplate, type SmsTemplate } from "@/lib/api";
import { SMS_MESSAGE_MAX_LENGTH, SMS_VARIABLES } from "@/lib/sms";

const emptyForm = { name: "", content: "" };

export default function SmsTemplatesPage() {
  const { auth, businessId, isReady } = useAuth();
  const activeBusinessId = businessId ?? auth?.businesses[0]?.id ?? null;
  const [items, setItems] = useState<SmsTemplate[]>([]);
  const [form, setForm] = useState(emptyForm);
  const [editing, setEditing] = useState<SmsTemplate | null>(null);
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  async function load() {
    if (!activeBusinessId) return;
    setLoading(true);
    try { setItems(await getSmsTemplates(activeBusinessId, false)); }
    catch (e) { setError(e instanceof Error ? e.message : "بارگذاری قالب‌ها انجام نشد."); }
    finally { setLoading(false); }
  }

  useEffect(() => { if (isReady && activeBusinessId) void load(); }, [isReady, activeBusinessId]);

  function startCreate() { setEditing(null); setForm(emptyForm); setError(""); setSuccess(""); setOpen(true); }
  function startEdit(item: SmsTemplate) { setEditing(item); setForm({ name: item.name, content: item.content }); setError(""); setSuccess(""); setOpen(true); }
  function insertVariable(token: string) { setForm(current => current.content.length + token.length <= SMS_MESSAGE_MAX_LENGTH ? { ...current, content: `${current.content}${token}` } : current); }

  async function save() {
    if (!activeBusinessId) return;
    setError(""); setSuccess("");
    if (!form.name.trim()) return setError("نام قالب الزامی است.");
    if (!form.content.trim()) return setError("متن قالب الزامی است.");
    if (form.content.length > SMS_MESSAGE_MAX_LENGTH) return setError("متن قالب نمی‌تواند بیشتر از ۲۰۰۰ کاراکتر باشد.");
    setSaving(true);
    try {
      if (editing) await updateSmsTemplate(activeBusinessId, editing.id, { name: form.name.trim(), content: form.content.trim(), isActive: editing.isActive });
      else await createSmsTemplate(activeBusinessId, { name: form.name.trim(), content: form.content.trim() });
      setOpen(false); setSuccess(editing ? "قالب با موفقیت ویرایش شد." : "قالب با موفقیت ایجاد شد."); await load();
    } catch (e) { setError(e instanceof Error ? e.message : "ذخیره قالب انجام نشد."); }
    finally { setSaving(false); }
  }

  async function toggle(item: SmsTemplate) {
    if (!activeBusinessId) return;
    setError("");
    try { await updateSmsTemplate(activeBusinessId, item.id, { name: item.name, content: item.content, isActive: !item.isActive }); await load(); }
    catch (e) { setError(e instanceof Error ? e.message : "تغییر وضعیت قالب انجام نشد."); }
  }

  if (!isReady || !activeBusinessId) return <main className="p-4"><div className="h-32 animate-pulse rounded-3xl bg-white/70" /></main>;

  return (
    <main className="min-h-[calc(100vh-120px)]">
      <header className="mb-5 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div><p className="text-[11px] font-bold text-violet-600">ارتباط با مشتری</p><h1 className="mt-1 text-2xl font-black">قالب‌های SMS</h1><p className="mt-1 text-xs text-slate-500">قالب‌های آماده برای ارسال سریع و یکپارچه پیام.</p></div>
        <button type="button" onClick={startCreate} className="crm-action self-start">＋ قالب جدید</button>
      </header>

      {success && <div role="status" className="mb-4 rounded-2xl bg-emerald-50 px-4 py-3 text-xs font-bold text-emerald-700">{success}</div>}
      {error && !open && <div role="alert" className="mb-4 rounded-2xl bg-rose-50 px-4 py-3 text-xs font-bold leading-6 text-rose-700">{error}</div>}

      {loading ? <div className="space-y-3">{[1,2,3].map(x => <div key={x} className="h-28 animate-pulse rounded-2xl bg-white" />)}</div> : items.length === 0 ? (
        <section className="crm-card p-10 text-center"><div className="text-3xl">✉</div><h2 className="mt-3 text-base font-black">هنوز قالبی ساخته نشده</h2><p className="mt-1 text-xs text-slate-500">یک قالب بسازید تا در Composer پیامکی قابل انتخاب باشد.</p><button type="button" onClick={startCreate} className="crm-action mt-5">ساخت اولین قالب</button></section>
      ) : (
        <section className="grid gap-3 md:grid-cols-2">{items.map(item => (
          <article key={item.id} className={`crm-card p-4 ${!item.isActive ? "opacity-70" : ""}`}>
            <div className="flex items-start justify-between gap-3"><div><div className="flex items-center gap-2"><h2 className="font-black text-slate-900">{item.name}</h2><span className={`rounded-full px-2 py-1 text-[10px] font-bold ${item.isActive ? "bg-emerald-50 text-emerald-700" : "bg-slate-100 text-slate-500"}`}>{item.isActive ? "فعال" : "غیرفعال"}</span></div><p className="mt-2 whitespace-pre-wrap text-sm leading-7 text-slate-600">{item.content}</p></div></div>
            <div className="mt-4 flex flex-wrap gap-2 border-t border-slate-100 pt-3"><button type="button" onClick={() => startEdit(item)} className="crm-secondary-action">ویرایش</button><button type="button" onClick={() => void toggle(item)} className="crm-secondary-action">{item.isActive ? "غیرفعال کردن" : "فعال کردن"}</button></div>
          </article>
        ))}</section>
      )}

      {open && <div className="fixed inset-0 z-[60] flex items-end justify-center bg-slate-950/40 p-0 backdrop-blur-[2px] sm:items-center sm:p-4" onMouseDown={() => !saving && setOpen(false)}>
        <div role="dialog" aria-modal="true" onMouseDown={e => e.stopPropagation()} className="w-full max-w-xl rounded-t-[28px] bg-white shadow-2xl sm:rounded-[28px]">
          <header className="flex items-start justify-between border-b border-slate-100 px-5 py-4"><div><p className="text-[11px] font-bold text-violet-600">پیامک</p><h2 className="mt-1 text-lg font-black">{editing ? "ویرایش قالب" : "قالب جدید"}</h2></div><button type="button" disabled={saving} onClick={() => setOpen(false)} className="h-9 w-9 rounded-xl border border-slate-200 text-slate-500">×</button></header>
          <div className="space-y-4 p-5"><label className="block"><span className="mb-1.5 block text-xs font-bold">نام قالب</span><input value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} className="crm-input w-full" placeholder="مثلاً یادآوری مراجعه" /></label>
            <section><div className="flex items-center justify-between"><label className="text-xs font-bold">متن قالب</label><span className="text-[10px] text-slate-400">{form.content.length.toLocaleString("fa-IR")} / ۲۰۰۰</span></div><textarea value={form.content} onChange={e => setForm({ ...form, content: e.target.value })} maxLength={SMS_MESSAGE_MAX_LENGTH} rows={7} dir="rtl" className="crm-input mt-1.5 min-h-36 w-full resize-y leading-7" placeholder="متن پیام را وارد کنید..." /><div className="mt-2 flex flex-wrap gap-1.5">{SMS_VARIABLES.map(variable => <button type="button" key={variable.token} onClick={() => insertVariable(variable.token)} className="rounded-lg bg-violet-50 px-2.5 py-1.5 text-[10px] font-bold text-violet-700">+ {variable.label}</button>)}</div></section>
            {error && <div role="alert" className="rounded-xl bg-rose-50 px-3 py-2.5 text-xs font-bold leading-6 text-rose-700">{error}</div>}
          </div>
          <footer className="flex gap-2 border-t border-slate-100 p-4"><button type="button" disabled={saving} onClick={() => setOpen(false)} className="crm-secondary-action min-h-11 flex-1">انصراف</button><button type="button" disabled={saving} onClick={() => void save()} className="crm-action min-h-11 flex-1">{saving ? "در حال ذخیره..." : "ذخیره قالب"}</button></footer>
        </div>
      </div>}
    </main>
  );
}
