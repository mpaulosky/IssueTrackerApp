#!/usr/bin/env bash
# install-hooks.sh — copies committed hook templates to .git/hooks and makes them executable.
# Idempotent: safe to run multiple times.
# Run from repo root or any subdirectory.
set -euo pipefail

ROOT="$(git rev-parse --show-toplevel)"
HOOKS_SRC="$ROOT/.github/hooks"
HOOKS_DST="$ROOT/.git/hooks"

install_hook() {
  local name="$1"
  local src="$HOOKS_SRC/$name"
  local dst="$HOOKS_DST/$name"

  if [[ ! -f "$src" ]]; then
    echo "⚠️  Source hook not found: $src — skipping."
    return
  fi

  if [[ -f "$dst" ]]; then
    if cmp -s "$src" "$dst"; then
      echo "✅ $name is already up to date — nothing to do."
      return
    else
      echo "🔄 Updating $name (existing hook differs from template)..."
    fi
  else
    echo "📋 Installing $name..."
  fi

  cp "$src" "$dst"
  chmod +x "$dst"
  echo "✅ $name installed at $dst"
}

install_hook "pre-push"

echo ""
echo "Done. Git hooks are active for this clone."
