import type { ReactNode } from 'react';
import clsx from 'clsx';
import Heading from '@theme/Heading';
import styles from './styles.module.css';

type FeatureItem = {
  title: string;
  icon: string;
  description: ReactNode;
};

const FeatureList: FeatureItem[] = [
  {
    title: 'Any Stack',
    icon: '\u26A1',
    description: (
      <>
        Deploy Docker Compose projects, Next.js apps, .NET backends,
        static sites, or anything with a Dockerfile. Your stack, your rules.
      </>
    ),
  },
  {
    title: 'Auto SSL',
    icon: '\uD83D\uDD12',
    description: (
      <>
        Custom domains with automatic HTTPS certificates via Let's Encrypt.
        Point your DNS and you're live — zero configuration needed.
      </>
    ),
  },
  {
    title: 'Module System',
    icon: '\uD83E\uDDE9',
    description: (
      <>
        Plug-and-play full-stack modules for auth, payments, databases, and more.
        Add features in seconds with <code>compozerr add</code>.
      </>
    ),
  },
];

function Feature({ title, icon, description }: FeatureItem) {
  return (
    <div className={clsx('col col--4')}>
      <div className={styles.featureCard}>
        <div className={styles.featureIcon}>{icon}</div>
        <Heading as="h3" className={styles.featureTitle}>{title}</Heading>
        <p className={styles.featureDescription}>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures(): ReactNode {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
