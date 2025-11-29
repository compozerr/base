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

export const OnboardingPaymentEmail = () => (
  <Html>
    <Head />
    <Preview>Welcome to Compozerr - Add your payment method</Preview>
    <Body style={main}>
      <Container style={container}>
        {/* Simple header */}
        <Section style={header}>
          <Text style={logo}>Compozerr</Text>
          <Text style={headerSubtitle}>Welcome to Compozerr</Text>
        </Section>

        <Section style={content}>
          <Text style={greeting}>Hi % CustomerName %,</Text>

          <Text style={paragraph}>
            Welcome to Compozerr! To get started, please add your payment card within the next % DaysUntilTermination % days. If you do not add a payment method by then, your project will be terminated.
          </Text>

          {/* Simple CTA */}
          <Section style={ctaSection}>
            <Button style={payButton} href="% FrontendUrl %/settings?addPaymentMethod=true">
              Add Payment Method
            </Button>
          </Section>

          <Text style={paragraph}>
            If you have any questions, feel free to contact our support team. We're here to help!
          </Text>

          {/* Simple links */}
          <Section style={linksSection}>
            <Link style={link} href="% FrontendUrl %/dashboard">Dashboard</Link>
            <Text style={linkSeparator}>•</Text>
            <Link style={link} href="% FrontendUrl %/support">Contact Support</Link>
          </Section>
        </Section>

        {/* Minimal footer */}
        <Section style={footer}>
          <Text style={footerText}>
            Compozerr Inc., 1234 Cloud Lane, Tech City, TC 56789
          </Text>
        </Section>
      </Container>
    </Body>
  </Html>
);

export default OnboardingPaymentEmail;

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

const ctaSection = {
  textAlign: 'center' as const,
  margin: '32px 0',
};

const payButton = {
  backgroundColor: '#ffffff',
  color: '#000000',
  fontSize: '15px',
  fontWeight: '600',
  textDecoration: 'none',
  borderRadius: '6px',
  padding: '12px 24px',
  display: 'inline-block',
  border: 'none',
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

const footerLinks = {
  margin: '0',
};

const footerLink = {
  color: '#666666',
  fontSize: '12px',
  textDecoration: 'underline',
};