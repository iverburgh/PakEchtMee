export type PackingStatus = "Active" | "Prepared" | "Packed" | "Loaded" | "Deleted";

export const progressStatuses: PackingStatus[] = ["Active", "Prepared", "Packed", "Loaded"];

export const statusLabels: Record<PackingStatus, string> = {
  Active: "Te doen",
  Prepared: "Klaargelegd",
  Packed: "Ingepakt",
  Loaded: "Ingeladen",
  Deleted: "Verwijderd",
};

export type Category = {
  id: string;
  name: string;
  isDeleted: boolean;
};

export type CatalogItem = {
  id: string;
  categoryId: string;
  name: string;
  quantity: number | null;
  unit: string | null;
  isDeleted: boolean;
};

export type TripProgress = {
  total: number;
  active: number;
  prepared: number;
  packed: number;
  loaded: number;
  loadedShare: number;
};

export type TripSummary = {
  id: string;
  name: string;
  startDate: string | null;
  endDate: string | null;
  progress: TripProgress;
};

export type TripCategory = {
  categoryId: string;
  name: string;
  hasProgressedItems: boolean;
};

export type TripItem = {
  id: string;
  categoryId: string;
  categoryName: string;
  name: string;
  quantity: number | null;
  unit: string | null;
  status: PackingStatus;
  nextStatus: PackingStatus | null;
  previousStatus: PackingStatus | null;
  isDeleted: boolean;
};

export type TripDetails = {
  id: string;
  name: string;
  startDate: string | null;
  endDate: string | null;
  categories: TripCategory[];
  items: TripItem[];
  progress: TripProgress;
};

/** Renders "Sokken 7 paar", "Handdoek 2" or just "Paspoort". */
export function describeAmount(quantity: number | null, unit: string | null): string {
  if (quantity === null) {
    return "";
  }

  return unit === null ? `${quantity}` : `${quantity} ${unit}`;
}
