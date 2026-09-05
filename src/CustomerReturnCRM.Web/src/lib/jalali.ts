export type JalaliDate = { year: number; month: number; day: number };

const JALALI_MONTH_NAMES = [
  "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
  "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند",
];

export const jalaliMonthNames = JALALI_MONTH_NAMES;

function div(a: number, b: number) { return Math.floor(a / b); }

export function gregorianToJalali(gy: number, gm: number, gd: number): JalaliDate {
  const gDaysInMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
  const jDaysInMonth = [31, 31, 31, 31, 31, 31, 30, 30, 30, 30, 30, 29];
  let gy2 = gy - 1600;
  let gm2 = gm - 1;
  let gd2 = gd - 1;
  let gDayNo = 365 * gy2 + div(gy2 + 3, 4) - div(gy2 + 99, 100) + div(gy2 + 399, 400);
  for (let i = 0; i < gm2; i++) gDayNo += gDaysInMonth[i];
  if (gm2 > 1 && ((gy % 4 === 0 && gy % 100 !== 0) || gy % 400 === 0)) gDayNo++;
  gDayNo += gd2;
  let jDayNo = gDayNo - 79;
  const jNp = div(jDayNo, 12053);
  jDayNo %= 12053;
  let jy = 979 + 33 * jNp + 4 * div(jDayNo, 1461);
  jDayNo %= 1461;
  if (jDayNo >= 366) {
    jy += div(jDayNo - 1, 365);
    jDayNo = (jDayNo - 1) % 365;
  }
  let jm = 0;
  while (jm < 11 && jDayNo >= jDaysInMonth[jm]) jDayNo -= jDaysInMonth[jm++];
  return { year: jy, month: jm + 1, day: jDayNo + 1 };
}

export function jalaliToGregorian(jy: number, jm: number, jd: number): { year: number; month: number; day: number } {
  const jDaysInMonth = [31, 31, 31, 31, 31, 31, 30, 30, 30, 30, 30, 29];
  let jy2 = jy - 979;
  let jDayNo = 365 * jy2 + div(jy2, 33) * 8 + div((jy2 % 33) + 3, 4);
  for (let i = 0; i < jm - 1; i++) jDayNo += jDaysInMonth[i];
  jDayNo += jd - 1;
  let gDayNo = jDayNo + 79;
  let gy = 1600 + 400 * div(gDayNo, 146097);
  gDayNo %= 146097;
  let leap = true;
  if (gDayNo >= 36525) {
    gDayNo--;
    gy += 100 * div(gDayNo, 36524);
    gDayNo %= 36524;
    if (gDayNo >= 365) gDayNo++;
    else leap = false;
  }
  gy += 4 * div(gDayNo, 1461);
  gDayNo %= 1461;
  if (gDayNo >= 366) {
    leap = false;
    gDayNo--;
    gy += div(gDayNo, 365);
    gDayNo %= 365;
  }
  const gDaysInMonth = [31, leap ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
  let gm = 0;
  while (gm < 11 && gDayNo >= gDaysInMonth[gm]) gDayNo -= gDaysInMonth[gm++];
  return { year: gy, month: gm + 1, day: gDayNo + 1 };
}

export function todayJalali(): JalaliDate {
  const d = new Date();
  return gregorianToJalali(d.getFullYear(), d.getMonth() + 1, d.getDate());
}

export function jalaliToIso(j: JalaliDate): string {
  const g = jalaliToGregorian(j.year, j.month, j.day);
  return `${g.year}-${String(g.month).padStart(2, "0")}-${String(g.day).padStart(2, "0")}`;
}

export function isoToJalali(iso: string): JalaliDate {
  const [y, m, d] = iso.split("-").map(Number);
  return gregorianToJalali(y, m, d);
}

export function jalaliDateKey(j: JalaliDate): string {
  return `${j.year}-${String(j.month).padStart(2, "0")}-${String(j.day).padStart(2, "0")}`;
}

export function isJalaliLeapYear(year: number): boolean {
  const next = jalaliToGregorian(year + 1, 1, 1);
  const current = jalaliToGregorian(year, 1, 1);
  const diff = new Date(next.year, next.month - 1, next.day).getTime() - new Date(current.year, current.month - 1, current.day).getTime();
  return diff > 365 * 24 * 60 * 60 * 1000;
}

export function jalaliMonthDays(year: number, month: number): number {
  if (month <= 6) return 31;
  if (month <= 11) return 30;
  return isJalaliLeapYear(year) ? 30 : 29;
}

export function jalaliDayOfWeek(j: JalaliDate): number {
  const g = jalaliToGregorian(j.year, j.month, j.day);
  return new Date(g.year, g.month - 1, g.day).getDay();
}

export function shiftJalaliMonth(j: JalaliDate, delta: number): JalaliDate {
  let year = j.year;
  let month = j.month + delta;
  while (month > 12) { month -= 12; year++; }
  while (month < 1) { month += 12; year--; }
  return { year, month, day: Math.min(j.day, jalaliMonthDays(year, month)) };
}
