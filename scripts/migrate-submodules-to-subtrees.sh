#!/usr/bin/env bash
set -euo pipefail

# One-time migration script: convert 4 git submodules to git subtrees.
# Run from the repo root: ./scripts/migrate-submodules-to-subtrees.sh
#
# What this does:
#   For each submodule (template, config, mail, mail-resend):
#     1. Record its current commit hash
#     2. Deinit + git rm the submodule
#     3. Clean up .git/modules cache
#     4. Commit the removal
#     5. git subtree add with --squash at the recorded hash
#     6. Verify files exist
#
# After all 4 are done, .gitmodules is removed and all modules are regular directories.
#
# Safe to ctrl-c: each submodule is atomic. If it fails mid-way, the last
# successful submodule is committed. You can fix the issue and re-run.

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO_ROOT"

# Bail if working tree is dirty (ignoring untracked files)
if ! git diff --quiet HEAD; then
  echo "ERROR: Working tree has uncommitted changes. Commit or stash first."
  exit 1
fi

# Define submodules: name|path|remote
SUBMODULES=(
  "template|modules/template|https://github.com/compozerr/template"
  "config|modules/config|https://github.com/compozerr-lib/config"
  "mail|modules/mail|https://github.com/compozerr-lib/mail.git"
  "mail-resend|modules/mail-resend|https://github.com/compozerr-lib/mail-resend.git"
)

TOTAL=${#SUBMODULES[@]}
CURRENT=0

for entry in "${SUBMODULES[@]}"; do
  IFS='|' read -r NAME MOD_PATH REMOTE <<< "$entry"
  CURRENT=$((CURRENT + 1))

  echo ""
  echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
  echo "[$CURRENT/$TOTAL] Converting: $NAME"
  echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

  # Check if this submodule still exists (idempotent re-runs)
  if ! git submodule status "$MOD_PATH" &>/dev/null; then
    echo "  Already converted (not a submodule). Skipping."
    continue
  fi

  # 1. Record current commit hash
  HASH=$(git submodule status "$MOD_PATH" | awk '{print $1}' | tr -d '+')
  echo "  Commit hash: $HASH"

  # 2. Deinit the submodule
  echo "  Deinitializing submodule..."
  git submodule deinit -f "$MOD_PATH"

  # 3. Remove from index and .gitmodules
  echo "  Removing from git..."
  git rm -f "$MOD_PATH"

  # 4. Clean up .git/modules cache
  if [ -d ".git/modules/$MOD_PATH" ]; then
    echo "  Cleaning .git/modules cache..."
    rm -rf ".git/modules/$MOD_PATH"
  fi

  # 5. Commit the removal
  echo "  Committing removal..."
  git commit -m "$(cat <<EOF
refactor: remove $NAME submodule (preparing for subtree)

Converting $NAME from git submodule to git subtree.
Previous commit: $HASH

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>
EOF
)"

  # 6. Add back as subtree
  echo "  Adding as subtree (this fetches from remote)..."
  git subtree add --prefix="$MOD_PATH" "$REMOTE" "$HASH" --squash

  # 7. Verify
  if [ -d "$MOD_PATH" ]; then
    FILE_COUNT=$(find "$MOD_PATH" -type f | wc -l | tr -d ' ')
    echo "  Verified: $MOD_PATH exists ($FILE_COUNT files)"
  else
    echo "  ERROR: $MOD_PATH directory not found after subtree add!"
    exit 1
  fi

  echo "  Done."
done

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Migration complete!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Clean up .gitmodules if it's empty or only has whitespace
if [ -f ".gitmodules" ]; then
  REMAINING=$(git config --file .gitmodules --get-regexp path 2>/dev/null || true)
  if [ -z "$REMAINING" ]; then
    echo "Removing empty .gitmodules..."
    git rm -f .gitmodules
    git commit -m "chore: remove empty .gitmodules after subtree migration

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
  else
    echo "WARNING: .gitmodules still has $REMAINING entries."
  fi
else
  echo ".gitmodules already removed."
fi

echo ""
echo "Submodule status (should be empty):"
git submodule status 2>/dev/null || echo "  (none)"

echo ""
echo "Recent commits:"
git log --oneline -$((TOTAL * 2 + 1))
