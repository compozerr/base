import type { ReactNode } from 'react';
import Link from '@docusaurus/Link';
import styles from './styles.module.css';

interface WhatNextItem {
  title: string;
  to: string;
  description: string;
}

interface WhatNextProps {
  items: WhatNextItem[];
}

export default function WhatNext({ items }: WhatNextProps): ReactNode {
  return (
    <div className={styles.whatNext}>
      <h3 className={styles.whatNextHeading}>What's next?</h3>
      <div className={styles.whatNextGrid}>
        {items.map((item, idx) => (
          <Link key={idx} to={item.to} className={styles.whatNextCard}>
            <span className={styles.whatNextTitle}>{item.title}</span>
            <span className={styles.whatNextDescription}>{item.description}</span>
            <span className={styles.whatNextArrow}>&rarr;</span>
          </Link>
        ))}
      </div>
    </div>
  );
}
