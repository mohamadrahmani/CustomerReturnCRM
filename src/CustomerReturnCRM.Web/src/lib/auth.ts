import type { AuthenticationResult } from "./api";

const STORAGE_KEY = "crm_auth";

export function readAuth(): AuthenticationResult | null {
  if (typeof window === "undefined") return null;
  const raw = sessionStorage.getItem(STORAGE_KEY);
  if (!raw) return null;

  try {
    const value = JSON.parse(raw) as AuthenticationResult;
    if (!value.token || !value.expiresAt) return null;
    if (new Date(value.expiresAt).getTime() <= Date.now()) {
      sessionStorage.removeItem(STORAGE_KEY);
      return null;
    }
    return value;
  } catch {
    sessionStorage.removeItem(STORAGE_KEY);
    return null;
  }
}

export function writeAuth(value: AuthenticationResult) {
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(value));
}

export function clearAuth() {
  sessionStorage.removeItem(STORAGE_KEY);
  sessionStorage.removeItem("crm_business_id");
}

export function readBusinessId(auth: AuthenticationResult | null) {
  if (typeof window === "undefined" || !auth) return null;
  const stored = sessionStorage.getItem("crm_business_id");
  if (stored && auth.businesses.some((business) => business.id === stored)) return stored;
  return auth.businesses[0]?.id ?? null;
}
