import Link from "next/link";
import { ChevronLeft } from "lucide-react";
import { api } from "@/lib/api";
import { CategoryPicker } from "@/components/trips/category-picker";

export default async function TripCategoriesPage({ params }: PageProps<"/trips/[tripId]/categorieen">) {
  const { tripId } = await params;
  const [trip, categories] = await Promise.all([api.getTrip(tripId), api.listCategories()]);

  return (
    <>
      <header className="flex items-center gap-2 px-2 pt-4 pb-2">
        <Link
          href={`/trips/${tripId}`}
          className="text-muted-foreground hover:text-foreground flex size-11 items-center justify-center rounded-md"
          aria-label="Terug naar paklijst"
        >
          <ChevronLeft aria-hidden />
        </Link>
        <div className="min-w-0">
          <h1 className="truncate text-xl font-semibold tracking-tight">Categorieën</h1>
          <p className="text-muted-foreground text-xs">Wat moet er mee voor {trip.name}?</p>
        </div>
      </header>

      <main className="px-4">
        <CategoryPicker trip={trip} categories={categories} />
      </main>
    </>
  );
}
