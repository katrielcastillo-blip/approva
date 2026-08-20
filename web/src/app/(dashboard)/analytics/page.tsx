"use client";

import { useBottleneckAnalytics } from "@/lib/hooks";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { PageHeader } from "@/components/page-header";
import { Bar, BarChart, CartesianGrid, XAxis, YAxis, Tooltip, Legend, ResponsiveContainer } from "recharts";
import { AlertTriangle, BarChart3, Gauge, ListChecks, TimerReset } from "lucide-react";

function hoursLabel(hours: number) {
  if (hours < 24) return `${hours.toFixed(1)}h`;
  return `${(hours / 24).toFixed(1)}d`;
}

export default function AnalyticsPage() {
  const { data, isLoading } = useBottleneckAnalytics();

  const totalDecided = data?.steps.reduce((sum, s) => sum + s.decidedTaskCount, 0) ?? 0;
  const totalOverdue = data?.steps.reduce((sum, s) => sum + s.overdueCount, 0) ?? 0;
  const overallAvg =
    data && data.steps.length > 0
      ? data.steps.reduce((sum, s) => sum + s.avgHoursToDecide * s.decidedTaskCount, 0) / Math.max(totalDecided, 1)
      : 0;

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        icon={BarChart3}
        title="Analítica de cuellos de botella"
        description="Tiempo promedio y mediano entre asignación y decisión, por paso."
      />

      {isLoading && <Skeleton className="h-96 w-full rounded-2xl" />}

      {!isLoading && data && data.steps.length === 0 && (
        <Card className="glass rounded-2xl border-dashed border-border/60">
          <CardContent className="py-12 text-center text-muted-foreground">
            Todavía no hay suficientes decisiones registradas para calcular analítica.
          </CardContent>
        </Card>
      )}

      {!isLoading && data && data.steps.length > 0 && (
        <>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <StatTile icon={ListChecks} label="Decisiones registradas" value={totalDecided.toString()} />
            <StatTile icon={Gauge} label="Tiempo promedio global" value={hoursLabel(overallAvg)} />
            <StatTile
              icon={TimerReset}
              label="Tareas vencidas"
              value={totalOverdue.toString()}
              tone={totalOverdue > 0 ? "warn" : "default"}
            />
          </div>

          {data.slowestStepName && (
            <Card className="glass rounded-2xl border-amber-500/30 bg-amber-500/5">
              <CardContent className="flex items-center gap-3 py-4">
                <div className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-amber-500/15 text-amber-600 dark:text-amber-400">
                  <AlertTriangle className="size-4.5" />
                </div>
                <p className="text-sm">
                  Las solicitudes se atascan más en <strong>{data.slowestStepName}</strong>: un promedio de{" "}
                  <strong>
                    {hoursLabel(data.steps.find((s) => s.stepName === data.slowestStepName)!.avgHoursToDecide)}
                  </strong>{" "}
                  para decidir.
                </p>
              </CardContent>
            </Card>
          )}

          <Card className="glass rounded-2xl border-border/60">
            <CardHeader>
              <CardTitle className="text-base">Tiempo promedio y mediano por paso</CardTitle>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={320}>
                <BarChart data={data.steps} margin={{ left: 0, right: 16 }}>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                  <XAxis dataKey="stepName" tick={{ fontSize: 12 }} />
                  <YAxis tick={{ fontSize: 12 }} label={{ value: "horas", angle: -90, position: "insideLeft" }} />
                  <Tooltip
                    formatter={(value) => hoursLabel(Number(value))}
                    contentStyle={{
                      borderRadius: 12,
                      border: "1px solid var(--border)",
                      background: "var(--popover)",
                      color: "var(--popover-foreground)",
                    }}
                  />
                  <Legend wrapperStyle={{ fontSize: 12 }} />
                  <Bar dataKey="avgHoursToDecide" name="Promedio" fill="var(--chart-1)" radius={[6, 6, 0, 0]} />
                  <Bar dataKey="medianHoursToDecide" name="Mediana" fill="var(--chart-2)" radius={[6, 6, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>

          <Card className="glass rounded-2xl border-border/60">
            <CardHeader>
              <CardTitle className="text-base">Detalle por paso</CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col divide-y divide-border/60">
              {data.steps.map((s) => (
                <div key={s.stepName} className="flex items-center justify-between py-3 text-sm">
                  <div>
                    <p className="font-medium">{s.stepName}</p>
                    <p className="text-muted-foreground">
                      {s.decidedTaskCount} {s.decidedTaskCount === 1 ? "decisión registrada" : "decisiones registradas"}
                    </p>
                  </div>
                  <div className="flex gap-6 text-right">
                    <div>
                      <p className="text-muted-foreground">Promedio</p>
                      <p className="font-medium tabular-nums">{hoursLabel(s.avgHoursToDecide)}</p>
                    </div>
                    <div>
                      <p className="text-muted-foreground">Mediana</p>
                      <p className="font-medium tabular-nums">{hoursLabel(s.medianHoursToDecide)}</p>
                    </div>
                    <div>
                      <p className="text-muted-foreground">Vencidas</p>
                      <p className="font-medium tabular-nums">{s.overdueCount}</p>
                    </div>
                  </div>
                </div>
              ))}
            </CardContent>
          </Card>
        </>
      )}
    </div>
  );
}

function StatTile({
  icon: Icon,
  label,
  value,
  tone = "default",
}: {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  value: string;
  tone?: "default" | "warn";
}) {
  return (
    <Card className="glass rounded-2xl border-border/60 py-0">
      <CardContent className="flex items-center gap-3 px-5 py-4">
        <div
          className={
            tone === "warn"
              ? "flex size-10 shrink-0 items-center justify-center rounded-xl bg-amber-500/15 text-amber-600 dark:text-amber-400"
              : "flex size-10 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary"
          }
        >
          <Icon className="size-5" />
        </div>
        <div>
          <p className="text-xl font-semibold tabular-nums">{value}</p>
          <p className="text-xs text-muted-foreground">{label}</p>
        </div>
      </CardContent>
    </Card>
  );
}
