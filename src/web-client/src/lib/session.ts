import "server-only";

import { cache } from "react";
import { getServerSession } from "next-auth";
import { redirect } from "next/navigation";
import { authOptions } from "@/auth";

/** The signed-in address, or null; memoised per request so every component can ask without extra work. */
export const currentEmail = cache(async (): Promise<string | null> => {
  const session = await getServerSession(authOptions);

  return session?.user?.email?.toLowerCase() ?? null;
});

/**
 * The Data Access Layer check: every API call goes through here, so data is never fetched without a session.
 * Proxy only redirects optimistically; this is the check that counts on the frontend side.
 */
export const verifySession = cache(async (): Promise<string> => {
  const email = await currentEmail();

  if (!email) {
    redirect("/inloggen");
  }

  return email;
});
