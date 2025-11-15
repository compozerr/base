import { createFileRoute } from '@tanstack/react-router'
import Navbar from '@/components/navbar'
import Footer from '@/components/footer'

export const Route = createFileRoute('/terms-of-service')({
  component: TermsOfService,
})

function TermsOfService() {
  return (
    <div className="flex min-h-screen flex-col">
      <Navbar />
      <main className="flex-1">
        <div className="container max-w-4xl py-12 md:py-16">
          <h1 className="text-4xl font-bold mb-4">Terms of Service</h1>
          <p className="text-sm text-muted-foreground mb-8">
            Effective Date: {new Date().toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' })}
          </p>

          <div className="prose prose-neutral dark:prose-invert max-w-none">
            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">1. Agreement to Terms</h2>
              <p className="text-muted-foreground mb-4">
                By accessing and using compozerr's services, you accept and agree to be bound by the terms and provisions
                of this agreement. If you do not agree to these Terms of Service, you may not access or use our services.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">2. Service Description</h2>
              <p className="text-muted-foreground mb-4">
                compozerr provides independent hosting services for automation workflows, including but not limited to:
              </p>
              <ul className="list-disc list-inside text-muted-foreground space-y-2 ml-4">
                <li>n8n workflow automation hosting</li>
                <li>Custom domain support</li>
                <li>Deployment and infrastructure management</li>
                <li>Server tier options with varying specifications</li>
              </ul>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">3. User Obligations</h2>
              <p className="text-muted-foreground mb-4">
                You agree to:
              </p>
              <ul className="list-disc list-inside text-muted-foreground space-y-2 ml-4">
                <li>Provide accurate and complete registration information</li>
                <li>Maintain the security of your account credentials</li>
                <li>Use the service in compliance with all applicable laws and regulations</li>
                <li>Not use the service for any illegal or unauthorized purpose</li>
                <li>Not abuse, harass, or harm other users or the service infrastructure</li>
                <li>Not attempt to gain unauthorized access to other accounts or systems</li>
              </ul>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">4. Payment and Billing</h2>
              <p className="text-muted-foreground mb-4">
                Payment terms:
              </p>
              <ul className="list-disc list-inside text-muted-foreground space-y-2 ml-4">
                <li>Services are billed on a monthly subscription basis</li>
                <li>Payments are processed through Stripe, our third-party payment processor</li>
                <li>You authorize us to charge your payment method for all applicable fees</li>
                <li>Failure to pay may result in service suspension or termination</li>
                <li>Refunds are handled on a case-by-case basis at our discretion</li>
                <li>Promotional pricing (such as Black Friday specials) may have specific terms</li>
              </ul>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">5. Service Availability</h2>
              <p className="text-muted-foreground mb-4">
                While we strive to provide reliable service, we do not guarantee uninterrupted access. Service may be temporarily
                unavailable due to maintenance, updates, or circumstances beyond our control. We are not liable for any
                losses resulting from service interruptions.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">6. Data and Content</h2>
              <p className="text-muted-foreground mb-4">
                You retain ownership of all data and content you upload to our services. You grant us a license to store,
                process, and transmit your data as necessary to provide the service. You are responsible for maintaining
                backups of your data. We are not responsible for any data loss.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">7. Termination</h2>
              <p className="text-muted-foreground mb-4">
                Either party may terminate this agreement at any time:
              </p>
              <ul className="list-disc list-inside text-muted-foreground space-y-2 ml-4">
                <li>You may cancel your subscription at any time through your account settings</li>
                <li>We may suspend or terminate your account for violations of these terms</li>
                <li>Upon termination, you will lose access to the service and your data</li>
                <li>We recommend exporting your data before cancellation</li>
              </ul>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">8. Limitation of Liability</h2>
              <p className="text-muted-foreground mb-4">
                To the maximum extent permitted by law, compozerr shall not be liable for any indirect, incidental,
                special, consequential, or punitive damages, including loss of profits, data, or other intangible losses,
                resulting from your use of or inability to use the service.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">9. Indemnification</h2>
              <p className="text-muted-foreground mb-4">
                You agree to indemnify and hold harmless compozerr and its affiliates from any claims, damages, losses,
                liabilities, and expenses arising from your use of the service or violation of these terms.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">10. Changes to Terms</h2>
              <p className="text-muted-foreground mb-4">
                We reserve the right to modify these terms at any time. We will notify users of material changes via
                email or through the service. Continued use of the service after changes constitutes acceptance of the
                modified terms.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">11. Governing Law</h2>
              <p className="text-muted-foreground mb-4">
                These Terms shall be governed by and construed in accordance with applicable laws, without regard to
                conflict of law provisions.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-2xl font-semibold mb-4">12. Contact Information</h2>
              <p className="text-muted-foreground mb-4">
                If you have any questions about these Terms, please contact us through our{' '}
                <a href="/contact" className="text-primary hover:underline">contact page</a>.
              </p>
            </section>
          </div>
        </div>
      </main>
      <Footer />
    </div>
  )
}
