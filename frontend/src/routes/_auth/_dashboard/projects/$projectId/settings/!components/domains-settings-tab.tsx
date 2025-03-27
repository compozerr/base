import React, { useState } from 'react';
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Separator } from "@/components/ui/separator";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { TabsContent } from "@/components/ui/tabs";

interface Props {

}

const DomainsSettingsTab: React.FC<Props> = (props) => {
    const [showDnsGuide, setShowDnsGuide] = useState(false)

    return (
        <TabsContent value="domains" className="space-y-4 mt-6">
            <Card>
                <CardHeader>
                    <CardTitle>Domains</CardTitle>
                    <CardDescription>Manage domains for your project.</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                    <div className="space-y-2">
                        <div className="flex items-center justify-between">
                            <div>
                                <h3 className="font-medium">project-abc123.vercel.app</h3>
                                <p className="text-sm text-muted-foreground">Default Vercel domain</p>
                            </div>
                            <Badge>Default</Badge>
                        </div>
                        <Separator />
                        <div className="flex items-center justify-between">
                            <div>
                                <h3 className="font-medium">myawesomeproject.com</h3>
                                <p className="text-sm text-muted-foreground">Custom domain</p>
                            </div>
                            <Badge variant="outline">Valid</Badge>
                        </div>
                    </div>

                    <Separator className="my-4" />

                    <div className="space-y-2">
                        <h3 className="text-lg font-medium">Add Custom Domain</h3>
                        <div className="flex gap-2">
                            <Input placeholder="example.com" className="flex-1" />
                            <Button onClick={() => setShowDnsGuide(true)}>Add</Button>
                        </div>
                    </div>

                    {showDnsGuide && (
                        <Card className="mt-6 border-blue-200 bg-blue-50 dark:bg-blue-950 dark:border-blue-800">
                            <CardHeader className="pb-2">
                                <CardTitle className="text-lg">Set up DNS Records</CardTitle>
                                <CardDescription>Configure your domain's DNS settings to point to your project.</CardDescription>
                            </CardHeader>
                            <CardContent>
                                <div className="space-y-4">
                                    <div>
                                        <h4 className="font-medium mb-2">1. Add a CNAME record</h4>
                                        <div className="rounded-md border bg-card">
                                            <Table>
                                                <TableHeader>
                                                    <TableRow>
                                                        <TableHead>Type</TableHead>
                                                        <TableHead>Name</TableHead>
                                                        <TableHead>Value</TableHead>
                                                        <TableHead>TTL</TableHead>
                                                    </TableRow>
                                                </TableHeader>
                                                <TableBody>
                                                    <TableRow>
                                                        <TableCell className="font-medium">CNAME</TableCell>
                                                        <TableCell>www</TableCell>
                                                        <TableCell>cname.vercel-dns.com</TableCell>
                                                        <TableCell>3600</TableCell>
                                                    </TableRow>
                                                </TableBody>
                                            </Table>
                                        </div>
                                    </div>

                                    <div>
                                        <h4 className="font-medium mb-2">2. Add an A record for the apex domain (optional)</h4>
                                        <div className="rounded-md border bg-card">
                                            <Table>
                                                <TableHeader>
                                                    <TableRow>
                                                        <TableHead>Type</TableHead>
                                                        <TableHead>Name</TableHead>
                                                        <TableHead>Value</TableHead>
                                                        <TableHead>TTL</TableHead>
                                                    </TableRow>
                                                </TableHeader>
                                                <TableBody>
                                                    <TableRow>
                                                        <TableCell className="font-medium">A</TableCell>
                                                        <TableCell>@</TableCell>
                                                        <TableCell>76.76.21.21</TableCell>
                                                        <TableCell>3600</TableCell>
                                                    </TableRow>
                                                </TableBody>
                                            </Table>
                                        </div>
                                    </div>

                                    <div className="rounded-md bg-muted p-4">
                                        <h4 className="font-medium mb-2">DNS Propagation</h4>
                                        <p className="text-sm text-muted-foreground">
                                            DNS changes can take up to 48 hours to propagate worldwide, though they often take effect much
                                            sooner. We'll automatically check your DNS configuration and notify you when your domain is
                                            properly configured.
                                        </p>
                                    </div>

                                    <div className="flex justify-end space-x-2">
                                        <Button variant="outline" onClick={() => setShowDnsGuide(false)}>
                                            Close
                                        </Button>
                                        <Button>Verify DNS Configuration</Button>
                                    </div>
                                </div>
                            </CardContent>
                        </Card>
                    )}
                </CardContent>
            </Card>
        </TabsContent>
    );
}

export default DomainsSettingsTab;