import "server-only";

import type { NextAuthOptions } from "next-auth";
import GoogleProvider, { type GoogleProfile } from "next-auth/providers/google";
import { cookies } from "next/headers";
import { apiBaseUrl, apiToken } from "@/lib/api-token";

/** Holds the refused address for the no-access page, so it never has to travel in a URL. */
export const deniedEmailCookie = "pem-denied-email";

/** Whether the API knows this address as administrator or family member; anything unexpected is an error, not a "no". */
async function isKnownAccount(email: string): Promise<boolean> {
  const response = await fetch(`${apiBaseUrl()}/api/me`, {
    headers: { Authorization: `Bearer ${await apiToken(email)}` },
    cache: "no-store",
  });

  if (response.status === 403) {
    return false;
  }

  if (!response.ok) {
    throw new Error(`De toegangscontrole gaf status ${response.status}.`);
  }

  return true;
}

export const authOptions: NextAuthOptions = {
  providers: [
    GoogleProvider({
      clientId: process.env.GOOGLE_CLIENT_ID ?? "",
      clientSecret: process.env.GOOGLE_CLIENT_SECRET ?? "",
    }),
  ],
  // An encrypted cookie holds the session; membership is checked by the API on every call, not stored here.
  session: { strategy: "jwt" },
  pages: { signIn: "/inloggen", error: "/inloggen" },
  callbacks: {
    async signIn({ account, profile }) {
      const google = profile as GoogleProfile | undefined;
      const email = google?.email?.toLowerCase();

      if (account?.provider !== "google" || !email || google?.email_verified !== true) {
        return "/inloggen?error=EmailNotVerified";
      }

      if (await isKnownAccount(email)) {
        return true;
      }

      (await cookies()).set(deniedEmailCookie, email, {
        httpOnly: true,
        sameSite: "lax",
        path: "/",
        maxAge: 300,
      });

      return "/geen-toegang";
    },
    session({ session }) {
      if (session.user?.email) {
        session.user.email = session.user.email.toLowerCase();
      }

      return session;
    },
  },
};

/** True when the Google client is configured; the sign-in page explains what is missing otherwise. */
export function isGoogleConfigured(): boolean {
  return Boolean(process.env.GOOGLE_CLIENT_ID && process.env.GOOGLE_CLIENT_SECRET && process.env.NEXTAUTH_SECRET);
}
