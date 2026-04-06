# Submodule → Subtree Migration Guide

**For**: Milo (personal reference)
**Project**: `/Users/milo/milodev/gits/web`
**Date**: 2026-02-20

This only applies to the `web` project — the only project currently using modules.

## Current Submodules

From `.gitmodules`:

| Module | Path | Remote URL |
|--------|------|------------|
| template | `modules/template` | `https://github.com/compozerr/template` |
| config | `modules/config` | `https://github.com/compozerr-lib/config` |
| mail-resend | `modules/mail-resend` | `https://github.com/compozerr-lib/mail-resend.git` |
| mail | `modules/mail` | `https://github.com/compozerr-lib/mail.git` |

The other 10 modules (auth, database, github, stripe, jobs, terminal, storage, openTelemetry, docs, cli) are already inline directories — no changes needed for those.

## Prerequisites

- Clean working tree (`git status` shows nothing)
- All submodules are initialized and up to date
- You're on the `main` branch

## Step-by-Step: Convert Each Submodule

Repeat for each of the 4 submodules. I'll use `template` as the example.

### 1. Record the current commit hash

```bash
cd /Users/milo/milodev/gits/web
git submodule status modules/template
```

Note the commit hash (e.g., `2eb3f6eff562fa94ce815bf817a9ef1821fc2b48`).

### 2. Remove the submodule (keeps files staged for removal)

```bash
# Deinit the submodule
git submodule deinit -f modules/template

# Remove from .gitmodules and staging
git rm -f modules/template

# Clean up .git/modules cache
rm -rf .git/modules/modules/template
```

### 3. Commit the submodule removal

```bash
git commit -m "Remove template submodule (preparing for subtree conversion)"
```

### 4. Add back as a subtree

```bash
git subtree add --prefix=modules/template \
  https://github.com/compozerr/template \
  2eb3f6eff562fa94ce815bf817a9ef1821fc2b48 \
  --squash
```

This creates a squash commit that adds all the template files as regular tracked files.

### 5. Verify

```bash
# Files should be there
ls modules/template/

# No more submodule reference
git submodule status  # template should not appear

# Files are regular tracked files now
git log --oneline -3  # should show the subtree add commit
```

## Full Script (All 4 Submodules)

Run this from the repo root. Do it one at a time so you can verify each step.

```bash
cd /Users/milo/milodev/gits/web

# ── template ──
HASH_TEMPLATE=$(git submodule status modules/template | awk '{print $1}' | tr -d '+')
git submodule deinit -f modules/template
git rm -f modules/template
rm -rf .git/modules/modules/template
git commit -m "Remove template submodule"
git subtree add --prefix=modules/template https://github.com/compozerr/template $HASH_TEMPLATE --squash

# ── config ──
HASH_CONFIG=$(git submodule status modules/config | awk '{print $1}' | tr -d '+')
git submodule deinit -f modules/config
git rm -f modules/config
rm -rf .git/modules/modules/config
git commit -m "Remove config submodule"
git subtree add --prefix=modules/config https://github.com/compozerr-lib/config $HASH_CONFIG --squash

# ── mail ──
HASH_MAIL=$(git submodule status modules/mail | awk '{print $1}' | tr -d '+')
git submodule deinit -f modules/mail
git rm -f modules/mail
rm -rf .git/modules/modules/mail
git commit -m "Remove mail submodule"
git subtree add --prefix=modules/mail https://github.com/compozerr-lib/mail.git $HASH_MAIL --squash

# ── mail-resend ──
HASH_MAILRESEND=$(git submodule status modules/mail-resend | awk '{print $1}' | tr -d '+')
git submodule deinit -f modules/mail-resend
git rm -f modules/mail-resend
rm -rf .git/modules/modules/mail-resend
git commit -m "Remove mail-resend submodule"
git subtree add --prefix=modules/mail-resend https://github.com/compozerr-lib/mail-resend.git $HASH_MAILRESEND --squash
```

**Important**: Run the submodules in this order because you need `git submodule status` to still work for the remaining ones.

## After Migration

1. The `.gitmodules` file should be gone (or empty) after removing all 4 submodules
2. All 14 modules are now regular directories
3. `git submodule status` should return nothing
4. You can commit changes to any module with a normal `git add && git commit`

## Future Subtree Operations (Reference)

Once the CLI is updated, you won't need these. But for reference:

```bash
# Pull upstream changes into a module
git subtree pull --prefix=modules/template \
  https://github.com/compozerr/template main --squash

# Push local changes back to module source repo
git subtree push --prefix=modules/template \
  https://github.com/compozerr/template main
```

## Rollback

If something goes wrong mid-migration, you can always:

```bash
git reset --hard HEAD~1   # undo the last commit
git submodule update --init --recursive  # restore submodules
```

Only do this before you've pushed. If you've already pushed, coordinate accordingly.
