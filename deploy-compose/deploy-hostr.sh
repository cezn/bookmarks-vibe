#! /bin/bash
set -euo pipefail

# Resolve this script's directory reliably even when invoked via symlink or from PATH
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" >/dev/null 2>&1 && pwd)"

if [[ ! -f "$script_dir/deploy.sh" ]]; then
  echo "Error: $script_dir/deploy.sh not found" >&2
  exit 1
fi

remote_script=""
cleanup_remote_script() {
  if [[ -n "$remote_script" ]]; then
    ssh hostr "rm -f -- '$remote_script'" >/dev/null 2>&1 || true
  fi
}
trap cleanup_remote_script EXIT

remote_script="$(ssh hostr 'mktemp /tmp/deploy-XXXXXXXX.sh')"
scp "$script_dir/deploy.sh" "hostr:$remote_script"
ssh hostr "bash -- '$remote_script'"
