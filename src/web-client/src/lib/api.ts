import "server-only";

import { cache } from "react";
import { redirect } from "next/navigation";
import { apiBaseUrl, apiToken } from "@/lib/api-token";
import { verifySession } from "@/lib/session";
import type {
  Category,
  CatalogItem,
  Family,
  Me,
  TripDetails,
  TripSummary,
  TripItem,
} from "@/lib/types";

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

type ProblemDetails = {
  title?: string;
  detail?: string;
};

async function toApiError(response: Response): Promise<ApiError> {
  let message = "De actie kon niet worden uitgevoerd.";

  try {
    const problem = (await response.json()) as ProblemDetails;
    message = problem.detail ?? problem.title ?? message;
  } catch {
    // A non-JSON body means we keep the generic message.
  }

  return new ApiError(message, response.status);
}

async function send(path: string, init?: RequestInit): Promise<Response> {
  const email = await verifySession();

  return fetch(`${apiBaseUrl()}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${await apiToken(email)}`,
      ...init?.headers,
    },
    cache: "no-store",
  });
}

/**
 * Who the caller is, or null when the API does not know the account (any more). Memoised per request, so the
 * navigation and the page share one call.
 */
const getMe = cache(async (): Promise<Me | null> => {
  const response = await send("/api/me");

  if (response.status === 403) {
    return null;
  }

  if (!response.ok) {
    throw await toApiError(response);
  }

  return (await response.json()) as Me;
});

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await send(path, init);

  if (response.status === 401) {
    redirect("/inloggen");
  }

  // A 403 is either "this account has no access at all" (removed member) or a rule such as "only the
  // administrator edits general categories"; only the first leaves the app.
  if (response.status === 403 && (await getMe()) === null) {
    redirect("/geen-toegang");
  }

  if (!response.ok) {
    throw await toApiError(response);
  }

  return (await response.json()) as T;
}

export const api = {
  me: getMe,

  listCategories: (includeDeleted = false, includeSwitchedOff = false) =>
    request<Category[]>(`/api/categories?includeDeleted=${includeDeleted}&includeSwitchedOff=${includeSwitchedOff}`),

  listGeneralCategories: (includeDeleted = false) =>
    request<Category[]>(`/api/categories/general?includeDeleted=${includeDeleted}`),

  createCategory: (name: string, general = false) =>
    request<Category>("/api/categories", { method: "POST", body: JSON.stringify({ name, general }) }),

  switchCategory: (id: string, on: boolean) =>
    request<Category>(`/api/categories/${id}/switch`, { method: "PUT", body: JSON.stringify({ on }) }),

  renameCategory: (id: string, name: string) =>
    request<Category>(`/api/categories/${id}`, { method: "PUT", body: JSON.stringify({ name }) }),

  removeCategory: (id: string) => request<Category>(`/api/categories/${id}`, { method: "DELETE" }),

  restoreCategory: (id: string) =>
    request<Category>(`/api/categories/${id}/restore`, { method: "POST" }),

  listItems: (categoryId: string, includeDeleted = false) =>
    request<CatalogItem[]>(`/api/items?categoryId=${categoryId}&includeDeleted=${includeDeleted}`),

  createItem: (input: { categoryId: string; name: string; quantity: number | null; unit: string | null }) =>
    request<CatalogItem>("/api/items", { method: "POST", body: JSON.stringify(input) }),

  updateItem: (id: string, input: { name: string; quantity: number | null; unit: string | null }) =>
    request<CatalogItem>(`/api/items/${id}`, { method: "PUT", body: JSON.stringify(input) }),

  moveItem: (id: string, targetCategoryId: string) =>
    request<CatalogItem>(`/api/items/${id}/move`, {
      method: "POST",
      body: JSON.stringify({ targetCategoryId }),
    }),

  removeItem: (id: string) => request<CatalogItem>(`/api/items/${id}`, { method: "DELETE" }),

  restoreItem: (id: string) => request<CatalogItem>(`/api/items/${id}/restore`, { method: "POST" }),

  listTrips: () => request<TripSummary[]>("/api/trips"),

  getTrip: (tripId: string) => request<TripDetails>(`/api/trips/${tripId}`),

  createTrip: (input: { name: string; startDate: string | null; endDate: string | null }) =>
    request<TripSummary>("/api/trips", { method: "POST", body: JSON.stringify(input) }),

  selectTripCategories: (tripId: string, categoryIds: string[]) =>
    request<TripDetails>(`/api/trips/${tripId}/categories`, {
      method: "POST",
      body: JSON.stringify({ categoryIds }),
    }),

  removeTripCategory: (tripId: string, categoryId: string) =>
    request<TripDetails>(`/api/trips/${tripId}/categories/${categoryId}`, { method: "DELETE" }),

  advanceItem: (tripId: string, itemId: string) =>
    request<TripItem>(`/api/trips/${tripId}/items/${itemId}/advance`, { method: "POST" }),

  revertItem: (tripId: string, itemId: string) =>
    request<TripItem>(`/api/trips/${tripId}/items/${itemId}/revert`, { method: "POST" }),

  deleteItem: (tripId: string, itemId: string) =>
    request<TripItem>(`/api/trips/${tripId}/items/${itemId}/delete`, { method: "POST" }),

  restoreTripItem: (tripId: string, itemId: string) =>
    request<TripItem>(`/api/trips/${tripId}/items/${itemId}/restore`, { method: "POST" }),

  listFamilies: () => request<Family[]>("/api/admin/families"),

  createFamily: (name: string) =>
    request<Family>("/api/admin/families", { method: "POST", body: JSON.stringify({ name }) }),

  renameFamily: (id: string, name: string) =>
    request<Family>(`/api/admin/families/${id}`, { method: "PUT", body: JSON.stringify({ name }) }),

  addMember: (familyId: string, email: string) =>
    request<Family>(`/api/admin/families/${familyId}/members`, { method: "POST", body: JSON.stringify({ email }) }),

  removeMember: (familyId: string, email: string) =>
    request<Family>(`/api/admin/families/${familyId}/members/${encodeURIComponent(email)}`, { method: "DELETE" }),
};
