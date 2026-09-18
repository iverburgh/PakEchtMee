import type { Metadata, Viewport } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import { Toaster } from "@/components/ui/sonner";
import { BottomNav } from "@/components/bottom-nav";
import { ServiceWorkerUpdater } from "@/components/service-worker-updater";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "PakEchtMee",
  description: "Beheer je paklijst en loop hem snel af op je telefoon.",
  appleWebApp: { capable: true, statusBarStyle: "black-translucent", title: "PakEchtMee" },
};

export const viewport: Viewport = {
  themeColor: "#0f172a",
  width: "device-width",
  initialScale: 1,
  viewportFit: "cover",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html
      lang="nl"
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="bg-background text-foreground min-h-full">
        {/* pb-20 keeps the last row clear of the fixed bottom navigation. */}
        <div className="mx-auto flex min-h-dvh w-full max-w-3xl flex-col pb-20 print:max-w-none print:pb-0">
          {children}
        </div>
        <BottomNav />
        <ServiceWorkerUpdater />
        <Toaster position="top-center" richColors closeButton />
      </body>
    </html>
  );
}
