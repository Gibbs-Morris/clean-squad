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
    [switch] $NoRestore
)

. (Join-Path $PSScriptRoot 'common.ps1')

Push-Location $RepositoryRoot

try {
    if (-not $NoRestore) {
        Invoke-CheckedCommand -FilePath 'dotnet' -ArgumentList @('restore', $SolutionPath)
    }

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
finally {
    Pop-Location
}
