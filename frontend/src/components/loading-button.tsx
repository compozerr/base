import React from 'react';
import { Button, ButtonProps } from './ui/button';
import { LoaderIcon } from 'lucide-react';

interface Props extends ButtonProps {
    isLoading: boolean;
}

const LoadingButton: React.FC<Props> = (props) => {
    return (
        <Button
            {...props}
            disabled={props.isLoading || props.disabled}
        >
            {props.isLoading ? (
                <LoaderIcon className='animate-spin' />
            ) : (
                props.children
            )}
        </Button>
    );
}

export default LoadingButton;