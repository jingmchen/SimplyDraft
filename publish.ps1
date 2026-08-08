# publish.ps1

$ErrorActionPreference = "Stop"

# ============================================================
# Configuration
# ============================================================

$project = ".\src\SimplyDraft.AppHost\SimplyDraft.AppHost.csproj"
$exeName = "SimplyDraft.exe"
$pfxFile = "SimplyDraft-CodeSigning.pfx" # look at same dir of this ps1 script
$configuration = "Release"
$runtime = "win-x64"
$publishDir = Join-Path $PSScriptRoot "publish\$runtime"


# ============================================================
# Helper
# ============================================================

function Assert-LastExitCode {
    param(
        [string]$Message
    )

    if ($LASTEXITCODE -ne 0) {
        throw "$Message (exit code $LASTEXITCODE)"
    }
}


# ============================================================
# Resolve paths
# ============================================================

$projectPath = Join-Path $PSScriptRoot $project
$pfxPath = Join-Path $PSScriptRoot $pfxFile
$exePath = Join-Path $publishDir $exeName


# ============================================================
# Check project
# ============================================================

if (-not (Test-Path $projectPath)) {
    throw "Project not found: $projectPath"
}


# ============================================================
# Check signing certificate
# ============================================================

if (-not (Test-Path $pfxPath)) {
    throw @"
Signing certificate not found:

$pfxPath

Place '$pfxFile' beside publish.ps1.
"@
}

Write-Host ""
Write-Host "Signing certificate:"
Write-Host "  $pfxPath"


# ============================================================
# Ask for PFX password
# ============================================================

$pfxPassword = Read-Host "Enter PFX password"


# ============================================================
# Find signtool.exe
# ============================================================

Write-Host ""
Write-Host "Looking for signtool.exe..."

$windowsKitsBin = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"

if (-not (Test-Path $windowsKitsBin)) {
    throw @"
Windows SDK was not found.

Expected directory:
$windowsKitsBin

Install the Windows SDK and run this script again.
"@
}

$signTool = Get-ChildItem $windowsKitsBin `
    -Filter "signtool.exe" `
    -File `
    -Recurse `
    -ErrorAction SilentlyContinue |
    Where-Object {
        $_.FullName -match '\\x64\\signtool\.exe$'
    } |
    Sort-Object FullName -Descending |
    Select-Object -First 1

if (-not $signTool) {
    throw "signtool.exe was not found. Install the Windows SDK."
}

$signToolPath = $signTool.FullName

Write-Host "Using:"
Write-Host "  $signToolPath"


# ============================================================
# Clean previous publish output
# ============================================================

Write-Host ""
Write-Host "Cleaning previous publish..."

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

New-Item `
    -Path $publishDir `
    -ItemType Directory `
    -Force |
    Out-Null


# ============================================================
# Publish application
# ============================================================

Write-Host ""
Write-Host "Publishing application..."
Write-Host "  Project: $projectPath"
Write-Host "  Runtime: $runtime"
Write-Host "  Output:  $publishDir"
Write-Host ""

dotnet publish $projectPath `
    -c $configuration `
    -r $runtime `
    -p:PublishDir="$publishDir\"

Assert-LastExitCode "dotnet publish failed"


# ============================================================
# Check published EXE
# ============================================================

if (-not (Test-Path $exePath)) {
    throw @"
Publish completed, but the expected executable was not found:

$exePath

Check the `$exeName setting at the top of publish.ps1.
"@
}

Write-Host ""
Write-Host "Published executable:"
Write-Host "  $exePath"


# ============================================================
# Sign executable
# ============================================================

Write-Host ""
Write-Host "Signing executable..."

& $signToolPath sign `
    /f $pfxPath `
    /p $pfxPassword `
    /fd SHA256 `
    $exePath

Assert-LastExitCode "Signing failed"


# ============================================================
# Clear password variable
# ============================================================

$pfxPassword = $null


# ============================================================
# Verify signature
# ============================================================

Write-Host ""
Write-Host "Verifying digital signature..."
Write-Host ""

& $signToolPath verify `
    /pa `
    /v `
    $exePath

Assert-LastExitCode "Signature verification failed"


# ============================================================
# Finished
# ============================================================

Write-Host ""
Write-Host "============================================================"
Write-Host " Publish and signing successful"
Write-Host "============================================================"
Write-Host ""
Write-Host "Signed executable:"
Write-Host "  $exePath"
Write-Host ""