import Link from "next/link";
import { CalendarDays, ChevronRight, Luggage } from "lucide-react";
import { api } from "@/lib/api";
import type { TripSummary } from "@/lib/types";
import { Card } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";
import { PageHeader } from "@/components/page-header";
import { CreateTripDialog } from "@/components/trips/create-trip-dialog";

function formatRange(trip: TripSummary): string | null {
  const format = (value: string) =>
    new Date(`${value}T00:00:00`).toLocaleDateString("nl-NL", { day: "numeric", month: "short" });

  if (trip.startDate && trip.endDate) {
    return `${format(trip.startDate)} - ${format(trip.endDate)}`;
  }

  if (trip.startDate) {
    return format(trip.startDate);
  }

  return trip.endDate ? format(trip.endDate) : null;
}

export default async function TripsPage() {
  const trips = await api.listTrips();

  return (
    <>
      <PageHeader title="Uitjes" description="Je vakanties en dagjes weg.">
        <CreateTripDialog />
      </PageHeader>

      <main className="flex flex-col gap-3 px-4">
        {trips.length === 0 ? (
          <Card className="flex flex-col items-center gap-3 p-8 text-center">
            <Luggage className="text-muted-foreground size-10" aria-hidden />
            <p className="text-muted-foreground text-sm">
              Nog geen uitjes. Maak er een aan en kies welke categorieën mee moeten.
            </p>
          </Card>
        ) : (
          trips.map((trip) => {
            const range = formatRange(trip);

            return (
              <Link key={trip.id} href={`/trips/${trip.id}`} className="rounded-xl">
                <Card className="hover:bg-accent/50 flex min-h-20 flex-row items-center justify-between gap-4 p-4 transition-colors motion-reduce:transition-none">
                  <div className="flex min-w-0 flex-col gap-1">
                    <span className="truncate font-medium">{trip.name}</span>
                    {range ? (
                      <span className="text-muted-foreground flex items-center gap-1.5 text-xs">
                        <CalendarDays className="size-3.5" aria-hidden />
                        {range}
                      </span>
                    ) : null}
                    <div className="flex items-center gap-2 pt-1">
                      <Progress
                        value={trip.progress.loadedShare * 100}
                        className="h-1.5 w-24"
                        aria-label={`${trip.progress.loaded} van ${trip.progress.total} ingeladen`}
                      />
                      <span className="text-muted-foreground text-xs tabular-nums">
                        {trip.progress.loaded}/{trip.progress.total}
                      </span>
                    </div>
                  </div>
                  <ChevronRight className="text-muted-foreground size-5 shrink-0" aria-hidden />
                </Card>
              </Link>
            );
          })
        )}
      </main>
    </>
  );
}
