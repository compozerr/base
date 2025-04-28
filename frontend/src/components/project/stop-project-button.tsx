import { api } from '@/api-client';
import React, { useState } from 'react';
import LoadingButton from '../loading-button';
import { Pause } from 'lucide-react';
import { ButtonProps } from '../ui/button';

interface Props extends ButtonProps {
    projectId: string,
    onStopped?: () => void
}

const StopProjectButton: React.FC<Props> = (props) => {
    const [isLoading, setIsLoading] = useState(false);

    const { projectId, onStopped, ...propsRest } = props;

    const pause = async () => {
        setIsLoading(true);
        api.v1.getProjectsProjectIdStop.fetchQuery({
            parameters: {
                path: {
                    projectId: projectId
                }
            },
            gcTime: 0
        }).finally(() => {
            setIsLoading(false);
            onStopped?.();
        });
    }

    return (
        <LoadingButton {...propsRest} size="sm" variant={props.variant || "ghost"} isLoading={isLoading} onClick={pause}>
            <Pause className="h-4 w-4" />
        </LoadingButton>
    );
}

export default StopProjectButton;