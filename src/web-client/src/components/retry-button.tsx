"use client";

import { useEffect } from "react";
import { RotateCw } from "lucide-react";
import { Button } from "@/components/ui/button";

export function RetryButton() {
  useEffect(() => {
    const reload = () => window.location.reload();
    window.addEventListener("online", reload);

    return () => window.removeEventListener("online", reload);
  }, []);

  return (
    <Button onClick={() => window.location.reload()} className="min-h-11 px-6">
      <RotateCw aria-hidden />
      Opnieuw proberen
    </Button>
  );
}
