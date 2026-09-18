import "server-only";

import type {
  Category,
  CatalogItem,
  TripDetails,
  TripSummary,
  TripItem,
} from "@/lib/types";

/**
 * Aspire injects the API address as `services__web__http__0`; the fallback keeps a bare
 * `npm run dev` working against a locally started API.
 */
function baseUrl(): string {
  const url =
    process.env.services__web__https__0 ??
    process.env.services__web__http__0 ??
    process.env.API_BASE_URL ??
    "http://localhost:5080";

  return url.replace(/\/$/, "");
}

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

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${baseUrl()}${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", ...init?.headers },
    cache: "no-store",
  });

  if (!response.ok) {
    throw await toApiError(response);
  }

  return (await response.json()) as T;
}

export const api = {
  listCategories: (includeDeleted = false) =>
    request<Category[]>(`/api/categories?includeDeleted=${includeDeleted}`),

  createCategory: (name: string) =>
    request<Category>("/api/categories", { method: "POST", body: JSON.stringify({ name }) }),

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
};
