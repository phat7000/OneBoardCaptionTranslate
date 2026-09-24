[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$')]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$productName = 'OneBoard Capture Translate'
$artifactBaseName = "OneBoardCaptureTranslate-$Version-win-x64"
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$releaseRoot = Join-Path $repositoryRoot 'release'
$stagingDirectory = Join-Path $releaseRoot $artifactBaseName
$publishDirectory = Join-Path $releaseRoot '.portable-publish-win-x64'
$zipPath = Join-Path $releaseRoot "$artifactBaseName.zip"
$hashPath = Join-Path $releaseRoot "OneBoardCaptureTranslate-$Version-SHA256.txt"
$manifestPath = Join-Path $releaseRoot "OneBoardCaptureTranslate-$Version-MANIFEST.txt"
$projectPath = Join-Path $repositoryRoot 'LiveCaptionsTranslator.csproj'
$executableName = 'OneBoardCaptureTranslate.exe'
$executablePath = Join-Path $stagingDirectory $executableName

function Assert-ReleaseChildPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $resolvedReleaseRoot = [System.IO.Path]::GetFullPath($releaseRoot).TrimEnd('\') + '\'
    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    if (-not $resolvedPath.StartsWith($resolvedReleaseRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify a path outside the release directory: $resolvedPath"
    }
}

New-Item -ItemType Directory -Force -Path $releaseRoot | Out-Null
foreach ($path in @($stagingDirectory, $publishDirectory, $zipPath, $hashPath, $manifestPath)) {
    Assert-ReleaseChildPath -Path $path
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}

New-Item -ItemType Directory -Force -Path $stagingDirectory, $publishDirectory | Out-Null

try {
    & dotnet publish $projectPath -c Release -r win-x64 --self-contained true -o $publishDirectory
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }

    $publishedExecutable = Join-Path $publishDirectory $executableName
    if (-not (Test-Path -LiteralPath $publishedExecutable -PathType Leaf)) {
        throw "Expected published executable was not produced: $publishedExecutable"
    }

    Copy-Item -LiteralPath $publishedExecutable -Destination $executablePath

    $approvedDocuments = @(
        'LICENSE',
        'UPSTREAM_ATTRIBUTION.md',
        'THIRD_PARTY_NOTICES.md',
        'README.md',
        'README_zh-CN.md',
        'RELEASE_NOTES.md'
    )
    foreach ($document in $approvedDocuments) {
        $sourcePath = Join-Path $repositoryRoot $document
        if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
            throw "Required package document is missing: $sourcePath"
        }
        Copy-Item -LiteralPath $sourcePath -Destination (Join-Path $stagingDirectory $document)
    }

    # Append the exact license/notice texts shipped by dependencies whose
    # package-specific terms are longer than the consolidated repository index.
    $globalPackagesOutput = (& dotnet nuget locals global-packages --list).Trim()
    if ($LASTEXITCODE -ne 0 -or $globalPackagesOutput -notmatch '^global-packages:\s*(.+)$') {
        throw 'Unable to locate the restored NuGet global-packages directory.'
    }
    $globalPackagesDirectory = $Matches[1]
    $noticeAppendices = @(
        @{ Title = 'Interop.UIAutomationClient 10.19041.0 - LICENSE.txt'; Path = 'interop.uiautomationclient\10.19041.0\LICENSE.txt' }
        @{ Title = 'System.Text.Json 8.0.5 - LICENSE.TXT'; Path = 'system.text.json\8.0.5\LICENSE.TXT' }
        @{ Title = 'System.Text.Json 8.0.5 - THIRD-PARTY-NOTICES.TXT'; Path = 'system.text.json\8.0.5\THIRD-PARTY-NOTICES.TXT' }
        @{ Title = 'System.Net.Http 4.3.4 - dotnet_library_license.txt'; Path = 'system.net.http\4.3.4\dotnet_library_license.txt' }
        @{ Title = 'System.Net.Http 4.3.4 - ThirdPartyNotices.txt'; Path = 'system.net.http\4.3.4\ThirdPartyNotices.txt' }
        @{ Title = 'WPF-UI 4.0.1 - LICENSE.md'; Path = 'wpf-ui\4.0.1\LICENSE.md' }
        @{ Title = 'WPF-UI 4.0.1 - ThirdPartyNotices.txt'; Path = 'wpf-ui\4.0.1\ThirdPartyNotices.txt' }
        @{ Title = 'Microsoft.CognitiveServices.Speech 1.51.2 - LICENSE.txt'; Path = 'microsoft.cognitiveservices.speech\1.51.2\LICENSE.txt' }
        @{ Title = 'NAudio 2.2.1 - license.txt'; Path = 'naudio\2.2.1\license.txt' }
        @{ Title = 'Google.Cloud.Speech.V1 3.9.0 - LICENSE'; Path = 'google.cloud.speech.v1\3.9.0\LICENSE' }
        @{ Title = 'Google.Api.CommonProtos 2.17.0 - LICENSE'; Path = 'google.api.commonprotos\2.17.0\LICENSE' }
        @{ Title = 'Google.Api.Gax 4.12.1 - LICENSE'; Path = 'google.api.gax\4.12.1\LICENSE' }
    )
    $packagedNoticePath = Join-Path $stagingDirectory 'THIRD_PARTY_NOTICES.md'
    foreach ($appendix in $noticeAppendices) {
        $appendixPath = Join-Path $globalPackagesDirectory $appendix.Path
        if (-not (Test-Path -LiteralPath $appendixPath -PathType Leaf)) {
            throw "Required dependency notice is missing: $($appendix.Title)"
        }
        Add-Content -LiteralPath $packagedNoticePath -Encoding UTF8 -Value @(
            ''
            '---'
            ''
            "## Exact packaged notice: $($appendix.Title)"
            ''
            '```text'
            (Get-Content -Raw -LiteralPath $appendixPath)
            '```'
        )
    }
}
finally {
    if (Test-Path -LiteralPath $publishDirectory) {
        Remove-Item -LiteralPath $publishDirectory -Recurse -Force
    }
}

