import Link from "next/link";
import { ChevronLeft } from "lucide-react";
import { api } from "@/lib/api";
import { describeAmount, statusLabels, type PackingStatus, type TripItem } from "@/lib/types";
import { PrintButton } from "@/components/trips/print-button";

function toArray(value: string | string[] | undefined): string[] {
  if (value === undefined) {
    return [];
  }

  return Array.isArray(value) ? value : [value];
}

function group(items: TripItem[]): Map<string, TripItem[]> {
  const grouped = new Map<string, TripItem[]>();

  for (const item of items) {
    const current = grouped.get(item.categoryName) ?? [];
    current.push(item);
    grouped.set(item.categoryName, current);
  }

  return new Map([...grouped].sort(([left], [right]) => left.localeCompare(right, "nl")));
}

export default async function TripOverviewPage({
  params,
  searchParams,
}: PageProps<"/trips/[tripId]/overzicht">) {
  const { tripId } = await params;
  const query = await searchParams;

  const categoryIds = toArray(query.categoryId);
  const statuses = toArray(query.status) as PackingStatus[];

  const trip = await api.getTrip(tripId);
  const visible = trip.items.filter(
    (item) =>
      !item.isDeleted &&
      (categoryIds.length === 0 || categoryIds.includes(item.categoryId)) &&
      (statuses.length === 0 || statuses.includes(item.status)),
  );

  const grouped = group(visible);
  const range = [trip.startDate, trip.endDate]
    .filter((value): value is string => value !== null)
    .map((value) => new Date(`${value}T00:00:00`).toLocaleDateString("nl-NL", { day: "numeric", month: "long", year: "numeric" }))
    .join(" - ");

  const filterSummary = [
    categoryIds.length > 0
      ? `categorie: ${trip.categories
          .filter((category) => categoryIds.includes(category.categoryId))
          .map((category) => category.name)
          .join(", ")}`
      : null,
    statuses.length > 0 ? `status: ${statuses.map((status) => statusLabels[status]).join(", ")}` : null,
  ].filter((value): value is string => value !== null);

  return (
    <>
      <header className="flex items-center gap-2 px-2 pt-4 pb-2 print:hidden">
        <Link
          href={`/trips/${tripId}`}
          className="text-muted-foreground hover:text-foreground flex size-11 items-center justify-center rounded-md"
          aria-label="Terug naar paklijst"
        >
          <ChevronLeft aria-hidden />
        </Link>
        <h1 className="flex-1 truncate text-xl font-semibold tracking-tight">Overzicht</h1>
        <PrintButton />
      </header>

      <main className="print-sheet flex flex-col gap-6 px-4 pb-8 print:px-0">
        <section className="flex flex-col gap-1 border-b pb-3">
          <h2 className="text-2xl font-semibold tracking-tight">{trip.name}</h2>
          {range ? <p className="text-muted-foreground text-sm">{range}</p> : null}
          <p className="text-muted-foreground text-sm">
            {trip.progress.loaded} van {trip.progress.total} ingeladen
            {" · "}
            {statusLabels.Active}: {trip.progress.active}
            {" · "}
            {statusLabels.Prepared}: {trip.progress.prepared}
            {" · "}
            {statusLabels.Packed}: {trip.progress.packed}
          </p>
          {filterSummary.length > 0 ? (
            <p className="text-sm font-medium">
              Let op: dit is een gefilterd overzicht ({filterSummary.join("; ")}).
            </p>
          ) : null}
        </section>

        {grouped.size === 0 ? (
          <p className="text-muted-foreground text-sm">
            Deze paklijst is leeg. Kies eerst categorieën die mee moeten.
          </p>
        ) : (
          [...grouped].map(([categoryName, categoryItems]) => (
            <section key={categoryName} className="break-inside-avoid">
              <h3 className="border-b pb-1 text-base font-semibold">{categoryName}</h3>
              <ul className="divide-y">
                {categoryItems.map((item) => {
                  const amount = describeAmount(item.quantity, item.unit);

                  return (
                    <li key={item.id} className="flex items-center gap-3 py-2">
                      <span
                        aria-hidden
                        className="border-foreground inline-block size-4 shrink-0 border print:border-black"
                      />
                      <span className="flex-1">
                        {item.name}
                        {amount ? <span className="text-muted-foreground"> {amount}</span> : null}
                      </span>
                      {/* Text rather than colour, so the printed sheet stays readable in black and white. */}
                      <span className="text-muted-foreground text-xs uppercase">{statusLabels[item.status]}</span>
                    </li>
                  );
                })}
              </ul>
            </section>
          ))
        )}
      </main>
    </>
  );
}
