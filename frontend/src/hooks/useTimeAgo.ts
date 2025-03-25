import { useState, useEffect } from 'react';

export function useTimeAgo(date: string) {
  const [timeAgo, setTimeAgo] = useState<string>('');

  useEffect(() => {
    const calculateTimeAgo = () => {
      const now = new Date();
      const past = new Date(date);
      const diffInSeconds = Math.floor((now.getTime() - past.getTime()) / 1000);

      if (diffInSeconds < 60) return `${diffInSeconds}s ago`;
      if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`;
      if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`;
      return `${Math.floor(diffInSeconds / 86400)}d ago`;
    };

    setTimeAgo(calculateTimeAgo());
    const interval = setInterval(() => {
      setTimeAgo(calculateTimeAgo());
    }, 1000);

    return () => clearInterval(interval);
  }, [date]);

  return timeAgo;
}
