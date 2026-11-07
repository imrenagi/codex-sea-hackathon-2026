#!/bin/sh
set -eu

: "${UNITY_CLI:?Set UNITY_CLI}"
: "${UNITY_PROJECT_DIR:?Set UNITY_PROJECT_DIR}"
: "${WEB_BUILD_DIR:?Set WEB_BUILD_DIR}"

if ! command -v "$UNITY_CLI" >/dev/null 2>&1; then
    echo "Install the Unity CLI (https://unity.com/products/unity-cli) before building WebGL." >&2
    exit 1
fi

project_dir=$(CDPATH= cd -- "$UNITY_PROJECT_DIR" && pwd)
source_commit=$(git -C "$project_dir" rev-parse HEAD)
case "$WEB_BUILD_DIR" in
    /*) output_dir=$WEB_BUILD_DIR ;;
    *) output_dir="$PWD/$WEB_BUILD_DIR" ;;
esac

echo "Building WebGL from $project_dir into $output_dir"
# The Editor version comes from ProjectVersion.txt. Build the checked-out source,
# including local edits, and fail before deployment if compilation/build fails.
"$UNITY_CLI" build "$project_dir" --target WebGL \
    --execute-method JalanKita.EditorTools.ProjectSetup.BuildWeb \
    --output-path "$output_dir" --allow-dirty-build --timeout 1800 \
    --no-tail --format json

printf '{"commit":"%s"}\n' "$source_commit" > "$output_dir/build-info.json"
