"use client";

import { useEffect } from "react";
import { toast } from "sonner";

/**
 * Registers the service worker and asks before activating a new version, so an update
 * never swaps the app out from under someone halfway through a packing walkthrough.
 */
export function ServiceWorkerUpdater() {
  useEffect(() => {
    if (!("serviceWorker" in navigator) || process.env.NODE_ENV !== "production") {
      return;
    }

    const promptForUpdate = (waiting: ServiceWorker) => {
      toast("Er is een nieuwe versie beschikbaar.", {
        duration: Infinity,
        action: {
          label: "Vernieuwen",
          onClick: () => {
            waiting.postMessage({ type: "SKIP_WAITING" });
            window.location.reload();
          },
        },
      });
    };

    navigator.serviceWorker
      .register("/sw.js")
      .then((registration) => {
        if (registration.waiting) {
          promptForUpdate(registration.waiting);
        }

        registration.addEventListener("updatefound", () => {
          const installing = registration.installing;

          installing?.addEventListener("statechange", () => {
            if (installing.state === "installed" && navigator.serviceWorker.controller) {
              promptForUpdate(installing);
            }
          });
        });
      })
      .catch(() => {
        // Without a service worker the app still works; it is simply not installable offline.
      });
  }, []);

  return null;
}
