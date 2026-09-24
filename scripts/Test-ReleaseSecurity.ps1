[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+$')]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$releaseRoot = Join-Path $repositoryRoot 'release'
$stagingDirectory = Join-Path $releaseRoot "OneBoardCaptureTranslate-$Version-win-x64"
$zipPath = Join-Path $releaseRoot "OneBoardCaptureTranslate-$Version-win-x64.zip"
$installerPath = Join-Path $releaseRoot "OneBoardCaptureTranslate-Setup-$Version-win-x64.exe"
$mainExePath = Join-Path $stagingDirectory 'OneBoardCaptureTranslate.exe'
$temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("oneboard-security-" + [Guid]::NewGuid().ToString('N'))

$sensitivePatterns = @(
    '-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----',
    'private_key.{0,5}:[^\r\n]{20,}',
    'AIza[0-9A-Za-z_-]{30,}',
    'sk-[A-Za-z0-9]{40,64}',
    'sk-proj-[A-Za-z0-9_-]{20,}',
    'sk-svcacct-[A-Za-z0-9_-]{20,}',
    'github_pat_[A-Za-z0-9_]{20,}',
    'ghp_[A-Za-z0-9]{30,}',
    '(?i)Bearer\s+[A-Za-z0-9._~-]{24,}'
)

function Assert-NoSensitivePattern {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    foreach ($pattern in $sensitivePatterns) {
        & rg --text --files-with-matches --no-messages --regexp $pattern -- $Path | Out-Null
        if ($LASTEXITCODE -eq 0) {
            throw "Potential credential pattern found in $Label."
        }
        if ($LASTEXITCODE -gt 1) {
            throw "Credential scan failed for $Label."
        }
    }
}

foreach ($requiredPath in @($stagingDirectory, $zipPath, $installerPath, $mainExePath)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required release artifact is missing: $requiredPath"
    }
}

$forbiddenNames = @(
    'setting.json',
    'setting.json.bak',
    'translation_history.db',
    'translation_history.db-journal',
    'translation_history.db-wal',
    'translation_history.db-shm',
    'credentials.json',
    'secrets.json'
)
$forbiddenExtensions = @('.pdb', '.cs', '.xaml', '.csproj', '.sln', '.pem', '.p12', '.pfx', '.key')
$stagedFiles = @(Get-ChildItem -LiteralPath $stagingDirectory -Recurse -File)
$forbiddenStagedFiles = @($stagedFiles | Where-Object {
    $forbiddenNames -contains $_.Name.ToLowerInvariant() -or
    $forbiddenExtensions -contains $_.Extension.ToLowerInvariant() -or
    $_.Name -match '(?i)service-account|gcp-credentials'
})
if ($forbiddenStagedFiles.Count -gt 0) {
    throw "Forbidden files are present in the release staging directory."
}

New-Item -ItemType Directory -Path $temporaryDirectory | Out-Null
try {
    Expand-Archive -LiteralPath $zipPath -DestinationPath $temporaryDirectory
    $zipFiles = @(Get-ChildItem -LiteralPath $temporaryDirectory -Recurse -File)
    $forbiddenZipFiles = @($zipFiles | Where-Object {
        $forbiddenNames -contains $_.Name.ToLowerInvariant() -or
        $forbiddenExtensions -contains $_.Extension.ToLowerInvariant() -or
        $_.Name -match '(?i)service-account|gcp-credentials'
    })
    if ($forbiddenZipFiles.Count -gt 0) {
        throw "Forbidden files are present in the portable ZIP."
    }

    Assert-NoSensitivePattern -Path $stagingDirectory -Label 'portable staging / installer input'
    Assert-NoSensitivePattern -Path $temporaryDirectory -Label 'expanded portable ZIP'
    Assert-NoSensitivePattern -Path $mainExePath -Label 'main executable'
    Assert-NoSensitivePattern -Path $zipPath -Label 'portable ZIP container'
    Assert-NoSensitivePattern -Path $installerPath -Label 'installer executable'
}
finally {
    if (Test-Path -LiteralPath $temporaryDirectory) {
        Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force
    }
}

$historyPatterns = @(
    'AIza[0-9A-Za-z_-]{30,}',
    'sk-[A-Za-z0-9]{40,64}',
    'sk-proj-[A-Za-z0-9_-]{20,}',
    'sk-svcacct-[A-Za-z0-9_-]{20,}',
    '-----BEGIN PRIVATE KEY-----',
    'private_key'
)
foreach ($pattern in $historyPatterns) {
    $commits = @(& git -C $repositoryRoot log HEAD --format='%H' -G $pattern -- . ':!*.md' ':!*.txt' ':!scripts/Test-ReleaseSecurity.ps1')
    if ($LASTEXITCODE -ne 0) {
        throw 'Git-history credential scan failed.'
    }
    if ($commits.Count -gt 0) {
        throw 'Potential credential pattern exists in the release branch history.'
    }
}

Write-Output 'RELEASE SECURITY SCAN: PASS'
Write-Output 'Current release-branch history: PASS'
Write-Output 'Main EXE: PASS'
Write-Output 'Portable staging and ZIP: PASS'
Write-Output 'Installer input and EXE: PASS'

