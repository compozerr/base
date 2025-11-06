import { useState } from "react"
import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis, ReferenceLine } from "recharts"
import { format } from "date-fns"
import { Card, CardContent } from "@/components/ui/card"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Cpu, HardDrive, Network, MemoryStickIcon as Ram } from "lucide-react"
import { api } from "@/api-client"
import { components } from "@/generated"
import { Skeleton } from "./ui/skeleton"

// Enum to match the backend
export enum UsagePointType {
  CPU = "CPU",
  Ram = "Ram",
  DiskWrite = "DiskWrite",
  DiskRead = "DiskRead",
  NetworkIn = "NetworkIn",
  NetworkOut = "NetworkOut",
}

// Record to match the backend
export type UsagePoint = {
  timestamp: string
  value: number
}

// Response to match the backend
export type UsageData = {
  points: Record<UsagePointType, UsagePoint[]>
  usageSpan: string
}

// Props for the component
type UsageGraphProps = {
  projectId: string
}

// Mapping for friendly display names
const metricDisplayNames: Record<UsagePointType, string> = {
  [UsagePointType.CPU]: "CPU",
  [UsagePointType.Ram]: "Memory",
  [UsagePointType.DiskRead]: "Disk Read",
  [UsagePointType.DiskWrite]: "Disk Write",
  [UsagePointType.NetworkIn]: "Network In",
  [UsagePointType.NetworkOut]: "Network Out",
}

// Mapping for metric colors
const metricColors: Record<UsagePointType, string> = {
  [UsagePointType.CPU]: "#22c55e",
  [UsagePointType.Ram]: "#3b82f6",
  [UsagePointType.DiskRead]: "#f59e0b",
  [UsagePointType.DiskWrite]: "#ef4444",
  [UsagePointType.NetworkIn]: "#8b5cf6",
  [UsagePointType.NetworkOut]: "#ec4899",
}

// Mapping for metric icons
const MetricIcon = ({ type }: { type: UsagePointType }) => {
  switch (type) {
    case UsagePointType.CPU:
      return <Cpu className="h-4 w-4" />
    case UsagePointType.Ram:
      return <Ram className="h-4 w-4" />
    case UsagePointType.DiskRead:
    case UsagePointType.DiskWrite:
      return <HardDrive className="h-4 w-4" />
    case UsagePointType.NetworkIn:
    case UsagePointType.NetworkOut:
      return <Network className="h-4 w-4" />
    default:
      return null
  }
}

// Units for different metrics
const getMetricUnit = (type: UsagePointType): string => {
  switch (type) {
    case UsagePointType.CPU:
      return "%"
    case UsagePointType.Ram:
      return "GB"
    case UsagePointType.DiskRead:
    case UsagePointType.DiskWrite:
      return "MB/s"
    case UsagePointType.NetworkIn:
    case UsagePointType.NetworkOut:
      return "KB/s"
    default:
      return ""
  }
}

const getMaxGraphValue = (type: UsagePointType, data: UsagePoint[], allocatedMemoryGb: number): number => {
  // Find max value for better scaling
  const maxValue = Math.max(...data.map((item) => item.value))

  // Calculate domain with some padding (10%)
  // Always start from 0 to ensure baseline is visible
  const domainMax = maxValue + maxValue * 0.1
  let max;
  switch (type) {
    case UsagePointType.CPU:
      max = Math.ceil(domainMax / 10) * 10
      break;
    case UsagePointType.Ram:
      max = Math.ceil(domainMax / allocatedMemoryGb) * allocatedMemoryGb
      break;
    case UsagePointType.DiskRead:
    case UsagePointType.DiskWrite:
      max = Math.ceil(domainMax)
      break;
    case UsagePointType.NetworkIn:
    case UsagePointType.NetworkOut:
      max = Math.ceil(domainMax)
      break;
    default:
      max = domainMax;
  }

  return max;
}

const CustomTooltip = ({ active, payload, label }: any) => {
  if (active && payload && payload.length) {
    const value = payload[0].value
    const timestamp = label
    const unit = payload[0].unit || ""

    return (
      <div className="bg-white border border-gray-200 p-3 rounded-md shadow-md">
        <p className="text-xs text-gray-500 mb-1">{format(new Date(timestamp), "MMM d, yyyy HH:mm:ss")}</p>
        <p className="font-medium text-gray-900">{`${value.toFixed(2)}${unit}`}</p>
      </div>
    )
  }
  return null
}

