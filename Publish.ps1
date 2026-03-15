<#
.SYNOPSIS
    Publishes the PanoramicData.Mapper package to NuGet.org

.DESCRIPTION
    This script performs the following steps:
    1. Checks for uncommitted changes (git porcelain)
    2. Determines the Nerdbank git version
    3. Validates nuget-key.txt exists, has content, and is gitignored
    4. Runs unit tests (unless -SkipTests is specified)
    5. Publishes to nuget.org

.PARAMETER SkipTests
    If specified, skips running unit tests

.EXAMPLE
    .\Publish.ps1
    .\Publish.ps1 -SkipTests
#>

param(
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'

# Step 1: Check for uncommitted changes (git porcelain)
Write-Information "Checking for uncommitted changes..." -InformationAction Continue
$gitStatus = git status --porcelain
if ($gitStatus) {
    Write-Error "ERROR: There are uncommitted changes in the repository. Please commit or stash them before publishing."
    exit 1
}
Write-Information "No uncommitted changes detected." -InformationAction Continue

# Step 2: Determine the Nerdbank git version
Write-Information "Determining Nerdbank git version..." -InformationAction Continue
$version = nbgv get-version -v NuGetPackageVersion
if ($LASTEXITCODE -ne 0) {
    Write-Error "ERROR: Failed to determine Nerdbank git version. Ensure nbgv is installed (dotnet tool install -g nbgv)."
    exit 1
}
Write-Information "Version: $version" -InformationAction Continue

# Step 3: Check that nuget-key.txt exists, has content, and is gitignored
Write-Information "Validating nuget-key.txt..." -InformationAction Continue
$nugetKeyPath = Join-Path $PSScriptRoot "nuget-key.txt"

if (-not (Test-Path $nugetKeyPath)) {
    Write-Error "ERROR: nuget-key.txt does not exist in the solution root."
    exit 1
}

$nugetKey = (Get-Content $nugetKeyPath -Raw).Trim()
if ([string]::IsNullOrWhiteSpace($nugetKey)) {
    Write-Error "ERROR: nuget-key.txt is empty."
    exit 1
}

# Check if nuget-key.txt is gitignored
$null = git check-ignore -q "nuget-key.txt" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "ERROR: nuget-key.txt is not in .gitignore. This is a security risk."
    exit 1
}
Write-Information "nuget-key.txt validated and is gitignored." -InformationAction Continue

# Step 4: Run unit tests (unless -SkipTests is specified)
if (-not $SkipTests) {
    Write-Information "Running unit tests..." -InformationAction Continue
    dotnet test "$PSScriptRoot\PanoramicData.Mapper.Test\PanoramicData.Mapper.Test.csproj" --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Error "ERROR: Unit tests failed."
        exit 1
    }
    Write-Information "Unit tests passed." -InformationAction Continue
} else {
    Write-Warning "Skipping unit tests as requested."
}

# Step 5: Build and publish to nuget.org
Write-Information "Building package..." -InformationAction Continue
dotnet build "$PSScriptRoot\PanoramicData.Mapper\PanoramicData.Mapper.csproj" --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Error "ERROR: Build failed."
    exit 1
}

$packagePath = Join-Path $PSScriptRoot "PanoramicData.Mapper\bin\Release\PanoramicData.Mapper.$version.nupkg"
if (-not (Test-Path $packagePath)) {
    Write-Error "ERROR: Package not found at expected path: $packagePath"
    exit 1
}

Write-Information "Publishing to nuget.org..." -InformationAction Continue
dotnet nuget push $packagePath --api-key $nugetKey --source https://api.nuget.org/v3/index.json --skip-duplicate
if ($LASTEXITCODE -ne 0) {
    Write-Error "ERROR: Failed to publish to nuget.org."
    exit 1
}

Write-Information "Successfully published PanoramicData.Mapper version $version to nuget.org!" -InformationAction Continue
exit 0
