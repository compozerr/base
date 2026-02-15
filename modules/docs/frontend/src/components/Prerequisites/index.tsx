import type { ReactNode } from 'react';
import styles from './styles.module.css';

interface PrerequisitesProps {
  items: string[];
}

function CheckIcon() {
  return (
    <svg
      className={styles.checkmark}
      viewBox="0 0 16 16"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
    >
      <path
        d="M13.25 4.75L6 12L2.75 8.75"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

export default function Prerequisites({ items }: PrerequisitesProps): ReactNode {
  return (
    <div className={styles.prerequisites}>
      <div className={styles.prerequisitesHeader}>Before you begin</div>
      <ul className={styles.prerequisitesList}>
        {items.map((item, idx) => (
          <li key={idx} className={styles.prerequisitesItem}>
            <CheckIcon />
            {item}
          </li>
        ))}
      </ul>
    </div>
  );
}
