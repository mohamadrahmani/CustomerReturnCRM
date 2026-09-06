"use client";

import { useState } from "react";
import type { Customer } from "@/lib/api";

function customerName(customer: Customer) {
  return [customer.firstName, customer.lastName].filter(Boolean).join(" ");
}

export function CustomerPicker({ items, value, onChange, required = false }: { items: Customer[]; value: string; onChange: (id: string) => void; required?: boolean }) {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const selected = items.find(customer => customer.id === value);
  const normalized = query.trim().toLocaleLowerCase("fa-IR");
  const filtered = items.filter(customer => `${customer.firstName} ${customer.lastName ?? ""} ${customer.mobile}`.toLocaleLowerCase("fa-IR").includes(normalized));

  return (
    <div className="relative">
      <button type="button" onClick={() => setOpen(current => !current)} className="crm-input flex w-full items-center justify-between text-right" aria-haspopup="listbox" aria-expanded={open}>
        <span className={selected ? "text-slate-900" : "text-slate-400"}>{selected ? customerName(selected) : "انتخاب مشتری"}</span>
        <span aria-hidden="true">⌄</span>
      </button>
      {required && <input tabIndex={-1} value={value} onChange={() => undefined} required className="sr-only" aria-label="مشتری" />}
      {open && (
        <div className="absolute right-0 top-full z-50 mt-2 w-full overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-2xl">
          <div className="border-b border-slate-100 p-2">
            <input autoFocus value={query} onChange={event => setQuery(event.target.value)} placeholder="جستجوی نام، نام خانوادگی یا موبایل..." className="crm-input w-full" />
          </div>
          <div className="max-h-64 overflow-y-auto p-1" role="listbox">
            {filtered.length === 0 ? <div className="p-5 text-center text-xs text-slate-400">مشتری‌ای پیدا نشد.</div> : filtered.map(customer => (
              <button type="button" key={customer.id} onClick={() => { onChange(customer.id); setQuery(""); setOpen(false); }} className={`flex w-full items-center justify-between rounded-xl px-3 py-2.5 text-right hover:bg-pink-50 ${customer.id === value ? "bg-pink-50" : ""}`}>
                <span><span className="block text-sm font-bold">{customerName(customer)}</span><span dir="ltr" className="block text-[11px] text-slate-400">{customer.mobile}</span></span>
                {customer.id === value && <span className="text-xs font-black text-pink-600">✓</span>}
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
