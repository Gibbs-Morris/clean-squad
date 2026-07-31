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
    [switch] $NoRestore,

    [Parameter()]
    [switch] $NoBuild,

    [Parameter()]
    [switch] $NoToolRestore
)

. (Join-Path $PSScriptRoot 'common.ps1')

$dotSettingsPath = Join-Path $RepositoryRoot 'Directory.DotSettings'
$generatedSolutionDirectory = Join-Path $RepositoryRoot '.scratchpad/cleanup'
$generatedSolutionPath = Join-Path $generatedSolutionDirectory 'CleanSquad.sln'
$cacheRoot = Join-Path ([System.IO.Path]::GetTempPath()) 'CleanSquad'
$cachesHome = Join-Path $cacheRoot 'jb-caches'

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
        $buildArguments.Add('-warnaserror')
        Add-VersionArgument -ArgumentList $buildArguments -Version $Version

        Invoke-CheckedCommand -FilePath 'dotnet' -ArgumentList $buildArguments
    }

    if (-not $NoToolRestore) {
        Invoke-CheckedCommand -FilePath 'dotnet' -ArgumentList @('tool', 'restore')
    }

    $null = New-Item -ItemType Directory -Path $generatedSolutionDirectory -Force
    $null = New-Item -ItemType Directory -Path $cachesHome -Force

    Invoke-CheckedCommand -FilePath 'dotnet' -ArgumentList @(
        'tool'
        'run'
        'slngen'
        $SolutionPath
        '--solutionfile'
        $generatedSolutionPath
        '--launch'
        'false'
    )

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        Invoke-CheckedCommand -FilePath 'dotnet' -ArgumentList @(
            'tool'
            'run'
            'jb'
            'cleanupcode'
            '--profile=Built-in: Full Cleanup'
            "--settings=$dotSettingsPath"
            "--caches-home=$cachesHome"
            '--no-updates'
            $generatedSolutionPath
            '--LogLevel=TRACE'
        )
    }
    finally {
        $stopwatch.Stop()
    }

    Write-Host ''
    Write-Host 'Cleanup completed.' -ForegroundColor Green
    Write-Host "Duration: $($stopwatch.Elapsed.ToString('hh\:mm\:ss\.fff'))"
}
finally {
    if (Test-Path -LiteralPath $generatedSolutionPath) {
        Remove-Item -LiteralPath $generatedSolutionPath -Force
    }

    Pop-Location
}
