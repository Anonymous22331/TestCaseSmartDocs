$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$outputDir = Join-Path $projectRoot 'artifacts'
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$archivePath = Join-Path $outputDir 'TestCaseSmartDocs.zip'

function Get-SourceFiles([string]$directory) {
    foreach ($item in Get-ChildItem -LiteralPath $directory -Force) {
        if ($item.PSIsContainer) {
            if ($item.Name -notin @('.git', 'node_modules', 'bin', 'obj', 'dist', 'artifacts', '.rollup.cache')) {
                Get-SourceFiles $item.FullName
            }
        } else { $item }
    }
}

$archiveStream = [IO.File]::Open($archivePath, [IO.FileMode]::Create)
$archive = New-Object IO.Compression.ZipArchive($archiveStream, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-SourceFiles $projectRoot) {
        $relativePath = $file.FullName.Substring($projectRoot.Length + 1).Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $relativePath) | Out-Null
    }
} finally { $archive.Dispose(); $archiveStream.Dispose() }
Write-Output $archivePath
