# WebGL deployment

The production service is `jalan-kita-web` in project `ujian-sim-514002`, region
`asia-southeast2`. Nginx Alpine Slim serves the exported Unity player. Cloud Run
terminates TLS and forwards HTTP/2 cleartext to Nginx, including data files over
the HTTP/1 response-size limit.

## Local deployment

Install the Unity CLI, the Editor version in `Unity/ProjectSettings/ProjectVersion.txt`
with WebGL Build Support, and Google Cloud CLI. Activate Unity and authenticate
gcloud with an account allowed to build and deploy. Close this project's Unity
Editor before the batch build, then run:

```sh
make deploy/web
```

Every invocation rebuilds the checked-out source before submitting the image to
Cloud Build. A Unity failure stops deployment. `make build/web` only builds Unity.
Build logs are under `Unity/Logs/`. Local Docker is not required.

Override `GCP_PROJECT`, `GCP_REGION`, `WEB_SERVICE`, `ARTIFACT_REPOSITORY`,
`UNITY_CLI`, `UNITY_PROJECT_DIR`, or `WEB_BUILD_DIR` when needed.

## GitHub Actions

`.github/workflows/deploy-web.yml` runs on pushes to `main` and manual dispatches.
It builds WebGL on a GitHub-hosted Ubuntu runner, uploads that run's export,
authenticates through Google Workload Identity Federation, submits a Cloud Build,
and deploys the image by digest. A final check downloads every exported file and
compares its hash and source commit. Only a successful fresh Unity build can
reach deployment. Superseded commits are skipped before publishing; deployments
are serialized.

### One-time administrator setup

The Artifact Registry `web` repository, source bucket, and production Cloud Run
service must already exist (the local deployment command can create them).
Run with project-administrator gcloud credentials and repository-admin gh access:

```sh
sh deploy/web/setup-github.sh
```

The script enables the required APIs, creates the federation pool/provider and
three service accounts, grants scoped roles, and sets the repository's `GCP_*`,
`WEB_SERVICE`, and `ARTIFACT_REPOSITORY` Actions variables. It creates no service
account keys.

The provider accepts only this repository's numeric ID and owner ID, `main`,
the exact `deploy-web.yml` workflow, and push/manual events. Pull requests and
forks cannot use its Google identity. The accounts are separated:

| Account | Access |
| --- | --- |
| `github-web-deployer` | Submit builds, upload sources to the build bucket, read images, update Cloud Run, and act as only the dedicated builder/runtime accounts |
| `web-cloud-build` | Read staged build sources, write images to the `web` repository, write build logs |
| `jalan-kita-web-runtime` | No project roles; serve static files |

CI does not enable APIs, create repositories, or change public invocation IAM.
The service's existing public access is retained. Cloud Build logs are in Cloud
Logging and linked from the build's console page.

### Unity activation

Google federation handles Google authentication; Unity requires its own CI
activation. Add these in **Settings → Secrets and variables → Actions**:

- `UNITY_EMAIL` and `UNITY_PASSWORD`: the Unity account used for CI activation.
  The pinned GameCI CLI supports account-based Unity Personal activation, so
  Personal users do not need to obtain a legacy `.ulf` file.
- Professional license: also set `UNITY_SERIAL`.
- If your license supports file activation, `UNITY_LICENSE` can contain that
  activated license file instead.

See the pinned [GameCI activation implementation](https://github.com/game-ci/cli/blob/v0.1.72/dist/platforms/ubuntu/steps/activate.sh)
and [activation guidance](https://game.ci/docs/github/activation/).
Enter credentials in GitHub's secret UI or with `gh secret set`; never put them
in source, workflow variables, logs, or chat. Without activation configuration,
the build reports the missing setup and does not deploy an older export.

For a Google-only check, manually run **Deploy WebGL** with
`verify_identity_only` enabled. This verifies real GitHub OIDC authentication
and resource access without requiring a Unity license or changing production.
