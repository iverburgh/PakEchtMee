"use server";

import { revalidatePath } from "next/cache";
import { ApiError, api } from "@/lib/api";
import type { CatalogItem, Category, TripItem, TripSummary } from "@/lib/types";

export type ActionResult<T> =
  | { ok: true; value: T }
  | { ok: false; error: string };

/** Turns any API failure into a message the UI can show; the caller then restores the previous state. */
async function run<T>(operation: () => Promise<T>): Promise<ActionResult<T>> {
  try {
    return { ok: true, value: await operation() };
  } catch (error) {
    if (error instanceof ApiError) {
      return { ok: false, error: error.message };
    }

    return { ok: false, error: "Geen verbinding met de server. De wijziging is niet opgeslagen." };
  }
}

export async function createCategoryAction(name: string): Promise<ActionResult<Category>> {
  const result = await run(() => api.createCategory(name));
  revalidatePath("/catalogus");

  return result;
}

export async function renameCategoryAction(id: string, name: string): Promise<ActionResult<Category>> {
  const result = await run(() => api.renameCategory(id, name));
  revalidatePath("/catalogus");

  return result;
}

export async function removeCategoryAction(id: string): Promise<ActionResult<Category>> {
  const result = await run(() => api.removeCategory(id));
  revalidatePath("/catalogus");

  return result;
}

export async function restoreCategoryAction(id: string): Promise<ActionResult<Category>> {
  const result = await run(() => api.restoreCategory(id));
  revalidatePath("/catalogus");

  return result;
}

export async function createItemAction(input: {
  categoryId: string;
  name: string;
  quantity: number | null;
  unit: string | null;
}): Promise<ActionResult<CatalogItem>> {
  const result = await run(() => api.createItem(input));
  revalidatePath(`/catalogus/${input.categoryId}`);

  return result;
}

export async function updateItemAction(
  categoryId: string,
  id: string,
  input: { name: string; quantity: number | null; unit: string | null },
): Promise<ActionResult<CatalogItem>> {
  const result = await run(() => api.updateItem(id, input));
  revalidatePath(`/catalogus/${categoryId}`);

  return result;
}

export async function moveItemAction(
  categoryId: string,
  id: string,
  targetCategoryId: string,
): Promise<ActionResult<CatalogItem>> {
  const result = await run(() => api.moveItem(id, targetCategoryId));
  revalidatePath(`/catalogus/${categoryId}`);
  revalidatePath(`/catalogus/${targetCategoryId}`);

  return result;
}

export async function removeItemAction(categoryId: string, id: string): Promise<ActionResult<CatalogItem>> {
  const result = await run(() => api.removeItem(id));
  revalidatePath(`/catalogus/${categoryId}`);

  return result;
}

export async function restoreItemAction(categoryId: string, id: string): Promise<ActionResult<CatalogItem>> {
  const result = await run(() => api.restoreItem(id));
  revalidatePath(`/catalogus/${categoryId}`);

  return result;
}

export async function createTripAction(input: {
  name: string;
  startDate: string | null;
  endDate: string | null;
}): Promise<ActionResult<TripSummary>> {
  const result = await run(() => api.createTrip(input));
  revalidatePath("/");

  return result;
}

export async function selectTripCategoriesAction(
  tripId: string,
  categoryIds: string[],
): Promise<ActionResult<null>> {
  const result = await run(() => api.selectTripCategories(tripId, categoryIds));
  revalidatePath(`/trips/${tripId}`);

  return result.ok ? { ok: true, value: null } : result;
}

export async function removeTripCategoryAction(
  tripId: string,
  categoryId: string,
): Promise<ActionResult<null>> {
  const result = await run(() => api.removeTripCategory(tripId, categoryId));
  revalidatePath(`/trips/${tripId}`);

  return result.ok ? { ok: true, value: null } : result;
}

export async function advanceTripItemAction(tripId: string, itemId: string): Promise<ActionResult<TripItem>> {
  const result = await run(() => api.advanceItem(tripId, itemId));
  revalidatePath(`/trips/${tripId}`);

  return result;
}

export async function revertTripItemAction(tripId: string, itemId: string): Promise<ActionResult<TripItem>> {
  const result = await run(() => api.revertItem(tripId, itemId));
  revalidatePath(`/trips/${tripId}`);

  return result;
}

export async function deleteTripItemAction(tripId: string, itemId: string): Promise<ActionResult<TripItem>> {
  const result = await run(() => api.deleteItem(tripId, itemId));
  revalidatePath(`/trips/${tripId}`);

  return result;
}

export async function restoreTripItemAction(tripId: string, itemId: string): Promise<ActionResult<TripItem>> {
  const result = await run(() => api.restoreTripItem(tripId, itemId));
  revalidatePath(`/trips/${tripId}`);

  return result;
}
