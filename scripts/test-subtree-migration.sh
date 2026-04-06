#!/usr/bin/env bash
set -euo pipefail

# End-to-end test for submodule → subtree migration.
# Creates local repos in /tmp, sets up submodules, runs the ACTUAL migration
# script (patched for local paths), and verifies results.
# No network access needed.

TEST_DIR="/tmp/subtree-migration-test-$$"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
MIGRATION_SCRIPT="$SCRIPT_DIR/migrate-submodules-to-subtrees.sh"

GREEN='\033[0;32m'
RED='\033[0;31m'
BOLD='\033[1m'
RESET='\033[0m'

pass() { echo -e "  ${GREEN}PASS${RESET} $1"; }
fail() { echo -e "  ${RED}FAIL${RESET} $1"; FAILURES=$((FAILURES + 1)); }
FAILURES=0

cleanup() {
  git config --global --unset protocol.file.allow 2>/dev/null || true
  echo ""
  if [ "$FAILURES" -eq 0 ]; then
    echo -e "${GREEN}${BOLD}All tests passed.${RESET} Cleaning up $TEST_DIR"
    rm -rf "$TEST_DIR"
  else
    echo -e "${RED}${BOLD}$FAILURES test(s) failed.${RESET} Test dir preserved at: $TEST_DIR"
  fi
}
trap cleanup EXIT

echo "Setting up test environment in $TEST_DIR"
mkdir -p "$TEST_DIR/remotes"

# Allow local file:// transport (needed for submodule add with local paths)
git config --global protocol.file.allow always

# ─── Helper: create a bare "source" repo with some content ─────────────

create_source_repo() {
  local name="$1"
  local bare="$TEST_DIR/remotes/$name.git"

  git init --bare -q "$bare"

  local work="$TEST_DIR/.work-$name"
  git clone -q "$bare" "$work"
  cd "$work"
  git config user.email "test@test.com"
  git config user.name "Test"

  mkdir -p src
  echo "module: $name" > src/main.txt
  echo "version: 1.0" > src/version.txt
  echo "# $name" > README.md
  git add -A
  git commit -q -m "Initial commit for $name"
  git branch -M main
  git push -q -u origin main

  rm -rf "$work"
  echo "$bare"
}

# ─── Create 4 source repos ─────────────────────────────────────────────

echo "Creating source repos..."
TEMPLATE_REPO=$(create_source_repo "template")
CONFIG_REPO=$(create_source_repo "config")
MAIL_REPO=$(create_source_repo "mail")
MAIL_RESEND_REPO=$(create_source_repo "mail-resend")

# ─── Create project repo with submodules ────────────────────────────────

echo "Creating project repo with submodules..."
PROJECT="$TEST_DIR/project"
git init -q "$PROJECT"
cd "$PROJECT"
git config user.email "test@test.com"
git config user.name "Test"

# Non-module content (should survive migration untouched)
echo '{"name": "test-project"}' > compozerr.json
mkdir -p modules/auth/src
echo "inline module" > modules/auth/src/main.txt
mkdir -p scripts
git add -A
git commit -q -m "Initial project"

# Add the 4 submodules
git submodule add -q "$TEMPLATE_REPO" modules/template
git submodule add -q "$CONFIG_REPO" modules/config
git submodule add -q "$MAIL_REPO" modules/mail
git submodule add -q "$MAIL_RESEND_REPO" modules/mail-resend
git commit -q -m "Add submodules"

echo "Setup complete."
echo ""

# ─── Record pre-migration state ────────────────────────────────────────

echo "Pre-migration state:"
echo "  Submodules: $(git submodule status | wc -l | xargs)"
echo "  .gitmodules: $([ -f .gitmodules ] && echo exists || echo missing)"
echo ""

# ─── Patch the real migration script for local paths ────────────────────

# Put patched script inside project/scripts/ (same layout as real repo)
mkdir -p "$PROJECT/scripts"
PATCHED="$PROJECT/scripts/migrate.sh"
sed \
  -e "s|https://github.com/compozerr/template|$TEMPLATE_REPO|g" \
  -e "s|https://github.com/compozerr-lib/config|$CONFIG_REPO|g" \
  -e "s|https://github.com/compozerr-lib/mail.git|$MAIL_REPO|g" \
  -e "s|https://github.com/compozerr-lib/mail-resend.git|$MAIL_RESEND_REPO|g" \
  "$MIGRATION_SCRIPT" > "$PATCHED"
chmod +x "$PATCHED"

# Commit the script so it doesn't show as untracked
cd "$PROJECT"
git add scripts/migrate.sh
git commit -q -m "Add migration script"

# ─── Run the actual migration script ───────────────────────────────────

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Running migration script..."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

cd "$PROJECT"
"$PATCHED"

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Running verification tests..."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

cd "$PROJECT"

# 1. No submodules remain
REMAINING=$(git submodule status 2>/dev/null | wc -l | xargs)
[ "$REMAINING" -eq 0 ] && pass "No submodules remain" || fail "Still $REMAINING submodules"

# 2. .gitmodules is gone
[ ! -f .gitmodules ] && pass ".gitmodules removed" || fail ".gitmodules still exists"

# 3. Module directories exist with content
for mod in template config mail mail-resend; do
  if [ -d "modules/$mod" ] && [ -f "modules/$mod/src/main.txt" ]; then
    pass "modules/$mod/ has content"
  else
    fail "modules/$mod/ missing or empty"
  fi
done

# 4. File content is preserved
for mod in template config mail mail-resend; do
  CONTENT=$(cat "modules/$mod/src/main.txt")
  [ "$CONTENT" = "module: $mod" ] && pass "modules/$mod content correct" || fail "modules/$mod content: '$CONTENT'"
done

# 5. Inline module untouched
if [ -f "modules/auth/src/main.txt" ]; then
  [ "$(cat modules/auth/src/main.txt)" = "inline module" ] \
    && pass "Inline module (auth) untouched" \
    || fail "Inline module content changed"
else
  fail "Inline module (auth) deleted"
fi

# 6. Root file untouched
[ "$(cat compozerr.json)" = '{"name": "test-project"}' ] \
  && pass "compozerr.json untouched" \
  || fail "compozerr.json modified"

# 7. Files are regular tracked blobs (not 160000 submodule refs)
MODE=$(git ls-files --stage modules/template/README.md | awk '{print $1}')
[ "$MODE" = "100644" ] && pass "Files are regular blobs (mode $MODE)" || fail "Unexpected mode: $MODE"

# 8. Clean working tree
if git diff --quiet HEAD && [ -z "$(git status --porcelain)" ]; then
  pass "Working tree clean"
else
  fail "Working tree dirty"
fi

# 9. Can commit to former-submodule files (the whole point!)
echo "modified" >> modules/template/src/main.txt
git add modules/template/src/main.txt
git commit -q -m "Normal commit to former submodule"
pass "Can commit changes to ex-submodule files"

# 10. Idempotent re-run (should skip all)
RERUN_OUTPUT=$("$PATCHED" 2>&1)
if echo "$RERUN_OUTPUT" | grep -q "Already converted"; then
  pass "Re-run skips already-converted modules"
else
  fail "Re-run didn't detect already-converted modules"
fi

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
