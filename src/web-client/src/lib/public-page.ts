/**
 * Set by `proxy.ts` on public pages (sign-in, no access, offline) so the layout renders no account details there;
 * the offline page in particular is cached by the service worker.
 */
export const publicPageHeader = "x-pem-public-page";
