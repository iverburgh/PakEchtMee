import { cookies } from "next/headers";
import { ShieldX } from "lucide-react";
import { deniedEmailCookie } from "@/auth";
import { currentEmail } from "@/lib/session";
import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { SignOutButton } from "@/components/auth/sign-out-button";

export const metadata = { title: "Geen toegang - PakEchtMee" };

/**
 * Shown for a refused sign-in (the address arrives in a short-lived cookie, never in the URL) and for a member who
 * was removed while signed in (the address is still in the session).
 */
export default async function NoAccessPage() {
  const refused = (await cookies()).get(deniedEmailCookie)?.value ?? null;
  const signedIn = await currentEmail();
  const email = signedIn ?? refused;

  return (
    <main className="flex flex-1 flex-col items-center justify-center px-6 py-12">
      <Card className="flex w-full max-w-sm flex-col items-center gap-5 p-8 text-center">
        <ShieldX className="text-destructive size-12" aria-hidden />
        <div className="flex flex-col gap-2">
          <h1 className="text-xl font-semibold">Geen toegang</h1>
          <p className="text-muted-foreground text-sm">
            {email ? (
              <>
                <span className="text-foreground font-medium break-all">{email}</span> hoort (nog) niet bij een familie in
                PakEchtMee.
              </>
            ) : (
              "Dit account hoort (nog) niet bij een familie in PakEchtMee."
            )}{" "}
            Vraag de beheerder om dit adres toe te voegen.
          </p>
        </div>
        {signedIn ? (
          <SignOutButton label="Inloggen met een ander account" />
        ) : (
          <Button variant="outline" className="h-11 w-full" nativeButton={false} render={<a href="/inloggen" />}>
            Inloggen met een ander account
          </Button>
        )}
      </Card>
    </main>
  );
}
