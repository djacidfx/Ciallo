#!/usr/bin/env bash
set -euo pipefail

project_path="${1:-Ciallo}"
project_file="$project_path/project.godot"

version_line="$(grep -E '^config/version=' "$project_file" | head -n 1)"
project_version="${version_line#*=}"
project_version="${project_version%\"}"
project_version="${project_version#\"}"

if ! [[ "$project_version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  echo "project.godot config/version must be plain semver X.Y.Z. Actual: $project_version" >&2
  exit 1
fi

printf '%s\n' "$project_version"
