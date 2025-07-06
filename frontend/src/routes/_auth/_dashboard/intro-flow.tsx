import * as React from 'react'
import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { Link } from '@tanstack/react-router'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { api } from '@/api-client'

export const Route = createFileRoute('/_auth/_dashboard/intro-flow')({
    component: RouteComponent,
})

function RouteComponent() {

    const navigate = useNavigate()
    const handleGetStarted = () => {
        localStorage.setItem('introFlowCompleted', 'true')
        navigate({
            to: '/settings',
        })
    }

    const handleSkip = () => {
        localStorage.setItem('introFlowCompleted', 'true')
        navigate({
            to: '/settings',
        })
    }

    const {data: paymentMethodsData, isLoading} = api.v1.getStripePaymentMethodsUser.useQuery();
    

    return (
        <div className="fixed inset-0 bg-black z-50 flex items-center justify-center p-6">
            <div className="w-full max-w-2xl">
                <Card className="border-zinc-800 bg-zinc-900">
                    <CardHeader className="text-center space-y-4">
                        <div className="mx-auto w-16 h-16 bg-blue-600 rounded-full flex items-center justify-center">
                            <svg className="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                            </svg>
                        </div>
                        <CardTitle className="text-3xl font-bold text-white">Welcome to Your Dashboard</CardTitle>
                        <CardDescription className="text-xl text-zinc-400">
                            Deploy and manage your applications with ease
                        </CardDescription>
                    </CardHeader>

                    <CardContent className="space-y-8">
                        <div className="grid gap-6">
                            <div className="flex items-start space-x-4">
                                <div className="w-8 h-8 bg-green-600 rounded-full flex items-center justify-center flex-shrink-0">
                                    <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                                    </svg>
                                </div>
                                <div>
                                    <h3 className="font-semibold text-white">Pay-as-you-go Pricing</h3>
                                    <p className="text-zinc-400 text-sm">Setup costs $0. You only pay when your services are actively deployed and running.</p>
                                </div>
                            </div>

                            <div className="flex items-start space-x-4">
                                <div className="w-8 h-8 bg-blue-600 rounded-full flex items-center justify-center flex-shrink-0">
                                    <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                                    </svg>
                                </div>
                                <div>
                                    <h3 className="font-semibold text-white">Secure Billing</h3>
                                    <p className="text-zinc-400 text-sm">Your payment information is securely processed by Stripe. No charges until you deploy.</p>
                                </div>
                            </div>

                            <div className="flex items-start space-x-4">
                                <div className="w-8 h-8 bg-purple-600 rounded-full flex items-center justify-center flex-shrink-0">
                                    <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                                    </svg>
                                </div>
                                <div>
                                    <h3 className="font-semibold text-white">Instant Deployments</h3>
                                    <p className="text-zinc-400 text-sm">Deploy your applications instantly with our streamlined deployment pipeline.</p>
                                </div>
                            </div>
                        </div>

                        <div className="bg-zinc-800 rounded-lg p-6 border border-zinc-700">
                            <h4 className="font-semibold text-white mb-2">Ready to get started?</h4>
                            <p className="text-zinc-400 text-sm mb-4">
                                Add your payment method to unlock all features. You'll only be charged when you deploy and run services.
                            </p>
                            <div className="flex flex-col sm:flex-row gap-3">
                                <Button asChild className="flex-1 bg-blue-600 hover:bg-blue-700">
                                    <Link
                                        to="/settings"
                                        search={{ addPaymentMethod: true }}
                                        onClick={handleGetStarted}
                                    >
                                        <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
                                        </svg>
                                        Set Up Billing - $0 Setup Cost
                                    </Link>
                                </Button>
                                <Button
                                    variant="outline"
                                    onClick={handleSkip}
                                    className="border-zinc-600 text-zinc-300 hover:bg-zinc-800"
                                >
                                    Skip for now
                                </Button>
                            </div>
                        </div>
                    </CardContent>
                </Card>
            </div>
        </div>)
}