export function UsageGraph({ projectId }: UsageGraphProps) {
  const [timeRange, setTimeRange] = useState<components["schemas"]["Api.Endpoints.Projects.Usage.Get.UsageSpan"]>("Day")
  const [selectedMetric, setSelectedMetric] = useState<UsagePointType>(UsagePointType.CPU)

  const { data: usageData, error: usageError } = api.v1.getProjectsProjectIdUsageSpan.useQuery({
    path: {
      projectId,
      usageSpan: timeRange
    }
  })

  // Format data for the chart
  const formatChartData = (data: UsagePoint[]) => {
    return data?.map((point) => ({
      timestamp: new Date(point.timestamp),
      value: point.value,
    }))
  }

  // Get the current data based on selected metric
  const currentData = usageData?.points![selectedMetric] || []
  const chartData = formatChartData(currentData as UsagePoint[])

  const maxGraphValue = getMaxGraphValue(selectedMetric, currentData as UsagePoint[], usageData?.allocatedMemoryGb ?? 0);

  // Get the unit for the current metric
  const unit = getMetricUnit(selectedMetric)

  return (
    <Card className="col-span-2">
      <CardContent className="pt-6">
        <div className="space-y-4">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <h2 className="text-xl font-semibold">Resource Usage</h2>
            <div className="flex items-center gap-2">
              <Select value={selectedMetric} onValueChange={(value) => setSelectedMetric(value as UsagePointType)}>
                <SelectTrigger className="w-[180px]">
                  <SelectValue placeholder="Select metric" />
                </SelectTrigger>
                <SelectContent>
                  {Object.values(UsagePointType).map((type) => (
                    <SelectItem key={type} value={type}>
                      <div className="flex items-center gap-2">
                        <MetricIcon type={type} />
                        <span>{metricDisplayNames[type]}</span>
                      </div>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <Tabs defaultValue="Day" value={timeRange} onValueChange={(x) => setTimeRange(x as components["schemas"]["Api.Endpoints.Projects.Usage.Get.UsageSpan"])}>
            <TabsList className="grid w-full grid-cols-5">
              <TabsTrigger value="Day">Day</TabsTrigger>
              <TabsTrigger value="Week">Week</TabsTrigger>
              <TabsTrigger value="Month">Month</TabsTrigger>
              <TabsTrigger value="Year">Year</TabsTrigger>
              <TabsTrigger value="Total">Total</TabsTrigger>
            </TabsList>

            <TabsContent value={timeRange} className="mt-2">
              <div className="h-[300px] w-full">
                <ResponsiveContainer width="100%" height="100%">
                  {usageError ? <div>Error loading usage data: {JSON.stringify(usageError)}</div> :
                    !usageData ? (
                      <Skeleton className="h-full w-full" />
                    )
                      :
                      <AreaChart data={chartData} margin={{ top: 20, right: 30, left: 30, bottom: 10 }}>
                        <defs>
                          <linearGradient id={`gradient-${selectedMetric}`} x1="0" y1="0" x2="0" y2="1">
                            <stop offset="5%" stopColor={metricColors[selectedMetric]} stopOpacity={0.8} />
                            <stop offset="95%" stopColor={metricColors[selectedMetric]} stopOpacity={0.1} />
                          </linearGradient>
                        </defs>
                        <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e5e7eb" />
                        <XAxis
                          dataKey="timestamp"
                          tickFormatter={(timestamp) => {
                            const date = new Date(timestamp)
                            return format(date, "HH:mm")
                          }}
                          stroke="#6b7280"
                          fontSize={12}
                          tickMargin={10}
                          axisLine={{ stroke: "#e5e7eb" }}
                          tickLine={{ stroke: "#e5e7eb" }}
                        />
                        <YAxis
                          stroke="#6b7280"
                          fontSize={12}
                          domain={[0, maxGraphValue]}
                          tickCount={5}
                          allowDecimals={false}
                          tickFormatter={(value) => `${Math.round(value)}${unit}`}
                          axisLine={{ stroke: "#e5e7eb" }}
                          tickLine={{ stroke: "#e5e7eb" }}
                        />
                        <Tooltip
                          content={<CustomTooltip />}
                          cursor={{ stroke: "#9ca3af", strokeWidth: 1, strokeDasharray: "5 5" }}
                          wrapperStyle={{ zIndex: 100, pointerEvents: "none" }}
                          isAnimationActive={false}
                        />
                        {selectedMetric === UsagePointType.Ram && (
                          <ReferenceLine
                            y={usageData?.allocatedMemoryGb ?? 0}
                            stroke="#ef4444"
                            strokeDasharray="5 5"
                            strokeWidth={2}
                            label={{ value: `Allocated: ${usageData?.allocatedMemoryGb ?? 0}GB`, position: "top", fill: "#ef4444", fontSize: 12 }}
                          />
                        )}
                        <Area
                          type="monotone"
                          dataKey="value"
                          stroke={metricColors[selectedMetric]}
                          strokeWidth={2}
                          fillOpacity={1}
                          fill={`url(#gradient-${selectedMetric})`}
                          isAnimationActive={true}
                          animationDuration={500}
                          unit={unit}
                          activeDot={{ r: 6, stroke: "white", strokeWidth: 2, fill: metricColors[selectedMetric] }}
                        />
                      </AreaChart>
                  }
                </ResponsiveContainer>
              </div>
              <div className="mt-2 flex justify-between text-xs text-muted-foreground">
                <div className="flex items-center gap-1">
                  <MetricIcon type={selectedMetric} />
                  <span>{metricDisplayNames[selectedMetric]}</span>
                </div>
                <div>{timeRange.charAt(0).toUpperCase() + timeRange.slice(1)} view</div>
              </div>
            </TabsContent>
          </Tabs>
        </div>
      </CardContent>
    </Card >
  )
}
