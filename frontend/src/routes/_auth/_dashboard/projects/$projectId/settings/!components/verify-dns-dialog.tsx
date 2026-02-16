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
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
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
import { AlertTriangle, Clock } from 'lucide-react'
import React from 'react'

interface Props {
    projectId: string
    domains: Awaited<ReturnType<typeof api.v1.getProjectsProjectIdDomains.fetchQuery>>["domains"]
    selectedDomainId: string | null
    onClose?: () => void;
}

// Check if domain is an apex/root domain (e.g., example.com vs www.example.com)
function isApexDomain(domain: string): boolean {
    // Remove trailing dot if present
    const cleanDomain = domain.replace(/\.$/, '');
    const parts = cleanDomain.split('.');

    // Common multi-part TLDs
    const multiPartTlds = ['co.uk', 'com.au', 'co.nz', 'com.br', 'co.jp', 'co.kr', 'com.cn', 'org.uk', 'net.au'];

    // Check if domain ends with a multi-part TLD
    for (const tld of multiPartTlds) {
        if (cleanDomain.endsWith(`.${tld}`) || cleanDomain === tld) {
            // For multi-part TLDs, apex has 3 parts (e.g., example.co.uk)
            return parts.length <= 3;
        }
    }

    // For standard TLDs, apex has 2 parts (e.g., example.com)
    return parts.length <= 2;
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

    const selectedDomainValue = useMemo(() =>
        props.domains?.find(x => x.domainId == props.selectedDomainId),
        [props.domains, props.selectedDomainId]
    );

    // Fetch existing DNS records to check for collisions
    const { data: dnsLookupData, isLoading: dnsLookupLoading } = api.v1.getProjectsProjectIdDomainsLookup.useQuery({
        path: { projectId: props.projectId },
        query: { Domain: selectedDomainValue?.value ?? '' }
    }, { enabled: !!selectedDomainValue?.value && isOpen });

    const dnsGuide = useMemo(() => {
        if (!props.selectedDomainId || !selectedDomainValue || !parentDomainData?.domain) return null;

        const domainName = selectedDomainValue.value!;
        const isApex = isApexDomain(domainName);

        return {
            name: domainName,
            value: parentDomainData.domain,
            serverIp: parentDomainData.serverIp,
            isApexDomain: isApex,
            // For apex domains, we need A record. For subdomains, CNAME works.
            recordType: isApex ? 'A' : 'CNAME',
            recordValue: isApex ? parentDomainData.serverIp : parentDomainData.domain
        }
    }, [parentDomainData, props.selectedDomainId, selectedDomainValue]);

    // Determine which existing records conflict
    const conflictingRecords = useMemo(() => {
        if (!dnsLookupData?.records || !dnsGuide) return [];

        const domainToCheck = dnsGuide.name.toLowerCase().replace(/\.$/, '');

        // Records that would conflict with the required setup
        return dnsLookupData.records.filter(record => {
            // Only consider records for the exact domain we're adding
            const recordName = (record.name ?? '').toLowerCase().replace(/\.$/, '');
            if (recordName !== domainToCheck) {
                return false;
            }

            // Any existing A, AAAA, or CNAME record is a potential conflict
            if (['A', 'AAAA', 'CNAME'].includes(record.type ?? '')) {
                // Check if it already points to the correct value
                if (dnsGuide.isApexDomain && record.type === 'A' && record.value === dnsGuide.serverIp) {
                    return false; // Already correct
                }
                if (!dnsGuide.isApexDomain && record.type === 'CNAME' && record.value === dnsGuide.value) {
                    return false; // Already correct
                }
                return true;
            }
            return false;
        });
    }, [dnsLookupData, dnsGuide]);

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
                    {/* Conflicting Records Warning */}
                    {!dnsLookupLoading && conflictingRecords.length > 0 && (
                        <Alert variant="destructive">
                            <AlertTriangle className="h-4 w-4" />
                            <AlertTitle>Remove these DNS records</AlertTitle>
                            <AlertDescription>
                                <div className="mt-2 rounded-md border border-destructive/30 overflow-hidden">
                                    <Table>
                                        <TableHeader>
                                            <TableRow className="border-destructive/30">
                                                <TableHead className="text-destructive-foreground text-xs">Type</TableHead>
                                                <TableHead className="text-destructive-foreground text-xs">Name</TableHead>
                                                <TableHead className="text-destructive-foreground text-xs">Value</TableHead>
                                            </TableRow>
                                        </TableHeader>
                                        <TableBody>
                                            {conflictingRecords.map((record, index) => (
                                                <TableRow key={index} className="border-destructive/20">
                                                    <TableCell className="font-mono text-sm font-medium">{record.type}</TableCell>
                                                    <TableCell className="font-mono text-sm">{record.name}</TableCell>
                                                    <TableCell className="font-mono text-sm break-all">{record.value}</TableCell>
                                                </TableRow>
                                            ))}
                                        </TableBody>
                                    </Table>
                                </div>
                            </AlertDescription>
                        </Alert>
                    )}

                    <div>
                        <h4 className="text-sm font-medium mb-2">
                            Add {dnsGuide?.recordType ?? 'DNS'} record
                        </h4>
                        <div className="rounded-lg border border-zinc-800 overflow-hidden">
                            <Table>
                                <TableHeader>
                                    <TableRow className="border-zinc-800">
                                        <TableHead className="text-xs">Type</TableHead>
                                        <TableHead className="text-xs">Name</TableHead>
                                        <TableHead className="text-xs">Value</TableHead>
                                        <TableHead className="text-xs">TTL</TableHead>
                                    </TableRow>
                                </TableHeader>
                                <TableBody>
                                    <TableRow className="border-zinc-800">
                                        <TableCell className="font-mono text-sm font-medium">
                                            {dnsGuide?.recordType ?? 'CNAME'}
                                        </TableCell>
                                        <TableCell className="font-mono text-sm">
                                            {dnsGuide?.name && <CopyText value={dnsGuide.name}>{dnsGuide.name}</CopyText>}
                                        </TableCell>
                                        <TableCell className="font-mono text-sm">
                                            {dnsGuide?.recordValue ? (
                                                <CopyText value={dnsGuide.recordValue}>{dnsGuide.recordValue}</CopyText>
                                            ) : (
                                                <span className="text-muted-foreground">
                                                    {dnsGuide?.isApexDomain ? 'Server IP not available' : 'Loading...'}
                                                </span>
                                            )}
                                        </TableCell>
                                        <TableCell className="font-mono text-sm text-muted-foreground">3600</TableCell>
                                    </TableRow>
                                </TableBody>
                            </Table>
                        </div>
                    </div>

                    <div className="rounded-lg bg-zinc-900/50 border border-zinc-800 p-4">
                        <div className="flex items-start gap-3">
                            <Clock className="h-4 w-4 text-muted-foreground mt-0.5 flex-shrink-0" />
                            <div>
                                <h4 className="text-sm font-medium mb-1">DNS Propagation</h4>
                                <p className="text-sm text-muted-foreground">
                                    DNS changes can take up to 48 hours to propagate
                                    worldwide, though they often take effect much sooner.
                                </p>
                            </div>
                        </div>
                    </div>
                </div>
                <DialogFooter className="gap-2">
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
