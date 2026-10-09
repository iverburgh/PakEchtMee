import Link from "next/link";
import { SearchX } from "lucide-react";

export default function NotFound() {
  return (
    <main className="flex flex-1 flex-col items-center justify-center gap-4 px-6 text-center">
      <SearchX className="text-muted-foreground size-12" aria-hidden />
      <h1 className="text-xl font-semibold">Niet gevonden</h1>
      <p className="text-muted-foreground max-w-sm text-sm">
        Deze pagina bestaat niet (meer). Misschien is de link verouderd.
      </p>
      <Link
        href="/"
        className="bg-primary text-primary-foreground inline-flex min-h-11 items-center rounded-lg px-6 text-sm font-medium"
      >
        Naar je uitjes
      </Link>
    </main>
  );
}
