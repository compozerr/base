import {
  Body,
  Button,
  Container,
  Head,
  Hr,
  Html,
  Link,
  Preview,
  Section,
  Text,
  Row,
  Column,
} from '@react-email/components';

export const ProjectHasBeenTerminated = () => (
  <Html>
    <Head />
    <Preview>Your project has been terminated</Preview>
    <Body style={main}>
      <Container style={container}>
        {/* Simple header */}
        <Section style={header}>
          <Text style={logo}>% CompanyName %</Text>
          <Text style={headerSubtitle}>Project Termination Notice</Text>
        </Section>

        <Section style={content}>
          <Text style={greeting}>Hi % CustomerName %,</Text>

          <Text style={paragraph}>
            We're writing to inform you that your project has been terminated.
          </Text>

          {/* Reason card */}
          <Section style={reasonCard}>
            <Text style={cardTitle}>Reason</Text>
            <Text style={reasonText}>% Reason %</Text>
          </Section>

          <Text style={paragraph}>
            If you have any questions or believe this was done in error, please contact our support team.
          </Text>

          {/* Simple links */}
          <Section style={linksSection}>
            <Link style={link} href="% DashboardLink %">Dashboard</Link>
            <Text style={linkSeparator}>•</Text>
            <Link style={link} href="% ContactLink %">Contact Support</Link>
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

export default ProjectHasBeenTerminated;

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

const reasonCard = {
  backgroundColor: '#1a1a1a',
  border: '1px solid #2a2a2a',
  borderRadius: '8px',
  padding: '24px',
  margin: '32px 0',
};

const cardTitle = {
  color: '#ffffff',
  fontSize: '16px',
  fontWeight: '600',
  margin: '0 0 16px 0',
};

const reasonText = {
  color: '#cccccc',
  fontSize: '15px',
  lineHeight: '22px',
  margin: '0',
};

const projectName = {
  color: '#ffffff',
  fontWeight: '500',
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

const signature = {
  color: '#888888',
  fontSize: '14px',
  margin: '40px 0 0 0',
  fontWeight: '500',
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

const footerLinks = {
  margin: '0',
};

const footerLink = {
  color: '#666666',
  fontSize: '12px',
  textDecoration: 'underline',
};