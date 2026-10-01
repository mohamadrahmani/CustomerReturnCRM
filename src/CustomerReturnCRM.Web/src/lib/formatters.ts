const faNumber = new Intl.NumberFormat("fa-IR", { maximumFractionDigits: 0 });

export function formatNumber(value: number | null | undefined) {
  return value == null ? "—" : faNumber.format(value);
}

export function formatMoney(value: number | null | undefined) {
  return value == null ? "—" : faNumber.format(value) + " تومان";
}

export function normalizeNumberInput(value: string) {
  return value
    .replace(/[۰-۹]/g, (digit) => String("۰۱۲۳۴۵۶۷۸۹".indexOf(digit)))
    .replace(/[٬,]/g, "")
    .replace(/٫/g, ".");
}
