import type { ActionResult } from "@/lib/actions";

export const offlineMessage = "Geen verbinding. De wijziging is niet opgeslagen.";

/**
 * Server Actions reject in the browser when the request never reaches the server (offline, server down).
 * Turning that into a failed result lets every caller restore its state instead of crashing the page.
 */
export async function callAction<T>(action: () => Promise<ActionResult<T>>): Promise<ActionResult<T>> {
  try {
    return await action();
  } catch {
    return { ok: false, error: offlineMessage };
  }
}
