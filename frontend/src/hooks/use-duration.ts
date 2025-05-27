import { useState, useEffect } from 'react';

export function useDuration(timespan: string | number, enabled: boolean = true) {
    const [duration, setDuration] = useState<string>('');
    const [elapsedSeconds, setElapsedSeconds] = useState<number>(0);
    
    useEffect(() => {
        const parseTimespan = (): number => {
            let diffInSeconds: number;
            
            // Handle timespan format (HH:MM:SS.ms)
            if (typeof timespan === 'string' && timespan.includes(':')) {
                const parts = timespan.split(':');
                const seconds = parts[2] ? parseFloat(parts[2]) : 0;
                const minutes = parts[1] ? parseInt(parts[1], 10) * 60 : 0;
                const hours = parts[0] ? parseInt(parts[0], 10) * 3600 : 0;
                diffInSeconds = hours + minutes + seconds;
            } else {
                // Handle numeric milliseconds
                diffInSeconds = typeof timespan === 'number' ? 
                    timespan / 1000 : 
                    parseFloat(timespan) / 1000;
            }
            
            return diffInSeconds;
        };
        
        const formatDuration = (seconds: number): string => {
            if (seconds < 60) return `${Math.floor(seconds)}s`;
            if (seconds < 3600) return `${Math.floor(seconds / 60)}m ${Math.floor(seconds % 60)}s`;
            if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ${Math.floor((seconds % 3600) / 60)}m`;
            return `${Math.floor(seconds / 86400)}d ${Math.floor((seconds % 86400) / 3600)}h`;
        };

        const calculateDuration = () => {
            const baseDuration = parseTimespan();
            return formatDuration(enabled ? baseDuration + elapsedSeconds : baseDuration);
        };

        setDuration(calculateDuration());
        
        let interval: NodeJS.Timeout | null = null;
        
        if (enabled) {
            interval = setInterval(() => {
                setElapsedSeconds(prev => {
                    const newValue = prev + 1;
                    // Update duration after incrementing elapsed seconds
                    setDuration(formatDuration(parseTimespan() + newValue));
                    return newValue;
                });
            }, 1000);
        }

        return () => {
            if (interval) clearInterval(interval);
        };
    }, [enabled]); 

    return duration;
}
