import { NextResponse, type NextRequest } from "next/server";
import { publicPageHeader } from "@/lib/public-page";

/** next-auth uses the secure name on HTTPS and the plain one on http://localhost. */
const sessionCookies = ["next-auth.session-token", "__Secure-next-auth.session-token"];

/** Pages a visitor without a session may open. */
const publicPaths = ["/inloggen", "/geen-toegang", "/offline"];

/**
 * Optimistic check only: no session cookie means "go sign in first". It never calls the API (Proxy also runs on
 * prefetches); whether the account may actually see data is decided by the API on every call.
 */
export function proxy(request: NextRequest) {
  const { pathname, search } = request.nextUrl;

  if (publicPaths.some((path) => pathname === path || pathname.startsWith(`${path}/`))) {
    const headers = new Headers(request.headers);
    headers.set(publicPageHeader, "1");

    return NextResponse.next({ request: { headers } });
  }

  if (sessionCookies.some((name) => request.cookies.has(name))) {
    // The marker is ours to set; a browser sending it itself must not hide anything.
    const headers = new Headers(request.headers);
    headers.delete(publicPageHeader);

    return NextResponse.next({ request: { headers } });
  }

  const signIn = new URL("/inloggen", request.url);
  signIn.searchParams.set("next", `${pathname}${search}`);

  return NextResponse.redirect(signIn);
}

export const config = {
  // Skips the auth routes themselves and static files, so CSS, icons and the service worker always load.
  matcher: ["/((?!api/auth|_next/static|_next/image|favicon.ico|sw.js|manifest.webmanifest|icons/).*)"],
};
