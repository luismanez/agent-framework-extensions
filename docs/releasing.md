# Releasing the NuGet packages

Retrieval and WorkContext share one repository version. The `v<version>` tag triggers [the release workflow](../.github/workflows/release.yml), which builds and tests the solution, packs both libraries with the tag version, checks for both `.nupkg` and `.snupkg` files, pushes both packages to NuGet.org, and attaches all four files to one GitHub release. [CI](../.github/workflows/ci.yml) packs and checks both libraries on every pull request and push to `main`.

## Before the first joint release

1. In the `luismanez` NuGet.org account, check the Trusted Publishing policy for GitHub repository `luismanez/agent-framework-extensions` and workflow file `release.yml`. Its scope must allow **new packages** for `Acterion.Agents.AI.Microsoft365.WorkContext` and **new versions** of `Acterion.Agents.AI.Microsoft365.Retrieval`. The workflow uses `NuGet/login@v1` and does not need a long-lived API key. These account settings cannot be verified from the repository.
2. Check that the WorkContext package ID is available on NuGet.org. Publishing a new ID is permanent; a published package version cannot be replaced.
3. Merge the release changes to `main` and wait for CI to pass. Inspect the CI artifact `nuget-packages` for both package IDs and their symbols.

## Create a release

1. Update the shared `<Version>` in [`nuget-package.props`](../nuget/nuget-package.props) and the release notes in each package project. Both projects must declare the exact version in the tag. The first joint release is prepared as `1.1.0`.
2. Merge those changes to `main` and wait for CI. From an up-to-date checkout of `main`, create and push an annotated tag:

   ```sh
   git tag -a v1.1.0 -m "Release v1.1.0"
   git push origin v1.1.0
   ```

3. Check the GitHub Actions **Release** run, the GitHub release assets, and both package pages on NuGet.org. A failed run can be rerun: the NuGet push step skips an already published package version so it can finish after a partial upload. Never move an existing release tag to different source code.

The tag points to one commit, and each NuGet package keeps its own package ID. The shared version is a release policy for this repository, not a NuGet requirement. Both package IDs will advance together, even when only one package's code changed.
