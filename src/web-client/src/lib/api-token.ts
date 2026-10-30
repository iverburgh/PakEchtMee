import "server-only";

import { SignJWT } from "jose";

/**
 * The short-lived token this server signs for every API call. Issuer, audience and algorithm must match
 * `Web/Access/ApiToken.cs`; the key is shared through the Aspire parameter `api-token-key`.
 * Google's own tokens never leave this server.
 */
const issuer = "pakechtmee-web";
const audience = "pakechtmee-api";
const lifetime = "5m";

function signingKey(): Uint8Array {
  const key = process.env.API_TOKEN_KEY;

  if (!key) {
    throw new Error("API_TOKEN_KEY is niet ingesteld; start de app via Aspire of zet de variabele zelf.");
  }

  return new TextEncoder().encode(key);
}

export async function apiToken(email: string): Promise<string> {
  return new SignJWT({})
    .setProtectedHeader({ alg: "HS256" })
    .setSubject(email.toLowerCase())
    .setIssuer(issuer)
    .setAudience(audience)
    .setIssuedAt()
    .setExpirationTime(lifetime)
    .sign(signingKey());
}

/** The API base URL; Aspire sets `API_BASE_URL`, the fallback keeps a bare `npm run dev` working against a local API. */
export function apiBaseUrl(): string {
  const url = process.env.API_BASE_URL ?? "http://localhost:5080";

  return url.replace(/\/$/, "");
}
