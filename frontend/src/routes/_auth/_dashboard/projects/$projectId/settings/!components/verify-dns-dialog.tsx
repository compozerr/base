import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table'
import { useMemo } from 'react'

import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle
} from "@/components/ui/dialog"
import React from 'react'
import { api } from '@/api-client'
import { Button } from '@/components/ui/button'

interface Props {
    projectId: string
    domains: Awaited<ReturnType<typeof api.v1.getProjectsProjectIdDomains.fetchQuery>>["domains"]
    selectedDomainId: string | null
    onClose?: () => void;
}

const VerifyDnsDialog: React.FC<Props> = (props) => {
    const { data: parentDomainData } = api.v1.getProjectsProjectIdDomainsDomainIdParent.useQuery({
        path: {
            domainId: props.selectedDomainId!,
            projectId: props.projectId
        }
    }, { enabled: !!props.selectedDomainId });


    const dnsGuide = useMemo(() => {
        const selectedDomainValue = props.domains?.find(x => x.domainId == props.selectedDomainId);
        if (!props.selectedDomainId || !selectedDomainValue || !parentDomainData?.domain) return null;

        return {
            name: selectedDomainValue.value,
            value: parentDomainData.domain
        }
    }, [parentDomainData, props.selectedDomainId]);

    return (
        <Dialog open={!!dnsGuide} onOpenChange={(open) => {
            if (!open) props.onClose?.();
        }}>
            <DialogContent className="max-w-3xl w-full overflow-hidden">
                <DialogHeader>
                    <DialogTitle>Set up DNS Records</DialogTitle>
                    <DialogDescription>
                        Configure your domain's DNS settings to point to your project.
                    </DialogDescription>
                </DialogHeader>
                <div className="space-y-4">
                    <div>
                        <h4 className="font-medium mb-2">Add CNAME record</h4>
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
                                        <TableCell>{dnsGuide?.name}</TableCell>
                                        <TableCell>{dnsGuide?.value}</TableCell>
                                        <TableCell>3600</TableCell>
                                    </TableRow>
                                </TableBody>
                            </Table>
                        </div>
                    </div>

                    <div className="rounded-md bg-muted p-4">
                        <h4 className="font-medium mb-2">DNS Propagation</h4>
                        <p className="text-sm text-muted-foreground">
                            DNS changes can take up to 48 hours to propagate
                            worldwide, though they often take effect much sooner.
                            {/* We'll automatically check your DNS configuration and
                notify you when your domain is properly configured. */}
                        </p>
                    </div>
                </div>
                <DialogFooter>
                    <Button
                        variant="outline"
                        onClick={() => props.onClose?.()}
                    >
                        Close
                    </Button>
                    <Button>Verify DNS Configuration</Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}

export default VerifyDnsDialog;