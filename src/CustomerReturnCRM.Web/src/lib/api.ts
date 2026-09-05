const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5000";

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(init.headers ?? {}),
    },
  });

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

export async function login(email: string, password: string) {
  return apiFetch<AuthenticationResult>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password }),
  });
}
