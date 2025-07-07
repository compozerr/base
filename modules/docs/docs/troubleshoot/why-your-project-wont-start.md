---
sidebar_position: 50
---

# Why your project won't start

Find out why your project is not starting and how to fix it.

## How to

1. Make sure docker is running. If you are using Docker Desktop, ensure it is started and running properly.
2. Check if the `compozerr` CLI is installed and up to date. You can update it with:
   ```bash
   compozerr update
   ```

3. Ensure you are in the correct directory of your project. The command should be run from the root of your project directory.

4. Setup verbose logging to see the error messages:

    ```bash
    npm run dev -- --verbose
    ```

This will provide detailed logs that can help you identify the issue.