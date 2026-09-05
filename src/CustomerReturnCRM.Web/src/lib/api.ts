import { readAuth } from "./auth";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5108";

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const auth = readAuth();
  const headers = new Headers(init.headers);
  headers.set("Content-Type", "application/json");
  if (auth?.token) headers.set("Authorization", `Bearer ${auth.token}`);

  const response = await fetch(`${API_BASE_URL}${path}`, { ...init, headers });

  if (response.status === 401 && typeof window !== "undefined") {
    sessionStorage.removeItem("crm_auth");
    sessionStorage.removeItem("crm_business_id");
    window.location.href = "/login";
    throw new Error("نشست شما منقضی شده است.");
  }

  if (!response.ok) {
    let message = `Request failed (${response.status})`;
    try {
      const body = await response.json();
      message = body.error ?? message;
    } catch {
      // Keep the status-based message when the API has no JSON error body.
    }
    throw new Error(message);
  }

  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export type AuthenticationBusiness = {
  id: string;
  name: string;
  role: string;
};

export type AuthenticationResult = {
  userId: string;
  email: string;
  token: string;
  expiresAt: string;
  businesses: AuthenticationBusiness[];
};

export type BusinessSetupRequest = {
  name: string;
  businessType: string;
  mobile: string;
  address?: string;
  city?: string;
  firstName: string;
  lastName: string;
  staffMobile?: string;
  serviceTemplateId?: string;
};

export type BusinessSetupResult = {
  businessId: string;
  membershipId: string;
  staffId: string;
  serviceId: string | null;
};

export type DashboardAppointment = {
  id: string;
  customerId: string;
  customerName: string;
  startAt: string;
  endAt: string;
  status: number;
  services: string[];
};

export type DashboardReminder = {
  id: string;
  customerId: string;
  customerName: string;
  serviceId: string | null;
  title: string;
  dueAt: string;
  status: number;
};

export type DashboardSmartListItem = {
  customerId: string;
  customerName: string;
  mobile: string;
  serviceId: string | null;
  serviceTitle: string | null;
  lastVisitAt: string;
  expectedReturnDate: string | null;
  daysFromExpectedReturn: number | null;
  smartListType: string;
};

export type DashboardVisit = {
  id: string;
  customerId: string;
  customerName: string;
  visitAt: string;
  totalAmount: number | null;
};

export type DashboardResult = {
  date: string;
  activeCustomerCount: number;
  todayAppointments: DashboardAppointment[];
  pendingReminders: DashboardReminder[];
  dueSoon: DashboardSmartListItem[];
  overdue: DashboardSmartListItem[];
  atRisk: DashboardSmartListItem[];
  noRecentVisit: DashboardSmartListItem[];
  recentVisits: DashboardVisit[];
};

export async function login(email: string, password: string) {
  return apiFetch<AuthenticationResult>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password }),
  });
}

export async function createBusiness(request: BusinessSetupRequest) {
  return apiFetch<BusinessSetupResult>("/api/businesses", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export async function getDashboard(businessId: string) {
  return apiFetch<DashboardResult>(`/api/businesses/${businessId}/dashboard`);
}
