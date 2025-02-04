import * as React from 'react'
import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute('/_auth/dashboard/services')({
    component: RouteComponent,
})

function RouteComponent() {
    return (<main>
        <h1 className="text-3xl font-bold mb-6">Services</h1>
        <div className="space-y-6">
           
        </div >
    </main>)
}
