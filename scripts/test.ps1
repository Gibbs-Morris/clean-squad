#Requires -Version 7.0
#Requires -PSEdition Core

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [Parameter()]
    [AllowEmptyString()]
    [string] $Version = $env:GITVERSION_SEMVER,

    [Parameter()]
    [string] $Filter = 'FullyQualifiedName~.UnitTests.',

    [Parameter()]
    [switch] $NoRestore,

    [Parameter()]
    [switch] $NoBuild
)

. (Join-Path $PSScriptRoot 'common.ps1')

$resultsDirectory = Join-Path $RepositoryRoot '.scratchpad/coverage-test-results'

Push-Location $RepositoryRoot

try {
    if (-not $NoRestore) {
        Invoke-CheckedCommand -FilePath 'dotnet' -ArgumentList @('restore', $SolutionPath)
    }

    if (-not $NoBuild) {
        $buildArguments = [System.Collections.Generic.List[string]]::new()
        $buildArguments.Add('build')
        $buildArguments.Add($SolutionPath)
        $buildArguments.Add('--configuration')
        $buildArguments.Add($Configuration)
        $buildArguments.Add('--no-restore')
        $buildArguments.Add('--no-incremental')
        Add-VersionArgument -ArgumentList $buildArguments -Version $Version

        Invoke-CheckedCommand -FilePath 'dotnet' -ArgumentList $buildArguments
    }

    $null = New-Item -ItemType Directory -Path $resultsDirectory -Force

    Invoke-CheckedCommand -FilePath 'dotnet' -ArgumentList @(
        'test'
        $SolutionPath
        '--no-build'
        '--configuration'
        $Configuration
        '--filter'
        $Filter
        '--logger'
        'trx;LogFileName=test_results.trx'
        '--results-directory'
        $resultsDirectory
    )
}
finally {
    Pop-Location
}