$forbiddenNames = @(
    'setting.json',
    'setting.json.bak',
    'translation_history.db',
    'translation_history.db-journal',
    'translation_history.db-wal',
    'translation_history.db-shm'
)
$forbiddenExtensions = @('.pdb', '.cs', '.xaml', '.csproj', '.sln')
$packagedFiles = @(Get-ChildItem -LiteralPath $stagingDirectory -Recurse -File)
$forbiddenFiles = @($packagedFiles | Where-Object {
    $forbiddenNames -contains $_.Name -or $forbiddenExtensions -contains $_.Extension.ToLowerInvariant()
})
if ($forbiddenFiles.Count -gt 0) {
    $forbiddenList = ($forbiddenFiles | ForEach-Object { $_.Name }) -join ', '
    throw "Forbidden files found in staging: $forbiddenList"
}

$unexpectedFiles = @($packagedFiles | Where-Object {
    $_.Name -notin (@($executableName) + $approvedDocuments)
})
if ($unexpectedFiles.Count -gt 0) {
    $unexpectedList = ($unexpectedFiles | ForEach-Object { $_.Name }) -join ', '
    throw "Unexpected files found in staging: $unexpectedList"
}

$sensitivePatterns = @(
    'C:\\Users\\',
    'sk-[A-Za-z0-9_-]{20,}',
    'AIza[0-9A-Za-z_-]{20,}',
    '(?i)(api[_-]?key|access[_-]?token|secret)\s*[:=]\s*["''][^"'']{8,}["'']'
)
$textFiles = @($packagedFiles | Where-Object { $_.Extension -in @('.md', '.txt') -or $_.Name -eq 'LICENSE' })
foreach ($textFile in $textFiles) {
    foreach ($pattern in $sensitivePatterns) {
        if (Select-String -LiteralPath $textFile.FullName -Pattern $pattern -Quiet) {
            throw "Potential sensitive data pattern found in packaged text file: $($textFile.Name)"
        }
    }
}

$versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($executablePath)
if ($versionInfo.ProductName -ne $productName -or $versionInfo.CompanyName -ne 'OneBoard') {
    throw 'Executable product/company metadata validation failed.'
}
if ($versionInfo.Comments -ne 'Lightweight real-time translation for Windows.') {
    throw "Executable description metadata validation failed: $($versionInfo.Comments)"
}
if ($versionInfo.FileVersion -ne '1.1.0.0' -or $versionInfo.ProductVersion -ne $Version) {
    throw "Executable version metadata validation failed. File=$($versionInfo.FileVersion), Product=$($versionInfo.ProductVersion)"
}

$stream = [System.IO.File]::OpenRead($executablePath)
try {
    $reader = New-Object System.IO.BinaryReader($stream)
    $stream.Position = 0x3c
    $peOffset = $reader.ReadInt32()
    $stream.Position = $peOffset + 4
    $machine = $reader.ReadUInt16()
}
finally {
    $stream.Dispose()
}
if ($machine -ne 0x8664) {
    throw ('Executable architecture validation failed. PE machine: 0x{0:X4}' -f $machine)
}

Compress-Archive -LiteralPath $stagingDirectory -DestinationPath $zipPath -CompressionLevel Optimal

$executableHash = (Get-FileHash -LiteralPath $executablePath -Algorithm SHA256).Hash.ToUpperInvariant()
$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToUpperInvariant()

@(
    "$executableHash  $artifactBaseName\$executableName"
    "$zipHash  $artifactBaseName.zip"
) | Set-Content -LiteralPath $hashPath -Encoding ASCII

$gitCommit = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to determine Git commit.'
}
$buildTimestamp = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
$fileLines = foreach ($file in (Get-ChildItem -LiteralPath $stagingDirectory -File | Sort-Object Name)) {
    $relativePath = "$artifactBaseName\$($file.Name)"
    "{0}`t{1}" -f $file.Length, $relativePath
}

@(
    "Product: $productName"
    "Version: $Version"
    'Architecture: Windows x64'
    'Build type: Release, self-contained, single-file, portable/no-install'
    "Git commit: $gitCommit"
    "Build timestamp (UTC): $buildTimestamp"
    ''
    'Packaged files (bytes, relative path):'
    $fileLines
    ''
    "Executable SHA256: $executableHash"
    "ZIP SHA256: $zipHash"
) | Set-Content -LiteralPath $manifestPath -Encoding UTF8

Write-Output "ZIP: $zipPath"
Write-Output "Executable: $executablePath"
Write-Output "Executable SHA256: $executableHash"
Write-Output "ZIP SHA256: $zipHash"
Write-Output "Manifest: $manifestPath"
