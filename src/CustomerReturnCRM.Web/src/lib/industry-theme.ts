export type IndustryKey = "salon" | "barbershop" | "repair" | "general";

export type IndustryTheme = {
  key: IndustryKey;
  label: string;
  accent: string;
  accentStrong: string;
  accentSoft: string;
  page: string;
  surface: string;
};

const themes: Record<IndustryKey, IndustryTheme> = {
  salon: { key: "salon", label: "سالن زیبایی", accent: "#C94F79", accentStrong: "#A63D63", accentSoft: "#F8E7ED", page: "#FAF6F2", surface: "#FFFFFF" },
  barbershop: { key: "barbershop", label: "آرایشگاه", accent: "#8B5E83", accentStrong: "#704765", accentSoft: "#F0E8F0", page: "#F7F5F7", surface: "#FFFFFF" },
  repair: { key: "repair", label: "خدمات تعمیراتی", accent: "#357A78", accentStrong: "#286260", accentSoft: "#E3F1F0", page: "#F4F7F7", surface: "#FFFFFF" },
  general: { key: "general", label: "کسب‌وکار خدماتی", accent: "#8B5CF6", accentStrong: "#6D28D9", accentSoft: "#EDE9FE", page: "#F7F7FB", surface: "#FFFFFF" },
};

export function industryKeyFromBusinessType(value?: string | null): IndustryKey {
  switch ((value ?? "").trim().toLowerCase()) {
    case "salon":
    case "beautysalon":
    case "beauty-salon":
      return "salon";
    case "barbershop":
      return "barbershop";
    case "repair":
      return "repair";
    default:
      return "general";
  }
}

export function getIndustryTheme(value?: string | null): IndustryTheme {
  return themes[industryKeyFromBusinessType(value)];
}
