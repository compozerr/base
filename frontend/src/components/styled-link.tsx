import React from 'react';
import { ButtonProps, buttonVariants } from './ui/button';
import { cn } from '@/lib/utils';
import { Link } from '@tanstack/react-router';

type Props = ButtonProps & React.AnchorHTMLAttributes<HTMLAnchorElement> & {
    children: React.ReactNode
}

const StyledLink: React.FC<Props> = (props) => {
    return (
        <Link href="/login"
            className={cn(buttonVariants({ variant: props.variant, size: props.size, className: props.className }))}
        >{props.children}</Link>
    );
}

export default StyledLink;