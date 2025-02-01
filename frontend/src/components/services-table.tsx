import { Avatar } from "@/components/ui/avatar"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Button } from "@/components/ui/button"
import { Play, Pause, RotateCw } from "lucide-react"

const services = [
  {
    name: "User Authentication",
    id: "auth-service",
    state: "Running",
    vCPUHours: 120.5,
    startDate: "05.10.2023",
    traffic: "high",
  },
  {
    name: "Payment Processing",
    id: "payment-service",
    state: "Stopped",
    vCPUHours: 45.2,
    startDate: "12.03.2023",
    traffic: "medium",
  },
  {
    name: "Data Analytics",
    id: "analytics-service",
    state: "Starting",
    vCPUHours: 80.7,
    startDate: "21.01.2023",
    traffic: "low",
  },
]

export function ServicesTable() {
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Service</TableHead>
          <TableHead>State</TableHead>
          <TableHead>vCPU Hours</TableHead>
          <TableHead>Start date</TableHead>
          <TableHead>Traffic</TableHead>
          <TableHead>Actions</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {services.map((service) => (
          <TableRow key={service.id}>
            <TableCell className="font-medium">
              <div className="flex items-center gap-2">
                <Avatar className="h-6 w-6">
                  <img src={`/placeholder.svg?height=24&width=24`} alt={service.name} />
                </Avatar>
                <div>
                  <div className="font-medium">{service.name}</div>
                  <div className="text-xs text-muted-foreground">{service.id}</div>
                </div>
              </div>
            </TableCell>
            <TableCell>
              <span
                className={`inline-flex items-center rounded-full px-2 py-1 text-xs ${
                  service.state === "Running"
                    ? "bg-green-500/10 text-green-500"
                    : service.state === "Stopped"
                      ? "bg-red-500/10 text-red-500"
                      : "bg-yellow-500/10 text-yellow-500"
                }`}
              >
                {service.state}
              </span>
            </TableCell>
            <TableCell>{service.vCPUHours.toFixed(2)}</TableCell>
            <TableCell>{service.startDate}</TableCell>
            <TableCell>
              <div className="flex gap-1">
                {Array.from({ length: 3 }).map((_, i) => (
                  <div
                    key={i}
                    className={`h-1.5 w-3 rounded-full ${
                      i < (service.traffic === "high" ? 3 : service.traffic === "medium" ? 2 : 1)
                        ? "bg-primary"
                        : "bg-muted"
                    }`}
                  />
                ))}
              </div>
            </TableCell>
            <TableCell>
              <div className="flex gap-2">
                {service.state === "Running" ? (
                  <Button size="sm" variant="ghost">
                    <Pause className="h-4 w-4" />
                  </Button>
                ) : service.state === "Stopped" ? (
                  <Button size="sm" variant="ghost">
                    <Play className="h-4 w-4" />
                  </Button>
                ) : (
                  <Button size="sm" variant="ghost">
                    <RotateCw className="h-4 w-4" />
                  </Button>
                )}
              </div>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}

