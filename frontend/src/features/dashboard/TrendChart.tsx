import { Line, LineChart, ResponsiveContainer } from "recharts";
import type { MetricTrendPoint } from "@/lib/api";

export function TrendChart({ points }: { points: MetricTrendPoint[] }) {
  if (points.length < 2) {
    return <p className="text-xs text-muted">Not enough history for a trend yet.</p>;
  }

  return (
    <ResponsiveContainer width="100%" height={48}>
      <LineChart data={points} margin={{ top: 4, right: 4, bottom: 4, left: 4 }}>
        <Line type="monotone" dataKey="value" stroke="#6366f1" strokeWidth={2} dot={false} isAnimationActive={false} />
      </LineChart>
    </ResponsiveContainer>
  );
}
