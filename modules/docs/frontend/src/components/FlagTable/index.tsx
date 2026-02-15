import type { ReactNode } from 'react';
import styles from './styles.module.css';

interface Flag {
  name: string;
  type: string;
  default?: string;
  description: string;
}

interface FlagTableProps {
  flags: Flag[];
}

export default function FlagTable({ flags }: FlagTableProps): ReactNode {
  return (
    <div className={styles.flagTableWrapper}>
      <table className={styles.flagTable}>
        <thead>
          <tr>
            <th>Flag</th>
            <th>Type</th>
            <th>Default</th>
            <th>Description</th>
          </tr>
        </thead>
        <tbody>
          {flags.map((flag, idx) => (
            <tr key={idx}>
              <td><code>{flag.name}</code></td>
              <td><span className={styles.typeBadge}>{flag.type}</span></td>
              <td>{flag.default ? <code>{flag.default}</code> : <span className={styles.noDefault}>&mdash;</span>}</td>
              <td>{flag.description}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
