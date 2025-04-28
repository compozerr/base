import { api } from '@/api-client';
import { Play } from 'lucide-react';
import React, { useState } from 'react';
import LoadingButton from '../loading-button';
import { ButtonProps } from '../ui/button';

interface Props extends ButtonProps {
    projectId: string,
    onStarted?: () => void,
}

const StopProjectButton: React.FC<Props> = (props) => {
    const [isLoading, setIsLoading] = useState(false);

    const { projectId, onStarted, ...propsRest } = props;

    const start = () => {
        setIsLoading(true);
        api.v1.getProjectsProjectIdStart.fetchQuery({
            parameters: {
                path: {
                    projectId: projectId
                }
            },
            gcTime: 0
        }).finally(() => {
            setIsLoading(false);
            onStarted?.();
        });
    }

    return (
        <LoadingButton {...propsRest} size="sm" variant={props.variant || "ghost"} isLoading={isLoading} onClick={(e) => { e.stopPropagation(); start() }}>
            <Play className="h-4 w-4" />
        </LoadingButton>
    );
}

export default StopProjectButton;