const currencyFormatter = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
  maximumFractionDigits: 0,
});

const numberFormatter = new Intl.NumberFormat("en-US", {
  maximumFractionDigits: 2,
});

/**
 * Metrics tagged with the "USD" unit render as full currency ($128,400)
 * rather than a bare "128400" + a separate "USD" suffix, since a dollar
 * amount without a $ sign or thousands separators is much harder to read at
 * a glance. Every other unit (lb, points, in, ...) still gets
 * thousands-separator formatting -- "1,050" instead of "1050" -- just
 * without a currency symbol, and keeps its unit shown alongside separately.
 */
export function formatMetricValue(value: number, unit: string): string {
  return unit === "USD" ? currencyFormatter.format(value) : numberFormatter.format(value);
}
