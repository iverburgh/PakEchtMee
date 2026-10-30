import { headers } from "next/headers";
import { api } from "@/lib/api";
import { publicPageHeader } from "@/lib/public-page";
import { currentEmail } from "@/lib/session";
import { BottomNav } from "@/components/bottom-nav";

/**
 * The navigation for a signed-in, known account. The sign-in, no-access and offline pages render without it, and
 * so does any page while the API is unreachable: the page itself reports that, the chrome stays out of the way.
 */
export async function AppChrome() {
  if ((await headers()).get(publicPageHeader) || !(await currentEmail())) {
    return null;
  }

  const me = await api.me().catch(() => null);

  if (!me) {
    return null;
  }

  return <BottomNav email={me.email} familyName={me.family?.name ?? null} isAdministrator={me.isAdministrator} />;
}
