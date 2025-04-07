import React, { useState, useEffect } from 'react';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from './ui/dialog';
import { Button } from './ui/button';

interface Props {
    title: string,
    subtitle: string,
    onAnswer: (wasSure: boolean) => void,
    open: boolean
}

const AreYouSureDialog: React.FC<Props> = (props) => {
    const [isOpen, setIsOpen] = useState<boolean>(false);
    
    // Ensure dialog state is properly synced with open prop
    useEffect(() => {
        setIsOpen(!!props.open);
    }, [props.open]);
    
    const handleOpenChange = (open: boolean) => {
        setIsOpen(open);
        if (!open) {
            // Ensure we call onAnswer after state update
            setTimeout(() => {
                props.onAnswer(false);
            }, 0);
        }
    };
    
    return (
        <Dialog 
            key={`sure-dialog-${props.open ? 'open' : 'closed'}`} 
            open={isOpen} 
            onOpenChange={handleOpenChange}
        >
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>{props.title}</DialogTitle>
                    <DialogDescription>
                        {props.subtitle}
                    </DialogDescription>
                </DialogHeader>
                <div className="flex justify-end space-x-2">
                    <Button
                        variant="ghost"
                        onClick={() => handleOpenChange(false)}
                    >
                        No
                    </Button>
                    <Button
                        variant="destructive"
                        onClick={() => {
                            handleOpenChange(false);
                            props.onAnswer(true);
                        }}
                    >
                        Yes
                    </Button>
                </div>
            </DialogContent>
        </Dialog>
    );
}

export default AreYouSureDialog;