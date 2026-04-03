# Stripe Native Invoices Refactor

## Goal

Replace the custom invoice generation system (monthly grouping, QuestPDF PDF generation, custom download endpoint) with Stripe's native invoice API. Show a simple invoice table in settings that links out to Stripe's hosted invoice pages and PDFs.

## Backend Changes

### Delete

- `modules/stripe/backend/Stripe/Services/MonthlyInvoicePdfService.cs`
- `modules/stripe/backend/Stripe/Services/IMonthlyInvoicePdfService.cs`
- `modules/stripe/backend/Stripe/Services/MonthlyInvoiceGroup.cs`
- `modules/stripe/backend/Stripe/Services/InvoiceLineDto.cs`
- `modules/stripe/backend/Stripe/Endpoints/Invoices/DownloadMonthlyInvoice/` (entire directory)
- QuestPDF NuGet package from `Stripe.csproj`

### Simplify

**`GetInvoicesCommandHandler`**: Remove monthly grouping logic. Return flat list of `InvoiceDto` from Stripe API.

**`GetInvoicesResponse`**: Change from `List<MonthlyInvoiceGroup>` to `List<InvoiceDto>`.

**`InvoicesGroup.cs`**: Remove `DownloadMonthlyInvoiceRoute` registration.

**`InvoiceDto`**: Keep `Id`, `Total`, `HostedInvoiceUrl`, `InvoicePdf`, `PeriodStart`, `PeriodEnd`, `Created`. Add `Status` field (paid/open/draft/void). Remove `Lines`, `StartingBalance`, `EndingBalance`.

**`InvoicesService`**: Increase limit from 20 to 100. Add status filter for non-draft invoices.

### Keep Untouched

- All webhook handlers (invoice.payment_succeeded, invoice.payment_failed)
- All event handlers (email sending, payment failure saga)
- Payment failure saga system
- Email templates referencing invoice links

## Frontend Changes

### Replace `invoices-list.tsx`

Simple table with columns: Date | Description | Amount | Status | Actions.

- Date: formatted from `created` timestamp
- Description: period range (e.g., "Apr 1 - Apr 30, 2026")
- Amount: formatted currency
- Status: Badge showing Paid (green), Open (amber), Void (gray)
- Actions: "View" link opens `hostedInvoiceUrl` in new tab, "PDF" link opens `invoicePdf` in new tab

### Subscription list — per-project subtotals

Group subscription rows by project. Show a subtotal row per project before the grand total.

## What This Removes

- ~313 lines of QuestPDF PDF generation code
- ~72 lines of download endpoint handler
- ~54 lines of monthly grouping logic
- ~53 lines of InvoiceLineDto
- Custom monthly invoice card UI with collapsible sections
- The entire concept of "monthly grouping" — Stripe handles billing periods

## What Users Get Instead

- Direct links to Stripe's hosted invoice page (view, pay, dispute)
- Direct links to Stripe-generated PDFs (professional, legally compliant)
- Simpler, faster settings page
- Per-project cost subtotals in subscription table
