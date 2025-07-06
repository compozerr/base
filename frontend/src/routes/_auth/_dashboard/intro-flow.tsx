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
    const { data: paymentMethodsData, isLoading } = api.v1.getStripePaymentMethodsUser.useQuery();

    // If user already has payment methods, redirect to settings
    React.useEffect(() => {
        if (!isLoading && paymentMethodsData?.paymentMethods && paymentMethodsData.paymentMethods.length > 0) {
            navigate({
                to: '/settings',
            })
        }
    }, [paymentMethodsData, isLoading, navigate])

    const handleGetStarted = () => {
        localStorage.setItem('introFlowCompleted', 'true')
        navigate({
            to: '/settings',
            search: { addPaymentMethod: true },
        })
    }

    const handleSkip = () => {
        localStorage.setItem('introFlowCompleted', 'true')
        navigate({
            to: '/projects',
        })
    }

    if (isLoading) {
        return <div className="min-h-screen bg-black" />
    }

    return (
        <div className="fixed inset-0 overflow-hidden bg-black z-[1000]">
            <div className="pointer-events-none fixed inset-0">
                <div className="absolute inset-0 bg-gradient-to-b from-background via-background/90 to-background" />
                <div className="absolute right-0 top-0 h-[500px] w-[500px] bg-blue-500/10 blur-[100px]" />
                <div className="absolute bottom-0 left-0 h-[500px] w-[500px] bg-purple-500/10 blur-[100px]" />
            </div>

            <div className="absolute inset-0 pointer-events-none">
                <style>{`
                  @keyframes wave {
                    0%, 100% {
                      transform: scale(0.8);
                      opacity: 0.1;
                    }
                    50% {
                      transform: scale(1);
                      opacity: 0.3;
                    }
                  }
                  .wave-dot {
                    animation: wave 3s ease-in-out infinite;
                  }
                `}</style>
                <div className="grid gap-6 p-4" style={{ 
                    gridTemplateColumns: 'repeat(auto-fit, minmax(6px, 1fr))',
                    minHeight: '100vh',
                    width: '100vw'
                }}>
                    {Array.from({ length: 800 }, (_, i) => {
                        const row = Math.floor(i / 40);
                        const col = i % 40;
                        const delay = (row * 0.08 + col * 0.03) % 3;
                        return (
                            <div
                                key={i}
                                className="w-1.5 h-1.5 bg-blue-500/20 rounded-full wave-dot opacity-0"
                                style={{
                                    animationDelay: `${delay}s`,
                                }}
                            />
                        );
                    })}
                </div>
            </div>

            <div className="relative z-10 flex flex-col justify-center items-center min-h-screen p-6">
                <style>{`
                  @keyframes rotate3d {
                    0% {
                      transform: rotateX(0deg) rotateY(0deg) rotateZ(0deg);
                    }
                    33% {
                      transform: rotateX(120deg) rotateY(120deg) rotateZ(0deg);
                    }
                    66% {
                      transform: rotateX(240deg) rotateY(240deg) rotateZ(120deg);
                    }
                    100% {
                      transform: rotateX(360deg) rotateY(360deg) rotateZ(360deg);
                    }
                  }
                  @keyframes float {
                    0%, 100% {
                      transform: translateZ(0px);
                    }
                    50% {
                      transform: translateZ(20px);
                    }
                  }
                  @keyframes glow {
                    0%, 100% {
                      box-shadow: 0 0 20px rgba(59, 130, 246, 0.3);
                    }
                    50% {
                      box-shadow: 0 0 40px rgba(147, 51, 234, 0.6);
                    }
                  }
                  @keyframes slideUp {
                    from {
                      transform: translateY(50px);
                      opacity: 0;
                    }
                    to {
                      transform: translateY(0);
                      opacity: 1;
                    }
                  }
                  .cube-3d {
                    transform-style: preserve-3d;
                    animation: rotate3d 8s linear infinite;
                  }
                  .cube-face {
                    position: absolute;
                    width: 60px;
                    height: 60px;
                    background: linear-gradient(45deg, rgba(59, 130, 246, 0.2), rgba(147, 51, 234, 0.2));
                    border: 1px solid rgba(59, 130, 246, 0.4);
                    animation: glow 3s ease-in-out infinite;
                  }
                  .cube-face:nth-child(1) { transform: rotateY(0deg) translateZ(30px); }
                  .cube-face:nth-child(2) { transform: rotateY(90deg) translateZ(30px); }
                  .cube-face:nth-child(3) { transform: rotateY(180deg) translateZ(30px); }
                  .cube-face:nth-child(4) { transform: rotateY(-90deg) translateZ(30px); }
                  .cube-face:nth-child(5) { transform: rotateX(90deg) translateZ(30px); }
                  .cube-face:nth-child(6) { transform: rotateX(-90deg) translateZ(30px); }
                  .sphere {
                    position: absolute;
                    width: 12px;
                    height: 12px;
                    background: radial-gradient(circle, rgba(59, 130, 246, 0.8), rgba(147, 51, 234, 0.8));
                    border-radius: 50%;
                    animation: float 2s ease-in-out infinite;
                  }
                  .intro-content {
                    animation: slideUp 1s ease-out 0.5s both;
                  }
                `}</style>
                
                <div className="text-center space-y-8 max-w-3xl">
                    <div className="intro-content space-y-8">
                        <div className="text-center space-y-4">
                            <h1 className="text-5xl font-bold text-white">Welcome</h1>
                            <p className="text-xl text-zinc-400">
                                Set up billing to start deploying
                            </p>
                        </div>

                        <div className="relative w-26 h-26 mx-auto py-8" style={{ perspective: '200px' }}>
                            <div className="cube-3d relative w-16 h-16 mx-auto">
                                <div className="cube-face"></div>
                                <div className="cube-face"></div>
                                <div className="cube-face"></div>
                                <div className="cube-face"></div>
                                <div className="cube-face"></div>
                                <div className="cube-face"></div>
                            </div>
                        </div>

                        <Card className="border-zinc-800 bg-zinc-900/80 backdrop-blur-sm">
                            <CardContent className="p-8 space-y-8">
                                <div className="space-y-4">
                                    <div className="flex items-center space-x-3">
                                        <svg className="w-4 h-4 text-green-400 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                                            <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                                        </svg>
                                        <p className="text-zinc-300">$0 setup cost - pay only when deployed</p>
                                    </div>

                                    <div className="flex items-center space-x-3">
                                        <svg className="w-4 h-4 text-green-400 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                                            <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                                        </svg>
                                        <p className="text-zinc-300">Secure billing powered by Stripe</p>
                                    </div>

                                    <div className="flex items-center space-x-3">
                                        <svg className="w-4 h-4 text-green-400 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                                            <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                                        </svg>
                                        <p className="text-zinc-300">Deploy applications instantly</p>
                                    </div>
                                </div>

                                <div className="space-y-6 pt-4">
                                    <div className="flex flex-col items-center gap-4">
                                        <Button onClick={handleGetStarted} className="bg-white text-black hover:bg-zinc-200 font-medium px-8 py-3">
                                            Add Payment Method
                                        </Button>
                                        <button
                                            onClick={handleSkip}
                                            className="text-zinc-500 hover:text-zinc-300 text-sm underline transition-colors"
                                        >
                                            Skip for now
                                        </button>
                                    </div>
                                </div>
                            </CardContent>
                        </Card>
                    </div>
                </div>
            </div>
        </div>
    )
}
