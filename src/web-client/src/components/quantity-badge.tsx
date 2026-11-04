import { cn } from "cn";
import { basisTone, describeAmount, describeRule, type QuantityBasis } from "@/lib/types";

type Rule = { quantity: number | null; unit: string | null; basis: QuantityBasis };

/** The rule chip, coloured per basis: "1 per kind". Fixed quantities have no chip; their number says it all. */
export function RuleChip({ quantity, unit, basis, className }: Rule & { className?: string }) {
  if (quantity === null || basis === "Fixed") {
    return null;
  }

  return (
    <span className={cn("inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium whitespace-nowrap", basisTone[basis], className)}>
      {describeRule(quantity, unit, basis)}
    </span>
  );
}

/**
 * How many to pack on a trip, shown prominently, with the rule it came from next to it. Nothing is rendered for an
 * item without a quantity.
 */
export function QuantityBadge({ quantity, unit, basis, total }: Rule & { total: number | null }) {
  if (quantity === null || total === null) {
    return null;
  }

  return (
    <span className="inline-flex flex-wrap items-center gap-1.5">
      <span className="bg-foreground text-background inline-flex min-w-6 items-center justify-center rounded-md px-1.5 py-0.5 text-xs font-semibold tabular-nums">
        {describeAmount(total, unit)}
      </span>
      <RuleChip quantity={quantity} unit={unit} basis={basis} />
    </span>
  );
}
