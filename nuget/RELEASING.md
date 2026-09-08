# Releasing to NuGet.org

Releases are published automatically by GitHub Actions when a version tag is pushed. Authentication uses NuGet Trusted Publishing and does not store a long-lived NuGet API key in GitHub.

## One-time setup

### NuGet.org

Sign in as the `luismanez` package owner and add a Trusted Publishing policy with these values:

| Setting | Value |
| --- | --- |
| Owner | `luismanez` |
| Repository owner | `luismanez` |
| Repository | `agent-framework-extensions` |
| Workflow file | `release.yml` |
| Environment | Leave empty |
| Scope | Push new packages and package versions |
| Package glob | `Acterion.Agents.AI.Microsoft365.Retrieval` |

The workflow filename must be entered without the `.github/workflows/` path.
No GitHub Actions secrets are required. The public NuGet.org profile name is declared in the workflow, while NuGet.org issues a temporary credential through OIDC for each release.

## Publish a release

1. Ensure the release commit is pushed to `main` and all CI checks pass.
2. Create an annotated tag whose name is `v` followed by the NuGet version.
3. Push the tag.

For the first preview:

```sh
git switch main
git pull --ff-only
git tag -a v0.1.0-preview.1 -m "Release 0.1.0-preview.1"
git push origin v0.1.0-preview.1
```

The release workflow validates the tag, builds and tests the solution, creates the `.nupkg` and `.snupkg`, uploads them as GitHub Actions artifacts, obtains a temporary NuGet credential through OIDC, and publishes the package and symbols.

Package versions are immutable. If publication fails after NuGet.org accepts the package, do not reuse the version; inspect the workflow and publish a new version instead.