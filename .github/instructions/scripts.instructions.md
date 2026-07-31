---
description: "Use when running or editing CleanSquad's cross-platform PowerShell build, test, and ReSharper cleanup scripts."
name: "CleanSquad Cross-Platform PowerShell Scripts"
applyTo: "scripts/**/*.ps1"
---
# CleanSquad Cross-Platform PowerShell Scripts

The scripts in `scripts/` provide local PowerShell equivalents of the build, unit-test, and ReSharper cleanup jobs in `.github/workflows`. The same `.ps1` files MUST run unchanged on Windows, macOS, and Linux using PowerShell Core (`pwsh`).

## Platform requirements

- PowerShell Core 7 or later (`pwsh`) MUST be used. Windows PowerShell 5.1 is not supported.
- The .NET SDK selected by `global.json` MUST be installed.
- Scripts MUST use PowerShell and .NET APIs for path and file handling.
- Scripts MUST NOT depend on drive-letter paths, backslash separators, Bash, or operating-system-specific commands.
- Scripts MUST resolve the repository root from `$PSScriptRoot` so they can be launched from any working directory.
- Scripts MUST stop and return a non-zero exit code when an underlying command fails.

`scripts/common.ps1` contains shared path and process handling for the entry-point scripts and MUST NOT be run directly.

## Build

Run the CI-equivalent Release build from the repository root:

```powershell
pwsh -File ./scripts/build.ps1
```

The script restores packages and performs the build used by `full-build.yml`, with incremental compilation disabled and warnings treated as errors.

Useful options:

```powershell
# Build Debug instead of Release.
pwsh -File ./scripts/build.ps1 -Configuration Debug

# Skip restore when packages are already restored.
pwsh -File ./scripts/build.ps1 -NoRestore

# Apply the same semantic version that GitVersion supplies in CI.
pwsh -File ./scripts/build.ps1 -Version 1.2.3
```

When `GITVERSION_SEMVER` is set, the script uses it as the default `Version`. Otherwise, it uses the normal MSBuild version.

## Test

Run the CI-equivalent unit-test sequence:

```powershell
pwsh -File ./scripts/test.ps1
```

The script restores, builds in Release, and runs tests matching `FullyQualifiedName~.UnitTests.`. It writes TRX results to `.scratchpad/coverage-test-results`, matching `unit-tests.yml`.

Useful options:

```powershell
# Reuse an existing restore and build.
pwsh -File ./scripts/test.ps1 -NoRestore -NoBuild

# Run a narrower test selection.
pwsh -File ./scripts/test.ps1 -Filter 'FullyQualifiedName~WorkflowDefinitionLoaderTests'
```

## ReSharper cleanup

Run the ReSharper full-cleanup sequence:

```powershell
pwsh -File ./scripts/cleanup.ps1
```

The script performs these steps:

1. Restores and builds the solution in Release with warnings treated as errors.
2. Restores the repository-local tools declared in `.config/dotnet-tools.json`.
3. Generates a temporary legacy solution for ReSharper.
4. Runs the `Built-in: Full Cleanup` profile with `Directory.DotSettings`.
5. Removes the temporary solution, including when cleanup fails.

ReSharper cleanup can modify tracked source files. Review the result with `git diff` before committing it. JetBrains analysis caches are stored under the operating system's temporary directory.

For a faster rerun after the solution and tools are ready:

```powershell
pwsh -File ./scripts/cleanup.ps1 -NoRestore -NoBuild -NoToolRestore
```
