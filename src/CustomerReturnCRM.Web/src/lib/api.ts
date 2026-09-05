import { readAuth } from "./auth";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5108";

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const auth = readAuth();
  const headers = new Headers(init.headers);
  headers.set("Content-Type", "application/json");
  if (auth?.token) headers.set("Authorization", `Bearer ${auth.token}`);
  const response = await fetch(`${API_BASE_URL}${path}`, { ...init, headers });
  if (response.status === 401 && typeof window !== "undefined") {
    sessionStorage.removeItem("crm_auth"); sessionStorage.removeItem("crm_business_id"); window.location.href = "/login"; throw new Error("نشست شما منقضی شده است.");
  }
  if (!response.ok) {
    let message = `Request failed (${response.status})`;
    try { const body = await response.json(); message = body.error ?? message; } catch { /* status message */ }
    throw new Error(message);
  }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export type AuthenticationBusiness = { id: string; name: string; role: string };
export type AuthenticationResult = { userId: string; email: string; token: string; expiresAt: string; businesses: AuthenticationBusiness[] };
export type BusinessSetupRequest = { name: string; businessType: string; mobile: string; address?: string; city?: string; firstName: string; lastName: string; staffMobile?: string; serviceTemplateId?: string };
export type BusinessSetupResult = { businessId: string; membershipId: string; staffId: string; serviceId: string | null };
export type DashboardAppointment = { id: string; customerId: string; customerName: string; startAt: string; endAt: string; status: number; services: string[] };
export type DashboardReminder = { id: string; customerId: string; customerName: string; serviceId: string | null; title: string; dueAt: string; status: number };
export type DashboardSmartListItem = { customerId: string; customerName: string; mobile: string; serviceId: string | null; serviceTitle: string | null; lastVisitAt: string; expectedReturnDate: string | null; daysFromExpectedReturn: number | null; smartListType: string };
export type DashboardVisit = { id: string; customerId: string; customerName: string; visitAt: string; totalAmount: number | null };
export type DashboardResult = { date: string; activeCustomerCount: number; todayAppointments: DashboardAppointment[]; pendingReminders: DashboardReminder[]; dueSoon: DashboardSmartListItem[]; overdue: DashboardSmartListItem[]; atRisk: DashboardSmartListItem[]; noRecentVisit: DashboardSmartListItem[]; recentVisits: DashboardVisit[] };
export type Customer = { id: string; businessId: string; firstName: string; lastName: string | null; mobile: string; birthDate: string | null; note: string | null; isActive: boolean; createdAt: string; updatedAt: string | null; lastVisitDate: string | null; totalVisits: number };
export type Service = { id: string; businessId: string; title: string; description: string | null; defaultPrice: number; defaultDurationMinutes: number; suggestedReturnDays: number | null; isActive: boolean; createdAt: string; updatedAt: string | null };
export type Staff = { id: string; businessId: string; firstName: string; lastName: string; mobile: string | null; userId: string | null; isActive: boolean; createdAt: string; updatedAt: string | null };
export type PagedResult<T> = { items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number };
export type CustomerProfileVisit = { id: string; visitAt: string; totalAmount: number | null; note: string | null; services: string[] };
export type CustomerProfileAppointment = { id: string; startAt: string; endAt: string; status: string; note: string | null; services: string[] };
export type CustomerProfileReminder = { id: string; serviceId: string | null; title: string; dueAt: string; status: string; note: string | null };
export type Reminder = { id: string; businessId: string; customerId: string; serviceId: string | null; title: string; dueAt: string; status: number; note: string | null; createdByUserId: string; completedAt: string | null; createdAt: string; updatedAt: string | null };
export type ExpectedReturn = { serviceId: string; serviceTitle: string; lastVisitAt: string; suggestedReturnDays: number; expectedReturnDate: string; daysFromExpectedReturn: number; hasFutureAppointment: boolean };
export type CustomerReturnAnalysis = { customerId: string; customerName: string; mobile: string; services: ExpectedReturn[] };
export type CustomerProfile = { customer: Customer; visits: CustomerProfileVisit[]; futureAppointments: CustomerProfileAppointment[]; reminders: CustomerProfileReminder[]; returnAnalysis: CustomerReturnAnalysis };

