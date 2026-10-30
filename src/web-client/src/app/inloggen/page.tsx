import { redirect } from "next/navigation";
import { Luggage, TriangleAlert } from "lucide-react";
import { isGoogleConfigured } from "@/auth";
import { currentEmail } from "@/lib/session";
import { Card } from "@/components/ui/card";
import { SignInButton } from "@/components/auth/sign-in-button";

export const metadata = { title: "Inloggen - PakEchtMee" };

/** Only same-site paths are accepted as a return address, so the sign-in page cannot be used to redirect elsewhere. */
function safeReturnPath(next: string | string[] | undefined): string {
  const value = Array.isArray(next) ? next[0] : next;

  return value && value.startsWith("/") && !value.startsWith("//") ? value : "/";
}

const errorMessages: Record<string, string> = {
  EmailNotVerified: "Google heeft het e-mailadres van dit account niet bevestigd. Gebruik een ander account.",
  Callback: "Inloggen is niet gelukt. Probeer het opnieuw.",
  OAuthCallback: "Inloggen is niet gelukt. Probeer het opnieuw.",
  AccessDenied: "Dit account heeft geen toegang.",
};

export default async function SignInPage({ searchParams }: PageProps<"/inloggen">) {
  const params = await searchParams;
  const callbackUrl = safeReturnPath(params.next ?? params.callbackUrl);

  if (await currentEmail()) {
    redirect(callbackUrl);
  }

  const errorCode = Array.isArray(params.error) ? params.error[0] : params.error;
  const error = errorCode ? (errorMessages[errorCode] ?? "Inloggen is niet gelukt. Probeer het opnieuw.") : null;
  const configured = isGoogleConfigured();

  return (
    <main className="flex flex-1 flex-col items-center justify-center px-6 py-12">
      <Card className="flex w-full max-w-sm flex-col items-center gap-6 p-8 text-center">
        <div className="bg-primary/10 text-primary flex size-16 items-center justify-center rounded-2xl">
          <Luggage className="size-8" aria-hidden />
        </div>
        <div className="flex flex-col gap-1">
          <h1 className="text-2xl font-semibold tracking-tight">PakEchtMee</h1>
          <p className="text-muted-foreground text-sm">Log in met het Google-account dat de beheerder aan je familie heeft toegevoegd.</p>
        </div>

        {error ? (
          <p role="alert" className="bg-destructive/10 text-destructive w-full rounded-lg px-3 py-2 text-sm">
            {error}
          </p>
        ) : null}

        {configured ? (
          <SignInButton callbackUrl={callbackUrl} />
        ) : (
          <p className="text-muted-foreground flex items-start gap-2 text-left text-sm">
            <TriangleAlert className="mt-0.5 size-4 shrink-0" aria-hidden />
            Inloggen met Google is nog niet ingesteld. Vul de Aspire-parameters in volgens docs/google-sign-in.md.
          </p>
        )}
      </Card>
    </main>
  );
}
