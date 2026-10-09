import "server-only";

import { notFound } from "next/navigation";
import { ApiError } from "@/lib/api";

/** An unknown or stale id in the URL is a missing page, not a connection problem. */
export async function orNotFound<T>(request: Promise<T>): Promise<T> {
  try {
    return await request;
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }

    throw error;
  }
}
