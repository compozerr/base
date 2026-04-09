import {
  Body,
  Button,
  Container,
  Head,
  Html,
  Preview,
  Section,
  Text,
} from '@react-email/components';

export const OrganizationInvitation = () => (
  <Html>
    <Head />
    <Preview>You've been invited to join an organization</Preview>
    <Body style={main}>
      <Container style={container}>
        <Section style={header}>
          <Text style={logo}>% CompanyName %</Text>
          <Text style={headerSubtitle}>Organization Invitation</Text>
        </Section>

        <Section style={content}>
          <Text style={greeting}>Hi there,</Text>

          <Text style={paragraph}>
            % InviterName % has invited you to join <strong style={{ color: '#ffffff' }}>% OrganizationName %</strong> as
            a <strong style={{ color: '#ffffff' }}>% Role %</strong>.
          </Text>

          <Section style={buttonSection}>
            <Button style={button} href="% AcceptLink %">
              Accept Invitation
            </Button>
          </Section>

          <Text style={finePrint}>
            This invitation expires in 7 days. If you didn't expect this email, you can safely ignore it.
          </Text>
        </Section>

        <Section style={footer}>
          <Text style={footerText}>
            % CompanyAddress %
          </Text>
        </Section>
      </Container>
    </Body>
  </Html>
);

export default OrganizationInvitation;

const main = {
  backgroundColor: '#0a0a0a',
  fontFamily: '-apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,"Helvetica Neue",Ubuntu,sans-serif',
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

const buttonSection = {
  textAlign: 'center' as const,
  margin: '32px 0',
};

const button = {
  backgroundColor: '#ffffff',
  color: '#0a0a0a',
  fontSize: '14px',
  fontWeight: '600',
  textDecoration: 'none',
  padding: '12px 32px',
  borderRadius: '8px',
};

const finePrint = {
  color: '#888888',
  fontSize: '13px',
  lineHeight: '20px',
  margin: '24px 0 0 0',
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
