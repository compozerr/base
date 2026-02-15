import { themes as prismThemes } from 'prism-react-renderer';
import type { Config } from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'compozerr',
  tagline: 'Deploy anything. From code to cloud in one command.',
  favicon: 'img/favicon.ico',
  url: 'https://docs.compozerr.com',
  baseUrl: '/',
  organizationName: 'compozerr',
  projectName: 'compozerr',
  onBrokenLinks: 'throw',
  markdown: {
    hooks: {
      onBrokenMarkdownLinks: 'warn',
    },
  },
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },
  presets: [
    [
      'classic',
      {
        docs: {
          routeBasePath: '/',
          sidebarPath: './sidebars.ts',
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],
  themeConfig: {
    colorMode: {
      disableSwitch: true,
      defaultMode: 'dark',
      respectPrefersColorScheme: false,
    },
    navbar: {
      title: 'compozerr',
      style: 'dark',
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'mainSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          href: 'https://github.com/compozerr',
          label: 'GitHub',
          position: 'right',
        },
        {
          href: 'https://compozerr.com',
          label: 'Dashboard',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Learn',
          items: [
            { label: 'Getting Started', to: '/getting-started/installation' },
            { label: 'Tutorials', to: '/tutorials/deploy-docker-compose' },
          ],
        },
        {
          title: 'Reference',
          items: [
            { label: 'CLI Commands', to: '/reference/cli/deploy' },
            { label: 'Configuration', to: '/reference/configuration/compozerr-json' },
          ],
        },
        {
          title: 'More',
          items: [
            { label: 'GitHub', href: 'https://github.com/compozerr' },
            { label: 'Dashboard', href: 'https://compozerr.com' },
          ],
        },
      ],
      copyright: `Copyright \u00a9 ${new Date().getFullYear()} Compozerr`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['bash', 'json', 'csharp', 'docker', 'yaml', 'nginx'],
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
