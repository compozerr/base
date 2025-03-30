import React from 'react';
import { Input } from '../ui/input';
import { FieldInfo } from './field-info';
import { useAppFieldContext } from './use-app-form';
import { cn } from '@/lib/utils';

export interface Props {
    label?: string
    placeholder?: string
    className?: string
}

const TextField: React.FC<Props> = (props) => {
    const field = useAppFieldContext<string>();

    return (
        <div className={cn('flex flex-col', props.className)}>
            {props.label && <label htmlFor={field.name} className='text-sm'>{props.label}</label>}
            <Input
                id={field.name}
                name={field.name}
                value={field.state.value}
                onBlur={field.handleBlur}
                onChange={(e) => field.handleChange(e.target.value)}
                placeholder={props.placeholder}
            />
            <FieldInfo field={field} />
        </div>
    );
}

export default TextField;