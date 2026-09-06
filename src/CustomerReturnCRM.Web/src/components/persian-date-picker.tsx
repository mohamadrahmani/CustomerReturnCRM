"use client";

import { useState } from "react";
import { gregorianToJalali, jalaliMonthDays, jalaliMonthNames, jalaliToIso, shiftJalaliMonth, todayJalali, type JalaliDate } from "@/lib/jalali";

const faNumber = new Intl.NumberFormat("fa-IR");
const weekDays = ["ش", "ی", "د", "س", "چ", "پ", "ج"];

function parseIso(value: string) {
  const [y, m, d] = value.split("-").map(Number);
  return new Date(y, m - 1, d);
}

function displayDate(value: string) {
  return new Intl.DateTimeFormat("fa-IR-u-ca-persian", { day: "numeric", month: "long", year: "numeric" }).format(parseIso(value));
}

export function PersianDatePicker({ value, onChange, placeholder = "انتخاب تاریخ" }: { value: string; onChange: (value: string) => void; placeholder?: string }) {
  const [open, setOpen] = useState(false);
  const selected = value ? gregorianToJalali(parseIso(value).getFullYear(), parseIso(value).getMonth() + 1, parseIso(value).getDate()) : todayJalali();
  const [month, setMonth] = useState<JalaliDate>(selected);
  const first = parseIso(jalaliToIso({ year: month.year, month: month.month, day: 1 }));
  const offset = (first.getDay() + 1) % 7;
  const days = jalaliMonthDays(month.year, month.month);
  const cells = Array.from({ length: offset + days }, (_, i) => i < offset ? null : i - offset + 1);

  return (
    <div className="relative">
      <button type="button" onClick={() => setOpen(x => !x)} className="crm-input flex w-full items-center justify-between text-right">
        <span className={value ? "text-slate-900" : "text-slate-400"}>{value ? displayDate(value) : placeholder}</span>
        <span aria-hidden="true">▦</span>
      </button>
      {open && (
        <div className="absolute right-0 top-full z-50 mt-2 w-[300px] max-w-[calc(100vw-2rem)] rounded-2xl border border-slate-200 bg-white p-3 shadow-2xl">
          <div className="mb-3 flex items-center justify-between">
            <button type="button" onClick={() => setMonth(m => shiftJalaliMonth(m, -1))} className="rounded-lg px-2 py-1 text-lg hover:bg-slate-100">‹</button>
            <b className="text-sm">{jalaliMonthNames[month.month - 1]} {faNumber.format(month.year)}</b>
            <button type="button" onClick={() => setMonth(m => shiftJalaliMonth(m, 1))} className="rounded-lg px-2 py-1 text-lg hover:bg-slate-100">›</button>
          </div>
          <div className="mb-1 grid grid-cols-7 text-center text-[10px] font-bold text-slate-400">{weekDays.map(x => <span key={x}>{x}</span>)}</div>
          <div className="grid grid-cols-7 gap-1">
            {cells.map((day, index) => day === null ? <span key={`empty-${index}`} /> : (
              <button key={day} type="button" onClick={() => { onChange(jalaliToIso({ year: month.year, month: month.month, day })); setOpen(false); }} className={`h-8 rounded-lg text-xs font-bold ${value && day === selected.day && month.month === selected.month && month.year === selected.year ? "bg-pink-500 text-white" : "hover:bg-slate-100"}`}>
                {faNumber.format(day)}
              </button>
            ))}
          </div>
          <button type="button" onClick={() => { const today = todayJalali(); onChange(jalaliToIso(today)); setMonth(today); setOpen(false); }} className="mt-3 w-full rounded-xl bg-slate-50 py-2 text-xs font-bold text-pink-600">امروز</button>
        </div>
      )}
    </div>
  );
}

export function PersianDateTimePicker({ value, onChange }: { value: string; onChange: (value: string) => void }) {
  const date = value ? value.slice(0, 10) : "";
  const time = value ? value.slice(11, 16) : "";
  return (
    <div className="grid grid-cols-[1fr_120px] gap-2">
      <PersianDatePicker value={date} onChange={nextDate => onChange(`${nextDate}T${time || "10:00"}`)} />
      <label className="relative">
        <span className="sr-only">ساعت</span>
        <input type="time" value={time} onChange={e => onChange(`${date || jalaliToIso(todayJalali())}T${e.target.value}`)} className="crm-input w-full" aria-label="ساعت" required />
      </label>
    </div>
  );
}
