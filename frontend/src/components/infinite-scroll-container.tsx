import React, { useEffect } from 'react';
import { useInView } from 'react-intersection-observer';

interface Props extends React.HTMLAttributes<HTMLDivElement> {
    children: React.ReactNode;
    onBottomReached?: () => void;
}

const InfiniteScrollContainer: React.FC<Props> = (props) => {
    const { ref, inView } = useInView({
        rootMargin: "50px",
        onChange(isInView) {
            if (isInView) {
                props.onBottomReached?.();
            }
        }
    });

    useEffect(() => {
        if (inView) {
            requestAnimationFrame(() => {
                props.onBottomReached?.();
            });
        }
    }, [inView]);

    return (
        <div {...props}>
            {props.children}
            <div id="infinite-scroll-anchor" ref={ref} />
        </div>
    );
}

export default InfiniteScrollContainer;