export async function login(email: string, password: string) { return apiFetch<AuthenticationResult>("/api/auth/login", { method: "POST", body: JSON.stringify({ email, password }) }); }
export async function register(email: string, password: string) { return apiFetch<AuthenticationResult>("/api/auth/register", { method: "POST", body: JSON.stringify({ email, password }) }); }
export async function createBusiness(request: BusinessSetupRequest) { return apiFetch<BusinessSetupResult>("/api/businesses", { method: "POST", body: JSON.stringify(request) }); }
export async function getDashboard(businessId: string) { return apiFetch<DashboardResult>(`/api/businesses/${businessId}/dashboard`); }
export async function getCustomers(businessId: string, page = 1, pageSize = 20, search = "", isActive: boolean | null = true) { const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) }); if (search.trim()) params.set("search", search.trim()); if (isActive !== null) params.set("isActive", String(isActive)); return apiFetch<PagedResult<Customer>>(`/api/businesses/${businessId}/customers?${params.toString()}`); }
export async function getCustomerProfile(businessId: string, customerId: string) { return apiFetch<CustomerProfile>(`/api/businesses/${businessId}/customers/${customerId}/profile`); }
export async function updateCustomer(businessId: string, customerId: string, request: { firstName: string; lastName?: string; mobile: string; birthDate?: string; note?: string; isActive?: boolean }) { return apiFetch<Customer>(`/api/businesses/${businessId}/customers/${customerId}`, { method: "PUT", body: JSON.stringify(request) }); }
export async function createReminder(businessId: string, request: { customerId: string; serviceId?: string; title: string; dueAt: string; note?: string }) { return apiFetch<Reminder>(`/api/businesses/${businessId}/reminders`, { method: "POST", body: JSON.stringify(request) }); }
export async function getReminders(businessId: string, status: number | null = null, page = 1, pageSize = 20) { const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) }); if (status !== null) params.set("status", String(status)); return apiFetch<PagedResult<Reminder>>(`/api/businesses/${businessId}/reminders?${params.toString()}`); }
export async function completeReminder(businessId: string, reminderId: string) { return apiFetch<Reminder>(`/api/businesses/${businessId}/reminders/${reminderId}/complete`, { method: "POST" }); }
export async function cancelReminder(businessId: string, reminderId: string) { return apiFetch<Reminder>(`/api/businesses/${businessId}/reminders/${reminderId}/cancel`, { method: "POST" }); }
export async function createCustomer(businessId: string, request: { firstName: string; lastName?: string; mobile: string; birthDate?: string; note?: string }) { return apiFetch<Customer>(`/api/businesses/${businessId}/customers`, { method: "POST", body: JSON.stringify(request) }); }
export async function getServices(businessId: string, page = 1, pageSize = 20, search = "", isActive: boolean | null = true) { const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) }); if (search.trim()) params.set("search", search.trim()); if (isActive !== null) params.set("isActive", String(isActive)); return apiFetch<PagedResult<Service>>(`/api/businesses/${businessId}/services?${params.toString()}`); }
export async function createService(businessId: string, request: { title: string; description?: string; defaultPrice: number; defaultDurationMinutes: number; suggestedReturnDays?: number }) { return apiFetch<Service>(`/api/businesses/${businessId}/services`, { method: "POST", body: JSON.stringify(request) }); }
export async function updateService(businessId: string, serviceId: string, request: { title: string; description?: string; defaultPrice: number; defaultDurationMinutes: number; suggestedReturnDays?: number; isActive: boolean }) { return apiFetch<Service>(`/api/businesses/${businessId}/services/${serviceId}`, { method: "PUT", body: JSON.stringify(request) }); }
export async function getStaff(businessId: string, page = 1, pageSize = 20, search = "", isActive: boolean | null = true) { const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) }); if (search.trim()) params.set("search", search.trim()); if (isActive !== null) params.set("isActive", String(isActive)); return apiFetch<PagedResult<Staff>>(`/api/businesses/${businessId}/staff?${params.toString()}`); }
export async function createStaff(businessId: string, request: { firstName: string; lastName: string; mobile?: string }) { return apiFetch<Staff>(`/api/businesses/${businessId}/staff`, { method: "POST", body: JSON.stringify(request) }); }
export async function updateStaff(businessId: string, staffId: string, request: { firstName: string; lastName: string; mobile?: string; isActive: boolean }) { return apiFetch<Staff>(`/api/businesses/${businessId}/staff/${staffId}`, { method: "PUT", body: JSON.stringify(request) }); }
