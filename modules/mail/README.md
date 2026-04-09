# Mail Module

Handles all email functionality: templates, sending, and delivery.

## How Email Templates Work

Email templates use [React Email](https://react.email) components. You write a `.tsx` file, and the tooling generates both the static HTML and the C# backing class automatically.

### File Locations

| What | Where | Edit? |
|------|-------|-------|
| React Email components (source of truth) | `frontend/src/email-templates/*.tsx` | Yes - create/edit these |
| Generated HTML | `backend/Api/Emails/*.html` | No - auto-generated |
| Generated C# template classes | `backend/Api/Emails/*.cs` | No - auto-generated |
| Generate script | `modules/mail/frontend/scripts/generate.ts` | Rarely |

### Creating a New Email Template

1. **Create a React Email component** in `frontend/src/email-templates/`:

   ```tsx
   // frontend/src/email-templates/my-notification.tsx
   import { Body, Container, Head, Html, Preview, Section, Text, Button } from '@react-email/components';

   export const MyNotification = () => (
     <Html>
       <Head />
       <Preview>Preview text shown in inbox</Preview>
       <Body style={main}>
         <Container style={container}>
           <Section style={content}>
             <Text style={greeting}>Hi % RecipientName %,</Text>
             <Text style={paragraph}>Your % ItemName % is ready.</Text>
             <Button style={button} href="% ActionLink %">View Now</Button>
           </Section>
         </Container>
       </Body>
     </Html>
   );

   export default MyNotification;

   const main = { backgroundColor: '#0a0a0a', fontFamily: '-apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,sans-serif' };
   const container = { backgroundColor: '#111111', margin: '0 auto', maxWidth: '560px', borderRadius: '12px', border: '1px solid #222222' };
   const content = { padding: '40px' };
   const greeting = { color: '#ffffff', fontSize: '16px', fontWeight: '500', margin: '0 0 24px 0' };
   const paragraph = { color: '#cccccc', fontSize: '15px', lineHeight: '22px', margin: '0 0 24px 0' };
   const button = { backgroundColor: '#ffffff', color: '#0a0a0a', fontSize: '14px', fontWeight: '600', textDecoration: 'none', padding: '12px 32px', borderRadius: '8px' };
   ```

   Use `% PlaceholderName %` syntax (with spaces) for dynamic values. These become C# properties.

2. **Run the generate command**:

   ```bash
   cd modules/mail/frontend && npm run generate
   ```

   This does two things:
   - Exports the React component to static HTML at `backend/Api/Emails/my-notification.html`
   - Scans the HTML for `% ... %` placeholders and generates `backend/Api/Emails/MyNotification.cs`:

   ```csharp
   // AUTO-GENERATED - do not edit
   public sealed class MyNotificationTemplate : ReactEmailTemplate
   {
       public MyNotificationTemplate() : base("Emails/my-notification.html") { }
       public required string RecipientName { get; init; }
       public required string ItemName { get; init; }
       public required string ActionLink { get; init; }
   }
   ```

3. **Use in backend code**:

   ```csharp
   var mail = await ReactEmail.CreateAsync(
       new EmailAddress("noreply@notifications.compozerr.com", "compozerr"),
       [new EmailAddress(recipientEmail)],
       "Your item is ready",
       new MyNotificationTemplate
       {
           RecipientName = user.Name,
           ItemName = item.Name,
           ActionLink = frontendLocation.GetFromPath("items/" + item.Id).ToString(),
       });

   await mailService.SendEmailAsync(mail);
   ```

### Preview Emails

```bash
cd modules/mail/frontend && npm run dev
# Opens React Email preview at http://localhost:1239
```

### Key Rules

- **Never hand-write** the `.html` or `.cs` files in `backend/Api/Emails/` — always create/edit the `.tsx` source and run `npm run generate`
- **Placeholder format**: Use `% PascalCaseName %` with spaces around the name. The generate script converts these to C# properties.
- **All placeholders must be filled** at runtime. The `ReactEmailTemplate` base class validates that no `% ... %` tokens remain in the final HTML and throws if any are unmatched.
- **Re-run generate** after modifying any `.tsx` template to update the HTML and C# files.

## Sending Emails (Backend)

1. Inject `IMailService` in your handler/service
2. Create the email: `var email = await ReactEmail.CreateAsync(from, to, subject, new YourTemplate { ... })`
3. Send: `await mailService.SendEmailAsync(email)`

The `IMailService` publishes a `SendEmailEvent` via MediatR, keeping email delivery decoupled from business logic.
