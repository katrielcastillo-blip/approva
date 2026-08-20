"use client";

import { useBottleneckAnalytics } from "@/lib/hooks";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Bar, BarChart, CartesianGrid, XAxis, YAxis, Tooltip, Legend, ResponsiveContainer } from "recharts";
import { AlertTriangle } from "lucide-react";

function hoursLabel(hours: number) {
  if (hours < 24) return `${hours.toFixed(1)}h`;
  return `${(hours / 24).toFixed(1)}d`;
}

export default function AnalyticsPage() {
  const { data, isLoading } = useBottleneckAnalytics();

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">Analítica de cuellos de botella</h1>
        <p className="text-sm text-muted-foreground">
          Tiempo promedio y mediano entre asignación y decisión, por paso.
        </p>
      </div>

      {isLoading && <Skeleton className="h-96 w-full" />}

      {!isLoading && data && data.steps.length === 0 && (
        <Card>
          <CardContent className="py-12 text-center text-muted-foreground">
            Todavía no hay suficientes decisiones registradas para calcular analítica.
          </CardContent>
        </Card>
      )}

      {!isLoading && data && data.steps.length > 0 && (
        <>
          {data.slowestStepName && (
            <Card className="border-amber-500/50 bg-amber-500/5">
              <CardContent className="flex items-center gap-3 py-4">
                <AlertTriangle className="size-5 text-amber-600" />
                <p className="text-sm">
                  Las solicitudes se atascan más en <strong>{data.slowestStepName}</strong>: un
                  promedio de{" "}
                  <strong>
                    {hoursLabel(data.steps.find((s) => s.stepName === data.slowestStepName)!.avgHoursToDecide)}
                  </strong>{" "}
                  para decidir.
                </p>
              </CardContent>
            </Card>
          )}

          <Card>
            <CardHeader>
              <CardTitle className="text-base">Tiempo promedio y mediano por paso</CardTitle>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={320}>
                <BarChart data={data.steps} margin={{ left: 0, right: 16 }}>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                  <XAxis dataKey="stepName" tick={{ fontSize: 12 }} />
                  <YAxis tick={{ fontSize: 12 }} label={{ value: "horas", angle: -90, position: "insideLeft" }} />
                  <Tooltip formatter={(value) => hoursLabel(Number(value))} />
                  <Legend wrapperStyle={{ fontSize: 12 }} />
                  <Bar dataKey="avgHoursToDecide" name="Promedio" fill="var(--chart-1)" radius={[4, 4, 0, 0]} />
                  <Bar dataKey="medianHoursToDecide" name="Mediana" fill="var(--chart-2)" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-base">Detalle por paso</CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col divide-y">
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
                      <p className="font-medium">{hoursLabel(s.avgHoursToDecide)}</p>
                    </div>
                    <div>
                      <p className="text-muted-foreground">Mediana</p>
                      <p className="font-medium">{hoursLabel(s.medianHoursToDecide)}</p>
                    </div>
                    <div>
                      <p className="text-muted-foreground">Vencidas</p>
                      <p className="font-medium">{s.overdueCount}</p>
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
