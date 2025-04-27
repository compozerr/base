import React, { useState } from 'react';
import { ButtonProps } from '../ui/button';
import { components } from '@/generated';
import StopProjectButton from './stop-project-button';
import StartProjectButton from './start-project-button';

interface Props extends ButtonProps {
    projectId: string,
    state: components["schemas"]["ProjectState"]
}

const StartStopProjectButton: React.FC<Props> = (props) => {
    if (props.state === "Running") return <StopProjectButton {...props} />
    if (props.state === "Stopped") return <StartProjectButton {...props} />

    return <StartProjectButton {...props} disabled />
}

export default StartStopProjectButton;