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

export const InvoiceEmailSucceeded = () => (
  <Html>
    <Head />
    <Preview>Payment Received - Thank you!</Preview>
    <Body style={main}>
      <Container style={container}>
        {/* Simple header */}
        <Section style={header}>
          <Text style={logo}>% CompanyName %</Text>
          <Text style={headerSubtitle}>Payment Confirmation</Text>
        </Section>

        <Section style={content}>
          <Text style={greeting}>Hi % CustomerName %,</Text>

          <Text style={paragraph}>
            Thank you! We've successfully received your payment. Your invoice has been marked as paid.
          </Text>

          {/* Clean invoice card */}
          <Section style={invoiceCard}>
            <Text style={cardTitle}>Payment Details</Text>

            <Row style={invoiceRow}>
              <Column style={labelColumn}>
                <Text style={label}>Amount Paid</Text>
              </Column>
              <Column style={valueColumn}>
                <Text style={amount}>% Currency %% AmountPaid %</Text>
              </Column>
            </Row>

            <Row style={invoiceRow}>
              <Column style={labelColumn}>
                <Text style={label}>Payment Date</Text>
              </Column>
              <Column style={valueColumn}>
                <Text style={successDate}>% PaymentDate %</Text>
              </Column>
            </Row>

            <Row style={invoiceRowLast}>
              <Column style={labelColumn}>
                <Text style={label}>Status</Text>
              </Column>
              <Column style={valueColumn}>
                <Text style={successStatus}>Paid</Text>
              </Column>
            </Row>
          </Section>

          {/* Simple CTA */}
          <Section style={ctaSection}>
            <Button style={viewButton} href="% InvoiceLink %">
              View Invoice
            </Button>
          </Section>

          <Text style={paragraph}>
            A receipt has been sent to your email. If you have any questions, feel free to contact our support team.
          </Text>

          {/* Simple links */}
          <Section style={linksSection}>
            <Link style={link} href="% DashboardLink %">Dashboard</Link>
            <Text style={linkSeparator}>•</Text>
            <Link style={link} href="% InvoiceLink %">View Invoice</Link>
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

export default InvoiceEmailSucceeded;

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

const invoiceCard = {
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
  margin: '0 0 20px 0',
};

const invoiceRow = {
  marginBottom: '16px',
};

const invoiceRowLast = {
  marginBottom: '0',
};

const labelColumn = {
  width: '30%',
  verticalAlign: 'top',
};

const valueColumn = {
  width: '70%',
  verticalAlign: 'top',
};

const label = {
  color: '#888888',
  fontSize: '14px',
  margin: '0',
  fontWeight: '400',
};

const value = {
  color: '#ffffff',
  fontSize: '14px',
  margin: '0',
  fontWeight: '500',
};

const amount = {
  color: '#ffffff',
  fontSize: '16px',
  margin: '0',
  fontWeight: '600',
};

const successDate = {
  color: '#ffffff',
  fontSize: '14px',
  margin: '0',
  fontWeight: '500',
};

const successStatus = {
  color: '#4ade80',
  fontSize: '14px',
  margin: '0',
  fontWeight: '500',
};

const ctaSection = {
  textAlign: 'center' as const,
  margin: '32px 0',
};

const viewButton = {
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