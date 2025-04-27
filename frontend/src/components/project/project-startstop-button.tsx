import { api } from '@/api-client';
import { components } from '@/generated';
import React from 'react';
import { ButtonProps } from '../ui/button';
import StartProjectButton from './start-project-button';
import StopProjectButton from './stop-project-button';
import { getRouteApi } from '@tanstack/react-router';

interface Props extends ButtonProps {
    projectId: string,
    state: components["schemas"]["ProjectState"]
}

const StartStopProjectButton: React.FC<Props> = (props) => {
    const invalidateAsync = async () => {
        await api.v1.getProjects.invalidateQueries();
        await api.v1.getProjectsProjectId.invalidateQueries({ parameters: { path: { projectId: props.projectId } } })
    }

    const { projectId, state, ...propsRest } = props;

    if (state === "Running") return <StopProjectButton {...propsRest} projectId={projectId} onStopped={() => invalidateAsync()} />
    if (state === "Stopped") return <StartProjectButton {...propsRest} projectId={projectId} onStarted={() => invalidateAsync()} />

    return <StartProjectButton {...propsRest} projectId={projectId} disabled />
}

export default StartStopProjectButton;