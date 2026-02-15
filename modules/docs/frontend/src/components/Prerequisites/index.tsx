import type { ReactNode } from 'react';
import styles from './styles.module.css';

interface PrerequisitesProps {
  items: string[];
}

export default function Prerequisites({ items }: PrerequisitesProps): ReactNode {
  return (
    <div className={styles.prerequisites}>
      <div className={styles.prerequisitesHeader}>Before you begin</div>
      <ul className={styles.prerequisitesList}>
        {items.map((item, idx) => (
          <li key={idx} className={styles.prerequisitesItem}>
            <span className={styles.checkmark}>&check;</span>
            {item}
          </li>
        ))}
      </ul>
    </div>
  );
}
