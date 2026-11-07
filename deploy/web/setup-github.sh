#!/bin/sh
# One-time setup, run with a project administrator's gcloud credentials.
set -eu

project=${GCP_PROJECT:-ujian-sim-514002}
region=${GCP_REGION:-asia-southeast2}
repo=${GITHUB_REPOSITORY:-imrenagi/codex-sea-hackathon-2026}
service=${WEB_SERVICE:-jalan-kita-web}
repository=${ARTIFACT_REPOSITORY:-web}
pool=github-actions
provider=github-main
project_number=$(gcloud projects describe "$project" --format='value(projectNumber)')
repo_id=$(gh api "repos/$repo" --jq '.id')
owner_id=$(gh api "repos/$repo" --jq '.owner.id')
deploy_sa="github-web-deployer@$project.iam.gserviceaccount.com"
build_sa="web-cloud-build@$project.iam.gserviceaccount.com"
runtime_sa="jalan-kita-web-runtime@$project.iam.gserviceaccount.com"
source_bucket="gs://${project}_cloudbuild"

gcloud services enable iam.googleapis.com iamcredentials.googleapis.com \
    sts.googleapis.com cloudbuild.googleapis.com artifactregistry.googleapis.com \
    run.googleapis.com --project="$project" --quiet

# Infrastructure is created by the administrator, never by the CI identity.
gcloud artifacts repositories describe "$repository" --project="$project" --location="$region" --quiet >/dev/null
gcloud run services describe "$service" --project="$project" --region="$region" --quiet >/dev/null
gcloud storage buckets describe "$source_bucket" --project="$project" --quiet >/dev/null

for account in github-web-deployer web-cloud-build jalan-kita-web-runtime; do
    if ! gcloud iam service-accounts describe "$account@$project.iam.gserviceaccount.com" \
        --project="$project" --quiet >/dev/null 2>&1; then
        gcloud iam service-accounts create "$account" --project="$project" \
            --display-name="$account" --quiet
    fi
done

if ! gcloud iam workload-identity-pools describe "$pool" --project="$project" \
    --location=global --quiet >/dev/null 2>&1; then
    gcloud iam workload-identity-pools create "$pool" --project="$project" \
        --location=global --display-name='GitHub Actions' --quiet
fi

mapping='google.subject=assertion.sub,attribute.repository_id=assertion.repository_id,attribute.repository_owner_id=assertion.repository_owner_id,attribute.ref=assertion.ref'
condition="assertion.repository_id == '$repo_id' && assertion.repository_owner_id == '$owner_id' && assertion.ref == 'refs/heads/main' && assertion.workflow_ref == '$repo/.github/workflows/deploy-web.yml@refs/heads/main' && (assertion.event_name == 'push' || assertion.event_name == 'workflow_dispatch')"
if gcloud iam workload-identity-pools providers describe "$provider" \
    --workload-identity-pool="$pool" --project="$project" --location=global --quiet >/dev/null 2>&1; then
    gcloud iam workload-identity-pools providers update-oidc "$provider" \
        --workload-identity-pool="$pool" --project="$project" --location=global \
        --issuer-uri=https://token.actions.githubusercontent.com \
        --attribute-mapping="$mapping" --attribute-condition="$condition" --quiet
else
    gcloud iam workload-identity-pools providers create-oidc "$provider" \
        --workload-identity-pool="$pool" --project="$project" --location=global \
        --issuer-uri=https://token.actions.githubusercontent.com \
        --attribute-mapping="$mapping" --attribute-condition="$condition" --quiet
fi

gcloud iam service-accounts add-iam-policy-binding "$deploy_sa" --project="$project" \
    --role=roles/iam.workloadIdentityUser \
    --member="principalSet://iam.googleapis.com/projects/$project_number/locations/global/workloadIdentityPools/$pool/attribute.repository_id/$repo_id" --quiet >/dev/null

for role in roles/cloudbuild.builds.editor roles/run.developer roles/serviceusage.serviceUsageConsumer; do
    gcloud projects add-iam-policy-binding "$project" --member="serviceAccount:$deploy_sa" \
        --role="$role" --condition=None --quiet >/dev/null
done
gcloud projects add-iam-policy-binding "$project" --member="serviceAccount:$build_sa" \
    --role=roles/logging.logWriter --condition=None --quiet >/dev/null

gcloud artifacts repositories add-iam-policy-binding "$repository" --project="$project" \
    --location="$region" --member="serviceAccount:$build_sa" --role=roles/artifactregistry.writer --quiet >/dev/null
gcloud artifacts repositories add-iam-policy-binding "$repository" --project="$project" \
    --location="$region" --member="serviceAccount:$deploy_sa" --role=roles/artifactregistry.reader --quiet >/dev/null

gcloud storage buckets add-iam-policy-binding "$source_bucket" \
    --member="serviceAccount:$deploy_sa" --role=roles/storage.objectAdmin --quiet >/dev/null
gcloud storage buckets add-iam-policy-binding "$source_bucket" \
    --member="serviceAccount:$deploy_sa" --role=roles/storage.legacyBucketReader --quiet >/dev/null
gcloud storage buckets add-iam-policy-binding "$source_bucket" \
    --member="serviceAccount:$build_sa" --role=roles/storage.objectViewer --quiet >/dev/null

for account in "$build_sa" "$runtime_sa"; do
    gcloud iam service-accounts add-iam-policy-binding "$account" --project="$project" \
        --member="serviceAccount:$deploy_sa" --role=roles/iam.serviceAccountUser --quiet >/dev/null
done

# Provider names and service-account addresses are configuration, not secrets.
gh variable set GCP_PROJECT_ID --repo="$repo" --body="$project"
gh variable set GCP_REGION --repo="$repo" --body="$region"
gh variable set WEB_SERVICE --repo="$repo" --body="$service"
gh variable set ARTIFACT_REPOSITORY --repo="$repo" --body="$repository"
gh variable set GCP_WIF_PROVIDER --repo="$repo" \
    --body="projects/$project_number/locations/global/workloadIdentityPools/$pool/providers/$provider"
gh variable set GCP_DEPLOY_SERVICE_ACCOUNT --repo="$repo" --body="$deploy_sa"
gh variable set GCP_BUILD_SERVICE_ACCOUNT --repo="$repo" --body="$build_sa"
gh variable set GCP_RUNTIME_SERVICE_ACCOUNT --repo="$repo" --body="$runtime_sa"

echo "Configured keyless GitHub deployment for $repo main."
