import type { ReactNode } from 'react';
import Link from '@docusaurus/Link';
import styles from './styles.module.css';

interface ReadMoreProps {
  to: string;
  title: string;
  description: string;
}

export default function ReadMore({ to, title, description }: ReadMoreProps): ReactNode {
  return (
    <Link to={to} className={styles.readMore}>
      <div className={styles.readMoreContent}>
        <span className={styles.readMoreLabel}>Read more</span>
        <span className={styles.readMoreTitle}>{title}</span>
        <span className={styles.readMoreDescription}>{description}</span>
      </div>
      <span className={styles.readMoreArrow}>&rarr;</span>
    </Link>
  );
}
