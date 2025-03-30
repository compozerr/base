import React from 'react';
import { Button, ButtonProps } from './ui/button';
import { LoaderIcon } from 'lucide-react';

interface Props extends ButtonProps {
    isLoading: boolean;
}

const LoadingButton: React.FC<Props> = ({isLoading, ...restProps}) => {
    return (
        <Button
            {...restProps}
            disabled={isLoading || restProps.disabled}
        >
            {isLoading ? (
                <LoaderIcon className='animate-spin' />
            ) : (
                restProps.children
            )}
        </Button>
    );
}

export default LoadingButton;