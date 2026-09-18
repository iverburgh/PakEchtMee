import type { ReactNode } from "react";
import { Label } from "@/components/ui/label";

/** Label, control and inline error in one block; keeps the form dialogs free of repetition. */
export function Field({
  id,
  label,
  error,
  description,
  children,
}: {
  id: string;
  label: string;
  error?: string;
  description?: string;
  children: ReactNode;
}) {
  return (
    <div className="flex flex-col gap-2">
      <Label htmlFor={id}>{label}</Label>
      {children}
      {description ? <p className="text-muted-foreground text-xs">{description}</p> : null}
      {error ? (
        <p role="alert" className="text-destructive text-sm">
          {error}
        </p>
      ) : null}
    </div>
  );
}
