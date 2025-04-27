import React, { useState, useEffect } from 'react';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from './ui/dialog';
import { Button } from './ui/button';
import { Input } from './ui/input';

interface Props {
    title: string,
    subtitle: string,
    textToAnswer: string,
    cancelButton: string
    destructiveButton: string
    onAnswer: (wasSure: boolean) => void,
    open: boolean
}

const AreYouSureDialogConfirmWithText: React.FC<Props> = (props) => {
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

    const [confirmationInputText, setConfirmationInputText] = useState("");

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
                <section className='gap-2 flex flex-col'>
                    <p className='text-sm'>Type '<span className='font-bold'>{props.textToAnswer}</span>' to confirm</p>
                    <Input value={confirmationInputText} onChange={(e) => setConfirmationInputText(e.target.value)}></Input>
                </section>
                <div className="flex justify-end space-x-2">
                    <Button
                        variant="ghost"
                        onClick={() => handleOpenChange(false)}
                    >
                        {props.cancelButton}
                    </Button>
                    <Button
                        variant="destructive"
                        onClick={() => {
                            handleOpenChange(false);
                            props.onAnswer(true);
                        }}
                        disabled={confirmationInputText !== props.textToAnswer}
                    >
                        {props.destructiveButton}
                    </Button>
                </div>
            </DialogContent>
        </Dialog>
    );
}

export default AreYouSureDialogConfirmWithText;