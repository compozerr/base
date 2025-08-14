# Stripe Invoice Payment Failure Handling

This implementation provides comprehensive handling for Stripe invoice payment failures through webhooks.

## Overview

When an invoice payment fails in Stripe (due to insufficient funds, removed payment methods, or other payment issues), the system will:

1. Receive a webhook event from Stripe
2. Process the failure and publish an internal event
3. Handle the failure based on the number of attempts
4. Log appropriate information and take necessary actions

## Setup

### 1. Configure Stripe Webhook

In your Stripe Dashboard:
1. Go to Webhooks section
2. Create a new webhook endpoint: `https://yourdomain.com/stripe/webhooks`
3. Select the event: `invoice.payment_failed`
4. Copy the webhook signing secret

### 2. Configuration

Add to your configuration (.env):

``` dotenv
STRIPE__WEBHOOKENDPOINTSECRET=whsec_xxxx
```

### 3. Endpoint

The webhook endpoint is automatically registered at: `POST /v1/stripe/webhooks`

## Components

### Events

**StripeInvoicePaymentFailedEvent**
- `InvoiceId`: The failed invoice ID
- `CustomerId`: Stripe customer ID
- `SubscriptionId`: Associated subscription ID
- `AmountDue`: Amount that failed to be charged
- `AttemptCount`: Number of payment attempts
- `FailureReason`: Reason for failure (if available)
- `NextPaymentAttempt`: When Stripe will retry (if available)

