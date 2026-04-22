import type { SidebarsConfig } from '@docusaurus/plugin-content-docs';

const sidebars: SidebarsConfig = {
  mainSidebar: [
    {
      type: 'category',
      label: 'Getting Started',
      collapsed: false,
      items: [
        'getting-started/installation',
        'getting-started/quick-start',
      ],
    },
    {
      type: 'category',
      label: 'Tutorials',
      collapsed: false,
      items: [
        'tutorials/deploy-docker-compose',
        'tutorials/deploy-nextjs',
        'tutorials/deploy-static-website',
        'tutorials/deploy-with-github-actions',
        'tutorials/build-fullstack-app',
        'tutorials/create-publish-module',
        'tutorials/custom-domains-ssl',
        'tutorials/ssh-into-project',
        'tutorials/monitor-deployments',
      ],
    },
    {
      type: 'category',
      label: 'Reference',
      collapsed: true,
      items: [
        {
          type: 'category',
          label: 'CLI Commands',
          collapsed: true,
          items: [
            'reference/cli/login',
            'reference/cli/new',
            'reference/cli/deploy',
            'reference/cli/deployment',
            'reference/cli/dev',
            'reference/cli/migrate',
            'reference/cli/ssh',
            'reference/cli/add-remove',
            'reference/cli/tiers',
            'reference/cli/cleanup',
            'reference/cli/update-reset',
          ],
        },
        {
          type: 'category',
          label: 'Configuration',
          collapsed: true,
          items: [
            'reference/configuration/compozerr-json',
            'reference/configuration/environment-variables',
          ],
        },
        {
          type: 'category',
          label: 'Architecture',
          collapsed: true,
          items: [
            'reference/architecture/how-deployment-works',
            'reference/architecture/module-system',
            'reference/architecture/frontend-architecture',
            'reference/architecture/project-structure',
          ],
        },
        'reference/github-action',
      ],
    },
    {
      type: 'category',
      label: 'Troubleshooting',
      collapsed: true,
      items: [
        'troubleshooting/project-wont-start',
        'troubleshooting/deployment-failures',
        'troubleshooting/domain-ssl-issues',
        'troubleshooting/common-errors',
      ],
    },
  ],
};

export default sidebars;
