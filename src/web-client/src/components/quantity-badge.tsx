import { CalendarX } from "lucide-react";
import { cn } from "cn";
import { basisTone, describeAmount, describeRule, isRule, type QuantityBasis, type QuantityPeriod } from "@/lib/types";

type Rule = { quantity: number | null; unit: string | null; basis: QuantityBasis; period: QuantityPeriod };

/** A fixed amount per day or night has no basis colour, so it gets a neutral chip. */
const neutralTone = "bg-muted text-foreground";

/**
 * The rule chip, coloured per basis: "1 per kind" or "2 per nacht". A fixed quantity once per trip has no chip; its
 * number says it all.
 */
export function RuleChip({ quantity, unit, basis, period, className }: Rule & { className?: string }) {
  if (quantity === null || !isRule(basis, period)) {
    return null;
  }

  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium whitespace-nowrap",
        basis === "Fixed" ? neutralTone : basisTone[basis],
        className,
      )}
    >
      {describeRule(quantity, unit, basis, period)}
    </span>
  );
}

/**
 * How many to pack on a trip, shown prominently, with the rule it came from next to it. On a trip without dates a
 * per-day or per-night total counts only once, which the hint makes clear. Nothing is rendered without a quantity.
 */
export function QuantityBadge({ quantity, unit, basis, period, total, missingDates }: Rule & { total: number | null; missingDates: boolean }) {
  if (quantity === null || total === null) {
    return null;
  }

  return (
    <span className="inline-flex flex-wrap items-center gap-1.5">
      <span className="bg-foreground text-background inline-flex min-w-6 items-center justify-center rounded-md px-1.5 py-0.5 text-xs font-semibold tabular-nums">
        {describeAmount(total, unit)}
      </span>
      <RuleChip quantity={quantity} unit={unit} basis={basis} period={period} />
      {missingDates && period !== "PerTrip" ? (
        <span className="text-muted-foreground inline-flex items-center gap-1 text-xs">
          <CalendarX className="size-3.5" aria-hidden />
          geen datums
        </span>
      ) : null}
    </span>
  );
}
