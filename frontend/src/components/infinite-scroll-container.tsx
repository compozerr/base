import React, { useEffect } from 'react';
import { useInView } from 'react-intersection-observer';

interface Props extends React.HTMLAttributes<HTMLDivElement> {
    children: React.ReactNode;
    onBottomReached?: () => void;
}

const InfiniteScrollContainer: React.FC<Props> = ({ onBottomReached, children, ...divProps }) => {
    const { ref, inView } = useInView({
        rootMargin: "50px",
        onChange(isInView) {
            if (isInView) {
                onBottomReached?.();
            }
        }
    });

    useEffect(() => {
        if (inView) {
            requestAnimationFrame(() => {
                onBottomReached?.();
            });
        }
    }, [inView, onBottomReached]);

    return (
        <div {...divProps}>
            {children}
            <div ref={ref} />
        </div>
    );
}

export default InfiniteScrollContainer;