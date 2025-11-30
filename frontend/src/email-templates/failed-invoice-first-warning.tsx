import {
  Body,
  Button,
  Container,
  Head,
  Html,
  Link,
  Preview,
  Section,
  Text,
  Row,
  Column,
} from '@react-email/components';

export const FailedInvoiceFirstWarning = () => (
  <Html>
    <Head />
    <Preview>Payment reminder - % DaysUntilTermination % days until project termination</Preview>
    <Body style={main}>
      <Container style={container}>
        <Section style={header}>
          <Text style={logo}>% CompanyName %</Text>
          <Text style={headerSubtitle}>Payment Reminder</Text>
        </Section>

        <Section style={content}>
          <Text style={greeting}>Hi % CustomerName %,</Text>

          <Text style={paragraph}>
            This is a reminder that your invoice remains unpaid. Your project will be terminated in <strong style={emphasis}>% DaysUntilTermination % days</strong> if payment is not received.
          </Text>

          <Section style={invoiceCard}>
            <Text style={cardTitle}>Invoice Details</Text>

            <Row style={invoiceRow}>
              <Column style={labelColumn}>
                <Text style={label}>Amount Due</Text>
              </Column>
              <Column style={valueColumn}>
                <Text style={amount}>% Currency %% AmountDue %</Text>
              </Column>
            </Row>

            <Row style={invoiceRow}>
              <Column style={labelColumn}>
                <Text style={label}>Due Date</Text>
              </Column>
              <Column style={valueColumn}>
                <Text style={value}>% DueDate %</Text>
              </Column>
            </Row>

            <Row style={invoiceRowLast}>
              <Column style={labelColumn}>
                <Text style={label}>Days Overdue</Text>
              </Column>
              <Column style={valueColumn}>
                <Text style={overdue}>% DaysOverdue % days</Text>
              </Column>
            </Row>
          </Section>

          <Section style={ctaSection}>
            <Button style={payButton} href="% PaymentLink %">
              Pay Invoice Now
            </Button>
          </Section>

          <Text style={warningParagraph}>
            ⚠️ Action required: Please complete payment to avoid service interruption.
          </Text>

          <Section style={linksSection}>
            <Link style={link} href="% DashboardLink %">Dashboard</Link>
            <Text style={linkSeparator}>•</Text>
            <Link style={link} href="% ContactLink %">Contact Support</Link>
          </Section>
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

export default FailedInvoiceFirstWarning;

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

const emphasis = {
  color: '#ff9966',
  fontWeight: '600',
};

const warningParagraph = {
  color: '#ff9966',
  fontSize: '15px',
  lineHeight: '22px',
  margin: '0 0 24px 0',
  fontWeight: '500',
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
  width: '40%',
  verticalAlign: 'top',
};

const valueColumn = {
  width: '60%',
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

const overdue = {
  color: '#ff6b6b',
  fontSize: '14px',
  margin: '0',
  fontWeight: '500',
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
