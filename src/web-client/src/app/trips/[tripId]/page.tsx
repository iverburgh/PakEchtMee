import Link from "next/link";
import { CalendarDays, ChevronLeft, Layers, Printer } from "lucide-react";
import { api } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { TripWorkspace } from "@/components/trips/trip-workspace";

export default async function TripPage({ params }: PageProps<"/trips/[tripId]">) {
  const { tripId } = await params;
  const trip = await api.getTrip(tripId);

  const range = [trip.startDate, trip.endDate]
    .filter((value): value is string => value !== null)
    .map((value) => new Date(`${value}T00:00:00`).toLocaleDateString("nl-NL", { day: "numeric", month: "short" }))
    .join(" - ");

  return (
    <>
      <header className="flex items-center gap-2 px-2 pt-4 pb-2">
        <Link
          href="/"
          className="text-muted-foreground hover:text-foreground flex size-11 shrink-0 items-center justify-center rounded-md"
          aria-label="Terug naar uitjes"
        >
          <ChevronLeft aria-hidden />
        </Link>
        <div className="min-w-0 flex-1">
          <h1 className="truncate text-xl font-semibold tracking-tight">{trip.name}</h1>
          {range ? (
            <p className="text-muted-foreground flex items-center gap-1.5 text-xs">
              <CalendarDays className="size-3.5" aria-hidden />
              {range}
            </p>
          ) : null}
        </div>
        <Button
          render={<Link href={`/trips/${tripId}/categorieen`} aria-label="Categorieën kiezen" />}
          nativeButton={false}
          variant="ghost"
          size="icon"
          className="size-11"
        >
          <Layers aria-hidden />
        </Button>
        <Button
          render={<Link href={`/trips/${tripId}/overzicht`} aria-label="Overzicht en printen" />}
          nativeButton={false}
          variant="ghost"
          size="icon"
          className="size-11"
        >
          <Printer aria-hidden />
        </Button>
      </header>

      <TripWorkspace trip={trip} />
    </>
  );
}
