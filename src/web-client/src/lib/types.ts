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
  /** Maintained by the administrator and shared with every family; read-only for members. */
  isGeneral: boolean;
  /** For a general category: whether the caller's family uses it. Own categories are always on. */
  isSwitchedOn: boolean;
};

export type Me = {
  email: string;
  isAdministrator: boolean;
  family: { id: string; name: string } | null;
};

export type Family = {
  id: string;
  name: string;
  members: string[];
};

/** What a quantity is counted against: a fixed number, or a number for every person, adult or child on a trip. */
export type QuantityBasis = "Fixed" | "PerPerson" | "PerAdult" | "PerChild";

export const quantityBases: QuantityBasis[] = ["Fixed", "PerPerson", "PerAdult", "PerChild"];

export const basisLabels: Record<QuantityBasis, string> = {
  Fixed: "Vast",
  PerPerson: "Per persoon",
  PerAdult: "Per volwassene",
  PerChild: "Per kind",
};

const basisSuffixes: Record<QuantityBasis, string> = {
  Fixed: "",
  PerPerson: "per persoon",
  PerAdult: "per volwassene",
  PerChild: "per kind",
};

/**
 * Chip colours per basis. Rose, violet and teal are not used by the status badges (slate, amber, sky, emerald),
 * so a rule is never mistaken for a status.
 */
export const basisTone: Record<Exclude<QuantityBasis, "Fixed">, string> = {
  PerPerson: "bg-teal-100 text-teal-800 dark:bg-teal-900/40 dark:text-teal-200",
  PerAdult: "bg-violet-100 text-violet-800 dark:bg-violet-900/40 dark:text-violet-200",
  PerChild: "bg-rose-100 text-rose-800 dark:bg-rose-900/40 dark:text-rose-200",
};

export type Composition = { adults: number; children: number };

export type CatalogItem = {
  id: string;
  categoryId: string;
  name: string;
  quantity: number | null;
  unit: string | null;
  basis: QuantityBasis;
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
  basis: QuantityBasis;
  /** How many to pack on this trip: the quantity times the heads its basis counts. Null without a quantity. */
  total: number | null;
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
  adults: number;
  children: number;
  categories: TripCategory[];
  items: TripItem[];
  progress: TripProgress;
};

/** Renders "7 paar", "2" or "" for an item without a quantity. */
export function describeAmount(quantity: number | null, unit: string | null): string {
  if (quantity === null) {
    return "";
  }

  return unit === null ? `${quantity}` : `${quantity} ${unit}`;
}

/** Renders the rule: "20", "1 per kind" or "2 liter per persoon"; "" without a quantity. */
export function describeRule(quantity: number | null, unit: string | null, basis: QuantityBasis): string {
  const amount = describeAmount(quantity, unit);

  return amount === "" || basis === "Fixed" ? amount : `${amount} ${basisSuffixes[basis]}`;
}

/** Renders "2 volwassenen · 3 kinderen", with singular forms and without a group of zero. */
export function describeComposition({ adults, children }: Composition): string {
  return [
    adults > 0 ? `${adults} ${adults === 1 ? "volwassene" : "volwassenen"}` : null,
    children > 0 ? `${children} ${children === 1 ? "kind" : "kinderen"}` : null,
  ]
    .filter((part) => part !== null)
    .join(" · ");
}
