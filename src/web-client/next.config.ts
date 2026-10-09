import { randomUUID } from "node:crypto";
import withSerwistInit from "@serwist/next";
import type { NextConfig } from "next";

const withSerwist = withSerwistInit({
  swSrc: "src/app/sw.ts",
  swDest: "public/sw.js",
  // The fallback page must be available without a network, so it is precached explicitly.
  additionalPrecacheEntries: [{ url: "/offline", revision: randomUUID() }],
  // A global reload on every "online" event would wipe search and filters mid-walkthrough; the offline page reloads itself.
  reloadOnOnline: false,
  // Keeps `npm run dev` free of service-worker caching while debugging.
  disable: process.env.NODE_ENV === "development",
});

const nextConfig: NextConfig = {
  reactStrictMode: true,
};

export default withSerwist(nextConfig);
