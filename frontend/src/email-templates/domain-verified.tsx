import {
  Body,
  Container,
  Head,
  Html,
  Link,
  Preview,
  Section,
  Text,
} from '@react-email/components';

export const DomainVerified = () => (
  <Html>
    <Head />
    <Preview>Your domain has been verified</Preview>
    <Body style={main}>
      <Container style={container}>
        {/* Simple header */}
        <Section style={header}>
          <Text style={logo}>% CompanyName %</Text>
          <Text style={headerSubtitle}>Domain Verified</Text>
        </Section>

        <Section style={content}>
          <Text style={greeting}>Hi % CustomerName %,</Text>

          <Text style={paragraph}>
            Great news! Your custom domain has been verified and is now active for your project.
          </Text>

          {/* Domain card */}
          <Section style={domainCard}>
            <Text style={cardLabel}>Domain</Text>
            <Text style={domainName}>% DomainName %</Text>
            <Text style={projectLabel}>% ProjectName %</Text>
          </Section>

          <Text style={paragraph}>
            Your domain is now live and serving your project. DNS propagation is complete.
          </Text>

          {/* Simple links */}
          <Section style={linksSection}>
            <Link style={link} href="% DashboardLink %">View Project</Link>
            <Text style={linkSeparator}>•</Text>
            <Link style={link} href="% DomainLink %">Visit Domain</Link>
          </Section>

        </Section>

        {/* Minimal footer */}
        <Section style={footer}>
          <Text style={footerText}>
            % CompanyAddress %
          </Text>
        </Section>
      </Container>
    </Body>
  </Html>
);

export default DomainVerified;

const main = {
  backgroundColor: '#0a0a0a',
  fontFamily: '-apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,"Helvetica Neue",Ubuntu,sans-serif',
  padding: '',
};

const container = {
  backgroundColor: '#111111',
  margin: '0 auto',
  maxWidth: '560px',
  borderRadius: '12px',
  border: '1px solid #222222',
};

const header = {
  padding: '40px 40px 32px 40px',
  borderBottom: '1px solid #222222',
};

const logo = {
  color: '#ffffff',
  fontSize: '20px',
  fontWeight: '600',
  margin: '0 0 8px 0',
};

const headerSubtitle = {
  color: '#888888',
  fontSize: '14px',
  margin: '0',
  fontWeight: '400',
};

const content = {
  padding: '40px',
};

const greeting = {
  color: '#ffffff',
  fontSize: '16px',
  fontWeight: '500',
  margin: '0 0 24px 0',
};

const paragraph = {
  color: '#cccccc',
  fontSize: '15px',
  lineHeight: '22px',
  margin: '0 0 24px 0',
};

const domainCard = {
  backgroundColor: '#0d2818',
  border: '1px solid #1a4d2e',
  borderRadius: '8px',
  padding: '24px',
  margin: '32px 0',
  textAlign: 'center' as const,
};

const cardLabel = {
  color: '#4ade80',
  fontSize: '12px',
  fontWeight: '500',
  textTransform: 'uppercase' as const,
  letterSpacing: '0.05em',
  margin: '0 0 8px 0',
};

const domainName = {
  color: '#ffffff',
  fontSize: '20px',
  fontWeight: '600',
  margin: '0 0 8px 0',
};

const projectLabel = {
  color: '#888888',
  fontSize: '14px',
  margin: '0',
};

const linksSection = {
  textAlign: 'center' as const,
  margin: '32px 0',
};

const link = {
  color: '#888888',
  textDecoration: 'none',
  fontSize: '14px',
  fontWeight: '400',
};

const linkSeparator = {
  color: '#444444',
  margin: '0 12px',
  fontSize: '14px',
};

const footer = {
  padding: '24px 40px',
  borderTop: '1px solid #222222',
  textAlign: 'center' as const,
};

const footerText = {
  color: '#666666',
  fontSize: '12px',
  margin: '0 0 8px 0',
};
