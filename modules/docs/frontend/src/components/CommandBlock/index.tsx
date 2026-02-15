import { type ReactNode, useState } from 'react';
import styles from './styles.module.css';

interface CommandBlockProps {
  command: string;
  output?: string;
}

export default function CommandBlock({ command, output }: CommandBlockProps): ReactNode {
  const [copied, setCopied] = useState(false);

  const handleCopy = () => {
    navigator.clipboard.writeText(command);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className={styles.commandBlock}>
      <div className={styles.commandHeader}>
        <div className={styles.dots}>
          <span className={styles.dot} />
          <span className={styles.dot} />
          <span className={styles.dot} />
        </div>
        <button
          className={styles.copyButton}
          onClick={handleCopy}
          title="Copy command"
        >
          {copied ? 'Copied!' : 'Copy'}
        </button>
      </div>
      <div className={styles.commandBody}>
        <div className={styles.commandLine}>
          <span className={styles.prompt}>$</span>
          <span className={styles.command}>{command}</span>
        </div>
        {output && (
          <div className={styles.output}>
            {output.split('\n').map((line, idx) => (
              <div key={idx} className={styles.outputLine}>{line}</div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
