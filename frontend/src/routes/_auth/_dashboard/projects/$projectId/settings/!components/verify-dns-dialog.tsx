import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table'
import { useEffect, useMemo, useState } from 'react'

import { api } from '@/api-client'
import { CopyText } from '@/components/copy-text'
import LoadingButton from '@/components/loading-button'
import { Button } from '@/components/ui/button'
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle
} from "@/components/ui/dialog"
import { useToast } from '@/hooks/use-toast'
import React from 'react'

interface Props {
    projectId: string
    domains: Awaited<ReturnType<typeof api.v1.getProjectsProjectIdDomains.fetchQuery>>["domains"]
    selectedDomainId: string | null
    onClose?: () => void;
}

const VerifyDnsDialog: React.FC<Props> = (props) => {

    const { toast } = useToast();

    const [verifyLoading, setVerifyLoading] = useState<boolean>(false);
    const [isOpen, setIsOpen] = useState<boolean>(false);

    // Ensure dialog state is properly synced with selectedDomainId
    useEffect(() => {
        setIsOpen(!!props.selectedDomainId);
    }, [props.selectedDomainId]);

    const checkIfVerifiedAsync = async () => {
        setVerifyLoading(true);
        const result = await api.v1.getProjectsProjectIdDomainsDomainIdVerify.fetchQuery({
            parameters: {
                path: {

                    projectId: props.projectId,
                    domainId: props.selectedDomainId!
                }
            },
            staleTime: 0
        });
        setVerifyLoading(false);

        if (result) {
            toast({
                title: "Domain verified",
                description: "Your domain has been verified successfully.",
                variant: "default"
            });

            props.onClose?.();
        } else {
            toast({
                title: "Domain not verified",
                description: "Your domain has not been verified yet. Please try again later.",
                variant: "destructive"
            });
        }
    }

    const { data: parentDomainData } = api.v1.getProjectsProjectIdDomainsDomainIdParent.useQuery({
        path: {
            domainId: props.selectedDomainId!,
            projectId: props.projectId
        }
    }, { enabled: !!props.selectedDomainId && !!props.domains });

    const dnsGuide = useMemo(() => {
        const selectedDomainValue = props.domains?.find(x => x.domainId == props.selectedDomainId);
        if (!props.selectedDomainId || !selectedDomainValue || !parentDomainData?.domain) return null;

        return {
            name: selectedDomainValue.value,
            value: parentDomainData.domain
        }
    }, [parentDomainData, props.selectedDomainId, props.domains]);

    const handleOpenChange = (open: boolean) => {
        setIsOpen(open);
        if (!open) {
            // Ensure we call onClose after state update
            setTimeout(() => {
                props.onClose?.();
            }, 0);
        }
    };

    return (
        <Dialog
            key={`dialog-${props.selectedDomainId || 'closed'}`}
            open={isOpen}
            onOpenChange={handleOpenChange}
        >
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
                                        <TableCell>
                                            {dnsGuide?.name && <CopyText value={dnsGuide.name} >{dnsGuide.name}</CopyText>}
                                        </TableCell>
                                        <TableCell>
                                            {dnsGuide?.value && <CopyText value={dnsGuide.value} >{dnsGuide.value}</CopyText>}
                                        </TableCell>
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
                        onClick={() => handleOpenChange(false)}
                    >
                        Close
                    </Button>
                    <LoadingButton isLoading={verifyLoading} onClick={() => {
                        checkIfVerifiedAsync();
                    }}>Verify DNS Configuration</LoadingButton>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}

export default VerifyDnsDialog;