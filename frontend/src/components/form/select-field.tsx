import React from 'react';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { FieldInfo } from './field-info';
import { useAppFieldContext } from './use-app-form';
import { cn } from '@/lib/utils';

export interface Props {
    className?: string
    label?: string
    placeHolder?: string
    values: readonly string[]
}

const SelectField: React.FC<Props> = (props) => {
    const field = useAppFieldContext<string>();

    return (
        <div className={cn('flex flex-col', props.className)}>
            {props.label && <label htmlFor={field.name} className='text-sm'>{props.label}</label>}
            <Select value={field.state.value} onValueChange={(value) => field.handleChange(value)} >
                <SelectTrigger id={field.name}>
                    <SelectValue placeholder="Select system type" />
                </SelectTrigger>
                <SelectContent>
                    {
                        props.values.map(x => (
                            <SelectItem key={x} value={x}>
                                {x}
                            </SelectItem>
                        ))
                    }
                </SelectContent>
            </Select>
            <FieldInfo field={field} />
        </div>
    );
}

export default SelectField;