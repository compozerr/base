import React from 'react';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from './ui/dialog';
import { Button } from './ui/button';

interface Props {
    title: string,
    subtitle: string,
    onAnswer: (wasSure: boolean) => void,
    open: boolean
}

const AreYouSureDialog: React.FC<Props> = (props) => {
    return (
        <Dialog open={props.open}>
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
                        onClick={() => props.onAnswer(false)}
                    >
                        No
                    </Button>
                    <Button
                        variant="destructive"
                        onClick={() => props.onAnswer(true)}
                    >
                        Yes
                    </Button>
                </div>
            </DialogContent>
        </Dialog>
    );
}

export default AreYouSureDialog;