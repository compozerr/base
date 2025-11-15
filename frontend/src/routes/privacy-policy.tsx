import { createFileRoute } from '@tanstack/react-router'
import Navbar from '@/components/navbar'
import Footer from '@/components/footer'

export const Route = createFileRoute('/privacy-policy')({
  component: PrivacyPolicy,
})

function PrivacyPolicy() {
  return (
    <div className="flex min-h-screen flex-col">
      <Navbar />
      <main className="flex-1">
        <div className="container max-w-4xl py-12 md:py-16">
          <h1 className="text-4xl font-bold mb-4">Privacy Policy</h1>
          <p className="text-sm text-muted-foreground mb-8">
            Effective Date: {new Date().toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' })}
          </p>

          <div className="prose prose-neutral dark:prose-invert max-w-none">
            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">1. Introduction</h2>
              <p className="text-muted-foreground mb-4">
                compozerr ("we," "us," or "our") is committed to protecting your privacy. This Privacy Policy explains
                how we collect, use, disclose, and safeguard your information when you use our services.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">2. Information We Collect</h2>
              <p className="text-muted-foreground mb-4">
                We collect information that you provide directly to us, including:
              </p>
              <ul className="list-disc list-inside text-muted-foreground space-y-2 ml-4">
                <li><strong>Account Information:</strong> Name, email address, and password when you create an account</li>
                <li><strong>Payment Information:</strong> Payment card details processed securely through Stripe (we do not store card numbers)</li>
                <li><strong>Profile Information:</strong> Any additional information you choose to provide</li>
                <li><strong>Usage Data:</strong> Information about how you interact with our services</li>
                <li><strong>Technical Data:</strong> IP address, browser type, device information, and operating system</li>
                <li><strong>Project Data:</strong> Configuration, deployments, and content you create within our services</li>
              </ul>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">3. How We Use Your Information</h2>
              <p className="text-muted-foreground mb-4">
                We use the information we collect to:
              </p>
              <ul className="list-disc list-inside text-muted-foreground space-y-2 ml-4">
                <li>Provide, maintain, and improve our services</li>
                <li>Process your transactions and manage your subscriptions</li>
                <li>Send you technical notices, updates, and support messages</li>
                <li>Respond to your comments, questions, and customer service requests</li>
                <li>Monitor and analyze trends, usage, and activities</li>
                <li>Detect, prevent, and address technical issues and security threats</li>
                <li>Comply with legal obligations</li>
              </ul>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">4. Data Storage and Security</h2>
              <p className="text-muted-foreground mb-4">
                We implement appropriate technical and organizational measures to protect your personal data:
              </p>
              <ul className="list-disc list-inside text-muted-foreground space-y-2 ml-4">
                <li>Your data is stored on secure servers provided by OVHcloud</li>
                <li>We use encryption for data transmission (HTTPS/TLS)</li>
                <li>Access to personal data is restricted to authorized personnel only</li>
                <li>We regularly review and update our security practices</li>
              </ul>
              <p className="text-muted-foreground mt-4">
                However, no method of transmission over the Internet is 100% secure. While we strive to protect your
                personal data, we cannot guarantee absolute security.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">5. Third-Party Services</h2>
              <p className="text-muted-foreground mb-4">
                We use the following third-party services that may collect information about you:
              </p>
              <ul className="list-disc list-inside text-muted-foreground space-y-2 ml-4">
                <li><strong>Stripe:</strong> Payment processing (subject to Stripe's privacy policy)</li>
                <li><strong>OVHcloud:</strong> Infrastructure and hosting services</li>
                <li><strong>Authentication Providers:</strong> For secure login (e.g., GitHub OAuth)</li>
              </ul>
              <p className="text-muted-foreground mt-4">
                These third parties have their own privacy policies governing their use of your information.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">6. Cookies and Tracking</h2>
              <p className="text-muted-foreground mb-4">
                We use cookies and similar tracking technologies to:
              </p>
              <ul className="list-disc list-inside text-muted-foreground space-y-2 ml-4">
                <li>Maintain your session and keep you logged in</li>
                <li>Remember your preferences</li>
                <li>Understand how you use our services</li>
              </ul>
              <p className="text-muted-foreground mt-4">
                You can control cookies through your browser settings, but this may affect your ability to use some features of our services.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">7. Data Retention</h2>
              <p className="text-muted-foreground mb-4">
                We retain your personal data for as long as necessary to provide our services and fulfill the purposes
                outlined in this policy. When you delete your account, we will delete or anonymize your personal data,
                unless we are required to retain it for legal or regulatory purposes.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">8. Your Rights (GDPR)</h2>
              <p className="text-muted-foreground mb-4">
                If you are located in the European Economic Area (EEA), you have the following rights:
              </p>
              <ul className="list-disc list-inside text-muted-foreground space-y-2 ml-4">
                <li><strong>Right to Access:</strong> Request a copy of your personal data</li>
                <li><strong>Right to Rectification:</strong> Request correction of inaccurate data</li>
                <li><strong>Right to Erasure:</strong> Request deletion of your personal data</li>
                <li><strong>Right to Restriction:</strong> Request limitation of processing</li>
                <li><strong>Right to Data Portability:</strong> Receive your data in a structured format</li>
                <li><strong>Right to Object:</strong> Object to processing of your data</li>
                <li><strong>Right to Withdraw Consent:</strong> Withdraw consent at any time</li>
              </ul>
              <p className="text-muted-foreground mt-4">
                To exercise these rights, please contact us through our <a href="/contact" className="text-primary hover:underline">contact page</a>.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">9. Children's Privacy</h2>
              <p className="text-muted-foreground mb-4">
                Our services are not intended for individuals under the age of 18. We do not knowingly collect personal
                data from children. If you believe we have collected data from a child, please contact us immediately.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">10. International Data Transfers</h2>
              <p className="text-muted-foreground mb-4">
                Your data may be transferred to and processed in countries other than your own. We ensure appropriate
                safeguards are in place to protect your data in accordance with this Privacy Policy.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">11. Changes to This Policy</h2>
              <p className="text-muted-foreground mb-4">
                We may update this Privacy Policy from time to time. We will notify you of material changes by email
                or through a prominent notice on our website. Your continued use of our services after such modifications
                constitutes acceptance of the updated Privacy Policy.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">12. Contact Us</h2>
              <p className="text-muted-foreground mb-4">
                If you have questions or concerns about this Privacy Policy or our data practices, please contact us
                through our <a href="/contact" className="text-primary hover:underline">contact page</a>.
              </p>
            </section>
          </div>
        </div>
      </main>
      <Footer />
    </div>
  )
}
