# Module Overview

This module is responsible for handling all email-related functionality, including sending emails, managing templates, and processing incoming email requests.

# Usage

To use this module create a new template tsx file in the `frontend/email-templates` directory. You can use the existing templates as a reference for your new template.
Then it will automatically generate the corresponding HTML file in the `backend/Api/Emails` directory. And a Dto class that corresponds to the email template.

To use:
1. Dependency Inject IMailService
2. Create a new mail by doing `var email = await ReactEmail.CreateAsync(<from email>, <to emails>, <subject>, new <YourEmailTemplateDto>)`
3. Send the email using `await mailService.SendAsync(email)`

# Module Structure