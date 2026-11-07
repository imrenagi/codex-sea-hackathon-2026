#!/bin/sh
set -eu

: "${GCP_PROJECT:?Set GCP_PROJECT}"
: "${GCP_REGION:?Set GCP_REGION}"
: "${WEB_SERVICE:?Set WEB_SERVICE}"
: "${ARTIFACT_REPOSITORY:?Set ARTIFACT_REPOSITORY}"
: "${WEB_BUILD_DIR:?Set WEB_BUILD_DIR}"

if ! command -v gcloud >/dev/null 2>&1; then
    echo "Install the Google Cloud CLI and run gcloud auth login first." >&2
    exit 1
fi

if [ ! -s "$WEB_BUILD_DIR/index.html" ] || [ ! -d "$WEB_BUILD_DIR/Build" ]; then
    echo "No WebGL export at $WEB_BUILD_DIR. Use RoadSense > Build WebGL in Unity first." >&2
    exit 1
fi
for pattern in '*.loader.js' '*.data*' '*.wasm*' '*.framework.js*'; do
    if [ -z "$(find "$WEB_BUILD_DIR/Build" -type f -name "$pattern" -size +0c -print)" ]; then
        echo "Incomplete WebGL export: missing $pattern in $WEB_BUILD_DIR/Build." >&2
        exit 1
    fi
done

# Stage only the exported game and container configuration. Unity's Library,
# sources, desktop builds, and local credentials never enter the build context.
deploy_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
context=$(mktemp -d "${TMPDIR:-/tmp}/jalan-kita-web.XXXXXXXX")
trap 'rm -rf "$context"' EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
cp "$deploy_dir/Dockerfile" "$deploy_dir/nginx.conf.template" \
    "$deploy_dir/cloudbuild.yaml" "$deploy_dir/.gcloudignore" "$context/"
mkdir "$context/webgl"
cp -R "$WEB_BUILD_DIR/." "$context/webgl/"

gcloud projects describe "$GCP_PROJECT" --quiet --format='value(projectId)' >/dev/null
if [ "${WEB_BOOTSTRAP:-true}" = true ]; then
    gcloud services enable cloudbuild.googleapis.com artifactregistry.googleapis.com \
        run.googleapis.com --project="$GCP_PROJECT" --quiet
fi

if ! gcloud artifacts repositories describe "$ARTIFACT_REPOSITORY" \
    --project="$GCP_PROJECT" --location="$GCP_REGION" --quiet >/dev/null 2>&1; then
    if [ "${WEB_BOOTSTRAP:-true}" != true ]; then
        echo "Artifact Registry repository is unavailable. Run administrator setup first." >&2
        exit 1
    fi
    gcloud artifacts repositories create "$ARTIFACT_REPOSITORY" \
        --project="$GCP_PROJECT" --location="$GCP_REGION" \
        --repository-format=docker --description='Web application images' --quiet
fi

image_base="$GCP_REGION-docker.pkg.dev/$GCP_PROJECT/$ARTIFACT_REPOSITORY/$WEB_SERVICE"
tag="$(date -u +%Y%m%d-%H%M%S)-${context##*.}"
image="$image_base:$tag"

set --
if [ -n "${GCP_BUILD_SERVICE_ACCOUNT:-}" ]; then
    set -- "$@" "--service-account=projects/$GCP_PROJECT/serviceAccounts/$GCP_BUILD_SERVICE_ACCOUNT"
fi
gcloud builds submit "$context" --project="$GCP_PROJECT" --region="$GCP_REGION" \
    --config="$context/cloudbuild.yaml" --substitutions="_IMAGE=$image" --suppress-logs "$@" --quiet

# Deploy the exact image built above, even if a tag is subsequently moved.
digest=$(gcloud artifacts docker images describe "$image" \
    --project="$GCP_PROJECT" --format='value(image_summary.digest)' --quiet)
if [ -z "$digest" ]; then
    echo "Cloud Build completed but no image digest was returned for $image." >&2
    exit 1
fi
set --
if [ "${WEB_PUBLIC:-true}" = true ]; then
    set -- "$@" --allow-unauthenticated
fi
if [ -n "${GCP_RUNTIME_SERVICE_ACCOUNT:-}" ]; then
    set -- "$@" "--service-account=$GCP_RUNTIME_SERVICE_ACCOUNT"
fi
gcloud run deploy "$WEB_SERVICE" --project="$GCP_PROJECT" --region="$GCP_REGION" \
    --image="$image_base@$digest" --port=8080 --use-http2 --ingress=all "$@" \
    --cpu=1 --memory=256Mi --concurrency=80 --min-instances=0 --max-instances=3 \
    --timeout=60 --quiet

gcloud run services describe "$WEB_SERVICE" --project="$GCP_PROJECT" \
    --region="$GCP_REGION" --format='value(status.url)' --quiet
